# Adding Custom Fields to Content Indexes

When Umbraco indexes published content, you often need to add fields that are not standard Umbraco
properties — computed values, data from external services, denormalized related content, etc.

The new search API gives you two ways to do this. Both are demonstrated in this repo.

---

## The Two Approaches

| Approach | Interface / Hook | When it fires | Best for |
|----------|-----------------|---------------|----------|
| **`IContentIndexer`** | `IContentIndexer` | During field assembly, before notification | Synchronous, service-injected enrichment |
| **`ContentIndexingNotification`** | `INotificationAsyncHandler<ContentIndexingNotification>` | Just before the document is written | Async enrichment, cross-cutting concerns |

Both approaches receive the document's `Id` (a `Guid`) and can add or modify `IndexField` objects.

---

## Approach 1: `IContentIndexer`

This is the **preferred approach** for most cases. Implement `IContentIndexer`, which has a single method:

```csharp
public interface IContentIndexer
{
    Task<IEnumerable<IndexField>> GetIndexFieldsAsync(
        IContentBase content,
        string?[] cultures,
        bool published,
        CancellationToken cancellationToken);
}
```

You receive the full `IContentBase` object (the unpublished Umbraco content model), which gives you access
to `content.Key`, `content.ContentType.Alias`, all property values, etc.

### The Recipe Rating Example

The demo uses this to write the current star rating into every recipe document:

```csharp
// src/ContentIndexing/RecipeRatingContentIndexer.cs
public class RecipeRatingContentIndexer : IContentIndexer
{
    private readonly IRecipeRatingService _recipeRatingService;

    public RecipeRatingContentIndexer(IRecipeRatingService recipeRatingService)
        => _recipeRatingService = recipeRatingService;

    public Task<IEnumerable<IndexField>> GetIndexFieldsAsync(
        IContentBase content,
        string?[] cultures,
        bool published,
        CancellationToken cancellationToken)
    {
        if (content.ContentType.Alias is not "recipe")
        {
            // this only applies to recipes — return empty for everything else
            return Task.FromResult(Enumerable.Empty<IndexField>());
        }

        var rating = _recipeRatingService.Get(content.Key);

        return Task.FromResult<IEnumerable<IndexField>>([
            new IndexField(
                FieldName: "rating",
                Value: new IndexValue { Decimals = [Convert.ToDecimal(rating)] },
                Culture: null,   // null = culture-invariant
                Segment: null    // null = no segment
            )
        ]);
    }
}
```

### Registration

Register as **transient** in your Composer:

```csharp
builder.Services.AddTransient<IContentIndexer, RecipeRatingContentIndexer>();
```

You can register **multiple `IContentIndexer` implementations**. The pipeline calls all of them and merges
the resulting `IndexField` collections into one document. This lets you keep concerns separated across
multiple classes.

---

## Approach 2: `ContentIndexingNotification`

This is an event-driven alternative. The notification fires **after** all `IContentIndexer` implementations
have run, just before the document is written to the index. You can inspect and mutate `notification.Fields`.

> **Note:** The `RecipeRatingIndexingNotificationHandler` in this repo is explicitly marked as _"not used
> in this demo"_ — it exists as a reference implementation showing what the notification approach looks
> like. The demo uses `RecipeRatingContentIndexer` (approach 1) instead.

### The Related Recipe Name Example

Example 3 _does_ use the notification approach, to write the name of a related recipe into the index:

```csharp
// src/NotificationHandlers/RelatedRecipeIndexingNotificationHandler.cs
public class RelatedRecipeIndexingNotificationHandler
    : INotificationAsyncHandler<ContentIndexingNotification>
{
    private readonly IPublishedContentCache _publishedContentCache;

    public RelatedRecipeIndexingNotificationHandler(IPublishedContentCache publishedContentCache)
        => _publishedContentCache = publishedContentCache;

    public async Task HandleAsync(
        ContentIndexingNotification notification,
        CancellationToken cancellationToken)
    {
        // guard: only handle the PublishedContent index
        if (notification.IndexAlias is not SearchConstants.IndexAliases.PublishedContent)
            return;

        // guard: only handle recipes
        var content = await _publishedContentCache.GetByIdAsync(notification.Id);
        if (content?.ContentType.Alias != "recipe")
            return;

        // look up the related recipe property
        var relatedRecipe = content.Value<IPublishedContent>("relatedRecipe");
        if (relatedRecipe is null)
            return;

        // mutate notification.Fields — append the new field to the existing collection
        notification.Fields = notification.Fields
            .Union([
                new IndexField(
                    FieldName: "relatedRecipeName",
                    Value: new IndexValue { Texts = [relatedRecipe.Name] },
                    Culture: null,
                    Segment: null
                )
            ])
            .ToArray();
    }
}
```

### Registration

```csharp
builder.AddNotificationAsyncHandler<ContentIndexingNotification, RelatedRecipeIndexingNotificationHandler>();
```

### Key Difference from `IContentIndexer`

In the notification handler you receive `notification.Id` (a `Guid`) and `notification.Fields` — not the
full `IContentBase`. To access the published content model, you inject and use `IPublishedContentCache`
(as the example does with `GetByIdAsync`). This is async-friendly and suitable for loading related content.

---

## The `IndexField` Type

Both approaches produce `IndexField` objects. Here is what they contain:

```csharp
new IndexField(
    FieldName: "myField",       // the field name in the index
    Value: new IndexValue
    {
        // Provide ONE of these value types (or multiple for dual-purpose fields):
        Keywords = ["italian"],                              // exact-match keyword(s)
        Texts   = ["A long description for full-text search"], // full-text search
        Integers = [42],                                     // integer value(s)
        Decimals = [4.5m],                                   // decimal value(s)
        DateTimeOffsets = [DateTimeOffset.UtcNow],           // date/time value(s)
        Booleans = [true],                                   // boolean value(s)
    },
    Culture: null,     // null = culture-invariant; "en-US" = culture-specific
    Segment: null      // null = no segment
);
```

### Keywords vs Texts — a critical distinction

| Type | Analysed? | Suitable for | Example use |
|------|-----------|-------------|-------------|
| `Keywords` | No (stored as-is) | Exact-match filtering, faceting, sorting | cuisine = "Italian" |
| `Texts` | Yes (tokenised, stemmed) | Full-text search | recipe name, description |

A single field can carry **both** — the `genre` field in the people index does this, allowing both
full-text search ("jazz") and exact-match faceting ("Jazz"):

```csharp
// src/Services/PeopleIndexingService.cs
new IndexField(
    SiteConstants.FieldNames.Genre,
    new IndexValue
    {
        Keywords = [person.Genre],  // for faceting: exact "Jazz"
        Texts    = [person.Genre],  // for searching: "jaz" matches "Jazz" via stemming
    },
    Culture: null,
    Segment: null
)
```

---

## Choosing Between the Two Approaches

Use `IContentIndexer` when:
- You have a service dependency that provides the extra data (e.g., a rating service)
- You want strongly-typed, testable classes
- The enrichment applies to all indexes that include the content type

Use `ContentIndexingNotification` when:
- You need async access to the **published content model** (`IPublishedContent`) for related data
- You want to conditionally enrich only specific indexes (check `notification.IndexAlias`)
- You are adding cross-cutting concerns in a pipeline style

> Both approaches can coexist. The pipeline runs all `IContentIndexer` implementations first,
> then fires the notification for final adjustment.

---

## How the Alternative Notification Handler Works (Reference Only)

For completeness, here is the `RecipeRatingIndexingNotificationHandler` — the approach that is **not**
used in the demo but is kept as a reference implementation:

```csharp
// src/NotificationHandlers/RecipeRatingIndexingNotificationHandler.cs
// an alternative approach to indexing recipe ratings (not used in this demo)
public class RecipeRatingIndexingNotificationHandler
    : INotificationAsyncHandler<ContentIndexingNotification>
{
    public async Task HandleAsync(ContentIndexingNotification notification, CancellationToken ct)
    {
        // only handle the relevant indexes
        if (notification.IndexAlias is not
            (SearchConstants.IndexAliases.PublishedContent
             or SiteConstants.IndexAliases.CustomIndexElasticsearch))
            return;

        var content = await _publishedContentCache.GetByIdAsync(notification.Id);
        if (content?.ContentType.Alias != "recipe")
            return;

        var rating = _recipeRatingService.Get(notification.Id);

        notification.Fields = notification.Fields
            .Union([
                new IndexField("rating", new IndexValue { Decimals = [Convert.ToDecimal(rating)] }, null, null)
            ])
            .ToArray();
    }
}
```

Notice it guards on `notification.IndexAlias` to apply to both the Examine index _and_ the Elasticsearch
index — you can target multiple indexes from a single handler.

---

## Continue Reading

- [Custom Data Indexes →](05-custom-data-indexes.md)
- [Content Change Strategies →](06-content-change-strategies.md)
- [Examine-Specific Gotchas →](08-examine-gotchas.md)
