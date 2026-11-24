using Application.Activities;
using Application.Courses;
using Application.Interfaces;
using Application.Teams;
using Domain.Courses;
using Domain.Entities;
using Infrastructure.Activities;
using Infrastructure.Courses;
using Infrastructure.Repositories;
using Infrastructure.Teams;
using Studium.Domain.Entities;

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
}
