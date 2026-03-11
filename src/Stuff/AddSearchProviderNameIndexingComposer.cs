using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Search.Core.Notifications;

namespace Site.Stuff;

public class AddSearchProviderNameIndexingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationHandler<ContentIndexingNotification, AddSearchProviderNameIndexingNotificationHandler>();
}