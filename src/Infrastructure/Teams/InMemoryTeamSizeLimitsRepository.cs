using System.Collections.Concurrent;
using Application.Teams;
using Domain.Teams;

namespace Infrastructure.Teams;

/// <summary>
/// Memorijski adapter samo za demonstraciju; podaci se gube pri ponovnom
/// pokretanju procesa. Trajni adapter treba da implementira isti port.
/// </summary>
public sealed class InMemoryTeamSizeLimitsRepository : ITeamSizeLimitsRepository
{
    private readonly ConcurrentDictionary<Guid, TeamSizeLimits> _limits = new();

    public Task SaveAsync(
        Guid projectActivityId,
        TeamSizeLimits limits,
        CancellationToken cancellationToken = default)
    {
        _limits[projectActivityId] = limits;
        return Task.CompletedTask;
    }

    public Task<TeamSizeLimits?> GetAsync(
        Guid projectActivityId,
        CancellationToken cancellationToken = default)
    {
        _limits.TryGetValue(projectActivityId, out var limits);
        return Task.FromResult(limits);
    }
}
