namespace Application.Teams;

/// <summary>
/// ST-021 (kontrola pristupa): predstavnik tima zatvara tim samo za pozvane studente
/// ili ga ponovo otvara. Odbijena operacija ne menja tim.
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

        team.SetInviteOnly(command.InviteOnly);
        await _repository.UpdateAsync(team, cancellationToken);

        return SetTeamInviteOnlyResult.Updated(team.IsInviteOnly);
    }
}
