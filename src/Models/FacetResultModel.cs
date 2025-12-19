namespace Site.Models;

public class FacetResultModel
{
    public required string FieldName { get; init; }

    public required IEnumerable<FacetValueModel> Values { get; init; }
}
