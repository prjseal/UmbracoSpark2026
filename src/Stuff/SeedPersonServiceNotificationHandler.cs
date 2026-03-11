using System.Text.Json;
using Site.Models;
using Site.Services;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Site.Stuff;

public class SeedPersonServiceNotificationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IPersonService _personService;

    public SeedPersonServiceNotificationHandler(IPersonService personService)
        => _personService = personService;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync("people.json", cancellationToken);
        var people = JsonSerializer.Deserialize<Person[]>(text, JsonSerializerOptions.Web)
                   ?? throw new InvalidOperationException("Could not deserialize the JSON file");

        _personService.Seed(people);
    }
}