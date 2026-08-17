// Tests for gesture thresholds and diagonal rejection.

using NUnit.Framework;
using Polarity.Core;
using static Polarity.Core.SwipeInterpreter;

namespace Polarity.Core.Tests
{
    [TestFixture]
    public class SwipeInterpreterTests
    {
        private const float MinDistance = 50f;
        private const float Dominance = 1.2f;

        private static bool Resolve(float dx, float dy, out Direction direction, out Rejection rejection) =>
            TryResolve(dx, dy, MinDistance, Dominance, out direction, out rejection);

        [TestCase(0f, 100f, Direction.Up)]
        [TestCase(0f, -100f, Direction.Down)]
        [TestCase(-100f, 0f, Direction.Left)]
        [TestCase(100f, 0f, Direction.Right)]
        public void CleanSwipes_ResolveToTheObviousDirection(float dx, float dy, Direction expected)
        {
            Assert.That(Resolve(dx, dy, out Direction direction, out _), Is.True);
            Assert.That(direction, Is.EqualTo(expected));
        }

        [Test]
        public void ATapIsNotASwipe()
        {
            Assert.That(Resolve(2f, 3f, out _, out Rejection rejection), Is.False);
            Assert.That(rejection, Is.EqualTo(Rejection.TooShort));
        }

        [Test]
        public void AJustTooShortSwipe_IsRejected()
        {
            Assert.That(Resolve(0f, MinDistance - 0.1f, out _, out Rejection rejection), Is.False);
            Assert.That(rejection, Is.EqualTo(Rejection.TooShort));
        }

        [Test]
        public void AJustLongEnoughSwipe_IsAccepted()
        {
            Assert.That(Resolve(0f, MinDistance, out Direction direction, out _), Is.True);
            Assert.That(direction, Is.EqualTo(Direction.Up));
        }

        [Test]
        public void APerfectDiagonal_IsRefusedRatherThanGuessed()
        {
            Assert.That(Resolve(100f, 100f, out _, out Rejection rejection), Is.False);
            Assert.That(rejection, Is.EqualTo(Rejection.TooDiagonal));
        }

        [Test]
        public void ALeaningDiagonal_ResolvesToItsDominantAxis()
        {
            Assert.That(Resolve(200f, 60f, out Direction direction, out _), Is.True);
            Assert.That(direction, Is.EqualTo(Direction.Right));
        }

        [Test]
        public void ADiagonalInsideTheDeadzone_IsRefused()
        {
            Assert.That(Resolve(110f, 100f, out _, out Rejection rejection), Is.False);
            Assert.That(rejection, Is.EqualTo(Rejection.TooDiagonal));
        }

        [Test]
        public void LongSwipesStayResolvableEvenWhenSlightlyOffAxis()
        {
            Assert.That(Resolve(-800f, 120f, out Direction direction, out _), Is.True);
            Assert.That(direction, Is.EqualTo(Direction.Left));
        }

        [Test]
        public void ExactAxisMovementIsAlwaysDominant()
        {
            Assert.That(Resolve(0f, 400f, out Direction direction, out Rejection rejection), Is.True);
            Assert.That(rejection, Is.EqualTo(Rejection.None));
            Assert.That(direction, Is.EqualTo(Direction.Up));
        }

        [Test]
        public void ZeroMovementIsRejected()
        {
            Assert.That(Resolve(0f, 0f, out _, out Rejection rejection), Is.False);
            Assert.That(rejection, Is.EqualTo(Rejection.TooShort));
        }
    }
}
