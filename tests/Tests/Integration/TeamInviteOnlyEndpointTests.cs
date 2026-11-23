using System.Net;
using System.Net.Http.Json;
using System.Text;
using Application.Teams;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Studium.Domain.Entities;

namespace Tests.Integration;

/// <summary>
/// ST-021 (kontrola pristupa): HTTP sloj rute PUT /teams/{teamId}/invite-only.
/// Testovi pokrivaju JSON vezivanje tela, zaglavlje X-Student-Id, DI registraciju i
/// mapiranja 200/400/401/403/404, kao i da odbijeni zahtevi ne menjaju demo tim.
/// </summary>
[TestFixture]
public sealed class TeamInviteOnlyEndpointTests
{
    private static readonly string TeamPath = $"/teams/{DemoData.DemoTeamId}/invite-only";
    private static readonly Guid Representative = DemoData.DemoTeamRepresentativeId;
    private static readonly Guid OtherStudent = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Test]
    public async Task PutInviteOnly_RepresentativeClosesTeam_ReturnsOkAndStoresState()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await PutInviteOnlyAsync(client, Representative, inviteOnly: true);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<InviteOnlyResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(body!.TeamId, Is.EqualTo(DemoData.DemoTeamId));
            Assert.That(body.InviteOnly, Is.True);
        });
        Assert.That((await GetStoredTeamAsync(factory)).IsInviteOnly, Is.True);
    }

    [Test]
    public async Task PutInviteOnly_RepresentativeReopensTeam_ReturnsOkAndStoresState()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        await PutInviteOnlyAsync(client, Representative, inviteOnly: true);
        var response = await PutInviteOnlyAsync(client, Representative, inviteOnly: false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<InviteOnlyResponse>();
        Assert.That(body!.InviteOnly, Is.False);
        Assert.That((await GetStoredTeamAsync(factory)).IsInviteOnly, Is.False);
    }

    [Test]
    public async Task PutInviteOnly_WithoutStudentHeader_ReturnsUnauthorizedAndLeavesTeamOpen()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await PutInviteOnlyAsync(client, studentId: null, inviteOnly: true);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await GetStoredTeamAsync(factory)).IsInviteOnly, Is.False);
    }

    [Test]
    public async Task PutInviteOnly_WithoutInviteOnlyField_ReturnsBadRequestAndLeavesTeamOpen()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await PutInviteOnlyAsync(client, Representative, json: "{}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await GetStoredTeamAsync(factory)).IsInviteOnly, Is.False);
    }

    [Test]
    public async Task PutInviteOnly_StudentWhoIsNotRepresentative_ReturnsForbiddenAndLeavesTeamOpen()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await PutInviteOnlyAsync(client, OtherStudent, inviteOnly: true);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await GetStoredTeamAsync(factory)).IsInviteOnly, Is.False);
    }

    [Test]
    public async Task PutInviteOnly_UnknownTeam_ReturnsNotFound()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.SendAsync(BuildRequest(
            $"/teams/{Guid.NewGuid()}/invite-only",
            Representative,
            "{\"inviteOnly\":true}"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private static Task<HttpResponseMessage> PutInviteOnlyAsync(
        HttpClient client,
        Guid? studentId,
        bool inviteOnly = true,
        string? json = null) =>
        client.SendAsync(BuildRequest(
            TeamPath,
            studentId,
            json ?? $"{{\"inviteOnly\":{inviteOnly.ToString().ToLowerInvariant()}}}"));

    private static HttpRequestMessage BuildRequest(string path, Guid? studentId, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (studentId is { } id)
        {
            request.Headers.Add("X-Student-Id", id.ToString());
        }

        return request;
    }

    private static async Task<StudentTeam> GetStoredTeamAsync(WebApplicationFactory<Program> factory)
    {
        var repository = factory.Services.GetRequiredService<ITeamRepository>();
        var team = await repository.GetByIdAsync(DemoData.DemoTeamId);
        return team!;
    }

    private sealed record InviteOnlyResponse(Guid TeamId, bool InviteOnly);
}
