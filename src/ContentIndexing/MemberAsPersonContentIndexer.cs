using Site.Extensions;
using Site.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.ContentIndexing;

public class MemberAsPersonContentIndexer : IContentIndexer
{
    private readonly IMemberToPersonService _memberToPersonService;

    public MemberAsPersonContentIndexer(IMemberToPersonService memberToPersonService)
        => _memberToPersonService = memberToPersonService;


    public async Task<IEnumerable<IndexField>> GetIndexFieldsAsync(IContentBase content, string?[] cultures, bool published, CancellationToken cancellationToken)
    {
        if (content is not IMember member)
        {
            return [];
        }

        var person = await _memberToPersonService.GetPersonForMemberAsync(member);
        return person?.AsIndexFields() ?? [];
    }
}