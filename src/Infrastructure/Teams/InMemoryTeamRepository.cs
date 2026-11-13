using System.Collections.Concurrent;
using Interface.Repositories;
using Studium.Domain.Entities;

namespace Infrastructure.Teams;

/// <summary>
/// Demonstracioni memorijski repozitorijum; podaci se gube pri ponovnom pokretanju.
/// </summary>
public sealed class InMemoryTeamRepository : ITeamRepository
{
    private readonly ConcurrentDictionary<Guid, StudentTeam> _teams = new();

    public Task<bool> AddAsync(StudentTeam team)
    {
        ArgumentNullException.ThrowIfNull(team);
        return Task.FromResult(_teams.TryAdd(team.Id, team));
    }

    public Task<StudentTeam?> GetByIdAsync(Guid id)
    {
        _teams.TryGetValue(id, out var team);
        return Task.FromResult(team);
    }

    public Task UpdateAsync(StudentTeam team)
    {
        ArgumentNullException.ThrowIfNull(team);

        if (!_teams.ContainsKey(team.Id))
        {
            throw new InvalidOperationException($"Tim sa ID-em '{team.Id}' nije pronađen.");
        }

        _teams[team.Id] = team;
        return Task.CompletedTask;
    }
}
