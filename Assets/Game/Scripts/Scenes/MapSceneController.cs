using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiceRogue
{
    public class MapSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private bool useRuntimeLayout = true;
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
        private RectTransform runtimeNodeButtonRoot;

        private void Awake()
        {
            UIInputSystemHelper.EnsureEventSystem();
            runManager = GameRunManager.EnsureInstance(runConfig);
            runManager.EnsureDebugRunForScene();
            ApplyLayoutIfEnabled();
            DynamicButtonLayoutHelper.EnsureVerticalButtonLayout(nodeButtonRoot, nodeButtonTemplate, 24f, 120f);

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
            ApplyLayoutIfEnabled();
            Render();
        }

        private void Render()
        {
            if (headerText != null)
            {
                headerText.text = "Dungeon Route";
            }

            if (playerStatsText != null)
            {
                playerStatsText.text = BuildPlayerOverviewText();
            }

            if (diceText != null)
            {
                diceText.text = $"Current 6 Faces\n{runManager.PlayerState.GetDiceText()}";
            }

            if (unlockedSkillsText != null)
            {
                unlockedSkillsText.text = $"Unlocked Skills\n{runManager.GetUnlockedSkillSummary()}";
            }

            if (mapProgressText != null)
            {
                var builder = new StringBuilder();
                foreach (var node in runManager.MapSystem.Nodes)
                {
                    var status = node.IsCompleted ? "Complete" : node.IsUnlocked ? "Available" : "Locked";
                    builder.AppendLine($"{node.Index + 1}. [{node.Definition.GetNodeTypeLabel()}] {node.Definition.DisplayName} / {node.Definition.GetEncounterSummary()} / {status}");
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
                var button = Instantiate(nodeButtonTemplate, nodeButtonRoot, false);
                button.gameObject.SetActive(true);
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = $"[{node.Definition.GetNodeTypeLabel()}] {node.Definition.DisplayName}\n{node.Definition.GetEncounterSummary()}";
                }

                if (button.transform is RectTransform rectTransform)
                {
                    rectTransform.localScale = Vector3.one;
                    rectTransform.localRotation = Quaternion.identity;
                }

                var nodeIndex = node.Index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => runManager.SelectMapNode(nodeIndex));
            }

            DynamicButtonLayoutHelper.ArrangeChildrenVertically(nodeButtonRoot, nodeButtonTemplate, 24f);
            SceneUILayoutHelper.SetButtonLabel(returnMenuButton, "Main Menu");
            SceneUILayoutHelper.SetButtonLabel(debugRewardButton, "Debug Reward");
        }

        private void ApplyLayout()
        {
            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);

            if (canvas == null)
            {
                return;
            }

            SceneUILayoutHelper.EnsureFullscreenImage(canvas.transform, "RuntimeMapBackdrop", new Color(0.07f, 0.11f, 0.18f, 1f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeMapTopCard", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(980f, 520f), new Color(0.1f, 0.16f, 0.28f, 0.95f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeMapNodeCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(940f, 520f), new Color(0.12f, 0.19f, 0.31f, 0.92f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeMapDiceCard", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 196f), new Vector2(940f, 300f), new Color(0.1f, 0.16f, 0.27f, 0.94f));

            if (headerText != null && headerText.transform is RectTransform headerRect)
            {
                SceneUILayoutHelper.SetRect(headerRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(760f, 84f));
                SceneUILayoutHelper.StyleText(headerText, 58f, TextAlignmentOptions.Center, FontStyles.Bold);
                headerText.color = Color.white;
            }

            if (playerStatsText != null && playerStatsText.transform is RectTransform playerStatsRect)
            {
                SceneUILayoutHelper.SetRect(playerStatsRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(76f, -190f), new Vector2(430f, 120f));
                SceneUILayoutHelper.StyleText(playerStatsText, 24f, TextAlignmentOptions.TopLeft, FontStyles.Bold);
                playerStatsText.color = Color.white;
            }

            if (unlockedSkillsText != null && unlockedSkillsText.transform is RectTransform unlockedSkillsRect)
            {
                SceneUILayoutHelper.SetRect(unlockedSkillsRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(76f, -324f), new Vector2(430f, 148f));
                SceneUILayoutHelper.StyleText(unlockedSkillsText, 22f, TextAlignmentOptions.TopLeft);
                unlockedSkillsText.color = new Color(0.85f, 0.92f, 1f, 1f);
            }

            if (mapProgressText != null && mapProgressText.transform is RectTransform mapProgressRect)
            {
                SceneUILayoutHelper.SetRect(mapProgressRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-76f, -190f), new Vector2(430f, 282f));
                SceneUILayoutHelper.StyleText(mapProgressText, 20f, TextAlignmentOptions.TopLeft);
                mapProgressText.color = new Color(0.86f, 0.9f, 0.96f, 1f);
            }

            if (diceText != null && diceText.transform is RectTransform diceRect)
            {
                SceneUILayoutHelper.SetRect(diceRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 208f), new Vector2(880f, 246f));
                SceneUILayoutHelper.StyleText(diceText, 18f, TextAlignmentOptions.TopLeft);
                diceText.color = Color.white;
            }

            runtimeNodeButtonRoot = SceneUILayoutHelper.EnsureVerticalListRoot(
                canvas.transform,
                "RuntimeMapNodeButtonRoot",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 46f),
                new Vector2(760f, 440f),
                28f,
                new RectOffset(0, 0, 12, 12));
            nodeButtonRoot = runtimeNodeButtonRoot;

            if (nodeButtonTemplate != null)
            {
                SceneUILayoutHelper.StyleButton(nodeButtonTemplate, new Vector2(700f, 120f), 30f, new Color(0.16f, 0.54f, 0.98f), Color.white);
            }

            if (returnMenuButton != null && returnMenuButton.transform is RectTransform returnRect)
            {
                returnMenuButton.transform.SetParent(canvas.transform, false);
                SceneUILayoutHelper.SetRect(returnRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(56f, 70f), new Vector2(290f, 96f));
                SceneUILayoutHelper.StyleButton(returnMenuButton, new Vector2(290f, 96f), 24f, new Color(0.19f, 0.24f, 0.34f), Color.white);
            }

            if (debugRewardButton != null && debugRewardButton.transform is RectTransform debugRewardRect)
            {
                debugRewardButton.transform.SetParent(canvas.transform, false);
                SceneUILayoutHelper.SetRect(debugRewardRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-56f, 70f), new Vector2(290f, 96f));
                SceneUILayoutHelper.StyleButton(debugRewardButton, new Vector2(290f, 96f), 24f, new Color(0.2f, 0.74f, 0.44f), Color.white);
            }
        }

        private void ApplyLayoutIfEnabled()
        {
            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);

            if (!useRuntimeLayout)
            {
                return;
            }

            ApplyLayout();
        }

        private string BuildPlayerOverviewText()
        {
            var player = runManager != null ? runManager.PlayerState : null;
            if (player == null)
            {
                return "Player";
            }

            return player.IsBerserkActive
                ? $"Player HP {player.CurrentHp}/{player.MaxHp}  Shield {player.Shield}  Armor {player.Armor}  Rage {player.Rage}  Berserk {player.BerserkTurnsRemaining}t"
                : $"Player HP {player.CurrentHp}/{player.MaxHp}  Shield {player.Shield}  Armor {player.Armor}  Rage {player.Rage}";
        }
    }
}
