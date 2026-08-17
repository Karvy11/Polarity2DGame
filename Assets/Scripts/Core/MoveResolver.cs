// Applies one swipe: splits the board into lanes, matches crossings, packs survivors.

namespace Polarity.Core
{
    public sealed class MoveResolver
    {
        private readonly GridModel _grid;

        private readonly int[] _lane;
        private readonly int[] _openMoons;
        private readonly int[] _survivingSuns;
        private readonly int[] _survivingMoons;

        public MoveResolver(GridModel grid)
        {
            _grid = grid;

            int longestLane = grid.Width > grid.Height ? grid.Width : grid.Height;
            _lane = new int[longestLane];
            _openMoons = new int[longestLane];
            _survivingSuns = new int[longestLane];
            _survivingMoons = new int[longestLane];
        }

        public void Resolve(Direction direction, MoveRecord record)
        {
            record.Reset();
            record.Direction = direction;

            bool vertical = direction.IsVertical();
            int laneCount = vertical ? _grid.Width : _grid.Height;
            int laneLength = vertical ? _grid.Height : _grid.Width;

            for (int lane = 0; lane < laneCount; lane++)
            {
                BuildLane(direction, lane, laneLength);
                ResolveLane(laneLength, record);
            }

            BreakNeutronsAdjacentToBlasts(record);
        }

        public bool HasLegalMove(MoveRecord probe)
        {
            for (int i = 0; i < 4; i++)
            {
                Resolve((Direction)i, probe);
                bool changed = probe.Changed;
                MoveReverter.Revert(_grid, probe);

                if (changed) return true;
            }

            return false;
        }

        private void BuildLane(Direction direction, int lane, int laneLength)
        {
            switch (direction)
            {
                case Direction.Up:
                    for (int k = 0; k < laneLength; k++)
                        _lane[k] = _grid.CellIndex(lane, _grid.Height - 1 - k);
                    break;

                case Direction.Down:
                    for (int k = 0; k < laneLength; k++)
                        _lane[k] = _grid.CellIndex(lane, k);
                    break;

                case Direction.Right:
                    for (int k = 0; k < laneLength; k++)
                        _lane[k] = _grid.CellIndex(_grid.Width - 1 - k, lane);
                    break;

                default:
                    for (int k = 0; k < laneLength; k++)
                        _lane[k] = _grid.CellIndex(k, lane);
                    break;
            }
        }

        private void ResolveLane(int laneLength, MoveRecord record)
        {
            int segmentStart = 0;

            for (int k = 0; k <= laneLength; k++)
            {
                bool isBoundary = k == laneLength || _grid.TypeAt(_lane[k]) == TileType.Neutron;
                if (!isBoundary) continue;

                if (k > segmentStart) ResolveSegment(segmentStart, k, record);
                segmentStart = k + 1;
            }
        }

        private void ResolveSegment(int from, int to, MoveRecord record)
        {
            int openMoonCount = 0;
            int survivingSunCount = 0;
            int firstAnnihilation = record.Annihilations.Count;

            for (int k = from; k < to; k++)
            {
                int cell = _lane[k];
                int tileId = _grid.TileAt(cell);
                if (tileId == GridModel.NoTile) continue;

                if (_grid.TypeOf(tileId) == TileType.Moon)
                {
                    _openMoons[openMoonCount++] = k;
                    continue;
                }

                if (openMoonCount == 0)
                {
                    _survivingSuns[survivingSunCount++] = tileId;
                    continue;
                }

                int moonLaneIndex = _openMoons[--openMoonCount];
                int moonCell = _lane[moonLaneIndex];

                record.Annihilations.Add(new Annihilation(
                    sunId: tileId,
                    moonId: _grid.TileAt(moonCell),
                    sunFromCell: cell,
                    moonFromCell: moonCell,
                    collisionCell: _lane[(k + moonLaneIndex) / 2]));
            }

            int survivingMoonCount = openMoonCount;
            for (int i = 0; i < survivingMoonCount; i++)
                _survivingMoons[i] = _grid.TileAt(_lane[_openMoons[i]]);

            for (int k = from; k < to; k++) _grid.ClearCell(_lane[k]);

            for (int i = firstAnnihilation; i < record.Annihilations.Count; i++)
            {
                Annihilation blast = record.Annihilations[i];
                _grid.KillTile(blast.SunId);
                _grid.KillTile(blast.MoonId);
            }

            for (int i = 0; i < survivingSunCount; i++)
                Reseat(_survivingSuns[i], _lane[from + i], record);

            for (int i = 0; i < survivingMoonCount; i++)
                Reseat(_survivingMoons[i], _lane[to - survivingMoonCount + i], record);
        }

        private void Reseat(int tileId, int targetCell, MoveRecord record)
        {
            int originCell = _grid.CellOf(tileId);
            _grid.PlaceTile(tileId, targetCell);

            if (originCell != targetCell)
                record.Moves.Add(new TileMove(tileId, originCell, targetCell));
        }

        private void BreakNeutronsAdjacentToBlasts(MoveRecord record)
        {
            for (int i = 0; i < record.Annihilations.Count; i++)
            {
                int cell = record.Annihilations[i].CollisionCell;
                TryBreakNeutron(cell, 1, 0, record);
                TryBreakNeutron(cell, -1, 0, record);
                TryBreakNeutron(cell, 0, 1, record);
                TryBreakNeutron(cell, 0, -1, record);
            }
        }

        private void TryBreakNeutron(int cell, int dx, int dy, MoveRecord record)
        {
            int x = _grid.CellX(cell) + dx;
            int y = _grid.CellY(cell) + dy;
            if (!_grid.InBounds(x, y)) return;

            int neighbourCell = _grid.CellIndex(x, y);
            int tileId = _grid.TileAt(neighbourCell);

            if (tileId == GridModel.NoTile || _grid.TypeOf(tileId) != TileType.Neutron) return;

            _grid.ClearCell(neighbourCell);
            _grid.KillTile(tileId);
            record.NeutronBreaks.Add(new NeutronBreak(tileId, neighbourCell));
        }
    }
}
