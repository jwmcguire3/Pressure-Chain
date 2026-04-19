using PressureChain.Core.Grid;

namespace PressureChain.Core.Board;

public sealed class Board
{
    private readonly HexGrid<Node> _grid;

    public Board(HexGrid<Node> initialGrid)
    {
        ArgumentNullException.ThrowIfNull(initialGrid);

        var coords = initialGrid.AllCoords().ToArray();
        _grid = new HexGrid<Node>(coords);

        foreach (var coord in coords)
        {
            if (!initialGrid.TryGet(coord, out var node))
            {
                throw new ArgumentException("Initial grid must contain a node for every board coordinate.", nameof(initialGrid));
            }

            _grid.Set(coord, node);
        }

        Coords = Array.AsReadOnly(coords);
    }

    public IReadOnlyCollection<HexCoord> Coords { get; }

    public Node NodeAt(HexCoord c)
    {
        if (!_grid.Contains(c))
        {
            throw new ArgumentOutOfRangeException(nameof(c), "Coordinate is not part of this board.");
        }

        if (!_grid.TryGet(c, out var node))
        {
            throw new InvalidOperationException("Board is missing a node for a valid coordinate.");
        }

        return node;
    }

    public Board WithNode(HexCoord c, Node replacement)
    {
        if (!_grid.Contains(c))
        {
            throw new ArgumentOutOfRangeException(nameof(c), "Coordinate is not part of this board.");
        }

        var nextGrid = new HexGrid<Node>(Coords);
        foreach (var coord in Coords)
        {
            nextGrid.Set(coord, coord == c ? replacement : NodeAt(coord));
        }

        return new Board(nextGrid);
    }
}
