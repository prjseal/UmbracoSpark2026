using Microsoft.AspNetCore.Mvc;
using Site.Models;
using Site.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Core.Services;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;

namespace Site.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipeSearchController : ControllerBase
{
    private readonly ISearcherResolver _searcherResolver;
    private readonly IApiPublishedContentCache _publishedContentCache;
    private readonly IRecipeRatingService _recipeRatingService;

    public RecipeSearchController(
        ISearcherResolver searcherResolver,
        IApiPublishedContentCache publishedContentCache,
        IRecipeRatingService recipeRatingService)
    {
        _searcherResolver = searcherResolver;
        _publishedContentCache = publishedContentCache;
        _recipeRatingService = recipeRatingService;
    }

    private static readonly (string Label, int From, int To)[] PreparationTimeRanges =
    [
        ("Less than 15 min", 0, 15 ),
        ("15 to 30 min", 15, 30 ),
        ("30 to 60 min", 30, 60 ),
        ("More than 60 min", 60, 60*24 ),
    ];

    [HttpGet]
    public async Task<IActionResult> Get(
        string? query,
        int skip = 0,
        int take = 10,
        [FromQuery] string[]? preparationTime = null,
        [FromQuery] string[]? cuisine = null,
        [FromQuery] string[]? mealType = null,
        string? sortBy = null,
        string? sortDirection = null,
        string provider = "examine")
    {
        var filters = new List<Filter>
        {
            // Filter to include only chilren of the "Example One" document
            new KeywordFilter(SearchConstants.FieldNames.ParentId, ["9622bf04-83a8-44e1-9535-bdcc3b6066eb"], false)
        };

        if (cuisine is { Length: > 0 })
        {
            filters.Add(new KeywordFilter("cuisine", cuisine, false));
        }

        if (mealType is { Length: > 0 })
        {
            filters.Add(new KeywordFilter("mealType", mealType, false));
        }
        
        if (preparationTime is { Length: > 0 })
        {
            var ranges = PreparationTimeRanges
                .Where(c => preparationTime.Contains(c.Label))
                .Select(c => new IntegerRangeFilterRange(c.From, c.To))
                .ToArray();

            filters.Add(new IntegerRangeFilter("preparationTime", ranges, false));
        }

        var facets = new List<Facet>
        {
            new KeywordFacet("cuisine"),
            new KeywordFacet("mealType"),
            new IntegerRangeFacet(
                "preparationTime",
                PreparationTimeRanges
                    .Select(c => new IntegerRangeFacetRange(c.Label, c.From, c.To))
                    .ToArray()
            )
        };
        
        var direction = sortDirection?.ToLowerInvariant() == "asc"
            ? Direction.Ascending
            : Direction.Descending;

        Sorter sorter = sortBy?.ToLowerInvariant() switch
        {
            "preparationTime" => new IntegerSorter("preparationTime", direction),
            "cuisine" => new KeywordSorter("cuisine", direction),
            "name" => new TextSorter(SearchConstants.FieldNames.Name, direction),
            "rating" => new DecimalSorter("rating", direction),
            _ => new ScoreSorter(direction)
        };

        var indexAlias = provider.ToLowerInvariant() switch
        {
            "algolia" => "CustomIndexAlgolia",
            "elasticsearch" => "CustomIndexElasticsearch",
            "typesense" => "CustomIndexTypesense",
            _ => SearchConstants.IndexAliases.PublishedContent
        };

        var searcher = _searcherResolver.GetRequiredSearcher(indexAlias);

        var result = await searcher.SearchAsync(
            indexAlias: indexAlias,
            query: query,
            filters: filters,
            facets: facets,
            sorters: [sorter],
            skip: skip,
            take: take);
        
        var publishedContent = await _publishedContentCache.GetByIdsAsync(result.Documents.Select(d => d.Id));

        return Ok(
            new ContentSearchResponseModel
            {
                Total = result.Total,
                Items = publishedContent.Select(c => new ContentSearchResultItemModel
                {
                    Id = c.Key,
                    Name = c.Name,
                    Url = c.Url(),
                    Teaser = c.Value<string>("teaser"),
                    Cuisine = c.Value<IEnumerable<string>>("cuisine"),
                    MealType = c.Value<IEnumerable<string>>("mealType"),
                    PreparationTime = c.Value<int?>("preparationTime"),
                    Rating = _recipeRatingService.Get(c.Key)
                }),
                Facets = result.Facets.Select(f => new FacetResultModel
                {
                    FieldName = f.FieldName,
                    Values = f.Values.Select(v => v switch
                    {
                        KeywordFacetValue kv => new FacetValueModel { Key = kv.Key, Count = kv.Count },
                        IntegerRangeFacetValue rv => new FacetValueModel { Key = rv.Key, Count = rv.Count },
                        _ => null
                    }).OfType<FacetValueModel>().Where(v => v.Count > 0)
                })
            }
        );
    }
}