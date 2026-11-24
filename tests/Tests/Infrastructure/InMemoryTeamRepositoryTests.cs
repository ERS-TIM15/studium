using Application.Teams;
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
    public async Task GetByIdAsync_ReturnsSnapshotSoReaderCannotChangeStoredState()
    {
        var team = CreateTeam();
        var repository = new InMemoryTeamRepository(new[] { team });

        var firstRead = await repository.GetByIdAsync(team.Id);
        firstRead!.SetInviteOnly(true);

        var secondRead = await repository.GetByIdAsync(team.Id);

        Assert.Multiple(() =>
        {
            Assert.That(firstRead, Is.Not.SameAs(secondRead));
            Assert.That(firstRead.MemberStudentIds, Is.Not.SameAs(secondRead!.MemberStudentIds));
            Assert.That(secondRead.IsInviteOnly, Is.False);
        });
    }

    [Test]
    public async Task UpdateAsync_KeepsChangedInviteOnlySetting()
    {
        var team = CreateTeam();
        var repository = new InMemoryTeamRepository(new[] { team });
        var updated = CreateTeam(team.Id);
        updated.SetInviteOnly(true);

        var result = await repository.UpdateAsync(updated);
        var stored = await repository.GetByIdAsync(team.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(TeamUpdateStatus.Updated));
            Assert.That(stored!.IsInviteOnly, Is.True);
        });
    }

    [Test]
    public async Task UpdateAsync_StoresSnapshotSoLaterChangesDoNotLeak()
    {
        var team = CreateTeam();
        var repository = new InMemoryTeamRepository(new[] { team });
        var updated = CreateTeam(team.Id);
        updated.SetInviteOnly(true);

        await repository.UpdateAsync(updated);
        updated.SetInviteOnly(false);

        var stored = await repository.GetByIdAsync(team.Id);

        Assert.That(stored!.IsInviteOnly, Is.True);
    }

    [Test]
    public async Task UpdateAsync_ReturnsConflictWhenTeamChangedAfterRead()
    {
        var team = CreateTeam();
        var repository = new InMemoryTeamRepository(new[] { team });
        var firstRead = await repository.GetByIdAsync(team.Id);
        var staleRead = await repository.GetByIdAsync(team.Id);

        firstRead!.SetInviteOnly(true);
        var firstResult = await repository.UpdateAsync(firstRead);

        staleRead!.SetInviteOnly(false);
        var staleResult = await repository.UpdateAsync(staleRead);

        var stored = await repository.GetByIdAsync(team.Id);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Status, Is.EqualTo(TeamUpdateStatus.Updated));
            Assert.That(staleResult.Status, Is.EqualTo(TeamUpdateStatus.Conflict));
            Assert.That(stored!.IsInviteOnly, Is.True);
        });
    }

    [Test]
    public async Task UpdateAsync_ReturnsTeamNotFoundForUnknownTeam()
    {
        var repository = new InMemoryTeamRepository(Array.Empty<StudentTeam>());
        var team = CreateTeam();
        team.SetInviteOnly(true);

        var result = await repository.UpdateAsync(team);

        Assert.That(result.Status, Is.EqualTo(TeamUpdateStatus.TeamNotFound));
    }

    private static StudentTeam CreateTeam(Guid? teamId = null) =>
        StudentTeam.TryCreate(Guid.NewGuid(), Guid.NewGuid(), "Tim A", Guid.NewGuid(), 2, 5, teamId).Team!;
}
