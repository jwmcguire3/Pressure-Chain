using PressureChain.Core.Board;
using PressureChain.Core.Chains;
using PressureChain.Core.Grid;
using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Core.Actions;

public sealed class ActionResolver
{
    private const string MissingNodeReason = "Action target must be on the board.";
    private const string MergeAdjacencyReason = "Merge requires adjacent nodes.";
    private const string MergeTypeReason = "Merge requires nodes of the same type.";
    private const string MergeBulwarkReason = "Bulwarks cannot be merged.";
    private const string MergeOverflowReason = "Merge cannot exceed 100 total pressure.";
    private const string VentRedirectReason = "Vent redirect requires a vent target.";
    private const string TriggerEarlyPressureReason = "Trigger early requires pressure 75 or higher.";
    private const string TriggerEarlyBulwarkReason = "Bulwarks cannot be triggered early.";

    private readonly ChainResolver _chainResolver;

    public ActionResolver(ChainResolver chainResolver)
    {
        _chainResolver = chainResolver ?? throw new ArgumentNullException(nameof(chainResolver));
    }

    public GameBoard Apply(GameBoard board, PlayerAction action)
    {
        return ApplyDetailed(board, action).Board;
    }

    public ActionOutcome ApplyDetailed(GameBoard board, PlayerAction action)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(action);

        return action switch
        {
            MergeAction mergeAction => new ActionOutcome(ApplyMerge(board, mergeAction), ChainResolution: null),
            VentRedirectAction ventRedirectAction => new ActionOutcome(ApplyVentRedirect(board, ventRedirectAction), ChainResolution: null),
            TriggerEarlyAction triggerEarlyAction => ApplyTriggerEarly(board, triggerEarlyAction),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported player action.")
        };
    }

    private static GameBoard ApplyMerge(GameBoard board, MergeAction action)
    {
        var nodeA = RequireNode(board, action.A);
        var nodeB = RequireNode(board, action.B);

        if (action.A.DistanceTo(action.B) != 1)
        {
            throw new InvalidActionException(MergeAdjacencyReason);
        }

        if (nodeA.Type == NodeType.Bulwark || nodeB.Type == NodeType.Bulwark)
        {
            throw new InvalidActionException(MergeBulwarkReason);
        }

        if (nodeA.Type != nodeB.Type)
        {
            throw new InvalidActionException(MergeTypeReason);
        }

        if (nodeA.Pressure + nodeB.Pressure > 100)
        {
            throw new InvalidActionException(MergeOverflowReason);
        }

        var clearedNode = nodeA with
        {
            Type = NodeType.Cell,
            Pressure = 0,
            Facing = null
        };

        var mergedPressure = nodeA.Pressure + nodeB.Pressure;
        var mergedNode = nodeB with { Pressure = mergedPressure };

        return board
            .WithNode(action.A, clearedNode)
            .WithNode(action.B, mergedNode);
    }

    private static GameBoard ApplyVentRedirect(GameBoard board, VentRedirectAction action)
    {
        var targetNode = RequireNode(board, action.Target);
        if (targetNode.Type != NodeType.Vent)
        {
            throw new InvalidActionException(VentRedirectReason);
        }

        return board.WithNode(action.Target, targetNode with { Facing = action.NewFacing });
    }

    private ActionOutcome ApplyTriggerEarly(GameBoard board, TriggerEarlyAction action)
    {
        var targetNode = RequireNode(board, action.Target);
        if (targetNode.Type == NodeType.Bulwark)
        {
            throw new InvalidActionException(TriggerEarlyBulwarkReason);
        }

        if (targetNode.Pressure < 75)
        {
            throw new InvalidActionException(TriggerEarlyPressureReason);
        }

        var releasePressure = targetNode.Pressure * 75 / 100;
        var resolution = _chainResolver.Resolve(board, action.Target, initialReleasePressureOverride: releasePressure);

        return new ActionOutcome(resolution.FinalBoard, resolution);
    }

    private static Node RequireNode(GameBoard board, HexCoord coord)
    {
        if (!board.Coords.Contains(coord))
        {
            throw new InvalidActionException(MissingNodeReason);
        }

        return board.NodeAt(coord);
    }
}
