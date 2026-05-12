using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoboRiot.Grid;
using RoboRiot.Combat;

namespace RoboRiot.Units
{
    /// <summary>
    /// Enemy unit with basic AI.
    /// Uses CombatCalculator for attacks and notifies TurnManager when done.
    /// </summary>
    public class EnemyUnit : Unit
    {
        [Header("AI Settings")]
        [SerializeField] private float actionDelay = 0.6f;

        private Unit _playerUnit;

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        protected override void Start()
        {
            base.Start();
            _playerUnit = FindFirstObjectByType<PlayerUnit>();
        }

        // ---------------------------------------------------------------
        // Turn
        // ---------------------------------------------------------------
        public override void StartTurn()
        {
            base.StartTurn();
            StartCoroutine(RunAI());
        }

        // ---------------------------------------------------------------
        // AI routine
        // ---------------------------------------------------------------
        private IEnumerator RunAI()
        {
            yield return new WaitForSeconds(actionDelay);

            if (_playerUnit == null || !_playerUnit.IsAlive)
            {
                EndTurn();
                yield break;
            }

            while (HasActionsLeft)
            {
                bool acted = TryAttack();
                if (!acted) acted = TryMoveTowardsPlayer();
                if (!acted) break;

                yield return new WaitForSeconds(actionDelay);

                // Try attacking again after moving
                if (acted) TryAttack();
            }

            EndTurn();
        }

        public override void EndTurn()
        {
            base.EndTurn();
            TurnManager.Instance?.NotifyTurnEnded(this);
        }

        // ---------------------------------------------------------------
        // AI: Attack
        // ---------------------------------------------------------------
        private bool TryAttack()
        {
            if (_playerUnit == null || CurrentCell == null) return false;

            int dist = Mathf.Abs(_playerUnit.CurrentCell.X - CurrentCell.X)
                     + Mathf.Abs(_playerUnit.CurrentCell.Y - CurrentCell.Y);

            if (dist > AttackRange) return false;

            if (!GridManager.Instance.HasLineOfSight(CurrentCell, _playerUnit.CurrentCell))
                return false;

            // Use first attack ability if available
            if (Abilities != null)
            {
                for (int i = 0; i < Abilities.Length; i++)
                {
                    if (Abilities[i] != null && Abilities[i].type == AbilityType.Attack)
                    {
                        if (!SpendAP(Abilities[i].apCost)) return false;
                        var result = CombatCalculator.ResolveAttack(this, _playerUnit, Abilities[i]);
                        CombatLog.Instance?.LogAttack(result);
                        return true;
                    }
                }
            }

            // Fallback basic attack
            if (!SpendAP(1)) return false;
            var basicResult = CombatCalculator.ResolveAttack(this, _playerUnit);
            CombatLog.Instance?.LogAttack(basicResult);
            return true;
        }

        // ---------------------------------------------------------------
        // AI: Move towards player
        // ---------------------------------------------------------------
        private bool TryMoveTowardsPlayer()
        {
            if (_playerUnit == null || CurrentCell == null) return false;

            List<GridCell> reachable = GridManager.Instance.GetCellsInRange(
                CurrentCell, MoveRange, walkableOnly: true
            );

            GridCell bestCell = null;
            int      bestDist = int.MaxValue;

            foreach (var cell in reachable)
            {
                if (cell.IsOccupied) continue;
                int dist = Mathf.Abs(_playerUnit.CurrentCell.X - cell.X)
                         + Mathf.Abs(_playerUnit.CurrentCell.Y - cell.Y);
                if (dist < bestDist) { bestDist = dist; bestCell = cell; }
            }

            if (bestCell == null || !SpendAP(1)) return false;

            PlaceOnCell(bestCell);
            CombatLog.Instance?.LogMove(this, bestCell);
            return true;
        }

        // ---------------------------------------------------------------
        // Death
        // ---------------------------------------------------------------
        protected override void Die()
        {
            CombatLog.Instance?.LogDeath(this);
            base.Die();
        }
    }
}