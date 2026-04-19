using PressureChain.Core.Levels;

namespace PressureChain.Presentation;

public static class Phase1TestLevelFactory
{
    public static LevelState Create()
    {
        return Phase1LevelCatalog.All[0].CreateInitialState();
    }
}
