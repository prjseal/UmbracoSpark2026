// TODO: clean up here
// TODO: reconfigure the default members index to use Elasticsearch
// using Kjac.SearchProvider.Algolia.Services;
// using Kjac.SearchProvider.Elasticsearch.Services;
// using Kjac.SearchProvider.Typesense.Services;
// using Umbraco.Cms.Core.Models;
// using Umbraco.Cms.Search.Core.Configuration;
// using Umbraco.Cms.Search.Core.Services;
// using Umbraco.Cms.Search.Core.Services.ContentIndexing;
//
// namespace Site.DependencyInjection;
//
// public static partial class UmbracoBuilderExtensions
// {
//     public static IUmbracoBuilder RegisterCustomIndexes(this IUmbracoBuilder builder)
//     {
//         // register the custom index for Algolia
//         builder.RegisterCustomIndex<IAlgoliaIndexer, IAlgoliaSearcher>(Constants.CustomIndexes.Algolia);
//
//         // register the custom index for Elasticsearch
//         builder.RegisterCustomIndex<IElasticsearchIndexer, IElasticsearchSearcher>(Constants.CustomIndexes.Elasticsearch);
//
//         // register the custom index for Typesense
//         builder.RegisterCustomIndex<ITypesenseIndexer, ITypesenseSearcher>(Constants.CustomIndexes.Typesense);
//
//         return builder;
//     }
//
//     private static void RegisterCustomIndex<TIndexer, TSearcher>(this IUmbracoBuilder builder, string indexAlias)
//         where TIndexer : class, IIndexer
//         where TSearcher : class, ISearcher
//         => builder
//             .Services
//             .Configure<IndexOptions>(options => options
//                 .RegisterIndex<TIndexer, TSearcher, IPublishedContentChangeStrategy>(
//                     indexAlias,
//                     UmbracoObjectTypes.Document
//                 )
//             );
// }
//
//
//
//
//
//
//
//
//
//
//
