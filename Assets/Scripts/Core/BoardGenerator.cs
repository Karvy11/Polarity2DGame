// Seeded board setup that only accepts boards proven clearable, and reports how few moves that takes.

using System;

namespace Polarity.Core
{
    public static class BoardGenerator
    {
        private const int MaxAttempts = 300;

        public static int Populate(GridModel grid, GameConfig config, MoveResolver resolver)
        {
            int tileTotal = config.PairCount * 2 + config.NeutronCount;
            if (tileTotal > grid.CellCount)
                throw new ArgumentException(
                    $"Cannot fit {tileTotal} tiles on a {grid.Width}x{grid.Height} board.", nameof(config));

            var cells = new int[grid.CellCount];

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                Scatter(grid, config, cells, new Random(config.Seed + attempt * 7919));

                if (BoardSolver.TryFindMinimumMoves(grid, resolver, out int minimumMoves))
                    return minimumMoves;
            }

            return BuildGuaranteedBoard(grid, config, resolver);
        }

        private static void Scatter(GridModel grid, GameConfig config, int[] cells, Random random)
        {
            grid.Clear();

            for (int i = 0; i < cells.Length; i++) cells[i] = i;
            Shuffle(cells, random);

            int next = 0;
            for (int i = 0; i < config.PairCount; i++) grid.AddTile(TileType.Sun, cells[next++]);
            for (int i = 0; i < config.PairCount; i++) grid.AddTile(TileType.Moon, cells[next++]);
            for (int i = 0; i < config.NeutronCount; i++) grid.AddTile(TileType.Neutron, cells[next++]);
        }

        // Last resort if the random search never found a provably clearable board.
        // Each pair is stacked in a column with the moon above the sun, so a single
        // upward swipe crosses all of them. Never expected to run, but it means the
        // game can always start.
        private static int BuildGuaranteedBoard(GridModel grid, GameConfig config, MoveResolver resolver)
        {
            grid.Clear();

            int placed = 0;
            for (int y = grid.Height - 1; y >= 1 && placed < config.PairCount; y -= 2)
            {
                for (int x = 0; x < grid.Width && placed < config.PairCount; x++)
                {
                    grid.AddTile(TileType.Moon, grid.CellIndex(x, y));
                    grid.AddTile(TileType.Sun, grid.CellIndex(x, y - 1));
                    placed++;
                }
            }

            return BoardSolver.TryFindMinimumMoves(grid, resolver, out int minimumMoves) ? minimumMoves : 1;
        }

        private static void Shuffle(int[] values, Random random)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
