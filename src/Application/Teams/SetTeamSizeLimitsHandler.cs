using Domain.Teams;

namespace Application.Teams;

/// <summary>
/// ST-021: nastavnik određuje najmanju i najveću veličinu tima za projektnu
/// aktivnost. Ovlašćenje se proverava pre svega ostalog; odbijen zahtev ne
/// menja postojeće granice. Razlog neuspeha vraća se kao status, bez izuzetaka.
/// </summary>
public sealed class SetTeamSizeLimitsHandler
{
    private readonly ITeamSizeLimitsRepository _repository;
    private readonly ITeacherAccessChecker _accessChecker;

    public SetTeamSizeLimitsHandler(
        ITeamSizeLimitsRepository repository,
        ITeacherAccessChecker accessChecker)
    {
        _repository = repository;
        _accessChecker = accessChecker;
    }

    public async Task<SetTeamSizeLimitsResult> HandleAsync(
        SetTeamSizeLimitsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return SetTeamSizeLimitsResult.Canceled();
        }

        var access = await _accessChecker.CheckAsync(
            command.TeacherId,
            command.ProjectActivityId,
            cancellationToken);

        switch (access)
        {
            case TeacherAccess.ActivityNotFound:
                return SetTeamSizeLimitsResult.ActivityNotFound();
            case TeacherAccess.Allowed:
                break;
            default:
                return SetTeamSizeLimitsResult.Forbidden();
        }

        var creation = TeamSizeLimits.TryCreate(command.MinSize, command.MaxSize);

        if (creation.Limits is not { } limits)
        {
            return SetTeamSizeLimitsResult.InvalidRequest(
                creation.Error ?? "Nevažeće granice veličine tima.");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return SetTeamSizeLimitsResult.Canceled();
        }

        await _repository.SaveAsync(command.ProjectActivityId, limits, cancellationToken);

        return SetTeamSizeLimitsResult.Saved(limits);
    }
}
