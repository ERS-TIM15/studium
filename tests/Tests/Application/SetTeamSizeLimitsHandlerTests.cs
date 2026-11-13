using Moq;
using NUnit.Framework;
using Application.Teams;
using Domain.Teams;

namespace Tests.Application;

[TestFixture]
public sealed class SetTeamSizeLimitsHandlerTests
{
    private static readonly Guid Teacher = Guid.NewGuid();
    private static readonly Guid Activity = Guid.NewGuid();

    private Mock<ITeamSizeLimitsRepository> _repository = null!;
    private Mock<ITeacherAccessChecker> _access = null!;
    private SetTeamSizeLimitsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<ITeamSizeLimitsRepository>();
        _access = new Mock<ITeacherAccessChecker>();
        _access.Setup(x => x.CheckAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeacherAccess.Allowed);
        _handler = new SetTeamSizeLimitsHandler(_repository.Object, _access.Object);
    }

    [Test]
    public async Task HandleAsync_SavesLimitsForAuthorizedTeacher()
    {
        var result = await _handler.HandleAsync(
            new SetTeamSizeLimitsCommand(Teacher, Activity, 4, 6));

        Assert.That(result.Status, Is.EqualTo(SetTeamSizeLimitsStatus.Saved));
        Assert.That(result.Limits!.MinSize, Is.EqualTo(4));
        Assert.That(result.Limits!.MaxSize, Is.EqualTo(6));
        _repository.Verify(x => x.SaveAsync(
            Activity,
            It.Is<TeamSizeLimits>(l => l.MinSize == 4 && l.MaxSize == 6),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(0, 5)]
    [TestCase(5, 4)]
    public async Task HandleAsync_RejectsInvalidLimitsWithoutSaving(int min, int max)
    {
        var result = await _handler.HandleAsync(
            new SetTeamSizeLimitsCommand(Teacher, Activity, min, max));

        Assert.That(result.Status, Is.EqualTo(SetTeamSizeLimitsStatus.InvalidRequest));
        Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
        _repository.Verify(x => x.SaveAsync(
            It.IsAny<Guid>(), It.IsAny<TeamSizeLimits>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_RejectsTeacherWithoutAccessWithoutSaving()
    {
        _access.Setup(x => x.CheckAsync(
                Teacher, Activity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeacherAccess.Denied);

        var result = await _handler.HandleAsync(
            new SetTeamSizeLimitsCommand(Teacher, Activity, 4, 6));

        Assert.That(result.Status, Is.EqualTo(SetTeamSizeLimitsStatus.Forbidden));
        _repository.Verify(x => x.SaveAsync(
            It.IsAny<Guid>(), It.IsAny<TeamSizeLimits>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ChecksAccessBeforeValidatingInput()
    {
        _access.Setup(x => x.CheckAsync(
                Teacher, Activity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeacherAccess.Denied);

        var result = await _handler.HandleAsync(
            new SetTeamSizeLimitsCommand(Teacher, Activity, 0, 0));

        Assert.That(result.Status, Is.EqualTo(SetTeamSizeLimitsStatus.Forbidden));
    }

    [Test]
    public async Task HandleAsync_ReportsMissingActivityWithoutSaving()
    {
        _access.Setup(x => x.CheckAsync(
                Teacher, Activity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeacherAccess.ActivityNotFound);

        var result = await _handler.HandleAsync(
            new SetTeamSizeLimitsCommand(Teacher, Activity, 4, 6));

        Assert.That(result.Status, Is.EqualTo(SetTeamSizeLimitsStatus.ActivityNotFound));
        _repository.Verify(x => x.SaveAsync(
            It.IsAny<Guid>(), It.IsAny<TeamSizeLimits>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_ReturnsCanceledWithoutSavingWhenAlreadyCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _handler.HandleAsync(
            new SetTeamSizeLimitsCommand(Teacher, Activity, 4, 6), cts.Token);

        Assert.That(result.Status, Is.EqualTo(SetTeamSizeLimitsStatus.Canceled));
        _repository.Verify(x => x.SaveAsync(
            It.IsAny<Guid>(), It.IsAny<TeamSizeLimits>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
