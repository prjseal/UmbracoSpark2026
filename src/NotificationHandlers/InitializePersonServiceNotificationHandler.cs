using System.Text.Json;
using Site.Models;
using Site.Services;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Site.NotificationHandlers;

public class InitializePersonServiceNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IPersonService _personService;

    public InitializePersonServiceNotificationHandler(IPersonService personService)
        => _personService = personService;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync("people.json", cancellationToken);
        var people = JsonSerializer.Deserialize<Person[]>(text, JsonSerializerOptions.Web)
                   ?? throw new InvalidOperationException("Could not deserialize the JSON file");

        _personService.Seed(people);
    }
}