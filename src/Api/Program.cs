using System;
using Api.Authentication;
using Api.Contracts;
using Application.Activities;
using Application.Courses;
using Application.Interfaces;
using Application.Services;
using Application.Teams;
using Infrastructure.Activities;
using Infrastructure.Demo;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ICourseReader>(_ => DemoData.CreateCourseReader());
builder.Services.AddScoped<ListCoursesHandler>();
builder.Services.AddScoped<SearchCoursesHandler>();
builder.Services.AddSingleton<IEnrollmentRepository>(
    _ => new EnrollmentRepository(DemoData.CreateEnrollments()));
builder.Services.AddSingleton<IOfferingAccessReader>(_ => DemoData.CreateOfferingAccessReader());
builder.Services.AddScoped<EnrollmentService>();


builder.Services
    .AddAuthentication(DemoAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DemoAuthHandler>(DemoAuthHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

// Memorijski adapter mora biti singleton da bi aktivnosti preživele između zahteva.
builder.Services.AddSingleton<IActivityRepository>(_ => DemoData.CreateActivityRepository());
builder.Services.AddSingleton<IActivityContextReader, InMemoryActivityContextReader>();
builder.Services.AddScoped<CreateActivityHandler>();
builder.Services.AddScoped<ListActivitiesHandler>();

// Memorijski adapter mora biti singleton da bi izmene timova preživele između zahteva.
builder.Services.AddSingleton<ITeamRepository>(_ => DemoData.CreateTeamRepository());
builder.Services.AddScoped<SetTeamInviteOnlyHandler>();
builder.Services.AddScoped<InviteStudentToTeamHandler>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


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

// Demonstracioni tok aktivnosti: memorijski adapter i bez autentifikacije.
// Ne predstavlja završenu funkcionalnost ST-021 niti proveru prava nad izvođenjem.
// Oznaka konteksta je oblika OZNAKA-xxxxx, npr. ERS-11111; razrešava je
// IActivityContextReader, pa se kasnije može zameniti oznakom izvođenja predmeta.
app.MapPost("/Course/{contextSlug}/CreateActivity", async (
    string contextSlug,
    CreateActivityRequest request,
    CreateActivityHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        new CreateActivityCommand(
            contextSlug,
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate),
        cancellationToken);

    return result.Status switch
    {
        CreateActivityStatus.Created => Results.Json(
            result.Activity,
            statusCode: StatusCodes.Status201Created),
        CreateActivityStatus.ContextNotFound => Results.Problem(
            title: "Predmet ili izvođenje nije pronađeno",
            detail: result.Error,
            statusCode: StatusCodes.Status404NotFound),
        CreateActivityStatus.InvalidRequest => Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["activity"] = new[] { result.Error ?? "Nevažeći podaci aktivnosti." }
            }),
        CreateActivityStatus.Conflict => Results.Problem(
            title: "Aktivnost već postoji",
            detail: result.Error,
            statusCode: StatusCodes.Status409Conflict),
        CreateActivityStatus.Canceled => Results.StatusCode(StatusCodes.Status499ClientClosedRequest),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.MapGet("/ListActivities", async (
    ListActivitiesHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(cancellationToken);

    return result.IsSuccess
        ? Results.Ok(result.Activities)
        : Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
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
        SetTeamInviteOnlyStatus.Conflict => Results.Problem(
            title: "Tim je u međuvremenu izmenjen",
            detail: result.Error,
            statusCode: StatusCodes.Status409Conflict),
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
        InviteStudentToTeamStatus.Conflict => Results.Problem(
            title: "Tim je u međuvremenu izmenjen",
            detail: result.Error,
            statusCode: StatusCodes.Status409Conflict),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.Run();

// Potrebno za WebApplicationFactory u integracionim testovima.
public partial class Program { }
