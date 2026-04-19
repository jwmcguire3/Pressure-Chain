namespace PressureChain.Core.Grid;

public sealed class HexGrid<T> where T : class
{
    private readonly HashSet<HexCoord> _validCoords;
    private readonly Dictionary<HexCoord, T> _values;

    public HexGrid(IEnumerable<HexCoord> validCoords)
    {
        ArgumentNullException.ThrowIfNull(validCoords);

        _validCoords = [.. validCoords];
        _values = new Dictionary<HexCoord, T>();
    }

    public T? Get(HexCoord coord)
    {
        if (!_validCoords.Contains(coord))
        {
            return null;
        }

        return _values.GetValueOrDefault(coord);
    }

    public void Set(HexCoord coord, T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!_validCoords.Contains(coord))
        {
            throw new ArgumentOutOfRangeException(nameof(coord), "Coordinate is not part of this grid.");
        }

        _values[coord] = value;
    }

    public bool Contains(HexCoord coord)
    {
        return _validCoords.Contains(coord);
    }

    public IEnumerable<HexCoord> AllCoords()
    {
        return _validCoords;
    }

    public IEnumerable<(HexCoord coord, T value)> Cells()
    {
        foreach (var pair in _values)
        {
            yield return (pair.Key, pair.Value);
        }
    }
}
