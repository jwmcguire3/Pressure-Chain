using PressureChain.Core.Board;
using PressureChain.Core.Chains;
using PressureChain.Core.Grid;
using PressureChain.Core.Levels;

namespace PressureChain.Core.Tests.Levels;

public sealed class ChainScorerTests
{
    [Fact]
    public void Score_OneWaveChainClearingThreeCells_ReturnsThirty()
    {
        var first = new HexCoord(0, 0);
        var second = new HexCoord(1, 0);
        var third = new HexCoord(2, 0);
        var board = CreateBoard(
            (first, CreateNode(NodeType.Cell, 0)),
            (second, CreateNode(NodeType.Cell, 0)),
            (third, CreateNode(NodeType.Cell, 0)));

        var resolution = new ChainResolution(
            Waves:
            [
                [
                    new BurstEvent(first, Wave: 0, WasChained: false),
                    new BurstEvent(second, Wave: 0, WasChained: false),
                    new BurstEvent(third, Wave: 0, WasChained: false)
                ]
            ],
            FinalBoard: board,
            TotalBurstCount: 3);

        var score = ChainScorer.Score(resolution);

        Assert.Equal(30, score);
    }

    [Fact]
    public void Score_ThreeWaveChainClearingSixCells_ReturnsOneHundredTwo()
    {
        var coords = Enumerable.Range(0, 6).Select(index => new HexCoord(index, 0)).ToArray();
        var board = CreateBoard(coords.Select(coord => (coord, CreateNode(NodeType.Cell, 0))).ToArray());

        var resolution = new ChainResolution(
            Waves:
            [
                [new BurstEvent(coords[0], Wave: 0, WasChained: false)],
                [new BurstEvent(coords[1], Wave: 1, WasChained: true), new BurstEvent(coords[2], Wave: 1, WasChained: true)],
                [
                    new BurstEvent(coords[3], Wave: 2, WasChained: true),
                    new BurstEvent(coords[4], Wave: 2, WasChained: true),
                    new BurstEvent(coords[5], Wave: 2, WasChained: true)
                ]
            ],
            FinalBoard: board,
            TotalBurstCount: 6);

        var score = ChainScorer.Score(resolution);

        Assert.Equal(102, score);
    }

    [Fact]
    public void Score_AmplifierInWaveOne_MultipliesDownstreamWaveScoresByOnePointFive()
    {
        var amplifier = new HexCoord(0, 0);
        var downstreamOne = new HexCoord(1, 0);
        var downstreamTwo = new HexCoord(2, 0);
        var board = CreateBoard(
            (amplifier, CreateNode(NodeType.Amplifier, 0)),
            (downstreamOne, CreateNode(NodeType.Cell, 0)),
            (downstreamTwo, CreateNode(NodeType.Cell, 0)));

        var resolution = new ChainResolution(
            Waves:
            [
                [new BurstEvent(amplifier, Wave: 0, WasChained: false)],
                [
                    new BurstEvent(downstreamOne, Wave: 1, WasChained: true),
                    new BurstEvent(downstreamTwo, Wave: 1, WasChained: true)
                ]
            ],
            FinalBoard: board,
            TotalBurstCount: 3);

        var score = ChainScorer.Score(resolution);

        Assert.Equal(52, score);
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

    private static Node CreateNode(NodeType type, int pressure)
    {
        return new Node(
            type,
            pressure,
            Facing: null,
            Connections: ConnectionMask.AllOpen(),
            NodeModifiers.None);
    }
}
