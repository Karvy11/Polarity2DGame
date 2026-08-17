// Tests that boards handed to the player can actually be cleared.

using NUnit.Framework;
using Polarity.Core;
using static Polarity.Core.Tests.BoardAscii;

namespace Polarity.Core.Tests
{
    [TestFixture]
    public class BoardSolverTests
    {
        [Test]
        public void ACrossingPair_IsSolvableInOneMove()
        {
            var grid = Board(
                "M",
                "S");

            Assert.That(BoardSolver.TryFindMinimumMoves(grid, new MoveResolver(grid), out int moves), Is.True);
            Assert.That(moves, Is.EqualTo(1));
        }

        [Test]
        public void ASeparatedPair_StillSolvesByReversingDirection()
        {
            // Sun already above the moon: swiping up drives them apart, swiping
            // down brings them together.
            var grid = Board(
                "S",
                ".",
                "M");

            Assert.That(BoardSolver.TryFindMinimumMoves(grid, new MoveResolver(grid), out int moves), Is.True);
            Assert.That(moves, Is.EqualTo(1));
        }

        [Test]
        public void OppositeCornersOnATwoByTwo_AreUnsolvable()
        {
            // The deadlock this solver exists to catch. Every state has a legal
            // move, but the two tiles never share a lane at the moment they would
            // cross, so the board cycles forever.
            var grid = Board(
                ".M",
                "S.");

            Assert.That(BoardSolver.IsSolvable(grid, new MoveResolver(grid)), Is.False);
        }

        [Test]
        public void SolvingLeavesTheBoardUntouched()
        {
            var grid = Board(
                "M.S.",
                ".SM.",
                "S..M",
                ".M.S");

            string before = grid.ToAscii();
            BoardSolver.IsSolvable(grid, new MoveResolver(grid));

            Assert.That(grid.ToAscii(), Is.EqualTo(before),
                "The search must revert every move it tries.");
        }

        [Test]
        public void ANeutronCanMakeABoardUnsolvable()
        {
            // The only pair is walled off from each other permanently.
            var grid = Board(
                "M",
                "N",
                "S");

            Assert.That(BoardSolver.IsSolvable(grid, new MoveResolver(grid)), Is.False);
        }

        [Test]
        public void AnAlreadyClearBoardNeedsNoMoves()
        {
            var grid = Board(
                "..",
                "..");

            Assert.That(BoardSolver.TryFindMinimumMoves(grid, new MoveResolver(grid), out int moves), Is.True);
            Assert.That(moves, Is.EqualTo(0));
        }

        [Test]
        public void EveryGeneratedBoard_CanActuallyBeCleared()
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                var config = new GameConfig(
                    width: 5, height: 6, pairCount: 6, neutronCount: 2, moveSlack: 6, seed: seed);

                var session = new GameSession(config);

                Assert.That(session.ParMoves, Is.GreaterThan(0), $"Seed {seed} reported a par of zero.");
                Assert.That(BoardSolver.IsSolvable(session.Grid, new MoveResolver(session.Grid)), Is.True,
                    $"Seed {seed} produced a board that cannot be cleared.");
            }
        }

        [Test]
        public void EveryGeneratedBoard_CanBeClearedInsideItsBudget()
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                var config = new GameConfig(
                    width: 5, height: 6, pairCount: 6, neutronCount: 2, moveSlack: 6, seed: seed);

                var session = new GameSession(config);

                Assert.That(session.MoveBudget, Is.GreaterThanOrEqualTo(session.ParMoves),
                    $"Seed {seed} cannot be won inside its own move budget.");
            }
        }

        [Test]
        public void ParIsTheShortestClear_NotJustAnyClear()
        {
            var config = new GameConfig(
                width: 4, height: 4, pairCount: 3, neutronCount: 0, moveSlack: 5, seed: 5);

            var session = new GameSession(config);
            var resolver = new MoveResolver(session.Grid);

            Assert.That(
                BoardSolver.IsSolvable(session.Grid, resolver, maxMoves: session.ParMoves - 1),
                Is.False,
                "If it solves in fewer moves than par, par is not the minimum.");
        }
    }
}
