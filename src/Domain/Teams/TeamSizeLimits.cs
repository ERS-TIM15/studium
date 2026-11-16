namespace Domain.Teams;

/// <summary>
/// Najmanja i najveća veličina studentskog tima koju nastavnik određuje za
/// projektnu aktivnost (ST-021). Neispravne vrednosti se ne signaliziraju
/// izuzetkom, već rezultatom <see cref="TeamSizeLimitsCreation"/>.
/// </summary>
public sealed record TeamSizeLimits
{
    private TeamSizeLimits(int minSize, int maxSize)
    {
        MinSize = minSize;
        MaxSize = maxSize;
    }

    public int MinSize { get; }

    public int MaxSize { get; }

    public static TeamSizeLimitsCreation TryCreate(int minSize, int maxSize)
    {
        if (minSize < 1)
        {
            return TeamSizeLimitsCreation.Failure("Najmanji broj članova tima mora biti najmanje 1");
        }

        if (maxSize < minSize)
        {
            return TeamSizeLimitsCreation.Failure("Najveći broj članova tima ne sme biti manji od najmanjeg");
        }

        return TeamSizeLimitsCreation.Success(new TeamSizeLimits(minSize, maxSize));
    }
}
