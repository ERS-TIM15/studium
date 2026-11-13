using NUnit.Framework;
using Domain.Teams;

namespace Tests.Domain;

[TestFixture]
public sealed class TeamSizeLimitsTests
{
    [Test]
    public void TryCreate_AssignsMinAndMax()
    {
        var creation = TeamSizeLimits.TryCreate(4, 6);

        Assert.That(creation.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(creation.Limits!.MinSize, Is.EqualTo(4));
            Assert.That(creation.Limits!.MaxSize, Is.EqualTo(6));
            Assert.That(creation.Error, Is.Null);
        });
    }

    [TestCase(1, 1)]
    [TestCase(3, 3)]
    [TestCase(1, 100)]
    public void TryCreate_AcceptsBoundaryValues(int min, int max)
    {
        var creation = TeamSizeLimits.TryCreate(min, max);

        Assert.That(creation.IsSuccess, Is.True);
    }

    [TestCase(0, 5)]
    [TestCase(-1, 5)]
    [TestCase(int.MinValue, 5)]
    public void TryCreate_RejectsMinBelowOne(int min, int max)
    {
        var creation = TeamSizeLimits.TryCreate(min, max);

        Assert.That(creation.IsSuccess, Is.False);
        Assert.That(creation.Error, Is.Not.Null.And.Not.Empty);
    }

    [TestCase(5, 4)]
    [TestCase(2, 1)]
    [TestCase(3, 0)]
    [TestCase(3, -2)]
    public void TryCreate_RejectsMaxBelowMin(int min, int max)
    {
        var creation = TeamSizeLimits.TryCreate(min, max);

        Assert.That(creation.IsSuccess, Is.False);
        Assert.That(creation.Limits, Is.Null);
        Assert.That(creation.Error, Is.Not.Null.And.Not.Empty);
    }
}
