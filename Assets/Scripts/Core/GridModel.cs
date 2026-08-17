// The logical board: cells, tiles, and O(1) lookup in both directions.

using System;
using System.Text;

namespace Polarity.Core
{
    public sealed class GridModel
    {
        public const int NoTile = -1;
        public const int NoCell = -1;

        public int Width { get; }
        public int Height { get; }
        public int CellCount => Width * Height;

        public int TileCount => _tileCount;

        public int LiveSunCount => _liveSuns;
        public int LiveMoonCount => _liveMoons;

        public bool IsCleared => _liveSuns == 0 && _liveMoons == 0;

        private readonly int[] _cellToTile;
        private readonly TileType[] _tileTypes;
        private readonly int[] _tileCells;

        private int _tileCount;
        private int _liveSuns;
        private int _liveMoons;

        public GridModel(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");

            Width = width;
            Height = height;

            int capacity = width * height;
            _cellToTile = new int[capacity];
            _tileTypes = new TileType[capacity];
            _tileCells = new int[capacity];

            for (int i = 0; i < capacity; i++) _cellToTile[i] = NoTile;
        }

        public int CellIndex(int x, int y) => y * Width + x;
        public int CellX(int cell) => cell % Width;
        public int CellY(int cell) => cell / Width;

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public int TileAt(int cell) => _cellToTile[cell];

        public TileType TypeAt(int cell)
        {
            int tileId = _cellToTile[cell];
            return tileId == NoTile ? TileType.None : _tileTypes[tileId];
        }

        public TileType TypeOf(int tileId) => _tileTypes[tileId];

        public int CellOf(int tileId) => _tileCells[tileId];

        public bool IsAlive(int tileId) => _tileCells[tileId] != NoCell;

        public int AddTile(TileType type, int cell)
        {
            if (type == TileType.None)
                throw new ArgumentException("Cannot add a tile of type None.", nameof(type));
            if (_cellToTile[cell] != NoTile)
                throw new InvalidOperationException($"Cell {cell} is already occupied.");

            int tileId = _tileCount++;
            _tileTypes[tileId] = type;
            _tileCells[tileId] = cell;
            _cellToTile[cell] = tileId;

            if (type == TileType.Sun) _liveSuns++;
            else if (type == TileType.Moon) _liveMoons++;

            return tileId;
        }

        public void ClearCell(int cell) => _cellToTile[cell] = NoTile;

        public void PlaceTile(int tileId, int cell)
        {
            _cellToTile[cell] = tileId;
            _tileCells[tileId] = cell;
        }

        public void KillTile(int tileId)
        {
            _tileCells[tileId] = NoCell;

            TileType type = _tileTypes[tileId];
            if (type == TileType.Sun) _liveSuns--;
            else if (type == TileType.Moon) _liveMoons--;
        }

        public void ReviveTile(int tileId, int cell)
        {
            TileType type = _tileTypes[tileId];
            if (type == TileType.Sun) _liveSuns++;
            else if (type == TileType.Moon) _liveMoons++;

            PlaceTile(tileId, cell);
        }

        public void Clear()
        {
            for (int i = 0; i < _cellToTile.Length; i++) _cellToTile[i] = NoTile;
            _tileCount = 0;
            _liveSuns = 0;
            _liveMoons = 0;
        }

        public string ToAscii()
        {
            var builder = new StringBuilder(Height * (Width + 1));

            for (int y = Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < Width; x++)
                {
                    builder.Append(TypeAt(CellIndex(x, y)) switch
                    {
                        TileType.Sun => 'S',
                        TileType.Moon => 'M',
                        TileType.Neutron => 'N',
                        _ => '.'
                    });
                }

                if (y > 0) builder.Append('\n');
            }

            return builder.ToString();
        }

        public override string ToString() => ToAscii();
    }
}
