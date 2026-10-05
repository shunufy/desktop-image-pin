using DesktopImagePin.Services;

namespace DesktopImagePin.Tests;

public sealed class GroupTransformCalculatorTests
{
    [Fact]
    public void Scale_ChangesSizesAndSpacingAroundAnchor()
    {
        var anchorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var results = GroupTransformCalculator.Scale(
            [
                new GroupTransformInput(anchorId, 100, 100, 1, 1),
                new GroupTransformInput(memberId, 200, 150, 0.5, 0.75)
            ],
            anchorId,
            factorX: 2,
            factorY: 2,
            minimumScale: 0.05,
            maximumScale: 10);

        var anchor = Assert.Single(results, result => result.Id == anchorId);
        var member = Assert.Single(results, result => result.Id == memberId);
        Assert.Equal(100, anchor.Left);
        Assert.Equal(100, anchor.Top);
        Assert.Equal(300, member.Left);
        Assert.Equal(200, member.Top);
        Assert.Equal(1, member.ScaleX);
        Assert.Equal(1.5, member.ScaleY);
    }

    [Fact]
    public void Scale_HorizontalOnly_PreservesVerticalValues()
    {
        var anchorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var results = GroupTransformCalculator.Scale(
            [
                new GroupTransformInput(anchorId, 10, 20, 1, 1),
                new GroupTransformInput(memberId, 30, 50, 1, 1)
            ],
            anchorId,
            factorX: 1.5,
            factorY: 1,
            minimumScale: 0.05,
            maximumScale: 10);

        var member = Assert.Single(results, result => result.Id == memberId);
        Assert.Equal(40, member.Left);
        Assert.Equal(50, member.Top);
        Assert.Equal(1.5, member.ScaleX);
        Assert.Equal(1, member.ScaleY);
    }

    [Fact]
    public void Scale_UsesOneConstrainedFactorForWholeGroup()
    {
        var anchorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var results = GroupTransformCalculator.Scale(
            [
                new GroupTransformInput(anchorId, 0, 0, 9, 9),
                new GroupTransformInput(memberId, 90, 0, 3, 3)
            ],
            anchorId,
            factorX: 2,
            factorY: 2,
            minimumScale: 0.05,
            maximumScale: 10);

        var anchor = Assert.Single(results, result => result.Id == anchorId);
        var member = Assert.Single(results, result => result.Id == memberId);
        Assert.Equal(10, anchor.ScaleX, precision: 8);
        Assert.Equal(10, anchor.ScaleY, precision: 8);
        Assert.Equal(100, member.Left, precision: 8);
        Assert.Equal(10.0 / 3.0, member.ScaleX, precision: 8);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(270)]
    public void HorizontalScale_MapsScreenAxisToRotatedImageAxis(int rotation)
    {
        var anchorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var results = GroupTransformCalculator.Scale(
            [new(anchorId, 0, 0, 1, 1), new(memberId, 100, 100, 1, 1, rotation)],
            anchorId, 2, 1, 0.05, 10);

        var member = Assert.Single(results, result => result.Id == memberId);
        Assert.Equal(200, member.Left);
        Assert.Equal(100, member.Top);
        Assert.Equal(1, member.ScaleX);
        Assert.Equal(2, member.ScaleY);
    }

    [Theory]
    [InlineData(10, 1.1)]
    [InlineData(0.05, 0.9)]
    public void UniformZoom_StopsBothAxesWhenAnyMemberReachesALimit(double limit, double factor)
    {
        var anchorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var results = GroupTransformCalculator.Scale(
            [new(anchorId, 0, 0, limit, 1), new(memberId, 100, 100, 1, 1)],
            anchorId, factor, factor, 0.05, 10);

        var member = Assert.Single(results, result => result.Id == memberId);
        Assert.Equal(1, member.ScaleX);
        Assert.Equal(1, member.ScaleY);
        Assert.Equal(100, member.Left);
        Assert.Equal(100, member.Top);
    }

    [Fact]
    public void HorizontalZoom_RespectsRotatedMembersScreenWidthLimit()
    {
        var id = Guid.NewGuid();
        var result = Assert.Single(GroupTransformCalculator.Scale(
            [new(id, 0, 0, 1, 10, 90)], id, 1.1, 1, 0.05, 10));

        Assert.Equal(1, result.ScaleX);
        Assert.Equal(10, result.ScaleY);
    }

    [Fact]
    public void LargeRotatedGroup_UsesTheLimitingMemberAndPreservesEveryRelativePosition()
    {
        var inputs = Enumerable.Range(0, 2048)
            .Select(index => new GroupTransformInput(Guid.NewGuid(), index % 64 * 100, index / 64 * 80,
                1, index == 2047 ? 8 : 1, index % 4 * 90))
            .ToArray();
        var anchor = inputs[1000];

        var results = GroupTransformCalculator.Scale(inputs, anchor.Id, 2, 1,
            ImageManager.MinimumScale, ImageManager.MaximumScale);

        Assert.Equal(inputs.Length, results.Count);
        for (var index = 0; index < inputs.Length; index++)
        {
            var input = inputs[index];
            var result = results[index];
            Assert.Equal(input.Id, result.Id);
            Assert.Equal(anchor.Left + (input.Left - anchor.Left) * 1.25, result.Left, precision: 8);
            Assert.Equal(input.Top, result.Top);
            var rotated = input.RotationDegrees is 90 or 270;
            Assert.Equal(input.ScaleX * (rotated ? 1 : 1.25), result.ScaleX, precision: 8);
            Assert.Equal(input.ScaleY * (rotated ? 1.25 : 1), result.ScaleY, precision: 8);
        }
    }
}
