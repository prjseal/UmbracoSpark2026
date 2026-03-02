using Site.Services;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Notifications;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;

namespace Site.NotificationHandlers;

public class RecipeRatingIndexingNotificationHandler : INotificationAsyncHandler<IndexingNotification>
{
    private readonly IPublishedContentCache _publishedContentCache;
    private readonly IRecipeRatingService _recipeRatingService;

    public RecipeRatingIndexingNotificationHandler(IPublishedContentCache publishedContentCache, IRecipeRatingService recipeRatingService)
    {
        _publishedContentCache = publishedContentCache;
        _recipeRatingService = recipeRatingService;
    }

    public async Task HandleAsync(IndexingNotification notification, CancellationToken cancellationToken)
    {
        if (notification.IndexInfo.IndexAlias is not SearchConstants.IndexAliases.PublishedContent)
        {
            return;
        }

        var content = await _publishedContentCache.GetByIdAsync(notification.Id);
        if (content?.ContentType.Alias != "recipe")
        {
            return;
        }

        var rating = _recipeRatingService.Get(notification.Id);

        notification.Fields = notification
            .Fields
            .Union([
                new IndexField(
                    FieldName: "rating",
                    Value: new IndexValue { Decimals = [Convert.ToDecimal(rating)] },
                    Culture: null,
                    Segment: null
                )
            ])
            .ToArray();
    }
}