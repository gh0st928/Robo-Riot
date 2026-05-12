using System.Collections.Generic;
using UnityEngine;

namespace RoboRiot.Grid
{
    /// <summary>
    /// Creates and manages the grid of GridCell GameObjects.
    ///
    /// Setup:
    ///  1. Create an empty GameObject, name it "GridManager"
    ///  2. Attach GridManager.cs to it
    ///  3. Create TileData assets (Right Click > Create > RoboRiot > Tile Data)
    ///     and assign a Default Tile in the Inspector
    ///  4. Press Play — the grid spawns automatically
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector
        // ---------------------------------------------------------------
        [Header("Grid Size")]
        [SerializeField] private int width  = 20;
        [SerializeField] private int height = 20;

        [Header("Cell Size (match your sprite's Pixels Per Unit)")]
        [SerializeField] private float cellSize = 1f;

        [Header("Origin (bottom-left corner of the grid)")]
        [SerializeField] private Vector2 originPosition = Vector2.zero;

        [Header("Default Tile Data (used for all cells on startup)")]
        [SerializeField] private TileData defaultTileData;

        // ---------------------------------------------------------------
        // Singleton
        // ---------------------------------------------------------------
        public static GridManager Instance { get; private set; }

        // ---------------------------------------------------------------
        // Public properties
        // ---------------------------------------------------------------
        public int   Width    => width;
        public int   Height   => height;
        public float CellSize => cellSize;

        // ---------------------------------------------------------------
        // Internal
        // ---------------------------------------------------------------
        private GridCell[,] _grid;
        private Transform   _cellContainer;   // Parent object to keep hierarchy tidy

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildGrid();
        }

        // ---------------------------------------------------------------
        // Grid construction
        // ---------------------------------------------------------------
        private void BuildGrid()
        {
            // Centre the grid on the camera
            Vector3 camPos = Camera.main.transform.position;
            originPosition = new Vector2(
            camPos.x - (width  * cellSize) / 2f,
            camPos.y - (height * cellSize) / 2f
            );

            _cellContainer = new GameObject("--- Cells ---").transform;
            _cellContainer.SetParent(transform);

            _grid = new GridCell[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Vector3 worldPos = GridToWorld(x, y);
                    GameObject cellObj = new GameObject($"Cell ({x},{y})");
                    cellObj.transform.SetParent(_cellContainer);
                    cellObj.transform.position = worldPos;

                    GridCell cell = cellObj.AddComponent<GridCell>();
                    cell.Initialise(x, y, defaultTileData);
                    _grid[x, y] = cell;
                }

            Debug.Log($"[GridManager] Built {width}x{height} grid.");
        }     



        // ---------------------------------------------------------------
        // Coordinate conversion
        // ---------------------------------------------------------------

        /// <summary>World position of the centre of grid cell (x, y).</summary>
        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3(
                originPosition.x + x * cellSize + cellSize * 0.5f,
                originPosition.y + y * cellSize + cellSize * 0.5f,
                0f
            );
        }

        /// <summary>Nearest grid coordinate to a world position.</summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - originPosition.x) / cellSize);
            int y = Mathf.FloorToInt((worldPos.y - originPosition.y) / cellSize);
            return new Vector2Int(x, y);
        }

        // ---------------------------------------------------------------
        // Cell accessors
        // ---------------------------------------------------------------
        public GridCell GetCell(int x, int y)
        {
           
            if (!InBounds(x, y)) return null;
            if (_grid == null) return null;
            return _grid[x, y];
        }

        public GridCell GetCell(Vector2Int coord)     => GetCell(coord.x, coord.y);
        public GridCell GetCellAtWorldPos(Vector3 wp) => GetCell(WorldToGrid(wp));
        public bool     InBounds(int x, int y)        => x >= 0 && x < width && y >= 0 && y < height;

        // ---------------------------------------------------------------
        // Neighbours
        // ---------------------------------------------------------------
        private static readonly Vector2Int[] Cardinals =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        private static readonly Vector2Int[] AllDirs =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new( 1,  1), new(-1,  1), new( 1, -1), new(-1, -1)
        };

        public List<GridCell> GetNeighbours(int x, int y, bool includeDiagonals = false)
        {
            var result = new List<GridCell>();
            foreach (var dir in includeDiagonals ? AllDirs : Cardinals)
            {
                var cell = GetCell(x + dir.x, y + dir.y);
                if (cell != null && cell.IsWalkable) result.Add(cell);
            }
            return result;
        }

        public List<GridCell> GetNeighbours(GridCell cell, bool includeDiagonals = false)
            => GetNeighbours(cell.X, cell.Y, includeDiagonals);

        // ---------------------------------------------------------------
        // Range
        // ---------------------------------------------------------------
        public List<GridCell> GetCellsInRange(int x, int y, int range, bool walkableOnly = false)
        {
            var result = new List<GridCell>();
            for (int dx = -range; dx <= range; dx++)
            for (int dy = -range; dy <= range; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > range) continue;
                if (dx == 0 && dy == 0) continue;
                var cell = GetCell(x + dx, y + dy);
                if (cell == null) continue;
                if (walkableOnly && !cell.IsWalkable) continue;
                result.Add(cell);
            }
            return result;
        }

        public List<GridCell> GetCellsInRange(GridCell origin, int range, bool walkableOnly = false)
            => GetCellsInRange(origin.X, origin.Y, range, walkableOnly);

        // ---------------------------------------------------------------
        // Line-of-sight
        // ---------------------------------------------------------------
        public bool HasLineOfSight(GridCell from, GridCell to)
            => HasLineOfSight(from.X, from.Y, to.X, to.Y);

        public bool HasLineOfSight(int x0, int y0, int x1, int y1)
        {
            foreach (var coord in BresenhamLine(x0, y0, x1, y1))
            {
                if (coord.x == x0 && coord.y == y0) continue;
                if (coord.x == x1 && coord.y == y1) continue;
                var cell = GetCell(coord.x, coord.y);
                if (cell != null && cell.BlocksLineOfSight) return false;
            }
            return true;
        }

        // ---------------------------------------------------------------
        // Tile editing
        // ---------------------------------------------------------------
        public void SetTileData(int x, int y, TileData data)
        {
            var cell = GetCell(x, y);
            if (cell != null) cell.SetTileData(data);
        }

        // ---------------------------------------------------------------
        // Bresenham
        // ---------------------------------------------------------------
        private static IEnumerable<Vector2Int> BresenhamLine(int x0, int y0, int x1, int y1)
        {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1,   sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                yield return new Vector2Int(x0, y0);
                if (x0 == x1 && y0 == y1) yield break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 <  dx) { err += dx; y0 += sy; }
            }
        }
    }
}