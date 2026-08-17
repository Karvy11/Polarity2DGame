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

        // Spare moves on top of the shortest solution the generator found. This is
        // the difficulty dial: the budget is always enough to win, plus room to
        // make mistakes.
        public readonly int MoveSlack;

        public readonly int Seed;

        public GameConfig(int width, int height, int pairCount, int neutronCount, int moveSlack, int seed)
        {
            Width = width;
            Height = height;
            PairCount = pairCount;
            NeutronCount = neutronCount;
            MoveSlack = moveSlack;
            Seed = seed;
        }

        public static GameConfig Default => new GameConfig(
            width: 5, height: 6, pairCount: 5, neutronCount: 2, moveSlack: 6, seed: 0);
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
