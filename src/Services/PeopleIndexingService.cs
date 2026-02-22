using Kjac.SearchProvider.Elasticsearch.Services;
using Site.Extensions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Site.Services;

public class PeopleIndexingService : IPeopleIndexingService
{
    private readonly IElasticsearchIndexer _indexer;
    private readonly IPeopleService _peopleService;
    private readonly ILogger<PeopleIndexingService> _logger;

    public PeopleIndexingService(IElasticsearchIndexer indexer, IPeopleService peopleService, ILogger<PeopleIndexingService> logger)
    {
        _indexer = indexer;
        _peopleService = peopleService;
        _logger = logger;
    }

    private string IndexAlias => SiteConstants.IndexAliases.CustomPeopleIndex;
    
    public async Task RebuildIndexAsync()
    {
        _logger.LogInformation("Starting rebuild of index: {indexAlias}...", IndexAlias);

        var people = await _peopleService.GetAllAsync();

        await _indexer.ResetAsync(IndexAlias);

        foreach (var person in people)
        {
            var fields = person.AsIndexFields();

            await _indexer.AddOrUpdateAsync(
                IndexAlias,
                person.Id,
                UmbracoObjectTypes.Unknown,
                [new Variation(null, null)],
                fields,
                null);
        }

        _logger.LogInformation("Finished rebuild of index: {indexAlias}", IndexAlias);
    }
}