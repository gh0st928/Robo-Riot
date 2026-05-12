using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoboRiot.Grid
{
    /// <summary>
    /// Reads painted Unity Tilemaps and stamps TileData into GridManager cells.
    /// Attach to the GridManager GameObject alongside GridManager.cs.
    /// </summary>
    public class TilemapLoader : MonoBehaviour
    {
        [Header("Tilemap Layers")]
        [SerializeField] private Tilemap tilemapFloor;
        [SerializeField] private Tilemap tilemapWalls;
        [SerializeField] private Tilemap tilemapCover;
        [SerializeField] private Tilemap tilemapHeavyCover;
        [SerializeField] private Tilemap tilemapHazards;
        [SerializeField] private Tilemap tilemapSpawns;
        [SerializeField] private Tilemap tilemapObjectives;

        [Header("TileData Assets (assign matching ScriptableObjects)")]
        [SerializeField] private TileData dataFloor;
        [SerializeField] private TileData dataWall;
        [SerializeField] private TileData dataCover;
        [SerializeField] private TileData dataHeavyCover;
        [SerializeField] private TileData dataHazard;
        [SerializeField] private TileData dataSpawn;
        [SerializeField] private TileData dataObjective;

        private void Start() => LoadAll();

        public void LoadAll()
        {
            var gm = GridManager.Instance;
            if (gm == null) { Debug.LogError("[TilemapLoader] GridManager not found."); return; }

            ReadLayer(tilemapFloor,      dataFloor,      gm);
            ReadLayer(tilemapCover,      dataCover,      gm);
            ReadLayer(tilemapHeavyCover, dataHeavyCover, gm);
            ReadLayer(tilemapHazards,    dataHazard,     gm);
            ReadLayer(tilemapSpawns,     dataSpawn,      gm);
            ReadLayer(tilemapObjectives, dataObjective,  gm);
            ReadLayer(tilemapWalls,      dataWall,       gm);  // Walls last — always win

            Debug.Log("[TilemapLoader] Loaded.");
        }

        private void ReadLayer(Tilemap tilemap, TileData data, GridManager gm)
        {
            if (tilemap == null || data == null) return;
            tilemap.CompressBounds();
            BoundsInt bounds = tilemap.cellBounds;

            foreach (var pos in bounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(pos)) continue;
                int gx = pos.x - bounds.xMin;
                int gy = pos.y - bounds.yMin;
                if (!gm.InBounds(gx, gy)) continue;
                gm.SetTileData(gx, gy, data);
            }
        }
    }
}