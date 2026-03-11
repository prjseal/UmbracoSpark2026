using Microsoft.AspNetCore.Mvc;
using Site.Services;

namespace Site.Stuff;

[ApiController]
[Route("api/[controller]")]
public class PersonIndexingController : ControllerBase
{
    private readonly IPersonIndexingService _personIndexingService;

    public PersonIndexingController(IPersonIndexingService personIndexingService)
        => _personIndexingService = personIndexingService;

    [HttpPost]
    public async Task<IActionResult> Rebuild()
    {
        await _personIndexingService.RebuildIndexAsync();
        return Ok("👍");
    }
}