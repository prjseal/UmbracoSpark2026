using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Notifications;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;

namespace Site.NotificationHandlers;

public class RelatedRecipeIndexingNotificationHandler : INotificationAsyncHandler<ContentIndexingNotification>
{
    private readonly IPublishedContentCache _publishedContentCache;

    public RelatedRecipeIndexingNotificationHandler(IPublishedContentCache publishedContentCache)
        => _publishedContentCache = publishedContentCache;

    public async Task HandleAsync(ContentIndexingNotification notification, CancellationToken cancellationToken)
    {
        if (notification.IndexAlias is not SearchConstants.IndexAliases.PublishedContent)
        {
            return;
        }

        var content = await _publishedContentCache.GetByIdAsync(notification.Id);
        if (content?.ContentType.Alias != "recipe")
        {
            return;
        }

        var relatedRecipe = content.Value<IPublishedContent>("relatedRecipe");
        if (relatedRecipe is null)
        {
            return;
        }
        
        notification.Fields = notification
            .Fields
            .Union([
                new IndexField(
                    FieldName: "relatedRecipeName",
                    Value: new IndexValue { Texts = [relatedRecipe.Name] },
                    Culture: null,
                    Segment: null
                )
            ])
            .ToArray();
    }
}