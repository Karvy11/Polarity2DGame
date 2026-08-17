// Tests that replaying a MoveRecord backwards restores the exact previous state.

using System.Collections.Generic;
using NUnit.Framework;
using Polarity.Core;
using static Polarity.Core.Tests.BoardAscii;

namespace Polarity.Core.Tests
{
    [TestFixture]
    public class UndoTests
    {
        [Test]
        public void RevertingOneMove_RestoresTheBoard()
        {
            var grid = Board(
                "M.",
                "..",
                "S.");

            string opening = grid.ToAscii();

            var record = new MoveRecord();
            new MoveResolver(grid).Resolve(Direction.Up, record);

            Assert.That(grid.ToAscii(), Is.Not.EqualTo(opening), "Sanity: the move did something.");

            MoveReverter.Revert(grid, record);

            Assert.That(grid.ToAscii(), Is.EqualTo(opening));
        }

        [Test]
        public void RevertingRestoresLiveCounts()
        {
            var grid = Board(
                "MM",
                "SS");

            var record = new MoveRecord();
            new MoveResolver(grid).Resolve(Direction.Up, record);
            Assert.That(grid.IsCleared, Is.True);

            MoveReverter.Revert(grid, record);

            Assert.That(grid.LiveSunCount, Is.EqualTo(2));
            Assert.That(grid.LiveMoonCount, Is.EqualTo(2));
            Assert.That(grid.IsCleared, Is.False);
        }

        [Test]
        public void RevertingRestoresABrokenNeutron()
        {
            var grid = Board(
                "MN",
                "S.");

            string opening = grid.ToAscii();

            var record = new MoveRecord();
            new MoveResolver(grid).Resolve(Direction.Up, record);
            Assert.That(record.NeutronBreaks.Count, Is.EqualTo(1));

            MoveReverter.Revert(grid, record);

            Assert.That(grid.ToAscii(), Is.EqualTo(opening));
        }

        [Test]
        public void RevertingRestoresTileIdentity_NotJustLayout()
        {
            var grid = Board(
                "..",
                "S.",
                ".S");

            int topSun = grid.TileAt(grid.CellIndex(0, 1));
            int lowSun = grid.TileAt(grid.CellIndex(1, 0));

            var record = new MoveRecord();
            new MoveResolver(grid).Resolve(Direction.Up, record);
            MoveReverter.Revert(grid, record);

            Assert.That(grid.TileAt(grid.CellIndex(0, 1)), Is.EqualTo(topSun));
            Assert.That(grid.TileAt(grid.CellIndex(1, 0)), Is.EqualTo(lowSun));
        }

        [Test]
        public void UndoingAWholeGame_RestoresTheOpeningBoard()
        {
            string[] layout =
            {
                "M.SN",
                ".SM.",
                "NM.S",
                "S.MN"
            };

            var grid = Board(layout);
            string opening = grid.ToAscii();
            int openingSuns = grid.LiveSunCount;
            int openingMoons = grid.LiveMoonCount;

            var resolver = new MoveResolver(grid);
            var history = new MoveHistory();
            var applied = new List<MoveRecord>();

            Direction[] script =
            {
                Direction.Up, Direction.Left, Direction.Down,
                Direction.Right, Direction.Up, Direction.Left, Direction.Down
            };

            foreach (Direction direction in script)
            {
                MoveRecord record = history.Rent();
                resolver.Resolve(direction, record);

                if (record.Changed) applied.Add(record);
                else history.Recycle(record);
            }

            Assert.That(applied.Count, Is.GreaterThan(0), "Sanity: the script changed the board.");

            for (int i = applied.Count - 1; i >= 0; i--)
                MoveReverter.Revert(grid, applied[i]);

            Assert.That(grid.ToAscii(), Is.EqualTo(opening));
            Assert.That(grid.LiveSunCount, Is.EqualTo(openingSuns));
            Assert.That(grid.LiveMoonCount, Is.EqualTo(openingMoons));
        }

        [Test]
        public void RevertHandlesTilesSwappingCells()
        {
            var grid = Board(
                "S",
                "S",
                ".",
                ".");

            string opening = grid.ToAscii();

            var record = new MoveRecord();
            new MoveResolver(grid).Resolve(Direction.Down, record);
            MoveReverter.Revert(grid, record);

            Assert.That(grid.ToAscii(), Is.EqualTo(opening));
        }

        [Test]
        public void History_RecyclesRecordsRatherThanAllocating()
        {
            var history = new MoveHistory();

            MoveRecord first = history.Rent();
            history.Recycle(first);
            MoveRecord second = history.Rent();

            Assert.That(second, Is.SameAs(first),
                "A recycled record must come back out of the pool.");
        }

        [Test]
        public void RecycledRecord_ComesBackEmpty()
        {
            var history = new MoveHistory();

            MoveRecord record = history.Rent();
            record.Moves.Add(new TileMove(0, 1, 2));
            record.ScoreDelta = 99;
            history.Recycle(record);

            MoveRecord reused = history.Rent();

            Assert.That(reused.Moves.Count, Is.EqualTo(0));
            Assert.That(reused.ScoreDelta, Is.EqualTo(0));
        }
    }
}
