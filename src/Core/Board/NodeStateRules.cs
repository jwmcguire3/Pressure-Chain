namespace PressureChain.Core.Board;

public static class NodeStateRules
{
    public static NodeState FromPressure(int pressure)
    {
        if (pressure is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pressure), pressure, "Pressure must be in range 0-100.");
        }

        return pressure switch
        {
            <= 24 => NodeState.Stable,
            <= 49 => NodeState.Swelling,
            <= 74 => NodeState.Critical,
            <= 99 => NodeState.Volatile,
            _ => NodeState.Burst
        };
    }
}
