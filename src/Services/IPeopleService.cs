using Site.Models;

namespace Site.Services;

public interface IPeopleService
{
    void Seed(IEnumerable<Person> people);

    Task<IEnumerable<Person>> GetAllAsync();

    Task<IEnumerable<Person>> GetByIdsAsync(params Guid[] ids);
}