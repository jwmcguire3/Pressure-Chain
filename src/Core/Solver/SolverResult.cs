using PressureChain.Core.Actions;

namespace PressureChain.Core.Solver;

public sealed record SolverResult(
    bool Solvable,
    int MinMovesUsed,
    IReadOnlyList<PlayerAction> ExampleSolution,
    int DistinctSolutionsFound);
