// TODO: clean up here
// TODO: reconfigure the default members index to use Elasticsearch

using Examine;
using Examine.Lucene.Providers;
using Site.ContentIndexing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Services;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;
using IndexOptions = Umbraco.Cms.Search.Core.Configuration.IndexOptions;

namespace Site.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder RegisterIndexes(this IUmbracoBuilder builder)
    {
        // // register a custom document index for Algolia
        // builder.RegisterDocumentIndex<IAlgoliaIndexer, IAlgoliaSearcher>(SiteConstants.IndexAliases.Algolia);
        //
        // // register a custom document index for Elasticsearch
        // builder.RegisterDocumentIndex<IElasticsearchIndexer, IElasticsearchSearcher>(SiteConstants.IndexAliases.Elasticsearch);
        //
        // // register a custom document index for Typesense
        // builder.RegisterDocumentIndex<ITypesenseIndexer, ITypesenseSearcher>(SiteConstants.IndexAliases.Typesense);

        // TODO: use Elasticsearch here
        // re-register the default member index to use Elasticsearch
        builder.RegisterIndex<IExamineIndexer, IExamineSearcher, IDraftContentChangeStrategy>
        (
            SearchConstants.IndexAliases.DraftMembers,
            UmbracoObjectTypes.Member
        );

        // TODO: use Elasticsearch here
        // register a custom member index using the custom member content change strategy
        builder.RegisterIndex<IExamineIndexer, IExamineSearcher, IMemberContentChangeStrategy>
        (
            SiteConstants.IndexAliases.CustomMemberIndex,
            UmbracoObjectTypes.Member
        );
        // TODO: remove this once Elasticsearch is used
        builder.Services.AddExamineLuceneIndex<LuceneIndex, ConfigurationEnabledDirectoryFactory>(SiteConstants.IndexAliases.CustomMemberIndex, _ => { });

        // TODO: remove this once Elasticsearch is used
        builder.Services.AddExamineLuceneIndex<LuceneIndex, ConfigurationEnabledDirectoryFactory>(SiteConstants.IndexAliases.CustomPeopleIndex, _ => { });

        return builder;
    }

    private static void RegisterDocumentIndex<TIndexer, TSearcher>(this IUmbracoBuilder builder, string indexAlias)
        where TIndexer : class, IIndexer
        where TSearcher : class, ISearcher
        => builder.RegisterIndex<TIndexer, TSearcher, IPublishedContentChangeStrategy>(indexAlias, UmbracoObjectTypes.Document);

    private static void RegisterIndex<TIndexer, TSearcher, TChangeStrategy>(this IUmbracoBuilder builder, string indexAlias, UmbracoObjectTypes objectTypes)
        where TIndexer : class, IIndexer
        where TSearcher : class, ISearcher
        where TChangeStrategy : class, IContentChangeStrategy
        => builder
            .Services
            .Configure<IndexOptions>(options => options
                .RegisterIndex<TIndexer, TSearcher, TChangeStrategy>(
                    indexAlias,
                    objectTypes
                )
            );
}
