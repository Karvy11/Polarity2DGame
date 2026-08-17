// One playthrough: board, score, move budget, win/lose and undo.

namespace Polarity.Core
{
    public sealed class GameSession
    {
        public GridModel Grid { get; }
        public GameConfig Config { get; private set; }

        public int Score { get; private set; }
        public int MovesRemaining { get; private set; }
        public int MovesUsed => Config.MoveBudget - MovesRemaining;
        public GameState State { get; private set; }

        public bool CanUndo => _history.CanUndo;
        public bool IsOver => State != GameState.Playing;

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

            BoardGenerator.Populate(Grid, config, _resolver);

            Score = 0;
            MovesRemaining = config.MoveBudget;
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

            _pendingRecycle = record;
            return record;
        }

        private void RefreshState()
        {
            if (Grid.IsCleared)
            {
                State = GameState.Won;
                return;
            }

            State = MovesRemaining <= 0 || !_resolver.HasLegalMove(_probe)
                ? GameState.Lost
                : GameState.Playing;
        }

        private void ReleasePending()
        {
            if (_pendingRecycle == null) return;

            _history.Recycle(_pendingRecycle);
            _pendingRecycle = null;
        }
    }
}
