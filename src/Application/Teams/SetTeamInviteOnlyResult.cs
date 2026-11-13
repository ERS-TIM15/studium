namespace Application.Teams;

public enum SetTeamInviteOnlyStatus
{
    Updated,
    TeamNotFound,
    Forbidden
}

public sealed record SetTeamInviteOnlyResult(
    SetTeamInviteOnlyStatus Status,
    bool InviteOnly = false,
    string? Error = null)
{
    public bool IsSuccess => Status == SetTeamInviteOnlyStatus.Updated;

    public static SetTeamInviteOnlyResult Updated(bool inviteOnly) =>
        new(SetTeamInviteOnlyStatus.Updated, inviteOnly);

    public static SetTeamInviteOnlyResult TeamNotFound() =>
        new(SetTeamInviteOnlyStatus.TeamNotFound, Error: "Tim nije pronađen.");

    public static SetTeamInviteOnlyResult Forbidden() =>
        new(SetTeamInviteOnlyStatus.Forbidden,
            Error: "Samo predstavnik tima može da promeni ko može da pristupi timu.");
}
