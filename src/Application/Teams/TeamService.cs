using System;
using System.Threading.Tasks;
using Interface.Repositories;
using Domain.Activity;

namespace Application.Teams;

public class TeamService : ITeamService
{
    private readonly ITeamRepository _teamRepository;

    public TeamService(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<(bool IsSuccess, string ErrorMessage)> LockTeamAsync(Guid teamId)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null)
        {
            return (false, "Tim nije pronađen.");
        }

        bool isLocked = team.LockTeam();
        if (!isLocked)
        {
            return (false, "Tim je već zaključan.");
        }

        await _teamRepository.UpdateAsync(team);
        return (true, string.Empty);
    }
}