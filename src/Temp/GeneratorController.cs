using Microsoft.AspNetCore.Mvc;
using Site.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;

namespace Site.Temp;

// TODO: DELETE THIS CONTROLLER
[ApiController]
[Route("api/[controller]")]
public class GeneratorController : ControllerBase
{
    private readonly IMemberEditingService _memberEditingService;
    private readonly IUserService _userService;
    private readonly IPeopleService _peopleService;

    public GeneratorController(IMemberEditingService memberEditingService, IUserService userService, IPeopleService peopleService)
    {
        _memberEditingService = memberEditingService;
        _userService = userService;
        _peopleService = peopleService;
    }

    [HttpPost]
    public async Task<IActionResult> Generate()
    {
        // return await ImportMembers();
        return Ok();
    }

    private async Task<IActionResult> ImportMembers()
    {
        var superUser = await _userService.GetAsync(Constants.Security.SuperUserKey)
                        ?? throw new InvalidOperationException("No super user found");
        var random = new Random();
        var people = await _peopleService.GetAllAsync();
        var completed = 0;
        foreach (var person in people.Skip(10))
        {
            var createResult = await _memberEditingService.CreateAsync(
                new MemberCreateModel
                {
                    ContentTypeKey = Guid.Parse("d59be02f-1df9-4228-aa1e-01917d806cda"),
                    Email = person.Email,
                    Username = person.Email,
                    Password = Guid.NewGuid().ToString("N"),
                    IsApproved = true,
                    Properties =
                    [
                        new()
                        {
                            Alias = SiteConstants.FieldNames.Personality,
                            Value = random.Next(2) == 1 ? "Cat person" : "Dog person"
                        },
                        new()
                        {
                            Alias = SiteConstants.FieldNames.PersonId,
                            Value = person.Id.ToString("D")
                        }
                    ],
                    Variants =
                    [
                        new()
                        {
                            Name = person.Email
                        }
                    ]
                },
                superUser
            );

            if (createResult.Success is false)
            {
                return BadRequest($"Could not create a member: {createResult.Status}");
            }

            Console.CursorLeft = 0;
            Console.Write($"Completed: {++completed}...    ");
        }

        Console.WriteLine();
        Console.WriteLine("Done!");

        return Ok();
    }
}