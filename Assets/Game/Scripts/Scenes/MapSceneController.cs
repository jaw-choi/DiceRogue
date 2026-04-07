using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace DiceRogue
{
    public class MapSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text playerStatsText;
        [SerializeField] private TMP_Text diceText;
        [SerializeField] private TMP_Text unlockedSkillsText;
        [SerializeField] private TMP_Text mapProgressText;
        [SerializeField] private Transform nodeButtonRoot;
        [SerializeField] private Button nodeButtonTemplate;
        [SerializeField] private Button returnMenuButton;
        [SerializeField] private Button debugRewardButton;

        private GameRunManager runManager;

        private void Awake()
        {
            UIInputSystemHelper.EnsureEventSystem();
            runManager = GameRunManager.EnsureInstance(runConfig);
            runManager.EnsureDebugRunForScene();

            if (returnMenuButton != null)
            {
                returnMenuButton.onClick.AddListener(() => SceneManager.LoadScene(runManager.Config.MainMenuSceneName));
            }

            if (debugRewardButton != null)
            {
                debugRewardButton.onClick.AddListener(runManager.StartDebugReward);
            }
        }

        private void OnEnable()
        {
            Render();
        }

        private void Render()
        {
            if (headerText != null)
            {
                headerText.text = "Map Scene";
            }

            if (playerStatsText != null)
            {
                playerStatsText.text = runManager.PlayerState.GetStatsText();
            }

            if (diceText != null)
            {
                diceText.text = runManager.PlayerState.GetDiceText();
            }

            if (unlockedSkillsText != null)
            {
                unlockedSkillsText.text = $"Unlocked Skills: {runManager.GetUnlockedSkillSummary()}";
            }

            if (mapProgressText != null)
            {
                var builder = new StringBuilder();
                foreach (var node in runManager.MapSystem.Nodes)
                {
                    var status = node.IsCompleted ? "Done" : node.IsUnlocked ? "Ready" : "Locked";
                    builder.AppendLine($"{node.Index + 1}. {node.Definition.DisplayName} / {node.Definition.EnemyTemplate.DisplayName} / {status}");
                }

                mapProgressText.text = builder.ToString();
            }

            if (nodeButtonRoot == null || nodeButtonTemplate == null)
            {
                return;
            }

            for (var index = nodeButtonRoot.childCount - 1; index >= 0; index--)
            {
                var child = nodeButtonRoot.GetChild(index);
                if (child != nodeButtonTemplate.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            nodeButtonTemplate.gameObject.SetActive(false);

            foreach (var node in runManager.MapSystem.GetAvailableNodes().ToList())
            {
                var button = Instantiate(nodeButtonTemplate, nodeButtonRoot);
                button.gameObject.SetActive(true);
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = $"{node.Definition.DisplayName}\n{node.Definition.EnemyTemplate.DisplayName}";
                }

                var nodeIndex = node.Index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => runManager.BeginBattleFromMap(nodeIndex));
            }
        }
    }
}
