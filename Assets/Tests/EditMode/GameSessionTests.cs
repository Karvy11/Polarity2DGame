// Tests for scoring, move budget, undo, win and loss.

using NUnit.Framework;
using Polarity.Core;

namespace Polarity.Core.Tests
{
    [TestFixture]
    public class GameSessionTests
    {
        private static GameConfig Config(int moveBudget = 15, int seed = 1) =>
            new GameConfig(width: 6, height: 6, pairCount: 8, neutronCount: 3,
                moveBudget: moveBudget, seed: seed);

        private static MoveRecord PlayAnyLegalMove(GameSession session)
        {
            for (int i = 0; i < 4; i++)
            {
                MoveRecord record = session.TryMove((Direction)i);
                if (record != null) return record;
            }

            return null;
        }

        [Test]
        public void NewSession_StartsPlayableAndUnscored()
        {
            var session = new GameSession(Config());

            Assert.That(session.State, Is.EqualTo(GameState.Playing));
            Assert.That(session.Score, Is.EqualTo(0));
            Assert.That(session.MovesRemaining, Is.EqualTo(15));
            Assert.That(session.MovesUsed, Is.EqualTo(0));
            Assert.That(session.CanUndo, Is.False);
        }

        [Test]
        public void GeneratedBoard_HasEqualSunsAndMoons()
        {
            var session = new GameSession(Config());

            Assert.That(session.Grid.LiveSunCount, Is.EqualTo(session.Grid.LiveMoonCount),
                "A sun can only leave with a moon, so the board must start balanced.");
        }

        [Test]
        public void GeneratedBoard_IsPlayable()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var session = new GameSession(Config(seed: seed));

                Assert.That(session.State, Is.EqualTo(GameState.Playing),
                    $"Seed {seed} produced an unplayable opening board.");
            }
        }

        [Test]
        public void SameSeed_ProducesTheSameBoard()
        {
            var first = new GameSession(Config(seed: 7));
            var second = new GameSession(Config(seed: 7));

            Assert.That(second.Grid.ToAscii(), Is.EqualTo(first.Grid.ToAscii()));
        }

        [Test]
        public void ALegalMove_SpendsExactlyOneMove()
        {
            var session = new GameSession(Config());

            Assert.That(PlayAnyLegalMove(session), Is.Not.Null);
            Assert.That(session.MovesRemaining, Is.EqualTo(14));
            Assert.That(session.MovesUsed, Is.EqualTo(1));
        }

        [Test]
        public void AMoveTheBoardIgnores_CostsNothing()
        {
            var session = new GameSession(Config());

            Direction settled = Direction.Up;
            while (session.TryMove(settled) != null) { }

            int movesBefore = session.MovesRemaining;
            int scoreBefore = session.Score;

            Assert.That(session.TryMove(settled), Is.Null);
            Assert.That(session.MovesRemaining, Is.EqualTo(movesBefore));
            Assert.That(session.Score, Is.EqualTo(scoreBefore));
        }

        [Test]
        public void Undo_RewindsBoardScoreAndBudget()
        {
            var session = new GameSession(Config());

            string opening = session.Grid.ToAscii();
            PlayAnyLegalMove(session);

            Assert.That(session.CanUndo, Is.True);

            session.Undo();

            Assert.That(session.Grid.ToAscii(), Is.EqualTo(opening));
            Assert.That(session.Score, Is.EqualTo(0));
            Assert.That(session.MovesRemaining, Is.EqualTo(15));
            Assert.That(session.CanUndo, Is.False);
        }

        [Test]
        public void Undo_WithNothingToUndo_ReturnsNull()
        {
            var session = new GameSession(Config());

            Assert.That(session.Undo(), Is.Null);
        }

        [Test]
        public void UndoingEveryMove_ReturnsToTheOpeningPosition()
        {
            var session = new GameSession(Config());
            string opening = session.Grid.ToAscii();

            for (int i = 0; i < 6; i++) PlayAnyLegalMove(session);
            while (session.CanUndo) session.Undo();

            Assert.That(session.Grid.ToAscii(), Is.EqualTo(opening));
            Assert.That(session.Score, Is.EqualTo(0));
            Assert.That(session.MovesRemaining, Is.EqualTo(15));
        }

        [Test]
        public void Score_RewardsCombosAboveSinglePairs()
        {
            Assert.That(ScoreRules.Evaluate(1, 0), Is.EqualTo(10));
            Assert.That(ScoreRules.Evaluate(2, 0), Is.EqualTo(45));
            Assert.That(ScoreRules.Evaluate(3, 0), Is.EqualTo(80));

            Assert.That(ScoreRules.Evaluate(2, 0), Is.GreaterThan(2 * ScoreRules.Evaluate(1, 0)),
                "Two pairs in one swipe must beat two swipes of one pair.");
        }

        [Test]
        public void Score_PaysForBrokenNeutrons()
        {
            Assert.That(ScoreRules.Evaluate(1, 1), Is.EqualTo(10 + 40));
        }

        [Test]
        public void Score_IsZeroWithoutAnnihilations()
        {
            Assert.That(ScoreRules.Evaluate(0, 0), Is.EqualTo(0));
        }

        [Test]
        public void RunningOutOfMoves_LosesTheGame()
        {
            var session = new GameSession(Config(moveBudget: 2));

            while (session.State == GameState.Playing && PlayAnyLegalMove(session) != null) { }

            Assert.That(session.State, Is.Not.EqualTo(GameState.Playing));

            if (session.State == GameState.Lost)
                Assert.That(session.MovesRemaining == 0 || !session.Grid.IsCleared, Is.True);
        }

        [Test]
        public void ClearingTheBoard_WinsTheGame()
        {
            var session = new GameSession(new GameConfig(
                width: 2, height: 1, pairCount: 1, neutronCount: 0, moveBudget: 5, seed: 3));

            session.TryMove(Direction.Right);
            if (session.State == GameState.Playing) session.TryMove(Direction.Left);

            Assert.That(session.State, Is.EqualTo(GameState.Won));
            Assert.That(session.Grid.IsCleared, Is.True);
            Assert.That(session.Score, Is.GreaterThan(0));
        }

        [Test]
        public void MovesAfterTheGameEnds_AreRejected()
        {
            var session = new GameSession(new GameConfig(
                width: 2, height: 1, pairCount: 1, neutronCount: 0, moveBudget: 5, seed: 3));

            session.TryMove(Direction.Right);
            if (session.State == GameState.Playing) session.TryMove(Direction.Left);
            Assert.That(session.State, Is.EqualTo(GameState.Won));

            int scoreAtWin = session.Score;

            Assert.That(session.TryMove(Direction.Up), Is.Null);
            Assert.That(session.Score, Is.EqualTo(scoreAtWin));
        }

        [Test]
        public void UndoingAWin_PutsTheGameBackInPlay()
        {
            var session = new GameSession(new GameConfig(
                width: 2, height: 1, pairCount: 1, neutronCount: 0, moveBudget: 5, seed: 3));

            session.TryMove(Direction.Right);
            if (session.State == GameState.Playing) session.TryMove(Direction.Left);
            Assert.That(session.State, Is.EqualTo(GameState.Won));

            session.Undo();

            Assert.That(session.State, Is.EqualTo(GameState.Playing));
            Assert.That(session.Grid.IsCleared, Is.False);
            Assert.That(session.Score, Is.EqualTo(0));
        }

        [Test]
        public void Restart_ResetsEverything()
        {
            var session = new GameSession(Config());
            PlayAnyLegalMove(session);

            session.Restart(Config(seed: 42));

            Assert.That(session.Score, Is.EqualTo(0));
            Assert.That(session.MovesRemaining, Is.EqualTo(15));
            Assert.That(session.State, Is.EqualTo(GameState.Playing));
            Assert.That(session.CanUndo, Is.False);
        }
    }
}
