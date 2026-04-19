using PressureChain.Core.Actions;
using PressureChain.Core.Chains;
using PressureChain.Core.Levels;
using PressureChain.Core.Solver;

namespace PressureChain.Core.Tests.Levels;

public sealed class Phase1LevelCatalogTests
{
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
}
