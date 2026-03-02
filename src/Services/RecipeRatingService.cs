namespace Site.Services;

public class RecipeRatingService : IRecipeRatingService
{
    private readonly Dictionary<Guid, RatingData> _ratings = new();

    public double Get(Guid id)
        => _ratings.TryGetValue(id, out var ratingData)
            ? Math.Round(ratingData.Total / ratingData.Votes, 1)
            : 2.5;

    public void Set(Guid id, double rating)
    {
        if (_ratings.TryGetValue(id, out var current) is false)
        {
            current = new RatingData(0, 0);
        }
        _ratings[id] = new RatingData(current.Votes + 1, current.Total +  rating);
    }
    
    private record RatingData(int Votes, double Total);
}