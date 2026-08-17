// One playthrough: board, score, move budget, win/lose and undo.

namespace Polarity.Core
{
    public sealed class GameSession
    {
        public GridModel Grid { get; }
        public GameConfig Config { get; private set; }

        public int Score { get; private set; }
        public int MovesRemaining { get; private set; }

        // Shortest clear the generator could prove for this board.
        public int ParMoves { get; private set; }

        public int MoveBudget => ParMoves + Config.MoveSlack;
        public int MovesUsed => MoveBudget - MovesRemaining;
        public GameState State { get; private set; }

        public bool CanUndo => _history.CanUndo;
        public bool IsOver => State != GameState.Playing;

        // Lost because the board can no longer be cleared, rather than because the
        // moves ran out. Lets the result screen say which it was.
        public bool IsDeadPosition { get; private set; }

        private readonly MoveResolver _resolver;
        private readonly MoveHistory _history = new MoveHistory();

        private readonly MoveRecord _probe = new MoveRecord();

        private MoveRecord _pendingRecycle;

        public GameSession(GameConfig config)
        {
            Config = config;
            Grid = new GridModel(config.Width, config.Height);
            _resolver = new MoveResolver(Grid);

            Restart(config);
        }

        public void Restart(GameConfig config)
        {
            Config = config;

            ReleasePending();
            _history.Clear();

            ParMoves = BoardGenerator.Populate(Grid, config, _resolver);

            Score = 0;
            MovesRemaining = MoveBudget;
            State = GameState.Playing;
        }

        public MoveRecord TryMove(Direction direction)
        {
            ReleasePending();

            if (State != GameState.Playing) return null;

            MoveRecord record = _history.Rent();
            _resolver.Resolve(direction, record);

            if (!record.Changed)
            {
                _history.Recycle(record);
                return null;
            }

            record.ScoreDelta = ScoreRules.Evaluate(record.PairCount, record.NeutronBreaks.Count);
            Score += record.ScoreDelta;
            MovesRemaining--;

            _history.Push(record);
            RefreshState();

            return record;
        }

        public MoveRecord Undo()
        {
            ReleasePending();

            if (!_history.CanUndo) return null;

            MoveRecord record = _history.Pop();
            MoveReverter.Revert(Grid, record);

            Score -= record.ScoreDelta;
            MovesRemaining++;
            State = GameState.Playing;
            IsDeadPosition = false;

            _pendingRecycle = record;
            return record;
        }

        private void RefreshState()
        {
            IsDeadPosition = false;

            if (Grid.IsCleared)
            {
                State = GameState.Won;
                return;
            }

            if (MovesRemaining <= 0 || !_resolver.HasLegalMove(_probe))
            {
                State = GameState.Lost;
                return;
            }

            // A position can still have legal moves and yet have no route to a
            // clear, which is what made the game feel unfair. Searching to the
            // remaining move count settles it: no clear inside the moves left
            // means the game is genuinely over, so say so rather than letting the
            // player grind. Only a conclusive search counts; if the search runs
            // out of budget it says nothing and play continues.
            Solvability outlook = BoardSolver.Analyse(Grid, _resolver, out _, MovesRemaining);

            if (outlook == Solvability.Unsolvable)
            {
                State = GameState.Lost;
                IsDeadPosition = true;
                return;
            }

            State = GameState.Playing;
        }

        private void ReleasePending()
        {
            if (_pendingRecycle == null) return;

            _history.Recycle(_pendingRecycle);
            _pendingRecycle = null;
        }
    }
}
