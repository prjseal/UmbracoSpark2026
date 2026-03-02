using Site.Models;
using Umbraco.Cms.Core.Models;

namespace Site.Services;

public class MemberToPersonService : IMemberToPersonService
{
    private readonly IPersonService _personService;
    private readonly ILogger<MemberToPersonService> _logger;

    public MemberToPersonService(IPersonService personService, ILogger<MemberToPersonService> logger)
    {
        _personService = personService;
        _logger = logger;
    }

    public async Task<Person?> GetPersonForMemberAsync(IMember member)
    {
        if (Guid.TryParse(member.GetValue<string>(SiteConstants.FieldNames.PersonId), out Guid personId) is false)
        {
            _logger.LogWarning("Could not parse the Person ID for member with Umbraco ID: {umbracoId}", member.Key);
            return null;
        }

        var person = await _personService.GetByIdAsync(personId);
        if (person is null)
        {
            _logger.LogWarning("Could not retrieve the Person for Person ID: {personId}", personId);
        }

        return person;
    }
}