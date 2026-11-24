namespace Application.Teams;

/// <summary>
/// ST-021 (kontrola pristupa): predstavnik tima zatvara tim samo za pozvane studente
/// ili ga ponovo otvara. Odbijena operacija ne menja tim, a izmena koju je neko drugi
/// napravio u međuvremenu se prijavljuje kao konflikt, bez prepisivanja tuđeg stanja.
/// </summary>
public sealed class SetTeamInviteOnlyHandler
{
    private readonly ITeamRepository _repository;

    public SetTeamInviteOnlyHandler(ITeamRepository repository)
    {
        _repository = repository;
    }

    public async Task<SetTeamInviteOnlyResult> HandleAsync(
        SetTeamInviteOnlyCommand command,
        CancellationToken cancellationToken = default)
    {
        var team = await _repository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
        {
            return SetTeamInviteOnlyResult.TeamNotFound();
        }

        if (!team.IsRepresentative(command.RequestingStudentId))
        {
            return SetTeamInviteOnlyResult.Forbidden();
        }

        if (team.IsInviteOnly == command.InviteOnly)
        {
            return SetTeamInviteOnlyResult.Updated(team.IsInviteOnly);
        }

        team.SetInviteOnly(command.InviteOnly);

        var update = await _repository.UpdateAsync(team, cancellationToken);

        return update.Status switch
        {
            TeamUpdateStatus.Updated => SetTeamInviteOnlyResult.Updated(team.IsInviteOnly),
            TeamUpdateStatus.TeamNotFound => SetTeamInviteOnlyResult.TeamNotFound(),
            _ => SetTeamInviteOnlyResult.Conflict()
        };
    }
}
