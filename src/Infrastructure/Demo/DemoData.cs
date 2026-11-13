using Application.Activities;
using Application.Courses;
using Application.Teams;
using Domain.Courses;
using Infrastructure.Activities;
using Infrastructure.Courses;
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
            maxSize: 6).Team!;
        team.Id = DemoTeamId;

        return new InMemoryTeamRepository(new[] { team });
    }

    public static ICourseReader CreateCourseReader()
    {
        return new InMemoryCourseReader(new[]
        {
            new Course(Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "ERS", "Elementi razvoja softvera"),
            new Course(Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "OIB", "Osnove informacione bezbednosti")
        });
    }

    public static IActivityRepository CreateActivityRepository()
    {
        return new InMemoryActivityRepository();
    }
}
