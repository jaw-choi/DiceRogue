using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceRogue
{
    public class RewardSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
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

        private void Awake()
        {
            UIInputSystemHelper.EnsureEventSystem();
            runManager = GameRunManager.EnsureInstance(runConfig);
            runManager.EnsureDebugRunForScene();

            if (runManager.CurrentRewards == null || runManager.CurrentRewards.Count == 0)
            {
                runManager.StartDebugReward();
                return;
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(runManager.ReturnToMap);
            }
        }

        private void OnEnable()
        {
            selectedReward = null;
            stateController?.Show(rewardSelectStateId);
            RenderRewardButtons();
            RenderSharedTexts();
        }

        private void RenderSharedTexts()
        {
            if (headerText != null)
            {
                headerText.text = "Reward Scene";
            }

            if (playerStatsText != null)
            {
                playerStatsText.text = runManager.PlayerState.GetStatsText();
            }

            if (diceText != null)
            {
                diceText.text = runManager.PlayerState.GetDiceText();
            }

            if (promptText != null)
            {
                promptText.text = selectedReward == null
                    ? "Choose one reward. New skills can only be equipped after you learn them here."
                    : $"Choose a slot for {selectedReward.Title}.";
            }
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
        }

        private void RenderSlotButtons()
        {
            stateController?.Show(slotSelectStateId);

            PopulateButtons(
                slotButtonRoot,
                slotButtonTemplate,
                runManager.PlayerState.DiceFaces.Count,
                index => $"Slot {index + 1}\n{runManager.PlayerState.DiceFaces[index].GetSummary()}",
                OnSlotSelected);
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
                var button = Instantiate(template, root);
                button.gameObject.SetActive(true);

                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = labelProvider(index);
                }

                var localIndex = index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => clickHandler(localIndex));
            }
        }
    }
}
