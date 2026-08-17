// Draws the board, fits it to any N x M, and animates transitions.

using DG.Tweening;
using Polarity.Core;
using UnityEngine;

namespace Polarity.Presentation
{
    public sealed class BoardView : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private PolarityTheme theme;
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private RectTransform cellPrefab;

        [Tooltip("One entry per tile type. Order does not matter; lookup is by the style's own type.")]
        [SerializeField] private TileStyle[] tileStyles;

        [Header("Scene")]
        [Tooltip("The area the board is fitted into. The board centres itself inside this.")]
        [SerializeField] private RectTransform boardArea;

        [SerializeField] private RectTransform boardRoot;
        [SerializeField] private RectTransform cellLayer;
        [SerializeField] private RectTransform tileLayer;

        private GridModel _grid;
        private TileView[] _tiles;
        private float _cellSize;
        private Sequence _current;
        private Vector2 _lastAreaSize;

        public bool IsAnimating => _current != null && _current.IsActive() && _current.IsPlaying();

        public void Build(GridModel grid)
        {
            _grid = grid;

            BuildCells();
            BuildTiles();
            Relayout();
        }

        private TileStyle StyleFor(TileType type)
        {
            for (int i = 0; i < tileStyles.Length; i++)
                if (tileStyles[i] != null && tileStyles[i].type == type)
                    return tileStyles[i];

            Debug.LogError($"[Polarity] No TileStyle assigned for {type}.", this);
            return tileStyles.Length > 0 ? tileStyles[0] : null;
        }

        private void BuildCells()
        {
            ClearChildren(cellLayer);

            for (int cell = 0; cell < _grid.CellCount; cell++)
            {
                RectTransform slot = Instantiate(cellPrefab, cellLayer);
                slot.name = $"Cell{cell}";
            }
        }

        private void BuildTiles()
        {
            ClearChildren(tileLayer);

            _tiles = new TileView[_grid.TileCount];

            for (int id = 0; id < _grid.TileCount; id++)
            {
                TileView view = Instantiate(tilePrefab, tileLayer);
                view.Bind(id, StyleFor(_grid.TypeOf(id)), theme);
                _tiles[id] = view;
            }
        }

        private static void ClearChildren(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        public void Rebuild()
        {
            BuildTiles();

            for (int id = 0; id < _tiles.Length; id++) _tiles[id].SetCellSize(_cellSize);

            SyncToModel();
        }

        private void Update()
        {
            if (_grid == null) return;

            Vector2 size = boardArea.rect.size;
            if ((size - _lastAreaSize).sqrMagnitude > 1f) Relayout();
        }

        public void Relayout()
        {
            _lastAreaSize = boardArea.rect.size;

            Vector2 available = _lastAreaSize - Vector2.one * (theme.boardPadding * 2f);
            _cellSize = Mathf.Floor(Mathf.Min(available.x / _grid.Width, available.y / _grid.Height));

            boardRoot.sizeDelta = new Vector2(_cellSize * _grid.Width, _cellSize * _grid.Height);

            float slot = _cellSize * (1f - theme.cellGap);

            for (int cell = 0; cell < _grid.CellCount; cell++)
            {
                var rect = (RectTransform)cellLayer.GetChild(cell);
                rect.sizeDelta = new Vector2(slot, slot);
                rect.anchoredPosition = CellPosition(cell);
            }

            for (int id = 0; id < _tiles.Length; id++) _tiles[id].SetCellSize(_cellSize);

            SyncToModel();
        }

        public Vector2 CellPosition(int cell)
        {
            float x = (_grid.CellX(cell) + 0.5f - _grid.Width * 0.5f) * _cellSize;
            float y = (_grid.CellY(cell) + 0.5f - _grid.Height * 0.5f) * _cellSize;
            return new Vector2(x, y);
        }

        public void SyncToModel()
        {
            for (int id = 0; id < _tiles.Length; id++)
            {
                TileView view = _tiles[id];
                bool alive = _grid.IsAlive(id);

                if (view.gameObject.activeSelf != alive) view.gameObject.SetActive(alive);
                if (!alive) continue;

                view.KillTweens();
                view.ResetVisualState();
                view.SetPosition(CellPosition(_grid.CellOf(id)));
            }
        }

        public Sequence PlayIntro()
        {
            Sequence sequence = DOTween.Sequence();
            int shown = 0;

            for (int id = 0; id < _tiles.Length; id++)
            {
                if (!_grid.IsAlive(id)) continue;

                sequence.Join(_tiles[id].Appear(shown * theme.spawnStagger));
                shown++;
            }

            return Track(sequence);
        }

        public Sequence Play(MoveRecord record)
        {
            Sequence sequence = DOTween.Sequence();
            float slide = theme.slideDuration;

            for (int i = 0; i < record.Moves.Count; i++)
            {
                TileMove move = record.Moves[i];
                sequence.Join(_tiles[move.TileId].MoveTo(CellPosition(move.ToCell), slide));
            }

            for (int i = 0; i < record.Annihilations.Count; i++)
            {
                Annihilation blast = record.Annihilations[i];
                Vector2 meeting = CellPosition(blast.CollisionCell);

                TileView sun = _tiles[blast.SunId];
                TileView moon = _tiles[blast.MoonId];

                sequence.Join(sun.MoveTo(meeting, slide));
                sequence.Join(moon.MoveTo(meeting, slide));

                sequence.Insert(slide, sun.FlashGlow());
                sequence.Insert(slide, moon.FlashGlow());
                sequence.Insert(slide, sun.Implode());
                sequence.Insert(slide, moon.Implode());
            }

            for (int i = 0; i < record.NeutronBreaks.Count; i++)
            {
                TileView neutron = _tiles[record.NeutronBreaks[i].TileId];
                sequence.Insert(slide + 0.06f, neutron.Shatter());
            }

            return Track(sequence);
        }

        public Sequence PlayReversed(MoveRecord record)
        {
            Sequence sequence = DOTween.Sequence();
            float slide = theme.slideDuration;

            for (int i = 0; i < record.Annihilations.Count; i++)
            {
                Annihilation blast = record.Annihilations[i];
                Vector2 meeting = CellPosition(blast.CollisionCell);

                TileView sun = _tiles[blast.SunId];
                TileView moon = _tiles[blast.MoonId];

                RestoreAt(sun, meeting);
                RestoreAt(moon, meeting);

                sequence.Join(sun.Rect.DOScale(1f, slide).SetEase(Ease.OutBack));
                sequence.Join(moon.Rect.DOScale(1f, slide).SetEase(Ease.OutBack));
                sequence.Join(sun.MoveTo(CellPosition(blast.SunFromCell), slide));
                sequence.Join(moon.MoveTo(CellPosition(blast.MoonFromCell), slide));
            }

            for (int i = 0; i < record.NeutronBreaks.Count; i++)
            {
                NeutronBreak broken = record.NeutronBreaks[i];
                TileView view = _tiles[broken.TileId];

                RestoreAt(view, CellPosition(broken.Cell));
                sequence.Join(view.Rect.DOScale(1f, slide).SetEase(Ease.OutBack));
            }

            for (int i = 0; i < record.Moves.Count; i++)
            {
                TileMove move = record.Moves[i];
                sequence.Join(_tiles[move.TileId].MoveTo(CellPosition(move.FromCell), slide));
            }

            return Track(sequence);
        }

        private static void RestoreAt(TileView view, Vector2 position)
        {
            view.KillTweens();
            view.gameObject.SetActive(true);
            view.ResetVisualState();
            view.SetPosition(position);
            view.SetScale(0f);
        }

        public Tween RejectFeedback(Direction direction)
        {
            Vector2 nudge = direction switch
            {
                Direction.Up => Vector2.up,
                Direction.Down => Vector2.down,
                Direction.Left => Vector2.left,
                _ => Vector2.right
            } * (_cellSize * theme.rejectNudge);

            boardRoot.DOKill();
            boardRoot.anchoredPosition = Vector2.zero;

            return boardRoot.DOPunchAnchorPos(nudge, 0.28f, 8, 0.8f)
                .OnComplete(() => boardRoot.anchoredPosition = Vector2.zero);
        }

        public void CompleteImmediately()
        {
            if (_current != null && _current.IsActive()) _current.Complete(true);
        }

        private Sequence Track(Sequence sequence)
        {
            _current = sequence;
            sequence.OnComplete(SyncToModel);
            sequence.OnKill(() => { if (_current == sequence) _current = null; });
            return sequence;
        }
    }
}
