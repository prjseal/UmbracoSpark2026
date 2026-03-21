using System.Text.Json;
using Site.Models;

namespace Site.Services;

public class PeopleService : IPeopleService
{
    private Person[]? _people;

    public async Task<IEnumerable<Person>> GetAllAsync()
    {
        await EnsurePeopleAreLoaded();
        return _people!;
    }

    public async Task<IEnumerable<Person>> GetByIdsAsync(params Guid[] ids)
    {
        await EnsurePeopleAreLoaded();
        return _people!.Where(person => ids.Contains(person.Id));
    }

    private async Task EnsurePeopleAreLoaded()
    {
        if (_people is not null)
        {
            return;
        }
        var text = await File.ReadAllTextAsync("people.json");
        
        _people = JsonSerializer.Deserialize<Person[]>(text, JsonSerializerOptions.Web)
                     ?? throw new InvalidOperationException("Could not deserialize the JSON file");
    }
}