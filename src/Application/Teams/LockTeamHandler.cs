namespace Application.Teams;

/// <summary>
/// ST-021: zaključavanje sastava formiranog studentskog tima. Zaključan tim ostaje
/// zaključan, a izmena koju je neko drugi napravio u međuvremenu se prijavljuje kao
/// konflikt, bez prepisivanja tuđeg stanja.
/// </summary>
public sealed class LockTeamHandler
{
    private readonly ITeamRepository _repository;

    public LockTeamHandler(ITeamRepository repository)
    {
        _repository = repository;
    }

    public async Task<LockTeamResult> HandleAsync(
        LockTeamCommand command,
        CancellationToken cancellationToken = default)
    {
        var team = await _repository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
        {
            return LockTeamResult.TeamNotFound();
        }

        if (!team.LockComposition())
        {
            return LockTeamResult.AlreadyLocked();
        }

        var update = await _repository.UpdateAsync(team, cancellationToken);

        return update.Status switch
        {
            TeamUpdateStatus.Updated => LockTeamResult.Locked(),
            TeamUpdateStatus.TeamNotFound => LockTeamResult.TeamNotFound(),
            _ => LockTeamResult.Conflict()
        };
    }
}
