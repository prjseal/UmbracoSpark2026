using Site.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.ContentIndexing;

public class RecipeRatingContentIndexer : IContentIndexer
{
    private readonly IRecipeRatingService _recipeRatingService;

    public RecipeRatingContentIndexer(IRecipeRatingService recipeRatingService)
        => _recipeRatingService = recipeRatingService;

    public Task<IEnumerable<IndexField>> GetIndexFieldsAsync(
        IContentBase content,
        string?[] cultures,
        bool published,
        CancellationToken cancellationToken)
    {
        if (content.ContentType.Alias is not "recipe")
        {
            // this only applies to recipes
            return Task.FromResult(Enumerable.Empty<IndexField>());
        }

        // get the recipe rating
        var rating = _recipeRatingService.Get(content.Key);

        // add the recipe rating to the index in a decimal field called "rating"
        return Task.FromResult<IEnumerable<IndexField>>([
            new IndexField(
                FieldName: "rating",
                Value: new IndexValue { Decimals = [Convert.ToDecimal(rating)] },
                Culture: null,
                Segment: null
            )
        ]);
    }
}