namespace Application.Teams;

public enum InviteStudentToTeamStatus
{
    Success,
    TeamNotFound,
    Forbidden,
    AlreadyMember,
    AlreadyInvited,
    TeamLocked,
    TeamFull,
    InvalidStudent
}

public sealed record InviteStudentToTeamResult(
    InviteStudentToTeamStatus Status,
    Guid? InvitedStudentId = null,
    string? Error = null)
{
    public bool IsSuccess => Status == InviteStudentToTeamStatus.Success;

    public static InviteStudentToTeamResult Success(Guid invitedStudentId) =>
        new(InviteStudentToTeamStatus.Success, invitedStudentId);

    public static InviteStudentToTeamResult TeamNotFound() =>
        new(InviteStudentToTeamStatus.TeamNotFound, Error: "Tim nije pronađen.");

    public static InviteStudentToTeamResult Forbidden() =>
        new(InviteStudentToTeamStatus.Forbidden,
            Error: "Samo član tima može da pozove drugog studenta u tim.");

    public static InviteStudentToTeamResult AlreadyMember() =>
        new(InviteStudentToTeamStatus.AlreadyMember,
            Error: "Student je već član ovog tima.");

    public static InviteStudentToTeamResult AlreadyInvited() =>
        new(InviteStudentToTeamStatus.AlreadyInvited,
            Error: "Student je već pozvan u ovaj tim.");

    public static InviteStudentToTeamResult TeamLocked() =>
        new(InviteStudentToTeamStatus.TeamLocked,
            Error: "Sastav tima je zaključan, pa se ne mogu slati novi pozivi.");

    public static InviteStudentToTeamResult TeamFull() =>
        new(InviteStudentToTeamStatus.TeamFull,
            Error: "Tim je već dostigao maksimalan broj članova.");

    public static InviteStudentToTeamResult InvalidStudent(string message = "Identifikator studenta nije validan.") =>
        new(InviteStudentToTeamStatus.InvalidStudent, Error: message);
}
