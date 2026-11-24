using System;
using Api.Authentication;
using Api.Contracts;
using Api.Materials;
using Application.Activities;
using Application.Courses;
using Application.Interfaces;
using Application.Materials;
using Application.Services;
using Application.Teams;
using Infrastructure.Activities;
using Infrastructure.Demo;
using Infrastructure.Materials;
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
builder.Services.AddScoped<LockTeamHandler>();

// ST-009: memorijsko skladište materijala do odluke o bazi (ADR).
builder.Services.AddSingleton<IMaterialRepository, InMemoryMaterialRepository>();
builder.Services.AddScoped<CreateMaterialHandler>();
// ST-005 još nije integrisan: privremeni adapter bez angažovanja odbija sve (403).
builder.Services.AddSingleton<IOfferingStaffChecker>(_ => new InMemoryOfferingStaffChecker());

// ST-009 (US-05): dozvoljeni tipovi i maksimalna veličina priloga iz konfiguracije.
// Nedostajuća ili neispravna konfiguracija zaustavlja pokretanje umesto da prihvati sve.
var attachmentSection = builder.Configuration.GetSection("MaterialAttachments");
builder.Services.AddSingleton(new AttachmentUploadPolicy(
    attachmentSection.GetSection("AllowedExtensions").Get<string[]>() ?? [],
    attachmentSection.GetValue<long>("MaxSizeInBytes")));

// ST-009 (US-04): skladište sadržaja priloga na disku (ADR-0001) i podaci o prilozima.
// Relativna putanja se računa od korena aplikacije; bez konfiguracije pokretanje se zaustavlja.
var storageRootPath = attachmentSection.GetValue<string>("StorageRootPath");
if (string.IsNullOrWhiteSpace(storageRootPath))
{
    throw new InvalidOperationException(
        "Nije zadat 'MaterialAttachments:StorageRootPath' (direktorijum za priloge).");
}
builder.Services.AddSingleton<IAttachmentFileStorage>(new LocalDiskAttachmentFileStorage(
    Path.Combine(builder.Environment.ContentRootPath, storageRootPath)));
builder.Services.AddSingleton<IMaterialAttachmentRepository, InMemoryMaterialAttachmentRepository>();
builder.Services.AddScoped<AttachFileToMaterialHandler>();
builder.Services.AddScoped<UploadMaterialAttachmentsHandler>();
builder.Services.AddScoped<RemoveMaterialAttachmentHandler>();

// ST-009 (US-03): memorijsko skladište spoljnih veza materijala do odluke o bazi (ADR-0003).
// HTTP endpoint za dodavanje veze još nije dodat.
builder.Services.AddSingleton<IMaterialExternalLinkRepository, InMemoryMaterialExternalLinkRepository>();
builder.Services.AddScoped<AddMaterialExternalLinkHandler>();

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

// ST-021: zaključavanje sastava formiranog studentskog tima. Ruta još ne proverava
// nastavničko ovlašćenje jer projekat nema prijavu korisnika (ST-002); podaci su u memoriji.
app.MapPatch("/teams/{teamId:guid}/lock", async (
    Guid teamId,
    LockTeamHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new LockTeamCommand(teamId), cancellationToken);

    return result.Status switch
    {
        LockTeamStatus.Locked => Results.Ok(new { teamId, status = "Locked" }),
        LockTeamStatus.TeamNotFound => Results.Problem(
            title: "Tim nije pronađen",
            detail: result.Error,
            statusCode: StatusCodes.Status404NotFound),
        LockTeamStatus.AlreadyLocked => Results.Problem(
            title: "Sastav tima je već zaključan",
            detail: result.Error,
            statusCode: StatusCodes.Status409Conflict),
        LockTeamStatus.Conflict => Results.Problem(
            title: "Tim je u međuvremenu izmenjen",
            detail: result.Error,
            statusCode: StatusCodes.Status409Conflict),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
});

// ST-009: endpointi za nastavne materijale i priloge (src/Api/Materials/MaterialEndpoints.cs).
app.MapMaterialEndpoints();

app.Run();

// Potrebno za WebApplicationFactory u integracionim testovima.
public partial class Program { }
