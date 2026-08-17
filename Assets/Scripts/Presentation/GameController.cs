// The seam between simulation and view: owns the session, routes swipes, drives animation.

using DG.Tweening;
using Polarity.Core;
using Polarity.Input;
using UnityEngine;

namespace Polarity.Presentation
{
    public sealed class GameController : MonoBehaviour
    {
        [Header("Board setup")]
        [SerializeField, Range(3, 10)] private int boardWidth = 5;
        [SerializeField, Range(3, 12)] private int boardHeight = 6;
        [SerializeField, Range(1, 30)] private int pairCount = 5;
        [SerializeField, Range(0, 12)] private int neutronCount = 2;

        [Tooltip("Spare moves on top of the shortest possible clear. Higher is easier.")]
        [SerializeField, Range(0, 20)] private int moveSlack = 6;

        [Tooltip("Leave at 0 for a different board every run. Set a value to reproduce one.")]
        [SerializeField] private int fixedSeed;

        [Header("Feel")]
        [SerializeField] private float comboShakeStrength = 16f;
        [SerializeField] private float resultDelay = 0.5f;

        [Header("Scene references")]
        [SerializeField] private SwipeDetector swipeDetector;
        [SerializeField] private BoardView boardView;
        [SerializeField] private HudView hudView;
        [SerializeField] private OverlayView overlayView;
        [SerializeField] private TutorialView tutorialView;
        [SerializeField] private GameAudio gameAudio;
        [SerializeField] private ScreenShake screenShake;

        private GameSession _session;

        public GameSession Session => _session;

        private void Awake()
        {
            DOTween.SetTweensCapacity(500, 100);
            Application.targetFrameRate = 60;

            _session = new GameSession(BuildConfig());

            swipeDetector.Swiped += OnSwiped;
            hudView.UndoRequested += OnUndo;
            hudView.RestartRequested += OnRestart;
            overlayView.RestartRequested += OnRestart;
            overlayView.UndoRequested += OnUndo;
        }

        private void Start()
        {
            boardView.Build(_session.Grid);
            boardView.PlayIntro();
            hudView.Refresh(_session, animateScore: false);

            if (tutorialView != null && tutorialView.ShowOnStart) tutorialView.Show();
        }

        private void OnDestroy()
        {
            if (swipeDetector != null) swipeDetector.Swiped -= OnSwiped;

            if (hudView != null)
            {
                hudView.UndoRequested -= OnUndo;
                hudView.RestartRequested -= OnRestart;
            }

            if (overlayView != null)
            {
                overlayView.RestartRequested -= OnRestart;
                overlayView.UndoRequested -= OnUndo;
            }
        }

        private GameConfig BuildConfig()
        {
            int seed = fixedSeed != 0 ? fixedSeed : Random.Range(1, int.MaxValue);
            return new GameConfig(boardWidth, boardHeight, pairCount, neutronCount, moveSlack, seed);
        }

        private void OnSwiped(Direction direction)
        {
            if (tutorialView != null && tutorialView.IsActive)
            {
                tutorialView.HandleSwipe(direction);
                return;
            }

            if (overlayView.IsShown || _session.IsOver) return;

            if (boardView.IsAnimating) boardView.CompleteImmediately();

            MoveRecord record = _session.TryMove(direction);

            if (record == null)
            {
                boardView.RejectFeedback(direction);
                gameAudio.PlayBlocked();
                return;
            }

            boardView.Play(record);
            gameAudio.PlaySlide();

            if (record.PairCount > 0) gameAudio.PlayAnnihilation(record.PairCount);
            if (record.NeutronBreaks.Count > 0) gameAudio.PlayNeutronBreak();

            if (record.PairCount >= 2)
                screenShake.Shake(comboShakeStrength * Mathf.Min(record.PairCount, 4) * 0.5f);

            hudView.Refresh(_session);
            hudView.PulseMoves();

            if (_session.IsOver) ScheduleResult();
        }

        private void OnUndo()
        {
            if (!_session.CanUndo) return;

            if (boardView.IsAnimating) boardView.CompleteImmediately();

            MoveRecord record = _session.Undo();
            if (record == null) return;

            overlayView.Hide();
            boardView.PlayReversed(record);
            gameAudio.PlayUndo();
            hudView.Refresh(_session);
        }

        private void OnRestart()
        {
            overlayView.Hide();

            if (boardView.IsAnimating) boardView.CompleteImmediately();

            _session.Restart(BuildConfig());

            boardView.Rebuild();
            boardView.PlayIntro();
            hudView.Refresh(_session, animateScore: false);
        }

        private void ScheduleResult()
        {
            bool won = _session.State == GameState.Won;

            if (won)
            {
                gameAudio.PlayWin();
            }
            else
            {
                gameAudio.PlayLose();
                screenShake.Shake(comboShakeStrength * 1.4f, 0.5f);
            }

            bool deadEnd = _session.IsDeadPosition;

            DOVirtual.DelayedCall(resultDelay,
                () => overlayView.Show(won, deadEnd, _session.Score, _session.MovesUsed));
        }
    }
}
