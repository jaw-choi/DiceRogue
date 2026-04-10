using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRogue
{
    public class RewardSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private bool useRuntimeLayout = true;
        [SerializeField] private UIStateController stateController;
        [SerializeField] private string rewardSelectStateId = "RewardSelect";
        [SerializeField] private string slotSelectStateId = "SlotSelect";
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text playerStatsText;
        [SerializeField] private TMP_Text diceText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Transform rewardButtonRoot;
        [SerializeField] private Transform slotButtonRoot;
        [SerializeField] private Button rewardButtonTemplate;
        [SerializeField] private Button slotButtonTemplate;
        [SerializeField] private Button skipButton;

        private GameRunManager runManager;
        private RewardOptionRuntime selectedReward;
        private TMP_Text slotPromptText;
        private string resolvedSlotSelectStateId;
        private RectTransform runtimeRewardButtonRoot;
        private RectTransform runtimeSlotButtonRoot;

        private void Awake()
        {
            UIInputSystemHelper.EnsureEventSystem();
            runManager = GameRunManager.EnsureInstance(runConfig);
            runManager.EnsureDebugRunForScene();
            runManager.EnsureRewardChoices();
            ResolveStateIds();
            ApplyLayoutIfEnabled();
            DynamicButtonLayoutHelper.EnsureVerticalButtonLayout(rewardButtonRoot, rewardButtonTemplate, 24f, 132f);
            DynamicButtonLayoutHelper.EnsureVerticalButtonLayout(slotButtonRoot, slotButtonTemplate, 24f, 132f);

            slotPromptText = FindTextByName("SlotPromptText");

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(runManager.SkipCurrentReward);
            }
        }

        private void OnEnable()
        {
            selectedReward = null;
            ApplyLayoutIfEnabled();
            stateController?.Show(rewardSelectStateId);
            RenderRewardButtons();
            RenderSharedTexts();
        }

        private void RenderSharedTexts()
        {
            if (headerText != null)
            {
                headerText.text = selectedReward == null
                    ? runManager.GetRewardContextLabel()
                    : "Choose a Die Face";
            }

            if (playerStatsText != null)
            {
                playerStatsText.text = BuildPlayerOverviewText();
            }

            if (diceText != null)
            {
                diceText.text = $"Current 6 Faces\n{runManager.PlayerState.GetDiceText()}";
            }

            if (promptText != null)
            {
                promptText.text = selectedReward == null
                    ? BuildRewardPrompt()
                    : selectedReward.RewardType == RewardType.UpgradeFace
                        ? $"{selectedReward.Title}: choose which face to upgrade."
                        : $"{selectedReward.Title}: choose which face to replace.";
            }

            if (slotPromptText != null)
            {
                slotPromptText.text = selectedReward == null
                    ? "Select a reward first."
                    : selectedReward.RewardType == RewardType.UpgradeFace
                        ? "Pick one current face to upgrade."
                        : "Pick one current face to replace.";
            }

            SceneUILayoutHelper.SetButtonLabel(skipButton, runManager.CurrentRewardSourceType == MapNodeType.Shop ? "Leave Shop" : "Skip Reward");
        }

        private void RenderRewardButtons()
        {
            PopulateButtons(
                rewardButtonRoot,
                rewardButtonTemplate,
                runManager.CurrentRewards.Count,
                index =>
                {
                    var reward = runManager.CurrentRewards[index];
                    return $"{reward.Title}\n{reward.Description}";
                },
                OnRewardSelected);

            DynamicButtonLayoutHelper.ArrangeChildrenVertically(rewardButtonRoot, rewardButtonTemplate, 24f);
        }

        private void RenderSlotButtons()
        {
            stateController?.Show(resolvedSlotSelectStateId);

            PopulateButtons(
                slotButtonRoot,
                slotButtonTemplate,
                runManager.PlayerState.DiceFaces.Count,
                index => runManager.PlayerState.DiceFaces[index].GetInspectText(index),
                OnSlotSelected);

            DynamicButtonLayoutHelper.ArrangeChildrenVertically(slotButtonRoot, slotButtonTemplate, 24f);
        }

        private string BuildRewardPrompt()
        {
            return runManager.CurrentRewardSourceType switch
            {
                MapNodeType.Shop => "Choose a forge offer. Skill offers replace one face. Upgrade improves one face.",
                MapNodeType.Reward => "Choose a treasure reward. Skill offers replace one face. Upgrade improves one face.",
                MapNodeType.EliteBattle => "Elite rewards are stronger. Pick one skill offer or upgrade one face.",
                _ => "Choose one reward. Skill offers replace one face. Upgrade improves one face."
            };
        }

        private void OnRewardSelected(int rewardIndex)
        {
            selectedReward = runManager.CurrentRewards[rewardIndex];
            RenderSharedTexts();
            RenderSlotButtons();
        }

        private void OnSlotSelected(int slotIndex)
        {
            runManager.ApplyReward(selectedReward, slotIndex);
            runManager.ReturnToMap();
        }

        private void PopulateButtons(Transform root, Button template, int count, System.Func<int, string> labelProvider, System.Action<int> clickHandler)
        {
            if (root == null || template == null)
            {
                return;
            }

            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var child = root.GetChild(index);
                if (child != template.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            template.gameObject.SetActive(false);

            for (var index = 0; index < count; index++)
            {
                var button = Instantiate(template, root, false);
                button.gameObject.SetActive(true);

                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = labelProvider(index);
                    StyleChoiceButtonLabel(label);
                }

                if (button.transform is RectTransform rectTransform)
                {
                    rectTransform.localScale = Vector3.one;
                    rectTransform.localRotation = Quaternion.identity;
                }

                var localIndex = index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => clickHandler(localIndex));
            }
        }

        private void ResolveStateIds()
        {
            resolvedSlotSelectStateId = slotSelectStateId;

            if (stateController == null || stateController.HasState(resolvedSlotSelectStateId))
            {
                return;
            }

            if (stateController.HasState("SlotSelectPanel"))
            {
                resolvedSlotSelectStateId = "SlotSelectPanel";
            }
        }

        private void ApplyLayout()
        {
            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);

            if (canvas == null)
            {
                return;
            }

            SceneUILayoutHelper.EnsureFullscreenImage(canvas.transform, "RuntimeRewardBackdrop", new Color(0.08f, 0.11f, 0.18f, 1f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeRewardTopCard", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(980f, 620f), new Color(0.11f, 0.17f, 0.28f, 0.95f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeRewardChoiceCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(940f, 720f), new Color(0.12f, 0.19f, 0.31f, 0.93f));

            var rewardPanel = FindRectByName("RewardSelectPanel");
            var slotPanel = FindRectByName("SlotSelectPanel");

            if (rewardPanel != null)
            {
                SceneUILayoutHelper.Stretch(rewardPanel);
            }

            if (slotPanel != null)
            {
                SceneUILayoutHelper.Stretch(slotPanel);
            }

            if (headerText != null && headerText.transform is RectTransform headerRect)
            {
                SceneUILayoutHelper.SetRect(headerRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(900f, 84f));
                SceneUILayoutHelper.StyleText(headerText, 56f, TextAlignmentOptions.Center, FontStyles.Bold);
                headerText.color = Color.white;
            }

            if (playerStatsText != null && playerStatsText.transform is RectTransform playerStatsRect)
            {
                SceneUILayoutHelper.SetRect(playerStatsRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -188f), new Vector2(920f, 70f));
                SceneUILayoutHelper.StyleText(playerStatsText, 24f, TextAlignmentOptions.Center, FontStyles.Bold);
                playerStatsText.color = Color.white;
            }

            if (diceText != null && diceText.transform is RectTransform diceRect)
            {
                SceneUILayoutHelper.SetRect(diceRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -410f), new Vector2(920f, 250f));
                SceneUILayoutHelper.StyleText(diceText, 17f, TextAlignmentOptions.TopLeft);
                diceText.color = new Color(0.88f, 0.93f, 1f, 1f);
            }

            if (promptText != null && promptText.transform is RectTransform promptRect)
            {
                SceneUILayoutHelper.SetRect(promptRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -612f), new Vector2(920f, 76f));
                SceneUILayoutHelper.StyleText(promptText, 22f, TextAlignmentOptions.Center, FontStyles.Bold);
                promptText.color = new Color(0.76f, 0.86f, 0.98f, 1f);
            }

            slotPromptText ??= FindTextByName("SlotPromptText");
            if (slotPromptText != null && slotPromptText.transform is RectTransform slotPromptRect)
            {
                SceneUILayoutHelper.SetRect(slotPromptRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -164f), new Vector2(920f, 86f));
                SceneUILayoutHelper.StyleText(slotPromptText, 22f, TextAlignmentOptions.Center, FontStyles.Bold);
                slotPromptText.color = new Color(0.76f, 0.86f, 0.98f, 1f);
            }

            runtimeRewardButtonRoot = SceneUILayoutHelper.EnsureVerticalListRoot(
                rewardPanel != null ? rewardPanel : canvas.transform as RectTransform,
                "RuntimeRewardButtonRoot",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 120f),
                new Vector2(820f, 520f),
                28f,
                new RectOffset(0, 0, 8, 8));
            rewardButtonRoot = runtimeRewardButtonRoot;

            runtimeSlotButtonRoot = SceneUILayoutHelper.EnsureVerticalListRoot(
                slotPanel != null ? slotPanel : canvas.transform as RectTransform,
                "RuntimeSlotButtonRoot",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 60f),
                new Vector2(820f, 620f),
                28f,
                new RectOffset(0, 0, 8, 8));
            slotButtonRoot = runtimeSlotButtonRoot;

            if (rewardButtonTemplate != null)
            {
                SceneUILayoutHelper.StyleButton(rewardButtonTemplate, new Vector2(760f, 150f), 24f, new Color(0.17f, 0.53f, 0.98f), Color.white);
                StyleChoiceButtonLabel(rewardButtonTemplate.GetComponentInChildren<TMP_Text>(true));
            }

            if (slotButtonTemplate != null)
            {
                SceneUILayoutHelper.StyleButton(slotButtonTemplate, new Vector2(760f, 150f), 24f, new Color(0.98f, 0.58f, 0.22f), Color.white);
                StyleChoiceButtonLabel(slotButtonTemplate.GetComponentInChildren<TMP_Text>(true));
            }

            if (skipButton != null && skipButton.transform is RectTransform skipRect)
            {
                skipButton.transform.SetParent(rewardPanel != null ? rewardPanel : canvas.transform, false);
                SceneUILayoutHelper.SetRect(skipRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(320f, 96f));
                SceneUILayoutHelper.StyleButton(skipButton, new Vector2(320f, 96f), 24f, new Color(0.2f, 0.25f, 0.35f), Color.white);
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

        private static RectTransform FindRectByName(string objectName)
        {
            var transforms = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < transforms.Length; index++)
            {
                if (transforms[index] != null && transforms[index].name == objectName && transforms[index].gameObject.scene.IsValid())
                {
                    return transforms[index];
                }
            }

            return null;
        }

        private static TMP_Text FindTextByName(string objectName)
        {
            var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < texts.Length; index++)
            {
                if (texts[index] != null && texts[index].name == objectName && texts[index].gameObject.scene.IsValid())
                {
                    return texts[index];
                }
            }

            return null;
        }

        private static void StyleChoiceButtonLabel(TMP_Text label)
        {
            if (label == null)
            {
                return;
            }

            if (label.transform is RectTransform labelRect)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.offsetMin = new Vector2(24f, 16f);
                labelRect.offsetMax = new Vector2(-24f, -16f);
                labelRect.localScale = Vector3.one;
                labelRect.localRotation = Quaternion.identity;
            }

            label.enableAutoSizing = true;
            label.fontSizeMax = 36f;
            label.fontSizeMin = 18f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
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
