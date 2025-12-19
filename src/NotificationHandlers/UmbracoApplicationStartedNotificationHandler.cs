using System.Text.Json;
using Site.Models;
using Site.Services;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Site.NotificationHandlers;

public class UmbracoApplicationStartedNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IPeopleService _peopleService;

    public UmbracoApplicationStartedNotificationHandler(IPeopleService peopleService)
        => _peopleService = peopleService;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync("people.json", cancellationToken);
        var people = JsonSerializer.Deserialize<Person[]>(text, JsonSerializerOptions.Web)
                   ?? throw new InvalidOperationException("Could not deserialize the JSON file");

        _peopleService.Seed(people);
    }
}