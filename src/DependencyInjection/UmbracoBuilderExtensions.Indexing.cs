using Site.NotificationHandlers;
using Umbraco.Cms.Core.Notifications;

namespace Site.DependencyInjection;

// TODO: clean up here
public static partial class UmbracoBuilderExtensions
{
    // public static IUmbracoBuilder RegisterIndexingNotificationHandlers(this IUmbracoBuilder builder)
    //     => builder
    //         //.AddNotificationHandler<IndexingNotification, RandomValueIndexingNotificationHandler>()
    //         .AddNotificationHandler<IndexingNotification, AddSearchProviderNameIndexingNotificationHandler>()
    //         .AddNotificationHandler<IndexingNotification, CleanUpIndexingNotificationHandler>();

    public static IUmbracoBuilder RebuildAllIndexesAfterStartup(this IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<UmbracoApplicationStartedNotification, RebuildAllIndexesNotificationHandler>();
        return builder;
    }
}