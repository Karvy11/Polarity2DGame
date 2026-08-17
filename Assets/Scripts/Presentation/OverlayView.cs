// The win and loss screen.

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

        [Header("Copy")]
        [SerializeField] private string wonHeadline = "BOARD CLEAR";
        [SerializeField] private string lostHeadline = "OUT OF MOVES";
        [SerializeField] private string lostDetail = "undo and try a different order";

        public event Action RestartRequested;

        public bool IsShown { get; private set; }

        private void Awake()
        {
            playAgainButton.onClick.AddListener(() => RestartRequested?.Invoke());

            group.alpha = 0f;
            group.blocksRaycasts = false;
            root.SetActive(false);
        }

        public void Show(bool won, int score, int movesUsed)
        {
            IsShown = true;
            root.SetActive(true);
            group.blocksRaycasts = true;

            Color tint = won ? theme.accent : theme.danger;

            headline.text = won ? wonHeadline : lostHeadline;
            headline.color = won ? theme.textPrimary : theme.danger;
            accentBar.color = tint;
            scoreValue.color = tint;

            detail.text = won
                ? $"every pair annihilated in {movesUsed} move{(movesUsed == 1 ? "" : "s")}"
                : lostDetail;

            scoreValue.text = score.ToString();

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
