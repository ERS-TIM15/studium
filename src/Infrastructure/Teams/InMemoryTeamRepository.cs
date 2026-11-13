using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Teams;
using Domain;

namespace Infrastructure.Teams
{
    public class InMemoryTeamRepository : ITeamRepository
    {
        private readonly List<Team> _teams = new();

        public Task<Team?> GetByIdAsync(Guid teamId)
        {
            
            var team = _teams.FirstOrDefault(t => t.Name == "DemoTeam");
            return Task.FromResult(team);
        }

        public Task UpdateAsync(Team team)
        {
            return Task.CompletedTask;
        }
    }
}
