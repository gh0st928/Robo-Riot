using UnityEngine;
using UnityEngine.InputSystem;

namespace RoboRiot
{
    /// <summary>
    /// Handles resetting the current mission by reloading the scene.
    /// Attach to any persistent GameObject in your game scene.
    /// Press R to reset, or call GameResetter.Reset() from anywhere.
    /// </summary>
    public class GameResetter : MonoBehaviour
    {
        [Header("Reset Key")]
        [SerializeField] private bool allowKeyboardReset = true;

        private void Update()
        {
            if (!allowKeyboardReset) return;
            if (Keyboard.current == null) return;
            if (Keyboard.current.rKey.wasPressedThisFrame)
                Reset();
        }

        /// <summary>Reloads the current scene — regenerates map, respawns units.</summary>
        public static void Reset()
        {
            Debug.Log("[GameResetter] Resetting mission...");
            SceneLoader.ReloadCurrentScene();
        }
    }
}