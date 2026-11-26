namespace Application.Teams;

/// <summary>
/// Zahtev studenta koji je član tima da direktno pozove drugog studenta u svoj tim.
/// <paramref name="RequestingStudentId"/> mora poticati iz prijave korisnika, a ne iz slobodnog unosa.
/// </summary>
public sealed record InviteStudentToTeamCommand(
    Guid TeamId,
    Guid RequestingStudentId,
    Guid InvitedStudentId);
