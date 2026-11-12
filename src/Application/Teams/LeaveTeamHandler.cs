using System;
using System.Threading;
using System.Threading.Tasks;
using Domain;

namespace Application.Teams
{
    public record LeaveTeamCommand(Guid TeamId, Guid StudentId);

    public class LeaveTeamHandler
    {
        private readonly ITeamRepository _teamRepository;

        public LeaveTeamHandler(ITeamRepository teamRepository)
        {
            _teamRepository = teamRepository;
        }

        public async Task HandleAsync(LeaveTeamCommand command, CancellationToken cancellationToken = default)
        {
            var team = await _teamRepository.GetByIdAsync(command.TeamId);
            if (team == null)
            {
                throw new InvalidOperationException("Tim nije postoje");
            }

            team.RemoveMember(command.StudentId);

            await _teamRepository.UpdateAsync(team);
        }
    }
}
