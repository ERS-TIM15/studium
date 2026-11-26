using NUnit.Framework;
using Domain.Teams;
using Infrastructure.Teams;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryTeamSizeLimitsRepositoryTests
{
    private static TeamSizeLimits Limits(int min, int max) =>
        TeamSizeLimits.TryCreate(min, max).Limits!;

    [Test]
    public async Task GetAsync_ReturnsNullWhenNothingSaved()
    {
        var repository = new InMemoryTeamSizeLimitsRepository();

        var limits = await repository.GetAsync(Guid.NewGuid());

        Assert.That(limits, Is.Null);
    }

    [Test]
    public async Task SaveAsync_ThenGetAsync_ReturnsSavedLimits()
    {
        var repository = new InMemoryTeamSizeLimitsRepository();
        var activity = Guid.NewGuid();

        await repository.SaveAsync(activity, Limits(4, 6));
        var limits = await repository.GetAsync(activity);

        Assert.That(limits, Is.EqualTo(Limits(4, 6)));
    }

    [Test]
    public async Task SaveAsync_ReplacesPreviousLimitsForSameActivity()
    {
        var repository = new InMemoryTeamSizeLimitsRepository();
        var activity = Guid.NewGuid();

        await repository.SaveAsync(activity, Limits(2, 3));
        await repository.SaveAsync(activity, Limits(4, 6));

        Assert.That(await repository.GetAsync(activity), Is.EqualTo(Limits(4, 6)));
    }

    [Test]
    public async Task SaveAsync_KeepsActivitiesIndependent()
    {
        var repository = new InMemoryTeamSizeLimitsRepository();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        await repository.SaveAsync(first, Limits(2, 3));
        await repository.SaveAsync(second, Limits(4, 6));

        Assert.That(await repository.GetAsync(first), Is.EqualTo(Limits(2, 3)));
        Assert.That(await repository.GetAsync(second), Is.EqualTo(Limits(4, 6)));
    }
}
