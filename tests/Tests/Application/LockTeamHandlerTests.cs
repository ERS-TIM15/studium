using Application.Teams;
using Moq;
using NUnit.Framework;
using Studium.Domain.Entities;

namespace Tests.Application;

[TestFixture]
public sealed class LockTeamHandlerTests
{
    private StudentTeam _team = null!;
    private Mock<ITeamRepository> _repository = null!;
    private LockTeamHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _team = StudentTeam.TryCreate(Guid.NewGuid(), Guid.NewGuid(), "Tim A", Guid.NewGuid(), 2, 5).Team!;

        _repository = new Mock<ITeamRepository>();
        _repository.Setup(x => x.GetByIdAsync(_team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_team);
        _repository.Setup(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeamUpdateResult.Updated());

        _handler = new LockTeamHandler(_repository.Object);
    }

    [Test]
    public async Task HandleAsync_LocksOpenTeam()
    {
        var result = await _handler.HandleAsync(new LockTeamCommand(_team.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LockTeamStatus.Locked));
            Assert.That(_team.Status, Is.EqualTo(TeamCompositionStatus.Locked));
        });
        _repository.Verify(x => x.UpdateAsync(_team, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ReturnsAlreadyLockedWithoutWrite()
    {
        _team.LockComposition();

        var result = await _handler.HandleAsync(new LockTeamCommand(_team.Id));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LockTeamStatus.AlreadyLocked));
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
        });
        _repository.Verify(
            x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsNotFoundForUnknownTeam()
    {
        var result = await _handler.HandleAsync(new LockTeamCommand(Guid.NewGuid()));

        Assert.That(result.Status, Is.EqualTo(LockTeamStatus.TeamNotFound));
        _repository.Verify(
            x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReportsConflictWhenTeamChangedAfterRead()
    {
        _repository.Setup(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeamUpdateResult.Conflict());

        var result = await _handler.HandleAsync(new LockTeamCommand(_team.Id));

        Assert.That(result.Status, Is.EqualTo(LockTeamStatus.Conflict));
    }
}
