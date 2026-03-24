# Documentation Plan: Umbraco Search (New API)

This repo is the demo code from the talk _"Umbraco Search - a developer's perspective"_ at Umbraco Spark 26.
It demonstrates the **new `Umbraco.Cms.Search` API** introduced in Umbraco 17, which replaces direct Examine usage with
a clean, provider-agnostic abstraction layer.

All documentation lives in the `/docs` folder. It is split into phases to be written incrementally.

---

## Document Index

| Phase | File | Status | Topic |
|-------|------|--------|-------|
| 1 | [docs/01-introduction.md](docs/01-introduction.md) | ✅ Done | What is the new search API, and how does it compare to classic Examine? |
| 2 | [docs/02-architecture.md](docs/02-architecture.md) | ✅ Done | Architecture diagrams — how providers, indexes, indexers, and searchers fit together |
| 3 | [docs/03-setup-and-registration.md](docs/03-setup-and-registration.md) | ⬜ Pending | Installing packages, registering providers, Elasticsearch config |
| 4 | [docs/04-indexing-content.md](docs/04-indexing-content.md) | ⬜ Pending | Adding custom fields to content indexes via `IContentIndexer` and `ContentIndexingNotification` |
| 5 | [docs/05-custom-data-indexes.md](docs/05-custom-data-indexes.md) | ⬜ Pending | Indexing non-Umbraco data (the People example) |
| 6 | [docs/06-content-change-strategies.md](docs/06-content-change-strategies.md) | ⬜ Pending | Controlling what gets re-indexed when content changes (`IContentChangeStrategy`) |
| 7 | [docs/07-searching.md](docs/07-searching.md) | ⬜ Pending | Querying: filters, facets, sorters, pagination |
| 8 | [docs/08-examine-gotchas.md](docs/08-examine-gotchas.md) | ⬜ Pending | Examine-specific requirements and performance warnings |
| 9 | [docs/09-real-time-updates.md](docs/09-real-time-updates.md) | ⬜ Pending | Triggering index refreshes from code (`IDistributedContentIndexRefresher`) |

---

## Key Themes Covered

- **Provider abstraction:** swap Examine for Elasticsearch (or any future provider) with minimal code change
- **Typed search API:** no more raw Lucene query strings — use strongly-typed `Filter`, `Facet`, and `Sorter` objects
- **IContentIndexer:** the clean way to add custom fields during the content indexing pipeline
- **ContentIndexingNotification:** the event-driven alternative approach to enriching index documents
- **IContentChangeStrategy:** intercept and extend what gets re-indexed when content changes
- **ExpandFacetValues warning:** a performance penalty in Examine that you need to know about
- **IDistributedContentIndexRefresher:** how to trigger a reindex from code, and why you should batch this
- **Custom (non-Umbraco) indexes:** full worked example of indexing arbitrary data from a JSON file
- **Index rebuild on startup:** the current manual approach, and the note that Umbraco Search may improve this

---

## Audience

Written for developers who are experienced with **classic Umbraco Examine** (direct Lucene) and are learning the
new `Umbraco.Cms.Search` abstraction layer introduced in Umbraco 17.
