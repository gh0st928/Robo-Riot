using UnityEngine;
using RoboRiot.Grid;
using RoboRiot.Units;

namespace RoboRiot.Combat
{
    /// <summary>
    /// Stateless calculator for all combat math.
    /// Call CombatCalculator.ResolveAttack() to process an attack.
    /// No MonoBehaviour needed — pure static logic.
    /// </summary>
    public static class CombatCalculator
    {
        // ---------------------------------------------------------------
        // Constants
        // ---------------------------------------------------------------
        private const int BaseHitChance     = 100;  // Before modifiers
        private const int MinHitChance      = 5;    // Always at least 5% chance to hit
        private const int MaxHitChance      = 95;   // Never guaranteed to hit
        private const float DamageVariance  = 0.25f; // ±25% damage roll

        // ---------------------------------------------------------------
        // Attack result data
        // ---------------------------------------------------------------
        public struct AttackResult
        {
            public bool  Hit;
            public bool  Missed;
            public int   DamageDealt;
            public int   RawDamage;         // Before cover/defense reduction
            public int   HitChance;         // Final hit % shown to player
            public float CoverReduction;    // 0-1, how much cover helped
            public string Summary;          // Human readable log entry
        }

        // ---------------------------------------------------------------
        // Main entry point
        // ---------------------------------------------------------------

        /// <summary>
        /// Resolves a full attack from attacker to defender.
        /// Applies damage to the defender automatically.
        /// Returns an AttackResult with all the details.
        /// </summary>
        public static AttackResult ResolveAttack(Unit attacker, Unit defender, AbilityData ability = null)
        {
            AttackResult result = new();

            // --- Step 1: Calculate hit chance ---
            result.HitChance = CalculateHitChance(attacker, defender, ability);

            // --- Step 2: Roll to hit ---
            int roll = Random.Range(1, 101);
            result.Hit    = roll <= result.HitChance;
            result.Missed = !result.Hit;

            if (result.Missed)
            {
                result.Summary = $"{attacker.UnitName} attacks {defender.UnitName} — MISS! (rolled {roll}, needed {result.HitChance})";
                return result;
            }

            // --- Step 3: Roll damage ---
            int baseDamage = ability != null
                ? ability.power + attacker.AttackDamage
                : attacker.AttackDamage;

            result.RawDamage  = RollDamage(baseDamage);

            // --- Step 4: Apply cover reduction ---
            result.CoverReduction = GetCoverReduction(attacker, defender);
            int afterCover        = Mathf.RoundToInt(result.RawDamage * (1f - result.CoverReduction));

            // --- Step 5: Apply defense ---
            result.DamageDealt = Mathf.Max(1, afterCover - defender.Defense);

            // --- Step 6: Apply damage to defender ---
            defender.TakeDamage(result.DamageDealt);

            // --- Step 7: Build summary ---
            string coverNote = result.CoverReduction > 0
                ? $" (cover -{Mathf.RoundToInt(result.CoverReduction * 100)}%)"
                : "";

            result.Summary = $"{attacker.UnitName} hits {defender.UnitName} for {result.DamageDealt} damage{coverNote}. " +
                             $"(rolled {roll}/{result.HitChance}, dmg {result.RawDamage}→{result.DamageDealt})";

            return result;
        }

        // ---------------------------------------------------------------
        // Hit chance calculation
        // ---------------------------------------------------------------
        public static int CalculateHitChance(Unit attacker, Unit defender, AbilityData ability = null)
        {
            int chance = attacker.Accuracy;

            // Range penalty — accuracy drops off past half the weapon's range
            int maxRange  = ability != null ? ability.range : attacker.AttackRange;
            int dist      = ManhattanDistance(attacker.CurrentCell, defender.CurrentCell);
            int halfRange = maxRange / 2;

            if (dist > halfRange)
            {
                int penalty = (dist - halfRange) * 10;
                chance -= penalty;
            }

            // Cover penalty — cover makes the target harder to hit
            float cover = GetCoverReduction(attacker, defender);
            chance -= Mathf.RoundToInt(cover * 40f);   // Max -40% from full cover

            // Line of sight bonus — flanking (no cover between units) gives +10
            if (GridManager.Instance != null &&
                attacker.CurrentCell != null &&
                defender.CurrentCell != null)
            {
                bool los = GridManager.Instance.HasLineOfSight(attacker.CurrentCell, defender.CurrentCell);
                if (!los) chance -= 20;   // Indirect fire penalty
            }

            return Mathf.Clamp(chance, MinHitChance, MaxHitChance);
        }

        // ---------------------------------------------------------------
        // Damage roll
        // ---------------------------------------------------------------
        public static int RollDamage(int baseDamage)
        {
            float variance = Random.Range(-DamageVariance, DamageVariance);
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * (1f + variance)));
        }

        // ---------------------------------------------------------------
        // Cover
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns the cover reduction value (0-1) the defender gets.
        /// Cover is directional — only applies if cover is between attacker and defender.
        /// </summary>
        public static float GetCoverReduction(Unit attacker, Unit defender)
        {
            if (defender.CurrentCell == null || attacker.CurrentCell == null)
                return 0f;

            // Base cover value of the defender's tile
            float coverValue = defender.CurrentCell.CoverValue;
            if (coverValue <= 0f) return 0f;

            // Check if the cover is actually facing the attacker
            // (cover on the side closest to the attacker counts)
            Vector2Int attackDir = new Vector2Int(
                (int)Mathf.Sign(attacker.CurrentCell.X - defender.CurrentCell.X),
                (int)Mathf.Sign(attacker.CurrentCell.Y - defender.CurrentCell.Y)
            );

            GridCell coverCell = GridManager.Instance?.GetCell(
                defender.CurrentCell.X + attackDir.x,
                defender.CurrentCell.Y + attackDir.y
            );

            // If the cell between defender and attacker is cover, it applies
            if (coverCell != null && coverCell.CoverValue > 0f)
                return coverCell.CoverValue;

            // Otherwise the defender's own tile cover applies (they're in cover)
            return coverValue;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private static int ManhattanDistance(GridCell a, GridCell b)
        {
            if (a == null || b == null) return 0;
            return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
        }
    }
}