using UnityEngine;

namespace RoboRiot.Units
{
    public enum AbilityType
    {
        Attack,       // Deal damage to a target
        Move,         // Move to a tile
        Heal,         // Restore health
        Shield,       // Reduce incoming damage
        Hack,         // Disable an enemy for a turn
        Overcharge    // Boost stats temporarily
    }

    /// <summary>
    /// Data asset defining a single ability.
    /// Create via: Right Click > Create > RoboRiot > Ability Data
    /// </summary>
    [CreateAssetMenu(fileName = "AbilityData", menuName = "RoboRiot/Ability Data")]
    public class AbilityData : ScriptableObject
    {
        [Header("Identity")]
        public string      abilityName  = "Ability";
        public string      description  = "";
        public Sprite      icon;
        public AbilityType type         = AbilityType.Attack;

        [Header("Cost")]
        public int apCost    = 1;         // Action Points required

        [Header("Range & Area")]
        public int range     = 5;         // Max tile range
        public int areaOfEffect = 0;      // 0 = single target, 1+ = radius

        [Header("Effect")]
        public int power     = 3;         // Damage, heal amount, etc.
        public int duration  = 0;         // Turns the effect lasts (0 = instant)

        [Header("Targeting")]
        public bool requiresLineOfSight = true;
        public bool canTargetSelf       = false;
        public bool canTargetAllies     = false;
        public bool canTargetEnemies    = true;
    }
}