using Kjac.SearchProvider.Elasticsearch.Services;
using Site.ContentIndexing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;
using IndexOptions = Umbraco.Cms.Search.Core.Configuration.IndexOptions;

namespace Site.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder RegisterIndexes(this IUmbracoBuilder builder)
    {
        // re-register the default member index to use Elasticsearch
        builder.RegisterIndex<IElasticsearchIndexer, IElasticsearchSearcher, IDraftContentChangeStrategy>
        (
            SearchConstants.IndexAliases.DraftMembers,
            UmbracoObjectTypes.Member
        );

        // register a custom member index using the custom member content change strategy
        builder.RegisterIndex<IElasticsearchIndexer, IElasticsearchSearcher, IMemberContentChangeStrategy>
        (
            SiteConstants.IndexAliases.CustomMemberIndex,
            UmbracoObjectTypes.Member
        );

        return builder;
    }

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
