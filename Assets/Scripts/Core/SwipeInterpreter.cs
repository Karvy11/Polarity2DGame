// Turns a pointer displacement into one of the four directions.

using System;

namespace Polarity.Core
{
    public static class SwipeInterpreter
    {
        public enum Rejection : byte
        {
            None = 0,

            TooShort = 1,

            TooDiagonal = 2
        }

        public static bool TryResolve(
            float dx, float dy, float minDistance, float dominanceRatio,
            out Direction direction, out Rejection rejection)
        {
            direction = Direction.Up;

            float ax = Math.Abs(dx);
            float ay = Math.Abs(dy);

            if (ax < minDistance && ay < minDistance)
            {
                rejection = Rejection.TooShort;
                return false;
            }

            bool horizontal = ax > ay;
            float leading = horizontal ? ax : ay;
            float trailing = horizontal ? ay : ax;

            if (leading < trailing * dominanceRatio)
            {
                rejection = Rejection.TooDiagonal;
                return false;
            }

            direction = horizontal
                ? (dx > 0 ? Direction.Right : Direction.Left)
                : (dy > 0 ? Direction.Up : Direction.Down);

            rejection = Rejection.None;
            return true;
        }

        public static bool TryResolve(float dx, float dy, float minDistance, float dominanceRatio, out Direction direction) =>
            TryResolve(dx, dy, minDistance, dominanceRatio, out direction, out _);
    }
}
