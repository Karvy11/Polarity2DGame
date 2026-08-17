// ScriptableObject describing how one tile type looks.

using Polarity.Core;
using UnityEngine;

namespace Polarity.Presentation
{
    [CreateAssetMenu(menuName = "Polarity/Tile Style", fileName = "TileStyle")]
    public sealed class TileStyle : ScriptableObject
    {
        public TileType type = TileType.Sun;

        [Header("Colours")]
        public Color body = Color.white;
        public Color mark = Color.white;

        [Tooltip("Colour of the flash when this tile is destroyed.")]
        public Color flash = Color.white;

        [Header("Mark")]
        public Sprite markSprite;

        [Tooltip("Mark size as a fraction of the tile.")]
        [Range(0.1f, 0.9f)] public float markScale = 0.42f;

        [Header("Depth")]
        public bool castShadow = true;
    }
}
