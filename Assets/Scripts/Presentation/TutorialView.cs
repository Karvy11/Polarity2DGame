// Three-step interactive tutorial: the player must perform the swipe to advance.

using System;
using DG.Tweening;
using Polarity.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Polarity.Presentation
{
    public sealed class TutorialView : MonoBehaviour
    {
        [Serializable]
        public struct Step
        {
            public string headline;
            [TextArea(2, 3)] public string body;
            public bool showSun;
            public bool showMoon;
        }

        [Header("Assets")]
        [SerializeField] private PolarityTheme theme;
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private RectTransform cellPrefab;
        [SerializeField] private TileStyle sunStyle;
        [SerializeField] private TileStyle moonStyle;

        [Header("Scene")]
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform demoArea;
        [SerializeField] private TextMeshProUGUI stepLabel;
        [SerializeField] private TextMeshProUGUI headline;
        [SerializeField] private TextMeshProUGUI body;
        [SerializeField] private TextMeshProUGUI hint;
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI nextLabel;
        [SerializeField] private Button skipButton;

        [Header("Behaviour")]
        [SerializeField] private bool showOnStart = true;
        [SerializeField] private Direction expected = Direction.Right;
        [SerializeField] private string hintText = "swipe right to try it";
        [SerializeField] private string nextText = "NEXT";
        [SerializeField] private string finishText = "PLAY";

        [Header("Demo strip")]
        [SerializeField, Range(3, 9)] private int demoCells = 5;
        [SerializeField] private float demoCellSize = 116f;

        [SerializeField] private Step[] steps;

        public bool IsActive { get; private set; }
        public bool ShowOnStart => showOnStart;
        public event Action Completed;

        private TileView _sun;
        private TileView _moon;
        private int _index;
        private bool _solved;
        private Sequence _demo;

        private void Awake()
        {
            nextButton.onClick.AddListener(Advance);
            skipButton.onClick.AddListener(Finish);

            BuildDemoStrip();

            group.alpha = 0f;
            group.blocksRaycasts = false;
            root.SetActive(false);
        }

        private void BuildDemoStrip()
        {
            for (int i = 0; i < demoCells; i++)
            {
                RectTransform slot = Instantiate(cellPrefab, demoArea);
                slot.name = $"DemoCell{i}";
                slot.sizeDelta = Vector2.one * (demoCellSize * (1f - theme.cellGap));
                slot.anchoredPosition = DemoPosition(i);
            }

            _sun = Instantiate(tilePrefab, demoArea);
            _sun.Bind(0, sunStyle, theme);
            _sun.SetCellSize(demoCellSize);

            _moon = Instantiate(tilePrefab, demoArea);
            _moon.Bind(1, moonStyle, theme);
            _moon.SetCellSize(demoCellSize);
        }

        private Vector2 DemoPosition(int index) =>
            new Vector2((index - (demoCells - 1) * 0.5f) * demoCellSize, 0f);

        public void Show()
        {
            IsActive = true;
            root.SetActive(true);
            group.blocksRaycasts = true;

            group.DOKill();
            panel.DOKill();
            panel.localScale = Vector3.one * 0.9f;

            DOTween.Sequence()
                .Append(group.DOFade(1f, theme.uiFadeDuration))
                .Join(panel.DOScale(1f, theme.uiFadeDuration + 0.1f).SetEase(Ease.OutBack))
                .SetUpdate(true);

            GoToStep(0);
        }

        private void GoToStep(int index)
        {
            _index = Mathf.Clamp(index, 0, steps.Length - 1);
            _solved = false;

            Step step = steps[_index];

            stepLabel.text = $"{_index + 1} / {steps.Length}";
            headline.text = step.headline;
            body.text = step.body;

            nextLabel.text = _index == steps.Length - 1 ? finishText : nextText;
            nextButton.interactable = false;

            hint.text = hintText;
            hint.color = theme.accent;
            hint.gameObject.SetActive(true);

            ResetDemo(step);
        }

        private void ResetDemo(Step step)
        {
            _demo?.Kill();

            _sun.gameObject.SetActive(step.showSun);
            _moon.gameObject.SetActive(step.showMoon);

            _sun.ResetVisualState();
            _moon.ResetVisualState();

            _sun.SetPosition(DemoPosition(0));
            _moon.SetPosition(DemoPosition(demoCells - 1));
        }

        public void HandleSwipe(Direction direction)
        {
            if (!IsActive || _solved) return;

            if (direction != expected)
            {
                hint.rectTransform.DOKill();
                hint.rectTransform.anchoredPosition = Vector2.zero;
                hint.rectTransform.DOPunchAnchorPos(new Vector2(14f, 0f), 0.3f, 9, 0.8f).SetUpdate(true);
                return;
            }

            PlayDemo();
        }

        private void PlayDemo()
        {
            _solved = true;
            hint.gameObject.SetActive(false);

            float slide = theme.slideDuration * 2f;
            _demo?.Kill();
            _demo = DOTween.Sequence().SetUpdate(true);

            switch (_index)
            {
                case 0:
                    _demo.Append(_sun.MoveTo(DemoPosition(demoCells - 1), slide));
                    _demo.Append(_sun.PunchScale());
                    break;

                case 1:
                    _demo.Append(_moon.MoveTo(DemoPosition(0), slide));
                    _demo.Append(_moon.PunchScale());
                    break;

                default:
                    Vector2 meeting = DemoPosition((demoCells - 1) / 2);
                    _demo.Append(_sun.MoveTo(meeting, slide));
                    _demo.Join(_moon.MoveTo(meeting, slide));
                    _demo.Append(_sun.FlashGlow());
                    _demo.Join(_moon.FlashGlow());
                    _demo.Join(_sun.Implode());
                    _demo.Join(_moon.Implode());
                    break;
            }

            _demo.OnComplete(() => nextButton.interactable = true);
        }

        private void Advance()
        {
            if (_index >= steps.Length - 1)
            {
                Finish();
                return;
            }

            GoToStep(_index + 1);
        }

        private void Finish()
        {
            if (!IsActive) return;

            IsActive = false;
            _demo?.Kill();

            group.blocksRaycasts = false;
            group.DOKill();

            group.DOFade(0f, theme.uiFadeDuration)
                .OnComplete(() =>
                {
                    root.SetActive(false);
                    Completed?.Invoke();
                })
                .SetUpdate(true);
        }
    }
}
