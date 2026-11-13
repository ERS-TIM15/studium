namespace Application.Teams;

public sealed record SetTeamSizeLimitsCommand(
    Guid TeacherId,
    Guid ProjectActivityId,
    int MinSize,
    int MaxSize);
