using PressureChain.Core.Actions;
using PressureChain.Core.Levels;

namespace PressureChain.Core.Telemetry;

public sealed class NullActionLogger : IActionLogger
{
    public void LogAction(PlayerAction action, LevelState before, LevelState after)
    {
    }

    public void LogLevelStart(LevelState initial)
    {
    }

    public void LogLevelEnd(LevelStatus outcome, int finalScore)
    {
    }
}
