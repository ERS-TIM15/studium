using Infrastructure.Teams;
using NUnit.Framework;
using Studium.Domain.Entities;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryTeamRepositoryTests
{
    [Test]
    public async Task GetByIdAsync_ReturnsSeededTeam()
    {
        var team = CreateTeam();
        var repository = new InMemoryTeamRepository(new[] { team });

        var result = await repository.GetByIdAsync(team.Id);

        Assert.That(result?.Id, Is.EqualTo(team.Id));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNullForUnknownTeam()
    {
        var repository = new InMemoryTeamRepository(Array.Empty<StudentTeam>());

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_KeepsChangedInviteOnlySetting()
    {
        var team = CreateTeam();
        var repository = new InMemoryTeamRepository(new[] { team });
        var updated = CreateTeam();
        updated.Id = team.Id;
        updated.SetInviteOnly(true);

        await repository.UpdateAsync(updated);
        var result = await repository.GetByIdAsync(team.Id);

        Assert.That(result!.IsInviteOnly, Is.True);
    }

    private static StudentTeam CreateTeam() =>
        StudentTeam.TryCreate(Guid.NewGuid(), Guid.NewGuid(), "Tim A", Guid.NewGuid(), 2, 5).Team!;
}
