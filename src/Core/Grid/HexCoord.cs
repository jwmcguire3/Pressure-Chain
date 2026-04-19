namespace PressureChain.Core.Grid;

public readonly record struct HexCoord(int Q, int R)
{
    public static HexCoord[] Directions { get; } =
    [
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, 1)
    ];

    public IEnumerable<HexCoord> Neighbors()
    {
        foreach (var direction in Directions)
        {
            yield return new HexCoord(Q + direction.Q, R + direction.R);
        }
    }

    public int DistanceTo(HexCoord other)
    {
        var deltaQ = Q - other.Q;
        var deltaR = R - other.R;
        var deltaS = (-Q - R) - (-other.Q - other.R);

        return (Math.Abs(deltaQ) + Math.Abs(deltaR) + Math.Abs(deltaS)) / 2;
    }

    public static HexCoord operator +(HexCoord left, HexCoord right)
    {
        return new HexCoord(left.Q + right.Q, left.R + right.R);
    }
}
