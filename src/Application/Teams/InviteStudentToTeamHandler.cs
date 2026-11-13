using Studium.Domain.Entities;

namespace Application.Teams;

/// <summary>
/// ST-021 (pozivanje u tim): student unutar jednog tima direktno poziva drugog studenta u svoj tim.
/// Odbijena operacija ne menja tim.
/// </summary>
public sealed class InviteStudentToTeamHandler
{
    private readonly ITeamRepository _repository;

    public InviteStudentToTeamHandler(ITeamRepository repository)
    {
        _repository = repository;
    }

    public async Task<InviteStudentToTeamResult> HandleAsync(
        InviteStudentToTeamCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.RequestingStudentId == Guid.Empty)
        {
            return InviteStudentToTeamResult.Forbidden();
        }

        if (command.InvitedStudentId == Guid.Empty)
        {
            return InviteStudentToTeamResult.InvalidStudent();
        }

        if (command.InvitedStudentId == command.RequestingStudentId)
        {
            return InviteStudentToTeamResult.InvalidStudent("Student ne može pozvati samog sebe.");
        }

        var team = await _repository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
        {
            return InviteStudentToTeamResult.TeamNotFound();
        }

        if (!team.IsMember(command.RequestingStudentId))
        {
            return InviteStudentToTeamResult.Forbidden();
        }

        if (team.Status == TeamCompositionStatus.Locked)
        {
            return InviteStudentToTeamResult.TeamLocked();
        }

        if (team.MemberStudentIds.Count >= team.MaxSize)
        {
            return InviteStudentToTeamResult.TeamFull();
        }

        if (team.IsMember(command.InvitedStudentId))
        {
            return InviteStudentToTeamResult.AlreadyMember();
        }

        if (team.HasPendingInvitation(command.InvitedStudentId))
        {
            return InviteStudentToTeamResult.AlreadyInvited();
        }

        var invited = team.InviteStudent(command.InvitedStudentId);
        if (!invited)
        {
            return InviteStudentToTeamResult.InvalidStudent("Slanje poziva nije uspelo.");
        }

        await _repository.UpdateAsync(team, cancellationToken);

        return InviteStudentToTeamResult.Success(command.InvitedStudentId);
    }
}
