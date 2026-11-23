namespace Application.Teams;

/// <summary>
/// Zahtev predstavnika tima da tim zatvori samo za pozvane studente ili ga ponovo otvori.
/// <paramref name="RequestingStudentId"/> mora poticati iz prijave korisnika, a ne iz slobodnog unosa.
/// </summary>
public sealed record SetTeamInviteOnlyCommand(
    Guid TeamId,
    Guid RequestingStudentId,
    bool InviteOnly);
