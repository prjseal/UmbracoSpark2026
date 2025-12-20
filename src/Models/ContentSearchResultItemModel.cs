namespace Site.Models;

public class ContentSearchResultItemModel
{
    public required string Name { get; init; }

    public required string Url { get; init; }

    public required string? Teaser { get; init; }

    public required IEnumerable<string>? Cuisine { get; init; }

    public required IEnumerable<string>? MealType { get; init; }

    public required int? PreparationTime { get; init; }
}
