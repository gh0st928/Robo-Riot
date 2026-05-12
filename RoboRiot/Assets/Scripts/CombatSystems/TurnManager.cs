using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRiot.Grid;
using RoboRiot.Units;

namespace RoboRiot.Combat
{
    /// <summary>
    /// Manages the turn queue for all units in the scene.
    /// Units are sorted by initiative (highest first) at the start of each round.
    /// Handles player and enemy turns, end conditions, and round tracking.
    ///
    /// Setup:
    ///  1. Create an empty GameObject, name it "TurnManager"
    ///  2. Attach this script
    ///  3. TurnManager finds all units automatically on Start
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Singleton
        // ---------------------------------------------------------------
        public static TurnManager Instance { get; private set; }

        // ---------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------
        public System.Action<Unit>  OnTurnStarted;      // Fired when a unit's turn begins
        public System.Action<Unit>  OnTurnEnded;        // Fired when a unit's turn ends
        public System.Action<int>   OnRoundStarted;     // Fired at the start of each round
        public System.Action        OnPlayerVictory;    // All enemies defeated
        public System.Action        OnPlayerDefeat;     // Player unit defeated

        // ---------------------------------------------------------------
        // State
        // ---------------------------------------------------------------
        public Unit         ActiveUnit    { get; private set; }
        public int          CurrentRound  { get; private set; } = 1;
        public bool         CombatActive  { get; private set; } = false;

        private List<Unit>  _turnQueue    = new();
        private int         _queueIndex   = 0;

        [Header("Settings")]
        [SerializeField] private float enemyTurnDelay = 0.5f;   // Pause before enemy acts

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Wait a frame so UnitSpawner has finished placing units
            StartCoroutine(StartCombatNextFrame());
        }

        private IEnumerator StartCombatNextFrame()
        {
            yield return null;
            StartCombat();
        }

        // ---------------------------------------------------------------
        // Combat start
        // ---------------------------------------------------------------
        public void StartCombat()
        {
            BuildTurnQueue();

            if (_turnQueue.Count == 0)
            {
                Debug.LogWarning("[TurnManager] No units found in scene.");
                return;
            }

            CombatActive = true;
            CurrentRound = 1;
            _queueIndex  = 0;

            Debug.Log($"[TurnManager] Combat started. Round {CurrentRound}. {_turnQueue.Count} units in queue.");
            OnRoundStarted?.Invoke(CurrentRound);

            StartCoroutine(RunTurnQueue());
        }

        // ---------------------------------------------------------------
        // Turn queue
        // ---------------------------------------------------------------
        private void BuildTurnQueue()
        {
            // Find all active units in the scene
            var allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None)
                .Where(u => u.IsAlive && u.gameObject.activeInHierarchy)
                .OrderByDescending(u => u.Initiative)   // Highest initiative first
                .ToList();

            _turnQueue  = allUnits;
            _queueIndex = 0;

            Debug.Log($"[TurnManager] Turn order: {string.Join(" → ", _turnQueue.Select(u => $"{u.UnitName}({u.Initiative})"))}");
        }

        private IEnumerator RunTurnQueue()
        {
            while (CombatActive)
            {
                // Check end conditions before each turn
                if (CheckEndConditions()) yield break;

                // Get next living unit
                Unit unit = GetNextLivingUnit();
                if (unit == null) yield break;

                ActiveUnit = unit;
                OnTurnStarted?.Invoke(unit);

                // Enemy turns have a delay so the player can follow along
                if (unit is EnemyUnit)
                    yield return new WaitForSeconds(enemyTurnDelay);

                // Start the unit's turn
                unit.StartTurn();

                // Wait for the unit to finish its turn
                yield return new WaitUntil(() => !IsUnitTakingTurn(unit));

                OnTurnEnded?.Invoke(unit);

                // Advance queue
                _queueIndex++;

                // New round when we've gone through everyone
                if (_queueIndex >= _turnQueue.Count)
                {
                    _queueIndex = 0;
                    CurrentRound++;

                    // Rebuild queue each round to remove dead units
                    BuildTurnQueue();

                    Debug.Log($"[TurnManager] Round {CurrentRound} started.");
                    OnRoundStarted?.Invoke(CurrentRound);

                    yield return new WaitForSeconds(0.25f);
                }
            }
        }

        // ---------------------------------------------------------------
        // Unit turn tracking
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns true if the unit still has AP and it's their turn.
        /// Player units wait for input; enemy units run their AI coroutine.
        /// </summary>
        private bool IsUnitTakingTurn(Unit unit)
        {
            if (!unit.IsAlive) return false;
            if (unit is PlayerUnit) return unit.HasActionsLeft;
            return false;   // Enemy units end their own turns via coroutine
        }

        // ---------------------------------------------------------------
        // Public API — called by PlayerUnit when ending turn
        // ---------------------------------------------------------------

        /// <summary>Call this from PlayerUnit.EndTurn() to advance to the next unit.</summary>
        public void NotifyTurnEnded(Unit unit)
        {
            if (unit != ActiveUnit) return;
            // The WaitUntil in RunTurnQueue will detect HasActionsLeft = 0
            // and advance automatically. Nothing extra needed here.
            Debug.Log($"[TurnManager] {unit.UnitName} ended their turn.");
        }

        // ---------------------------------------------------------------
        // End conditions
        // ---------------------------------------------------------------
        private bool CheckEndConditions()
        {
            bool playerAlive  = _turnQueue.Any(u => u is PlayerUnit && u.IsAlive);
            bool enemiesAlive = _turnQueue.Any(u => u is EnemyUnit  && u.IsAlive);

            if (!playerAlive)
            {
                Debug.Log("[TurnManager] Player defeated. Game over.");
                CombatActive = false;
                OnPlayerDefeat?.Invoke();
                return true;
            }

            if (!enemiesAlive)
            {
                Debug.Log("[TurnManager] All enemies defeated. Victory!");
                CombatActive = false;
                OnPlayerVictory?.Invoke();
                return true;
            }

            return false;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private Unit GetNextLivingUnit()
        {
            int attempts = 0;
            while (attempts < _turnQueue.Count)
            {
                Unit unit = _turnQueue[_queueIndex % _turnQueue.Count];
                if (unit != null && unit.IsAlive) return unit;
                _queueIndex++;
                attempts++;
            }
            return null;
        }
    }
}