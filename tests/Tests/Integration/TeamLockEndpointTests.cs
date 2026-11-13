using System.Net;
using Interface.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Studium.Domain.Entities;

namespace Tests.Integration;

[TestFixture]
public sealed class TeamLockEndpointTests
{
    [Test]
    public async Task PatchLock_ForExistingOpenTeam_ReturnsOkAndPersistsLockedState()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var repository = factory.Services.GetRequiredService<ITeamRepository>();
        var team = CreateTeam();
        var added = await repository.AddAsync(team);

        var response = await client.PatchAsync($"/api/Team/{team.Id}/lock", content: null);
        var storedTeam = await repository.GetByIdAsync(team.Id);

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.True);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(storedTeam!.Status, Is.EqualTo(TeamCompositionStatus.Locked));
        });
    }

    [Test]
    public async Task PatchLock_ForUnknownTeam_ReturnsBadRequest()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PatchAsync($"/api/Team/{Guid.NewGuid()}/lock", content: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task PatchLock_ForAlreadyLockedTeam_ReturnsBadRequestAndKeepsTeamLocked()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var repository = factory.Services.GetRequiredService<ITeamRepository>();
        var team = CreateTeam();
        team.LockComposition();
        await repository.AddAsync(team);

        var response = await client.PatchAsync($"/api/Team/{team.Id}/lock", content: null);
        var storedTeam = await repository.GetByIdAsync(team.Id);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(storedTeam!.Status, Is.EqualTo(TeamCompositionStatus.Locked));
        });
    }

    private static StudentTeam CreateTeam() =>
        StudentTeam.TryCreate(Guid.NewGuid(), Guid.NewGuid(), "Tim A", Guid.NewGuid(), 2, 5).Team!;
}
