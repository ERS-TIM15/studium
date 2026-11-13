namespace Application.Teams;

/// <summary>
/// Zahtev da se sastav formiranog tima zaključa. Provera nastavničkog ovlašćenja još
/// nije deo komande jer projekat nema prijavu korisnika (ST-002).
/// </summary>
public sealed record LockTeamCommand(Guid TeamId);
