using Site.Extensions;
using Site.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.ContentIndexing;

public class MemberContentChangeStrategy : IMemberContentChangeStrategy
{
    private readonly ISystemFieldsContentIndexer _systemFieldsContentIndexer;
    private readonly IMemberService _memberService;
    private readonly IMemberToPersonService _memberToPersonService;
    private readonly ILogger<MemberContentChangeStrategy> _logger;

    private string IndexAlias => SiteConstants.IndexAliases.CustomMemberIndex;
    
    public MemberContentChangeStrategy(
        // TODO: inject this explicitly once Search has an explicit registration for ISystemFieldsContentIndexer
        //ISystemFieldsContentIndexer systemFieldsContentIndexer,
        IEnumerable<IContentIndexer> contentIndexers,
        IMemberService memberService,
        IMemberToPersonService memberToPersonService,
        ILogger<MemberContentChangeStrategy> logger
    )
    {
        // TODO: add this when ISystemFieldsContentIndexer can be injected
        // _systemFieldsContentIndexer = systemFieldsContentIndexer;
        // TODO: remove this when ISystemFieldsContentIndexer can be injected
        _systemFieldsContentIndexer = contentIndexers.OfType<ISystemFieldsContentIndexer>().Single();
        _memberService = memberService;
        _logger = logger;
        _memberToPersonService = memberToPersonService;
    }

    public async Task HandleAsync(IEnumerable<IndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        var indexInfo = indexInfos.FirstOrDefault();
        if (indexInfo?.IndexAlias != IndexAlias)
        {
            return;
        }

        // get the relevant changes for this change strategy
        var changesAsArray = changes.Where(change =>
                change.ContentState is ContentState.Draft
                && change.ObjectType is UmbracoObjectTypes.Member)
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
                // if the member is not approved, make sure it's removed from the index
                unApprovedMemberIds.Add(member.Key);
            }
        }

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

        // append the "Person" fields from the people service
        var person = await _memberToPersonService.GetPersonForMemberAsync(member);
        if (person is null)
        {
            _logger.LogWarning("No Person were found for member with ID: {id}", member.Key);
            return;
        }
        
        fields.AddRange(person.AsIndexFields());

        await indexer.AddOrUpdateAsync(
            IndexAlias,
            member.Key,
            UmbracoObjectTypes.Member,
            [new Variation(null, null)],
            fields,
            null);
    }
}
