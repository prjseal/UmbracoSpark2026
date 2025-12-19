using Site.ContentIndexing;
using Site.NotificationHandlers;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.DependencyInjection;

// TODO: clean up here
public static partial class UmbracoBuilderExtensions
{
    // public static IUmbracoBuilder RegisterIndexingNotificationHandlers(this IUmbracoBuilder builder)
    //     => builder
    //         //.AddNotificationHandler<IndexingNotification, RandomValueIndexingNotificationHandler>()
    //         .AddNotificationHandler<IndexingNotification, AddSearchProviderNameIndexingNotificationHandler>()
    //         .AddNotificationHandler<IndexingNotification, CleanUpIndexingNotificationHandler>();

    public static IUmbracoBuilder RegisterContentIndexers(this IUmbracoBuilder builder)
    {
        builder.Services.AddTransient<IContentIndexer, PersonContentIndexer>();
        // builder.Services.AddTransient<IContentIndexer, PublishYearAsDecadeContentIndexer>();
        // builder.Services.AddTransient<IContentIndexer, AlgoliaBookContentIndexer>();
        return builder;
    }

    // public static IUmbracoBuilder RebuildAllIndexesAfterStartup(this IUmbracoBuilder builder)
    // {
    //     builder.AddNotificationHandler<UmbracoApplicationStartedNotification, RebuildAllIndexesNotificationHandler>();
    //     return builder;
    // }
}