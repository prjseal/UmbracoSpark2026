using Site.Models;
using Umbraco.Cms.Core.Models;

namespace Site.Services;

public interface IMemberToPersonService
{
    Task<Person?> GetPersonForMemberAsync(IMember member);
}