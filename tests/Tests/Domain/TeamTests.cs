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
        team.InviteStudent(invited);

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
        team.InviteStudent(invited);

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

    [Test]
    public void Copy_IsIndependentSnapshot()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;
        var invited = Guid.NewGuid();
        team.InviteStudent(invited);
        team.SetInviteOnly(true);
        team.MarkStored(7);

        var copy = team.Copy();

        copy.SetInviteOnly(false);
        copy.InviteStudent(Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(copy.Id, Is.EqualTo(team.Id));
            Assert.That(copy.Version, Is.EqualTo(7));
            Assert.That(copy.MemberStudentIds, Is.Not.SameAs(team.MemberStudentIds));
            Assert.That(copy.PendingInvitationStudentIds, Is.Not.SameAs(team.PendingInvitationStudentIds));
            Assert.That(team.IsInviteOnly, Is.True);
            Assert.That(team.MemberStudentIds, Has.Count.EqualTo(1));
            Assert.That(team.PendingInvitationStudentIds, Does.Contain(invited));
        });
    }

    [Test]
    public void Copy_CarriesInvitationsAndMembership()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;
        var invited = Guid.NewGuid();
        team.InviteStudent(invited);

        var copy = team.Copy();

        Assert.Multiple(() =>
        {
            Assert.That(copy.MemberStudentIds, Has.Count.EqualTo(1));
            Assert.That(copy.MemberStudentIds, Does.Contain(Creator));
            Assert.That(copy.PendingInvitationStudentIds, Does.Contain(invited));
            Assert.That(copy.RepresentativeStudentId, Is.EqualTo(Creator));
            Assert.That(copy.IsRepresentative(Creator), Is.True);
        });
    }

    [Test]
    public void StudentTeam_ExposesNoPublicSetters()
    {
        var writable = typeof(StudentTeam)
            .GetProperties()
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => property.Name)
            .ToArray();

        Assert.That(writable, Is.Empty);
    }

    [Test]
    public void MemberStudentIds_IsReadOnlyView()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;

        var members = team.MemberStudentIds;

        Assert.Multiple(() =>
        {
            Assert.That(members, Is.EqualTo(new[] { Creator }));
            Assert.That(((ICollection<Guid>)members).IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => ((ICollection<Guid>)members).Add(Guid.NewGuid()));
            Assert.That(team.MemberStudentIds, Is.EqualTo(new[] { Creator }));
        });
    }

    [Test]
    public void PendingInvitationStudentIds_IsReadOnlyView()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;

        var invitations = team.PendingInvitationStudentIds;

        Assert.Multiple(() =>
        {
            Assert.That(invitations, Is.Empty);
            Assert.That(((ICollection<Guid>)invitations).IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => ((ICollection<Guid>)invitations).Add(Guid.NewGuid()));
        });
    }

    [Test]
    public void MemberStudentIds_ViewCannotBeMutatedThroughReturnedCollection()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;
        var stranger = Guid.NewGuid();

        Assert.Throws<NotSupportedException>(() => ((IList<Guid>)team.MemberStudentIds).Add(stranger));
        Assert.That(team.MemberStudentIds, Does.Not.Contain(stranger));
    }

    [Test]
    public void InviteStudent_RejectsEmptyExistingMemberAndDuplicate()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;
        var invited = Guid.NewGuid();

        var accepted = team.InviteStudent(invited);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(team.InviteStudent(Guid.Empty), Is.False);
            Assert.That(team.InviteStudent(Creator), Is.False);
            Assert.That(team.InviteStudent(invited), Is.False);
            Assert.That(team.PendingInvitationStudentIds, Is.EqualTo(new[] { invited }));
            Assert.That(team.MemberStudentIds, Is.EqualTo(new[] { Creator }));
        });
    }

    [Test]
    public void TryCreate_RejectsEmptyTeamId()
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5, Guid.Empty);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_UsesProvidedTeamIdForSeededData()
    {
        var teamId = Guid.NewGuid();

        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5, teamId).Team!;

        Assert.That(team.Id, Is.EqualTo(teamId));
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
