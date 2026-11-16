using Studium.Domain.Entities;

namespace Application.Teams;

/// <summary>
/// Port za čitanje i čuvanje studentskih timova. Memorijski adapter je demonstracioni;
/// trajno skladište treba da zameni implementaciju bez izmene slučajeva upotrebe.
/// Adapter vraća i čuva snimke (kopije), pa izmene nad pročitanim objektom ne menjaju
/// sačuvano stanje.
/// </summary>
public interface ITeamRepository
{
    Task<StudentTeam?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upisuje izmenu ako niko drugi nije promenio tim od trenutka čitanja.
    /// U suprotnom vraća <see cref="TeamUpdateStatus.Conflict"/> i ne menja sačuvano stanje.
    /// </summary>
    Task<TeamUpdateResult> UpdateAsync(StudentTeam team, CancellationToken cancellationToken = default);
}
