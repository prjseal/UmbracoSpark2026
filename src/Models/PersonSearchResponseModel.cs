namespace Site.Models;

public class PersonSearchResponseModel
{
    public required long Total { get; init; }

    public required IEnumerable<PersonSearchResultItemModel> Items { get; init; }

    public required IEnumerable<FacetResultModel> Facets { get; init; }
}