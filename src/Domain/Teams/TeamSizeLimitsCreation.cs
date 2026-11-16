namespace Domain.Teams;

/// <summary>
/// Rezultat kreiranja granica veličine tima. Domen ne baca izuzetke za
/// neispravne podatke, već vraća grešku koju sloj iznad prevodi u odgovor.
/// </summary>
public sealed record TeamSizeLimitsCreation(TeamSizeLimits? Limits, string? Error)
{
    public bool IsSuccess => Limits is not null;

    public static TeamSizeLimitsCreation Success(TeamSizeLimits limits) =>
        new(limits, null);

    public static TeamSizeLimitsCreation Failure(string error) =>
        new(null, error);
}
