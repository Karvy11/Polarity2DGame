// Test helper: builds and asserts boards from ASCII art.

using System;
using NUnit.Framework;
using Polarity.Core;

namespace Polarity.Core.Tests
{
    internal static class BoardAscii
    {
        public static GridModel Board(params string[] rowsTopFirst)
        {
            if (rowsTopFirst == null || rowsTopFirst.Length == 0)
                throw new ArgumentException("A board needs at least one row.", nameof(rowsTopFirst));

            int height = rowsTopFirst.Length;
            int width = rowsTopFirst[0].Length;

            for (int i = 0; i < height; i++)
            {
                if (rowsTopFirst[i].Length != width)
                    throw new ArgumentException($"Row {i} is {rowsTopFirst[i].Length} wide, expected {width}.");
            }

            var grid = new GridModel(width, height);

            for (int row = 0; row < height; row++)
            {
                int y = height - 1 - row;

                for (int x = 0; x < width; x++)
                {
                    TileType type = rowsTopFirst[row][x] switch
                    {
                        'S' => TileType.Sun,
                        'M' => TileType.Moon,
                        'N' => TileType.Neutron,
                        '.' => TileType.None,
                        var c => throw new ArgumentException($"Unknown board character '{c}'.")
                    };

                    if (type != TileType.None) grid.AddTile(type, grid.CellIndex(x, y));
                }
            }

            return grid;
        }

        public static string Expect(params string[] rowsTopFirst) => string.Join("\n", rowsTopFirst);

        public static void AssertBoard(GridModel grid, params string[] expectedRowsTopFirst)
        {
            string expected = Expect(expectedRowsTopFirst);
            Assert.That(grid.ToAscii(), Is.EqualTo(expected),
                $"\nExpected:\n{expected}\n\nActual:\n{grid.ToAscii()}\n");
        }

        public static MoveRecord Swipe(GridModel grid, Direction direction)
        {
            var record = new MoveRecord();
            new MoveResolver(grid).Resolve(direction, record);
            return record;
        }
    }
}
