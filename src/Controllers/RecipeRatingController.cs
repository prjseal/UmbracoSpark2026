using Microsoft.AspNetCore.Mvc;
using Site.Services;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipeRatingController : ControllerBase
{
    private readonly IRecipeRatingService _recipeRatingService;
    private readonly IContentService _contentService;
    private readonly IDistributedContentIndexRefresher _distributedContentIndexRefresher;

    public RecipeRatingController(
        IRecipeRatingService recipeRatingService,
        IContentService contentService,
        IDistributedContentIndexRefresher distributedContentIndexRefresher)
    {
        _recipeRatingService = recipeRatingService;
        _contentService = contentService;
        _distributedContentIndexRefresher = distributedContentIndexRefresher;
    }

    [HttpPost("{id:guid}")]
    public IActionResult Rate(Guid id, double rating)
    {
        var content = _contentService.GetById(id);
        if (content is not { Published: true, ContentType.Alias: "recipe" })
        {
            return BadRequest();
        }

        // update the recipe rating
        _recipeRatingService.Set(id, rating);

        // trigger a reindex of the recipe to update the rating
        // NOTE: this should really be handled with a timed delay on a background thread, to handle multiple ratings
        //       withing a timeframe as a single indexing operation.
        _distributedContentIndexRefresher.RefreshContent([content], ContentState.Published);

        return Ok();
    }
}











