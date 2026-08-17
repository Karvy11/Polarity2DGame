// Tests for the swipe resolver, run with no scene loaded.

using NUnit.Framework;
using Polarity.Core;
using static Polarity.Core.Tests.BoardAscii;

namespace Polarity.Core.Tests
{
    [TestFixture]
    public class MoveResolverTests
    {

        [Test]
        public void Sun_TravelsInSwipeDirection()
        {
            var grid = Board(
                "....",
                "....",
                ".S..",
                "....");

            Swipe(grid, Direction.Up);

            AssertBoard(grid,
                ".S..",
                "....",
                "....",
                "....");
        }

        [Test]
        public void Moon_TravelsAgainstSwipeDirection()
        {
            var grid = Board(
                "....",
                ".M..",
                "....",
                "....");

            Swipe(grid, Direction.Up);

            AssertBoard(grid,
                "....",
                "....",
                "....",
                ".M..");
        }

        [Test]
        public void Neutron_NeverMoves()
        {
            var grid = Board(
                "..",
                "N.");

            MoveRecord record = Swipe(grid, Direction.Up);

            AssertBoard(grid,
                "..",
                "N.");
            Assert.That(record.Changed, Is.False, "A lone neutron cannot produce a board change.");
        }

        [Test]
        public void Suns_AndMoons_SeparateToOppositeWalls()
        {
            var grid = Board(
                ".",
                "S",
                "M",
                ".");

            Swipe(grid, Direction.Up);

            AssertBoard(grid,
                "S",
                ".",
                ".",
                "M");
        }

        [Test]
        public void MoonAheadOfSun_Annihilates()
        {
            var grid = Board(
                "M",
                ".",
                "S");

            MoveRecord record = Swipe(grid, Direction.Up);

            AssertBoard(grid,
                ".",
                ".",
                ".");
            Assert.That(record.PairCount, Is.EqualTo(1));
            Assert.That(grid.IsCleared, Is.True);
        }

        [Test]
        public void SunAheadOfMoon_NeverMeets()
        {
            var grid = Board(
                "S",
                ".",
                "M");

            MoveRecord record = Swipe(grid, Direction.Up);

            Assert.That(record.PairCount, Is.EqualTo(0),
                "The sun is already ahead of the moon, so they move apart.");
            Assert.That(grid.LiveSunCount, Is.EqualTo(1));
            Assert.That(grid.LiveMoonCount, Is.EqualTo(1));
        }

        [Test]
        public void AlternatingPairs_BothAnnihilate()
        {
            var grid = Board(
                ".",
                "M",
                "S",
                ".",
                "M",
                "S");

            MoveRecord record = Swipe(grid, Direction.Up);

            Assert.That(record.PairCount, Is.EqualTo(2));
            Assert.That(grid.IsCleared, Is.True);
        }

        [Test]
        public void Sun_MatchesNearestMoon_NotFurthest()
        {
            var grid = Board(
                "S",
                "M",
                "M",
                "S");

            MoveRecord record = Swipe(grid, Direction.Up);

            Assert.That(record.PairCount, Is.EqualTo(1));
            AssertBoard(grid,
                "S",
                ".",
                ".",
                "M");
        }

        [Test]
        public void SurvivingTiles_KeepTheirRelativeOrder()
        {
            var grid = Board(
                "S",
                ".",
                "S",
                ".");

            var first = grid.TileAt(grid.CellIndex(0, 3));
            var second = grid.TileAt(grid.CellIndex(0, 1));

            Swipe(grid, Direction.Up);

            Assert.That(grid.TileAt(grid.CellIndex(0, 3)), Is.EqualTo(first));
            Assert.That(grid.TileAt(grid.CellIndex(0, 2)), Is.EqualTo(second));
        }

        [Test]
        public void Neutron_PreventsACrossingThatWouldOtherwiseHappen()
        {
            var withoutNeutron = Board(
                "M",
                ".",
                "S");

            Assert.That(Swipe(withoutNeutron, Direction.Up).PairCount, Is.EqualTo(1),
                "Control: without the neutron these two cross.");

            var withNeutron = Board(
                "M",
                "N",
                "S");

            MoveRecord record = Swipe(withNeutron, Direction.Up);

            Assert.That(record.PairCount, Is.EqualTo(0));
            Assert.That(record.Changed, Is.False, "Both tiles are already flush against their segment walls.");
            AssertBoard(withNeutron,
                "M",
                "N",
                "S");
        }

        [Test]
        public void Neutron_BreaksWhenABlastGoesOffBesideIt()
        {
            var grid = Board(
                "MN",
                "S.");

            MoveRecord record = Swipe(grid, Direction.Up);

            Assert.That(record.PairCount, Is.EqualTo(1));
            Assert.That(record.NeutronBreaks.Count, Is.EqualTo(1));
            AssertBoard(grid,
                "..",
                "..");
        }

        [Test]
        public void Neutron_SurvivesABlastThatIsNotAdjacent()
        {
            var grid = Board(
                "M.",
                "..",
                "S.",
                ".N");

            MoveRecord record = Swipe(grid, Direction.Up);

            Assert.That(record.PairCount, Is.EqualTo(1));
            Assert.That(record.NeutronBreaks.Count, Is.EqualTo(0));
            Assert.That(grid.TypeAt(grid.CellIndex(1, 0)), Is.EqualTo(TileType.Neutron));
        }

        [Test]
        public void Neutron_SplitsALaneIntoIndependentSegments()
        {
            var grid = Board(
                "M",
                "S",
                "N",
                "M",
                "S");

            MoveRecord record = Swipe(grid, Direction.Up);

            Assert.That(record.PairCount, Is.EqualTo(2),
                "One pair annihilates above the neutron and one below.");
        }

        [Test]
        public void SwipeRight_MirrorsSwipeUp()
        {
            var grid = Board("SM");

            MoveRecord record = Swipe(grid, Direction.Right);

            Assert.That(record.PairCount, Is.EqualTo(1));
            AssertBoard(grid, "..");
        }

        [Test]
        public void SwipeRight_WithTilesAlreadyParked_DoesNothing()
        {
            var grid = Board("MS");

            MoveRecord record = Swipe(grid, Direction.Right);

            Assert.That(record.Changed, Is.False);
            AssertBoard(grid, "MS");
        }

        [Test]
        public void SwipeLeft_SendsSunsLeft()
        {
            var grid = Board("..S.");

            Swipe(grid, Direction.Left);

            AssertBoard(grid, "S...");
        }

        [Test]
        public void Lanes_ResolveIndependently()
        {
            var grid = Board(
                "MS",
                "SM");

            MoveRecord record = Swipe(grid, Direction.Up);

            Assert.That(record.PairCount, Is.EqualTo(1), "Only the left column has a crossing.");
            AssertBoard(grid,
                ".S",
                ".M");
        }

        [Test]
        public void RepeatingTheSameSwipe_ProducesNoSecondChange()
        {
            var grid = Board(
                "..",
                "S.",
                ".M",
                "..");

            var resolver = new MoveResolver(grid);
            var record = new MoveRecord();

            resolver.Resolve(Direction.Up, record);
            Assert.That(record.Changed, Is.True);

            resolver.Resolve(Direction.Up, record);
            Assert.That(record.Changed, Is.False, "The board is already settled in that direction.");
        }

        [Test]
        public void EmptyBoard_ReportsNoChange()
        {
            var grid = Board(
                "..",
                "..");

            Assert.That(Swipe(grid, Direction.Up).Changed, Is.False);
        }

        [Test]
        public void MoveRecord_DescribesEveryTileThatMoved()
        {
            var grid = Board(
                "..",
                ".S");

            MoveRecord record = Swipe(grid, Direction.Up);

            Assert.That(record.Moves.Count, Is.EqualTo(1));
            Assert.That(record.Moves[0].FromCell, Is.EqualTo(grid.CellIndex(1, 0)));
            Assert.That(record.Moves[0].ToCell, Is.EqualTo(grid.CellIndex(1, 1)));
        }

        [Test]
        public void AnnihilatedTiles_AreNotAlsoReportedAsMoves()
        {
            var grid = Board(
                "M",
                "S");

            MoveRecord record = Swipe(grid, Direction.Up);

            Assert.That(record.PairCount, Is.EqualTo(1));
            Assert.That(record.Moves.Count, Is.EqualTo(0),
                "A destroyed tile is described by its annihilation, not by a move.");
        }

        [Test]
        public void LiveCounts_TrackAnnihilations()
        {
            var grid = Board(
                "MM",
                "SS");

            Assert.That(grid.LiveSunCount, Is.EqualTo(2));
            Assert.That(grid.LiveMoonCount, Is.EqualTo(2));

            Swipe(grid, Direction.Up);

            Assert.That(grid.LiveSunCount, Is.EqualTo(0));
            Assert.That(grid.LiveMoonCount, Is.EqualTo(0));
            Assert.That(grid.IsCleared, Is.True);
        }

        [Test]
        public void Resolution_IsDeterministic()
        {
            string[] layout =
            {
                "M.SN",
                ".SM.",
                "NM.S",
                "S.MN"
            };

            var first = Board(layout);
            var second = Board(layout);

            MoveRecord firstRecord = Swipe(first, Direction.Left);
            MoveRecord secondRecord = Swipe(second, Direction.Left);

            Assert.That(second.ToAscii(), Is.EqualTo(first.ToAscii()));
            Assert.That(secondRecord.PairCount, Is.EqualTo(firstRecord.PairCount));
        }

        [Test]
        public void NonSquareBoards_ResolveOnBothAxes()
        {
            var grid = Board(
                "M..",
                "S..");

            Assert.That(Swipe(grid, Direction.Up).PairCount, Is.EqualTo(1));

            var wide = Board("SM.....");

            Assert.That(Swipe(wide, Direction.Right).PairCount, Is.EqualTo(1));
        }
    }
}
