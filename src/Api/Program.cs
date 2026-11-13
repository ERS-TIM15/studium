using Api.Contracts;
using Application.Courses;
using Application.Teams;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Composition root: spoljašnji sloj bira implementaciju ugovora.
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();

// Memorijski adapter mora biti singleton da bi izmene timova preživele između zahteva.
builder.Services.AddSingleton<ITeamRepository>(_ => DemoData.CreateTeamRepository());
builder.Services.AddScoped<SetTeamInviteOnlyHandler>();
builder.Services.AddScoped<InviteStudentToTeamHandler>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Javno dostupan demonstracioni endpoint, bez autentifikacije:
// ne predstavlja funkcionalnost ST-003 i ne koristiti ga za stvarne podatke.
app.MapGet("/demo/courses", async (
    ListCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(cancellationToken);
    return Results.Ok(courses);
});

app.MapGet("/courses", async (
    string? query,
    SearchCoursesHandler handler,
    CancellationToken cancellationToken) =>
{
    var courses = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(courses);
});

// ST-021 (kontrola pristupa): predstavnik tima zatvara tim samo za pozvane studente
// ili ga ponovo otvara.
// PRIVREMENO: dok ne postoji prijava korisnika (ST-002), identitet se čita iz zaglavlja
// X-Student-Id. To NIJE bezbednosna kontrola i mora se zameniti identitetom iz prijave.
app.MapPut("/teams/{teamId:guid}/invite-only", async (
    Guid teamId,
    [FromHeader(Name = "X-Student-Id")] Guid? studentId,
    SetTeamInviteOnlyRequest request,
    SetTeamInviteOnlyHandler handler,
    CancellationToken cancellationToken) =>
{
    if (studentId is null)
    {
        return Results.Problem(
            title: "Korisnik nije prijavljen",
            statusCode: StatusCodes.Status401Unauthorized);
    }

    if (request.InviteOnly is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["inviteOnly"] = new[] { "Vrednost inviteOnly je obavezna." }
        });
    }

    var result = await handler.HandleAsync(
        new SetTeamInviteOnlyCommand(teamId, studentId.Value, request.InviteOnly.Value),
        cancellationToken);

    return result.Status switch
    {
        SetTeamInviteOnlyStatus.Updated => Results.Ok(new { teamId, inviteOnly = result.InviteOnly }),
        SetTeamInviteOnlyStatus.TeamNotFound => Results.Problem(
            title: "Tim nije pronađen",
            detail: result.Error,
            statusCode: StatusCodes.Status404NotFound),
        SetTeamInviteOnlyStatus.Forbidden => Results.Problem(
            title: "Nedovoljno ovlašćenje",
            detail: result.Error,
            statusCode: StatusCodes.Status403Forbidden),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
});

// ST-021 (pozivanje u tim): student unutar tima poziva drugog studenta u svoj tim.
// PRIVREMENO: dok ne postoji prijava korisnika (ST-002), identitet se čita iz zaglavlja
// X-Student-Id. To NIJE bezbednosna kontrola i mora se zameniti identitetom iz prijave.
app.MapPost("/teams/{teamId:guid}/invitations", async (
    Guid teamId,
    [FromHeader(Name = "X-Student-Id")] Guid? studentId,
    InviteStudentRequest request,
    InviteStudentToTeamHandler handler,
    CancellationToken cancellationToken) =>
{
    if (studentId is null)
    {
        return Results.Problem(
            title: "Korisnik nije prijavljen",
            statusCode: StatusCodes.Status401Unauthorized);
    }

    if (request.StudentId is null || request.StudentId == Guid.Empty)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["studentId"] = new[] { "Identifikator studenta koji se poziva je obavezan." }
        });
    }

    var result = await handler.HandleAsync(
        new InviteStudentToTeamCommand(teamId, studentId.Value, request.StudentId.Value),
        cancellationToken);

    return result.Status switch
    {
        InviteStudentToTeamStatus.Success => Results.Ok(new { teamId, invitedStudentId = result.InvitedStudentId }),
        InviteStudentToTeamStatus.TeamNotFound => Results.Problem(
            title: "Tim nije pronađen",
            detail: result.Error,
            statusCode: StatusCodes.Status404NotFound),
        InviteStudentToTeamStatus.Forbidden => Results.Problem(
            title: "Nedovoljno ovlašćenje",
            detail: result.Error,
            statusCode: StatusCodes.Status403Forbidden),
        InviteStudentToTeamStatus.AlreadyMember or
        InviteStudentToTeamStatus.AlreadyInvited or
        InviteStudentToTeamStatus.TeamLocked or
        InviteStudentToTeamStatus.TeamFull or
        InviteStudentToTeamStatus.InvalidStudent => Results.Problem(
            title: "Neuspešno slanje poziva",
            detail: result.Error,
            statusCode: StatusCodes.Status409Conflict),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.Run();
