using Microsoft.AspNetCore.Mvc;
using Site.Services;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipeRatingController : ControllerBase
{
    private readonly IRecipeRatingService _recipeRatingService;
    private readonly IPublishedContentCache _publishedContentCache;
    private readonly IContentIndexingService _contentIndexingService;
    private readonly IIndexDocumentService _indexDocumentService;

    public RecipeRatingController(
        IRecipeRatingService recipeRatingService,
        IPublishedContentCache publishedContentCache,
        IContentIndexingService contentIndexingService,
        IIndexDocumentService indexDocumentService)
    {
        _recipeRatingService = recipeRatingService;
        _publishedContentCache = publishedContentCache;
        _contentIndexingService = contentIndexingService;
        _indexDocumentService = indexDocumentService;
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Rate(Guid id, double rating)
    {
        var content = await _publishedContentCache.GetByIdAsync(id);
        if (content?.ContentType.Alias is not "recipe")
        {
            return BadRequest();
        }

        // update the recipe rating
        _recipeRatingService.Set(id, rating);

        // the recipe ratings are appended with a content indexer, which means they become part of the index
        // document cache in the DB... which in turn means we need to flush the cache to trigger a re-index.
        await _indexDocumentService.DeleteAsync([id], true);
        
        // trigger a reindex of the recipe to update the rating
        // NOTE: this should really be handled with a timed delay on a background thread, to handle multiple ratings
        //       withing a timeframe as a single indexing operation.
        _contentIndexingService.Handle([ContentChange.Document(id, ChangeImpact.Refresh, ContentState.Published)]);
        return Ok();
    }
}











