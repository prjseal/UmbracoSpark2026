using Kjac.SearchProvider.Elasticsearch.Services;
using Site.ContentIndexing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Configuration;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;

namespace Site.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder ConfigureExampleFour(this IUmbracoBuilder builder)
    {
        // re-register the default member index to use Elasticsearch with a custom change strategy
        builder.Services.Configure<IndexOptions>(options =>
            options.RegisterIndex<IElasticsearchIndexer, IElasticsearchSearcher, IMemberContentChangeStrategy>
            (
                SearchConstants.IndexAliases.DraftMembers,
                UmbracoObjectTypes.Member
            )
        );

        // add the required services for example two (part 2)
        builder.Services
            .AddTransient<IMemberContentChangeStrategy, MemberContentChangeStrategy>();

        return builder;
    }
}
