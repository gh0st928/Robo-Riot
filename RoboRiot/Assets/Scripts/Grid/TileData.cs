using UnityEngine;

namespace RoboRiot.Grid
{
    public enum TileType
    {
        Empty,
        Floor,
        Wall,
        Cover,
        HeavyCover,
        Hazard,
        Spawn,
        Objective
    }

    /// <summary>
    /// Data asset for a single tile type.
    /// Create via: Right Click in Project > Create > RoboRiot > Tile Data
    /// </summary>
    [CreateAssetMenu(fileName = "TileData", menuName = "RoboRiot/Tile Data")]
    public class TileData : ScriptableObject
    {
        [Header("Tile Settings")]
        public TileType tileType = TileType.Floor;
        public Color    debugColor = Color.grey;

        [Header("Gameplay")]
        public bool isWalkable        = true;
        public bool blocksLineOfSight = false;
        public float coverValue       = 0f;   // 0 = none, 0.5 = half, 1.0 = full
        public int  moveCost          = 1;    // AP cost to enter this tile
    }
}