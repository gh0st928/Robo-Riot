using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace RoboRiot
{
    /// <summary>
    /// Handles scene loading with a simple text loading screen.
    /// Lives in LoadingScene — DontDestroyOnLoad keeps it alive.
    ///
    /// Setup:
    ///  1. Create a LoadingScene with a Canvas
    ///  2. Add a TextMeshPro text object named "StatusText"
    ///  3. Attach this script to a GameObject in LoadingScene
    ///  4. Assign StatusText in the Inspector
    ///  5. Add all scenes to Build Settings
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Singleton
        // ---------------------------------------------------------------
        public static SceneLoader Instance { get; private set; }

        // ---------------------------------------------------------------
        // Inspector
        // ---------------------------------------------------------------
        [Header("Loading Screen UI")]
        [SerializeField] private TextMeshProUGUI statusText;

        // ---------------------------------------------------------------
        // Static target — survives scene loads
        // ---------------------------------------------------------------
        private static string _targetScene;

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(_targetScene))
                StartCoroutine(LoadSceneAsync(_targetScene));
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>Load a scene by name, showing the loading screen.</summary>
        public static void LoadScene(string sceneName)
        {
            _targetScene = sceneName;
            SceneManager.LoadScene("LoadingScene");
        }

        /// <summary>Reload the current scene — resets everything including the map.</summary>
        public static void ReloadCurrentScene()
        {
            _targetScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene("LoadingScene");
        }

        // ---------------------------------------------------------------
        // Async loading
        // ---------------------------------------------------------------
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            SetStatus($"LOADING...");
            yield return null;

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (!op.isDone)
            {
                if (op.progress >= 0.9f)
                {
                    SetStatus("READY");
                    yield return new WaitForSeconds(0.3f);
                    op.allowSceneActivation = true;
                }
                yield return null;
            }

            _targetScene = null;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
            Debug.Log($"[SceneLoader] {message}");
        }
    }
}