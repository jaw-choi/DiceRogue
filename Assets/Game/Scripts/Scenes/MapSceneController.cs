using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiceRogue
{
    [ExecuteAlways]
    public class MapSceneController : MonoBehaviour
    {
        private static readonly Vector2Int StartGridPosition = new Vector2Int(0, 0);
        private static readonly float[] ColumnAnchors = { 0.14f, 0.38f, 0.62f, 0.86f };
        private static readonly float[] RowAnchors = { 0.16f, 0.39f, 0.62f, 0.85f };
        private static readonly string[] EditorPreviewSkillIds = { "basic_attack", "defensive_stance", "focused_defense", "counter", "shield_burst", "blood_slash" };
        private const string MapSpriteResourcePath = "map";
        private const string MapIconResourcePath = "map_icon";
        private const string PlayerIconResourcePath = "player_icon";
        private const string CloudSpriteName = "map_icon_0";
        private const string BattleSpriteName = "map_icon_1";
        private const string BossSpriteName = "map_icon_2";
        private const string RewardSpriteName = "map_icon_3";
        private const string ShopSpriteName = "map_icon_4";

        private static Sprite runtimeUiSprite;
        private static readonly Dictionary<int, Sprite> CenteredSpriteCache = new Dictionary<int, Sprite>();

        [SerializeField] private RunConfig runConfig;
        [SerializeField] private bool useRuntimeLayout = true;
        [SerializeField] private bool useEditorUiLayout = false;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text playerStatsText;
        [SerializeField] private TMP_Text diceText;
        [SerializeField] private TMP_Text unlockedSkillsText;
        [SerializeField] private TMP_Text mapProgressText;
        [SerializeField] private Transform nodeButtonRoot;
        [SerializeField] private Button nodeButtonTemplate;
        [SerializeField] private Button returnMenuButton;
        [SerializeField] private Button debugRewardButton;
        [Header("Editor Layout Override")]
        [SerializeField] private RectTransform boardPlacementReference;
        [SerializeField] private RectTransform nodeAnchorRoot;
        [SerializeField] private float nodeVisualScale = 1f;
        [SerializeField] private float playerMoveDuration = 0.28f;
        [SerializeField] private float playerMoveArcHeight = 26f;
        [Header("Battle Hit Effect")]
        [SerializeField] private BattleHitEffectSettings battleHitEffectSettings = new BattleHitEffectSettings();
        [Header("Battle Overlay Layout")]
        [SerializeField] private BattleOverlayLayoutSettings battleOverlayLayout = new BattleOverlayLayoutSettings();
        [Header("Map Top HUD")]
        [SerializeField] private RuntimeFrameLayoutSettings mapTopHudLayout = new RuntimeFrameLayoutSettings
        {
            AnchoredPosition = new Vector2(0f, -86f),
            UseNativeSize = true,
            SpriteScale = new Vector2(0.34f, 0.34f),
            ExtraSize = Vector2.zero,
            FixedSize = new Vector2(760f, 180f)
        };
        [Header("Dice Inspect UI")]
        [SerializeField] private RuntimeFrameLayoutSettings diceInspectPanelLayout = new RuntimeFrameLayoutSettings
        {
            AnchoredPosition = new Vector2(0f, -428f),
            UseNativeSize = true,
            SpriteScale = new Vector2(0.34f, 0.34f),
            ExtraSize = Vector2.zero,
            FixedSize = new Vector2(680f, 620f)
        };
        [SerializeField] private RuntimeFrameLayoutSettings diceInspectDetailLayout = new RuntimeFrameLayoutSettings
        {
            AnchoredPosition = new Vector2(0f, -824f),
            UseNativeSize = true,
            SpriteScale = new Vector2(0.28f, 0.28f),
            ExtraSize = Vector2.zero,
            FixedSize = new Vector2(540f, 190f)
        };
        [Header("Runtime UI Tuner")]
        [SerializeField] private RuntimeUiTunerSettings runtimeUiTunerSettings = new RuntimeUiTunerSettings
        {
            ShowRuntimeTuner = false
        };
        [Header("Editor Preview")]
        [SerializeField] private bool showEditorDiceInspect = true;
        [SerializeField] private bool showEditorBattleOverlay = false;

        private readonly Dictionary<int, MapNodeView> nodeViews = new Dictionary<int, MapNodeView>();
        private readonly Dictionary<string, RectTransform> editorAnchorMap = new Dictionary<string, RectTransform>();

        private GameRunManager runManager;
        private RectTransform runtimeMapBoardRoot;
        private RectTransform runtimeMapOverlayRoot;
        private MapTopHudView mapTopHudView;
        private DiceInspectView diceInspectView;
        private RuntimeUiTunerView runtimeUiTunerView;
        private MapNodeView startNodeView;
        private PlayerMarkerView playerMarkerView;
        private BattleOverlayView battleOverlayView;
        private Sprite mapBoardSprite;
        private Sprite playerMarkerSprite;
        private Dictionary<string, Sprite> iconSpritesByName;
        private BattleHUD battleHud;
        private BattlePresenter battlePresenter;
        private FloatingTextSpawner battleFloatingTextSpawner;
        private Coroutine battleAutoBattleRoutine;
        private bool isNodeTransitionPlaying;
        private bool isBattleOverlayOpen;
        private bool isBattleResolvingPresentation;
        private bool isDiceInspectOpen;
        private bool pendingRuntimeBattleAuthoringRefresh;
        private int lastBattleOverlayLayoutSignature;
        private int lastBattleHitEffectSignature;
        private int lastRuntimeUiLayoutSignature;
        private RuntimeUiTunerTarget runtimeUiTunerTarget = RuntimeUiTunerTarget.MapTopHud;
        private int selectedDiceFaceIndex;

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            UIInputSystemHelper.EnsureEventSystem();
            runManager = GameRunManager.EnsureInstance(runConfig);
            runManager.EnsureDebugRunForScene();
            ApplyLayoutIfEnabled();

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
            if (!Application.isPlaying)
            {
                RenderEditorPreview();
                CacheRuntimeBattleAuthoringSignatures();
                return;
            }

            ApplyLayoutIfEnabled();
            Render();
            CacheRuntimeBattleAuthoringSignatures();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            StopAllCoroutines();
            battleAutoBattleRoutine = null;
            isBattleResolvingPresentation = false;
            pendingRuntimeBattleAuthoringRefresh = false;
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (!pendingRuntimeBattleAuthoringRefresh)
                {
                    return;
                }

                pendingRuntimeBattleAuthoringRefresh = false;
                RenderEditorPreview();
                CacheRuntimeBattleAuthoringSignatures();
                return;
            }

            if (!pendingRuntimeBattleAuthoringRefresh)
            {
                return;
            }

            pendingRuntimeBattleAuthoringRefresh = false;
            ApplyRuntimeBattleAuthoringRefresh();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                pendingRuntimeBattleAuthoringRefresh = true;
                return;
            }

            pendingRuntimeBattleAuthoringRefresh = true;
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
                mapProgressText.text = BuildMapStatusText();
            }

            if (useRuntimeLayout)
            {
                EnsureRuntimeMapBoard();
                EnsureRuntimeMapTopHud();
                EnsureDiceInspectPanel();
                EnsureRuntimeUiTuner();
                RenderMapBoard();
                RefreshRuntimeMapTopHud();
                RefreshDiceInspectPanel();
                HideLegacyNodeButtons();
                SetLegacyMapInfoVisibility(false);
            }
            else
            {
                RenderLegacyNodeButtons();
                SetLegacyMapInfoVisibility(true);
            }

            SceneUILayoutHelper.SetButtonLabel(returnMenuButton, "Main Menu");
            SceneUILayoutHelper.SetButtonLabel(debugRewardButton, "Debug Reward");

            if (isBattleOverlayOpen)
            {
                RefreshBattleOverlayPresentation();
            }
        }

        private void RenderEditorPreview()
        {
            ApplyLayoutIfEnabled();
            EnsureRuntimeMapBoard();
            EnsureRuntimeMapTopHud();
            EnsureDiceInspectPanel();
            EnsureRuntimeUiTuner();
            EnsureBattleOverlay();
            SetLegacyMapInfoVisibility(!useRuntimeLayout || useEditorUiLayout);
            RefreshRuntimeMapTopHud();
            RefreshDiceInspectPanel();
            RefreshRuntimeUiTuner();
        }

        private void RenderLegacyNodeButtons()
        {
            if (nodeButtonRoot == null || nodeButtonTemplate == null)
            {
                return;
            }

            nodeButtonRoot.gameObject.SetActive(true);
            nodeButtonTemplate.gameObject.SetActive(false);

            for (var index = nodeButtonRoot.childCount - 1; index >= 0; index--)
            {
                var child = nodeButtonRoot.GetChild(index);
                if (child != nodeButtonTemplate.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            foreach (var node in runManager.MapSystem.GetAvailableNodes().ToList())
            {
                var button = Instantiate(nodeButtonTemplate, nodeButtonRoot, false);
                button.gameObject.SetActive(true);
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = $"[{node.Definition.GetNodeTypeLabel()}] {node.Definition.DisplayName}\n{node.Definition.GetEncounterSummary()}";
                }

                var nodeIndex = node.Index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => HandleNodeSelection(nodeIndex));
            }

            DynamicButtonLayoutHelper.EnsureVerticalButtonLayout(nodeButtonRoot, nodeButtonTemplate, 24f, 120f);
            DynamicButtonLayoutHelper.ArrangeChildrenVertically(nodeButtonRoot, nodeButtonTemplate, 24f);
        }

        private void HideLegacyNodeButtons()
        {
            if (nodeButtonTemplate != null)
            {
                nodeButtonTemplate.gameObject.SetActive(false);
            }

            if (nodeButtonRoot != null)
            {
                nodeButtonRoot.gameObject.SetActive(false);
            }
        }

        private void EnsureRuntimeMapBoard()
        {
            if (!useRuntimeLayout)
            {
                return;
            }

            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);
            if (canvas == null)
            {
                return;
            }

            LoadMapSpritesIfNeeded();
            CacheEditorAnchors();
            runtimeMapBoardRoot = SceneUILayoutHelper.EnsureRuntimeRect(canvas.transform, "RuntimeMapBoardRoot");
            ApplyBoardPlacement(runtimeMapBoardRoot);

            var boardImage = runtimeMapBoardRoot.GetComponent<Image>() ?? runtimeMapBoardRoot.gameObject.AddComponent<Image>();
            boardImage.sprite = GetCenteredSprite(mapBoardSprite);
            boardImage.color = Color.white;
            boardImage.preserveAspect = true;
            boardImage.raycastTarget = false;

            runtimeMapOverlayRoot = SceneUILayoutHelper.EnsureRuntimeRect(runtimeMapBoardRoot, "RuntimeMapOverlayRoot");
            SceneUILayoutHelper.Stretch(runtimeMapOverlayRoot);
            DisableDuplicateMapImages(canvas, boardImage);
            EnsureRuntimeMapTopHud();
            EnsureDiceInspectPanel();
            EnsureRuntimeUiTuner();
        }

        private void SetLegacyMapInfoVisibility(bool isVisible)
        {
            SetLegacyElementVisible(headerText, isVisible);
            SetLegacyElementVisible(playerStatsText, isVisible);
            SetLegacyElementVisible(diceText, isVisible);
            SetLegacyElementVisible(unlockedSkillsText, isVisible);
            SetLegacyElementVisible(mapProgressText, isVisible);
        }

        private void EnsureRuntimeMapTopHud()
        {
            if (!useRuntimeLayout)
            {
                return;
            }

            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);
            if (canvas == null)
            {
                return;
            }

            LoadMapSpritesIfNeeded();

            var existingRoot = FindRuntimeRect(canvas.transform, "RuntimeMapTopHudRoot");
            if (existingRoot != null && (!Application.isPlaying || mapTopHudView == null))
            {
                SyncRuntimeFrameLayoutFromRect(mapTopHudLayout, existingRoot);
            }

            var root = existingRoot ?? SceneUILayoutHelper.EnsureRuntimeRect(canvas.transform, "RuntimeMapTopHudRoot");
            var topSprite = UiFrameSpriteLibrary.GetTopHudSprite();
            var canvasRect = canvas.transform as RectTransform;
            var defaultRootSize = FitSizeToCanvas(ResolveRuntimeFrameSize(topSprite, mapTopHudLayout), canvasRect, new Vector2(0.94f, 0.145f), new Vector2(18f, 18f));
            SceneUILayoutHelper.SetRect(
                root,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                mapTopHudLayout.AnchoredPosition,
                defaultRootSize,
                true);

            if (existingRoot == null)
            {
                ClampRectToCanvas(root, canvasRect, new Vector2(18f, 18f));
            }

            var rootSize = SceneUILayoutHelper.ResolveRectSize(root, defaultRootSize);

            var background = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            background.sprite = topSprite;
            background.type = Image.Type.Simple;
            background.color = Color.white;
            background.preserveAspect = false;
            background.raycastTarget = false;

            var portraitImage = EnsureRuntimeImage(root, "PortraitImage", new Vector2(rootSize.y * 0.72f, rootSize.y * 0.72f), new Vector2(-rootSize.x * 0.365f, 0f), true);
            portraitImage.sprite = playerMarkerSprite;
            portraitImage.preserveAspect = true;
            portraitImage.color = Color.white;
            portraitImage.raycastTarget = false;

            var titleText = EnsureRuntimeText(root, "TopHudTitleText", new Vector2(rootSize.x * 0.44f, rootSize.y * 0.24f), new Vector2(-rootSize.x * 0.05f, rootSize.y * 0.18f), true);
            SceneUILayoutHelper.StyleText(titleText, 18f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            titleText.color = new Color(0.18f, 0.12f, 0.06f, 1f);

            var bodyText = EnsureRuntimeText(root, "TopHudBodyText", new Vector2(rootSize.x * 0.46f, rootSize.y * 0.46f), new Vector2(-rootSize.x * 0.03f, -rootSize.y * 0.06f), true);
            SceneUILayoutHelper.StyleText(bodyText, 14f, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            bodyText.color = new Color(0.23f, 0.17f, 0.09f, 1f);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.overflowMode = TextOverflowModes.Ellipsis;

            var diceButton = EnsureTransparentButton(root, "TopHudDiceButton", new Vector2(rootSize.x * 0.19f, rootSize.y * 0.72f), new Vector2(rootSize.x * 0.377f, -2f), true);
            diceButton.onClick.RemoveAllListeners();
            diceButton.onClick.AddListener(ToggleDiceInspectPanel);

            mapTopHudView = new MapTopHudView
            {
                Root = root,
                PortraitImage = portraitImage,
                TitleText = titleText,
                BodyText = bodyText,
                DiceButton = diceButton
            };

        }

        private void RefreshRuntimeMapTopHud()
        {
            if (mapTopHudView == null)
            {
                return;
            }

            if (mapTopHudView.PortraitImage != null)
            {
                mapTopHudView.PortraitImage.sprite = playerMarkerSprite;
            }

            if (mapTopHudView.TitleText != null)
            {
                mapTopHudView.TitleText.text = "Dice Knight Overview";
            }

            if (mapTopHudView.BodyText != null)
            {
                mapTopHudView.BodyText.text = BuildRuntimeTopHudBodyText();
            }

        }

        private void EnsureDiceInspectPanel()
        {
            if (!useRuntimeLayout)
            {
                return;
            }

            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);
            if (canvas == null)
            {
                return;
            }

            var existingPanelRoot = FindRuntimeRect(canvas.transform, "RuntimeMapDiceInspectPanel");
            if (existingPanelRoot != null && (!Application.isPlaying || diceInspectView == null))
            {
                SyncRuntimeFrameLayoutFromRect(diceInspectPanelLayout, existingPanelRoot);
            }

            var panelRoot = existingPanelRoot ?? SceneUILayoutHelper.EnsureRuntimeRect(canvas.transform, "RuntimeMapDiceInspectPanel");
            var panelSprite = UiFrameSpriteLibrary.GetDicePanelSprite();
            var canvasRect = canvas.transform as RectTransform;
            var panelSize = FitSizeToCanvas(ResolveRuntimeFrameSize(panelSprite, diceInspectPanelLayout), canvasRect, new Vector2(0.94f, 0.46f), new Vector2(18f, 18f));
            SceneUILayoutHelper.SetRect(panelRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), diceInspectPanelLayout.AnchoredPosition, panelSize);
            ClampRectToCanvas(panelRoot, canvasRect, new Vector2(18f, 18f));

            var panelImage = panelRoot.GetComponent<Image>() ?? panelRoot.gameObject.AddComponent<Image>();
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Simple;
            panelImage.color = Color.white;
            panelImage.preserveAspect = false;
            panelImage.raycastTarget = false;

            var existingDetailRoot = FindRuntimeRect(canvas.transform, "RuntimeMapDiceInspectDetail");
            if (existingDetailRoot != null && (!Application.isPlaying || diceInspectView == null))
            {
                SyncRuntimeFrameLayoutFromRect(diceInspectDetailLayout, existingDetailRoot);
            }

            var detailCard = RuntimeSkillCardFactory.EnsureSkillCard(
                canvas.transform,
                "RuntimeMapDiceInspectDetail",
                headerText,
                diceInspectDetailLayout.AnchoredPosition,
                FitSizeToCanvas(ResolveRuntimeFrameSize(UiFrameSpriteLibrary.GetSkillDetailSprite(), diceInspectDetailLayout), canvasRect, new Vector2(0.94f, 0.19f), new Vector2(18f, 18f)),
                true);
            SceneUILayoutHelper.SetRect(
                detailCard.Root,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                diceInspectDetailLayout.AnchoredPosition,
                FitSizeToCanvas(ResolveRuntimeFrameSize(UiFrameSpriteLibrary.GetSkillDetailSprite(), diceInspectDetailLayout), canvasRect, new Vector2(0.94f, 0.19f), new Vector2(18f, 18f)),
                true);
            ClampRectToCanvas(detailCard.Root, canvasRect, new Vector2(18f, 18f));

            var slotViews = new DiceInspectSlotView[6];
            var slotPositions = GetDiceInspectSlotPositions(panelSize);
            var defaultSlotSize = new Vector2(panelSize.x * 0.22f, panelSize.y * 0.22f);

            for (var index = 0; index < slotViews.Length; index++)
            {
                var button = EnsureDiceInspectSlotButton(canvas.transform, panelRoot, $"DiceInspectSlotButton_{index}", defaultSlotSize, slotPositions[index]);
                button.onClick.RemoveAllListeners();
                var capturedIndex = index;
                button.onClick.AddListener(() => SelectDiceFace(capturedIndex));

                var buttonRect = button.transform as RectTransform;
                var slotSize = ResolveRectSize(buttonRect, defaultSlotSize);
                var highlight = EnsureRuntimeImage(button.transform, "Highlight", slotSize, Vector2.zero, true);
                highlight.sprite = GetRuntimeUiSprite();
                highlight.color = index == selectedDiceFaceIndex ? new Color(1f, 0.92f, 0.45f, 0.28f) : new Color(1f, 1f, 1f, 0f);
                highlight.raycastTarget = false;

                var icon = EnsureRuntimeImage(button.transform, "Icon", slotSize * 0.62f, Vector2.zero, true);
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.raycastTarget = false;

                var label = EnsureRuntimeText(button.transform, "Label", new Vector2(slotSize.x, slotSize.y * 0.25f), new Vector2(0f, -slotSize.y * 0.48f), true);
                SceneUILayoutHelper.StyleText(label, 11f, TextAlignmentOptions.Center, FontStyles.Bold);
                label.color = new Color(0.25f, 0.17f, 0.08f, 1f);

                slotViews[index] = new DiceInspectSlotView
                {
                    Button = button,
                    HighlightImage = highlight,
                    IconImage = icon,
                    LabelText = label
                };
            }

            diceInspectView = new DiceInspectView
            {
                PanelRoot = panelRoot,
                DetailCard = detailCard,
                SlotViews = slotViews
            };

            panelRoot.gameObject.SetActive(isDiceInspectOpen);
            detailCard.Root.gameObject.SetActive(isDiceInspectOpen);
            SetDiceInspectSlotButtonsVisible(slotViews, isDiceInspectOpen);
        }

        private void RefreshDiceInspectPanel()
        {
            if (diceInspectView == null)
            {
                return;
            }

            var faces = runManager?.PlayerState?.DiceFaces;
            var faceCount = faces != null ? faces.Count : 0;
            var isEditorPreview = !Application.isPlaying && runManager == null;
            var previewFaceCount = EditorPreviewSkillIds.Length;
            if (selectedDiceFaceIndex >= (isEditorPreview ? previewFaceCount : faceCount))
            {
                selectedDiceFaceIndex = Mathf.Max(0, (isEditorPreview ? previewFaceCount : faceCount) - 1);
            }

            for (var index = 0; index < diceInspectView.SlotViews.Length; index++)
            {
                var slotView = diceInspectView.SlotViews[index];
                var face = index < faceCount ? faces[index] : null;
                var previewSkillId = isEditorPreview && index < previewFaceCount ? EditorPreviewSkillIds[index] : null;
                var icon = face?.Skill != null ? SkillIconLibrary.GetSkillIcon(face.Skill) : SkillIconLibrary.GetSkillIcon(previewSkillId);
                if (slotView.IconImage != null)
                {
                    slotView.IconImage.sprite = icon;
                    slotView.IconImage.enabled = icon != null;
                    slotView.IconImage.preserveAspect = true;
                }

                if (slotView.LabelText != null)
                {
                    slotView.LabelText.text = face?.Skill != null
                        ? $"{index + 1}. {face.Skill.DisplayName}"
                        : !string.IsNullOrWhiteSpace(previewSkillId)
                            ? $"{index + 1}. {GetEditorPreviewSkillTitle(index)}"
                            : $"{index + 1}. Empty";
                }

                if (slotView.HighlightImage != null)
                {
                    slotView.HighlightImage.color = index == selectedDiceFaceIndex
                        ? new Color(1f, 0.92f, 0.45f, 0.28f)
                        : new Color(1f, 1f, 1f, 0f);
                }
            }

            var selectedFace = faceCount > 0 && selectedDiceFaceIndex < faceCount ? faces[selectedDiceFaceIndex] : null;
            if (selectedFace != null)
            {
                RuntimeSkillCardFactory.ApplySkillPresentation(
                    diceInspectView.DetailCard.IconImage,
                    diceInspectView.DetailCard.TitleText,
                    diceInspectView.DetailCard.BodyText,
                    selectedFace,
                    $"Face {selectedDiceFaceIndex + 1}",
                    "Face Detail",
                    "Choose a die face to inspect its skill.");
            }
            else
            {
                ApplyEditorPreviewSkillCard(diceInspectView.DetailCard, selectedDiceFaceIndex);
            }

            var isVisible = Application.isPlaying ? isDiceInspectOpen : showEditorDiceInspect;
            diceInspectView.PanelRoot.gameObject.SetActive(isVisible);
            diceInspectView.DetailCard.Root.gameObject.SetActive(isVisible);
            SetDiceInspectSlotButtonsVisible(diceInspectView.SlotViews, isVisible);
        }

        private void EnsureRuntimeUiTuner()
        {
            if (!useRuntimeLayout)
            {
                return;
            }

            if (!runtimeUiTunerSettings.ShowRuntimeTuner)
            {
                RemoveRuntimeUiTunerRoot();
                return;
            }

            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);
            if (canvas == null)
            {
                return;
            }

            var root = SceneUILayoutHelper.EnsureRuntimeRect(canvas.transform, "RuntimeUiTunerRoot");
            SceneUILayoutHelper.SetRect(root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), runtimeUiTunerSettings.AnchoredPosition, runtimeUiTunerSettings.PanelSize);

            var background = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            background.sprite = GetRuntimeUiSprite();
            background.type = Image.Type.Simple;
            background.color = new Color(0.09f, 0.13f, 0.2f, 0.9f);
            background.raycastTarget = true;

            var titleText = EnsureRuntimeText(root, "RuntimeUiTunerTitle", new Vector2(runtimeUiTunerSettings.PanelSize.x - 24f, 38f), new Vector2(0f, runtimeUiTunerSettings.PanelSize.y * 0.34f));
            SceneUILayoutHelper.StyleText(titleText, 14f, TextAlignmentOptions.Center, FontStyles.Bold);
            titleText.color = Color.white;

            var infoText = EnsureRuntimeText(root, "RuntimeUiTunerInfo", new Vector2(runtimeUiTunerSettings.PanelSize.x - 24f, 84f), new Vector2(0f, runtimeUiTunerSettings.PanelSize.y * 0.08f));
            SceneUILayoutHelper.StyleText(infoText, 11f, TextAlignmentOptions.Center, FontStyles.Bold);
            infoText.color = new Color(0.84f, 0.91f, 1f, 1f);
            infoText.textWrappingMode = TextWrappingModes.Normal;
            infoText.overflowMode = TextOverflowModes.Ellipsis;

            var targetButton = EnsureBattleButton(root, "RuntimeUiTunerTargetButton", "Target", new Vector2(0f, -22f), new Vector2(180f, 46f), new Color(0.21f, 0.42f, 0.72f));
            targetButton.onClick.RemoveAllListeners();
            targetButton.onClick.AddListener(CycleRuntimeUiTunerTarget);

            var xMinus = EnsureBattleButton(root, "RuntimeUiTunerXMinus", "X-", new Vector2(-94f, -82f), new Vector2(68f, 42f), new Color(0.22f, 0.28f, 0.38f));
            var xPlus = EnsureBattleButton(root, "RuntimeUiTunerXPlus", "X+", new Vector2(-18f, -82f), new Vector2(68f, 42f), new Color(0.22f, 0.28f, 0.38f));
            var yMinus = EnsureBattleButton(root, "RuntimeUiTunerYMinus", "Y-", new Vector2(58f, -82f), new Vector2(68f, 42f), new Color(0.22f, 0.28f, 0.38f));
            var yPlus = EnsureBattleButton(root, "RuntimeUiTunerYPlus", "Y+", new Vector2(134f, -82f), new Vector2(68f, 42f), new Color(0.22f, 0.28f, 0.38f));
            var scaleMinus = EnsureBattleButton(root, "RuntimeUiTunerScaleMinus", "S-", new Vector2(-56f, -134f), new Vector2(88f, 42f), new Color(0.18f, 0.46f, 0.66f));
            var scalePlus = EnsureBattleButton(root, "RuntimeUiTunerScalePlus", "S+", new Vector2(56f, -134f), new Vector2(88f, 42f), new Color(0.18f, 0.46f, 0.66f));

            BindTunerButton(xMinus, () => AdjustRuntimeUiTarget(-runtimeUiTunerSettings.PositionStep, 0f, 0f));
            BindTunerButton(xPlus, () => AdjustRuntimeUiTarget(runtimeUiTunerSettings.PositionStep, 0f, 0f));
            BindTunerButton(yMinus, () => AdjustRuntimeUiTarget(0f, -runtimeUiTunerSettings.PositionStep, 0f));
            BindTunerButton(yPlus, () => AdjustRuntimeUiTarget(0f, runtimeUiTunerSettings.PositionStep, 0f));
            BindTunerButton(scaleMinus, () => AdjustRuntimeUiTarget(0f, 0f, -runtimeUiTunerSettings.ScaleStep));
            BindTunerButton(scalePlus, () => AdjustRuntimeUiTarget(0f, 0f, runtimeUiTunerSettings.ScaleStep));

            runtimeUiTunerView = new RuntimeUiTunerView
            {
                Root = root,
                TitleText = titleText,
                InfoText = infoText
            };

            RefreshRuntimeUiTuner();
        }

        private void RefreshRuntimeUiTuner()
        {
            if (runtimeUiTunerView == null)
            {
                return;
            }

            runtimeUiTunerView.Root.gameObject.SetActive(runtimeUiTunerSettings.ShowRuntimeTuner);
            runtimeUiTunerView.TitleText.text = $"UI Tuner | {runtimeUiTunerTarget}";
            var position = GetSelectedRuntimeUiTargetPosition();
            var scale = GetSelectedRuntimeUiTargetScale();
            runtimeUiTunerView.InfoText.text = $"Pos {position.x:0}/{position.y:0}\nScale {scale.x:0.00}/{scale.y:0.00}";

        }

        private void RemoveRuntimeUiTunerRoot()
        {
            var existingRoot = runtimeUiTunerView?.Root ?? FindRuntimeRect(SceneUILayoutHelper.FindRootCanvas()?.transform, "RuntimeUiTunerRoot");
            runtimeUiTunerView = null;
            if (existingRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existingRoot.gameObject);
                return;
            }

            DestroyImmediate(existingRoot.gameObject);
        }

        private void ToggleDiceInspectPanel()
        {
            isDiceInspectOpen = !isDiceInspectOpen;
            if (isDiceInspectOpen && selectedDiceFaceIndex < 0)
            {
                selectedDiceFaceIndex = 0;
            }

            EnsureDiceInspectPanel();
            RefreshDiceInspectPanel();
            RefreshRuntimeUiTuner();
        }

        private void SelectDiceFace(int faceIndex)
        {
            selectedDiceFaceIndex = Mathf.Clamp(faceIndex, 0, Mathf.Max(0, (runManager?.PlayerState?.DiceFaces.Count ?? 1) - 1));
            isDiceInspectOpen = true;
            RefreshDiceInspectPanel();
        }

        private void CycleRuntimeUiTunerTarget()
        {
            runtimeUiTunerTarget = runtimeUiTunerTarget == RuntimeUiTunerTarget.Battlefield
                ? RuntimeUiTunerTarget.MapTopHud
                : runtimeUiTunerTarget + 1;
            RefreshRuntimeUiTuner();
        }

        private void AdjustRuntimeUiTarget(float deltaX, float deltaY, float deltaScale)
        {
            switch (runtimeUiTunerTarget)
            {
                case RuntimeUiTunerTarget.MapTopHud:
                    AdjustRuntimeFrame(mapTopHudLayout, deltaX, deltaY, deltaScale);
                    break;
                case RuntimeUiTunerTarget.DicePanel:
                    AdjustRuntimeFrame(diceInspectPanelLayout, deltaX, deltaY, deltaScale);
                    break;
                case RuntimeUiTunerTarget.DiceDetail:
                    AdjustRuntimeFrame(diceInspectDetailLayout, deltaX, deltaY, deltaScale);
                    break;
                case RuntimeUiTunerTarget.BattlePlayerCard:
                    battleOverlayLayout.PlayerSkillCardPosition += new Vector2(deltaX, deltaY);
                    AdjustRuntimeFrameScale(battleOverlayLayout.SkillCardFrame, deltaScale);
                    break;
                case RuntimeUiTunerTarget.BattleEnemyCard:
                    battleOverlayLayout.EnemySkillCardPosition += new Vector2(deltaX, deltaY);
                    AdjustRuntimeFrameScale(battleOverlayLayout.SkillCardFrame, deltaScale);
                    break;
                case RuntimeUiTunerTarget.Battlefield:
                    battleOverlayLayout.FieldPanelAnchoredPosition += new Vector2(deltaX, deltaY);
                    battleOverlayLayout.BattlefieldSpriteScale += new Vector2(deltaScale, deltaScale);
                    battleOverlayLayout.BattlefieldSpriteScale = new Vector2(
                        Mathf.Max(0.05f, battleOverlayLayout.BattlefieldSpriteScale.x),
                        Mathf.Max(0.05f, battleOverlayLayout.BattlefieldSpriteScale.y));
                    break;
            }

            EnsureRuntimeMapTopHud();
            EnsureDiceInspectPanel();
            EnsureBattleOverlay();
            RefreshRuntimeMapTopHud();
            RefreshDiceInspectPanel();
            RefreshBattleOverlayPresentation();
            RefreshRuntimeUiTuner();
        }

        private void RenderMapBoard()
        {
            if (!useRuntimeLayout || runtimeMapBoardRoot == null || runtimeMapOverlayRoot == null || runManager == null)
            {
                return;
            }

            var currentPosition = GetCurrentPlayerGridPosition();
            var pendingRevealIndices = new HashSet<int>(runManager.MapSystem.ConsumeNodesPendingReveal().Select(node => node.Index));
            startNodeView ??= CreateNodeView("RuntimeMapStartNode");
            PlaceNodeView(startNodeView, StartGridPosition);
            ConfigureStartNodeView(startNodeView, currentPosition == StartGridPosition, CanMoveToStart(currentPosition));
            playerMarkerView ??= CreatePlayerMarkerView();

            foreach (var node in runManager.MapSystem.Nodes)
            {
                if (!nodeViews.TryGetValue(node.Index, out var view))
                {
                    view = CreateNodeView($"RuntimeMapNode_{node.Index}");
                    view.NodeIndex = node.Index;
                    nodeViews[node.Index] = view;
                }

                PlaceNodeView(view, node.GridPosition);
                ConfigureMapNodeView(view, node, currentPosition == node.GridPosition, pendingRevealIndices.Contains(node.Index));
            }

            if (!isNodeTransitionPlaying)
            {
                PlacePlayerMarker(currentPosition);
            }
        }

        private void ConfigureStartNodeView(MapNodeView view, bool isCurrentPosition, bool canMoveToStart)
        {
            EnsureCloudCanvasGroup(view);
            view.Button.interactable = canMoveToStart && !IsMapInteractionBlocked();
            view.Button.onClick.RemoveAllListeners();
            if (view.Button.interactable)
            {
                view.Button.onClick.AddListener(HandleStartSelection);
            }
            view.IconImage.gameObject.SetActive(false);
            view.CloudImage.gameObject.SetActive(false);
            view.HighlightImage.gameObject.SetActive(true);
            view.HighlightImage.color = isCurrentPosition ? new Color(0.16f, 0.67f, 0.97f, 0.42f) : new Color(0.14f, 0.36f, 0.84f, 0.18f);
            view.CaptionText.gameObject.SetActive(isCurrentPosition);
            view.CaptionText.text = isCurrentPosition ? "START" : string.Empty;
            view.CaptionText.color = Color.white;
            view.MarkerBadge.gameObject.SetActive(false);
            view.MarkerText.gameObject.SetActive(false);
        }

        private void ConfigureMapNodeView(MapNodeView view, MapNodeRuntimeState node, bool isCurrentPosition, bool shouldAnimateReveal)
        {
            EnsureCloudCanvasGroup(view);
            view.Button.onClick.RemoveAllListeners();
            view.Button.interactable = node.IsUnlocked && !IsMapInteractionBlocked();
            if (view.Button.interactable)
            {
                var nodeIndex = node.Index;
                view.Button.onClick.AddListener(() => HandleNodeSelection(nodeIndex));
            }

            var isVisible = node.IsRevealed || shouldAnimateReveal;
            var hideNodeVisuals = node.IsCompleted;
            view.IconImage.sprite = GetCenteredSprite(ResolveIconSprite(node.Definition.NodeType));
            ApplyNativeSpriteLayout(view.IconImage, view.IconImage.sprite, new Vector2(0f, ScaleValue(8f)), nodeVisualScale);
            view.IconImage.gameObject.SetActive(isVisible && !hideNodeVisuals && view.IconImage.sprite != null);
            view.IconImage.color = ResolveIconColor(node, isCurrentPosition);
            view.HighlightImage.gameObject.SetActive(isVisible || isCurrentPosition);
            view.HighlightImage.color = ResolveHighlightColor(node, isCurrentPosition);
            view.CaptionText.gameObject.SetActive(isVisible && !hideNodeVisuals);
            view.CaptionText.text = hideNodeVisuals ? string.Empty : ResolveCaptionText(node, isCurrentPosition);
            view.CaptionText.color = ResolveCaptionColor(node, isCurrentPosition);
            view.MarkerBadge.gameObject.SetActive(false);
            view.MarkerText.gameObject.SetActive(false);

            if (shouldAnimateReveal)
            {
                StartCoroutine(FadeOutCloud(view));
            }
            else
            {
                ApplyNativeSpriteLayout(view.CloudImage, GetCenteredSprite(ResolveSprite(CloudSpriteName)), new Vector2(0f, ScaleValue(8f)), nodeVisualScale);
                view.CloudCanvasGroup.alpha = isVisible ? 0f : 1f;
                view.CloudImage.gameObject.SetActive(!isVisible);
            }
        }

        private IEnumerator FadeOutCloud(MapNodeView view)
        {
            EnsureCloudCanvasGroup(view);
            view.CloudImage.gameObject.SetActive(true);
            view.CloudCanvasGroup.alpha = 1f;
            var baseScale = Vector3.one * Mathf.Max(0.1f, nodeVisualScale);
            view.CloudImage.rectTransform.localScale = baseScale;

            const float duration = 0.45f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                view.CloudCanvasGroup.alpha = 1f - progress;
                view.CloudImage.rectTransform.localScale = Vector3.Lerp(baseScale, baseScale * 1.06f, progress);
                yield return null;
            }

            view.CloudCanvasGroup.alpha = 0f;
            view.CloudImage.rectTransform.localScale = baseScale;
            view.CloudImage.gameObject.SetActive(false);
        }

        private void HandleNodeSelection(int nodeIndex)
        {
            if (runManager == null)
            {
                return;
            }

            if (IsMapInteractionBlocked())
            {
                return;
            }

            var node = runManager.MapSystem.GetNode(nodeIndex);
            if (node == null || !node.IsUnlocked)
            {
                return;
            }

            if (node.IsCompleted)
            {
                if (!useRuntimeLayout || runtimeMapOverlayRoot == null)
                {
                    runManager.MoveToMapNode(node.Index);
                    Render();
                    return;
                }

                StartCoroutine(PlayVisitedNodeTransition(node));
                return;
            }

            if (!useRuntimeLayout || runtimeMapOverlayRoot == null)
            {
                runManager.SelectMapNode(nodeIndex);
                return;
            }

            StartCoroutine(PlayEncounterNodeTransition(node));
        }

        private void HandleStartSelection()
        {
            if (runManager == null)
            {
                return;
            }

            if (IsMapInteractionBlocked() || !CanMoveToStart(GetCurrentPlayerGridPosition()))
            {
                return;
            }

            if (!useRuntimeLayout || runtimeMapOverlayRoot == null)
            {
                runManager.MoveToMapStart();
                Render();
                return;
            }

            StartCoroutine(PlayReturnToStartTransition());
        }

        private bool IsMapInteractionBlocked()
        {
            return isNodeTransitionPlaying || isBattleOverlayOpen || isBattleResolvingPresentation;
        }

        private IEnumerator PlayEncounterNodeTransition(MapNodeRuntimeState targetNode)
        {
            if (targetNode == null)
            {
                yield break;
            }

            yield return PlayPlayerMarkerMotion(GetCurrentPlayerGridPosition(), targetNode.GridPosition);

            if (targetNode.Definition.NodeType == MapNodeType.Reward || targetNode.Definition.NodeType == MapNodeType.Shop)
            {
                runManager.SelectMapNode(targetNode.Index);
                yield break;
            }

            runManager.PrepareBattle(targetNode.Index);
            isNodeTransitionPlaying = false;
            OpenBattleOverlay();
            Render();
        }

        private IEnumerator PlayVisitedNodeTransition(MapNodeRuntimeState targetNode)
        {
            if (targetNode == null)
            {
                yield break;
            }

            yield return PlayPlayerMarkerMotion(GetCurrentPlayerGridPosition(), targetNode.GridPosition);
            runManager.MoveToMapNode(targetNode.Index);
            isNodeTransitionPlaying = false;
            Render();
        }

        private IEnumerator PlayReturnToStartTransition()
        {
            yield return PlayPlayerMarkerMotion(GetCurrentPlayerGridPosition(), StartGridPosition);
            runManager.MoveToMapStart();
            isNodeTransitionPlaying = false;
            Render();
        }

        private IEnumerator PlayPlayerMarkerMotion(Vector2Int fromGridPosition, Vector2Int toGridPosition)
        {
            isNodeTransitionPlaying = true;
            SetNodeInteractionEnabled(false);
            playerMarkerView ??= CreatePlayerMarkerView();

            var startPosition = GetMarkerTargetPosition(fromGridPosition);
            var endPosition = GetMarkerTargetPosition(toGridPosition);
            PlacePlayerMarker(fromGridPosition);

            var duration = Mathf.Max(0.08f, playerMoveDuration);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                var arcOffset = Mathf.Sin(progress * Mathf.PI) * playerMoveArcHeight;
                playerMarkerView.Root.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedProgress) + new Vector2(0f, arcOffset);
                playerMarkerView.Root.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, Mathf.Sin(progress * Mathf.PI));
                yield return null;
            }

            playerMarkerView.Root.anchoredPosition = endPosition;
            playerMarkerView.Root.localScale = Vector3.one;
        }

        private MapNodeView CreateNodeView(string objectName)
        {
            var rootSize = ScaleSize(380f);
            var highlightSize = ScaleSize(110f);
            var badgeSize = new Vector2(ScaleValue(76f), ScaleValue(28f));
            var markerOffset = new Vector2(0f, ScaleValue(54f));
            var captionSize = new Vector2(ScaleValue(132f), ScaleValue(40f));
            var captionOffset = new Vector2(0f, -ScaleValue(56f));

            var root = SceneUILayoutHelper.EnsureRuntimeRect(runtimeMapOverlayRoot, objectName);
            SceneUILayoutHelper.SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, rootSize);

            var buttonImage = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            buttonImage.sprite = GetRuntimeUiSprite();
            buttonImage.color = new Color(1f, 1f, 1f, 0.01f);
            var button = root.GetComponent<Button>() ?? root.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            var highlightImage = EnsureRuntimeImage(root, "Highlight", highlightSize, new Vector2(0f, ScaleValue(4f)));
            highlightImage.sprite = GetRuntimeUiSprite();
            highlightImage.raycastTarget = false;

            var iconImage = EnsureRuntimeImage(root, "Icon", Vector2.zero, new Vector2(0f, ScaleValue(8f)));
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;

            var cloudImage = EnsureRuntimeImage(root, "Cloud", Vector2.zero, new Vector2(0f, ScaleValue(8f)));
            cloudImage.color = Color.white;
            cloudImage.raycastTarget = false;
            cloudImage.preserveAspect = true;
            ApplyNativeSpriteLayout(cloudImage, GetCenteredSprite(ResolveSprite(CloudSpriteName)), new Vector2(0f, ScaleValue(8f)), nodeVisualScale);
            var cloudCanvasGroup = cloudImage.GetComponent<CanvasGroup>() ?? cloudImage.gameObject.AddComponent<CanvasGroup>();

            var badgeImage = EnsureRuntimeImage(root, "MarkerBadge", badgeSize, markerOffset);
            badgeImage.sprite = GetRuntimeUiSprite();
            badgeImage.color = new Color(0.12f, 0.55f, 0.96f, 0.96f);
            badgeImage.raycastTarget = false;

            var markerText = EnsureRuntimeText(root, "MarkerText", badgeSize, markerOffset);
            markerText.fontSize = ScaleFont(18f);
            markerText.alignment = TextAlignmentOptions.Center;
            markerText.fontStyle = FontStyles.Bold;
            markerText.color = Color.white;

            var captionText = EnsureRuntimeText(root, "CaptionText", captionSize, captionOffset);
            captionText.fontSize = ScaleFont(18f);
            captionText.alignment = TextAlignmentOptions.Center;
            captionText.fontStyle = FontStyles.Bold;
            captionText.color = Color.white;

            return new MapNodeView
            {
                Root = root,
                Button = button,
                HighlightImage = highlightImage,
                IconImage = iconImage,
                CloudImage = cloudImage,
                CloudCanvasGroup = cloudCanvasGroup,
                MarkerBadge = badgeImage,
                MarkerText = markerText,
                CaptionText = captionText
            };
        }

        private PlayerMarkerView CreatePlayerMarkerView()
        {
            LoadMapSpritesIfNeeded();

            var resolvedMarkerSprite = GetCenteredSprite(playerMarkerSprite);
            var markerScale = Mathf.Max(0.18f, nodeVisualScale * 0.42f);
            var markerSize = resolvedMarkerSprite != null
                ? new Vector2(resolvedMarkerSprite.rect.width * markerScale, resolvedMarkerSprite.rect.height * markerScale)
                : new Vector2(ScaleValue(76f), ScaleValue(28f));
            var markerOffset = resolvedMarkerSprite != null
                ? new Vector2(0f, Mathf.Max(ScaleValue(54f), markerSize.y * 0.55f))
                : new Vector2(0f, ScaleValue(54f));

            var root = SceneUILayoutHelper.EnsureRuntimeRect(runtimeMapOverlayRoot, "RuntimeMapPlayerMarker");
            SceneUILayoutHelper.SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, markerSize);

            var badgeImage = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            badgeImage.sprite = resolvedMarkerSprite != null ? resolvedMarkerSprite : GetRuntimeUiSprite();
            badgeImage.color = resolvedMarkerSprite != null ? Color.white : new Color(0.12f, 0.55f, 0.96f, 0.96f);
            badgeImage.raycastTarget = false;
            badgeImage.preserveAspect = resolvedMarkerSprite != null;

            var markerText = EnsureRuntimeText(root, "MarkerText", markerSize, Vector2.zero);
            markerText.fontSize = ScaleFont(18f);
            markerText.alignment = TextAlignmentOptions.Center;
            markerText.fontStyle = FontStyles.Bold;
            markerText.color = Color.white;
            markerText.text = "YOU";
            markerText.gameObject.SetActive(resolvedMarkerSprite == null);

            root.anchoredPosition = markerOffset;

            return new PlayerMarkerView
            {
                Root = root,
                BadgeImage = badgeImage,
                MarkerText = markerText,
                Offset = markerOffset
            };
        }

        private void PlaceNodeView(MapNodeView view, Vector2Int gridPosition)
        {
            view.Root.anchoredPosition = GetBoardLocalPosition(gridPosition);
        }

        private void PlacePlayerMarker(Vector2Int gridPosition)
        {
            playerMarkerView ??= CreatePlayerMarkerView();
            if (playerMarkerView?.Root == null)
            {
                return;
            }

            playerMarkerView.Root.anchoredPosition = GetMarkerTargetPosition(gridPosition);
            playerMarkerView.Root.localScale = Vector3.one;
            playerMarkerView.Root.gameObject.SetActive(true);
        }

        private Vector2 GetMarkerTargetPosition(Vector2Int gridPosition)
        {
            playerMarkerView ??= CreatePlayerMarkerView();
            var offset = playerMarkerView != null ? playerMarkerView.Offset : new Vector2(0f, ScaleValue(54f));
            return GetBoardLocalPosition(gridPosition) + offset;
        }

        private void SetNodeInteractionEnabled(bool isEnabled)
        {
            isEnabled &= !IsMapInteractionBlocked();

            if (startNodeView?.Button != null)
            {
                startNodeView.Button.interactable = isEnabled && CanMoveToStart(GetCurrentPlayerGridPosition());
            }

            foreach (var entry in nodeViews.Values)
            {
                if (entry?.Button == null)
                {
                    continue;
                }

                if (!isEnabled)
                {
                    entry.Button.interactable = false;
                    continue;
                }

                var node = runManager.MapSystem.GetNode(entry.NodeIndex);
                entry.Button.interactable = node != null && node.IsUnlocked && !node.IsCompleted;
            }
        }

        private Vector2 GetBoardLocalPosition(Vector2Int gridPosition)
        {
            if (TryGetEditorAnchorPosition(gridPosition, out var anchoredPosition))
            {
                return anchoredPosition;
            }

            var x = (ColumnAnchors[Mathf.Clamp(gridPosition.x, 0, ColumnAnchors.Length - 1)] - 0.5f) * runtimeMapBoardRoot.rect.width;
            var y = (RowAnchors[Mathf.Clamp(gridPosition.y, 0, RowAnchors.Length - 1)] - 0.5f) * runtimeMapBoardRoot.rect.height;
            return new Vector2(x, y);
        }

        private void EnsureBattleOverlay()
        {
            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);
            if (canvas == null)
            {
                return;
            }

            var root = SceneUILayoutHelper.EnsureRuntimeRect(canvas.transform, "RuntimeMapBattleOverlayRoot");
            SceneUILayoutHelper.Stretch(root, preserveExistingLayout: true);

            var blocker = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            blocker.sprite = GetRuntimeUiSprite();
            blocker.type = Image.Type.Simple;
            blocker.color = new Color(0f, 0f, 0f, 0.72f);
            blocker.raycastTarget = true;

            var battlefieldSprite = MonsterSpriteLibrary.GetBattlefieldSprite();

            var topCard = SceneUILayoutHelper.EnsurePanel(
                root,
                "RuntimeMapBattleTopCard",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                battleOverlayLayout.FieldPanelAnchoredPosition,
                ResolveBattlefieldPanelSize(battlefieldSprite),
                new Color(0.11f, 0.17f, 0.28f, 0.96f),
                true);

            var bottomCard = SceneUILayoutHelper.EnsurePanel(
                root,
                "RuntimeMapBattleBottomCard",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                battleOverlayLayout.LogPanelAnchoredPosition,
                battleOverlayLayout.LogPanelSize,
                new Color(0.12f, 0.18f, 0.3f, 0.95f),
                true);
            if (battlefieldSprite != null)
            {
                topCard.sprite = battlefieldSprite;
                topCard.type = Image.Type.Simple;
                topCard.color = Color.white;
                topCard.preserveAspect = false;
            }

            var presentationRoot = SceneUILayoutHelper.EnsureRuntimeRect(topCard.rectTransform, "RuntimeMapBattlePresentation");
            SceneUILayoutHelper.Stretch(presentationRoot, 18f, 18f, 22f, 22f, true);

            var bottomContentRoot = SceneUILayoutHelper.EnsureRuntimeRect(bottomCard.rectTransform, "RuntimeMapBattleBottomContent");
            SceneUILayoutHelper.Stretch(bottomContentRoot, 22f, 22f, 18f, 18f, true);

            battleHud ??= root.GetComponent<BattleHUD>() ?? root.gameObject.AddComponent<BattleHUD>();
            battlePresenter ??= root.GetComponent<BattlePresenter>() ?? root.gameObject.AddComponent<BattlePresenter>();

            var turnText = EnsureRuntimeText(presentationRoot, "MapBattleTurnText", new Vector2(360f, 64f), battleOverlayLayout.TurnTextPosition, true);
            turnText.text = "Turn 1";
            SceneUILayoutHelper.StyleText(turnText, 34f, TextAlignmentOptions.Center, FontStyles.Bold);
            turnText.color = Color.white;

            var actingUnitText = EnsureRuntimeText(presentationRoot, "MapBattleActingUnitText", new Vector2(520f, 50f), battleOverlayLayout.ActingUnitTextPosition, true);
            actingUnitText.text = "Acting: Waiting";
            SceneUILayoutHelper.StyleText(actingUnitText, 22f, TextAlignmentOptions.Center, FontStyles.Bold);
            actingUnitText.color = new Color(1f, 0.92f, 0.65f, 1f);

            var turnQueueText = EnsureRuntimeText(presentationRoot, "MapBattleTurnQueueText", new Vector2(880f, 84f), battleOverlayLayout.TurnQueueTextPosition, true);
            turnQueueText.text = "Turn Queue";
            SceneUILayoutHelper.StyleText(turnQueueText, 17f, TextAlignmentOptions.Center, FontStyles.Bold);
            turnQueueText.color = new Color(0.84f, 0.9f, 0.98f, 1f);

            var battlePlayerStatsText = EnsureRuntimeText(presentationRoot, "MapBattlePlayerStatsText", new Vector2(320f, 190f), battleOverlayLayout.PlayerStatsTextPosition, true);
            battlePlayerStatsText.text = "Player";
            SceneUILayoutHelper.StyleText(battlePlayerStatsText, 18f, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            battlePlayerStatsText.color = Color.white;

            var enemyStatsText = EnsureRuntimeText(presentationRoot, "MapBattleEnemyStatsText", new Vector2(320f, 210f), battleOverlayLayout.EnemyStatsTextPosition, true);
            enemyStatsText.text = "Enemies";
            SceneUILayoutHelper.StyleText(enemyStatsText, 17f, TextAlignmentOptions.TopRight, FontStyles.Bold);
            enemyStatsText.color = Color.white;

            var rollLogText = EnsureRuntimeText(bottomContentRoot, "MapBattleRollLogText", new Vector2(860f, 52f), battleOverlayLayout.RollLogTextPosition, true);
            rollLogText.text = "Roll Log";
            SceneUILayoutHelper.StyleText(rollLogText, 18f, TextAlignmentOptions.Center, FontStyles.Bold);
            rollLogText.color = new Color(0.88f, 0.93f, 1f, 1f);

            var battleLogText = EnsureRuntimeText(bottomContentRoot, "MapBattleLogText", new Vector2(900f, 108f), battleOverlayLayout.BattleLogTextPosition, true);
            battleLogText.text = "Battle Log";
            SceneUILayoutHelper.StyleText(battleLogText, 16f, TextAlignmentOptions.TopLeft);
            battleLogText.color = new Color(0.86f, 0.92f, 0.98f, 1f);

            var resultText = EnsureRuntimeText(presentationRoot, "MapBattleResultText", new Vector2(860f, 220f), battleOverlayLayout.ResultTextPosition, true);
            resultText.text = string.Empty;
            SceneUILayoutHelper.StyleText(resultText, 36f, TextAlignmentOptions.Center, FontStyles.Bold);
            resultText.color = Color.white;

            var baseSkillCardSize = ResolveRuntimeFrameSize(UiFrameSpriteLibrary.GetSkillDetailSprite(), battleOverlayLayout.SkillCardFrame);
            var sharedSkillCardSize = new Vector2(
                Mathf.Min(Mathf.Max(320f, battleOverlayLayout.LogPanelSize.x - 120f), baseSkillCardSize.x * 2f + 80f),
                baseSkillCardSize.y);
            var sharedSkillCardPosition = (battleOverlayLayout.PlayerSkillCardPosition + battleOverlayLayout.EnemySkillCardPosition) * 0.5f;
            var sharedSkillCard = RuntimeSkillCardFactory.EnsureSkillCard(
                bottomContentRoot,
                "MapBattleSkillCard",
                headerText,
                sharedSkillCardPosition,
                sharedSkillCardSize,
                true);

            var legacyPlayerSkillCard = bottomContentRoot.Find("MapBattlePlayerSkillCard");
            if (legacyPlayerSkillCard != null)
            {
                legacyPlayerSkillCard.gameObject.SetActive(false);
            }

            var legacyEnemySkillCard = bottomContentRoot.Find("MapBattleEnemySkillCard");
            if (legacyEnemySkillCard != null)
            {
                legacyEnemySkillCard.gameObject.SetActive(false);
            }

            rollLogText.gameObject.SetActive(false);
            battleLogText.gameObject.SetActive(false);

            var rollButton = EnsureBattleButton(bottomContentRoot, "MapBattleRollButton", "Resolve Turn", battleOverlayLayout.RollButtonPosition, new Vector2(320f, 88f), new Color(0.17f, 0.55f, 0.98f));
            var continueButton = EnsureBattleButton(bottomContentRoot, "MapBattleContinueButton", "Continue", battleOverlayLayout.ContinueButtonPosition, new Vector2(340f, 88f), new Color(0.22f, 0.72f, 0.42f));
            var backButton = EnsureBattleButton(bottomContentRoot, "MapBattleBackButton", "Main Menu", battleOverlayLayout.BackButtonPosition, new Vector2(220f, 88f), new Color(0.2f, 0.24f, 0.34f));
            var autoToggle = EnsureBattleToggle(bottomContentRoot, "MapBattleAutoToggle", "Auto Battle", battleOverlayLayout.AutoTogglePosition, new Vector2(300f, 78f));

            var unitsRoot = SceneUILayoutHelper.EnsureRuntimeRect(presentationRoot, "RuntimeMapBattleUnits");
            SceneUILayoutHelper.Stretch(unitsRoot, preserveExistingLayout: true);

            var playerView = CreateBattleUnitView(unitsRoot, "RuntimeMapBattlePlayerUnitView", battleOverlayLayout.PlayerUnitPosition, battleOverlayLayout.CombatantViewSize);
            var enemyView01 = CreateBattleUnitView(unitsRoot, "RuntimeMapBattleEnemyUnitView01", battleOverlayLayout.EnemyUnitPosition01, battleOverlayLayout.CombatantViewSize);
            var enemyView02 = CreateBattleUnitView(unitsRoot, "RuntimeMapBattleEnemyUnitView02", battleOverlayLayout.EnemyUnitPosition02, battleOverlayLayout.CombatantViewSize);
            var enemyView03 = CreateBattleUnitView(unitsRoot, "RuntimeMapBattleEnemyUnitView03", battleOverlayLayout.EnemyUnitPosition03, battleOverlayLayout.CombatantViewSize);
            enemyView02.gameObject.SetActive(false);
            enemyView03.gameObject.SetActive(false);

            battleFloatingTextSpawner = EnsureBattleFloatingTextSpawner(presentationRoot);

            battleHud.Configure(
                turnText,
                actingUnitText,
                turnQueueText,
                battlePlayerStatsText,
                enemyStatsText,
                rollLogText,
                battleLogText,
                null,
                null,
                null,
                null,
                null,
                null);
            battleHud.ConfigureSharedSkillCard(
                sharedSkillCard.IconImage,
                sharedSkillCard.TitleText,
                sharedSkillCard.BodyText);
            battlePresenter.Configure(playerView, new[] { enemyView01, enemyView02, enemyView03 }, battleHud, battleFloatingTextSpawner, canvas, battleHitEffectSettings);

            rollButton.onClick.RemoveAllListeners();
            if (Application.isPlaying)
            {
                rollButton.onClick.AddListener(ResolveBattleTurn);
            }

            continueButton.onClick.RemoveAllListeners();
            if (Application.isPlaying && runManager != null)
            {
                continueButton.onClick.AddListener(runManager.CompleteBattleAndAdvance);
            }

            backButton.onClick.RemoveAllListeners();
            if (Application.isPlaying && runManager != null)
            {
                backButton.onClick.AddListener(() => SceneManager.LoadScene(runManager.Config.MainMenuSceneName));
            }

            autoToggle.onValueChanged.RemoveAllListeners();
            if (Application.isPlaying)
            {
                autoToggle.onValueChanged.AddListener(OnBattleAutoToggleChanged);
            }

            battleOverlayView = new BattleOverlayView
            {
                Root = root,
                TopCard = topCard.rectTransform,
                BottomCard = bottomCard.rectTransform,
                PresentationRoot = presentationRoot,
                TurnText = turnText,
                ActingUnitText = actingUnitText,
                TurnQueueText = turnQueueText,
                PlayerStatsText = battlePlayerStatsText,
                EnemyStatsText = enemyStatsText,
                RollLogText = rollLogText,
                BattleLogText = battleLogText,
                ResultText = resultText,
                ActiveSkillCard = sharedSkillCard,
                RollButton = rollButton,
                ContinueButton = continueButton,
                BackButton = backButton,
                AutoToggle = autoToggle
            };

            if (!Application.isPlaying)
            {
                PopulateEditorBattleOverlayPreview();
            }

            root.gameObject.SetActive(Application.isPlaying ? isBattleOverlayOpen : showEditorBattleOverlay);
        }

        private void OpenBattleOverlay()
        {
            EnsureBattleOverlay();
            if (battleOverlayView?.Root == null || runManager?.BattleSystem == null)
            {
                return;
            }

            isBattleOverlayOpen = true;
            battleOverlayView.Root.gameObject.SetActive(true);
            SetNodeInteractionEnabled(false);
            battleOverlayView.AutoToggle.isOn = runManager.AutoBattleEnabled;
            battlePresenter?.BindBattle(runManager.BattleSystem);
            battleHud?.Refresh(runManager.BattleSystem);
            ApplyBattleOverlayResultState();
            RefreshBattleOverlayPresentation();
            RefreshBattleAutoFlow();
            RefreshRuntimeMapTopHud();
            RefreshDiceInspectPanel();
            RefreshRuntimeUiTuner();
            CacheRuntimeBattleAuthoringSignatures();
        }

        private void RefreshBattleOverlayPresentation()
        {
            if (!isBattleOverlayOpen || runManager?.BattleSystem == null)
            {
                return;
            }

            battlePresenter?.BindBattle(runManager.BattleSystem);
            battleHud?.Refresh(runManager.BattleSystem);
            ApplyBattleOverlayResultState();
            RefreshBattleAutoFlow();
            RefreshRuntimeMapTopHud();
            RefreshDiceInspectPanel();
            RefreshRuntimeUiTuner();
        }

        private void ApplyRuntimeBattleAuthoringRefresh()
        {
            var layoutSignature = GetBattleOverlayLayoutSignature();
            var hitEffectSignature = GetBattleHitEffectSignature();
            var runtimeUiSignature = GetRuntimeUiLayoutSignature();
            var layoutChanged = layoutSignature != lastBattleOverlayLayoutSignature;
            var hitEffectChanged = hitEffectSignature != lastBattleHitEffectSignature;
            var runtimeUiChanged = runtimeUiSignature != lastRuntimeUiLayoutSignature;

            if (!layoutChanged && !hitEffectChanged && !runtimeUiChanged)
            {
                return;
            }

            if (runtimeUiChanged)
            {
                EnsureRuntimeMapTopHud();
                EnsureDiceInspectPanel();
                EnsureRuntimeUiTuner();
                RefreshRuntimeMapTopHud();
                RefreshDiceInspectPanel();
                RefreshRuntimeUiTuner();
            }

            if ((layoutChanged || hitEffectChanged || runtimeUiChanged) && isBattleOverlayOpen && battleOverlayView?.Root != null && battleOverlayView.Root.gameObject.activeInHierarchy)
            {
                EnsureBattleOverlay();

                if (layoutChanged || runtimeUiChanged)
                {
                    RefreshBattleOverlayPresentation();
                }

                if (hitEffectChanged)
                {
                    battlePresenter?.RefreshHitEffectSettings(SceneUILayoutHelper.FindRootCanvas(), battleHitEffectSettings);
                    battlePresenter?.PreviewConfiguredHitEffect();
                }
            }

            lastBattleOverlayLayoutSignature = layoutSignature;
            lastBattleHitEffectSignature = hitEffectSignature;
            lastRuntimeUiLayoutSignature = runtimeUiSignature;
        }

        private void OnBattleAutoToggleChanged(bool isOn)
        {
            if (runManager == null)
            {
                return;
            }

            runManager.AutoBattleEnabled = isOn;
            RefreshBattleAutoFlow();
        }

        private void RefreshBattleAutoFlow()
        {
            var battleSystem = runManager != null ? runManager.BattleSystem : null;
            var isBattleFinished = battleSystem == null || battleSystem.IsFinished;

            if (battleOverlayView?.RollButton != null)
            {
                battleOverlayView.RollButton.gameObject.SetActive(!isBattleFinished);
                battleOverlayView.RollButton.interactable = !runManager.AutoBattleEnabled && !isBattleFinished && !isBattleResolvingPresentation;
            }

            if (battleOverlayView?.AutoToggle != null)
            {
                battleOverlayView.AutoToggle.gameObject.SetActive(!isBattleFinished);
                battleOverlayView.AutoToggle.interactable = !isBattleFinished && !isBattleResolvingPresentation;
            }

            if (battleOverlayView?.BackButton != null)
            {
                battleOverlayView.BackButton.interactable = !isBattleResolvingPresentation;
            }

            if (!isBattleOverlayOpen || isBattleFinished)
            {
                if (battleAutoBattleRoutine != null)
                {
                    StopCoroutine(battleAutoBattleRoutine);
                    battleAutoBattleRoutine = null;
                }

                return;
            }

            if (runManager.AutoBattleEnabled && !isBattleResolvingPresentation)
            {
                if (battleAutoBattleRoutine == null)
                {
                    battleAutoBattleRoutine = StartCoroutine(BattleAutoResolveRoutine());
                }
            }
            else if (battleAutoBattleRoutine != null)
            {
                StopCoroutine(battleAutoBattleRoutine);
                battleAutoBattleRoutine = null;
            }
        }

        private IEnumerator BattleAutoResolveRoutine()
        {
            while (isBattleOverlayOpen && runManager.BattleSystem != null && !runManager.BattleSystem.IsFinished && runManager.AutoBattleEnabled)
            {
                ResolveBattleTurn();
                yield return new WaitForSeconds(runManager.Config.AutoTurnDelay);
            }

            battleAutoBattleRoutine = null;
        }

        private void ResolveBattleTurn()
        {
            if (!isBattleOverlayOpen || runManager.BattleSystem == null || runManager.BattleSystem.IsFinished || isBattleResolvingPresentation)
            {
                return;
            }

            var report = runManager.BattleSystem.ResolveNextTurn();
            StartCoroutine(PresentBattleTurn(report));
        }

        private IEnumerator PresentBattleTurn(BattleTurnReport report)
        {
            if (runManager.BattleSystem == null)
            {
                yield break;
            }

            isBattleResolvingPresentation = true;
            RefreshBattleAutoFlow();

            if (battleOverlayView?.BackButton != null)
            {
                battleOverlayView.BackButton.interactable = false;
            }

            if (battlePresenter != null)
            {
                yield return battlePresenter.PlayTurnReport(runManager.BattleSystem, report, null);
            }
            else
            {
                battleHud?.Refresh(runManager.BattleSystem, report);
            }

            battleHud?.Refresh(runManager.BattleSystem, report);
            isBattleResolvingPresentation = false;

            if (battleOverlayView?.BackButton != null)
            {
                battleOverlayView.BackButton.interactable = true;
            }

            ApplyBattleOverlayResultState();
            RefreshBattleAutoFlow();
        }

        private void ApplyBattleOverlayResultState()
        {
            if (battleOverlayView == null)
            {
                return;
            }

            var battleSystem = runManager != null ? runManager.BattleSystem : null;
            var isFinished = battleSystem != null && battleSystem.IsFinished;

            battleOverlayView.ResultText.gameObject.SetActive(isFinished);
            battleOverlayView.ContinueButton.gameObject.SetActive(isFinished);

            if (!isFinished)
            {
                battleOverlayView.ResultText.text = string.Empty;
                return;
            }

            battleOverlayView.ResultText.text = battleSystem.BattleResult == BattleResultType.Victory
                ? "Victory. Take your reward and continue the run."
                : "Defeat. Return to the main menu.";
        }

        private FloatingTextSpawner EnsureBattleFloatingTextSpawner(RectTransform parent)
        {
            var floatingCanvas = SceneUILayoutHelper.EnsureRuntimeRect(parent, "RuntimeMapBattleFloatingTextCanvas");
            SceneUILayoutHelper.Stretch(floatingCanvas, preserveExistingLayout: true);

            var spawner = floatingCanvas.GetComponent<FloatingTextSpawner>() ?? floatingCanvas.gameObject.AddComponent<FloatingTextSpawner>();
            var template = EnsureRuntimeText(floatingCanvas, "RuntimeMapBattleFloatingTextTemplate", new Vector2(240f, 60f), Vector2.zero, true);
            template.text = "-99";
            template.fontSize = 34f;
            template.fontSizeMax = 34f;
            template.fontSizeMin = 20f;
            template.alignment = TextAlignmentOptions.Center;
            template.fontStyle = FontStyles.Bold;
            template.gameObject.SetActive(false);
            spawner.Configure(floatingCanvas, template);
            return spawner;
        }

        private UnitView CreateBattleUnitView(RectTransform parent, string objectName, Vector2 anchoredPosition, Vector2 size)
        {
            var root = SceneUILayoutHelper.EnsureRuntimeRect(parent, objectName);
            SceneUILayoutHelper.SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size, true);

            var unitView = root.GetComponent<UnitView>() ?? root.gameObject.AddComponent<UnitView>();
            var canvasGroup = root.GetComponent<CanvasGroup>() ?? root.gameObject.AddComponent<CanvasGroup>();

            var highlightImage = EnsureRuntimeImage(root, "Highlight", new Vector2(232f, 232f), new Vector2(0f, 48f), true);
            highlightImage.sprite = GetRuntimeUiSprite();
            highlightImage.color = new Color(1f, 1f, 1f, 0.16f);

            var spriteImage = EnsureRuntimeImage(root, "SpriteImage", new Vector2(220f, 220f), new Vector2(0f, 48f), true);
            spriteImage.color = Color.white;
            spriteImage.preserveAspect = true;

            var popupAnchor = SceneUILayoutHelper.EnsureRuntimeRect(root, "PopupAnchor");
            SceneUILayoutHelper.SetRect(popupAnchor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), Vector2.zero, true);

            var nameText = EnsureRuntimeText(root, "NameText", new Vector2(240f, 38f), new Vector2(0f, 180f), true);
            nameText.text = objectName;
            nameText.fontSize = 28f;
            nameText.fontSizeMax = 28f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Center;

            var hpBar = CreateBattleSlider(root, "HpBar", new Vector2(0f, -74f), new Vector2(220f, 18f));

            var hpText = EnsureRuntimeText(root, "HpText", new Vector2(240f, 28f), new Vector2(0f, -102f), true);
            hpText.text = "HP 0/0";
            hpText.fontSize = 20f;
            hpText.fontSizeMax = 20f;
            hpText.alignment = TextAlignmentOptions.Center;

            var shieldArmorText = EnsureRuntimeText(root, "ShieldArmorText", new Vector2(260f, 28f), new Vector2(0f, -132f), true);
            shieldArmorText.text = "Shield 0 / Armor 0";
            shieldArmorText.fontSize = 18f;
            shieldArmorText.fontSizeMax = 18f;
            shieldArmorText.alignment = TextAlignmentOptions.Center;

            var rageText = EnsureRuntimeText(root, "RageText", new Vector2(220f, 28f), new Vector2(0f, -160f), true);
            rageText.text = "Rage 0";
            rageText.fontSize = 18f;
            rageText.fontSizeMax = 18f;
            rageText.alignment = TextAlignmentOptions.Center;

            unitView.ConfigureRuntime(root, popupAnchor, canvasGroup, spriteImage, highlightImage, hpBar, nameText, hpText, shieldArmorText, rageText);
            return unitView;
        }

        private Slider CreateBattleSlider(RectTransform parent, string objectName, Vector2 anchoredPosition, Vector2 size)
        {
            var sliderRoot = SceneUILayoutHelper.EnsureRuntimeRect(parent, objectName);
            SceneUILayoutHelper.SetRect(sliderRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size, true);

            var background = CreateBattleStretchImage(sliderRoot, "Background", new Color(0.12f, 0.12f, 0.12f, 0.9f));
            var fillArea = SceneUILayoutHelper.EnsureRuntimeRect(sliderRoot, "Fill Area");
            SceneUILayoutHelper.Stretch(fillArea, 2f, 2f, 2f, 2f, true);

            var fill = CreateBattleStretchImage(fillArea, "Fill", new Color(0.3f, 0.95f, 0.45f, 1f));

            var slider = sliderRoot.GetComponent<Slider>() ?? sliderRoot.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.normalizedValue = 1f;
            slider.targetGraphic = background;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = null;
            return slider;
        }

        private Image CreateBattleStretchImage(RectTransform parent, string objectName, Color color)
        {
            var rect = SceneUILayoutHelper.EnsureRuntimeRect(parent, objectName);
            SceneUILayoutHelper.Stretch(rect, preserveExistingLayout: true);

            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = GetRuntimeUiSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Button EnsureBattleButton(RectTransform parent, string objectName, string labelText, Vector2 anchoredPosition, Vector2 size, Color backgroundColor)
        {
            var rect = SceneUILayoutHelper.EnsureRuntimeRect(parent, objectName);
            SceneUILayoutHelper.SetRect(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), anchoredPosition, size, true);

            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = GetRuntimeUiSprite();
            image.type = Image.Type.Simple;
            image.raycastTarget = true;

            var button = rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = EnsureRuntimeText(rect, "Label", size - new Vector2(40f, 24f), Vector2.zero, true);
            label.text = labelText;
            SceneUILayoutHelper.StyleButton(button, size, 24f, backgroundColor, Color.white, true);
            return button;
        }

        private Toggle EnsureBattleToggle(RectTransform parent, string objectName, string labelText, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = SceneUILayoutHelper.EnsureRuntimeRect(parent, objectName);
            SceneUILayoutHelper.SetRect(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), anchoredPosition, size, true);

            var background = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            background.sprite = GetRuntimeUiSprite();
            background.type = Image.Type.Simple;
            background.color = new Color(0.16f, 0.22f, 0.34f, 0.96f);
            background.raycastTarget = true;

            var toggle = rect.GetComponent<Toggle>() ?? rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.transition = Selectable.Transition.ColorTint;

            var checkRect = SceneUILayoutHelper.EnsureRuntimeRect(rect, "Checkmark");
            SceneUILayoutHelper.SetRect(checkRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(30f, 30f), true);
            var checkImage = checkRect.GetComponent<Image>() ?? checkRect.gameObject.AddComponent<Image>();
            checkImage.sprite = GetRuntimeUiSprite();
            checkImage.type = Image.Type.Simple;
            checkImage.color = new Color(0.34f, 0.84f, 0.48f, 1f);
            checkImage.raycastTarget = false;
            toggle.graphic = checkImage;

            var label = EnsureRuntimeText(rect, "Label", new Vector2(size.x - 110f, size.y - 20f), new Vector2(52f, 0f), true);
            label.text = labelText;
            SceneUILayoutHelper.StyleText(label, 22f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            label.color = Color.white;

            return toggle;
        }

        private void ApplyLayout()
        {
            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);
            if (canvas == null)
            {
                return;
            }

            SceneUILayoutHelper.EnsureFullscreenImage(canvas.transform, "RuntimeMapBackdrop", new Color(0.06f, 0.1f, 0.16f, 1f), preserveExistingLayout: true);
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeMapBoardCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 28f), new Vector2(980f, 1100f), new Color(0.12f, 0.18f, 0.28f, 0.94f), true);

            var legacyTopCard = canvas.transform.Find("RuntimeMapTopCard");
            if (legacyTopCard != null)
            {
                legacyTopCard.gameObject.SetActive(false);
            }

            var legacyDiceCard = canvas.transform.Find("RuntimeMapDiceCard");
            if (legacyDiceCard != null)
            {
                legacyDiceCard.gameObject.SetActive(false);
            }

            if (headerText != null && headerText.transform is RectTransform headerRect)
            {
                SceneUILayoutHelper.SetRect(headerRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(700f, 74f));
                SceneUILayoutHelper.StyleText(headerText, 54f, TextAlignmentOptions.Center, FontStyles.Bold);
                headerText.color = Color.white;
            }

            if (playerStatsText != null && playerStatsText.transform is RectTransform playerStatsRect)
            {
                SceneUILayoutHelper.SetRect(playerStatsRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -168f), new Vector2(400f, 126f));
                SceneUILayoutHelper.StyleText(playerStatsText, 22f, TextAlignmentOptions.TopLeft, FontStyles.Bold);
                playerStatsText.color = Color.white;
                playerStatsText.overflowMode = TextOverflowModes.Overflow;
            }

            if (unlockedSkillsText != null && unlockedSkillsText.transform is RectTransform unlockedSkillsRect)
            {
                SceneUILayoutHelper.SetRect(unlockedSkillsRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-64f, -168f), new Vector2(400f, 126f));
                SceneUILayoutHelper.StyleText(unlockedSkillsText, 20f, TextAlignmentOptions.TopRight, FontStyles.Bold);
                unlockedSkillsText.color = new Color(0.88f, 0.94f, 1f, 1f);
                unlockedSkillsText.overflowMode = TextOverflowModes.Overflow;
            }

            if (mapProgressText != null && mapProgressText.transform is RectTransform mapProgressRect)
            {
                SceneUILayoutHelper.SetRect(mapProgressRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -448f), new Vector2(860f, 150f));
                SceneUILayoutHelper.StyleText(mapProgressText, 18f, TextAlignmentOptions.TopLeft, FontStyles.Bold);
                mapProgressText.color = new Color(0.9f, 0.94f, 0.98f, 1f);
                mapProgressText.overflowMode = TextOverflowModes.Overflow;
            }

            if (diceText != null && diceText.transform is RectTransform diceRect)
            {
                SceneUILayoutHelper.SetRect(diceRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(900f, 240f));
                SceneUILayoutHelper.StyleText(diceText, 18f, TextAlignmentOptions.TopLeft);
                diceText.color = Color.white;
                diceText.overflowMode = TextOverflowModes.Overflow;
            }

            if (nodeButtonRoot != null)
            {
                nodeButtonRoot.gameObject.SetActive(false);
            }

            if (returnMenuButton != null && returnMenuButton.transform is RectTransform returnRect)
            {
                returnMenuButton.transform.SetParent(canvas.transform, false);
                SceneUILayoutHelper.SetRect(returnRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(52f, 72f), new Vector2(300f, 96f));
                SceneUILayoutHelper.StyleButton(returnMenuButton, new Vector2(300f, 96f), 24f, new Color(0.2f, 0.25f, 0.35f), Color.white);
            }

            if (debugRewardButton != null && debugRewardButton.transform is RectTransform debugRewardRect)
            {
                debugRewardButton.transform.SetParent(canvas.transform, false);
                SceneUILayoutHelper.SetRect(debugRewardRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-52f, 72f), new Vector2(300f, 96f));
                SceneUILayoutHelper.StyleButton(debugRewardButton, new Vector2(300f, 96f), 24f, new Color(0.19f, 0.63f, 0.38f), Color.white);
            }

            EnsureBattleOverlay();
        }

        private void ApplyLayoutIfEnabled()
        {
            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);
            if (useRuntimeLayout && !useEditorUiLayout)
            {
                ApplyLayout();
            }
        }

        private string BuildPlayerOverviewText()
        {
            var player = runManager != null ? runManager.PlayerState : null;
            if (player == null)
            {
                return "Player HP 92/100  Shield 8  Armor 3  Rage 2\nPosition Start";
            }

            var overview = player.IsBerserkActive
                ? $"Player HP {player.CurrentHp}/{player.MaxHp}  Shield {player.Shield}  Armor {player.Armor}  Rage {player.Rage}  Berserk {player.BerserkTurnsRemaining}t"
                : $"Player HP {player.CurrentHp}/{player.MaxHp}  Shield {player.Shield}  Armor {player.Armor}  Rage {player.Rage}";
            return $"{overview}\nPosition {BuildCurrentLocationLabel()}";
        }

        private string BuildCurrentLocationLabel()
        {
            if (runManager == null || runManager.CurrentMapNodeIndex < 0)
            {
                return "Start";
            }

            var currentNode = runManager.MapSystem.GetNode(runManager.CurrentMapNodeIndex);
            return currentNode?.Definition != null ? currentNode.Definition.DisplayName : "Start";
        }

        private string BuildMapStatusText()
        {
            if (runManager == null)
            {
                return "Preview mode\n\nCurrent Position: Start\nAvailable Routes:\n- [Battle] Slime Camp\n- [Reward] Supply Cache";
            }

            var builder = new StringBuilder();
            builder.AppendLine(runManager.LastRunMessage);
            builder.AppendLine();
            builder.AppendLine($"Current Position: {BuildCurrentLocationLabel()}");

            var availableNodes = runManager.MapSystem.GetAvailableNodes().OrderBy(node => node.GridPosition.y).ThenBy(node => node.GridPosition.x).ToList();
            if (availableNodes.Count == 0)
            {
                builder.Append("No route is currently available.");
                return builder.ToString();
            }

            builder.AppendLine("Available Routes:");
            foreach (var node in availableNodes)
            {
                builder.AppendLine($"- [{node.Definition.GetNodeTypeLabel()}] {node.Definition.DisplayName}");
            }

            return builder.ToString();
        }

        private Vector2Int GetCurrentPlayerGridPosition()
        {
            if (runManager == null || runManager.CurrentMapNodeIndex < 0)
            {
                return StartGridPosition;
            }

            var currentNode = runManager.MapSystem.GetNode(runManager.CurrentMapNodeIndex);
            return currentNode != null ? currentNode.GridPosition : StartGridPosition;
        }

        private static bool CanMoveToStart(Vector2Int currentPosition)
        {
            return currentPosition != StartGridPosition &&
                   Mathf.Abs(currentPosition.x - StartGridPosition.x) + Mathf.Abs(currentPosition.y - StartGridPosition.y) == 1;
        }

        private void LoadMapSpritesIfNeeded()
        {
            mapBoardSprite ??= Resources.LoadAll<Sprite>(MapSpriteResourcePath).FirstOrDefault();
            iconSpritesByName ??= Resources.LoadAll<Sprite>(MapIconResourcePath).GroupBy(sprite => sprite.name).ToDictionary(group => group.Key, group => group.First());
            playerMarkerSprite ??= Resources.LoadAll<Sprite>(PlayerIconResourcePath).FirstOrDefault();
        }

        private float ScaleValue(float value)
        {
            return value * Mathf.Max(0.5f, nodeVisualScale);
        }

        private Vector2 ScaleSize(float value)
        {
            var scaled = ScaleValue(value);
            return new Vector2(scaled, scaled);
        }

        private float ScaleFont(float value)
        {
            return Mathf.Max(12f, value * Mathf.Max(0.75f, nodeVisualScale));
        }

        private void ApplyBoardPlacement(RectTransform target)
        {
            if (target == null)
            {
                return;
            }

            if (boardPlacementReference == null)
            {
                SceneUILayoutHelper.SetRect(target, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 56f), new Vector2(820f, 900f));
                return;
            }

            target.anchorMin = boardPlacementReference.anchorMin;
            target.anchorMax = boardPlacementReference.anchorMax;
            target.pivot = boardPlacementReference.pivot;
            target.anchoredPosition = boardPlacementReference.anchoredPosition;
            target.sizeDelta = boardPlacementReference.sizeDelta;
            target.offsetMin = boardPlacementReference.offsetMin;
            target.offsetMax = boardPlacementReference.offsetMax;
            target.localScale = boardPlacementReference.localScale;
            target.localRotation = boardPlacementReference.localRotation;
        }

        private void CacheEditorAnchors()
        {
            editorAnchorMap.Clear();

            if (nodeAnchorRoot == null)
            {
                return;
            }

            foreach (var anchor in nodeAnchorRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (anchor == null || anchor == nodeAnchorRoot)
                {
                    continue;
                }

                var key = NormalizeAnchorKey(anchor.name);
                if (!string.IsNullOrEmpty(key) && !editorAnchorMap.ContainsKey(key))
                {
                    editorAnchorMap.Add(key, anchor);
                }
            }
        }

        private bool TryGetEditorAnchorPosition(Vector2Int gridPosition, out Vector2 anchoredPosition)
        {
            anchoredPosition = default;

            if (runtimeMapOverlayRoot == null)
            {
                return false;
            }

            if (gridPosition == StartGridPosition && TryGetAnchorLocalPosition("start", out anchoredPosition))
            {
                return true;
            }

            return TryGetAnchorLocalPosition($"{gridPosition.x}_{gridPosition.y}", out anchoredPosition);
        }

        private bool TryGetAnchorLocalPosition(string key, out Vector2 anchoredPosition)
        {
            anchoredPosition = default;

            if (string.IsNullOrEmpty(key) || !editorAnchorMap.TryGetValue(key, out var anchor) || anchor == null)
            {
                return false;
            }

            var localPosition = runtimeMapOverlayRoot.InverseTransformPoint(anchor.position);
            anchoredPosition = new Vector2(localPosition.x, localPosition.y);
            return true;
        }

        private static string NormalizeAnchorKey(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return null;
            }

            var lowered = rawName.Trim().ToLowerInvariant();
            if (lowered == "start")
            {
                return "start";
            }

            if (lowered.StartsWith("node_"))
            {
                lowered = lowered.Substring(5);
            }

            lowered = lowered.Replace("-", "_").Replace(" ", string.Empty);
            var parts = lowered.Split('_');
            if (parts.Length != 2)
            {
                return null;
            }

            return int.TryParse(parts[0], out var x) && int.TryParse(parts[1], out var y)
                ? $"{x}_{y}"
                : null;
        }

        private Sprite ResolveIconSprite(MapNodeType nodeType)
        {
            return nodeType switch
            {
                MapNodeType.Boss => ResolveSprite(BossSpriteName),
                MapNodeType.Reward => ResolveSprite(RewardSpriteName),
                MapNodeType.Shop => ResolveSprite(ShopSpriteName),
                _ => ResolveSprite(BattleSpriteName)
            };
        }

        private Sprite ResolveSprite(string spriteName)
        {
            LoadMapSpritesIfNeeded();
            return iconSpritesByName != null && iconSpritesByName.TryGetValue(spriteName, out var sprite) ? sprite : null;
        }

        private void DisableDuplicateMapImages(Canvas canvas, Image boardImage)
        {
            if (canvas == null || boardImage == null || mapBoardSprite == null)
            {
                return;
            }

            foreach (var image in canvas.GetComponentsInChildren<Image>(true))
            {
                if (image == null || image == boardImage)
                {
                    continue;
                }

                if (image.sprite != null && image.sprite.name == mapBoardSprite.name)
                {
                    image.enabled = false;
                }
            }
        }

        private static Image EnsureRuntimeImage(Transform parent, string objectName, Vector2 size, Vector2 anchoredPosition, bool preserveExistingLayout = false)
        {
            var rectTransform = SceneUILayoutHelper.EnsureRuntimeRect(parent, objectName);
            SceneUILayoutHelper.SetRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size, preserveExistingLayout);
            return rectTransform.GetComponent<Image>() ?? rectTransform.gameObject.AddComponent<Image>();
        }

        private TMP_Text EnsureRuntimeText(Transform parent, string objectName, Vector2 size, Vector2 anchoredPosition, bool preserveExistingLayout = false)
        {
            var rectTransform = SceneUILayoutHelper.EnsureRuntimeRect(parent, objectName);
            SceneUILayoutHelper.SetRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size, preserveExistingLayout);
            var text = rectTransform.GetComponent<TextMeshProUGUI>() ?? rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
            if (headerText != null)
            {
                text.font = headerText.font;
                text.fontSharedMaterial = headerText.fontSharedMaterial;
            }

            text.enableAutoSizing = false;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private Button EnsureTransparentButton(Transform parent, string objectName, Vector2 size, Vector2 anchoredPosition, bool preserveExistingLayout = false)
        {
            var rectTransform = SceneUILayoutHelper.EnsureRuntimeRect(parent, objectName);
            SceneUILayoutHelper.SetRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size, preserveExistingLayout);

            var image = rectTransform.GetComponent<Image>() ?? rectTransform.gameObject.AddComponent<Image>();
            image.sprite = GetRuntimeUiSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, 0.01f);
            image.raycastTarget = true;

            var button = rectTransform.GetComponent<Button>() ?? rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            return button;
        }

        private Button EnsureDiceInspectSlotButton(Transform canvasRoot, RectTransform panelRoot, string objectName, Vector2 size, Vector2 anchoredPosition)
        {
            var rectTransform = FindRuntimeRect(canvasRoot, objectName);
            var created = false;
            if (rectTransform == null)
            {
                rectTransform = FindRuntimeRect(panelRoot, objectName);
                if (rectTransform != null)
                {
                    rectTransform.SetParent(canvasRoot, true);
                }
            }

            if (rectTransform == null)
            {
                rectTransform = SceneUILayoutHelper.EnsureRuntimeRect(canvasRoot, objectName);
                created = true;
            }

            var effectiveSize = ResolveRectSizeWithMinimum(rectTransform, size, size * 0.5f);
            if (created)
            {
                SceneUILayoutHelper.SetRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, effectiveSize);
            }
            else
            {
                SceneUILayoutHelper.SetRect(
                    rectTransform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    rectTransform.anchoredPosition,
                    effectiveSize);
            }

            var image = rectTransform.GetComponent<Image>() ?? rectTransform.gameObject.AddComponent<Image>();
            image.sprite = GetRuntimeUiSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, 0.01f);
            image.raycastTarget = true;

            var button = rectTransform.GetComponent<Button>() ?? rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            return button;
        }

        private static RectTransform FindRuntimeRect(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                if (parent.GetChild(index) is RectTransform rectTransform && rectTransform.name == objectName)
                {
                    return rectTransform;
                }
            }

            return null;
        }

        private static Vector2 ResolveRectSize(RectTransform rectTransform, Vector2 fallbackSize)
        {
            if (rectTransform == null)
            {
                return fallbackSize;
            }

            var rectSize = rectTransform.rect.size;
            if (rectSize.x > 0.01f && rectSize.y > 0.01f)
            {
                return rectSize;
            }

            return rectTransform.sizeDelta.x > 0.01f && rectTransform.sizeDelta.y > 0.01f
                ? rectTransform.sizeDelta
                : fallbackSize;
        }

        private static Vector2 ResolveRectSizeWithMinimum(RectTransform rectTransform, Vector2 fallbackSize, Vector2 minimumSize)
        {
            var resolvedSize = ResolveRectSize(rectTransform, fallbackSize);
            return new Vector2(
                resolvedSize.x >= minimumSize.x ? resolvedSize.x : fallbackSize.x,
                resolvedSize.y >= minimumSize.y ? resolvedSize.y : fallbackSize.y);
        }

        private static void SyncRuntimeFrameLayoutFromRect(RuntimeFrameLayoutSettings layout, RectTransform rectTransform)
        {
            if (layout == null || rectTransform == null)
            {
                return;
            }

            layout.AnchoredPosition = rectTransform.anchoredPosition;
            layout.FixedSize = ResolveRectSize(rectTransform, layout.FixedSize);
            layout.UseNativeSize = false;
        }

        private static void SetDiceInspectSlotButtonsVisible(DiceInspectSlotView[] slotViews, bool isVisible)
        {
            if (slotViews == null)
            {
                return;
            }

            for (var index = 0; index < slotViews.Length; index++)
            {
                var button = slotViews[index]?.Button;
                if (button != null && button.gameObject.activeSelf != isVisible)
                {
                    button.gameObject.SetActive(isVisible);
                }
            }
        }

        private static void BindTunerButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static Vector2[] GetDiceInspectSlotPositions(Vector2 panelSize)
        {
            return new[]
            {
                new Vector2(-panelSize.x * 0.31f, panelSize.y * 0.19f),
                new Vector2(0f, panelSize.y * 0.26f),
                new Vector2(panelSize.x * 0.31f, panelSize.y * 0.19f),
                new Vector2(-panelSize.x * 0.31f, -panelSize.y * 0.26f),
                new Vector2(0f, -panelSize.y * 0.33f),
                new Vector2(panelSize.x * 0.31f, -panelSize.y * 0.26f)
            };
        }

        private static void SetLegacyElementVisible(Object target, bool isVisible)
        {
            if (target is Component component && component.gameObject.activeSelf != isVisible)
            {
                component.gameObject.SetActive(isVisible);
            }
        }

        private static Vector2 ResolveRuntimeFrameSize(Sprite sprite, RuntimeFrameLayoutSettings layout)
        {
            if (layout == null)
            {
                return sprite != null ? sprite.rect.size : Vector2.zero;
            }

            return layout.UseNativeSize && sprite != null
                ? UiFrameSpriteLibrary.ResolveScaledSize(sprite, layout.SpriteScale, layout.ExtraSize)
                : layout.FixedSize;
        }

        private void ApplyEditorPreviewSkillCard(RuntimeSkillCardView card, int faceIndex)
        {
            if (card == null)
            {
                return;
            }

            var clampedIndex = Mathf.Clamp(faceIndex, 0, EditorPreviewSkillIds.Length - 1);
            var skillId = EditorPreviewSkillIds[clampedIndex];
            if (card.IconImage != null)
            {
                card.IconImage.sprite = SkillIconLibrary.GetSkillIcon(skillId);
                card.IconImage.enabled = card.IconImage.sprite != null;
                card.IconImage.preserveAspect = true;
            }

            if (card.TitleText != null)
            {
                card.TitleText.text = $"Face {clampedIndex + 1} | {GetEditorPreviewSkillTitle(clampedIndex)}";
            }

            if (card.BodyText != null)
            {
                card.BodyText.text = GetEditorPreviewSkillBody(clampedIndex);
            }
        }

        private void PopulateEditorBattleOverlayPreview()
        {
            if (battleOverlayView == null)
            {
                return;
            }

            battleOverlayView.TurnText.text = "Turn 3";
            battleOverlayView.ActingUnitText.text = "Acting: Dice Knight";
            battleOverlayView.TurnQueueText.text = "Turn Queue\nDice Knight -> Slime A -> Slime B";
            battleOverlayView.PlayerStatsText.text = "Player\nHP 92/100\nShield 8\nArmor 3\nRage 2";
            battleOverlayView.EnemyStatsText.text = "Enemies\nSlime A  HP 14/14\nShield 0 | Armor 0 | Rage 0\nSlime B  HP 14/14\nShield 0 | Armor 0 | Rage 0";
            battleOverlayView.RollLogText.text = string.Empty;
            battleOverlayView.BattleLogText.text = string.Empty;
            battleOverlayView.ResultText.text = string.Empty;
            ApplyEditorPreviewBattleSkillCard(battleOverlayView.ActiveSkillCard, "basic_attack", "Player Skill | Basic Attack", "Damage 6\nDeal damage. Upgrade: +3 damage.");
        }

        private static void ApplyEditorPreviewBattleSkillCard(RuntimeSkillCardView card, string skillId, string title, string body)
        {
            if (card == null)
            {
                return;
            }

            if (card.IconImage != null)
            {
                card.IconImage.sprite = SkillIconLibrary.GetSkillIcon(skillId);
                card.IconImage.enabled = card.IconImage.sprite != null;
                card.IconImage.preserveAspect = true;
            }

            if (card.TitleText != null)
            {
                card.TitleText.text = title;
            }

            if (card.BodyText != null)
            {
                card.BodyText.text = body;
            }
        }

        private static string GetEditorPreviewSkillTitle(int index)
        {
            return index switch
            {
                0 => "Basic Attack",
                1 => "Defensive Stance",
                2 => "Focused Defense",
                3 => "Counter",
                4 => "Shield Burst",
                5 => "Blood Slash",
                _ => "Skill"
            };
        }

        private static string GetEditorPreviewSkillBody(int index)
        {
            return index switch
            {
                0 => "Damage 6\nDeal damage. Upgrade: +3 damage.",
                1 => "Shield +10 / Armor +3\nGain shield and armor.",
                2 => "Shield +8 / Next turn Shield +8\nStack defense across turns.",
                3 => "Shield ratio 50%\nDeal damage based on current shield.",
                4 => "Shield ratio 60% / Consume all Shield\nBurst all enemies with stored shield.",
                5 => "Damage 8 / Self HP -4 / Rage +2\nTrade health for pressure.",
                _ => "Preview skill description."
            };
        }

        private static Vector2 FitSizeToCanvas(Vector2 requestedSize, RectTransform canvasRect, Vector2 maxCanvasFraction, Vector2 padding)
        {
            if (canvasRect == null || requestedSize.x <= 0f || requestedSize.y <= 0f)
            {
                return requestedSize;
            }

            var availableWidth = Mathf.Max(120f, (canvasRect.rect.width * Mathf.Clamp01(maxCanvasFraction.x)) - (padding.x * 2f));
            var availableHeight = Mathf.Max(120f, (canvasRect.rect.height * Mathf.Clamp01(maxCanvasFraction.y)) - (padding.y * 2f));
            var scaleFactor = Mathf.Min(1f, availableWidth / requestedSize.x, availableHeight / requestedSize.y);
            return requestedSize * scaleFactor;
        }

        private static void ClampRectToCanvas(RectTransform rectTransform, RectTransform canvasRect, Vector2 padding)
        {
            if (rectTransform == null || canvasRect == null)
            {
                return;
            }

            if ((rectTransform.anchorMax - rectTransform.anchorMin).sqrMagnitude > 0.0001f)
            {
                return;
            }

            var canvasBounds = canvasRect.rect;
            var size = rectTransform.sizeDelta;
            var anchor = rectTransform.anchorMin;
            var anchorReference = new Vector2(
                Mathf.Lerp(canvasBounds.xMin, canvasBounds.xMax, anchor.x),
                Mathf.Lerp(canvasBounds.yMin, canvasBounds.yMax, anchor.y));

            var pivot = rectTransform.pivot;
            var pivotPosition = anchorReference + rectTransform.anchoredPosition;

            var minPivotX = canvasBounds.xMin + padding.x + (pivot.x * size.x);
            var maxPivotX = canvasBounds.xMax - padding.x - ((1f - pivot.x) * size.x);
            var minPivotY = canvasBounds.yMin + padding.y + (pivot.y * size.y);
            var maxPivotY = canvasBounds.yMax - padding.y - ((1f - pivot.y) * size.y);

            if (minPivotX > maxPivotX)
            {
                minPivotX = maxPivotX = canvasBounds.center.x;
            }

            if (minPivotY > maxPivotY)
            {
                minPivotY = maxPivotY = canvasBounds.center.y;
            }

            pivotPosition.x = Mathf.Clamp(pivotPosition.x, minPivotX, maxPivotX);
            pivotPosition.y = Mathf.Clamp(pivotPosition.y, minPivotY, maxPivotY);
            rectTransform.anchoredPosition = pivotPosition - anchorReference;
        }

        private string BuildRuntimeTopHudBodyText()
        {
            var player = runManager?.PlayerState;
            if (player == null)
            {
                return "HP 92/100   Shield 8   Armor 3   Rage 2\nLocation: Start   Routes: 2   Preview layout mode";
            }

            var availableRoutes = runManager.MapSystem.GetAvailableNodes().Count;
            return
                $"HP {player.CurrentHp}/{player.MaxHp}   Shield {player.Shield}   Armor {player.Armor}   Rage {player.Rage}\n" +
                $"Location: {BuildCurrentLocationLabel()}   Routes: {availableRoutes}   {runManager.LastRunMessage}";
        }

        private Vector2 GetSelectedRuntimeUiTargetPosition()
        {
            return runtimeUiTunerTarget switch
            {
                RuntimeUiTunerTarget.MapTopHud => mapTopHudLayout.AnchoredPosition,
                RuntimeUiTunerTarget.DicePanel => diceInspectPanelLayout.AnchoredPosition,
                RuntimeUiTunerTarget.DiceDetail => diceInspectDetailLayout.AnchoredPosition,
                RuntimeUiTunerTarget.BattlePlayerCard => battleOverlayLayout.PlayerSkillCardPosition,
                RuntimeUiTunerTarget.BattleEnemyCard => battleOverlayLayout.EnemySkillCardPosition,
                RuntimeUiTunerTarget.Battlefield => battleOverlayLayout.FieldPanelAnchoredPosition,
                _ => Vector2.zero
            };
        }

        private Vector2 GetSelectedRuntimeUiTargetScale()
        {
            return runtimeUiTunerTarget switch
            {
                RuntimeUiTunerTarget.MapTopHud => mapTopHudLayout.SpriteScale,
                RuntimeUiTunerTarget.DicePanel => diceInspectPanelLayout.SpriteScale,
                RuntimeUiTunerTarget.DiceDetail => diceInspectDetailLayout.SpriteScale,
                RuntimeUiTunerTarget.BattlePlayerCard => battleOverlayLayout.SkillCardFrame.SpriteScale,
                RuntimeUiTunerTarget.BattleEnemyCard => battleOverlayLayout.SkillCardFrame.SpriteScale,
                RuntimeUiTunerTarget.Battlefield => battleOverlayLayout.BattlefieldSpriteScale,
                _ => Vector2.one
            };
        }

        private static void AdjustRuntimeFrame(RuntimeFrameLayoutSettings layout, float deltaX, float deltaY, float deltaScale)
        {
            if (layout == null)
            {
                return;
            }

            layout.AnchoredPosition += new Vector2(deltaX, deltaY);
            AdjustRuntimeFrameScale(layout, deltaScale);
        }

        private static void AdjustRuntimeFrameScale(RuntimeFrameLayoutSettings layout, float deltaScale)
        {
            if (layout == null || Mathf.Abs(deltaScale) <= Mathf.Epsilon)
            {
                return;
            }

            layout.SpriteScale += new Vector2(deltaScale, deltaScale);
            layout.SpriteScale = new Vector2(Mathf.Max(0.05f, layout.SpriteScale.x), Mathf.Max(0.05f, layout.SpriteScale.y));
        }

        private void CacheRuntimeBattleAuthoringSignatures()
        {
            lastBattleOverlayLayoutSignature = GetBattleOverlayLayoutSignature();
            lastBattleHitEffectSignature = GetBattleHitEffectSignature();
            lastRuntimeUiLayoutSignature = GetRuntimeUiLayoutSignature();
        }

        private int GetBattleOverlayLayoutSignature()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + battleOverlayLayout.FieldPanelAnchoredPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.FieldPanelSize.GetHashCode();
                hash = (hash * 31) + (battleOverlayLayout.UseBattlefieldSpriteNativeSize ? 1 : 0);
                hash = (hash * 31) + battleOverlayLayout.BattlefieldSpriteScale.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.BattlefieldSpriteExtraSize.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.LogPanelAnchoredPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.LogPanelSize.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.TurnTextPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.ActingUnitTextPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.TurnQueueTextPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.PlayerStatsTextPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.EnemyStatsTextPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.ResultTextPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.RollLogTextPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.BattleLogTextPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.PlayerSkillCardPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.EnemySkillCardPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.SkillCardFrame.AnchoredPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.SkillCardFrame.FixedSize.GetHashCode();
                hash = (hash * 31) + (battleOverlayLayout.SkillCardFrame.UseNativeSize ? 1 : 0);
                hash = (hash * 31) + battleOverlayLayout.SkillCardFrame.SpriteScale.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.SkillCardFrame.ExtraSize.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.RollButtonPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.ContinueButtonPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.BackButtonPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.AutoTogglePosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.PlayerUnitPosition.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.EnemyUnitPosition01.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.EnemyUnitPosition02.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.EnemyUnitPosition03.GetHashCode();
                hash = (hash * 31) + battleOverlayLayout.CombatantViewSize.GetHashCode();
                return hash;
            }
        }

        private int GetRuntimeUiLayoutSignature()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + mapTopHudLayout.AnchoredPosition.GetHashCode();
                hash = (hash * 31) + mapTopHudLayout.FixedSize.GetHashCode();
                hash = (hash * 31) + (mapTopHudLayout.UseNativeSize ? 1 : 0);
                hash = (hash * 31) + mapTopHudLayout.SpriteScale.GetHashCode();
                hash = (hash * 31) + mapTopHudLayout.ExtraSize.GetHashCode();
                hash = (hash * 31) + diceInspectPanelLayout.AnchoredPosition.GetHashCode();
                hash = (hash * 31) + diceInspectPanelLayout.FixedSize.GetHashCode();
                hash = (hash * 31) + (diceInspectPanelLayout.UseNativeSize ? 1 : 0);
                hash = (hash * 31) + diceInspectPanelLayout.SpriteScale.GetHashCode();
                hash = (hash * 31) + diceInspectPanelLayout.ExtraSize.GetHashCode();
                hash = (hash * 31) + diceInspectDetailLayout.AnchoredPosition.GetHashCode();
                hash = (hash * 31) + diceInspectDetailLayout.FixedSize.GetHashCode();
                hash = (hash * 31) + (diceInspectDetailLayout.UseNativeSize ? 1 : 0);
                hash = (hash * 31) + diceInspectDetailLayout.SpriteScale.GetHashCode();
                hash = (hash * 31) + diceInspectDetailLayout.ExtraSize.GetHashCode();
                hash = (hash * 31) + (runtimeUiTunerSettings.ShowRuntimeTuner ? 1 : 0);
                hash = (hash * 31) + runtimeUiTunerSettings.AnchoredPosition.GetHashCode();
                hash = (hash * 31) + runtimeUiTunerSettings.PanelSize.GetHashCode();
                hash = (hash * 31) + runtimeUiTunerSettings.PositionStep.GetHashCode();
                hash = (hash * 31) + runtimeUiTunerSettings.ScaleStep.GetHashCode();
                return hash;
            }
        }

        private int GetBattleHitEffectSignature()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + (battleHitEffectSettings.prefab != null ? battleHitEffectSettings.prefab.GetInstanceID() : 0);
                hash = (hash * 31) + battleHitEffectSettings.localScale.GetHashCode();
                hash = (hash * 31) + battleHitEffectSettings.impactLerpFromActorToTarget.GetHashCode();
                hash = (hash * 31) + battleHitEffectSettings.screenOffset.GetHashCode();
                hash = (hash * 31) + battleHitEffectSettings.depthOffsetTowardsCamera.GetHashCode();
                hash = (hash * 31) + battleHitEffectSettings.durationOverride.GetHashCode();
                return hash;
            }
        }

        private Vector2 ResolveBattlefieldPanelSize(Sprite battlefieldSprite)
        {
            if (battleOverlayLayout.UseBattlefieldSpriteNativeSize && battlefieldSprite != null)
            {
                return Vector2.Scale(battlefieldSprite.rect.size, battleOverlayLayout.BattlefieldSpriteScale) + battleOverlayLayout.BattlefieldSpriteExtraSize;
            }

            return battleOverlayLayout.FieldPanelSize;
        }

        private static Sprite GetRuntimeUiSprite()
        {
            if (runtimeUiSprite == null)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texture.Apply();
                runtimeUiSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                runtimeUiSprite.name = "RuntimeMapUiSprite";
                runtimeUiSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return runtimeUiSprite;
        }

        private static void EnsureCloudCanvasGroup(MapNodeView view)
        {
            if (view == null || view.CloudImage == null)
            {
                return;
            }

            if (view.CloudCanvasGroup == null)
            {
                view.CloudCanvasGroup = view.CloudImage.GetComponent<CanvasGroup>();
                if (view.CloudCanvasGroup == null)
                {
                    view.CloudCanvasGroup = view.CloudImage.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private static void ApplyNativeSpriteLayout(Image image, Sprite sprite, Vector2 anchoredPosition, float scaleMultiplier)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.preserveAspect = true;

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localRotation = Quaternion.identity;

            if (sprite == null)
            {
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.localScale = Vector3.one;
                return;
            }

            image.SetNativeSize();
            rectTransform.localScale = Vector3.one * Mathf.Max(0.1f, scaleMultiplier);
        }

        private static Sprite GetCenteredSprite(Sprite sourceSprite)
        {
            if (sourceSprite == null)
            {
                return null;
            }

            var key = sourceSprite.GetInstanceID();
            if (CenteredSpriteCache.TryGetValue(key, out var centeredSprite) && centeredSprite != null)
            {
                return centeredSprite;
            }

            centeredSprite = Sprite.Create(
                sourceSprite.texture,
                sourceSprite.rect,
                new Vector2(0.5f, 0.5f),
                sourceSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            centeredSprite.name = $"{sourceSprite.name}_Centered";
            centeredSprite.hideFlags = HideFlags.HideAndDontSave;
            CenteredSpriteCache[key] = centeredSprite;
            return centeredSprite;
        }

        private static Color ResolveIconColor(MapNodeRuntimeState node, bool isCurrentPosition)
        {
            var color = node.Definition.NodeType == MapNodeType.EliteBattle ? new Color(1f, 0.93f, 0.82f, 1f) : Color.white;
            if (isCurrentPosition || node.IsUnlocked)
            {
                return color;
            }

            color.a = node.IsCompleted ? 0.86f : 0.72f;
            return color;
        }

        private static Color ResolveHighlightColor(MapNodeRuntimeState node, bool isCurrentPosition)
        {
            if (isCurrentPosition) return new Color(0.16f, 0.67f, 0.97f, 0.4f);
            if (node.IsUnlocked) return new Color(1f, 0.91f, 0.46f, 0.24f);
            if (node.IsCompleted) return new Color(0.22f, 0.74f, 0.46f, 0.18f);
            if (node.Definition.NodeType == MapNodeType.Boss && node.IsRevealed) return new Color(0.98f, 0.34f, 0.24f, 0.16f);
            return new Color(1f, 1f, 1f, 0f);
        }

        private static string ResolveCaptionText(MapNodeRuntimeState node, bool isCurrentPosition)
        {
            if (isCurrentPosition) return node.Definition.GetNodeTypeLabel().ToUpperInvariant();
            return node.IsCompleted ? "CLEARED" : node.Definition.GetNodeTypeLabel().ToUpperInvariant();
        }

        private static Color ResolveCaptionColor(MapNodeRuntimeState node, bool isCurrentPosition)
        {
            if (isCurrentPosition) return Color.white;
            if (node.IsUnlocked) return new Color(1f, 0.97f, 0.82f, 1f);
            if (node.IsCompleted) return new Color(0.75f, 0.96f, 0.82f, 1f);
            return new Color(0.84f, 0.88f, 0.92f, 1f);
        }

        private sealed class MapNodeView
        {
            public int NodeIndex;
            public RectTransform Root;
            public Button Button;
            public Image HighlightImage;
            public Image IconImage;
            public Image CloudImage;
            public CanvasGroup CloudCanvasGroup;
            public Image MarkerBadge;
            public TMP_Text MarkerText;
            public TMP_Text CaptionText;
        }

        [System.Serializable]
        private sealed class RuntimeFrameLayoutSettings
        {
            public Vector2 AnchoredPosition = Vector2.zero;
            public Vector2 FixedSize = new Vector2(400f, 160f);
            public bool UseNativeSize = true;
            public Vector2 SpriteScale = new Vector2(0.25f, 0.25f);
            public Vector2 ExtraSize = Vector2.zero;
        }

        [System.Serializable]
        private sealed class RuntimeUiTunerSettings
        {
            public bool ShowRuntimeTuner = false;
            public Vector2 AnchoredPosition = new Vector2(28f, 240f);
            public Vector2 PanelSize = new Vector2(240f, 220f);
            public float PositionStep = 16f;
            public float ScaleStep = 0.02f;
        }

        private enum RuntimeUiTunerTarget
        {
            MapTopHud,
            DicePanel,
            DiceDetail,
            BattlePlayerCard,
            BattleEnemyCard,
            Battlefield
        }

        [System.Serializable]
        private sealed class BattleOverlayLayoutSettings
        {
            public Vector2 FieldPanelAnchoredPosition = new Vector2(0f, -18f);
            public Vector2 FieldPanelSize = new Vector2(1120f, 980f);
            public bool UseBattlefieldSpriteNativeSize = true;
            public Vector2 BattlefieldSpriteScale = new Vector2(0.46f, 0.46f);
            public Vector2 BattlefieldSpriteExtraSize = new Vector2(0f, 220f);
            public Vector2 LogPanelAnchoredPosition = new Vector2(0f, 128f);
            public Vector2 LogPanelSize = new Vector2(980f, 250f);
            public Vector2 TurnTextPosition = new Vector2(0f, 320f);
            public Vector2 ActingUnitTextPosition = new Vector2(0f, 276f);
            public Vector2 TurnQueueTextPosition = new Vector2(0f, 224f);
            public Vector2 PlayerStatsTextPosition = new Vector2(-290f, 232f);
            public Vector2 EnemyStatsTextPosition = new Vector2(290f, 232f);
            public Vector2 ResultTextPosition = new Vector2(0f, 8f);
            public Vector2 RollLogTextPosition = new Vector2(0f, 74f);
            public Vector2 BattleLogTextPosition = new Vector2(0f, -2f);
            public Vector2 PlayerSkillCardPosition = new Vector2(-240f, 78f);
            public Vector2 EnemySkillCardPosition = new Vector2(240f, 78f);
            public RuntimeFrameLayoutSettings SkillCardFrame = new RuntimeFrameLayoutSettings
            {
                AnchoredPosition = Vector2.zero,
                UseNativeSize = true,
                SpriteScale = new Vector2(0.22f, 0.22f),
                ExtraSize = Vector2.zero,
                FixedSize = new Vector2(420f, 160f)
            };
            public Vector2 RollButtonPosition = new Vector2(256f, -68f);
            public Vector2 ContinueButtonPosition = new Vector2(256f, -68f);
            public Vector2 BackButtonPosition = new Vector2(-16f, -68f);
            public Vector2 AutoTogglePosition = new Vector2(-278f, -66f);
            public Vector2 PlayerUnitPosition = new Vector2(-270f, -148f);
            public Vector2 EnemyUnitPosition01 = new Vector2(210f, -8f);
            public Vector2 EnemyUnitPosition02 = new Vector2(364f, 126f);
            public Vector2 EnemyUnitPosition03 = new Vector2(364f, -134f);
            public Vector2 CombatantViewSize = new Vector2(280f, 360f);
        }

        private sealed class PlayerMarkerView
        {
            public RectTransform Root;
            public Image BadgeImage;
            public TMP_Text MarkerText;
            public Vector2 Offset;
        }

        private sealed class MapTopHudView
        {
            public RectTransform Root;
            public Image PortraitImage;
            public TMP_Text TitleText;
            public TMP_Text BodyText;
            public Button DiceButton;
        }

        private sealed class DiceInspectSlotView
        {
            public Button Button;
            public Image HighlightImage;
            public Image IconImage;
            public TMP_Text LabelText;
        }

        private sealed class DiceInspectView
        {
            public RectTransform PanelRoot;
            public RuntimeSkillCardView DetailCard;
            public DiceInspectSlotView[] SlotViews;
        }

        private sealed class RuntimeUiTunerView
        {
            public RectTransform Root;
            public TMP_Text TitleText;
            public TMP_Text InfoText;
        }

        private sealed class BattleOverlayView
        {
            public RectTransform Root;
            public RectTransform TopCard;
            public RectTransform BottomCard;
            public RectTransform PresentationRoot;
            public TMP_Text TurnText;
            public TMP_Text ActingUnitText;
            public TMP_Text TurnQueueText;
            public TMP_Text PlayerStatsText;
            public TMP_Text EnemyStatsText;
            public TMP_Text RollLogText;
            public TMP_Text BattleLogText;
            public TMP_Text ResultText;
            public RuntimeSkillCardView ActiveSkillCard;
            public Button RollButton;
            public Button ContinueButton;
            public Button BackButton;
            public Toggle AutoToggle;
        }
    }
}
