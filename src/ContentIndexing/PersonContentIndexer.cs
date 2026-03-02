using Site.Extensions;
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

        // get the corresponding person from the person service
        var person = await _memberToPersonService.GetPersonForMemberAsync(member);

        // return the index fields for the person
        return person?.AsIndexFields() ?? [];
    }
}