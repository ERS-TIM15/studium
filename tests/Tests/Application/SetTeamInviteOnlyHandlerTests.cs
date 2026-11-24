using Application.Teams;
using Moq;
using NUnit.Framework;
using Studium.Domain.Entities;

namespace Tests.Application;

[TestFixture]
public sealed class SetTeamInviteOnlyHandlerTests
{
    private static readonly Guid Representative = Guid.NewGuid();

    private StudentTeam _team = null!;
    private Mock<ITeamRepository> _repository = null!;
    private SetTeamInviteOnlyHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _team = StudentTeam.TryCreate(Guid.NewGuid(), Guid.NewGuid(), "Tim A", Representative, 2, 5).Team!;

        _repository = new Mock<ITeamRepository>();
        _repository.Setup(x => x.GetByIdAsync(_team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_team);
        _repository.Setup(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeamUpdateResult.Updated());

        _handler = new SetTeamInviteOnlyHandler(_repository.Object);
    }

    [Test]
    public async Task HandleAsync_RepresentativeClosesTeam()
    {
        var result = await _handler.HandleAsync(
            new SetTeamInviteOnlyCommand(_team.Id, Representative, InviteOnly: true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SetTeamInviteOnlyStatus.Updated));
            Assert.That(result.InviteOnly, Is.True);
            Assert.That(_team.IsInviteOnly, Is.True);
        });
        _repository.Verify(x => x.UpdateAsync(_team, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_RepresentativeReopensTeam()
    {
        _team.SetInviteOnly(true);

        var result = await _handler.HandleAsync(
            new SetTeamInviteOnlyCommand(_team.Id, Representative, InviteOnly: false));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SetTeamInviteOnlyStatus.Updated));
            Assert.That(_team.IsInviteOnly, Is.False);
        });
    }

    [Test]
    public async Task HandleAsync_RepeatedRequestKeepsSameState()
    {
        var command = new SetTeamInviteOnlyCommand(_team.Id, Representative, InviteOnly: true);

        await _handler.HandleAsync(command);
        var result = await _handler.HandleAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SetTeamInviteOnlyStatus.Updated));
            Assert.That(_team.IsInviteOnly, Is.True);
        });
    }

    [Test]
    public async Task HandleAsync_RejectsStudentWhoIsNotRepresentative()
    {
        // Članstvo se zadaje samo pri kreiranju tima; tok prihvatanja poziva još nije
        // implementiran, pa se ovde proverava student koji nije predstavnik tima.
        var otherStudent = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new SetTeamInviteOnlyCommand(_team.Id, otherStudent, InviteOnly: true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SetTeamInviteOnlyStatus.Forbidden));
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
            Assert.That(_team.IsInviteOnly, Is.False);
        });
        _repository.Verify(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_RejectsEmptyRequesterId()
    {
        var result = await _handler.HandleAsync(
            new SetTeamInviteOnlyCommand(_team.Id, Guid.Empty, InviteOnly: true));

        Assert.That(result.Status, Is.EqualTo(SetTeamInviteOnlyStatus.Forbidden));
        Assert.That(_team.IsInviteOnly, Is.False);
    }

    [Test]
    public async Task HandleAsync_ReportsConflictWhenTeamChangedAfterRead()
    {
        _repository.Setup(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeamUpdateResult.Conflict());

        var result = await _handler.HandleAsync(
            new SetTeamInviteOnlyCommand(_team.Id, Representative, InviteOnly: true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SetTeamInviteOnlyStatus.Conflict));
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_SkipsWriteWhenRequestedStateAlreadyApplies()
    {
        _team.SetInviteOnly(true);

        var result = await _handler.HandleAsync(
            new SetTeamInviteOnlyCommand(_team.Id, Representative, InviteOnly: true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SetTeamInviteOnlyStatus.Updated));
            Assert.That(result.InviteOnly, Is.True);
        });
        _repository.Verify(
            x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsNotFoundWhenTeamDisappearsBeforeWrite()
    {
        _repository.Setup(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeamUpdateResult.TeamNotFound());

        var result = await _handler.HandleAsync(
            new SetTeamInviteOnlyCommand(_team.Id, Representative, InviteOnly: true));

        Assert.That(result.Status, Is.EqualTo(SetTeamInviteOnlyStatus.TeamNotFound));
    }

    [Test]
    public async Task HandleAsync_ReturnsNotFoundForUnknownTeam()
    {
        var result = await _handler.HandleAsync(
            new SetTeamInviteOnlyCommand(Guid.NewGuid(), Representative, InviteOnly: true));

        Assert.That(result.Status, Is.EqualTo(SetTeamInviteOnlyStatus.TeamNotFound));
        _repository.Verify(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
