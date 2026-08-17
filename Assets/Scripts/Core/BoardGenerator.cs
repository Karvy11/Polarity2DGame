// Seeded board setup that retries until the opening position is playable.

using System;

namespace Polarity.Core
{
    public static class BoardGenerator
    {
        private const int MaxAttempts = 32;

        public static void Populate(GridModel grid, GameConfig config, MoveResolver resolver)
        {
            int tileTotal = config.PairCount * 2 + config.NeutronCount;
            if (tileTotal > grid.CellCount)
                throw new ArgumentException(
                    $"Cannot fit {tileTotal} tiles on a {grid.Width}x{grid.Height} board.", nameof(config));

            var cells = new int[grid.CellCount];
            var probe = new MoveRecord();

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                grid.Clear();

                var random = new Random(config.Seed + attempt);
                for (int i = 0; i < cells.Length; i++) cells[i] = i;
                Shuffle(cells, random);

                int next = 0;
                for (int i = 0; i < config.PairCount; i++) grid.AddTile(TileType.Sun, cells[next++]);
                for (int i = 0; i < config.PairCount; i++) grid.AddTile(TileType.Moon, cells[next++]);
                for (int i = 0; i < config.NeutronCount; i++) grid.AddTile(TileType.Neutron, cells[next++]);

                if (!grid.IsCleared && resolver.HasLegalMove(probe)) return;
            }
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
