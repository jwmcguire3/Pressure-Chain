using PressureChain.Core.Actions;
using PressureChain.Core.Chains;
using PressureChain.Core.Levels;
using PressureChain.Core.Solver;

namespace PressureChain.Core.Tests.Levels;

public sealed class Phase1LevelCatalogTests
{
    private readonly ActionResolver _actionResolver = new(new ChainResolver());
    private readonly BruteForceSolver _solver = new(new LevelEngine(new ActionResolver(new ChainResolver())));

    [Fact]
    public void AuthoredPhase1Levels_AreSolvableWithinMoveCap_AndMeetDistinctSolutionGate()
    {
        foreach (var level in Phase1LevelCatalog.All)
        {
            var result = _solver.Solve(level.CreateInitialState(), level.SolverMaxDepth);

            Assert.True(result.Solvable, $"{level.Id} should be solvable.");
            Assert.True(result.MinMovesUsed <= level.MoveCap, $"{level.Id} exceeded its move cap.");
            Assert.True(
                result.DistinctSolutionsFound >= level.MinimumDistinctSolutions,
                $"{level.Id} found {result.DistinctSolutionsFound} solutions, expected at least {level.MinimumDistinctSolutions}.");
        }
    }

    [Fact]
    public void AuthoredPhase1Levels_DemonstrationActions_AchieveTheirWaveTargets()
    {
        foreach (var level in Phase1LevelCatalog.All)
        {
            var state = level.CreateInitialState();
            var largestWaveCount = 0;

            foreach (var action in level.DemonstrationActions)
            {
                var outcome = _actionResolver.ApplyDetailed(state.Board, action);
                if (outcome.ChainResolution is not null)
                {
                    largestWaveCount = Math.Max(largestWaveCount, outcome.ChainResolution.Value.Waves.Count);
                }

                state = new LevelEngine(_actionResolver).PlayAction(state, action);
            }

            Assert.True(state.Status == LevelStatus.Won, $"{level.Id} demonstration should complete the level.");
            Assert.True(
                largestWaveCount >= level.MinimumDemonstratedWaveCount,
                $"{level.Id} demonstration produced {largestWaveCount} waves, expected at least {level.MinimumDemonstratedWaveCount}.");
        }
    }
}
