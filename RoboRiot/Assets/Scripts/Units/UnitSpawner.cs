using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using RoboRiot.Grid;
using RoboRiot.Units;

namespace RoboRiot.Units
{
    /// <summary>
    /// Spawns player and enemy units onto the grid at mission start.
    /// Player spawns in the bottom left area.
    /// Enemies spawn randomly in the top right area, guaranteed walkable.
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector
        // ---------------------------------------------------------------
        [Header("Spawn Mode")]
        [SerializeField] private SpawnMode spawnMode = SpawnMode.Auto;

        [Header("Tilemap Mode")]
        [SerializeField] private Tilemap spawnTilemap;

        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector2Int playerSpawnCell = new Vector2Int(1, 1);

        [Header("Enemies")]
        [SerializeField] private List<EnemySpawnEntry> enemySpawns = new();

        [Header("Auto Spawn Settings")]
        [Tooltip("How far into the enemy corner to look for spawn cells")]
        [SerializeField] private int enemySpawnAreaSize = 4;
        [Tooltip("Minimum distance between enemy spawns")]
        [SerializeField] private int minEnemySpacing    = 2;
        [Tooltip("Minimum distance from player spawn")]
        [SerializeField] private int minDistFromPlayer  = 5;

        // ---------------------------------------------------------------
        // Public references
        // ---------------------------------------------------------------
        public PlayerUnit      SpawnedPlayer  { get; private set; }
        public List<EnemyUnit> SpawnedEnemies { get; private set; } = new();

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        private void Start()
        {
            SpawnAll();
        }

        // ---------------------------------------------------------------
        // Main spawn
        // ---------------------------------------------------------------
        public void SpawnAll()
        {
            switch (spawnMode)
            {
                case SpawnMode.Auto:    SpawnAuto();    break;
                case SpawnMode.Manual:  SpawnManual();  break;
                case SpawnMode.Tilemap: SpawnFromTilemap(); break;
            }
        }

        // ---------------------------------------------------------------
        // Mode A: Auto — player bottom left, enemies random top right
        // ---------------------------------------------------------------
        private void SpawnAuto()
        {
            var gm = GridManager.Instance;
            if (gm == null) return;

            // --- Spawn player in bottom left ---
            GridCell playerCell = FindWalkableCellInArea(
                wallPad, wallPad,
                wallPad + enemySpawnAreaSize, wallPad + enemySpawnAreaSize,
                new List<GridCell>()
            );

            SpawnPlayer(playerCell);

            // --- Spawn enemies in top right ---
            int startX = gm.Width  - wallPad - enemySpawnAreaSize - 1;
            int startY = gm.Height - wallPad - enemySpawnAreaSize - 1;
            int endX   = gm.Width  - wallPad - 1;
            int endY   = gm.Height - wallPad - 1;

            List<GridCell> usedCells = playerCell != null ? new List<GridCell> { playerCell } : new();

            foreach (var entry in enemySpawns)
            {
                if (entry.prefab == null) continue;

                GridCell cell = FindWalkableCellInArea(startX, startY, endX, endY, usedCells);
                if (cell == null)
                {
                    Debug.LogWarning("[UnitSpawner] Could not find valid enemy spawn cell.");
                    continue;
                }

                usedCells.Add(cell);
                SpawnEnemy(entry.prefab, cell);
            }
        }

        // Border padding to avoid spawning right next to walls
        private int wallPad => 1;

        /// <summary>
        /// Finds a random walkable unoccupied cell within a rectangular area
        /// that is far enough from already used cells.
        /// </summary>
        private GridCell FindWalkableCellInArea(int x0, int y0, int x1, int y1, List<GridCell> usedCells)
        {
            var gm = GridManager.Instance;
            if (gm == null) return null;

            // Gather all valid candidates in the area
            List<GridCell> candidates = new();

            for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                GridCell cell = gm.GetCell(x, y);
                if (cell == null || !cell.IsWalkable || cell.IsOccupied) continue;

                // Check spacing from already used cells
                bool tooClose = usedCells.Any(used =>
                    Mathf.Abs(used.X - cell.X) + Mathf.Abs(used.Y - cell.Y) < minEnemySpacing
                );
                if (tooClose) continue;

                candidates.Add(cell);
            }

            if (candidates.Count == 0)
            {
                // Fallback — try anywhere walkable in the area ignoring spacing
                for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    GridCell cell = gm.GetCell(x, y);
                    if (cell != null && cell.IsWalkable && !cell.IsOccupied)
                        candidates.Add(cell);
                }
            }

            if (candidates.Count == 0) return null;

            // Pick a random candidate
            return candidates[Random.Range(0, candidates.Count)];
        }

        // ---------------------------------------------------------------
        // Mode B: Manual — use Inspector coordinates
        // ---------------------------------------------------------------
        private void SpawnManual()
        {
            SpawnPlayer(GetOrFindCell(playerSpawnCell));

            foreach (var entry in enemySpawns)
            {
                if (entry.prefab == null) continue;
                SpawnEnemy(entry.prefab, GetOrFindCell(entry.spawnCell));
            }
        }

        // ---------------------------------------------------------------
        // Mode C: Tilemap — read from Tilemap_Spawns layer
        // ---------------------------------------------------------------
        private void SpawnFromTilemap()
        {
            if (spawnTilemap == null)
            {
                Debug.LogWarning("[UnitSpawner] No spawn tilemap assigned. Falling back to auto.");
                SpawnAuto();
                return;
            }

            spawnTilemap.CompressBounds();
            BoundsInt bounds = spawnTilemap.cellBounds;
            List<GridCell> spawnCells = new();

            foreach (var pos in bounds.allPositionsWithin)
            {
                if (!spawnTilemap.HasTile(pos)) continue;
                int gx = pos.x - bounds.xMin;
                int gy = pos.y - bounds.yMin;
                GridCell cell = GridManager.Instance?.GetCell(gx, gy);
                if (cell != null && cell.IsWalkable)
                    spawnCells.Add(cell);
            }

            if (spawnCells.Count == 0)
            {
                Debug.LogWarning("[UnitSpawner] No spawn tiles found. Falling back to auto.");
                SpawnAuto();
                return;
            }

            SpawnPlayer(spawnCells[0]);
            for (int i = 1; i < spawnCells.Count; i++)
            {
                if (i - 1 >= enemySpawns.Count) break;
                if (enemySpawns[i - 1].prefab == null) continue;
                SpawnEnemy(enemySpawns[i - 1].prefab, spawnCells[i]);
            }
        }

        // ---------------------------------------------------------------
        // Spawn helpers
        // ---------------------------------------------------------------
        private void SpawnPlayer(GridCell cell)
        {
            if (playerPrefab == null) { Debug.LogError("[UnitSpawner] No player prefab."); return; }
            if (cell == null)         { Debug.LogError("[UnitSpawner] No valid player spawn cell."); return; }

            GameObject obj = Instantiate(playerPrefab);
            SpawnedPlayer = obj.GetComponent<PlayerUnit>();

            if (SpawnedPlayer == null) { Debug.LogError("[UnitSpawner] PlayerUnit component missing."); Destroy(obj); return; }

            SpawnedPlayer.PlaceOnCell(cell);
            Debug.Log($"[UnitSpawner] Player spawned at {cell}.");
        }

        private void SpawnEnemy(GameObject prefab, GridCell cell)
        {
            if (cell == null) { Debug.LogWarning("[UnitSpawner] No valid enemy spawn cell. Skipping."); return; }

            GameObject obj   = Instantiate(prefab);
            EnemyUnit  enemy = obj.GetComponent<EnemyUnit>();

            if (enemy == null) { Debug.LogError("[UnitSpawner] EnemyUnit component missing."); Destroy(obj); return; }

            enemy.PlaceOnCell(cell);
            SpawnedEnemies.Add(enemy);
            Debug.Log($"[UnitSpawner] Enemy spawned at {cell}.");
        }

        // ---------------------------------------------------------------
        // Cell helpers
        // ---------------------------------------------------------------
        private GridCell GetOrFindCell(Vector2Int coord)
        {
            GridCell cell = GridManager.Instance?.GetCell(coord.x, coord.y);
            if (cell == null || !cell.IsWalkable || cell.IsOccupied)
            {
                Debug.LogWarning($"[UnitSpawner] Cell {coord} unavailable. Finding nearest...");
                cell = FindNearestWalkable(coord);
            }
            return cell;
        }

        private GridCell FindNearestWalkable(Vector2Int origin)
        {
            for (int radius = 1; radius <= 5; radius++)
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                GridCell cell = GridManager.Instance?.GetCell(origin.x + dx, origin.y + dy);
                if (cell != null && cell.IsWalkable && !cell.IsOccupied) return cell;
            }
            return null;
        }
    }

    // ---------------------------------------------------------------
    // Supporting types
    // ---------------------------------------------------------------
    public enum SpawnMode { Auto, Manual, Tilemap }

    [System.Serializable]
    public class EnemySpawnEntry
    {
        public GameObject prefab;
        public Vector2Int spawnCell = new Vector2Int(5, 5);
    }
}