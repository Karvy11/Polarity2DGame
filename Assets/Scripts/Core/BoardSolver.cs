// Decides whether a board can actually be cleared, and in how few moves.

using System.Collections.Generic;
using System.Text;

namespace Polarity.Core
{
    public enum Solvability : byte
    {
        Solvable = 0,

        // Searched every reachable position and none of them clear the board.
        Unsolvable = 1,

        // Ran out of search budget. Not a verdict, just an admission.
        Unknown = 2
    }

    public static class BoardSolver
    {
        public const int DefaultMaxMoves = 16;
        public const int DefaultNodeBudget = 40000;

        public static Solvability Analyse(
            GridModel grid, MoveResolver resolver, out int minimumMoves,
            int maxMoves = DefaultMaxMoves, int nodeBudget = DefaultNodeBudget)
        {
            minimumMoves = 0;
            if (grid.IsCleared) return Solvability.Solvable;
            if (maxMoves <= 0) return Solvability.Unsolvable;

            var search = new Search(grid, resolver, maxMoves, nodeBudget);
            bool conclusive = true;

            for (int limit = 1; limit <= maxMoves; limit++)
            {
                if (search.CanClearWithin(limit))
                {
                    minimumMoves = limit;
                    return Solvability.Solvable;
                }

                conclusive &= !search.RanOutOfBudget;
            }

            return conclusive ? Solvability.Unsolvable : Solvability.Unknown;
        }

        public static bool TryFindMinimumMoves(
            GridModel grid, MoveResolver resolver, out int minimumMoves,
            int maxMoves = DefaultMaxMoves, int nodeBudget = DefaultNodeBudget) =>
            Analyse(grid, resolver, out minimumMoves, maxMoves, nodeBudget) == Solvability.Solvable;

        public static bool IsSolvable(GridModel grid, MoveResolver resolver,
            int maxMoves = DefaultMaxMoves, int nodeBudget = DefaultNodeBudget) =>
            TryFindMinimumMoves(grid, resolver, out _, maxMoves, nodeBudget);

        private sealed class Search
        {
            private readonly GridModel _grid;
            private readonly MoveResolver _resolver;
            private readonly MoveRecord[] _records;
            private readonly Dictionary<string, int> _exhausted = new Dictionary<string, int>();
            private readonly StringBuilder _signature;
            private readonly int _nodeBudget;

            private int _nodes;

            public bool RanOutOfBudget { get; private set; }

            public Search(GridModel grid, MoveResolver resolver, int maxMoves, int nodeBudget)
            {
                _grid = grid;
                _resolver = resolver;
                _nodeBudget = nodeBudget;
                _signature = new StringBuilder(grid.CellCount);

                _records = new MoveRecord[maxMoves + 1];
                for (int i = 0; i < _records.Length; i++) _records[i] = new MoveRecord();
            }

            public bool CanClearWithin(int limit)
            {
                _exhausted.Clear();
                _nodes = 0;
                RanOutOfBudget = false;

                return Descend(0, limit);
            }

            private bool Descend(int depth, int limit)
            {
                if (_grid.IsCleared) return true;

                int remaining = limit - depth;
                if (remaining == 0) return false;

                if (++_nodes > _nodeBudget)
                {
                    RanOutOfBudget = true;
                    return false;
                }

                string key = Signature();
                if (_exhausted.TryGetValue(key, out int deepestFailure) && deepestFailure >= remaining)
                    return false;

                MoveRecord record = _records[depth];

                for (int direction = 0; direction < 4; direction++)
                {
                    _resolver.Resolve((Direction)direction, record);

                    bool changed = record.Changed;
                    bool solved = changed && Descend(depth + 1, limit);

                    MoveReverter.Revert(_grid, record);

                    if (solved) return true;
                }

                _exhausted[key] = remaining;
                return false;
            }

            private string Signature()
            {
                _signature.Length = 0;

                for (int cell = 0; cell < _grid.CellCount; cell++)
                    _signature.Append((char)('0' + (int)_grid.TypeAt(cell)));

                return _signature.ToString();
            }
        }
    }
}
