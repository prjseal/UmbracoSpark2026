namespace Site.Models;

public class PersonSearchResultItemModel
{
    public required string Name { get; init; }

    public required string Email { get; init; }

    public required DateTimeOffset Birthdate { get; init; }

    public required string Zodiac { get; init; }

    public required string Genre { get; init; }

    public required string Generation { get; init; }
}
