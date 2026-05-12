using System.Collections.Generic;
using UnityEngine;
using RoboRiot.Units;
using RoboRiot.Grid;

namespace RoboRiot.Combat
{
    /// <summary>
    /// Records all combat events and prints them to the Console.
    /// Later you can hook this up to a UI text panel.
    ///
    /// Attach to the TurnManager GameObject.
    /// </summary>
    public class CombatLog : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Singleton
        // ---------------------------------------------------------------
        public static CombatLog Instance { get; private set; }

        // ---------------------------------------------------------------
        // Log
        // ---------------------------------------------------------------
        private readonly List<string> _entries = new();
        public IReadOnlyList<string>  Entries  => _entries;

        [Header("Settings")]
        [SerializeField] private int maxEntries = 50;   // Keep last N entries

        // ---------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------
        public System.Action<string> OnEntryAdded;   // Hook this to UI later

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (TurnManager.Instance == null) return;
            TurnManager.Instance.OnTurnStarted  += LogTurnStart;
            TurnManager.Instance.OnRoundStarted += LogRoundStart;
            TurnManager.Instance.OnPlayerVictory += LogVictory;
            TurnManager.Instance.OnPlayerDefeat  += LogDefeat;
        }

        private void OnDisable()
        {
            if (TurnManager.Instance == null) return;
            TurnManager.Instance.OnTurnStarted  -= LogTurnStart;
            TurnManager.Instance.OnRoundStarted -= LogRoundStart;
            TurnManager.Instance.OnPlayerVictory -= LogVictory;
            TurnManager.Instance.OnPlayerDefeat  -= LogDefeat;
        }

        // ---------------------------------------------------------------
        // Logging API
        // ---------------------------------------------------------------
        public void Log(string message)
        {
            _entries.Add(message);
            if (_entries.Count > maxEntries)
                _entries.RemoveAt(0);

            Debug.Log($"[CombatLog] {message}");
            OnEntryAdded?.Invoke(message);
        }

        public void LogAttack(CombatCalculator.AttackResult result)
            => Log(result.Summary);

        public void LogDeath(Unit unit)
            => Log($"{unit.UnitName} has been destroyed.");

        public void LogMove(Unit unit, GridCell cell)
            => Log($"{unit.UnitName} moves to ({cell.X},{cell.Y}).");

        // ---------------------------------------------------------------
        // Auto-log from TurnManager events
        // ---------------------------------------------------------------
        private void LogTurnStart(Unit unit)
            => Log($"--- {unit.UnitName}'s turn ---");

        private void LogRoundStart(int round)
            => Log($"====== Round {round} ======");

        private void LogVictory()
            => Log("M.C. VICTORY — all hostiles neutralised.");

        private void LogDefeat()
            => Log("UNIT LOST — M.C. tactical retreat initiated.");
    }
}