using PressureChain.Core.Actions;
using PressureChain.Core.Levels;

namespace PressureChain.Core.Telemetry;

public interface IActionLogger
{
    void LogAction(PlayerAction action, LevelState before, LevelState after);

    void LogLevelStart(LevelState initial);

    void LogLevelEnd(LevelStatus outcome, int finalScore);
}
