namespace Application.Activities;

/// <summary>
/// Zahtev za kreiranje aktivnosti u kontekstu koji razrešava
/// <see cref="IActivityContextReader"/>. <paramref name="RequestingUserId"/> potiče iz
/// prijave korisnika (sesije), nikada iz tela zahteva ili zaglavlja koje klijent bira.
/// </summary>
public sealed record CreateActivityCommand(
    string ContextSlug,
    string Name,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    Guid RequestingUserId);
