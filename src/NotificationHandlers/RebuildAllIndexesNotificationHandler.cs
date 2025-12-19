using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;

namespace Site.NotificationHandlers;

public class RebuildAllIndexesNotificationHandler : INotificationHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentIndexingService _contentIndexingService;
    private readonly ILogger<RebuildAllIndexesNotificationHandler> _logger;

    public RebuildAllIndexesNotificationHandler(
        IContentIndexingService contentIndexingService,
        ILogger<RebuildAllIndexesNotificationHandler> logger)
    {
        _contentIndexingService = contentIndexingService;
        _logger = logger;
    }

    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        _logger.LogInformation("Rebuilding all search indexes...");
        // _contentIndexingService.Rebuild(SearchConstants.IndexAliases.PublishedContent);
        // _contentIndexingService.Rebuild(SearchConstants.IndexAliases.DraftMembers);
        // _contentIndexingService.Rebuild(Constants.CustomIndexes.Algolia);
        // _contentIndexingService.Rebuild(Constants.CustomIndexes.Elasticsearch);
        // _contentIndexingService.Rebuild(SiteConstants.IndexAliases.CustomMemberIndex);
    }
}
