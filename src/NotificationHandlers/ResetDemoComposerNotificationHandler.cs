using Site.Services;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.NotificationHandlers;

// A notification handler to reset the demo at start-up.
public class ResetDemoComposerNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IIndexDocumentService _indexDocumentService;
    private readonly IOriginProvider _originProvider;
    private readonly IContentIndexingService _contentIndexingService;
    private readonly IPeopleIndexingService _peopleIndexingService;

    public ResetDemoComposerNotificationHandler(
        IIndexDocumentService indexDocumentService,
        IOriginProvider originProvider,
        IContentIndexingService contentIndexingService,
        IPeopleIndexingService peopleIndexingService)
    {
        _indexDocumentService = indexDocumentService;
        _originProvider = originProvider;
        _contentIndexingService = contentIndexingService;
        _peopleIndexingService = peopleIndexingService;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        // The published content indexes might contain recipe ratings that differ from the ones returned by the recipe
        // rating service, since the latter is not persistent. Thus, rebuild the indexes for the demo to make sense.
        await RebuildPublishedContentIndexesAsync();

        // Comment this in to ensure that the people index is populated. This is not necessary to call at every boot,
        // since the demo does not alter the people index after boot.
        // await  RebuildPeopleIndexAsync();
    }

    private async Task RebuildPublishedContentIndexesAsync()
    {
        // NOTE: All this might be handled a little more gracefully by Umbraco Search in the future. Be sure to check the docs.

        // Clear the index document cache before enforcing a rebuild of the published indexes.
        await _indexDocumentService.DeleteAllAsync();
        _contentIndexingService.Rebuild(Constants.IndexAliases.PublishedContent, _originProvider.GetCurrent());
        _contentIndexingService.Rebuild(SiteConstants.IndexAliases.CustomIndexElasticsearch, _originProvider.GetCurrent());
    }

    private async Task RebuildPeopleIndexAsync()
        => await _peopleIndexingService.RebuildIndexAsync();
}