using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.Stuff;

// clear the index document cache when starting the site, to ensure all cached ratings are flushed
public class ClearIndexDocumentCacheNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IIndexDocumentService _indexDocumentService;

    public ClearIndexDocumentCacheNotificationHandler(IIndexDocumentService indexDocumentService)
        => _indexDocumentService = indexDocumentService;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
        => await _indexDocumentService.DeleteAllAsync();
}