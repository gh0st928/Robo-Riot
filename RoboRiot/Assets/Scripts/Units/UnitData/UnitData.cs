using UnityEngine;

namespace RoboRiot.Units
{
    /// <summary>
    /// Data asset defining a unit type's stats and abilities.
    /// Create via: Right Click > Create > RoboRiot > Unit Data
    /// Make one for each unit type: PlayerBot, EnemyDrone, EnemyTank, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitData", menuName = "RoboRiot/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("Identity")]
        public string unitName    = "Unit";
        public Sprite portrait;             // shown in UI
        public Color  debugColor  = Color.white;

        [Header("Faction")]
        public Faction faction    = Faction.Enemy;

        [Header("Core Stats")]
        public int maxHealth      = 10;
        public int maxActionPoints = 2;    // AP per turn
        public int moveRange      = 4;     // tiles per move action
        public int initiative     = 5;     // turn order — higher goes first

        [Header("Combat Stats")]
        public int attackDamage   = 3;
        public int attackRange    = 5;     // tiles
        public int defense        = 0;     // flat damage reduction
        public int accuracy       = 80;    // % hit chance base

        [Header("Abilities")]
        public AbilityData[] abilities;    // assign up to 6
    }

    public enum Faction
    {
        Player,
        Enemy
    }
}