# Searching: Filters, Facets, Sorters

This is where everything comes together. The new search API exposes a clean, typed interface for
querying that works identically regardless of whether you are using Examine or Elasticsearch.

---

## Getting a Searcher

Use `ISearcherResolver` to get a searcher for a specific index:

```csharp
// inject ISearcherResolver in your controller/service
private readonly ISearcherResolver _searcherResolver;

// get a searcher for the target index
var searcher = _searcherResolver.GetRequiredSearcher(indexAlias);
```

`GetRequiredSearcher` throws if no searcher is registered for that alias. There is also
`TryGetSearcher` if you want a nullable return instead.

The searcher you get back is an `ISearcher` — the provider-neutral interface. If you know you are
always using Elasticsearch, you can inject `IElasticsearchSearcher` directly (as the
`PeopleSearchController` does), but using `ISearcherResolver` with a string alias is more flexible.

---

## The Search Call

```csharp
var result = await searcher.SearchAsync(
    indexAlias: indexAlias,       // which index to query
    query:      "pasta",          // full-text query (null = match all)
    filters:    filters,          // IEnumerable<Filter> — narrow results
    facets:     facets,           // IEnumerable<Facet> — aggregate counts
    sorters:    [sorter],         // IEnumerable<Sorter> — ordering
    skip:       0,                // pagination offset
    take:       10                // page size
);
```

The result is a `SearchResult` containing:
- `result.Total` — total matching document count (for pagination UI)
- `result.Documents` — the current page of `Document` objects (each has `.Id`)
- `result.Facets` — a collection of `FacetResult` objects

---

## Filters

Filters narrow the result set. Multiple filters are combined with AND logic (all must match).
Within a single filter, multiple values are combined with OR logic (any can match).

```csharp
var filters = new List<Filter>
{
    // OR filter: cuisine must be "Italian" or "Mexican"
    new KeywordFilter("cuisine", ["Italian", "Mexican"], false),

    // OR filter: mealType must be "Lunch" or "Dinner"
    new KeywordFilter("mealType", ["Lunch", "Dinner"], false),

    // Range filter: preparationTime 15–30 minutes OR 30–60 minutes
    new IntegerRangeFilter("preparationTime",
    [
        new IntegerRangeFilterRange(15, 30),
        new IntegerRangeFilterRange(30, 60),
    ], false),
};
```

### Filter Types

| Filter class | Field type | Example use |
|-------------|-----------|-------------|
| `KeywordFilter` | Keywords | cuisine, mealType, zodiac |
| `IntegerRangeFilter` | Integers | preparation time ranges |
| `DateTimeOffsetRangeFilter` | DateTimeOffsets | birthdate ranges (generations) |

There are also built-in constants for standard Umbraco fields:

```csharp
// filter to only include children of a specific parent document
new KeywordFilter(SearchConstants.FieldNames.ParentId, ["9622bf04-83a8-44e1-9535-bdcc3b6066eb"], false)
```

`SearchConstants.FieldNames` includes: `ParentId`, `Name`, `ContentTypeAlias`, and others — check
`Umbraco.Cms.Search.Core.Constants.FieldNames` for the full list.

### The `negate` parameter (third argument)

The `false` in `new KeywordFilter("cuisine", [...], false)` is the `negate` flag. When `true`, the
filter _excludes_ matching documents. When `false` (default), it _includes_ them.

---

## Facets

Facets return aggregated counts grouped by field value — the classic "filter sidebar" pattern.

```csharp
var facets = new List<Facet>
{
    // keyword facet: count documents per unique cuisine value
    new KeywordFacet("cuisine"),

    // keyword facet: count documents per unique mealType value
    new KeywordFacet("mealType"),

    // range facet: count documents per preparation time bucket
    new IntegerRangeFacet("preparationTime",
    [
        new IntegerRangeFacetRange("Less than 15 min",  0,  15),
        new IntegerRangeFacetRange("15 to 30 min",     15,  30),
        new IntegerRangeFacetRange("30 to 60 min",     30,  60),
        new IntegerRangeFacetRange("More than 60 min", 60, 60*24),
    ]),

    // date range facet: count people per generation bucket
    new DateTimeOffsetRangeFacet("birthdate",
    [
        new DateTimeOffsetRangeFacetRange(
            "Millennials",
            new DateTimeOffset(1981, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(1997, 1, 1, 0, 0, 0, TimeSpan.Zero)
        ),
        // ...more ranges
    ]),
};
```

### Facet Types

| Facet class | Field type | Use case |
|------------|-----------|----------|
| `KeywordFacet` | Keywords | Distinct value counts (cuisine, genre) |
| `IntegerRangeFacet` | Integers | Bucketed numeric counts (time ranges) |
| `DateTimeOffsetRangeFacet` | DateTimeOffsets | Bucketed date counts (generations) |

### Reading Facet Results

```csharp
// from RecipeSearchController.cs
private static IEnumerable<FacetResultModel> CreateFacetResultModels(IEnumerable<FacetResult> facets)
    => facets.Select(f => new FacetResultModel
    {
        FieldName = f.FieldName,
        Values = f.Values.Select(v => v switch
        {
            KeywordFacetValue kv       => new FacetValueModel { Key = kv.Key,  Count = kv.Count },
            IntegerRangeFacetValue rv  => new FacetValueModel { Key = rv.Key,  Count = rv.Count },
            _ => null
        })
        .OfType<FacetValueModel>()
        .Where(v => v.Count > 0)  // ← filter out buckets with no results
    });
```

The `v switch` pattern is necessary because facet values are polymorphic — use pattern matching to
handle each concrete type.

> **Filtering out zero-count values:** Note `.Where(v => v.Count > 0)`. With `ExpandFacetValues = true`
> (Examine-only setting), non-selected facet values are included even when the count is zero. Filtering
> them out here keeps the UI clean.

---

## Sorters

Sorters define the ordering of results. Pass exactly one sorter per search (multiple sorters may be
supported in future API versions — check the docs).

```csharp
// sort by relevance score (default when no query filter is active)
Sorter sorter = new ScoreSorter(Direction.Descending);

// sort by a text field (name, alphabetical)
sorter = new TextSorter(SearchConstants.FieldNames.Name, Direction.Ascending);

// sort by a keyword field
sorter = new KeywordSorter("cuisine", Direction.Ascending);

// sort by an integer field
sorter = new IntegerSorter("preparationTime", Direction.Ascending);

// sort by a decimal field
sorter = new DecimalSorter("rating", Direction.Descending);

// sort by a date/time field
sorter = new DateTimeOffsetSorter(SiteConstants.FieldNames.Birthdate, Direction.Ascending);
```

### Sorter Reference Table

| Sorter class | Field value type | Notes |
|-------------|-----------------|-------|
| `ScoreSorter` | N/A | Relevance score — best for keyword search results |
| `TextSorter` | Texts | Alphabetical on analysed text |
| `KeywordSorter` | Keywords | Alphabetical on exact keyword |
| `IntegerSorter` | Integers | Numeric ascending/descending |
| `DecimalSorter` | Decimals | Numeric ascending/descending |
| `DateTimeOffsetSorter` | DateTimeOffsets | Chronological |

> **Examine requirement:** For Examine, fields used for sorting must be declared in `FieldOptions` with
> `Sortable = true`. If you forget this, Examine silently falls back to score ordering. See
> [docs/08-examine-gotchas.md](08-examine-gotchas.md).

---

## The Two-Phase Result Pattern

The search returns only IDs. You then fetch the full content from the published content cache:

```csharp
// phase 1: search
var result = await searcher.SearchAsync(/* ... */);
var recipeIds = result.Documents.Select(d => d.Id);

// phase 2: hydrate from content cache
var publishedContent = await _publishedContentCache.GetByIdsAsync(recipeIds);

// combine: merge content data + search-specific data (e.g. rating)
return publishedContent.Select(c => new ContentSearchResultItemModel
{
    Id              = c.Key,
    Name            = c.Name,
    Url             = c.Url(),
    Teaser          = c.Value<string>("teaser"),
    Cuisine         = c.Value<IEnumerable<string>>("cuisine"),
    PreparationTime = c.Value<int?>("preparationTime"),
    Rating          = _recipeRatingService.Get(c.Key)  // from external service, not the index
});
```

This pattern keeps the search index lean. You do not need to store every content property in the index —
just the fields you need for filtering, faceting, and sorting. Rich content (teaser text, URLs, etc.) is
fetched from Umbraco's optimised published content cache after the fact.

> **Important:** `_publishedContentCache.GetByIdsAsync(recipeIds)` returns results in an unspecified order.
> The search result order is **not** preserved. For the people search, the demo explicitly re-sorts the
> hydrated results to match the original search ranking:
> ```csharp
> var resultIds = result.Documents.Select(d => d.Id).ToArray();
> var people = await _peopleService.GetByIdsAsync(resultIds);
> var ordered = people.OrderBy(person => resultIds.IndexOf(person.Id)); // preserve rank order
> ```

---

## Full Controller Example

Here is the complete recipe search pattern condensed:

```csharp
// determine which index to use (provider switch)
var indexAlias = provider.ToLowerInvariant() is "elasticsearch"
    ? SiteConstants.IndexAliases.CustomIndexElasticsearch
    : SearchConstants.IndexAliases.PublishedContent;

var searcher  = _searcherResolver.GetRequiredSearcher(indexAlias);
var filters   = GetFilters(preparationTime, cuisine, mealType);
var facets    = GetFacets();
var sorter    = GetSorter(sortBy, sortDirection);

var result = await searcher.SearchAsync(
    indexAlias: indexAlias,
    query:      query,
    filters:    filters,
    facets:     facets,
    sorters:    [sorter],
    skip:       skip,
    take:       take);

return Ok(new ContentSearchResponseModel
{
    Total  = result.Total,
    Items  = await CreateSearchResultItemModels(result.Documents),
    Facets = CreateFacetResultModels(result.Facets)
});
```

---

## Continue Reading

- [Examine-Specific Gotchas →](08-examine-gotchas.md)
- [Real-time Index Updates →](09-real-time-updates.md)
