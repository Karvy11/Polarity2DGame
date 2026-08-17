// Shakes the play area for combos and the loss state.

using DG.Tweening;
using UnityEngine;

namespace Polarity.Presentation
{
    public sealed class ScreenShake : MonoBehaviour
    {
        [Tooltip("The transform that moves. Normally the content root, so overlays stay still.")]
        [SerializeField] private RectTransform target;

        [SerializeField] private int vibrato = 14;
        [SerializeField] private float randomness = 90f;

        public void Shake(float strength, float duration = 0.35f)
        {
            if (target == null) return;

            target.DOKill();
            target.anchoredPosition = Vector2.zero;

            target.DOShakeAnchorPos(duration, strength, vibrato, randomness, false, true)
                .OnComplete(() => target.anchoredPosition = Vector2.zero);
        }
    }
}
