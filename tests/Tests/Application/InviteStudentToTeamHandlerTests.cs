using Application.Teams;
using Moq;
using NUnit.Framework;
using Studium.Domain.Entities;

namespace Tests.Application;

[TestFixture]
public sealed class InviteStudentToTeamHandlerTests
{
    private static readonly Guid Creator = Guid.NewGuid();
    private static readonly Guid Member = Guid.NewGuid();

    private StudentTeam _team = null!;
    private Mock<ITeamRepository> _repository = null!;
    private InviteStudentToTeamHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _team = StudentTeam.TryCreate(Guid.NewGuid(), Guid.NewGuid(), "Tim A", Creator, 2, 5).Team!;
        _team.MemberStudentIds.Add(Member);

        _repository = new Mock<ITeamRepository>();
        _repository.Setup(x => x.GetByIdAsync(_team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_team);
        _repository.Setup(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new InviteStudentToTeamHandler(_repository.Object);
    }

    [Test]
    public async Task HandleAsync_MemberSuccessfullyInvitesAnotherStudent()
    {
        var candidate = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new InviteStudentToTeamCommand(_team.Id, Member, candidate));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InviteStudentToTeamStatus.Success));
            Assert.That(result.InvitedStudentId, Is.EqualTo(candidate));
            Assert.That(_team.PendingInvitationStudentIds, Contains.Item(candidate));
        });
        _repository.Verify(x => x.UpdateAsync(_team, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_RepresentativeCanAlsoInvite()
    {
        var candidate = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new InviteStudentToTeamCommand(_team.Id, Creator, candidate));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InviteStudentToTeamStatus.Success));
            Assert.That(_team.PendingInvitationStudentIds, Contains.Item(candidate));
        });
        _repository.Verify(x => x.UpdateAsync(_team, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_RejectsNonMember()
    {
        var stranger = Guid.NewGuid();
        var candidate = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new InviteStudentToTeamCommand(_team.Id, stranger, candidate));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InviteStudentToTeamStatus.Forbidden));
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
            Assert.That(_team.PendingInvitationStudentIds, Is.Empty);
        });
        _repository.Verify(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_RejectsEmptyRequesterId()
    {
        var candidate = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new InviteStudentToTeamCommand(_team.Id, Guid.Empty, candidate));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InviteStudentToTeamStatus.Forbidden));
            Assert.That(_team.PendingInvitationStudentIds, Is.Empty);
        });
        _repository.Verify(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsNotFoundForUnknownTeam()
    {
        var candidate = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new InviteStudentToTeamCommand(Guid.NewGuid(), Member, candidate));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InviteStudentToTeamStatus.TeamNotFound));
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
        });
        _repository.Verify(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_RejectsEmptyInvitedStudentId()
    {
        var result = await _handler.HandleAsync(
            new InviteStudentToTeamCommand(_team.Id, Member, Guid.Empty));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InviteStudentToTeamStatus.InvalidStudent));
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
            Assert.That(_team.PendingInvitationStudentIds, Is.Empty);
        });
        _repository.Verify(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_RejectsStudentInvitingThemselves()
    {
        var result = await _handler.HandleAsync(
            new InviteStudentToTeamCommand(_team.Id, Member, Member));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InviteStudentToTeamStatus.InvalidStudent));
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
            Assert.That(_team.PendingInvitationStudentIds, Is.Empty);
        });
        _repository.Verify(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_RejectsAlreadyMember()
    {
        var result = await _handler.HandleAsync(
            new InviteStudentToTeamCommand(_team.Id, Member, Creator));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InviteStudentToTeamStatus.AlreadyMember));
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
            Assert.That(_team.PendingInvitationStudentIds, Is.Empty);
        });
        _repository.Verify(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_RejectsAlreadyInvitedStudent()
    {
        var candidate = Guid.NewGuid();
        _team.InviteStudent(candidate);

        var result = await _handler.HandleAsync(
            new InviteStudentToTeamCommand(_team.Id, Member, candidate));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InviteStudentToTeamStatus.AlreadyInvited));
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
            Assert.That(_team.PendingInvitationStudentIds.Count, Is.EqualTo(1));
        });
        _repository.Verify(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_RejectsWhenTeamIsLocked()
    {
        _team.Status = TeamCompositionStatus.Locked;
        var candidate = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new InviteStudentToTeamCommand(_team.Id, Member, candidate));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InviteStudentToTeamStatus.TeamLocked));
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
            Assert.That(_team.PendingInvitationStudentIds, Is.Empty);
        });
        _repository.Verify(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_RejectsWhenTeamIsFull()
    {
        _team.MaxSize = _team.MemberStudentIds.Count;
        var candidate = Guid.NewGuid();

        var result = await _handler.HandleAsync(
            new InviteStudentToTeamCommand(_team.Id, Member, candidate));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(InviteStudentToTeamStatus.TeamFull));
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
            Assert.That(_team.PendingInvitationStudentIds, Is.Empty);
        });
        _repository.Verify(x => x.UpdateAsync(It.IsAny<StudentTeam>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
