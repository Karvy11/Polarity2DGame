// The win, loss and dead-end screen.

using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Polarity.Presentation
{
    public sealed class OverlayView : MonoBehaviour
    {
        [SerializeField] private PolarityTheme theme;

        [Header("Scene")]
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform panel;

        [Header("Content")]
        [SerializeField] private Image accentBar;
        [SerializeField] private TextMeshProUGUI headline;
        [SerializeField] private TextMeshProUGUI detail;
        [SerializeField] private TextMeshProUGUI scoreValue;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button undoButton;

        [Header("Copy")]
        [SerializeField] private string wonHeadline = "BOARD CLEAR";
        [SerializeField] private string outOfMovesHeadline = "OUT OF MOVES";
        [SerializeField] private string deadEndHeadline = "NO WAY THROUGH";
        [SerializeField] private string outOfMovesDetail = "undo a few moves and take another line";
        [SerializeField] private string deadEndDetail = "the last pair can never meet again";

        [Header("Button placement")]
        [SerializeField] private float pairedOffsetX = 165f;

        public event Action RestartRequested;
        public event Action UndoRequested;

        public bool IsShown { get; private set; }

        private void Awake()
        {
            playAgainButton.onClick.AddListener(() => RestartRequested?.Invoke());
            if (undoButton != null) undoButton.onClick.AddListener(() => UndoRequested?.Invoke());

            group.alpha = 0f;
            group.blocksRaycasts = false;
            root.SetActive(false);
        }

        public void Show(bool won, bool deadEnd, int score, int movesUsed)
        {
            IsShown = true;
            root.SetActive(true);
            group.blocksRaycasts = true;

            Color tint = won ? theme.accent : theme.danger;

            headline.text = won ? wonHeadline : deadEnd ? deadEndHeadline : outOfMovesHeadline;
            headline.color = won ? theme.textPrimary : theme.danger;
            accentBar.color = tint;
            scoreValue.color = tint;

            detail.text = won
                ? $"every pair annihilated in {movesUsed} move{(movesUsed == 1 ? "" : "s")}"
                : deadEnd ? deadEndDetail : outOfMovesDetail;

            scoreValue.text = score.ToString();

            // Undo is the useful action after a loss, and the only one that matters
            // after a dead end, so it sits beside Play Again rather than being
            // hidden behind a restart.
            if (undoButton != null) undoButton.gameObject.SetActive(!won);

            var playAgainRect = (RectTransform)playAgainButton.transform;
            Vector2 position = playAgainRect.anchoredPosition;
            position.x = won ? 0f : pairedOffsetX;
            playAgainRect.anchoredPosition = position;

            group.DOKill();
            panel.DOKill();
            panel.localScale = Vector3.one * 0.86f;

            DOTween.Sequence()
                .Append(group.DOFade(1f, theme.uiFadeDuration))
                .Join(panel.DOScale(1f, theme.uiFadeDuration + 0.1f).SetEase(Ease.OutBack))
                .SetUpdate(true);
        }

        public void Hide()
        {
            if (!IsShown) return;

            IsShown = false;
            group.blocksRaycasts = false;
            group.DOKill();

            group.DOFade(0f, theme.uiFadeDuration)
                .OnComplete(() => root.SetActive(false))
                .SetUpdate(true);
        }
    }
}
