using Microsoft.AspNetCore.Mvc;
using Site.Services;

namespace Site.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeopleIndexingController : ControllerBase
{
    private readonly IPeopleIndexingService _peopleIndexingService;

    public PeopleIndexingController(IPeopleIndexingService peopleIndexingService)
        => _peopleIndexingService = peopleIndexingService;

    [HttpPost]
    public async Task<IActionResult> Rebuild()
    {
        await _peopleIndexingService.RebuildIndexAsync();
        return Ok("👍");
    }
}