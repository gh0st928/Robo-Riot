using UnityEngine;
using UnityEngine.UI;

namespace RoboRiot.UI
{
    /// <summary>
    /// Simple main menu controller.
    /// Attach to the Canvas in MainMenu scene.
    /// Assign the Play button in the Inspector.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button playButton;

        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "GameScene";

        private void Start()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            SceneLoader.LoadScene(gameSceneName);
        }
    }
}