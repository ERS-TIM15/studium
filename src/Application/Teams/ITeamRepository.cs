using Studium.Domain.Entities;

namespace Application.Teams;

/// <summary>
/// Port za čitanje i čuvanje studentskih timova. Memorijski adapter je demonstracioni;
/// trajno skladište treba da zameni implementaciju bez izmene slučajeva upotrebe.
/// </summary>
public interface ITeamRepository
{
    Task<StudentTeam?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default);

    Task UpdateAsync(StudentTeam team, CancellationToken cancellationToken = default);
}
