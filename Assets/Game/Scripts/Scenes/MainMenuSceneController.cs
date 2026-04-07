using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceRogue
{
    public class MainMenuSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text configText;
        [SerializeField] private Button startRunButton;
        [SerializeField] private Button debugBattleButton;
        [SerializeField] private Button debugRewardButton;
        [SerializeField] private Button quitButton;

        private GameRunManager runManager;

        private void Awake()
        {
            UIInputSystemHelper.EnsureEventSystem();
            runManager = GameRunManager.EnsureInstance(runConfig);

            if (startRunButton != null)
            {
                startRunButton.onClick.AddListener(runManager.StartRunFromMenu);
            }

            if (debugBattleButton != null)
            {
                debugBattleButton.onClick.AddListener(runManager.StartDebugBattle);
            }

            if (debugRewardButton != null)
            {
                debugRewardButton.onClick.AddListener(runManager.StartDebugReward);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(Application.Quit);
            }
        }

        private void OnEnable()
        {
            if (titleText != null)
            {
                titleText.text = "DiceRogue";
            }

            if (statusText != null)
            {
                statusText.text = runManager.LastRunMessage;
            }

            if (configText != null)
            {
                configText.text = $"Scenes: {runManager.Config.MainMenuSceneName} / {runManager.Config.MapSceneName} / {runManager.Config.BattleSceneName} / {runManager.Config.RewardSceneName}";
            }
        }
    }
}
