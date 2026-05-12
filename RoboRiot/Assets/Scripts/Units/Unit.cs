using System.Collections.Generic;
using UnityEngine;
using RoboRiot.Grid;

namespace RoboRiot.Units
{
    /// <summary>
    /// Base class for all units (player and enemy).
    /// Handles stats, health, action points, and grid position.
    /// Attach to a unit GameObject alongside a SpriteRenderer.
    /// </summary>
    public class Unit : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector
        // ---------------------------------------------------------------
        [Header("Unit Data")]
        [SerializeField] protected UnitData data;

        // ---------------------------------------------------------------
        // Runtime stats (current values, change during gameplay)
        // ---------------------------------------------------------------
        public string UnitName        => data != null ? data.unitName : "Unknown";
        public Faction Faction        => data != null ? data.faction  : Faction.Enemy;

        public int MaxHealth          => data != null ? data.maxHealth         : 0;
        public int MaxActionPoints    => data != null ? data.maxActionPoints   : 0;
        public int MoveRange          => data != null ? data.moveRange         : 0;
        public int Initiative         => data != null ? data.initiative        : 0;
        public int AttackDamage       => data != null ? data.attackDamage      : 0;
        public int AttackRange        => data != null ? data.attackRange       : 0;
        public int Defense            => data != null ? data.defense           : 0;
        public int Accuracy           => data != null ? data.accuracy          : 0;
        public AbilityData[] Abilities => data != null ? data.abilities        : null;

        // Current values
        public int  CurrentHealth      { get; private set; }
        public int  CurrentActionPoints { get; private set; }
        public bool IsAlive            => CurrentHealth > 0;
        public bool HasActionsLeft     => CurrentActionPoints > 0;

        // ---------------------------------------------------------------
        // Grid position
        // ---------------------------------------------------------------
        public GridCell CurrentCell { get; private set; }

        // ---------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------
        public System.Action<int, int> OnHealthChanged;    // (current, max)
        public System.Action<int, int> OnAPChanged;        // (current, max)
        public System.Action           OnUnitDied;
        public System.Action           OnTurnStarted;
        public System.Action           OnTurnEnded;

        // ---------------------------------------------------------------
        // Components
        // ---------------------------------------------------------------
        protected SpriteRenderer _spriteRenderer;

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        protected virtual void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected virtual void Start()
        {
            Initialise();
        }

        // ---------------------------------------------------------------
        // Initialisation
        // ---------------------------------------------------------------
        public virtual void Initialise()
        {
            if (data == null)
            {
                Debug.LogError($"[Unit] {gameObject.name} has no UnitData assigned!");
                return;
            }

            CurrentHealth       = data.maxHealth;
            CurrentActionPoints = 0;   // AP is granted at turn start

            // Apply debug colour
            if (_spriteRenderer != null)
                _spriteRenderer.color = data.debugColor;

            gameObject.name = data.unitName;
        }

        // ---------------------------------------------------------------
        // Turn management
        // ---------------------------------------------------------------
        public virtual void StartTurn()
        {
            CurrentActionPoints = MaxActionPoints;
            OnAPChanged?.Invoke(CurrentActionPoints, MaxActionPoints);
            OnTurnStarted?.Invoke();
            Debug.Log($"[Unit] {UnitName}'s turn started. AP: {CurrentActionPoints}");
        }

        public virtual void EndTurn()
        {
            CurrentActionPoints = 0;
            OnAPChanged?.Invoke(CurrentActionPoints, MaxActionPoints);
            OnTurnEnded?.Invoke();
            Debug.Log($"[Unit] {UnitName}'s turn ended.");
        }

        public bool SpendAP(int amount)
        {
            if (CurrentActionPoints < amount)
            {
                Debug.Log($"[Unit] {UnitName} not enough AP. Has {CurrentActionPoints}, needs {amount}.");
                return false;
            }
            CurrentActionPoints -= amount;
            OnAPChanged?.Invoke(CurrentActionPoints, MaxActionPoints);
            return true;
        }

        // ---------------------------------------------------------------
        // Health
        // ---------------------------------------------------------------
        public virtual void TakeDamage(int damage)
        {
            int mitigated = Mathf.Max(0, damage - Defense);
            CurrentHealth = Mathf.Max(0, CurrentHealth - mitigated);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            Debug.Log($"[Unit] {UnitName} took {mitigated} damage. HP: {CurrentHealth}/{MaxHealth}");

            if (CurrentHealth <= 0) Die();
        }

        public virtual void Heal(int amount)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            Debug.Log($"[Unit] {UnitName} healed {amount}. HP: {CurrentHealth}/{MaxHealth}");
        }

        protected virtual void Die()
        {
            Debug.Log($"[Unit] {UnitName} has been destroyed.");
            OnUnitDied?.Invoke();

            // Free the grid cell
            if (CurrentCell != null) CurrentCell.ClearOccupant();

            gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------
        // Grid movement
        // ---------------------------------------------------------------
        public virtual void PlaceOnCell(GridCell cell)
        {
            // Free previous cell
            if (CurrentCell != null) CurrentCell.ClearOccupant();

            // Occupy new cell
            CurrentCell = cell;
            CurrentCell.SetOccupant(gameObject);

            // Move to world position
            transform.position = new Vector3(
                cell.transform.position.x,
                cell.transform.position.y,
                -1f   // Z=-1 puts units in front of tiles
            );
        }

        // ---------------------------------------------------------------
        // Abilities
        // ---------------------------------------------------------------
        public virtual bool UseAbility(int slotIndex, Unit target)
        {
            if (Abilities == null || slotIndex >= Abilities.Length) return false;

            AbilityData ability = Abilities[slotIndex];
            if (ability == null) return false;

            if (!SpendAP(ability.apCost)) return false;

            ExecuteAbility(ability, target);
            return true;
        }

        protected virtual void ExecuteAbility(AbilityData ability, Unit target)
        {
            switch (ability.type)
            {
                case AbilityType.Attack:
                    target?.TakeDamage(ability.power + AttackDamage);
                    break;
                case AbilityType.Heal:
                    Heal(ability.power);
                    break;
                default:
                    Debug.Log($"[Unit] {UnitName} used {ability.abilityName}.");
                    break;
            }
        }
    }
}