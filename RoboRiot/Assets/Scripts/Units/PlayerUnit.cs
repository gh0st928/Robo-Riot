using System.Collections.Generic;
using UnityEngine;
using RoboRiot.Grid;
using RoboRiot.Controls;
using RoboRiot.Combat;

namespace RoboRiot.Units
{
    /// <summary>
    /// Player controlled robot unit.
    /// Listens to InputHandler, uses CombatCalculator for attacks,
    /// and notifies TurnManager when the turn ends.
    /// </summary>
    public class PlayerUnit : Unit
    {
        // ---------------------------------------------------------------
        // State
        // ---------------------------------------------------------------
        private bool           _isMyTurn        = false;
        private int            _selectedAbility  = -1;
        private List<GridCell> _reachableCells   = new();

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        protected override void Awake() => base.Awake();

        private void Start()
        {
            base.Start();
            SubscribeToInput();
        }

        private void OnDisable() => UnsubscribeFromInput();

        private void SubscribeToInput()
        {
            if (InputHandler.Instance == null) return;
            InputHandler.Instance.OnWorldClicked.AddListener(OnWorldClicked);
            InputHandler.Instance.OnAbilitySelected.AddListener(OnAbilityKeyPressed);
            InputHandler.Instance.OnEndTurn.AddListener(OnEndTurnPressed);
            InputHandler.Instance.OnCancelPressed.AddListener(OnCancelPressed);
        }

        private void UnsubscribeFromInput()
        {
            if (InputHandler.Instance == null) return;
            InputHandler.Instance.OnWorldClicked.RemoveListener(OnWorldClicked);
            InputHandler.Instance.OnAbilitySelected.RemoveListener(OnAbilityKeyPressed);
            InputHandler.Instance.OnEndTurn.RemoveListener(OnEndTurnPressed);
            InputHandler.Instance.OnCancelPressed.RemoveListener(OnCancelPressed);
        }

        // ---------------------------------------------------------------
        // Turn
        // ---------------------------------------------------------------
        public override void StartTurn()
        {
            base.StartTurn();
            _isMyTurn        = true;
            _selectedAbility = -1;
            HighlightReachableCells();
            Debug.Log($"[PlayerUnit] Your turn! AP: {CurrentActionPoints}. Move: click tile. Ability: press 1-6. End turn: Space.");
        }

        public override void EndTurn()
        {
            base.EndTurn();
            _isMyTurn        = false;
            _selectedAbility = -1;
            ClearReachableHighlights();
            TurnManager.Instance?.NotifyTurnEnded(this);
        }

        // ---------------------------------------------------------------
        // Input callbacks
        // ---------------------------------------------------------------
        private void OnWorldClicked(Vector3 worldPos)
        {
            if (!_isMyTurn) return;

            GridCell clickedCell = GridManager.Instance?.GetCellAtWorldPos(worldPos);
            if (clickedCell == null) return;

            // If ability selected — try to use it on target in clicked cell
            if (_selectedAbility >= 0)
            {
                Unit target = clickedCell.IsOccupied
                    ? clickedCell.OccupyingUnit.GetComponent<Unit>()
                    : null;

                TryUseAbility(_selectedAbility, target, clickedCell);
                return;
            }

            // Otherwise try to move
            TryMoveTo(clickedCell);
        }

        private void OnAbilityKeyPressed(int slotIndex)
        {
            if (!_isMyTurn) return;
            if (Abilities == null || slotIndex >= Abilities.Length) return;
            if (Abilities[slotIndex] == null) return;

            _selectedAbility = slotIndex;
            Debug.Log($"[PlayerUnit] Selected: {Abilities[slotIndex].abilityName} — click a target.");
        }

        private void OnEndTurnPressed()
        {
            if (!_isMyTurn) return;
            EndTurn();
        }

        private void OnCancelPressed()
        {
            if (!_isMyTurn) return;
            _selectedAbility = -1;
            Debug.Log("[PlayerUnit] Selection cancelled.");
        }

        // ---------------------------------------------------------------
        // Movement
        // ---------------------------------------------------------------
        private void TryMoveTo(GridCell targetCell)
        {
            if (!targetCell.IsWalkable)             { Debug.Log("[PlayerUnit] Not walkable.");  return; }
            if (targetCell.IsOccupied)              { Debug.Log("[PlayerUnit] Cell occupied."); return; }
            if (!_reachableCells.Contains(targetCell)) { Debug.Log("[PlayerUnit] Out of range."); return; }
            if (!SpendAP(1))                        return;

            ClearReachableHighlights();
            PlaceOnCell(targetCell);
            CombatLog.Instance?.LogMove(this, targetCell);
            HighlightReachableCells();
        }

        // ---------------------------------------------------------------
        // Abilities
        // ---------------------------------------------------------------
        private void TryUseAbility(int slotIndex, Unit target, GridCell targetCell)
        {
            if (Abilities == null || slotIndex >= Abilities.Length) return;
            AbilityData ability = Abilities[slotIndex];
            if (ability == null) return;

            // Validate target
            if (ability.canTargetEnemies && target == null)
            {
                Debug.Log("[PlayerUnit] No target in that cell.");
                return;
            }

            // Check range
            if (CurrentCell != null && targetCell != null)
            {
                int dist = Mathf.Abs(targetCell.X - CurrentCell.X)
                         + Mathf.Abs(targetCell.Y - CurrentCell.Y);
                if (dist > ability.range)
                {
                    Debug.Log($"[PlayerUnit] Target out of range. ({dist} > {ability.range})");
                    return;
                }
            }

            // Check line of sight
            if (ability.requiresLineOfSight && target != null)
            {
                if (!GridManager.Instance.HasLineOfSight(CurrentCell, target.CurrentCell))
                {
                    Debug.Log("[PlayerUnit] No line of sight to target.");
                    return;
                }
            }

            if (!SpendAP(ability.apCost)) return;

            // Resolve attack through CombatCalculator
            if (ability.type == AbilityType.Attack && target != null)
            {
                var result = CombatCalculator.ResolveAttack(this, target, ability);
                CombatLog.Instance?.LogAttack(result);
            }
            else
            {
                UseAbility(slotIndex, target);
            }

            _selectedAbility = -1;
        }

        // ---------------------------------------------------------------
        // Move range highlights
        // ---------------------------------------------------------------
        private void HighlightReachableCells()
        {
            if (GridManager.Instance == null || CurrentCell == null) return;
            _reachableCells = GridManager.Instance.GetCellsInRange(CurrentCell, MoveRange, walkableOnly: true);

            var vis = FindObjectOfType<GridVisualizer>();
            if (vis == null) return;
            foreach (var cell in _reachableCells)
                vis.SetHighlight(cell.X, cell.Y, new Color(0.2f, 0.6f, 1f, 0.5f));
        }

        private void ClearReachableHighlights()
        {
            FindObjectOfType<GridVisualizer>()?.ClearHighlights();
            _reachableCells.Clear();
        }
    }
}