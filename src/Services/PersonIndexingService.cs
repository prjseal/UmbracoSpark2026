using Kjac.SearchProvider.Elasticsearch.Services;
using Site.Extensions;
using Site.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Site.Services;

public class PersonIndexingService : IPersonIndexingService
{
    private readonly IElasticsearchIndexer _indexer;
    private readonly IPersonService _personService;
    private readonly ILogger<PersonIndexingService> _logger;

    public PersonIndexingService(IElasticsearchIndexer indexer, IPersonService personService, ILogger<PersonIndexingService> logger)
    {
        _indexer = indexer;
        _personService = personService;
        _logger = logger;
    }

    private string IndexAlias => SiteConstants.IndexAliases.CustomPersonIndex;
    
    public async Task RebuildIndexAsync()
    {
        _logger.LogInformation("Starting rebuild of index: {indexAlias}...", IndexAlias);

        // fetch all people from the person service
        var people = await _personService.GetAllAsync();

        // reset the search index before rebuilding the index
        await _indexer.ResetAsync(IndexAlias);

        foreach (var person in people)
        {
            // get the index fields for the person
            var fields = GetIndexFields(person);

            // add the person to the index
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

    private static IEnumerable<IndexField> GetIndexFields(Person person)
        =>
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