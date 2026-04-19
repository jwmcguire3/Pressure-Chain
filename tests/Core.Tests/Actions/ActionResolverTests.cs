using PressureChain.Core.Actions;
using PressureChain.Core.Board;
using PressureChain.Core.Chains;
using PressureChain.Core.Grid;
using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Core.Tests.Actions;

public sealed class ActionResolverTests
{
    private static readonly HexCoord Center = new(0, 0);
    private static readonly HexCoord East = new(1, 0);
    private static readonly HexCoord West = new(-1, 0);

    private readonly ActionResolver _resolver = new(new ChainResolver());

    [Fact]
    public void Apply_MergeTwoCells_LeavesOneAtFiftyAndClearsTheOther()
    {
        var board = CreateBoard(
            (Center, CreateNode(NodeType.Cell, 20)),
            (East, CreateNode(NodeType.Cell, 30)));

        var result = _resolver.Apply(board, new MergeAction(Center, East));

        Assert.Equal(NodeType.Cell, result.NodeAt(Center).Type);
        Assert.Equal(0, result.NodeAt(Center).Pressure);
        Assert.Equal(NodeType.Cell, result.NodeAt(East).Type);
        Assert.Equal(50, result.NodeAt(East).Pressure);
    }

    [Fact]
    public void Apply_MergePastOneHundred_Throws()
    {
        var board = CreateBoard(
            (Center, CreateNode(NodeType.Cell, 70)),
            (East, CreateNode(NodeType.Cell, 60)));

        var exception = Assert.Throws<InvalidActionException>(() => _resolver.Apply(board, new MergeAction(Center, East)));

        Assert.Equal("Merge cannot exceed 100 total pressure.", exception.Reason);
    }

    [Fact]
    public void Apply_MergeNonAdjacentNodes_Throws()
    {
        var board = CreateBoard(
            (Center, CreateNode(NodeType.Cell, 20)),
            (new HexCoord(2, 0), CreateNode(NodeType.Cell, 30)));

        var exception = Assert.Throws<InvalidActionException>(() => _resolver.Apply(board, new MergeAction(Center, new HexCoord(2, 0))));

        Assert.Equal("Merge requires adjacent nodes.", exception.Reason);
    }

    [Fact]
    public void Apply_MergeDifferentTypes_Throws()
    {
        var board = CreateBoard(
            (Center, CreateNode(NodeType.Cell, 20)),
            (East, CreateNode(NodeType.Vent, 30, facing: HexDirection.E)));

        var exception = Assert.Throws<InvalidActionException>(() => _resolver.Apply(board, new MergeAction(Center, East)));

        Assert.Equal("Merge requires nodes of the same type.", exception.Reason);
    }

    [Fact]
    public void Apply_VentRedirectOnNonVent_Throws()
    {
        var board = CreateBoard((Center, CreateNode(NodeType.Cell, 20)));

        var exception = Assert.Throws<InvalidActionException>(() => _resolver.Apply(board, new VentRedirectAction(Center, HexDirection.NE)));

        Assert.Equal("Vent redirect requires a vent target.", exception.Reason);
    }

    [Fact]
    public void Apply_TriggerEarlyBelowCriticalPressure_Throws()
    {
        var board = CreateBoard((Center, CreateNode(NodeType.Cell, 74)));

        var exception = Assert.Throws<InvalidActionException>(() => _resolver.Apply(board, new TriggerEarlyAction(Center)));

        Assert.Equal("Trigger early requires pressure 75 or higher.", exception.Reason);
    }

    [Fact]
    public void Apply_TriggerEarlyUsesSeventyFivePercentRelease()
    {
        var board = CreateBoard(
            (Center, CreateNode(NodeType.Cell, 80, connections: OpenOnly(HexDirection.E))),
            (East, CreateNode(NodeType.Cell, 0, connections: OpenOnly(HexDirection.W))),
            (West, CreateNode(NodeType.Cell, 0)));

        var result = _resolver.Apply(board, new TriggerEarlyAction(Center));

        Assert.Equal(20, result.NodeAt(Center).Pressure);
        Assert.Equal(10, result.NodeAt(East).Pressure);
        Assert.Equal(0, result.NodeAt(West).Pressure);
    }

    private static GameBoard CreateBoard(params (HexCoord coord, Node node)[] entries)
    {
        var grid = new HexGrid<Node>(entries.Select(entry => entry.coord));
        foreach (var (coord, node) in entries)
        {
            grid.Set(coord, node);
        }

        return new GameBoard(grid);
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
