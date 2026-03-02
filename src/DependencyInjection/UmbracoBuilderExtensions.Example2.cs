using Kjac.SearchProvider.Elasticsearch.Services;
using Site.ContentIndexing;
using Site.NotificationHandlers;
using Site.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;

namespace Site.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder ConfigureExampleTwo(this IUmbracoBuilder builder)
    {
        // re-register the default member index to use Elasticsearch
        builder.Services.Configure<IndexOptions>(options =>
            options.RegisterIndex<IElasticsearchIndexer, IElasticsearchSearcher, IDraftContentChangeStrategy>
            (
                SearchConstants.IndexAliases.DraftMembers,
                UmbracoObjectTypes.Member
            )
        );

        // add the required services for example two (part 1)
        builder.Services
            .AddSingleton<IPersonService, PersonService>()
            .AddSingleton<IMemberToPersonService, MemberToPersonService>()
            .AddTransient<IContentIndexer, PersonContentIndexer>();

        // add a notification handler to initialize the person service at startup
        builder
            .AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, InitializePersonServiceNotificationHandler>();

        return builder;
    }
}
