using Site.Models;

namespace Site.Services;

public interface IPersonService
{
    void Seed(IEnumerable<Person> people);

    Task<IEnumerable<Person>> GetAllAsync();

    Task<Person?> GetByIdAsync(Guid id);

    Task<IEnumerable<Person>> GetByIdsAsync(params Guid[] ids);
}