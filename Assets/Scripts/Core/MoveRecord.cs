// One move described as a difference: moves, annihilations and neutron breaks.

using System.Collections.Generic;

namespace Polarity.Core
{
    public readonly struct TileMove
    {
        public readonly int TileId;
        public readonly int FromCell;
        public readonly int ToCell;

        public TileMove(int tileId, int fromCell, int toCell)
        {
            TileId = tileId;
            FromCell = fromCell;
            ToCell = toCell;
        }
    }

    public readonly struct Annihilation
    {
        public readonly int SunId;
        public readonly int MoonId;
        public readonly int SunFromCell;
        public readonly int MoonFromCell;

        public readonly int CollisionCell;

        public Annihilation(int sunId, int moonId, int sunFromCell, int moonFromCell, int collisionCell)
        {
            SunId = sunId;
            MoonId = moonId;
            SunFromCell = sunFromCell;
            MoonFromCell = moonFromCell;
            CollisionCell = collisionCell;
        }
    }

    public readonly struct NeutronBreak
    {
        public readonly int TileId;
        public readonly int Cell;

        public NeutronBreak(int tileId, int cell)
        {
            TileId = tileId;
            Cell = cell;
        }
    }

    public sealed class MoveRecord
    {
        public Direction Direction;

        public int ScoreDelta;

        public readonly List<TileMove> Moves = new List<TileMove>();
        public readonly List<Annihilation> Annihilations = new List<Annihilation>();
        public readonly List<NeutronBreak> NeutronBreaks = new List<NeutronBreak>();

        public bool Changed => Moves.Count > 0 || Annihilations.Count > 0;

        public int PairCount => Annihilations.Count;

        public void Reset()
        {
            ScoreDelta = 0;
            Moves.Clear();
            Annihilations.Clear();
            NeutronBreaks.Clear();
        }
    }
}
