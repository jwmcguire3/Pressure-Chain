using PressureChain.Core.Board;
using PressureChain.Core.Chains;
using PressureChain.Core.Grid;

namespace PressureChain.Core.Tests.Chains;

public sealed class ChainResolverTests
{
    private static readonly HexCoord Center = new(0, 0);
    private static readonly HexCoord East = new(1, 0);
    private static readonly HexCoord NorthEast = new(1, -1);
    private static readonly HexCoord NorthWest = new(0, -1);
    private static readonly HexCoord West = new(-1, 0);
    private static readonly HexCoord SouthWest = new(-1, 1);
    private static readonly HexCoord SouthEast = new(0, 1);

    private readonly ChainResolver _resolver = new();

    [Fact]
    public void Resolve_SingleCellBurst_PushesSixteenPressureToEachOpenNeighbor()
    {
        var board = CreateBoard(
            (Center, CreateNode(NodeType.Cell, 100)),
            (East, CreateNode(NodeType.Cell, 0)),
            (NorthEast, CreateNode(NodeType.Cell, 0)),
            (NorthWest, CreateNode(NodeType.Cell, 0)),
            (West, CreateNode(NodeType.Cell, 0)),
            (SouthWest, CreateNode(NodeType.Cell, 0)),
            (SouthEast, CreateNode(NodeType.Cell, 0)));

        var resolution = _resolver.Resolve(board, Center);

        Assert.Single(resolution.Waves);
        Assert.Single(resolution.Waves[0]);
        Assert.Equal(new BurstEvent(Center, Wave: 0, WasChained: false), resolution.Waves[0][0]);
        Assert.Equal(1, resolution.TotalBurstCount);
        Assert.Equal(16, resolution.FinalBoard.NodeAt(East).Pressure);
        Assert.Equal(16, resolution.FinalBoard.NodeAt(NorthEast).Pressure);
        Assert.Equal(16, resolution.FinalBoard.NodeAt(NorthWest).Pressure);
        Assert.Equal(16, resolution.FinalBoard.NodeAt(West).Pressure);
        Assert.Equal(16, resolution.FinalBoard.NodeAt(SouthWest).Pressure);
        Assert.Equal(16, resolution.FinalBoard.NodeAt(SouthEast).Pressure);
    }

    [Fact]
    public void Resolve_TwoAdjacentCells_BurstsAcrossTwoBreadthFirstWaves()
    {
        var board = CreateBoard(
            (Center, CreateNode(NodeType.Cell, 100, connections: OpenOnly(HexDirection.E))),
            (East, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W))));

        var resolution = _resolver.Resolve(board, Center);

        Assert.Equal(2, resolution.Waves.Count);
        Assert.Equal(new BurstEvent(Center, Wave: 0, WasChained: false), resolution.Waves[0][0]);
        Assert.Equal(new BurstEvent(East, Wave: 1, WasChained: true), resolution.Waves[1][0]);
        Assert.Equal(2, resolution.TotalBurstCount);
    }

    [Fact]
    public void Resolve_BulwarkBetweenCells_BlocksTheChain()
    {
        var board = CreateBoard(
            (Center, CreateNode(NodeType.Cell, 100, connections: OpenOnly(HexDirection.E))),
            (East, CreateNode(NodeType.Bulwark, 0, connections: OpenOnly(HexDirection.W, HexDirection.E))),
            (new HexCoord(2, 0), CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W))));

        var resolution = _resolver.Resolve(board, Center);

        Assert.Single(resolution.Waves);
        Assert.Equal(90, resolution.FinalBoard.NodeAt(new HexCoord(2, 0)).Pressure);
        Assert.Equal(0, resolution.FinalBoard.NodeAt(East).Pressure);
    }

    [Fact]
    public void Resolve_VentFacingEast_SendsEightyForwardAndTwentySplitAcrossTheOtherFaces()
    {
        var board = CreateBoard(
            (Center, CreateNode(NodeType.Vent, 100, facing: HexDirection.E)),
            (East, CreateNode(NodeType.Cell, 0)),
            (NorthEast, CreateNode(NodeType.Cell, 0)),
            (NorthWest, CreateNode(NodeType.Cell, 0)),
            (West, CreateNode(NodeType.Cell, 0)),
            (SouthWest, CreateNode(NodeType.Cell, 0)),
            (SouthEast, CreateNode(NodeType.Cell, 0)));

        var resolution = _resolver.Resolve(board, Center);

        Assert.Equal(80, resolution.FinalBoard.NodeAt(East).Pressure);
        Assert.Equal(4, resolution.FinalBoard.NodeAt(NorthEast).Pressure);
        Assert.Equal(4, resolution.FinalBoard.NodeAt(NorthWest).Pressure);
        Assert.Equal(4, resolution.FinalBoard.NodeAt(West).Pressure);
        Assert.Equal(4, resolution.FinalBoard.NodeAt(SouthWest).Pressure);
        Assert.Equal(4, resolution.FinalBoard.NodeAt(SouthEast).Pressure);
    }

    [Fact]
    public void Resolve_AmplifierOutputsNormalStandaloneAndDoubleWhenChained()
    {
        var standaloneBoard = CreateBoard(
            (Center, CreateNode(NodeType.Amplifier, 100, connections: OpenOnly(HexDirection.E))),
            (East, CreateNode(NodeType.Cell, 0, connections: OpenOnly(HexDirection.W))));

        var standalone = _resolver.Resolve(standaloneBoard, Center);

        Assert.Equal(16, standalone.FinalBoard.NodeAt(East).Pressure);

        var chainedBoard = CreateBoard(
            (West, CreateNode(NodeType.Cell, 100, connections: OpenOnly(HexDirection.E))),
            (Center, CreateNode(NodeType.Amplifier, 90, connections: OpenOnly(HexDirection.W, HexDirection.E))),
            (East, CreateNode(NodeType.Cell, 0, connections: OpenOnly(HexDirection.W))));

        var chained = _resolver.Resolve(chainedBoard, West);

        Assert.Equal(new BurstEvent(Center, Wave: 1, WasChained: true), chained.Waves[1][0]);
        Assert.Equal(33, chained.FinalBoard.NodeAt(East).Pressure);
    }

    [Fact]
    public void Resolve_TerminatesWhenNoNewBurstsAreProduced()
    {
        var board = CreateBoard(
            (Center, CreateNode(NodeType.Cell, 100, connections: OpenOnly(HexDirection.E))),
            (East, CreateNode(NodeType.Cell, 10, connections: OpenOnly(HexDirection.W))));

        var resolution = _resolver.Resolve(board, Center);

        Assert.Single(resolution.Waves);
        Assert.Equal(1, resolution.TotalBurstCount);
        Assert.Equal(26, resolution.FinalBoard.NodeAt(East).Pressure);
    }

    [Fact]
    public void Resolve_ClosedConnectionFace_BlocksPressureTransfer()
    {
        var board = CreateBoard(
            (Center, CreateNode(NodeType.Cell, 100, connections: OpenOnly(HexDirection.E))),
            (East, CreateNode(NodeType.Cell, 90, connections: ConnectionMask.AllClosed())));

        var resolution = _resolver.Resolve(board, Center);

        Assert.Single(resolution.Waves);
        Assert.Equal(90, resolution.FinalBoard.NodeAt(East).Pressure);
    }

    [Fact]
    public void Resolve_CascadeExample_ProducesFiveWaveChain()
    {
        var first = new HexCoord(0, 0);
        var second = new HexCoord(1, 0);
        var third = new HexCoord(2, 0);
        var fourth = new HexCoord(3, 0);
        var fifth = new HexCoord(4, 0);

        var board = CreateBoard(
            (first, CreateNode(NodeType.Cell, 100, connections: OpenOnly(HexDirection.E))),
            (second, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W, HexDirection.E))),
            (third, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W, HexDirection.E))),
            (fourth, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W, HexDirection.E))),
            (fifth, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W))));

        var resolution = _resolver.Resolve(board, first);

        Assert.Equal(5, resolution.Waves.Count);
        Assert.Equal(first, resolution.Waves[0][0].Origin);
        Assert.Equal(second, resolution.Waves[1][0].Origin);
        Assert.Equal(third, resolution.Waves[2][0].Origin);
        Assert.Equal(fourth, resolution.Waves[3][0].Origin);
        Assert.Equal(fifth, resolution.Waves[4][0].Origin);
        Assert.Equal(5, resolution.TotalBurstCount);
    }

    [Fact]
    public void Resolve_RandomBoardsWithoutAmplifiers_NeverIncreaseTotalPressure()
    {
        var random = new Random(12345);

        for (var iteration = 0; iteration < 200; iteration++)
        {
            var board = CreateRandomBoardWithoutAmplifiers(random, out var triggerOrigin);
            var initialTotal = board.Coords.Sum(coord => board.NodeAt(coord).Pressure);

            var resolution = _resolver.Resolve(board, triggerOrigin);
            var finalTotal = resolution.FinalBoard.Coords.Sum(coord => resolution.FinalBoard.NodeAt(coord).Pressure);

            Assert.True(finalTotal <= initialTotal, $"Iteration {iteration} increased pressure from {initialTotal} to {finalTotal}.");
        }
    }

    [Fact]
    public void Resolve_RandomBoardsAlwaysTerminate()
    {
        var random = new Random(67890);

        for (var iteration = 0; iteration < 200; iteration++)
        {
            var board = CreateRandomBoard(random, out var triggerOrigin);

            var resolution = _resolver.Resolve(board, triggerOrigin);

            Assert.True(resolution.TotalBurstCount <= board.Coords.Count, $"Iteration {iteration} burst more nodes than exist on the board.");
            Assert.True(resolution.Waves.Count <= board.Coords.Count, $"Iteration {iteration} produced too many waves.");
            Assert.Equal(
                resolution.TotalBurstCount,
                resolution.Waves.SelectMany(wave => wave).Select(burst => burst.Origin).Distinct().Count());
        }
    }

    private static PressureChain.Core.Board.Board CreateBoard(params (HexCoord coord, Node node)[] entries)
    {
        var grid = new HexGrid<Node>(entries.Select(entry => entry.coord));
        foreach (var (coord, node) in entries)
        {
            grid.Set(coord, node);
        }

        return new PressureChain.Core.Board.Board(grid);
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

    private static PressureChain.Core.Board.Board CreateRandomBoardWithoutAmplifiers(Random random, out HexCoord triggerOrigin)
    {
        return CreateRandomBoard(
            random,
            [NodeType.Cell, NodeType.Vent, NodeType.Bulwark],
            out triggerOrigin);
    }

    private static PressureChain.Core.Board.Board CreateRandomBoard(Random random, out HexCoord triggerOrigin)
    {
        return CreateRandomBoard(
            random,
            [NodeType.Cell, NodeType.Vent, NodeType.Bulwark, NodeType.Amplifier],
            out triggerOrigin);
    }

    private static PressureChain.Core.Board.Board CreateRandomBoard(Random random, NodeType[] availableTypes, out HexCoord triggerOrigin)
    {
        var coords = new[] { Center, East, NorthEast, NorthWest, West, SouthWest, SouthEast };
        var entries = new List<(HexCoord coord, Node node)>(coords.Length);

        foreach (var coord in coords)
        {
            var type = availableTypes[random.Next(availableTypes.Length)];
            var pressure = random.Next(0, 101);
            var facing = type == NodeType.Vent ? (HexDirection?)random.Next(0, 6) : null;
            var connections = RandomConnections(random);

            entries.Add((coord, CreateNode(type, pressure, facing, connections)));
        }

        var triggerIndex = random.Next(coords.Length);
        var triggerCoord = coords[triggerIndex];
        var triggerNode = entries[triggerIndex].node;
        if (triggerNode.Type == NodeType.Bulwark)
        {
            triggerNode = triggerNode with
            {
                Type = NodeType.Cell,
                Facing = null
            };
        }

        triggerNode = triggerNode with { Pressure = 100 };
        entries[triggerIndex] = (triggerCoord, triggerNode);

        triggerOrigin = triggerCoord;
        return CreateBoard(entries.ToArray());
    }

    private static ConnectionMask RandomConnections(Random random)
    {
        var mask = ConnectionMask.AllClosed();
        foreach (var direction in Enum.GetValues<HexDirection>())
        {
            if (random.NextDouble() >= 0.5d)
            {
                mask = mask.With(direction, open: true);
            }
        }

        return mask;
    }
}
