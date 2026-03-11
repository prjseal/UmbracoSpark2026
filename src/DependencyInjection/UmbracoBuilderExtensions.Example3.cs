using Site.ContentIndexing;
using Site.NotificationHandlers;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Cms.Search.Core.Services;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;

namespace Site.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder ConfigureExampleThree(this IUmbracoBuilder builder)
    {
        // add a notification handler to enrich the index with related recipe values
        builder.AddNotificationAsyncHandler<ContentIndexingNotification, RelatedRecipeIndexingNotificationHandler>();
        
        // re-register the default published content index to use a custom content change strategy
        builder.Services.Configure<IndexOptions>(options =>
            options.RegisterContentIndex<IIndexer, ISearcher, RelatedRecipePublishedContentChangeStrategy>
            (
                SearchConstants.IndexAliases.PublishedContent,
                UmbracoObjectTypes.Document
            )
        );
        
        // also remember to actually register the custom change strategy itself
        builder.Services.AddSingleton<RelatedRecipePublishedContentChangeStrategy>();

        return builder;
    }
}







