using System.Collections.Concurrent;
using Application.Teams;
using Studium.Domain.Entities;

namespace Infrastructure.Teams;

/// <summary>
/// Memorijski adapter za demonstraciju; nema trajnog čuvanja podataka.
/// Čuva i vraća snimke, pa pozivaoci ne dele isti objekat sa skladištem. Upis je
/// upoređivanje-i-zamena po verziji: ako je tim izmenjen od čitanja, vraća se konflikt.
/// </summary>
public sealed class InMemoryTeamRepository : ITeamRepository
{
    private readonly ConcurrentDictionary<Guid, StudentTeam> _teams;

    public InMemoryTeamRepository(IEnumerable<StudentTeam> teams)
    {
        _teams = new ConcurrentDictionary<Guid, StudentTeam>(
            teams.Select(team => new KeyValuePair<Guid, StudentTeam>(team.Id, team.Copy())));
    }

    public Task<StudentTeam?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            _teams.TryGetValue(teamId, out var team) ? team.Copy() : null);
    }

    public Task<TeamUpdateResult> UpdateAsync(
        StudentTeam team,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        while (true)
        {
            if (!_teams.TryGetValue(team.Id, out var current))
            {
                return Task.FromResult(TeamUpdateResult.TeamNotFound());
            }

            if (current.Version != team.Version)
            {
                return Task.FromResult(TeamUpdateResult.Conflict());
            }

            var snapshot = team.Copy();
            snapshot.MarkStored(current.Version + 1);

            if (_teams.TryUpdate(team.Id, snapshot, current))
            {
                return Task.FromResult(TeamUpdateResult.Updated());
            }
        }
    }
}
