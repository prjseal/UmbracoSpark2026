namespace Site.Services;

public interface IRecipeRatingService
{
    double Get(Guid id);

    void Set(Guid id, double rating);
}