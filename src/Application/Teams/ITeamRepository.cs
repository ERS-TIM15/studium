using Domain;
namespace Application.Teams
{
    public interface ITeamRepository
    {
        Task<Team?> GetByIdAsync(Guid teamId);
        Task UpdateAsync(Team team);
    }
}
