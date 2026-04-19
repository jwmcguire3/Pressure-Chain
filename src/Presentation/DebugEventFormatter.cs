using PressureChain.Core.Actions;
using PressureChain.Core.Board;
using PressureChain.Core.Chains;
using PressureChain.Core.Grid;

namespace PressureChain.Presentation;

internal static class DebugEventFormatter
{
    public static string FormatAction(PlayerAction action)
    {
        return action switch
        {
            MergeAction mergeAction => $"merge {FormatCoord(mergeAction.A)} -> {FormatCoord(mergeAction.B)}",
            VentRedirectAction ventRedirectAction => $"vent rotate {FormatCoord(ventRedirectAction.Target)} -> {ventRedirectAction.NewFacing}",
            TriggerEarlyAction triggerEarlyAction => $"trigger early {FormatCoord(triggerEarlyAction.Target)}",
            _ => action.GetType().Name
        };
    }

    public static string FormatNode(HexCoord coord, Node node)
    {
        var facing = node.Facing is null ? string.Empty : $" facing {node.Facing.Value}";
        return $"{FormatCoord(coord)} {node.Type} P{node.Pressure} {node.State}{facing}";
    }

    public static string FormatChainSummary(ChainResolution resolution)
    {
        var waveParts = resolution.Waves
            .Select((wave, index) => $"W{index}:{string.Join(", ", wave.Select(burst => FormatCoord(burst.Origin)))}");

        return $"chain {resolution.TotalBurstCount} bursts, {resolution.Waves.Count} waves | {string.Join(" | ", waveParts)}";
    }

    private static string FormatCoord(HexCoord coord) => $"({coord.Q},{coord.R})";
}
