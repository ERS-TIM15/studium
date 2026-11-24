namespace Application.Teams;

public enum TeamUpdateStatus
{
    Updated,
    Conflict,
    TeamNotFound
}

/// <summary>
/// Ishod upisa tima. <see cref="TeamUpdateStatus.Conflict"/> znači da je tim izmenjen
/// između čitanja i upisa, pa pozivalac mora ponovo da pročita stanje umesto da pregazi
/// tuđu izmenu.
/// </summary>
public sealed record TeamUpdateResult(TeamUpdateStatus Status)
{
    public bool IsSuccess => Status == TeamUpdateStatus.Updated;

    public static TeamUpdateResult Updated() =>
        new(TeamUpdateStatus.Updated);

    public static TeamUpdateResult Conflict() =>
        new(TeamUpdateStatus.Conflict);

    public static TeamUpdateResult TeamNotFound() =>
        new(TeamUpdateStatus.TeamNotFound);
}
