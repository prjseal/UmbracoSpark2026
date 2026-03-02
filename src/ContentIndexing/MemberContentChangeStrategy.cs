using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using SearchConstants = Umbraco.Cms.Search.Core.Constants;

namespace Site.ContentIndexing;

public class MemberContentChangeStrategy : IMemberContentChangeStrategy
{
    private readonly ISystemFieldsContentIndexer _systemFieldsContentIndexer;
    private readonly PersonContentIndexer _personContentIndexer;
    private readonly IMemberService _memberService;
    private readonly ILogger<MemberContentChangeStrategy> _logger;

    private string IndexAlias => SearchConstants.IndexAliases.DraftMembers;
    
    public MemberContentChangeStrategy(
        IEnumerable<IContentIndexer> contentIndexers,
        IMemberService memberService,
        ILogger<MemberContentChangeStrategy> logger
    )
    {
        var contentIndexersAsArray = contentIndexers as IContentIndexer[] ?? contentIndexers.ToArray();

        _systemFieldsContentIndexer = contentIndexersAsArray.OfType<ISystemFieldsContentIndexer>().Single();
        _personContentIndexer = contentIndexersAsArray.OfType<PersonContentIndexer>().Single();
        _memberService = memberService;
        _logger = logger;
    }

    public async Task HandleAsync(IEnumerable<IndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        var indexInfo = indexInfos.FirstOrDefault();
        if (indexInfo?.IndexAlias != IndexAlias)
        {
            return;
        }

        // get the relevant changes for this change strategy
        var changesAsArray = changes
            .Where(change => change.ObjectType is UmbracoObjectTypes.Member)
            .ToArray();

        if (changesAsArray.Length is 0)
        {
            return;
        }

        // first handle all removals
        var idsToRemove = changesAsArray
            .Where(change => change.ChangeImpact is ChangeImpact.Remove)
            .Select(change => change.Id)
            .ToArray();
        await indexInfo.Indexer.DeleteAsync(IndexAlias, idsToRemove);

        // now handle all updates
        var idsToUpdate = changesAsArray
            .Select(change => change.Id)
            .Except(idsToRemove)
            .ToArray();

        if (idsToUpdate.Length == 0)
        {
            return;
        }

        var unApprovedMemberIds = new List<Guid>();
        foreach (var id in idsToUpdate)
        {
            var member = _memberService.GetById(id);
            if (member is null)
            {
                continue;
            }

            if (member.IsApproved)
            {
                await UpdateIndexAsync(member, indexInfo.Indexer, cancellationToken);
            }
            else
            {
                unApprovedMemberIds.Add(member.Key);
            }
        }

        // remove any unapproved members (if they are still in the index)
        await indexInfo.Indexer.DeleteAsync(IndexAlias, unApprovedMemberIds);
    }

    public async Task RebuildAsync(IndexInfo indexInfo, CancellationToken cancellationToken)
    {
        if (indexInfo.IndexAlias != IndexAlias)
        {
            return;
        }

        // first clear out the index
        await indexInfo.Indexer.ResetAsync(IndexAlias);

        // next iterate all members and push updates to the index
        IMember[] members;
        var pageIndex = 0;
        const int pageSize = 1000;
        do
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            members = _memberService.GetAll(pageIndex, pageSize, out _, "sortOrder", Direction.Ascending).ToArray();
            foreach (IMember member in members)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await UpdateIndexAsync(member, indexInfo.Indexer, cancellationToken);
            }

            pageIndex++;
        }
        while (members.Length == pageSize);

        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Cancellation requested for rebuild of index: {indexAlias}", IndexAlias);
        }
    }

    private async Task UpdateIndexAsync(IMember member, IIndexer indexer, CancellationToken cancellationToken)
    {
        var fields = (await _systemFieldsContentIndexer.GetIndexFieldsAsync(member, [], false, cancellationToken)).ToList();
        if (fields.Any() is false)
        {
            _logger.LogWarning("No system fields were found for member with ID: {id}", member.Key);
            return;
        }

        // append the "personality" field from the Umbraco Member
        var personality = member.GetValue<string>(SiteConstants.FieldNames.Personality);
        if (personality.IsNullOrWhiteSpace() is false)
        {
            fields.Add(
                new (
                    SiteConstants.FieldNames.Personality,
                    new IndexValue
                    {
                        Keywords = [personality]
                    },
                    Culture: null,
                    Segment: null
                )
            );
        }

        // append the "Person" fields from the person service (via the person content indexer)
        var personFields = await _personContentIndexer.GetIndexFieldsAsync(member, [], false, cancellationToken);
        fields.AddRange(personFields);

        await indexer.AddOrUpdateAsync(
            IndexAlias,
            member.Key,
            UmbracoObjectTypes.Member,
            [new Variation(null, null)],
            fields,
            null);
    }
}
