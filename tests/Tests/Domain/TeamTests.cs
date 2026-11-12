using NUnit.Framework;
using Studium.Domain.Entities;

namespace Tests.Domain;

[TestFixture]
public sealed class TeamTests
{
    private static readonly Guid Offering = Guid.NewGuid();
    private static readonly Guid Activity = Guid.NewGuid();
    private static readonly Guid Creator = Guid.NewGuid();

    [Test]
    public void TryCreate_AssignsProperties()
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5);

        Assert.That(creation.IsSuccess, Is.True);
        var team = creation.Team!;
        Assert.Multiple(() =>
        {
            Assert.That(team.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(team.CourseOfferingId, Is.EqualTo(Offering));
            Assert.That(team.ProjectActivityId, Is.EqualTo(Activity));
            Assert.That(team.Name, Is.EqualTo("Tim A"));
            Assert.That(team.MinSize, Is.EqualTo(2));
            Assert.That(team.MaxSize, Is.EqualTo(5));
            Assert.That(team.MemberStudentIds, Is.EqualTo(new List<Guid> { Creator }));
            Assert.That(team.PendingInvitationStudentIds, Is.Empty);
            Assert.That(team.Status, Is.EqualTo(TeamCompositionStatus.Open));
        });
    }

    [Test]
    public void TryCreate_TrimsName()
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "  Tim A  ", Creator, 2, 5);

        Assert.That(creation.IsSuccess, Is.True);
        Assert.That(creation.Team!.Name, Is.EqualTo("Tim A"));
    }

    [Test]
    public void TryCreate_RejectsEmptyCourseOfferingId()
    {
        var creation = StudentTeam.TryCreate(Guid.Empty, Activity, "Tim A", Creator, 2, 5);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_RejectsEmptyProjectActivityId()
    {
        var creation = StudentTeam.TryCreate(Offering, Guid.Empty, "Tim A", Creator, 2, 5);

        AssertFailure(creation);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void TryCreate_RejectsBlankName(string? name)
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, name, Creator, 2, 5);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_RejectsEmptyCreator()
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "Tim A", Guid.Empty, 2, 5);

        AssertFailure(creation);
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void TryCreate_RejectsMinSizeBelowOne(int minSize)
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, minSize, 5);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_RejectsMaxSizeBelowMinSize()
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 4, 3);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_AcceptsMaxSizeEqualToMinSize()
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 3, 3);

        Assert.That(creation.IsSuccess, Is.True);
        Assert.That(creation.Team!.MinSize, Is.EqualTo(3));
        Assert.That(creation.Team.MaxSize, Is.EqualTo(3));
    }

    [Test]
    public void TryCreate_CreatorIsRepresentativeAndTeamIsOpen()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;

        Assert.Multiple(() =>
        {
            Assert.That(team.RepresentativeStudentId, Is.EqualTo(Creator));
            Assert.That(team.IsRepresentative(Creator), Is.True);
            Assert.That(team.IsInviteOnly, Is.False);
        });
    }

    [Test]
    public void IsRepresentative_RejectsOtherStudentAndEmptyId()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;

        Assert.Multiple(() =>
        {
            Assert.That(team.IsRepresentative(Guid.NewGuid()), Is.False);
            Assert.That(team.IsRepresentative(Guid.Empty), Is.False);
        });
    }

    [Test]
    public void SetInviteOnly_ClosesAndReopensTeam()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;

        team.SetInviteOnly(true);
        Assert.That(team.IsInviteOnly, Is.True);

        team.SetInviteOnly(false);
        Assert.That(team.IsInviteOnly, Is.False);
    }

    [Test]
    public void SetInviteOnly_KeepsMembersAndInvitations()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;
        var invited = Guid.NewGuid();
        team.PendingInvitationStudentIds.Add(invited);

        team.SetInviteOnly(true);

        Assert.Multiple(() =>
        {
            Assert.That(team.MemberStudentIds, Is.EqualTo(new List<Guid> { Creator }));
            Assert.That(team.PendingInvitationStudentIds, Is.EqualTo(new List<Guid> { invited }));
        });
    }

    [Test]
    public void AllowsJoinBy_OpenTeamAllowsAnyStudent()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;

        Assert.That(team.AllowsJoinBy(Guid.NewGuid()), Is.True);
    }

    [Test]
    public void AllowsJoinBy_InviteOnlyTeamAllowsOnlyInvitedStudents()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;
        var invited = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        team.PendingInvitationStudentIds.Add(invited);

        team.SetInviteOnly(true);

        Assert.Multiple(() =>
        {
            Assert.That(team.AllowsJoinBy(invited), Is.True);
            Assert.That(team.AllowsJoinBy(stranger), Is.False);
        });
    }

    [Test]
    public void AllowsJoinBy_ReopenedTeamAllowsAnyStudentAgain()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;
        team.SetInviteOnly(true);

        team.SetInviteOnly(false);

        Assert.That(team.AllowsJoinBy(Guid.NewGuid()), Is.True);
    }

    private static void AssertFailure(TeamCreation creation)
    {
        Assert.Multiple(() =>
        {
            Assert.That(creation.IsSuccess, Is.False);
            Assert.That(creation.Team, Is.Null);
            Assert.That(creation.Error, Is.Not.Null.And.Not.Empty);
        });
    }
}
