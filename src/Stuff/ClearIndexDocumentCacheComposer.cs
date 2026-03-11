using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;

namespace Site.Stuff;

public class ClearIndexDocumentCacheComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, ClearIndexDocumentCacheNotificationHandler>();
}