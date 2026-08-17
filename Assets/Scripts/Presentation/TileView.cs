// One tile on screen, plus the animation fragments the board sequences.

using DG.Tweening;
using Polarity.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Polarity.Presentation
{
    public sealed class TileView : MonoBehaviour
    {
        [Header("Graphics")]
        [SerializeField] private Image body;
        [SerializeField] private Image mark;
        [SerializeField] private Image shadow;
        [SerializeField] private Image glow;

        [Header("Shadow offset as a fraction of the tile")]
        [SerializeField, Range(0f, 0.2f)] private float shadowDrop = 0.05f;

        [Tooltip("How far the glow extends past the tile.")]
        [SerializeField, Range(1f, 4f)] private float glowSpread = 2.1f;

        public int TileId { get; private set; }
        public TileType Type { get; private set; }
        public RectTransform Rect { get; private set; }

        private TileStyle _style;
        private PolarityTheme _theme;

        private void Awake() => CacheRect();

        private void CacheRect()
        {
            if (Rect == null) Rect = (RectTransform)transform;
        }

        public void Bind(int tileId, TileStyle style, PolarityTheme theme)
        {
            CacheRect();

            TileId = tileId;
            Type = style.type;
            _style = style;
            _theme = theme;

            name = $"Tile{tileId}_{style.type}";

            body.color = style.body;
            mark.color = style.mark;
            mark.sprite = style.markSprite;
            shadow.enabled = style.castShadow;
            glow.color = Color.clear;
        }

        public void SetCellSize(float cellSize)
        {
            CacheRect();

            float tile = cellSize * (1f - _theme.cellGap - _theme.tileInset);
            Rect.sizeDelta = new Vector2(tile, tile);

            shadow.rectTransform.anchoredPosition = new Vector2(0f, -tile * shadowDrop);

            float markSize = tile * _style.markScale;
            mark.rectTransform.sizeDelta = new Vector2(markSize, markSize);

            float glowSize = tile * glowSpread;
            glow.rectTransform.sizeDelta = new Vector2(glowSize, glowSize);
        }

        public void SetPosition(Vector2 anchoredPosition) => Rect.anchoredPosition = anchoredPosition;

        public void SetScale(float scale) => Rect.localScale = Vector3.one * scale;

        public Tween MoveTo(Vector2 anchoredPosition, float duration) =>
            Rect.DOAnchorPos(anchoredPosition, duration).SetEase(Ease.OutQuint);

        public Tween PunchScale(float strength = 0.22f) =>
            Rect.DOPunchScale(Vector3.one * strength, _theme.impactDuration, 6, 0.6f);

        public Tween FlashGlow(float peakAlpha = 0.9f)
        {
            Color colour = _style.flash;
            glow.color = new Color(colour.r, colour.g, colour.b, 0f);

            return DOTween.Sequence()
                .Append(glow.DOFade(peakAlpha, 0.06f))
                .Append(glow.DOFade(0f, _theme.popDuration));
        }

        public Tween Implode()
        {
            return DOTween.Sequence()
                .Append(Rect.DOScale(1.18f, 0.08f).SetEase(Ease.OutQuad))
                .Append(Rect.DOScale(0f, _theme.popDuration - 0.08f).SetEase(Ease.InBack));
        }

        public Tween Appear(float delay = 0f)
        {
            Rect.localScale = Vector3.zero;
            return Rect.DOScale(1f, _theme.spawnDuration).SetEase(Ease.OutBack).SetDelay(delay);
        }

        public Tween Shatter()
        {
            return DOTween.Sequence()
                .Join(Rect.DOScale(0f, _theme.popDuration).SetEase(Ease.InBack))
                .Join(Rect.DORotate(new Vector3(0f, 0f, 90f), _theme.popDuration, RotateMode.FastBeyond360));
        }

        public void ResetVisualState()
        {
            Rect.localScale = Vector3.one;
            Rect.localRotation = Quaternion.identity;
            glow.color = Color.clear;
        }

        public void KillTweens()
        {
            Rect.DOKill();
            glow.DOKill();
        }
    }
}
