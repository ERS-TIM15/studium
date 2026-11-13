using Domain.Teams;

namespace Application.Teams;

/// <summary>
/// Port za čuvanje granica veličine tima po projektnoj aktivnosti. Ponovno
/// čuvanje za istu aktivnost zamenjuje prethodne granice.
/// </summary>
public interface ITeamSizeLimitsRepository
{
    Task SaveAsync(
        Guid projectActivityId,
        TeamSizeLimits limits,
        CancellationToken cancellationToken = default);

    Task<TeamSizeLimits?> GetAsync(
        Guid projectActivityId,
        CancellationToken cancellationToken = default);
}
