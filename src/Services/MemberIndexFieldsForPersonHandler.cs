using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Site.Services;

public class MemberIndexFieldsForPersonHandler : IMemberIndexFieldsForPersonHandler
{
    private readonly IMemberToPersonService _memberToPersonService;

    public MemberIndexFieldsForPersonHandler(IMemberToPersonService memberToPersonService)
        => _memberToPersonService = memberToPersonService;

    public async Task<IEnumerable<IndexField>> GetIndexFieldsAsync(IMember member)
    {
        var person = await _memberToPersonService.GetPersonForMemberAsync(member);

        return person is null
            ? []
            :
            [
                new(
                    SiteConstants.FieldNames.Zodiac,
                    new IndexValue
                    {
                        Keywords = [person.Zodiac]
                    },
                    Culture: null,
                    Segment: null
                ),
                new(
                    SiteConstants.FieldNames.Name,
                    new IndexValue
                    {
                        Texts = [person.Name]
                    },
                    Culture: null,
                    Segment: null
                ),
                new(
                    SiteConstants.FieldNames.Birthdate,
                    new IndexValue
                    {
                        DateTimeOffsets = [person.Birthdate]
                    },
                    Culture: null,
                    Segment: null
                ),
                new(
                    SiteConstants.FieldNames.Genre,
                    new IndexValue
                    {
                        Keywords = [person.Genre],
                        Texts = [person.Genre],
                    },
                    Culture: null,
                    Segment: null
                ),
            ];
    }
}