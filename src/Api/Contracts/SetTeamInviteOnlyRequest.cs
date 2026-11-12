namespace Api.Contracts;

/// <summary>
/// Telo zahteva: <c>true</c> zatvara tim samo za pozvane studente, <c>false</c> ga ponovo otvara.
/// Vrednost je obavezna, da izostavljeno polje ne bi slučajno otvorilo tim.
/// </summary>
public sealed record SetTeamInviteOnlyRequest(bool? InviteOnly);
