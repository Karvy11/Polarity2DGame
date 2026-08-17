// Watches touch, mouse and keyboard and reports finished gestures as directions.

using System;
using Polarity.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Polarity.Input
{
    public sealed class SwipeDetector : MonoBehaviour
    {
        [Header("Gesture thresholds")]
        [Tooltip("Minimum travel as a fraction of screen height. Keeps taps and jitter out.")]
        [SerializeField, Range(0.01f, 0.25f)] private float minSwipeScreenFraction = 0.04f;

        [Tooltip("How far the leading axis must beat the other. 1 accepts any diagonal; higher is stricter.")]
        [SerializeField, Range(1f, 3f)] private float dominanceRatio = 1.2f;

        [Header("Editor convenience")]
        [Tooltip("Arrow keys and WASD, so the game is playable without a touchscreen.")]
        [SerializeField] private bool enableKeyboard = true;

        public event Action<Direction> Swiped;

        private bool _tracking;
        private int _activeTouchId = InvalidTouch;
        private Vector2 _startPosition;

        private const int InvalidTouch = -1;

        private void Update()
        {
            if (enableKeyboard) ReadKeyboard();

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.touches.Count > 0)
            {
                ReadTouch(touchscreen);
                return;
            }

            ReadMouse(Mouse.current);
        }

        private void ReadTouch(Touchscreen touchscreen)
        {
            var touches = touchscreen.touches;

            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                TouchPhase phase = touch.phase.ReadValue();
                int touchId = touch.touchId.ReadValue();

                if (_tracking)
                {
                    if (touchId != _activeTouchId) continue;

                    if (phase == TouchPhase.Ended) Release(touch.position.ReadValue());
                    else if (phase == TouchPhase.Canceled) Cancel();

                    return;
                }

                if (phase != TouchPhase.Began) continue;

                Begin(touch.position.ReadValue(), touchId);
                return;
            }
        }

        private void ReadMouse(Mouse mouse)
        {
            if (mouse == null) return;

            if (!_tracking && mouse.leftButton.wasPressedThisFrame)
            {
                Begin(mouse.position.ReadValue(), InvalidTouch);
                return;
            }

            if (_tracking && _activeTouchId == InvalidTouch && mouse.leftButton.wasReleasedThisFrame)
                Release(mouse.position.ReadValue());
        }

        private void ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                Swiped?.Invoke(Direction.Up);
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                Swiped?.Invoke(Direction.Down);
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                Swiped?.Invoke(Direction.Left);
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                Swiped?.Invoke(Direction.Right);
        }

        private void Begin(Vector2 position, int touchId)
        {
            _tracking = true;
            _activeTouchId = touchId;
            _startPosition = position;
        }

        private void Cancel()
        {
            _tracking = false;
            _activeTouchId = InvalidTouch;
        }

        private void Release(Vector2 endPosition)
        {
            Vector2 delta = endPosition - _startPosition;
            Cancel();

            float minDistance = Screen.height * minSwipeScreenFraction;

            if (SwipeInterpreter.TryResolve(delta.x, delta.y, minDistance, dominanceRatio, out Direction direction))
                Swiped?.Invoke(direction);
        }
    }
}
