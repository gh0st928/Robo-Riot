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
    ///
    /// Two modes:
    ///  - Manual: set spawn coordinates directly in the Inspector
    ///  - Tilemap: paint spawn tiles on Tilemap_Spawns and let this
    ///    script read them automatically
    ///
    /// Setup:
    ///  1. Create an empty GameObject, name it "UnitSpawner"
    ///  2. Attach this script
    ///  3. Choose a spawn mode in the Inspector
    ///  4. Assign prefabs and spawn data accordingly
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector
        // ---------------------------------------------------------------
        [Header("Spawn Mode")]
        [SerializeField] private SpawnMode spawnMode = SpawnMode.Manual;

        [Header("Tilemap Mode — assign Tilemap_Spawns layer")]
        [SerializeField] private Tilemap spawnTilemap;

        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector2Int playerSpawnCell = new Vector2Int(1, 1);

        [Header("Enemies")]
        [SerializeField] private List<EnemySpawnEntry> enemySpawns = new();

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
        // Main spawn logic
        // ---------------------------------------------------------------
        public void SpawnAll()
        {
            if (spawnMode == SpawnMode.Tilemap)
                SpawnFromTilemap();
            else
                SpawnManual();
        }

        // ---------------------------------------------------------------
        // Mode A: Manual spawn from Inspector coordinates
        // ---------------------------------------------------------------
        private void SpawnManual()
        {
            SpawnPlayer(GetOrFindCell(playerSpawnCell));

            foreach (var entry in enemySpawns)
            {
                if (entry.prefab == null) continue;
                GridCell cell = GetOrFindCell(entry.spawnCell);
                SpawnEnemy(entry.prefab, cell);
            }
        }

        // ---------------------------------------------------------------
        // Mode B: Read spawn positions from Tilemap_Spawns layer
        // ---------------------------------------------------------------
        private void SpawnFromTilemap()
        {
            if (spawnTilemap == null)
            {
                Debug.LogError("[UnitSpawner] Tilemap mode selected but no spawn tilemap assigned. Falling back to manual.");
                SpawnManual();
                return;
            }

            spawnTilemap.CompressBounds();
            BoundsInt bounds = spawnTilemap.cellBounds;

            // Collect all painted spawn positions
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
                Debug.LogWarning("[UnitSpawner] No spawn tiles found on tilemap. Falling back to manual.");
                SpawnManual();
                return;
            }

            // First spawn tile = player, rest = enemies (in order painted)
            SpawnPlayer(spawnCells[0]);

            for (int i = 1; i < spawnCells.Count; i++)
            {
                if (i - 1 >= enemySpawns.Count) break;
                if (enemySpawns[i - 1].prefab == null) continue;
                SpawnEnemy(enemySpawns[i - 1].prefab, spawnCells[i]);
            }

            Debug.Log($"[UnitSpawner] Spawned from tilemap. {spawnCells.Count} spawn points found.");
        }

        // ---------------------------------------------------------------
        // Spawn helpers
        // ---------------------------------------------------------------
        private void SpawnPlayer(GridCell cell)
        {
            if (playerPrefab == null) { Debug.LogError("[UnitSpawner] No player prefab assigned."); return; }
            if (cell == null)         { Debug.LogError("[UnitSpawner] No valid spawn cell for player."); return; }

            GameObject obj = Instantiate(playerPrefab);
            SpawnedPlayer = obj.GetComponent<PlayerUnit>();

            if (SpawnedPlayer == null)
            {
                Debug.LogError("[UnitSpawner] Player prefab is missing a PlayerUnit component.");
                Destroy(obj);
                return;
            }

            SpawnedPlayer.PlaceOnCell(cell);
            Debug.Log($"[UnitSpawner] Player spawned at {cell}.");
        }

        private void SpawnEnemy(GameObject prefab, GridCell cell)
        {
            if (cell == null) { Debug.LogWarning("[UnitSpawner] No valid spawn cell for enemy. Skipping."); return; }

            GameObject obj   = Instantiate(prefab);
            EnemyUnit  enemy = obj.GetComponent<EnemyUnit>();

            if (enemy == null)
            {
                Debug.LogError("[UnitSpawner] Enemy prefab is missing an EnemyUnit component.");
                Destroy(obj);
                return;
            }

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
                Debug.LogWarning($"[UnitSpawner] Cell {coord} unavailable. Searching for nearest walkable...");
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
                if (cell != null && cell.IsWalkable && !cell.IsOccupied)
                    return cell;
            }
            return null;
        }
    }

    // ---------------------------------------------------------------
    // Supporting types
    // ---------------------------------------------------------------
    public enum SpawnMode
    {
        Manual,    // Set spawn coordinates in Inspector
        Tilemap    // Read from Tilemap_Spawns layer
    }

    [System.Serializable]
    public class EnemySpawnEntry
    {
        public GameObject prefab;
        public Vector2Int spawnCell = new Vector2Int(5, 5);
    }
}