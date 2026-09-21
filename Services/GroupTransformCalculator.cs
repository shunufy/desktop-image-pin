namespace DesktopImagePin.Services;

public readonly record struct GroupTransformInput(
    Guid Id,
    double Left,
    double Top,
    double ScaleX,
    double ScaleY,
    int RotationDegrees = 0);

public readonly record struct GroupTransformResult(
    Guid Id,
    double Left,
    double Top,
    double ScaleX,
    double ScaleY);

public static class GroupTransformCalculator
{
    public static IReadOnlyList<GroupTransformResult> Scale(
        IEnumerable<GroupTransformInput> inputs,
        Guid anchorId,
        double factorX,
        double factorY,
        double minimumScale,
        double maximumScale)
    {
        var items = inputs.ToArray();
        if (items.Length == 0)
        {
            return [];
        }

        var anchorIndex = Array.FindIndex(items, item => item.Id == anchorId);
        if (anchorIndex < 0)
        {
            throw new ArgumentException("Anchor item is not part of the group.", nameof(anchorId));
        }

        var anchor = items[anchorIndex];
        if (!double.IsFinite(factorX) || factorX <= 0
            || !double.IsFinite(factorY) || factorY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factorX), "Scale factors must be finite and positive.");
        }

        var constrainedFactorX = ConstrainFactor(
            items.Select(item => SwapsAxes(item) ? item.ScaleY : item.ScaleX),
            factorX,
            minimumScale,
            maximumScale);
        var constrainedFactorY = ConstrainFactor(
            items.Select(item => SwapsAxes(item) ? item.ScaleX : item.ScaleY),
            factorY,
            minimumScale,
            maximumScale);

        if (factorX == factorY)
        {
            var uniformFactor = ConstrainFactor(
                items.SelectMany(item => new[] { item.ScaleX, item.ScaleY }),
                factorX,
                minimumScale,
                maximumScale);
            constrainedFactorX = uniformFactor;
            constrainedFactorY = uniformFactor;
        }

        return items.Select(item => new GroupTransformResult(
            item.Id,
            anchor.Left + ((item.Left - anchor.Left) * constrainedFactorX),
            anchor.Top + ((item.Top - anchor.Top) * constrainedFactorY),
            Math.Clamp(item.ScaleX * (SwapsAxes(item) ? constrainedFactorY : constrainedFactorX), minimumScale, maximumScale),
            Math.Clamp(item.ScaleY * (SwapsAxes(item) ? constrainedFactorX : constrainedFactorY), minimumScale, maximumScale))).ToArray();
    }

    private static bool SwapsAxes(GroupTransformInput item) => Math.Abs(item.RotationDegrees % 180) == 90;

    private static double ConstrainFactor(
        IEnumerable<double> scales,
        double requestedFactor,
        double minimumScale,
        double maximumScale)
    {
        var scaleValues = scales.ToArray();
        var minimumFactor = scaleValues.Max(scale => minimumScale / scale);
        var maximumFactor = scaleValues.Min(scale => maximumScale / scale);
        return Math.Clamp(requestedFactor, minimumFactor, maximumFactor);
    }
}
