using System.Collections.Generic;
using UnityEngine;

namespace RoboRiot.Grid
{
    /// <summary>
    /// Draws coloured debug squares over each cell in the Scene view.
    /// Also spawns coloured SpriteRenderer quads at runtime so you can
    /// see the grid in the Game view without any art assets.
    /// </summary>
    [RequireComponent(typeof(GridManager))]
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Scene View Gizmos")]
        [SerializeField] private bool drawGizmos      = true;
        [SerializeField] private bool showCoordinates = false;

        [Header("Runtime Debug Sprites")]
        [Tooltip("Turn on to spawn coloured squares in the Game view (no art needed)")]
        [SerializeField] private bool spawnDebugSprites = true;

        [Header("Tile Colours")]
        [SerializeField] private Color colorFloor      = new Color(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color colorWall       = new Color(0.15f, 0.15f, 0.70f, 1f);
        [SerializeField] private Color colorCover      = new Color(0.00f, 0.75f, 0.20f, 1f);
        [SerializeField] private Color colorHeavyCover = new Color(0.00f, 0.35f, 0.10f, 1f);
        [SerializeField] private Color colorHazard     = new Color(1.00f, 0.30f, 0.00f, 1f);
        [SerializeField] private Color colorSpawn      = new Color(0.80f, 0.00f, 0.80f, 1f);
        [SerializeField] private Color colorObjective  = new Color(1.00f, 0.85f, 0.00f, 1f);
        [SerializeField] private Color colorEmpty      = new Color(0.10f, 0.10f, 0.10f, 1f);

        // ---------------------------------------------------------------
        // Highlight API
        // ---------------------------------------------------------------
        private readonly Dictionary<Vector2Int, Color> _highlights = new();

        public void SetHighlight(int x, int y, Color color)
            => _highlights[new Vector2Int(x, y)] = color;

        public void ClearHighlights() => _highlights.Clear();

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        private void Start()
        {
            if (spawnDebugSprites) SpawnDebugSprites();
        }

        // ---------------------------------------------------------------
        // Runtime coloured sprites (Game view visualisation, no art needed)
        // ---------------------------------------------------------------
        private void SpawnDebugSprites()
        {
            var gm = GetComponent<GridManager>();
            if (gm == null) return;

            // Unity's built-in white square sprite
            Sprite whiteSquare = Resources.Load<Sprite>("Sprites/Square");

            // Fallback: create a 1x1 white texture programmatically
            if (whiteSquare == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                whiteSquare = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }

            Transform container = new GameObject("--- Debug Sprites ---").transform;
            container.SetParent(transform);

            for (int x = 0; x < gm.Width; x++)
            for (int y = 0; y < gm.Height; y++)
            {
                GridCell cell = gm.GetCell(x, y);
                if (cell == null) continue;

                GameObject quad = new GameObject($"Sprite ({x},{y})");
                quad.transform.SetParent(container);
                quad.transform.position = new Vector3(
                    cell.transform.position.x,
                    cell.transform.position.y,
                    1f   // Z=1 puts debug sprites behind units (Z=0)
                );
                quad.transform.localScale = Vector3.one * (gm.CellSize * 0.95f);

                var sr = quad.AddComponent<SpriteRenderer>();
                sr.sprite = whiteSquare;
                sr.color  = TileColor(cell.Type);
            }
        }

        // ---------------------------------------------------------------
        // Gizmos (Scene view)
        // ---------------------------------------------------------------

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            if (!Application.isPlaying) return;
            var gm = GetComponent<GridManager>();
            if (gm == null) return;

            float cs = gm.CellSize;

            for (int x = 0; x < gm.Width; x++)
            for (int y = 0; y < gm.Height; y++)
            {
                GridCell cell = gm.GetCell(x, y);
                if (cell == null) continue;

                Vector3 centre = gm.GridToWorld(x, y);

                Color fill = TileColor(cell.Type);
                if (_highlights.TryGetValue(new Vector2Int(x, y), out Color hl)) fill = hl;

                Gizmos.color = fill;
                Gizmos.DrawCube(centre, new Vector3(cs * 0.93f, cs * 0.93f, 0.01f));

                Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
                Gizmos.DrawWireCube(centre, new Vector3(cs, cs, 0.01f));

                if (showCoordinates)
                    UnityEditor.Handles.Label(
                        centre,
                        $"{x},{y}",
                        new GUIStyle { fontSize = 7, normal = { textColor = Color.white } }
                    );
            }
        }
#endif

        // ---------------------------------------------------------------
        // Helper
        // ---------------------------------------------------------------
        public Color TileColor(TileType type) => type switch
        {
            TileType.Floor      => colorFloor,
            TileType.Wall       => colorWall,
            TileType.Cover      => colorCover,
            TileType.HeavyCover => colorHeavyCover,
            TileType.Hazard     => colorHazard,
            TileType.Spawn      => colorSpawn,
            TileType.Objective  => colorObjective,
            _                   => colorEmpty
        };
    }
}