// Replays a MoveRecord backwards to restore the previous board state.

namespace Polarity.Core
{
    public static class MoveReverter
    {
        public static void Revert(GridModel grid, MoveRecord record)
        {
            for (int i = 0; i < record.Moves.Count; i++)
                grid.ClearCell(record.Moves[i].ToCell);

            for (int i = 0; i < record.Moves.Count; i++)
            {
                TileMove move = record.Moves[i];
                grid.PlaceTile(move.TileId, move.FromCell);
            }

            for (int i = 0; i < record.NeutronBreaks.Count; i++)
            {
                NeutronBreak broken = record.NeutronBreaks[i];
                grid.ReviveTile(broken.TileId, broken.Cell);
            }

            for (int i = 0; i < record.Annihilations.Count; i++)
            {
                Annihilation blast = record.Annihilations[i];
                grid.ReviveTile(blast.SunId, blast.SunFromCell);
                grid.ReviveTile(blast.MoonId, blast.MoonFromCell);
            }
        }
    }
}
