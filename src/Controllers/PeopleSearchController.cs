using Kjac.SearchProvider.Elasticsearch.Services;
using Microsoft.AspNetCore.Mvc;
using Site.Models;
using Site.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;

namespace Site.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeopleSearchController : ControllerBase
{
    private readonly IElasticsearchSearcher _searcher;
    private readonly IPeopleService _peopleService;

    public PeopleSearchController(IElasticsearchSearcher searcher, IPeopleService peopleService)
    {
        _searcher = searcher;
        _peopleService = peopleService;
    }

    private static readonly (string Label, DateTimeOffset From, DateTimeOffset To)[] GenerationRanges =
    [
        (
            "Silent Generation",
            new DateTimeOffset(1928, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(1946, 1, 1, 0, 0, 0, TimeSpan.Zero)
        ),
        (
            "Boomers",
            new DateTimeOffset(1946, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(1965, 1, 1, 0, 0, 0, TimeSpan.Zero)
        ),
        (
            "Generation X",
            new DateTimeOffset(1965, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(1981, 1, 1, 0, 0, 0, TimeSpan.Zero)
        ),
        (
            "Millennials",
            new DateTimeOffset(1981, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(1997, 1, 1, 0, 0, 0, TimeSpan.Zero)
        ),
        (
            "Zoomers",
            new DateTimeOffset(1997, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2012, 1, 1, 0, 0, 0, TimeSpan.Zero)
        ),
        (
            "Generation Alpha",
            new DateTimeOffset(2012, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
        ),
    ];

    [HttpGet]
    public async Task<IActionResult> Get(
        string? query,
        int skip = 0,
        int take = 10,
        [FromQuery] string[]? zodiac = null,
        [FromQuery] string[]? genre = null,
        [FromQuery] string[]? generation = null,
        string? sortBy = null,
        string? sortDirection = null)
    {
        var filters = new List<Filter>();

        if (zodiac is { Length: > 0 })
        {
            filters.Add(new KeywordFilter(SiteConstants.FieldNames.Zodiac, zodiac, false));
        }

        if (genre is { Length: > 0 })
        {
            filters.Add(new KeywordFilter(SiteConstants.FieldNames.Genre, genre, false));
        }
        
        if (generation is { Length: > 0 })
        {
            var ranges = GenerationRanges
                .Where(c => generation.Contains(c.Label))
                .Select(c => new DateTimeOffsetRangeFilterRange(c.From, c.To))
                .ToArray();

            filters.Add(new DateTimeOffsetRangeFilter(SiteConstants.FieldNames.Birthdate, ranges, false));
        }

        var facets = new List<Facet>
        {
            new KeywordFacet(SiteConstants.FieldNames.Zodiac),
            new KeywordFacet(SiteConstants.FieldNames.Genre),
            new DateTimeOffsetRangeFacet(
                SiteConstants.FieldNames.Birthdate,
                GenerationRanges
                    .Select(c => new DateTimeOffsetRangeFacetRange(c.Label, c.From, c.To))
                    .ToArray()
            )
        };
        
        var direction = sortDirection?.ToLowerInvariant() == "asc"
            ? Direction.Ascending
            : Direction.Descending;

        Sorter sorter = sortBy?.ToLowerInvariant() switch
        {
            "birthdate" => new DateTimeOffsetSorter(SiteConstants.FieldNames.Birthdate, direction),
            "name" => new TextSorter(SiteConstants.FieldNames.Name, direction),
            _ => new ScoreSorter(direction)
        };

        var result = await _searcher.SearchAsync(
            indexAlias: SiteConstants.IndexAliases.CustomPeopleIndex,
            query: query,
            filters: filters,
            facets: facets,
            sorters: [sorter],
            skip: skip,
            take: take);

        var people = await _peopleService.GetByIdsAsync(result.Documents.Select(d => d.Id).ToArray());
        // TODO: sort the people collection by the order in the search results

        var personSearchResultItemModels = people
            .Select(person =>
            {
                var generationLabel = GenerationRanges.Single(r => person.Birthdate >= r.From && person.Birthdate < r.To).Label;
                return new PersonSearchResultItemModel
                {
                    Birthdate = person.Birthdate,
                    Name = person.Name,
                    Email = person.Email,
                    Genre = person.Genre,
                    Zodiac = person.Zodiac,
                    Generation = generationLabel
                };
            });
    
        return Ok(
            new PersonSearchResponseModel
            {
                Total = result.Total,
                Items = personSearchResultItemModels,
                Facets = result.Facets.Select(f => new FacetResultModel
                {
                    FieldName = f.FieldName is SiteConstants.FieldNames.Birthdate ? "generation" : f.FieldName,
                    Values = f.Values
                        .Select(v => v switch
                        {
                            KeywordFacetValue kv => new FacetValueModel { Key = kv.Key, Count = kv.Count },
                            DateTimeOffsetRangeFacetValue rv => new FacetValueModel { Key = rv.Key, Count = rv.Count },
                            _ => null
                        })
                        .WhereNotNull()
                        .Where(v => v.Count > 0)
                })
            }
        );
    }
}