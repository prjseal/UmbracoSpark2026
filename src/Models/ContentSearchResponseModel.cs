namespace Site.Models;

public class ContentSearchResponseModel
{
    public required long Total { get; init; }

    public required IEnumerable<ContentSearchResultItemModel> Items { get; init; }

    public required IEnumerable<FacetResultModel> Facets { get; init; }
}
