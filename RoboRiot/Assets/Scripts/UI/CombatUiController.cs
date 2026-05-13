using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoboRiot.Units;
using RoboRiot.Combat;
using RoboRiot.Controls;

namespace RoboRiot.UI
{
    /// <summary>
    /// Drives the combat UI using Unity's uGUI Canvas system.
    /// Ability bar and turn order bar are built dynamically at runtime.
    ///
    /// Setup:
    ///  1. Build the Canvas hierarchy as described in the setup guide
    ///  2. Attach this script to the Canvas GameObject
    ///  3. Drag all references into the Inspector slots
    /// </summary>
    public class CombatUIController : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector
        // ---------------------------------------------------------------

        [Header("Turn Order Bar")]
        [SerializeField] private TextMeshProUGUI roundLabel;
        [SerializeField] private Transform       turnOrderList;
        [SerializeField] private GameObject      turnCardPrefab;

        [Header("Player Panel")]
        [SerializeField] private TextMeshProUGUI unitNameLabel;
        //[SerializeField] private Image           healthBarFill;
        [SerializeField] private TextMeshProUGUI healthText;
        //[SerializeField] private Transform       apPipsContainer;
        //[SerializeField] private GameObject      apPipPrefab;
        [SerializeField] private TextMeshProUGUI apText;

        [Header("Ability Bar")]
        [SerializeField] private Transform  abilitySlotsContainer;
        [SerializeField] private GameObject abilitySlotPrefab;

        [Header("Ability Colours")]
        [SerializeField] private Color colorNormal   = new Color(0.06f, 0.06f, 0.10f, 1f);
        [SerializeField] private Color colorSelected = new Color(0.10f, 0.04f, 0.00f, 1f);
        [SerializeField] private Color colorNoAP     = new Color(0.06f, 0.06f, 0.10f, 0.5f);

        [Header("AP Pip Colours")]
        [SerializeField] private Color pipFull  = new Color(1f,    0.24f, 0f,    1f);
        [SerializeField] private Color pipEmpty = new Color(0.16f, 0.16f, 0.23f, 1f);

        // ---------------------------------------------------------------
        // Internal state
        // ---------------------------------------------------------------
        private PlayerUnit       _player;
        private int              _selectedSlot = -1;
        private List<GameObject> _spawnedSlots = new();

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        private void OnEnable()  => SubscribeToEvents();
        private void OnDisable() => UnsubscribeFromEvents();

        private void Start()
        {
            StartCoroutine(InitNextFrame());
        }

        private IEnumerator InitNextFrame()
        {
            yield return null;

            _player = FindFirstObjectByType<PlayerUnit>();

            if (_player != null)
            {
                BuildAbilityBar();
                RefreshPlayerPanel();
                _player.OnHealthChanged += OnHealthChanged;
                _player.OnAPChanged     += OnAPChanged;
            }

            BuildTurnOrder();
        }

        // ---------------------------------------------------------------
        // Event subscriptions
        // ---------------------------------------------------------------
        private void SubscribeToEvents()
        {
            if (TurnManager.Instance == null) return;
            TurnManager.Instance.OnTurnStarted  += OnTurnStarted;
            TurnManager.Instance.OnTurnEnded    += OnTurnEnded;
            TurnManager.Instance.OnRoundStarted += OnRoundStarted;
        }

        private void UnsubscribeFromEvents()
        {
            if (TurnManager.Instance == null) return;
            TurnManager.Instance.OnTurnStarted  -= OnTurnStarted;
            TurnManager.Instance.OnTurnEnded    -= OnTurnEnded;
            TurnManager.Instance.OnRoundStarted -= OnRoundStarted;
        }

        // ---------------------------------------------------------------
        // Player panel
        // ---------------------------------------------------------------
        private void RefreshPlayerPanel()
        {
            if (_player == null) return;

            if (unitNameLabel != null)
                unitNameLabel.text = _player.UnitName.ToUpper();

            UpdateHealthBar(_player.CurrentHealth, _player.MaxHealth);
            UpdateAPText(_player.CurrentActionPoints, _player.MaxActionPoints);

            //BuildAPPips(_player.MaxActionPoints);
            //UpdateAPPips(_player.CurrentActionPoints);
        }

        private void UpdateHealthBar(int current, int max)
        {
            /*
            if (healthBarFill != null)
                healthBarFill.fillAmount = max > 0 ? (float)current / max : 0f;
            */
            if (healthText != null)
                healthText.text = $"{current}/{max}";
        }
/*
        private void BuildAPPips(int maxAP)
        {
            if (apPipsContainer == null) return;

            foreach (Transform child in apPipsContainer)
                Destroy(child.gameObject);

            if (apPipPrefab == null) return;

            for (int i = 0; i < maxAP; i++)
                Instantiate(apPipPrefab, apPipsContainer);
        }

        private void UpdateAPPips(int currentAP)
        {
            if (apPipsContainer == null) return;

            int i = 0;
            foreach (Transform pip in apPipsContainer)
            {
                var img = pip.GetComponent<Image>();
                if (img != null)
                    img.color = i < currentAP ? pipFull : pipEmpty;
                i++;
            }
        }
*/
        private void UpdateAPText(int current, int max)
        {
            if (apText != null)
                apText.text = $"{current}/{max}";
        }
        private void OnHealthChanged(int current, int max)
            => UpdateHealthBar(current, max);

        private void OnAPChanged(int current, int max)
        {
            //UpdateAPPips(current);
            UpdateAPText(current, max);
            RefreshAbilityAPState(current);
        }

        // ---------------------------------------------------------------
        // Ability bar — built dynamically from unit's ability list
        // ---------------------------------------------------------------
        private void BuildAbilityBar()
        {
            if (_player == null || abilitySlotsContainer == null || abilitySlotPrefab == null)
            {
                Debug.LogWarning("[CombatUI] Ability bar references missing.");
                return;
            }

            // Clear old slots
            foreach (var slot in _spawnedSlots) Destroy(slot);
            _spawnedSlots.Clear();

            if (_player.Abilities == null || _player.Abilities.Length == 0)
            {
                Debug.Log("[CombatUI] Player has no abilities assigned.");
                return;
            }

            for (int i = 0; i < _player.Abilities.Length; i++)
            {
                var ability = _player.Abilities[i];
                if (ability == null) continue;

                // Spawn slot from prefab
                GameObject slot = Instantiate(abilitySlotPrefab, abilitySlotsContainer);
                _spawnedSlots.Add(slot);

                // Fill in labels by name
                var labels = slot.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var lbl in labels)
                {
                    if (lbl.name == "KeyLabel")    lbl.text = (i + 1).ToString();
                    if (lbl.name == "AbilityName") lbl.text = ability.abilityName.ToUpper();
                    if (lbl.name == "APCost")      lbl.text = $"{ability.apCost} AP";
                }

                // Wire up button click
                int slotIndex = i;
                var btn = slot.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnAbilitySlotClicked(slotIndex));
            }

            Debug.Log($"[CombatUI] Built {_spawnedSlots.Count} ability slots.");
        }

        private void OnAbilitySlotClicked(int slotIndex)
        {
            _selectedSlot = slotIndex;
            RefreshAbilitySelection();
            InputHandler.Instance?.SimulateAbilitySelect(slotIndex);
        }

        public void SetSelectedSlot(int slotIndex)
        {
            _selectedSlot = slotIndex;
            RefreshAbilitySelection();
        }

        public void ClearSelectedSlot()
        {
            _selectedSlot = -1;
            RefreshAbilitySelection();
        }

        private void RefreshAbilitySelection()
        {
            Debug.Log($"[CombatUI] Refreshing selection. Selected slot: {_selectedSlot}, Total slots: {_spawnedSlots.Count}");
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                var img = _spawnedSlots[i].GetComponent<Image>();
                Debug.Log($"[CombatUI] Slot {i} image: {img != null}");
                if (img != null)
                    img.color = i == _selectedSlot ? colorSelected : colorNormal;
            }
        }

        private void RefreshAbilityAPState(int currentAP)
        {
            if (_player?.Abilities == null) return;

            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                if (i >= _player.Abilities.Length || _player.Abilities[i] == null) continue;

                bool canAfford = currentAP >= _player.Abilities[i].apCost;

                var btn = _spawnedSlots[i].GetComponent<Button>();
                var img = _spawnedSlots[i].GetComponent<Image>();

                if (btn != null) btn.interactable = canAfford;
                if (img != null) img.color = canAfford ? colorNormal : colorNoAP;
            }
        }

        // ---------------------------------------------------------------
        // Turn order bar — built dynamically from all living units
        // ---------------------------------------------------------------
        private void BuildTurnOrder()
        {
            if (turnOrderList == null) return;

            // Clear old cards
            foreach (Transform child in turnOrderList)
                Destroy(child.gameObject);

            // Get all living units sorted by initiative
            var units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            System.Array.Sort(units, (a, b) => b.Initiative.CompareTo(a.Initiative));

            foreach (var unit in units)
            {
                if (!unit.IsAlive) continue;
                SpawnTurnCard(unit);
            }
        }

        private void SpawnTurnCard(Unit unit)
        {
            if (turnCardPrefab == null || turnOrderList == null) return;

            GameObject card = Instantiate(turnCardPrefab, turnOrderList);

            // Fill labels by name
            var labels = card.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var lbl in labels)
            {
                if (lbl.name == "InitLabel") lbl.text = unit.Initiative.ToString();
                if (lbl.name == "NameLabel") lbl.text = unit.UnitName.ToUpper();
            }

            // Highlight the active unit
            bool isActive = TurnManager.Instance?.ActiveUnit == unit;
            var img = card.GetComponent<Image>();
            if (img != null)
                img.color = isActive
                    ? new Color(0.10f, 0.04f, 0.00f, 0.95f)
                    : new Color(0.06f, 0.06f, 0.10f, 0.95f);
        }

        // ---------------------------------------------------------------
        // TurnManager events
        // ---------------------------------------------------------------
        private void OnTurnStarted(Unit unit)
        {
            BuildTurnOrder();
            if (unit is PlayerUnit) RefreshPlayerPanel();
        }

        private void OnTurnEnded(Unit unit)
        {
            BuildTurnOrder();
            ClearSelectedSlot();
        }

        private void OnRoundStarted(int round)
        {
            if (roundLabel != null)
                roundLabel.text = $"ROUND {round}";
            BuildTurnOrder();
        }
    }
}