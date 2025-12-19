using Site.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.ContentIndexing;

public class PersonContentIndexer : IContentIndexer
{
    private readonly IMemberToPersonService _memberToPersonService;

    public PersonContentIndexer(IMemberToPersonService memberToPersonService)
        => _memberToPersonService = memberToPersonService;

    public async Task<IEnumerable<IndexField>> GetIndexFieldsAsync(IContentBase content, string?[] cultures, bool published, CancellationToken cancellationToken)
    {
        if (content is not IMember member)
        {
            return [];
        }

        var person = await _memberToPersonService.GetPersonForMemberAsync(member);
        if (person is null)
        {
            return [];
        }

        return
        [
            new (
                SiteConstants.FieldNames.Zodiac,
                new IndexValue
                {
                    Keywords = [person.Zodiac]
                },
                Culture: null,
                Segment: null
            ),
            new (
                SiteConstants.FieldNames.Name,
                new IndexValue
                {
                    Texts = [person.Name]
                },
                Culture: null,
                Segment: null
            ),
            new (
                SiteConstants.FieldNames.Birthdate,
                new IndexValue
                {
                    DateTimeOffsets = [person.Birthdate]
                },
                Culture: null,
                Segment: null
            ),
            new (
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