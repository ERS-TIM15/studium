using System.Net;
using System.Net.Http.Json;
using Application.Teams;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Studium.Domain.Entities;

namespace Tests.Integration;

/// <summary>
/// ST-021: HTTP sloj rute PATCH /teams/{teamId}/lock. Testovi pokrivaju uspešno
/// zaključavanje, ponovni poziv nad zaključanim timom i nepostojeći tim, kao i da
/// zaključavanje ne menja ostala svojstva tima.
/// </summary>
[TestFixture]
public sealed class TeamLockEndpointTests
{
    private static readonly string TeamPath = $"/teams/{DemoData.DemoTeamId}/lock";

    [Test]
    public async Task PatchLock_ForOpenTeam_ReturnsOkAndStoresLockedState()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PatchAsync(TeamPath, content: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await response.Content.ReadFromJsonAsync<LockResponse>();
        Assert.That(body!.Status, Is.EqualTo("Locked"));

        var stored = await GetStoredTeamAsync(factory);
        Assert.That(stored.Status, Is.EqualTo(TeamCompositionStatus.Locked));
    }

    [Test]
    public async Task PatchLock_ForAlreadyLockedTeam_ReturnsConflictAndKeepsTeamLocked()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var first = await client.PatchAsync(TeamPath, content: null);
        var second = await client.PatchAsync(TeamPath, content: null);
        var stored = await GetStoredTeamAsync(factory);

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(stored.Status, Is.EqualTo(TeamCompositionStatus.Locked));
        });
    }

    [Test]
    public async Task PatchLock_ForUnknownTeam_ReturnsNotFound()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PatchAsync($"/teams/{Guid.NewGuid()}/lock", content: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PatchLock_KeepsInviteOnlySetting()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var repository = GetRepository(factory);
        var team = (await repository.GetByIdAsync(DemoData.DemoTeamId))!;
        team.SetInviteOnly(true);
        await repository.UpdateAsync(team);

        var response = await client.PatchAsync(TeamPath, content: null);

        var stored = await GetStoredTeamAsync(factory);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(stored.Status, Is.EqualTo(TeamCompositionStatus.Locked));
            Assert.That(stored.IsInviteOnly, Is.True);
        });
    }

    private static ITeamRepository GetRepository(WebApplicationFactory<Program> factory) =>
        factory.Services.GetRequiredService<ITeamRepository>();

    private static async Task<StudentTeam> GetStoredTeamAsync(WebApplicationFactory<Program> factory)
    {
        var team = await GetRepository(factory).GetByIdAsync(DemoData.DemoTeamId);
        return team!;
    }

    private sealed record LockResponse(Guid TeamId, string Status);
}
