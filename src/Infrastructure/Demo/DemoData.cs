using Application.Activities;
using Application.Courses;
using Application.Engagements;
using Application.Enrollments;
using Application.Interfaces;
using Application.Teams;
using Domain.Courses;
using Domain.Entities;
using Domain.Enrollments;
using Domain.Offerings;
using Domain.Staff;
using Infrastructure.Activities;
using Infrastructure.Courses;
using Infrastructure.Engagements;
using Infrastructure.Enrollments;
using Infrastructure.Repositories;
using Infrastructure.Teams;
using Studium.Domain.Entities;

// Integracija ST-007 i main: Enrollment postoji i u Domain.Entities (ovde) i u Domain.Enrollments (ST-006),
// pa se ovim alijasom jasno bira onaj koji koristi CreateEnrollments.
using Enrollment = Domain.Entities.Enrollment;

namespace Infrastructure.Demo;

public static class DemoData
{
    // Sintetički identifikatori demo tima (ST-021). Nisu stvarni podaci.
    public static readonly Guid DemoTeamId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid DemoTeamRepresentativeId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    /// <summary>
    /// Jedan demo tim, jer kreiranje tima još nema slučaj upotrebe niti endpoint.
    /// </summary>
    public static ITeamRepository CreateTeamRepository()
    {
        var team = StudentTeam.TryCreate(
            courseOfferingId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            projectActivityId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            name: "Demo tim",
            creatorStudentId: DemoTeamRepresentativeId,
            minSize: 4,
            maxSize: 6,
            teamId: DemoTeamId).Team!;

        return new InMemoryTeamRepository(new[] { team });
    }

    public static readonly Guid ErsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OibId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid ErsTeacherId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static ICourseReader CreateCourseReader()
    {
        return new InMemoryCourseReader(new[]
        {
            new Course(ErsId, "ERS", "Elementi razvoja softvera"),
            new Course(OibId, "OIB", "Osnove informacione bezbednosti")
        });
    }

    public static IActivityRepository CreateActivityRepository()
    {
        return new InMemoryActivityRepository();
    }

    public static IOfferingAccessReader CreateOfferingAccessReader()
    {
        return new InMemoryOfferingAccessReader(new[]
        {
            (ErsTeacherId, ErsId)
        });
    }

    public static IEnumerable<Enrollment> CreateEnrollments()
    {
        var prekinut = new Enrollment
        {
            OfferingId = ErsId,
            StudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003")
        };
        prekinut.Terminate();

        return new List<Enrollment>
        {
            new Enrollment
            {
                Id = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001"),
                OfferingId = ErsId,
                StudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001")
            },
            new Enrollment
            {
                Id = Guid.Parse("eeeeeeee-0000-0000-0000-000000000002"),
                OfferingId = ErsId,
                StudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002")
            },
            new Enrollment
            {
                Id = Guid.Parse("eeeeeeee-0000-0000-0000-000000000003"),
                OfferingId = OibId,
                StudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001")
            },
            prekinut
        };
    }

    // ---------------- ST-007 (US-7.1, US-7.2) ----------------

    public static readonly Guid DemoStudentId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    public static IStudentOfferingsReader CreateStudentOfferingsReader()
    {

        var ers = new Course(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "ERS", "Elementi razvoja softvera");
        var oib = new Course(Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "OIB", "Osnove informacione bezbednosti");

        var ersOffering = new Offering(Guid.Parse("33333333-0000-0000-0000-000000000001"), ers.Id, "2026/27");
        var oibOffering = new Offering(Guid.Parse("33333333-0000-0000-0000-000000000002"), oib.Id, "2026/27");
        var ersPrevious = new Offering(Guid.Parse("33333333-0000-0000-0000-000000000003"), ers.Id, "2025/26");

        return new InMemoryStudentOfferingsReader(
            new[] { ers, oib },
            new[] { ersOffering, oibOffering, ersPrevious },
            new[]
            {

                new Membership(Guid.Parse("44444444-0000-0000-0000-000000000001"), DemoStudentId, ersOffering.Id),
                new Membership(Guid.Parse("44444444-0000-0000-0000-000000000002"), DemoStudentId, oibOffering.Id),
                new Membership(Guid.Parse("44444444-0000-0000-0000-000000000003"), DemoStudentId, ersPrevious.Id,
                    MembershipStatus.Terminated)
            });
    }


    // US-7.2: sintetički nastavnik i asistentkinja, bez stvarnih osoba.
    public static readonly Guid DemoTeacherId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    public static readonly Guid DemoAssistantId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    public static IStaffOfferingsReader CreateStaffOfferingsReader()
    {

        var ers = new Course(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "ERS", "Elementi razvoja softvera");
        var oib = new Course(Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "OIB", "Osnove informacione bezbednosti");

        var ersOffering = new Offering(Guid.Parse("33333333-0000-0000-0000-000000000001"), ers.Id, "2026/27");
        var oibOffering = new Offering(Guid.Parse("33333333-0000-0000-0000-000000000002"), oib.Id, "2026/27");


        var removedFromErs = new TeachingAssignment(Guid.Parse("55555555-0000-0000-0000-000000000004"),
            ersOffering.Id, DemoAssistantId, Responsibility.Assistant);
        removedFromErs.Remove(DemoTeacherId, new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc));

        return new InMemoryStaffOfferingsReader(
            new[] { ers, oib },
            new[] { ersOffering, oibOffering },
            new[]
            {
                new TeachingAssignment(Guid.Parse("55555555-0000-0000-0000-000000000001"),
                    ersOffering.Id, DemoTeacherId, Responsibility.ResponsibleTeacher),
                new TeachingAssignment(Guid.Parse("55555555-0000-0000-0000-000000000002"),
                    oibOffering.Id, DemoTeacherId, Responsibility.Teacher),
                new TeachingAssignment(Guid.Parse("55555555-0000-0000-0000-000000000003"),
                    oibOffering.Id, DemoAssistantId, Responsibility.Assistant),
                removedFromErs
            });
    }
}
