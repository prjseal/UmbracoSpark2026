using Site.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Site.Extensions;

public static class PersonExtensions
{
    public static IEnumerable<IndexField> AsIndexFields(this Person person)
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