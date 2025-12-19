namespace Site.Models;

public  class Person
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    public required DateTimeOffset Birthdate { get; init; }

    public required string Zodiac { get; init; }

    public required string Genre { get; init; }
}