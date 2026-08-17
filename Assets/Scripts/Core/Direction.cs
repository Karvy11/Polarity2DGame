// The four discrete directions a swipe can resolve to.

namespace Polarity.Core
{
    public enum Direction : byte
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3
    }

    public static class DirectionExtensions
    {
        public static bool IsVertical(this Direction direction) =>
            direction == Direction.Up || direction == Direction.Down;

        public static Direction Opposite(this Direction direction) => direction switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            _ => Direction.Left
        };
    }
}
