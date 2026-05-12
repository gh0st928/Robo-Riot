using UnityEngine;

namespace RoboRiot.Grid
{
    /// <summary>
    /// Represents a single tile in the grid.
    /// GridManager creates one GridCell GameObject per tile automatically.
    /// You do not need to create or attach these manually.
    /// </summary>
    public class GridCell : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Grid position (set by GridManager on creation)
        // ---------------------------------------------------------------
        [Header("Grid Position (set automatically)")]
        [SerializeField] private int gridX;
        [SerializeField] private int gridY;

        public int X => gridX;
        public int Y => gridY;

        // ---------------------------------------------------------------
        // Tile data
        // ---------------------------------------------------------------
        [Header("Tile Data")]
        [SerializeField] private TileData tileData;

        public TileData Data          => tileData;
        public TileType Type          => tileData != null ? tileData.tileType          : TileType.Empty;
        public bool IsWalkable        => tileData != null ? tileData.isWalkable         : false;
        public bool BlocksLineOfSight => tileData != null ? tileData.blocksLineOfSight  : false;
        public float CoverValue       => tileData != null ? tileData.coverValue         : 0f;
        public int MoveCost           => tileData != null ? tileData.moveCost           : 1;

        // ---------------------------------------------------------------
        // Runtime state
        // ---------------------------------------------------------------
        [Header("Runtime State (read-only)")]
        [SerializeField] private GameObject occupyingUnit;

        public bool IsOccupied          => occupyingUnit != null;
        public GameObject OccupyingUnit => occupyingUnit;

        // ---------------------------------------------------------------
        // API
        // ---------------------------------------------------------------
        public void Initialise(int x, int y, TileData data)
        {
            gridX    = x;
            gridY    = y;
            tileData = data;
            gameObject.name = $"Cell ({x},{y})";
        }

        public void SetOccupant(GameObject unit) => occupyingUnit = unit;
        public void ClearOccupant()              => occupyingUnit = null;
        public void SetTileData(TileData data)   => tileData = data;

        public override string ToString() =>
            $"Cell({gridX},{gridY}) [{Type}]{(IsOccupied ? " OCCUPIED" : "")}";
    }
}