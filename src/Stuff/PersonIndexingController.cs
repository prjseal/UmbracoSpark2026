using Microsoft.AspNetCore.Mvc;
using Site.Services;

namespace Site.Stuff;

[ApiController]
[Route("api/[controller]")]
public class PersonIndexingController : ControllerBase
{
    private readonly IPeopleIndexingService _peopleIndexingService;

    public PersonIndexingController(IPeopleIndexingService peopleIndexingService)
        => _peopleIndexingService = peopleIndexingService;

    [HttpPost]
    public async Task<IActionResult> Rebuild()
    {
        await _peopleIndexingService.RebuildIndexAsync();
        return Ok("👍");
    }
}