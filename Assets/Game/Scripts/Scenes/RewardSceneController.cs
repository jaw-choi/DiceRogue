using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace DiceRogue
{
    public class RewardSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private bool useRuntimeLayout = true;
        [SerializeField] private bool useEditorUiLayout = false;
        [SerializeField] private UIStateController stateController;
        [SerializeField] private string rewardSelectStateId = "RewardSelect";
        [SerializeField] private string slotSelectStateId = "SlotSelect";
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text playerStatsText;
        [SerializeField] private TMP_Text diceText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Transform currentDiceSpriteRoot;
        [SerializeField] private Image currentDiceSpriteTemplate;
        [SerializeField] private Transform rewardSpriteRoot;
        [SerializeField] private Image rewardSpriteTemplate;
        [SerializeField] private Transform rewardButtonRoot;
        [SerializeField] private Transform slotButtonRoot;
        [SerializeField] private Button rewardButtonTemplate;
        [SerializeField] private Button slotButtonTemplate;
        [SerializeField] private Button skipButton;

        private GameRunManager runManager;
        private RewardOptionRuntime selectedReward;
        private int selectedRewardIndex = -1;
        private TMP_Text slotPromptText;
        private string resolvedSlotSelectStateId;
        private static Sprite treasureUpgradeIcon;
        private void Awake()
        {
            UIInputSystemHelper.EnsureEventSystem();
            runManager = GameRunManager.EnsureInstance(runConfig);
            runManager.EnsureDebugRunForScene();
            runManager.EnsureRewardChoices();
            ResolveStateIds();
            ApplyLayoutIfEnabled();

            slotPromptText = FindTextByName("SlotPromptText");

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(runManager.SkipCurrentReward);
            }
        }

        private void OnEnable()
        {
            selectedReward = null;
            selectedRewardIndex = -1;
            ApplyLayoutIfEnabled();
            stateController?.Show(rewardSelectStateId);
            SetLegacySelectionUiVisible(false);
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

            RenderCurrentDiceSprites();
            RenderRewardSprites();

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
                index => runManager.CurrentRewards[index].RewardType == RewardType.LearnSkill
                    ? SkillIconLibrary.GetSkillIcon(runManager.CurrentRewards[index].SkillDefinition)
                    : null,
                OnRewardSelected);

        }

        private void RenderSlotButtons()
        {
            stateController?.Show(resolvedSlotSelectStateId);

            PopulateButtons(
                slotButtonRoot,
                slotButtonTemplate,
                runManager.PlayerState.DiceFaces.Count,
                index => runManager.PlayerState.DiceFaces[index].GetInspectText(index),
                index => SkillIconLibrary.GetSkillIcon(runManager.PlayerState.DiceFaces[index].Skill),
                OnSlotSelected);

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
            selectedRewardIndex = rewardIndex;
            RenderSharedTexts();
        }

        private void OnSlotSelected(int slotIndex)
        {
            runManager.ApplyReward(selectedReward, slotIndex);
            runManager.ReturnToMap();
        }

        private void PopulateButtons(
            Transform root,
            Button template,
            int count,
            System.Func<int, string> labelProvider,
            System.Func<int, Sprite> iconProvider,
            System.Action<int> clickHandler)
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
                }

                var icon = iconProvider != null ? iconProvider(index) : null;
                ConfigureChoiceButtonIcon(button, label, icon);

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
            SetRuntimeRewardChromeVisible(false);
            slotPromptText ??= FindTextByName("SlotPromptText");
            SetLegacySelectionUiVisible(false);
        }

        private static void SetRuntimeRewardChromeVisible(bool isVisible)
        {
            SetObjectVisible("RuntimeRewardBackdrop", isVisible);
            SetObjectVisible("RuntimeRewardTopCard", isVisible);
            SetObjectVisible("RuntimeRewardChoiceCard", isVisible);
        }

        private static void SetObjectVisible(string objectName, bool isVisible)
        {
            var rect = FindRectByName(objectName);
            if (rect != null)
            {
                rect.gameObject.SetActive(isVisible);
            }
        }

        private void ApplyLayoutIfEnabled()
        {
            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);

            if (!useRuntimeLayout || useEditorUiLayout)
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

        private static void ConfigureChoiceButtonIcon(Button button, TMP_Text label, Sprite icon)
        {
            if (button == null)
            {
                return;
            }

            var iconImage = FindChoiceButtonIcon(button.transform);
            if (iconImage == null)
            {
                return;
            }

            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
                iconImage.enabled = true;
                iconImage.gameObject.SetActive(true);
            }
        }

        private static Image FindChoiceButtonIcon(Transform parent)
        {
            var images = parent.GetComponentsInChildren<Image>(true);
            for (var index = 0; index < images.Length; index++)
            {
                var image = images[index];
                if (image == null || image.transform == parent)
                {
                    continue;
                }

                var imageName = image.name;
                if (imageName == "RuntimeSkillIcon" || imageName == "Icon" || imageName == "SkillIcon" || imageName == "RewardIcon")
                {
                    image.raycastTarget = false;
                    return image;
                }
            }

            return null;
        }

        private void RenderCurrentDiceSprites()
        {
            currentDiceSpriteRoot ??= FindTransformByName("CurrentDiceSpriteRoot") ?? FindTransformByName("CurrentDiceSkillRoot");
            currentDiceSpriteTemplate = NormalizeSpriteTemplate(currentDiceSpriteRoot, currentDiceSpriteTemplate ?? FindImageByName("CurrentDiceSpriteTemplate"));

            PopulateSpriteSlots(
                currentDiceSpriteRoot,
                currentDiceSpriteTemplate,
                6,
                index => index < (runManager?.PlayerState?.DiceFaces.Count ?? 0)
                    ? SkillIconLibrary.GetSkillIcon(runManager.PlayerState.DiceFaces[index].Skill)
                    : null,
                index => index < (runManager?.PlayerState?.DiceFaces.Count ?? 0),
                index => selectedReward != null,
                OnSlotSelected,
                index => selectedReward != null ? Color.white : new Color(1f, 1f, 1f, 0.6f));
        }

        private void RenderRewardSprites()
        {
            rewardSpriteRoot ??= FindTransformByName("Reward SkillRoot") ?? FindTransformByName("RewardSkillRoot") ?? FindTransformByName("RewardSpriteRoot");
            rewardSpriteTemplate = NormalizeSpriteTemplate(rewardSpriteRoot, rewardSpriteTemplate ?? FindImageByName("RewardSkillTemplate") ?? FindImageByName("RewardSpriteTemplate"));

            if (TryRenderRewardCards())
            {
                return;
            }

            PopulateSpriteSlots(
                rewardSpriteRoot,
                rewardSpriteTemplate,
                3,
                index => index < (runManager?.CurrentRewards?.Count ?? 0)
                    ? ResolveRewardSprite(runManager.CurrentRewards[index])
                    : null,
                index => index < (runManager?.CurrentRewards?.Count ?? 0),
                index => true,
                OnRewardSelected,
                index => index == selectedRewardIndex ? Color.white : new Color(1f, 1f, 1f, 0.72f));
        }

        private static Sprite ResolveRewardSprite(RewardOptionRuntime reward)
        {
            if (reward?.SkillDefinition != null)
            {
                return SkillIconLibrary.GetSkillIcon(reward.SkillDefinition);
            }

            if (reward?.RewardType == RewardType.UpgradeFace && reward.Title == "Treasure Upgrade")
            {
                treasureUpgradeIcon ??= Resources.Load<Sprite>("skill_UI_2_2");
                return treasureUpgradeIcon;
            }

            return null;
        }

        private bool TryRenderRewardCards()
        {
            var rewardCards = GetRewardCardViews(rewardSpriteRoot, 3);
            if (rewardCards.Count == 0)
            {
                return false;
            }

            HideRewardOverlaySlots(rewardSpriteRoot);

            for (var index = 0; index < rewardCards.Count; index++)
            {
                var hasReward = index < (runManager?.CurrentRewards?.Count ?? 0);
                var reward = hasReward ? runManager.CurrentRewards[index] : null;
                ApplyRewardCardView(rewardCards[index], reward, index, hasReward);
            }

            return true;
        }

        private void ApplyRewardCardView(RewardCardView card, RewardOptionRuntime reward, int index, bool isVisible)
        {
            if (card.Root == null)
            {
                return;
            }

            card.Root.gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                return;
            }

            if (card.TitleText != null)
            {
                card.TitleText.text = reward?.Title ?? string.Empty;
            }

            if (card.BodyText != null)
            {
                card.BodyText.text = reward?.Description ?? string.Empty;
            }

            if (card.IconImage != null)
            {
                var icon = ResolveRewardSprite(reward);
                if (icon != null)
                {
                    card.IconImage.sprite = icon;
                    card.IconImage.color = Color.white;
                    card.IconImage.enabled = true;
                    card.IconImage.preserveAspect = true;
                }
                else
                {
                    card.IconImage.enabled = false;
                }
            }

            if (card.BackgroundImage != null)
            {
                card.BackgroundImage.color = index == selectedRewardIndex
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.94f);
            }

            ConfigureCardButton(card, index, true);
        }

        private static void PopulateSpriteSlots(
            Transform root,
            Image template,
            int count,
            System.Func<int, Sprite> spriteProvider,
            System.Func<int, bool> isVisibleProvider,
            System.Func<int, bool> isInteractableProvider,
            System.Action<int> clickHandler,
            System.Func<int, Color> colorProvider)
        {
            if (root == null)
            {
                return;
            }

            var slots = GetSpriteSlots(root, template, count);
            for (var index = 0; index < slots.Count; index++)
            {
                var slot = slots[index];
                if (slot == null)
                {
                    continue;
                }

                var shouldShow = isVisibleProvider == null || isVisibleProvider(index);
                slot.gameObject.SetActive(shouldShow);
                if (!shouldShow)
                {
                    continue;
                }

                var sprite = spriteProvider != null ? spriteProvider(index) : null;
                if (sprite != null)
                {
                    slot.sprite = sprite;
                    slot.enabled = true;
                    slot.preserveAspect = true;
                }

                slot.color = colorProvider != null ? colorProvider(index) : Color.white;
                ConfigureSpriteSlotButton(slot, index, clickHandler, isInteractableProvider == null || isInteractableProvider(index));
            }

            if (template != null && template.transform != root)
            {
                template.gameObject.SetActive(false);
            }
        }

        private static List<Image> GetSpriteSlots(Transform root, Image template, int count)
        {
            var preferredSlots = new List<Image>();
            var fallbackSlots = new List<Image>();

            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (template != null && child == template.transform)
                {
                    continue;
                }

                var image = child.GetComponent<Image>();
                if (image != null)
                {
                    if (child.name.Contains("Slot"))
                    {
                        preferredSlots.Add(image);
                    }
                    else
                    {
                        fallbackSlots.Add(image);
                    }
                }
            }

            var slots = preferredSlots.Count > 0 ? preferredSlots : fallbackSlots;

            while (slots.Count < count)
            {
                var instance = template != null
                    ? Object.Instantiate(template, root, false)
                    : CreateFallbackSpriteSlot(root, slots.Count, count);
                instance.gameObject.name = template != null
                    ? $"{template.gameObject.name}_{slots.Count}"
                    : $"SpriteSlot_{slots.Count}";
                instance.gameObject.SetActive(true);
                slots.Add(instance);
            }

            return slots;
        }

        private List<RewardCardView> GetRewardCardViews(Transform root, int count)
        {
            var views = new List<RewardCardView>();
            if (root == null)
            {
                return views;
            }

            var cardRoots = new List<Transform>();
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (child != null && child.name.StartsWith("Slot_"))
                {
                    cardRoots.Add(child);
                }
            }

            cardRoots.Sort((left, right) => ExtractTrailingNumber(left.name).CompareTo(ExtractTrailingNumber(right.name)));

            for (var index = 0; index < cardRoots.Count && index < count; index++)
            {
                var rootTransform = cardRoots[index] as RectTransform;
                if (rootTransform == null)
                {
                    continue;
                }

                views.Add(new RewardCardView
                {
                    Root = rootTransform,
                    BackgroundImage = rootTransform.GetComponent<Image>(),
                    IconImage = FindImageInChildren(rootTransform, "Icon") ?? FindImageInChildren(rootTransform, "SkillIcon") ?? FindImageInChildren(rootTransform, "RewardIcon"),
                    TitleText = FindTextInChildren(rootTransform, "Title"),
                    BodyText = FindTextInChildren(rootTransform, "Body")
                });
            }

            return views;
        }

        private void HideRewardOverlaySlots(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (child == null)
                {
                    continue;
                }

                if (child.name.StartsWith("RewardSlot_") || child.name.StartsWith("SpriteSlot_"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static int ExtractTrailingNumber(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return int.MaxValue;
            }

            for (var index = value.Length - 1; index >= 0; index--)
            {
                if (!char.IsDigit(value[index]))
                {
                    if (index == value.Length - 1)
                    {
                        return int.MaxValue;
                    }

                    return int.TryParse(value.Substring(index + 1), out var parsed) ? parsed : int.MaxValue;
                }
            }

            return int.TryParse(value, out var wholeValue) ? wholeValue : int.MaxValue;
        }

        private void ConfigureCardButton(RewardCardView card, int index, bool interactable)
        {
            if (card.Root == null)
            {
                return;
            }

            var button = card.Root.GetComponent<Button>() ?? card.Root.gameObject.AddComponent<Button>();
            button.targetGraphic = card.BackgroundImage;
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();

            if (interactable)
            {
                button.onClick.AddListener(() => OnRewardSelected(index));
            }
        }

        private static Image CreateFallbackSpriteSlot(Transform root, int index, int totalCount)
        {
            var slotObject = new GameObject($"SpriteSlot_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rectTransform = slotObject.GetComponent<RectTransform>();
            rectTransform.SetParent(root, false);

            var image = slotObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);
            image.raycastTarget = true;

            if (root is RectTransform rootRect)
            {
                ApplyFallbackSlotLayout(rootRect, rectTransform, index, totalCount);
            }

            return image;
        }

        private static void ApplyFallbackSlotLayout(RectTransform rootRect, RectTransform slotRect, int index, int totalCount)
        {
            if (rootRect == null || slotRect == null)
            {
                return;
            }

            var isRewardRow = totalCount <= 3;
            var columns = isRewardRow ? 3 : 3;
            var rows = isRewardRow ? 1 : 2;
            var column = index % columns;
            var row = index / columns;

            var width = rootRect.rect.width > 0f ? rootRect.rect.width : rootRect.sizeDelta.x;
            var height = rootRect.rect.height > 0f ? rootRect.rect.height : rootRect.sizeDelta.y;

            var cellWidth = width * (isRewardRow ? 0.22f : 0.18f);
            var cellHeight = height * (isRewardRow ? 0.32f : 0.16f);
            var xStep = width * 0.32f;
            var yStep = height * 0.18f;
            var x = (column - ((columns - 1) * 0.5f)) * xStep;
            var y = isRewardRow
                ? 0f
                : (((rows - 1) * 0.5f) - row) * yStep;

            SceneUILayoutHelper.SetRect(
                slotRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(x, y),
                new Vector2(cellWidth, cellHeight),
                true);
        }

        private static Image NormalizeSpriteTemplate(Transform root, Image template)
        {
            if (root == null)
            {
                return template;
            }

            if (template != null && template.transform != root)
            {
                return template;
            }

            var images = root.GetComponentsInChildren<Image>(true);
            for (var index = 0; index < images.Length; index++)
            {
                var image = images[index];
                if (image != null && image.transform != root && image.gameObject.name.Contains("Template"))
                {
                    return image;
                }
            }

            return null;
        }

        private void SetLegacySelectionUiVisible(bool isVisible)
        {
            if (rewardButtonRoot != null)
            {
                rewardButtonRoot.gameObject.SetActive(isVisible);
            }

            if (slotButtonRoot != null)
            {
                slotButtonRoot.gameObject.SetActive(isVisible);
            }

            if (rewardButtonTemplate != null)
            {
                rewardButtonTemplate.gameObject.SetActive(false);
            }

            if (slotButtonTemplate != null)
            {
                slotButtonTemplate.gameObject.SetActive(false);
            }
        }

        private static void ConfigureSpriteSlotButton(Image slot, int index, System.Action<int> clickHandler, bool interactable)
        {
            if (slot == null)
            {
                return;
            }

            var button = slot.GetComponent<Button>() ?? slot.gameObject.AddComponent<Button>();
            button.targetGraphic = slot;
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();

            if (interactable && clickHandler != null)
            {
                button.onClick.AddListener(() => clickHandler(index));
            }
        }

        private static Transform FindTransformByName(string objectName)
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < transforms.Length; index++)
            {
                if (transforms[index] != null && transforms[index].name == objectName && transforms[index].gameObject.scene.IsValid())
                {
                    return transforms[index];
                }
            }

            return null;
        }

        private static Image FindImageByName(string objectName)
        {
            var images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < images.Length; index++)
            {
                if (images[index] != null && images[index].name == objectName && images[index].gameObject.scene.IsValid())
                {
                    return images[index];
                }
            }

            return null;
        }

        private static Image FindImageInChildren(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            var images = parent.GetComponentsInChildren<Image>(true);
            for (var index = 0; index < images.Length; index++)
            {
                if (images[index] != null && images[index].name == objectName)
                {
                    return images[index];
                }
            }

            return null;
        }

        private static TMP_Text FindTextInChildren(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            var texts = parent.GetComponentsInChildren<TMP_Text>(true);
            for (var index = 0; index < texts.Length; index++)
            {
                if (texts[index] != null && texts[index].name == objectName)
                {
                    return texts[index];
                }
            }

            return null;
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

        private sealed class RewardCardView
        {
            public RectTransform Root;
            public Image BackgroundImage;
            public Image IconImage;
            public TMP_Text TitleText;
            public TMP_Text BodyText;
        }
    }
}
