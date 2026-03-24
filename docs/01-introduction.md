# Introduction: The New Umbraco Search API

## What is This?

Starting with Umbraco 17, Umbraco ships a new package called **`Umbraco.Cms.Search`** — a provider-agnostic
search abstraction layer that sits on top of whatever search backend you choose (Lucene via Examine,
Elasticsearch, or theoretically anything else). This demo repo is the companion code from the talk
_"Umbraco Search - a developer's perspective"_ at Umbraco Spark 26.

The NuGet packages in play are:

| Package | Version | Purpose |
|---------|---------|---------|
| `Umbraco.Cms.Search.Core` | 1.0.0-beta.2 | Core abstractions and interfaces |
| `Umbraco.Cms.Search.Provider.Examine` | 1.0.0-beta.2 | Lucene/Examine provider (built-in) |
| `Umbraco.Cms.Search.BackOffice` | 1.0.0-beta.2 | Back-office UI integration |
| `Kjac.SearchProvider.Elasticsearch` | 1.0.0-alpha.5 | Community Elasticsearch provider |

> **Note:** These packages are currently in beta/alpha. The API surface may change. Watch the
> official Umbraco documentation for updates.

---

## If You Know Classic Examine, Start Here

Coming from classic Examine in Umbraco 10–16, here is the mental model shift you need to make:

### Classic Examine (what you know)

In classic Umbraco you worked directly with Examine and Lucene:

```csharp
// Registering a custom index - Examine style
services.AddExamineLuceneIndex<UmbracoExamineIndex, ConfigurationEqualityComparer>(
    "MyIndex",
    fieldDefinitions: new FieldDefinitionCollection(
        new FieldDefinition("myField", FieldDefinitionTypes.FullText)
    )
);

// Adding custom fields during indexing
public class MyIndexPopulator : IIndexPopulator
{
    public void Populate(params IIndex[] indexes) { /* ... */ }
}

// Or via the TransformingIndexValues event
examineIndex.TransformingIndexValues += (sender, e) =>
{
    e.ValueSet.TryAdd("myCustomField", new List<object> { "someValue" });
};

// Querying - direct Lucene style
if (_examineManager.TryGetSearcher("MyIndex", out var searcher))
{
    var results = searcher.CreateQuery()
        .Field("nodeName", "recipe")
        .And()
        .IntegerRange("preparationTime", 0, 30)
        .Execute();
}
```

This was powerful but had drawbacks:
- Tightly coupled to Lucene internals
- Faceting required custom implementation or workarounds
- Sorting needed Lucene-specific field definitions
- No provider abstraction — changing backend meant rewriting search code
- Raw query strings were error-prone

### New Umbraco Search (what this demo uses)

The new API wraps all of that behind clean abstractions:

```csharp
// Registering — in your Composer
builder.AddSearchCore()
       .AddExamineSearchProvider()   // or .AddElasticsearchSearchProvider()

// Adding custom fields — implement IContentIndexer
public class RecipeRatingContentIndexer : IContentIndexer
{
    public Task<IEnumerable<IndexField>> GetIndexFieldsAsync(
        IContentBase content, string?[] cultures, bool published, CancellationToken ct)
    {
        var rating = _ratingService.Get(content.Key);
        return Task.FromResult<IEnumerable<IndexField>>([
            new IndexField("rating", new IndexValue { Decimals = [Convert.ToDecimal(rating)] }, null, null)
        ]);
    }
}

// Querying — typed, provider-agnostic
var searcher = _searcherResolver.GetRequiredSearcher(indexAlias);
var result = await searcher.SearchAsync(
    indexAlias: indexAlias,
    query: "pasta",
    filters: [new KeywordFilter("cuisine", ["Italian"], false)],
    facets:  [new KeywordFacet("cuisine"), new IntegerRangeFacet("preparationTime", ranges)],
    sorters: [new DecimalSorter("rating", Direction.Descending)],
    skip: 0,
    take: 10);
```

### Side-by-side Comparison

| Concern | Classic Examine | New Umbraco Search |
|---------|----------------|-------------------|
| Backend | Lucene only | Lucene (Examine) or Elasticsearch (or custom) |
| Index registration | `AddExamineLuceneIndex<>()` | `AddSearchCore()` + provider extension |
| Custom fields | `TransformingIndexValues` event | `IContentIndexer` or `ContentIndexingNotification` |
| Querying | `IExamineManager.TryGetSearcher()` + raw query builder | `ISearcherResolver` + typed `Filter`/`Facet`/`Sorter` objects |
| Faceting | Manual / third-party | Built-in `KeywordFacet`, `IntegerRangeFacet`, `DateTimeOffsetRangeFacet` |
| Sorting | Lucene `SortField` | Built-in `TextSorter`, `DecimalSorter`, `IntegerSorter`, etc. |
| Filtering | Lucene query syntax | Typed `KeywordFilter`, `IntegerRangeFilter`, `DateTimeOffsetRangeFilter` |
| Provider swap | Full rewrite | Change one line in DI registration |
| Non-Umbraco data | Awkward | First-class via `IElasticsearchIndexer` / direct indexer |

---

## The Three Examples in This Demo

This repo contains three distinct examples that build on each other:

### Example 1 — Recipe Search (Examine and Elasticsearch)
`/src/Controllers/RecipeSearchController.cs` searches Umbraco published content (recipes).
A `?provider=` query parameter lets you switch between Examine (Lucene) and Elasticsearch at runtime,
demonstrating the provider abstraction. Recipes also have a dynamic star rating that is written into
the index via `IContentIndexer`.

### Example 2 — People Search (Custom Elasticsearch Index)
`/src/Controllers/PeopleSearchController.cs` searches a fully custom Elasticsearch index populated
from a local JSON file (`people.json` with ~5,000 records). This demonstrates indexing arbitrary data
that has no connection to Umbraco content.

### Example 3 — Related Content Re-indexing
Wired up transparently via `/src/DependencyInjection/UmbracoBuilderExtensions.Example3.cs`. When a
recipe is published, the search system automatically re-indexes any recipes that have a _related recipe_
link pointing to it — so the related recipe's name is always fresh in the index. This uses a custom
`IContentChangeStrategy`.

---

## Key Files at a Glance

```
src/
├── DependencyInjection/
│   ├── SiteComposer.cs                          ← single entry point, wires all three examples
│   ├── UmbracoBuilderExtensions.Example1.cs     ← Examine + Elasticsearch setup, field config
│   ├── UmbracoBuilderExtensions.Example2.cs     ← custom people index setup
│   └── UmbracoBuilderExtensions.Example3.cs     ← related-content change strategy setup
├── Controllers/
│   ├── RecipeSearchController.cs                ← Example 1: search recipes
│   ├── PeopleSearchController.cs                ← Example 2: search people
│   └── RecipeRatingController.cs                ← triggers index refresh when a recipe is rated
├── ContentIndexing/
│   ├── RecipeRatingContentIndexer.cs            ← adds "rating" field via IContentIndexer
│   └── RelatedRecipePublishedContentChangeStrategy.cs ← custom change strategy (Example 3)
├── NotificationHandlers/
│   ├── RelatedRecipeIndexingNotificationHandler.cs  ← adds "relatedRecipeName" field to index
│   ├── RecipeRatingIndexingNotificationHandler.cs   ← alternative to IContentIndexer (not used)
│   └── ResetDemoComposerNotificationHandler.cs      ← rebuilds indexes on startup
├── Services/
│   ├── PeopleIndexingService.cs                 ← manually builds/rebuilds the people index
│   └── RecipeRatingService.cs                   ← in-memory rating store
└── SiteConstants.cs                             ← field names and index alias constants
```

---

## Continue Reading

- [Architecture Overview →](02-architecture.md)
- [Setup and Registration →](03-setup-and-registration.md)
- [Adding Custom Index Fields →](04-indexing-content.md)
- [Custom Data Indexes →](05-custom-data-indexes.md)
- [Content Change Strategies →](06-content-change-strategies.md)
- [Searching: Filters, Facets, Sorters →](07-searching.md)
- [Examine-Specific Gotchas →](08-examine-gotchas.md)
- [Real-time Index Updates →](09-real-time-updates.md)
