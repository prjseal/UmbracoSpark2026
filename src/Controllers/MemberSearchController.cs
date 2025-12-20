using Microsoft.AspNetCore.Mvc;
using Site.Models;
using Site.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Core.Services;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;

namespace Site.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MemberSearchController : ControllerBase
{
    private readonly ISearcherResolver _searcherResolver;
    private readonly IMemberService _memberService;
    private readonly IMemberToPersonService _memberToPersonService;

    public MemberSearchController(ISearcherResolver searcherResolver, IMemberService memberService, IMemberToPersonService memberToPersonService)
    {
        _searcherResolver = searcherResolver;
        _memberService = memberService;
        _memberToPersonService = memberToPersonService;
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
        [FromQuery] string[]? personality = null,
        [FromQuery] string[]? generation = null,
        string? sortBy = null,
        string? sortDirection = null,
        string provider = "default")
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

        if (personality is { Length: > 0 })
        {
            filters.Add(new KeywordFilter(SiteConstants.FieldNames.Personality, personality, false));
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
            new KeywordFacet(SiteConstants.FieldNames.Personality),
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

        var indexAlias = provider == "custom"
            ? SiteConstants.IndexAliases.CustomMemberIndex
            : SearchConstants.IndexAliases.DraftMembers;
        var searcher = _searcherResolver.GetRequiredSearcher(indexAlias);

        var result = await searcher.SearchAsync(
            indexAlias: indexAlias,
            query: query,
            filters: filters,
            facets: facets,
            sorters: [sorter],
            skip: skip,
            take: take);
        
        // TODO: is there a better/faster way to get members? looks like GetByKeysAsync() is explicitly uncached

        var memberSearchResultItemModels = new  List<MemberSearchResultItemModel>();
        foreach (var id in result.Documents.Select(d => d.Id))
        {
            var member = _memberService.GetById(id);
            if (member is null)
            {
                continue;
            }

            var person = await _memberToPersonService.GetPersonForMemberAsync(member);
            if (person is null)
            {
                continue;
            }

            var generationLabel = GenerationRanges.Single(r => person.Birthdate >= r.From && person.Birthdate < r.To).Label;
            
            memberSearchResultItemModels.Add(
                new ()
                {
                    Birthdate = person.Birthdate,
                    Name = person.Name,
                    Email = person.Email,
                    Genre = person.Genre,
                    Zodiac = person.Zodiac,
                    Generation = generationLabel,
                    Personality = member.GetValue<string>(SiteConstants.FieldNames.Personality) ?? string.Empty
                }
            );
        }

        return Ok(
            new MemberSearchResponseModel
            {
                Total = result.Total,
                Items = memberSearchResultItemModels,
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