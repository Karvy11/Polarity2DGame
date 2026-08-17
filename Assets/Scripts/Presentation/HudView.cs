// Score, moves remaining and the undo / new board controls.

using System;
using DG.Tweening;
using Polarity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Polarity.Presentation
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private PolarityTheme theme;

        [Header("Readouts")]
        [SerializeField] private TextMeshProUGUI scoreValue;
        [SerializeField] private TextMeshProUGUI movesValue;
        [SerializeField] private TextMeshProUGUI remainingLabel;

        [Header("Controls")]
        [SerializeField] private Button undoButton;
        [SerializeField] private Button restartButton;

        public event Action UndoRequested;
        public event Action RestartRequested;

        private int _shownScore;
        private Tween _scoreTween;

        private void Awake()
        {
            undoButton.onClick.AddListener(() => UndoRequested?.Invoke());
            restartButton.onClick.AddListener(() => RestartRequested?.Invoke());
        }

        public void Refresh(GameSession session, bool animateScore = true)
        {
            undoButton.interactable = session.CanUndo;

            movesValue.text = session.MovesRemaining.ToString();
            movesValue.color = session.MovesRemaining <= theme.lowMovesWarning
                ? theme.danger
                : theme.textPrimary;

            int remaining = session.Grid.LiveSunCount;
            remainingLabel.text = remaining == 0
                ? "board clear"
                : $"{remaining} pair{(remaining == 1 ? "" : "s")} left";

            if (!animateScore)
            {
                _scoreTween?.Kill();
                _shownScore = session.Score;
                scoreValue.text = _shownScore.ToString();
                return;
            }

            CountTo(session.Score);
        }

        private void CountTo(int target)
        {
            if (_shownScore == target) return;

            _scoreTween?.Kill();
            _scoreTween = DOTween.To(() => _shownScore, value =>
                {
                    _shownScore = value;
                    scoreValue.text = value.ToString();
                }, target, theme.scoreCountDuration)
                .SetEase(Ease.OutCubic);

            Punch(scoreValue.rectTransform, 0.18f);
        }

        public void PulseMoves() => Punch(movesValue.rectTransform, 0.22f);

        private static void Punch(RectTransform rect, float strength)
        {
            rect.DOKill();
            rect.localScale = Vector3.one;
            rect.DOPunchScale(Vector3.one * strength, 0.3f, 6, 0.7f);
        }
    }
}
