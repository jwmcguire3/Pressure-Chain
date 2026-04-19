using PressureChain.Core.Board;
using PressureChain.Core.Grid;

namespace PressureChain.Core.Chains;

public sealed class ChainResolver
{
    public ChainResolution Resolve(PressureChain.Core.Board.Board initial, HexCoord triggerOrigin)
    {
        ArgumentNullException.ThrowIfNull(initial);

        _ = initial.NodeAt(triggerOrigin);

        var triggerNode = initial.NodeAt(triggerOrigin);
        if (triggerNode.Type == NodeType.Bulwark)
        {
            throw new InvalidOperationException("Bulwarks cannot burst.");
        }

        if (triggerNode.Pressure < 100)
        {
            throw new InvalidOperationException("Trigger node must already be at burst pressure.");
        }

        var board = initial;
        var burstHistory = new HashSet<HexCoord> { triggerOrigin };
        var waves = new List<IReadOnlyList<BurstEvent>>();
        var currentWave = new List<BurstEvent> { new(triggerOrigin, Wave: 0, WasChained: false) };

        while (currentWave.Count > 0)
        {
            waves.Add(currentWave.AsReadOnly());

            var pressureDeltas = new Dictionary<HexCoord, int>();
            foreach (var burst in currentWave)
            {
                ApplyDelta(pressureDeltas, burst.Origin, -board.NodeAt(burst.Origin).Pressure);

                foreach (var transfer in EnumerateTransfers(board, burst))
                {
                    ApplyDelta(pressureDeltas, transfer.target, transfer.pressure);
                }
            }

            var updatedBoard = board;
            var nextWave = new List<BurstEvent>();
            foreach (var coord in board.Coords)
            {
                var node = board.NodeAt(coord);
                var previousPressure = node.Pressure;
                var pressureDelta = pressureDeltas.GetValueOrDefault(coord);
                var nextPressure = Math.Clamp(previousPressure + pressureDelta, 0, 100);
                var updatedNode = node with { Pressure = nextPressure };

                updatedBoard = updatedBoard.WithNode(coord, updatedNode);

                if (node.Type == NodeType.Bulwark)
                {
                    continue;
                }

                if (previousPressure < 100 &&
                    nextPressure >= 100 &&
                    burstHistory.Add(coord))
                {
                    nextWave.Add(new BurstEvent(coord, waves.Count, WasChained: true));
                }
            }

            board = updatedBoard;
            currentWave = nextWave;
        }

        return new ChainResolution(
            Waves: waves.AsReadOnly(),
            FinalBoard: board,
            TotalBurstCount: burstHistory.Count);
    }

    private static IEnumerable<(HexCoord target, int pressure)> EnumerateTransfers(PressureChain.Core.Board.Board board, BurstEvent burst)
    {
        var node = board.NodeAt(burst.Origin);
        var release = GetReleasePressure(node, burst.WasChained);
        if (release <= 0)
        {
            yield break;
        }

        switch (node.Type)
        {
            case NodeType.Cell:
            case NodeType.Amplifier:
                foreach (var direction in Enum.GetValues<HexDirection>())
                {
                    var pressurePerFace = release / 6;
                    if (pressurePerFace <= 0)
                    {
                        yield break;
                    }

                    if (CanTransfer(board, burst.Origin, direction))
                    {
                        yield return (burst.Origin + direction.Offset(), pressurePerFace);
                    }
                }

                break;
            case NodeType.Vent:
                if (node.Facing is null)
                {
                    throw new InvalidOperationException("Vent nodes must define a facing direction.");
                }

                var facing = node.Facing.Value;
                var forwardPressure = release * 80 / 100;
                if (forwardPressure > 0 && CanTransfer(board, burst.Origin, facing))
                {
                    yield return (burst.Origin + facing.Offset(), forwardPressure);
                }

                var sidePressure = (release - forwardPressure) / 5;
                if (sidePressure <= 0)
                {
                    yield break;
                }

                foreach (var direction in Enum.GetValues<HexDirection>())
                {
                    if (direction == facing)
                    {
                        continue;
                    }

                    if (CanTransfer(board, burst.Origin, direction))
                    {
                        yield return (burst.Origin + direction.Offset(), sidePressure);
                    }
                }

                break;
            case NodeType.Bulwark:
                throw new InvalidOperationException("Bulwarks cannot burst.");
            default:
                throw new ArgumentOutOfRangeException(nameof(node.Type), node.Type, "Unsupported node type.");
        }
    }

    private static int GetReleasePressure(Node node, bool wasChained)
    {
        return node.Type == NodeType.Amplifier && wasChained ? 200 : 100;
    }

    private static bool CanTransfer(PressureChain.Core.Board.Board board, HexCoord origin, HexDirection direction)
    {
        var sourceNode = board.NodeAt(origin);
        if (!sourceNode.Connections.IsOpen(direction))
        {
            return false;
        }

        var target = origin + direction.Offset();
        if (!board.Coords.Contains(target))
        {
            return false;
        }

        var targetNode = board.NodeAt(target);
        if (targetNode.Type == NodeType.Bulwark)
        {
            return false;
        }

        return targetNode.Connections.IsOpen(direction.Opposite());
    }

    private static void ApplyDelta(IDictionary<HexCoord, int> deltas, HexCoord coord, int delta)
    {
        var current = deltas.TryGetValue(coord, out var existingDelta) ? existingDelta : 0;
        deltas[coord] = current + delta;
    }
}
