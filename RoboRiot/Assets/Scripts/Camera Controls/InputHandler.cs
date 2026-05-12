using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace RoboRiot.Controls
{
    /// <summary>
    /// Central input handler using Unity's new Input System.
    ///
    /// Setup:
    ///  1. Attach this to an "InputHandler" GameObject in your scene
    ///  2. This script reads mouse and keyboard directly via InputSystem
    ///     — no Input Actions asset needed
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Singleton
        // ---------------------------------------------------------------
        public static InputHandler Instance { get; private set; }

        // ---------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------

        /// <summary>Fired when the player left-clicks a world position.</summary>
        public UnityEvent<Vector3> OnWorldClicked    = new();

        /// <summary>Fired when right click or Escape is pressed.</summary>
        public UnityEvent          OnCancelPressed   = new();

        /// <summary>Fired when an ability key (1-6) is pressed. Passes slot index (0-5).</summary>
        public UnityEvent<int>     OnAbilitySelected = new();

        /// <summary>Fired when Space or Enter is pressed.</summary>
        public UnityEvent          OnEndTurn         = new();

        // ---------------------------------------------------------------
        // State
        // ---------------------------------------------------------------
        private bool   _inputLocked = false;
        private Camera _cam;

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _cam = Camera.main;
        }

        private void Update()
        {
            if (_inputLocked) return;

            HandleMouseClick();
            HandleRightClick();
            HandleAbilityKeys();
            HandleEndTurn();
            HandleEscape();
        }

        // ---------------------------------------------------------------
        // Mouse clicks
        // ---------------------------------------------------------------
        private void HandleMouseClick()
        {
            if (Mouse.current == null) return;
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos  = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            worldPos.z = 0f;

            OnWorldClicked.Invoke(worldPos);
        }

        private void HandleRightClick()
        {
            if (Mouse.current == null) return;
            if (Mouse.current.rightButton.wasPressedThisFrame)
                OnCancelPressed.Invoke();
        }

        // ---------------------------------------------------------------
        // Ability keys 1-6
        // ---------------------------------------------------------------
        private void HandleAbilityKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) OnAbilitySelected.Invoke(0);
            if (kb.digit2Key.wasPressedThisFrame) OnAbilitySelected.Invoke(1);
            if (kb.digit3Key.wasPressedThisFrame) OnAbilitySelected.Invoke(2);
            if (kb.digit4Key.wasPressedThisFrame) OnAbilitySelected.Invoke(3);
            if (kb.digit5Key.wasPressedThisFrame) OnAbilitySelected.Invoke(4);
            if (kb.digit6Key.wasPressedThisFrame) OnAbilitySelected.Invoke(5);
        }

        // ---------------------------------------------------------------
        // End turn
        // ---------------------------------------------------------------
        private void HandleEndTurn()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                OnEndTurn.Invoke();
        }

        // ---------------------------------------------------------------
        // Escape
        // ---------------------------------------------------------------
        private void HandleEscape()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
                OnCancelPressed.Invoke();
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------
        public void LockInput()   => _inputLocked = true;
        public void UnlockInput() => _inputLocked = false;
    }
}