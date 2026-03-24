# Architecture Overview

This document shows how all the pieces of the new Umbraco Search API fit together, using Mermaid diagrams.

---

## The Big Picture: Layered Architecture

The new search system is built as a layered abstraction. Your application code only ever talks to the
**Core API** — it never touches Lucene or Elasticsearch directly.

```mermaid
graph TD
    subgraph "Your Application Code"
        A[Controllers / Services]
    end

    subgraph "Umbraco.Cms.Search.Core"
        B[ISearcherResolver]
        C[IContentIndexer]
        D[ContentIndexingNotification]
        E[IContentChangeStrategy]
        F[IDistributedContentIndexRefresher]
    end

    subgraph "Search Providers"
        G[Examine Provider\nUmbraco.Cms.Search.Provider.Examine]
        H[Elasticsearch Provider\nKjac.SearchProvider.Elasticsearch]
    end

    subgraph "Search Backends"
        I[Lucene Index\non disk]
        J[Elasticsearch\nHTTP cluster]
    end

    A --> B
    A --> C
    A --> D
    A --> E
    A --> F

    B --> G
    B --> H
    C --> G
    C --> H
    D --> G
    D --> H

    G --> I
    H --> J
```

The key insight is that **swapping providers** (e.g. from Examine to Elasticsearch) is a one-line change
in your DI registration. Your controllers, indexers, and notification handlers stay identical.

---

## Indexes: What Exists in This Demo

```mermaid
graph LR
    subgraph "Examine Provider (Lucene)"
        PCI[PublishedContent index\nSearchConstants.IndexAliases.PublishedContent]
    end

    subgraph "Elasticsearch Provider"
        CEI[CustomIndexElasticsearch\nSiteConstants.IndexAliases.CustomIndexElasticsearch]
        CPI[CustomPersonIndex\nSiteConstants.IndexAliases.CustomPersonIndex]
    end

    PCI -->|searched by| RC1[RecipeSearchController\nprovider=examine]
    CEI -->|searched by| RC2[RecipeSearchController\nprovider=elasticsearch]
    CPI -->|searched by| PC[PeopleSearchController]
```

- **PublishedContent** — the default Umbraco content index, managed automatically by Umbraco. Uses Examine.
- **CustomIndexElasticsearch** — a second index for the same published content, but powered by Elasticsearch.
  Demonstrates that you can have multiple indexes for the same data with different providers.
- **CustomPersonIndex** — a completely custom index for non-Umbraco data (people from a JSON file). Elasticsearch only.

---

## Content Indexing Pipeline

When Umbraco content is published, a pipeline runs to update the search index. Here is how it flows:

```mermaid
sequenceDiagram
    participant U as Umbraco CMS
    participant CS as IContentChangeStrategy
    participant CI as IContentIndexingService
    participant IX as IContentIndexer(s)
    participant N as ContentIndexingNotification
    participant P as Search Provider<br/>(Examine / ES)

    U->>CS: Content published event
    CS->>CS: Decide which documents to index<br/>(may add related docs)
    CS->>CI: Submit list of ContentChange objects
    CI->>IX: GetIndexFieldsAsync(content, cultures, published)
    IX-->>CI: Returns extra IndexField objects
    CI->>N: Fire ContentIndexingNotification<br/>(handlers can mutate Fields)
    N-->>CI: Fields collection (possibly enriched)
    CI->>P: Write document to index
    P-->>U: Done
```

Your two extension points in this pipeline are:

1. **`IContentIndexer`** — called synchronously during field assembly. Returns additional `IndexField` objects.
   Used in this demo to add the recipe `rating` field.

2. **`ContentIndexingNotification`** — fired just before the document is written. Handlers can add to
   `notification.Fields`. Used in this demo to add the `relatedRecipeName` field.

> See [docs/04-indexing-content.md](04-indexing-content.md) for full details on both approaches.

---

## Custom Data Indexing Pipeline (Non-Umbraco)

For data that has no connection to Umbraco content (like the people JSON file), you manage the full
lifecycle yourself:

```mermaid
sequenceDiagram
    participant S as Application Startup
    participant PIS as PeopleIndexingService
    participant PS as PeopleService
    participant EI as IElasticsearchIndexer
    participant ES as Elasticsearch

    S->>PIS: RebuildIndexAsync()
    PIS->>PS: GetAllAsync()
    PS-->>PIS: List<Person>
    PIS->>EI: ResetAsync(indexAlias)
    EI->>ES: Delete existing index
    loop For each Person
        PIS->>EI: AddOrUpdateAsync(alias, id, objectType, variations, fields, null)
        EI->>ES: Upsert document
    end
```

> See [docs/05-custom-data-indexes.md](05-custom-data-indexes.md) for the full example.

---

## Search Request Flow

When a search request comes in from a client:

```mermaid
sequenceDiagram
    participant C as HTTP Client
    participant CTL as RecipeSearchController
    participant SR as ISearcherResolver
    participant S as ISearcher
    participant P as Search Provider
    participant PC as IApiPublishedContentCache

    C->>CTL: GET /api/recipesearch?query=pasta&cuisine=Italian
    CTL->>CTL: Build filters, facets, sorters
    CTL->>SR: GetRequiredSearcher(indexAlias)
    SR-->>CTL: ISearcher instance
    CTL->>S: SearchAsync(alias, query, filters, facets, sorters, skip, take)
    S->>P: Execute provider-specific query
    P-->>S: SearchResult (Documents + Facets + Total)
    S-->>CTL: SearchResult
    CTL->>PC: GetByIdsAsync(documentIds)
    PC-->>CTL: IPublishedContent[]
    CTL-->>C: ContentSearchResponseModel (JSON)
```

The two-phase pattern here is important:
1. **Search** returns only IDs and score data (fast)
2. **Content cache lookup** hydrates those IDs into rich content objects

This keeps the search index lean (no need to store all content fields) while still giving you access to
the full Umbraco content model for your response.

---

## Index Refresh Flow (Ratings Example)

When a recipe is rated, the index needs to be updated without waiting for the next publish:

```mermaid
sequenceDiagram
    participant C as HTTP Client
    participant RRC as RecipeRatingController
    participant RS as IRecipeRatingService
    participant DCIR as IDistributedContentIndexRefresher
    participant CIS as IContentIndexingService
    participant IX as IContentIndexer(s)
    participant P as Search Provider

    C->>RRC: POST /api/reciperating/{id}?rating=4.5
    RRC->>RS: Set(id, rating)
    RRC->>DCIR: RefreshContent([content], ContentState.Published)
    DCIR->>CIS: Triggers re-index of that document
    CIS->>IX: GetIndexFieldsAsync (RecipeRatingContentIndexer runs)
    IX-->>CIS: IndexField "rating" with new value
    CIS->>P: Update document in index
    P-->>C: 200 OK
```

> **Warning (from the code comment):** This triggers a reindex per rating. In production you should
> batch multiple ratings within a time window into a single indexing operation using a background thread
> with a timed delay. See [docs/09-real-time-updates.md](09-real-time-updates.md).

---

## Related Content Re-indexing Flow (Example 3)

When recipe A is published, any recipe B that _links to_ recipe A should also be re-indexed, so that
`relatedRecipeName` in recipe B's index document stays current:

```mermaid
sequenceDiagram
    participant U as Umbraco CMS
    participant CS as RelatedRecipePublishedContentChangeStrategy
    participant TRS as ITrackedReferencesService
    participant PCCS as IPublishedContentChangeStrategy (core)
    participant NIH as RelatedRecipeIndexingNotificationHandler
    participant P as Search Provider

    U->>CS: HandleAsync(indexInfos, [Change: RecipeA Published])
    CS->>TRS: GetPagedRelationsForItemAsync(RecipeA.Id, 0, 1000)
    TRS-->>CS: [RecipeB, RecipeC, ...]
    CS->>PCCS: HandleAsync(indexInfos, [RecipeA Published, RecipeB Refresh, RecipeC Refresh])
    PCCS->>NIH: Fire ContentIndexingNotification for RecipeB
    NIH->>NIH: Look up relatedRecipe property on RecipeB
    NIH->>NIH: Add IndexField "relatedRecipeName" = RecipeA.Name
    PCCS->>P: Write updated document for RecipeB (and RecipeC, etc.)
```

> **Note (from the code comment):** The relation fetch is hardcoded to a maximum of 1,000 related
> documents. See [docs/06-content-change-strategies.md](06-content-change-strategies.md) for discussion.

---

## Component Dependency Map

```mermaid
graph TD
    SC[SiteComposer] --> E1[ConfigureExampleOne]
    SC --> E2[ConfigureExampleTwo]
    SC --> E3[ConfigureExampleThree]

    E1 --> ASC[AddSearchCore]
    E1 --> AES[AddElasticsearchSearchProvider]
    E1 --> AEX[AddExamineSearchProvider]
    E1 --> CEI[Register CustomIndexElasticsearch]
    E1 --> RRS[RecipeRatingService singleton]
    E1 --> RRCI[RecipeRatingContentIndexer transient]

    E2 --> PS[PeopleService singleton]
    E2 --> PIS[PeopleIndexingService singleton]

    E3 --> RRNIH[RelatedRecipeIndexingNotificationHandler]
    E3 --> RRPCS[RelatedRecipePublishedContentChangeStrategy]
    E3 --> RR_PCI[Re-register PublishedContent index\nwith custom change strategy]

    RDCH[ResetDemoComposerNotificationHandler] --> IDS[IIndexDocumentService]
    RDCH --> CIS2[IContentIndexingService]
    RDCH --> PIS
```

---

## Continue Reading

- [Setup and Registration →](03-setup-and-registration.md)
- [Adding Custom Index Fields →](04-indexing-content.md)
