using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Notifications;

namespace Site.NotificationHandlers;

public class AddSearchProviderNameIndexingNotificationHandler : INotificationHandler<IndexingNotification>
{
    public void Handle(IndexingNotification notification)
    {
        // figure out the name of the search provider (based on the index alias)
        var searchProviderName = notification.IndexInfo.IndexAlias switch
        {
            Umbraco.Cms.Search.Core.Constants.IndexAliases.PublishedContent => "Examine",
            SiteConstants.IndexAliases.CustomIndexElasticsearch => "Elasticsearch",
            _ => null
        };

        if (searchProviderName is null)
        {
            // this is one of the internal indexes (e.g. draft content)
            return;
        }

        // add an extra field to enable free text querying on the search provider name
        notification.Fields = notification
            .Fields
            .Union([
                new IndexField(
                    FieldName: "searchProviderName",
                    Value: new IndexValue { Texts = [searchProviderName] },
                    Culture: null,
                    Segment: null
                )
            ])
            .ToArray();
    }
}























