namespace Application.Teams;

public enum LockTeamStatus
{
    Locked,
    TeamNotFound,
    AlreadyLocked,
    Conflict
}

/// <summary>
/// Ishod zaključavanja tima. Domen i slučaj upotrebe ne bacaju izuzetke: nepostojeći tim,
/// već zaključan tim i izmena koju je neko drugi napravio u međuvremenu su statusi.
/// </summary>
public sealed record LockTeamResult(LockTeamStatus Status, string? Error = null)
{
    public bool IsSuccess => Status == LockTeamStatus.Locked;

    public static LockTeamResult Locked() =>
        new(LockTeamStatus.Locked);

    public static LockTeamResult TeamNotFound() =>
        new(LockTeamStatus.TeamNotFound, "Tim nije pronađen.");

    public static LockTeamResult AlreadyLocked() =>
        new(LockTeamStatus.AlreadyLocked, "Sastav tima je već zaključan.");

    public static LockTeamResult Conflict() =>
        new(LockTeamStatus.Conflict, "Tim je u međuvremenu izmenjen. Pokušajte ponovo.");
}
