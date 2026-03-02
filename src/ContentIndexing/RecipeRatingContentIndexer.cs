// TODO: DELETE THIS FILE
// using Site.Services;
// using Umbraco.Cms.Core.Models;
// using Umbraco.Cms.Search.Core.Models.Indexing;
// using Umbraco.Cms.Search.Core.Services.ContentIndexing;
//
// namespace Site.ContentIndexing;
//
// public class RecipeRatingContentIndexer : IContentIndexer
// {
//     private readonly IRecipeRatingService _recipeRatingService;
//
//     public RecipeRatingContentIndexer(IRecipeRatingService recipeRatingService)
//         => _recipeRatingService = recipeRatingService;
//
//     public Task<IEnumerable<IndexField>> GetIndexFieldsAsync(IContentBase content, string?[] cultures, bool published, CancellationToken cancellationToken)
//     {
//         if (content.ContentType.Alias is not "recipe")
//         {
//             return Task.FromResult(Enumerable.Empty<IndexField>());
//         }
//
//         var rating = _recipeRatingService.Get(content.Key);
//         IEnumerable<IndexField> indexFields = [
//             new (
//                 "rating",
//                 new IndexValue
//                 {
//                     Decimals = [Convert.ToDecimal(rating)]
//                 },
//                 Culture: null,
//                 Segment: null
//             )
//         ];
//
//         return Task.FromResult(indexFields);
//     }
// }