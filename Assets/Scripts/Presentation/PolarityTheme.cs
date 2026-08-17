// ScriptableObject holding the palette, board spacing and animation timings.

using UnityEngine;

namespace Polarity.Presentation
{
    [CreateAssetMenu(menuName = "Polarity/Theme", fileName = "PolarityTheme")]
    public sealed class PolarityTheme : ScriptableObject
    {
        [Header("Surfaces")]
        public Color background = new Color(0.043f, 0.063f, 0.149f);
        public Color boardWell = new Color(0.078f, 0.106f, 0.227f);
        public Color cellEmpty = new Color(0.106f, 0.137f, 0.314f);
        public Color scrim = new Color(0.043f, 0.063f, 0.149f, 0.88f);
        public Color shadow = new Color(0f, 0f, 0f, 0.35f);

        [Header("Text and accents")]
        public Color textPrimary = new Color(0.933f, 0.945f, 1f);
        public Color textMuted = new Color(0.529f, 0.573f, 0.753f);
        public Color accent = new Color(0.184f, 0.878f, 0.769f);
        public Color danger = new Color(1f, 0.361f, 0.478f);

        [Header("Board geometry")]
        [Tooltip("Padding between the board area and the outermost cells, in reference pixels.")]
        public float boardPadding = 26f;

        [Tooltip("Gap between cells, as a fraction of one cell.")]
        [Range(0f, 0.4f)] public float cellGap = 0.10f;

        [Tooltip("How far a tile sits inside its cell, as a fraction of one cell.")]
        [Range(0f, 0.4f)] public float tileInset = 0.06f;

        [Header("Timings (seconds)")]
        public float slideDuration = 0.22f;
        public float impactDuration = 0.16f;
        public float popDuration = 0.26f;
        public float spawnDuration = 0.32f;
        public float spawnStagger = 0.015f;
        public float uiFadeDuration = 0.25f;
        public float scoreCountDuration = 0.4f;

        [Header("Feel")]
        [Tooltip("Threshold at which the moves-left readout turns to the danger colour.")]
        public int lowMovesWarning = 3;

        [Tooltip("How far the board nudges when a swipe cannot change anything, as a fraction of a cell.")]
        [Range(0f, 0.5f)] public float rejectNudge = 0.16f;
    }
}
