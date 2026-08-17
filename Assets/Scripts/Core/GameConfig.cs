// Game setup values, the game state enum and the scoring table.

namespace Polarity.Core
{
    public enum GameState : byte
    {
        Playing = 0,
        Won = 1,
        Lost = 2
    }

    public readonly struct GameConfig
    {
        public readonly int Width;
        public readonly int Height;

        public readonly int PairCount;

        public readonly int NeutronCount;
        public readonly int MoveBudget;
        public readonly int Seed;

        public GameConfig(int width, int height, int pairCount, int neutronCount, int moveBudget, int seed)
        {
            Width = width;
            Height = height;
            PairCount = pairCount;
            NeutronCount = neutronCount;
            MoveBudget = moveBudget;
            Seed = seed;
        }

        public static GameConfig Default => new GameConfig(
            width: 6, height: 6, pairCount: 10, neutronCount: 3, moveBudget: 15, seed: 0);
    }

    public static class ScoreRules
    {
        public const int PerPair = 10;

        public const int ComboBonusPerExtraPair = 25;

        public const int NeutronBreakBonus = 40;

        public static int Evaluate(int pairCount, int neutronBreakCount)
        {
            if (pairCount <= 0) return 0;

            return pairCount * PerPair
                   + (pairCount - 1) * ComboBonusPerExtraPair
                   + neutronBreakCount * NeutronBreakBonus;
        }
    }
}
