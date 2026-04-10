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
    public class MapSceneController : MonoBehaviour
    {
        private static readonly Vector2Int StartGridPosition = new Vector2Int(0, 0);
        private static readonly float[] ColumnAnchors = { 0.14f, 0.38f, 0.62f, 0.86f };
        private static readonly float[] RowAnchors = { 0.16f, 0.39f, 0.62f, 0.85f };
        private const string MapSpriteResourcePath = "map";
        private const string MapIconResourcePath = "map_icon";
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

        private readonly Dictionary<int, MapNodeView> nodeViews = new Dictionary<int, MapNodeView>();
        private readonly Dictionary<string, RectTransform> editorAnchorMap = new Dictionary<string, RectTransform>();

        private GameRunManager runManager;
        private RectTransform runtimeMapBoardRoot;
        private RectTransform runtimeMapOverlayRoot;
        private MapNodeView startNodeView;
        private PlayerMarkerView playerMarkerView;
        private Sprite mapBoardSprite;
        private Dictionary<string, Sprite> iconSpritesByName;
        private bool isNodeTransitionPlaying;

        private void Awake()
        {
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
            ApplyLayoutIfEnabled();
            Render();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
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
                RenderMapBoard();
                HideLegacyNodeButtons();
            }
            else
            {
                RenderLegacyNodeButtons();
            }

            SceneUILayoutHelper.SetButtonLabel(returnMenuButton, "Main Menu");
            SceneUILayoutHelper.SetButtonLabel(debugRewardButton, "Debug Reward");
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
        }

        private void RenderMapBoard()
        {
            if (!useRuntimeLayout || runtimeMapBoardRoot == null || runtimeMapOverlayRoot == null)
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
            view.Button.interactable = canMoveToStart && !isNodeTransitionPlaying;
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
            view.Button.interactable = node.IsUnlocked && !isNodeTransitionPlaying;
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
            if (isNodeTransitionPlaying)
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
            if (isNodeTransitionPlaying || !CanMoveToStart(GetCurrentPlayerGridPosition()))
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

        private IEnumerator PlayEncounterNodeTransition(MapNodeRuntimeState targetNode)
        {
            if (targetNode == null)
            {
                yield break;
            }

            yield return PlayPlayerMarkerMotion(GetCurrentPlayerGridPosition(), targetNode.GridPosition);
            runManager.SelectMapNode(targetNode.Index);
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
            var markerSize = new Vector2(ScaleValue(76f), ScaleValue(28f));
            var markerOffset = new Vector2(0f, ScaleValue(54f));

            var root = SceneUILayoutHelper.EnsureRuntimeRect(runtimeMapOverlayRoot, "RuntimeMapPlayerMarker");
            SceneUILayoutHelper.SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, markerSize);

            var badgeImage = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            badgeImage.sprite = GetRuntimeUiSprite();
            badgeImage.color = new Color(0.12f, 0.55f, 0.96f, 0.96f);
            badgeImage.raycastTarget = false;

            var markerText = EnsureRuntimeText(root, "MarkerText", markerSize, Vector2.zero);
            markerText.fontSize = ScaleFont(18f);
            markerText.alignment = TextAlignmentOptions.Center;
            markerText.fontStyle = FontStyles.Bold;
            markerText.color = Color.white;
            markerText.text = "YOU";

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
            playerMarkerView.Root.SetAsLastSibling();
        }

        private Vector2 GetMarkerTargetPosition(Vector2Int gridPosition)
        {
            playerMarkerView ??= CreatePlayerMarkerView();
            var offset = playerMarkerView != null ? playerMarkerView.Offset : new Vector2(0f, ScaleValue(54f));
            return GetBoardLocalPosition(gridPosition) + offset;
        }

        private void SetNodeInteractionEnabled(bool isEnabled)
        {
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

        private void ApplyLayout()
        {
            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);
            if (canvas == null)
            {
                return;
            }

            SceneUILayoutHelper.EnsureFullscreenImage(canvas.transform, "RuntimeMapBackdrop", new Color(0.06f, 0.1f, 0.16f, 1f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeMapTopCard", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(980f, 280f), new Color(0.1f, 0.16f, 0.25f, 0.95f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeMapBoardCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 28f), new Vector2(980f, 1100f), new Color(0.12f, 0.18f, 0.28f, 0.94f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeMapDiceCard", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 210f), new Vector2(980f, 300f), new Color(0.1f, 0.16f, 0.25f, 0.95f));

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
                return "Player";
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

        private static Image EnsureRuntimeImage(Transform parent, string objectName, Vector2 size, Vector2 anchoredPosition)
        {
            var rectTransform = SceneUILayoutHelper.EnsureRuntimeRect(parent, objectName);
            SceneUILayoutHelper.SetRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);
            return rectTransform.GetComponent<Image>() ?? rectTransform.gameObject.AddComponent<Image>();
        }

        private TMP_Text EnsureRuntimeText(Transform parent, string objectName, Vector2 size, Vector2 anchoredPosition)
        {
            var rectTransform = SceneUILayoutHelper.EnsureRuntimeRect(parent, objectName);
            SceneUILayoutHelper.SetRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);
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

        private sealed class PlayerMarkerView
        {
            public RectTransform Root;
            public Image BadgeImage;
            public TMP_Text MarkerText;
            public Vector2 Offset;
        }
    }
}
