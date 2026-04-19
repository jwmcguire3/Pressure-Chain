using PressureChain.Core.Board;
using PressureChain.Core.Grid;
using PressureChain.Core.Levels;
using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Presentation;

public static class Phase1TestLevelFactory
{
    public static LevelState Create()
    {
        var coords = new List<HexCoord>(70);
        for (var r = 0; r < 7; r++)
        {
            for (var q = 0; q < 10; q++)
            {
                coords.Add(new HexCoord(q, r));
            }
        }

        var grid = new HexGrid<Node>(coords);
        foreach (var coord in coords)
        {
            grid.Set(coord, CreateNode(NodeType.Cell, 0));
        }

        Set(grid, new HexCoord(0, 3), CreateNode(NodeType.Vent, 70, facing: HexDirection.E));
        Set(grid, new HexCoord(1, 3), CreateNode(NodeType.Cell, 68, connections: OpenOnly(HexDirection.W, HexDirection.E)));
        Set(grid, new HexCoord(2, 3), CreateNode(NodeType.Cell, 34, connections: OpenOnly(HexDirection.W, HexDirection.E)));
        Set(grid, new HexCoord(3, 3), CreateNode(NodeType.Cell, 0, connections: OpenOnly(HexDirection.W, HexDirection.E)));

        Set(grid, new HexCoord(4, 2), CreateNode(NodeType.Bulwark, 0));
        Set(grid, new HexCoord(4, 3), CreateNode(NodeType.Bulwark, 0));
        Set(grid, new HexCoord(4, 4), CreateNode(NodeType.Bulwark, 0));

        Set(grid, new HexCoord(7, 0), CreateNode(NodeType.Cell, 60, connections: OpenOnly(HexDirection.E)));
        Set(grid, new HexCoord(8, 0), CreateNode(NodeType.Amplifier, 95, connections: OpenOnly(HexDirection.W, HexDirection.E)));
        Set(grid, new HexCoord(9, 0), CreateNode(NodeType.Cell, 70, connections: OpenOnly(HexDirection.W, HexDirection.SE)));
        Set(grid, new HexCoord(9, 1), CreateNode(NodeType.Cell, 84, connections: OpenOnly(HexDirection.NW)));

        Set(grid, new HexCoord(7, 5), CreateNode(NodeType.Vent, 55, facing: HexDirection.NW));
        Set(grid, new HexCoord(6, 5), CreateNode(NodeType.Cell, 32, connections: OpenOnly(HexDirection.E, HexDirection.W)));
        Set(grid, new HexCoord(5, 5), CreateNode(NodeType.Cell, 32, connections: OpenOnly(HexDirection.E)));

        Set(grid, new HexCoord(1, 1), CreateNode(NodeType.Cell, 28));
        Set(grid, new HexCoord(2, 1), CreateNode(NodeType.Cell, 28));
        Set(grid, new HexCoord(2, 2), CreateNode(NodeType.Cell, 52));

        return new LevelState(
            Board: new GameBoard(grid),
            MovesRemaining: 15,
            Objective: new ClearAllOfTypeObjective(NodeType.Cell),
            ScoreAccumulated: 0,
            Status: LevelStatus.InProgress);
    }

    private static Node CreateNode(
        NodeType type,
        int pressure,
        HexDirection? facing = null,
        ConnectionMask? connections = null,
        NodeModifiers modifiers = NodeModifiers.None)
    {
        return new Node(
            type,
            pressure,
            Facing: facing,
            Connections: connections ?? ConnectionMask.AllOpen(),
            modifiers);
    }

    private static void Set(HexGrid<Node> grid, HexCoord coord, Node node)
    {
        grid.Set(coord, node);
    }

    private static ConnectionMask OpenOnly(params HexDirection[] directions)
    {
        var mask = ConnectionMask.AllClosed();
        foreach (var direction in directions)
        {
            mask = mask.With(direction, open: true);
        }

        return mask;
    }
}
