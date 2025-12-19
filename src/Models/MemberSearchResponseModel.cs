namespace Site.Models;

public class MemberSearchResponseModel
{
    public required long Total { get; init; }

    public required IEnumerable<MemberSearchResultItemModel> Items { get; init; }

    public required IEnumerable<FacetResultModel> Facets { get; init; }
}