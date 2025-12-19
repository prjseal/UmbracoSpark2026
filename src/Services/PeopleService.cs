using Site.Models;

namespace Site.Services;

public class PeopleService : IPeopleService
{
    private readonly List<Person> _people = new();

    public void Seed(IEnumerable<Person> people)
    {
        _people.Clear();
        _people.AddRange(people);
    }

    public Task<IEnumerable<Person>> GetAllAsync() => Task.FromResult<IEnumerable<Person>>(_people);

    public Task<Person?> GetByIdAsync(Guid id) => Task.FromResult(_people.FirstOrDefault(x => x.Id == id));
}