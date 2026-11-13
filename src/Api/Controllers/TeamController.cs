using System;
using System.Threading.Tasks;
using Application;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamController : ControllerBase
{
    private readonly ITeamService _teamService;

    public TeamController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpPatch("{id}/lock")]
    public async Task<IActionResult> LockTeam(Guid id)
    {
        var result = await _teamService.LockTeamAsync(id);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Sastav tima je uspešno zaključan." });
    }
}