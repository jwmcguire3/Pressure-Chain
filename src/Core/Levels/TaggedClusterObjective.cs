using PressureChain.Core.Grid;

namespace PressureChain.Core.Levels;

public sealed record TaggedClusterObjective(
    string Name,
    IReadOnlyList<HexCoord> TargetCoords) : LevelObjective
{
    public override string ToString()
    {
        return Name;
    }
}
