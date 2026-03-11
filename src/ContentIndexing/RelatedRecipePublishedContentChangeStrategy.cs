using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.ContentIndexing;

public class RelatedRecipePublishedContentChangeStrategy : IContentChangeStrategy
{
    private readonly IPublishedContentChangeStrategy _publishedContentChangeStrategy;
    private readonly ITrackedReferencesService _trackedReferencesService;

    public RelatedRecipePublishedContentChangeStrategy(IPublishedContentChangeStrategy publishedContentChangeStrategy, ITrackedReferencesService trackedReferencesService)
    {
        _publishedContentChangeStrategy = publishedContentChangeStrategy;
        _trackedReferencesService = trackedReferencesService;
    }

    public async Task HandleAsync(IEnumerable<ContentIndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        var contentChangesAsArray = changes as ContentChange[] ?? changes.ToArray();
        var documentIds = contentChangesAsArray
            .Where(change => change.ObjectType is UmbracoObjectTypes.Document
                             && change.ContentState is ContentState.Published)
            .Select(change => change.Id)
            .ToArray();

        var relatedDocumentIds = new List<Guid>();
        foreach (var documentId in documentIds)
        {
            // get all relations for the document
            // NOTE: for simplicity, just fetch the first 1000 relations here
            var references = await _trackedReferencesService.GetPagedRelationsForItemAsync(
                documentId,
                UmbracoObjectTypes.Document,
                0,
                1000,
                true);
            if (references.Success)
            {
                relatedDocumentIds.AddRange(references.Result.Items.Select(item => item.NodeKey));
            }
        }

        var effectiveChanges = contentChangesAsArray
            .Union(relatedDocumentIds
                .Except(documentIds)
                .Select(documentId =>
                    ContentChange.Document(documentId, ChangeImpact.Refresh, ContentState.Published))
            );
        
        // let the core change strategy handle the effective changes
        await _publishedContentChangeStrategy.HandleAsync(indexInfos, effectiveChanges, cancellationToken);
    }

    public async Task RebuildAsync(ContentIndexInfo indexInfo, CancellationToken cancellationToken)
        // just delegate this entirely to the core change strategy
        => await _publishedContentChangeStrategy.RebuildAsync(indexInfo, cancellationToken);
}