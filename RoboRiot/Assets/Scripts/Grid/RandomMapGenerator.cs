using System.Collections.Generic;
using UnityEngine;
using RoboRiot.Grid;

namespace RoboRiot
{
    /// <summary>
    /// Procedurally generates a level layout on the grid.
    /// Adds outer walls, random inner obstacles, cover spots, hazards,
    /// and an objective tile.
    ///
    /// Setup:
    ///  1. Attach to your GridManager GameObject alongside MapSetup
    ///  2. Assign TileData assets in the Inspector
    ///  3. Press Play — a new layout generates every run
    ///  4. Set a fixed Seed to get the same layout every time
    /// </summary>
    public class RandomMapGenerator : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector
        // ---------------------------------------------------------------
        [Header("Seed (-1 = random each run)")]
        [SerializeField] private int seed = -1;

        [Header("TileData Assets")]
        [SerializeField] private TileData dataFloor;
        [SerializeField] private TileData dataWall;
        [SerializeField] private TileData dataCover;
        [SerializeField] private TileData dataHeavyCover;
        [SerializeField] private TileData dataHazard;
        [SerializeField] private TileData dataObjective;

        [Header("Generation Settings")]
        [SerializeField] private int wallPadding      = 1;    // Border of walls around edge
        [SerializeField] private int minRooms         = 2;    // Min inner obstacle rooms
        [SerializeField] private int maxRooms         = 5;    // Max inner obstacle rooms
        [SerializeField] private int minRoomSize      = 2;    // Min obstacle dimension
        [SerializeField] private int maxRoomSize      = 4;    // Max obstacle dimension
        [SerializeField] private int coverCount       = 6;    // Number of cover tiles
        [SerializeField] private int heavyCoverCount  = 2;    // Number of heavy cover tiles
        [SerializeField] private int hazardCount      = 3;    // Number of hazard tiles
        [SerializeField] private int objectiveCount   = 1;    // Number of objective tiles

        [Header("Spawn Safety")]
        [SerializeField] private int spawnSafeRadius  = 3;    // Tiles around spawn kept clear

        // ---------------------------------------------------------------
        // Internal
        // ---------------------------------------------------------------
        private System.Random _rng;
        private GridManager   _gm;
        private int           _width;
        private int           _height;

        // Track which cells are floors so we don't overwrite walls
        private HashSet<Vector2Int> _floorCells = new();

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        private void Awake()
        {
            int usedSeed = seed < 0 ? Random.Range(0, int.MaxValue) : seed;
            _rng = new System.Random(usedSeed);
            Debug.Log($"[RandomMapGenerator] Seed: {usedSeed}");

            _gm     = GridManager.Instance;
            _width  = _gm.Width;
            _height = _gm.Height;

            Generate();
        }

        // ---------------------------------------------------------------
        // Generation pipeline
        // ---------------------------------------------------------------
        private void Generate()
        {
            if (_gm == null) { Debug.LogError("[RandomMapGenerator] GridManager not found."); return; }

            PlaceOuterWalls();
            PlaceInnerObstacles();
            MarkFloorCells();
            PlaceTiles(dataCover,      coverCount,      safe: true);
            PlaceTiles(dataHeavyCover, heavyCoverCount, safe: true);
            PlaceTiles(dataHazard,     hazardCount,     safe: true);
            PlaceTiles(dataObjective,  objectiveCount,  safe: false);

            Debug.Log("[RandomMapGenerator] Level generated.");
        }

        // ---------------------------------------------------------------
        // Outer walls
        // ---------------------------------------------------------------
        private void PlaceOuterWalls()
        {
            for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
            {
                bool isBorder = x < wallPadding || x >= _width  - wallPadding
                             || y < wallPadding || y >= _height - wallPadding;
                if (isBorder)
                    SetTile(x, y, dataWall);
            }
        }

        // ---------------------------------------------------------------
        // Random inner obstacle rooms
        // ---------------------------------------------------------------
        private void PlaceInnerObstacles()
        {
            int roomCount = _rng.Next(minRooms, maxRooms + 1);

            for (int i = 0; i < roomCount; i++)
            {
                int rw = _rng.Next(minRoomSize, maxRoomSize + 1);
                int rh = _rng.Next(minRoomSize, maxRoomSize + 1);

                // Keep rooms away from edges
                int rx = _rng.Next(wallPadding + 1, _width  - wallPadding - rw - 1);
                int ry = _rng.Next(wallPadding + 1, _height - wallPadding - rh - 1);

                // Randomly choose solid block or hollow room
                bool hollow = _rng.NextDouble() > 0.4;

                for (int x = rx; x < rx + rw; x++)
                for (int y = ry; y < ry + rh; y++)
                {
                    bool isBorder = x == rx || x == rx + rw - 1
                                 || y == ry || y == ry + rh - 1;
                    if (!hollow || isBorder)
                        SetTile(x, y, dataWall);
                }
            }
        }

        // ---------------------------------------------------------------
        // Build floor cell set (used for random placement)
        // ---------------------------------------------------------------
        private void MarkFloorCells()
        {
            _floorCells.Clear();
            for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
            {
                GridCell cell = _gm.GetCell(x, y);
                if (cell != null && cell.IsWalkable)
                    _floorCells.Add(new Vector2Int(x, y));
            }
        }

        // ---------------------------------------------------------------
        // Place random tiles on floor cells
        // ---------------------------------------------------------------
        private void PlaceTiles(TileData data, int count, bool safe)
        {
            if (data == null) return;

            // Build candidate list
            List<Vector2Int> candidates = new(_floorCells);

            // Remove cells too close to spawn points if safe mode
            if (safe)
            {
                candidates.RemoveAll(c =>
                    IsNearSpawn(c.x, c.y, spawnSafeRadius)
                );
            }

            Shuffle(candidates);

            int placed = 0;
            foreach (var coord in candidates)
            {
                if (placed >= count) break;
                SetTile(coord.x, coord.y, data);

                // Remove from floor set so nothing else overwrites it
                _floorCells.Remove(coord);
                placed++;
            }
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private void SetTile(int x, int y, TileData data)
        {
            if (data == null) return;
            _gm.SetTileData(x, y, data);
        }

        /// <summary>
        /// Returns true if a cell is within safeRadius of any spawn-type cell,
        /// or the grid corners (fallback spawn positions).
        /// </summary>
        private bool IsNearSpawn(int x, int y, int radius)
        {
            // Check corners as default spawn zones
            Vector2Int[] spawnZones = new[]
            {
                new Vector2Int(wallPadding + 1, wallPadding + 1),                          // Bottom left
                new Vector2Int(_width - wallPadding - 2, _height - wallPadding - 2)        // Top right
            };

            foreach (var sz in spawnZones)
            {
                int dist = Mathf.Abs(x - sz.x) + Mathf.Abs(y - sz.y);
                if (dist <= radius) return true;
            }

            // Also check any actual Spawn tiles on the grid
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                GridCell cell = _gm.GetCell(x + dx, y + dy);
                if (cell != null && cell.Type == TileType.Spawn) return true;
            }

            return false;
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
