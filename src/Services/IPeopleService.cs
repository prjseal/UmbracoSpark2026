using Site.Models;

namespace Site.Services;

public interface IPeopleService
{
    Task<IEnumerable<Person>> GetAllAsync();

    Task<IEnumerable<Person>> GetByIdsAsync(params Guid[] ids);
}