using Domain.Teams;

namespace Application.Teams;

public enum SetTeamSizeLimitsStatus
{
    Saved,
    InvalidRequest,
    Forbidden,
    ActivityNotFound,
    Canceled
}

public sealed record SetTeamSizeLimitsResult(
    SetTeamSizeLimitsStatus Status,
    TeamSizeLimits? Limits = null,
    string? Error = null)
{
    public bool IsSuccess => Status == SetTeamSizeLimitsStatus.Saved;

    public static SetTeamSizeLimitsResult Saved(TeamSizeLimits limits) =>
        new(SetTeamSizeLimitsStatus.Saved, limits);

    public static SetTeamSizeLimitsResult InvalidRequest(string error) =>
        new(SetTeamSizeLimitsStatus.InvalidRequest, Error: error);

    public static SetTeamSizeLimitsResult Forbidden() =>
        new(SetTeamSizeLimitsStatus.Forbidden,
            Error: "Nemate ovlašćenje da određujete veličinu tima za ovu aktivnost.");

    public static SetTeamSizeLimitsResult ActivityNotFound() =>
        new(SetTeamSizeLimitsStatus.ActivityNotFound,
            Error: "Projektna aktivnost ne postoji.");

    public static SetTeamSizeLimitsResult Canceled() =>
        new(SetTeamSizeLimitsStatus.Canceled,
            Error: "Zahtev je otkazan pre čuvanja granica veličine tima.");
}
