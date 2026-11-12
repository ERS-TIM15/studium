using System.Collections.Concurrent;
using Application.Teams;
using Studium.Domain.Entities;

namespace Infrastructure.Teams;

/// <summary>
/// Memorijski adapter za demonstraciju; nema trajnog čuvanja podataka.
/// </summary>
public sealed class InMemoryTeamRepository : ITeamRepository
{
    private readonly ConcurrentDictionary<Guid, StudentTeam> _teams;

    public InMemoryTeamRepository(IEnumerable<StudentTeam> teams)
    {
        _teams = new ConcurrentDictionary<Guid, StudentTeam>(
            teams.Select(team => new KeyValuePair<Guid, StudentTeam>(team.Id, team)));
    }

    public Task<StudentTeam?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _teams.TryGetValue(teamId, out var team);
        return Task.FromResult(team);
    }

    public Task UpdateAsync(StudentTeam team, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _teams[team.Id] = team;
        return Task.CompletedTask;
    }
}
