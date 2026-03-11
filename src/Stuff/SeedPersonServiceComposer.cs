using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;

namespace Site.Stuff;

public class SeedPersonServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, SeedPersonServiceNotificationHandler>();
}