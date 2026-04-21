using System.Text;
using PressureChain.Core.Actions;
using PressureChain.Core.Board;
using PressureChain.Core.Grid;
using PressureChain.Core.Levels;

namespace PressureChain.Core.Solver;

public sealed class BruteForceSolver
{
    private const int DistinctSolutionCap = 10;

    private readonly LevelEngine _levelEngine;

    public BruteForceSolver(LevelEngine levelEngine)
    {
        _levelEngine = levelEngine ?? throw new ArgumentNullException(nameof(levelEngine));
    }

    public SolverResult Solve(LevelState initial, int maxDepth)
    {
        ArgumentNullException.ThrowIfNull(initial);

        if (maxDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepth), maxDepth, "Maximum search depth cannot be negative.");
        }

        var depthLimit = Math.Min(maxDepth, initial.MovesRemaining);
        var cache = new Dictionary<string, SearchSummary>(StringComparer.Ordinal);
        var summary = Search(initial, depthLimit, cache);

        return new SolverResult(
            Solvable: summary.Solvable,
            MinMovesUsed: summary.MinMovesUsed,
            ExampleSolution: summary.ExampleSolution,
            DistinctSolutionsFound: summary.DistinctSolutionsFound);
    }

    private SearchSummary Search(LevelState state, int depthRemaining, IDictionary<string, SearchSummary> cache)
    {
        if (state.Status == LevelStatus.Won)
        {
            return SearchSummary.Won(Array.Empty<PlayerAction>());
        }

        if (state.Status == LevelStatus.Lost || depthRemaining == 0 || state.MovesRemaining == 0)
        {
            return SearchSummary.Unsolved;
        }

        var cacheKey = CreateCacheKey(state, depthRemaining);
        if (cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var bestMoves = int.MaxValue;
        IReadOnlyList<PlayerAction> bestSolution = Array.Empty<PlayerAction>();
        var distinctSolutionsFound = 0;

        foreach (var action in EnumerateLegalActions(state.Board))
        {
            var nextState = _levelEngine.PlayAction(state, action);
            var childSummary = Search(nextState, depthRemaining - 1, cache);
            if (!childSummary.Solvable)
            {
                continue;
            }

            distinctSolutionsFound = Math.Min(
                DistinctSolutionCap,
                distinctSolutionsFound + childSummary.DistinctSolutionsFound);

            var candidateMoves = childSummary.MinMovesUsed + 1;
            if (candidateMoves >= bestMoves)
            {
                continue;
            }

            bestMoves = candidateMoves;
            bestSolution = PrependAction(action, childSummary.ExampleSolution);
        }

        var summary = distinctSolutionsFound > 0
            ? new SearchSummary(
                Solvable: true,
                MinMovesUsed: bestMoves,
                ExampleSolution: bestSolution,
                DistinctSolutionsFound: distinctSolutionsFound)
            : SearchSummary.Unsolved;

        cache[cacheKey] = summary;
        return summary;
    }

    private static IEnumerable<PlayerAction> EnumerateLegalActions(PressureChain.Core.Board.Board board)
    {
        var coordSet = board.Coords.ToHashSet();

        foreach (var coord in board.Coords)
        {
            var node = board.NodeAt(coord);

            if (node.Type != NodeType.Bulwark)
            {
                foreach (var direction in Enum.GetValues<HexDirection>())
                {
                    var neighbor = coord + direction.Offset();
                    if (!coordSet.Contains(neighbor))
                    {
                        continue;
                    }

                    var neighborNode = board.NodeAt(neighbor);
                    if (neighborNode.Type == node.Type)
                    {
                        yield return new MergeAction(coord, neighbor);
                    }
                }
            }

            if (node.Type == NodeType.Vent)
            {
                foreach (var direction in Enum.GetValues<HexDirection>())
                {
                    yield return new VentRedirectAction(coord, direction);
                }
            }

            if (node.Type != NodeType.Bulwark && node.Pressure >= 50)
            {
                yield return new TriggerEarlyAction(coord);
            }
        }
    }

    private static IReadOnlyList<PlayerAction> PrependAction(PlayerAction action, IReadOnlyList<PlayerAction> suffix)
    {
        var combined = new PlayerAction[suffix.Count + 1];
        combined[0] = action;

        for (var index = 0; index < suffix.Count; index++)
        {
            combined[index + 1] = suffix[index];
        }

        return combined;
    }

    private static string CreateCacheKey(LevelState state, int depthRemaining)
    {
        var builder = new StringBuilder();
        builder.Append(depthRemaining);
        builder.Append('|');
        builder.Append(state.MovesRemaining);
        builder.Append('|');
        builder.Append((int)state.Status);
        builder.Append('|');
        foreach (var clearedCoord in state.ClearedCoords.OrderBy(coord => coord.Q).ThenBy(coord => coord.R))
        {
            builder.Append(clearedCoord.Q);
            builder.Append(',');
            builder.Append(clearedCoord.R);
            builder.Append(';');
        }
        builder.Append('|');

        foreach (var coord in state.Board.Coords)
        {
            var node = state.Board.NodeAt(coord);
            builder.Append(coord.Q);
            builder.Append(',');
            builder.Append(coord.R);
            builder.Append(':');
            builder.Append((int)node.Type);
            builder.Append(',');
            builder.Append(node.Pressure);
            builder.Append(',');
            builder.Append(node.Facing.HasValue ? (int)node.Facing.Value : -1);
            builder.Append(',');
            builder.Append(node.Connections.Bits);
            builder.Append(',');
            builder.Append((int)node.Modifiers);
            builder.Append(';');
        }

        return builder.ToString();
    }

    private readonly record struct SearchSummary(
        bool Solvable,
        int MinMovesUsed,
        IReadOnlyList<PlayerAction> ExampleSolution,
        int DistinctSolutionsFound)
    {
        public static SearchSummary Unsolved { get; } = new(
            Solvable: false,
            MinMovesUsed: 0,
            ExampleSolution: Array.Empty<PlayerAction>(),
            DistinctSolutionsFound: 0);

        public static SearchSummary Won(IReadOnlyList<PlayerAction> solution) => new(
            Solvable: true,
            MinMovesUsed: 0,
            ExampleSolution: solution,
            DistinctSolutionsFound: 1);
    }
}
