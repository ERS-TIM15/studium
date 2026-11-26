namespace Api.Contracts;

/// <summary>
/// Telo zahteva za slanje poziva studentu u tim.
/// </summary>
public sealed record InviteStudentRequest(Guid? StudentId);
