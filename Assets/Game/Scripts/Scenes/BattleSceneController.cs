using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiceRogue
{
    public class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private bool useRuntimeLayout = true;
        [SerializeField] private UIStateController stateController;
        [SerializeField] private string battleStateId = "Battle";
        [SerializeField] private string resultStateId = "Result";
        [SerializeField] private BattleHUD battleHud;
        [SerializeField] private BattlePresenter battlePresenter;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Toggle autoBattleToggle;
        [SerializeField] private Button rollButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button backToMenuButton;

        private static Sprite cachedUiSprite;
        private const float RuntimeTextScaleMultiplier = 1.5f;

        private GameRunManager runManager;
        private Coroutine autoBattleRoutine;
        private bool isResolvingPresentation;

        private void Awake()
        {
            UIInputSystemHelper.EnsureEventSystem();
            runManager = GameRunManager.EnsureInstance(runConfig);
            runManager.EnsureDebugRunForScene();

            if (runManager.BattleSystem == null)
            {
                var availableNodes = runManager.MapSystem.GetAvailableNodes();
                if (availableNodes.Count > 0)
                {
                    runManager.PrepareBattle(availableNodes[0].Index);
                }
            }

            EnsureRuntimePresentation();
            ApplySceneLayoutIfEnabled();

            battlePresenter?.BindBattle(runManager.BattleSystem);
            battleHud?.Refresh(runManager.BattleSystem);

            if (autoBattleToggle != null)
            {
                autoBattleToggle.isOn = runManager.AutoBattleEnabled;
                autoBattleToggle.onValueChanged.AddListener(OnAutoToggleChanged);
            }

            if (rollButton != null)
            {
                rollButton.onClick.AddListener(ResolveOneTurn);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(runManager.CompleteBattleAndAdvance);
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene(runManager.Config.MainMenuSceneName));
            }

            SceneUILayoutHelper.SetButtonLabel(rollButton, "굴리기");
            SceneUILayoutHelper.SetButtonLabel(continueButton, "다음 진행");
            SceneUILayoutHelper.SetButtonLabel(backToMenuButton, "메인으로");
        }

        private void OnEnable()
        {
            stateController?.Show(battleStateId);
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(runManager.BattleSystem != null && runManager.BattleSystem.IsFinished);
            }

            EnsureRuntimePresentation();
            ApplySceneLayoutIfEnabled();
            battlePresenter?.BindBattle(runManager.BattleSystem);
            battleHud?.Refresh(runManager.BattleSystem);
            RefreshAutoFlow();
        }

        private void OnDisable()
        {
            if (autoBattleRoutine != null)
            {
                StopCoroutine(autoBattleRoutine);
                autoBattleRoutine = null;
            }
        }

        private void OnAutoToggleChanged(bool isOn)
        {
            runManager.AutoBattleEnabled = isOn;
            RefreshAutoFlow();
        }

        private void RefreshAutoFlow()
        {
            var battleSystem = runManager != null ? runManager.BattleSystem : null;
            var isBattleFinished = battleSystem == null || battleSystem.IsFinished;

            if (rollButton != null)
            {
                rollButton.interactable = !runManager.AutoBattleEnabled && !isBattleFinished && !isResolvingPresentation;
            }

            if (runManager.AutoBattleEnabled && !isBattleFinished && !isResolvingPresentation)
            {
                if (autoBattleRoutine == null)
                {
                    autoBattleRoutine = StartCoroutine(AutoResolveRoutine());
                }
            }
            else if (autoBattleRoutine != null)
            {
                StopCoroutine(autoBattleRoutine);
                autoBattleRoutine = null;
            }
        }

        private IEnumerator AutoResolveRoutine()
        {
            while (runManager.BattleSystem != null && !runManager.BattleSystem.IsFinished && runManager.AutoBattleEnabled)
            {
                ResolveOneTurn();
                yield return new WaitForSeconds(runManager.Config.AutoTurnDelay);
            }

            autoBattleRoutine = null;
        }

        private void ResolveOneTurn()
        {
            if (runManager.BattleSystem == null || runManager.BattleSystem.IsFinished || isResolvingPresentation)
            {
                return;
            }

            var report = runManager.BattleSystem.ResolveNextTurn();
            StartCoroutine(PresentResolvedTurn(report));
        }

        private IEnumerator PresentResolvedTurn(BattleTurnReport report)
        {
            if (runManager.BattleSystem == null)
            {
                yield break;
            }

            isResolvingPresentation = true;
            RefreshAutoFlow();

            if (backToMenuButton != null)
            {
                backToMenuButton.interactable = false;
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
            isResolvingPresentation = false;

            if (backToMenuButton != null)
            {
                backToMenuButton.interactable = true;
            }

            if (runManager.BattleSystem.IsFinished)
            {
                stateController?.Show(resultStateId);

                if (resultText != null)
                {
                    resultText.text = runManager.BattleSystem.BattleResult == BattleResultType.Victory
                        ? "승리했습니다. 보상을 받고 다음 진행으로 넘어갑니다."
                        : "패배했습니다. 메인 메뉴로 돌아갑니다.";
                }

                if (continueButton != null)
                {
                    continueButton.gameObject.SetActive(true);
                }
            }

            RefreshAutoFlow();
        }

        private void EnsureRuntimePresentation()
        {
            var canvas = FindSceneCanvas();
            if (canvas == null)
            {
                return;
            }

            battleHud = EnsureBattleHud(canvas);
            battlePresenter = EnsureBattlePresenter(canvas, battleHud);
        }

        private void ApplySceneLayout()
        {
            var canvas = FindSceneCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);
            if (canvas == null)
            {
                return;
            }

            SceneUILayoutHelper.EnsureFullscreenImage(canvas.transform, "RuntimeBattleBackdrop", new Color(0.07f, 0.1f, 0.17f, 1f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeBattleTopCard", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(980f, 360f), new Color(0.11f, 0.17f, 0.28f, 0.95f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeBattleBottomCard", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 116f), new Vector2(980f, 270f), new Color(0.12f, 0.18f, 0.3f, 0.94f));

            var turnText = FindComponentByName<TMP_Text>("TurnText");
            var playerStatsText = FindComponentByName<TMP_Text>("PlayerStatsText");
            var enemyStatsText = FindComponentByName<TMP_Text>("EnemyStatsText");
            var playerDiceText = FindComponentByName<TMP_Text>("PlayerDiceText");
            var battleLogText = FindComponentByName<TMP_Text>("BattleLogText");

            if (turnText != null && turnText.transform is RectTransform turnRect)
            {
                SceneUILayoutHelper.SetRect(turnRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(360f, 72f));
                SceneUILayoutHelper.StyleText(turnText, 34f, TextAlignmentOptions.Center, FontStyles.Bold);
                turnText.color = Color.white;
            }

            if (playerStatsText != null && playerStatsText.transform is RectTransform playerStatsRect)
            {
                SceneUILayoutHelper.SetRect(playerStatsRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(72f, 300f), new Vector2(370f, 108f));
                SceneUILayoutHelper.StyleText(playerStatsText, 19f, TextAlignmentOptions.TopLeft, FontStyles.Bold);
                playerStatsText.color = Color.white;
            }

            if (enemyStatsText != null && enemyStatsText.transform is RectTransform enemyStatsRect)
            {
                SceneUILayoutHelper.SetRect(enemyStatsRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-72f, -164f), new Vector2(370f, 108f));
                SceneUILayoutHelper.StyleText(enemyStatsText, 19f, TextAlignmentOptions.TopRight, FontStyles.Bold);
                enemyStatsText.color = Color.white;
            }

            if (playerDiceText != null && playerDiceText.transform is RectTransform diceRect)
            {
                SceneUILayoutHelper.SetRect(diceRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 228f), new Vector2(760f, 110f));
                SceneUILayoutHelper.StyleText(playerDiceText, 20f, TextAlignmentOptions.Center, FontStyles.Bold);
                playerDiceText.color = new Color(0.88f, 0.93f, 1f, 1f);
            }

            if (battleLogText != null && battleLogText.transform is RectTransform logRect)
            {
                SceneUILayoutHelper.SetRect(logRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(900f, 190f));
                SceneUILayoutHelper.StyleText(battleLogText, 18f, TextAlignmentOptions.TopLeft);
                battleLogText.color = new Color(0.86f, 0.92f, 0.98f, 1f);
            }

            if (rollButton != null && rollButton.transform is RectTransform rollRect)
            {
                SceneUILayoutHelper.SetRect(rollRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(320f, 104f));
                SceneUILayoutHelper.StyleButton(rollButton, new Vector2(320f, 104f), 26f, new Color(0.17f, 0.55f, 0.98f), Color.white);
            }

            if (backToMenuButton != null && backToMenuButton.transform is RectTransform backRect)
            {
                SceneUILayoutHelper.SetRect(backRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(56f, 82f), new Vector2(280f, 104f));
                SceneUILayoutHelper.StyleButton(backToMenuButton, new Vector2(280f, 104f), 24f, new Color(0.2f, 0.24f, 0.34f), Color.white);
            }

            if (continueButton != null && continueButton.transform is RectTransform continueRect)
            {
                SceneUILayoutHelper.SetRect(continueRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(340f, 104f));
                SceneUILayoutHelper.StyleButton(continueButton, new Vector2(340f, 104f), 24f, new Color(0.22f, 0.72f, 0.42f), Color.white);
            }

            if (autoBattleToggle != null && autoBattleToggle.transform is RectTransform toggleRect)
            {
                SceneUILayoutHelper.SetRect(toggleRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 208f), new Vector2(360f, 90f));

                if (autoBattleToggle.targetGraphic is Image toggleBackground)
                {
                    toggleBackground.color = new Color(0.16f, 0.22f, 0.34f, 0.96f);
                }

                if (autoBattleToggle.transform.Find("Background") is RectTransform backgroundRect)
                {
                    SceneUILayoutHelper.SetRect(backgroundRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(44f, 44f));
                }

                if (autoBattleToggle.graphic != null && autoBattleToggle.graphic.transform is RectTransform checkmarkRect)
                {
                    SceneUILayoutHelper.Stretch(checkmarkRect, 6f, 6f, 6f, 6f);
                    if (autoBattleToggle.graphic is Image checkmarkImage)
                    {
                        checkmarkImage.color = new Color(0.35f, 0.95f, 0.55f, 1f);
                    }
                }

                var toggleLabel = autoBattleToggle.GetComponentInChildren<TMP_Text>(true);
                if (toggleLabel != null && toggleLabel.transform is RectTransform labelRect)
                {
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.pivot = new Vector2(0.5f, 0.5f);
                    labelRect.offsetMin = new Vector2(76f, 0f);
                    labelRect.offsetMax = new Vector2(-12f, 0f);
                    labelRect.localScale = Vector3.one;
                    labelRect.localRotation = Quaternion.identity;
                    SceneUILayoutHelper.StyleText(toggleLabel, 24f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                    toggleLabel.color = Color.white;
                    toggleLabel.text = "자동 전투";
                }
            }

            if (resultText != null && resultText.transform is RectTransform resultRect)
            {
                SceneUILayoutHelper.SetRect(resultRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(860f, 220f));
                SceneUILayoutHelper.StyleText(resultText, 40f, TextAlignmentOptions.Center, FontStyles.Bold);
                resultText.color = Color.white;
            }
        }

        private void ApplySceneLayoutIfEnabled()
        {
            var canvas = FindSceneCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);

            if (!useRuntimeLayout)
            {
                return;
            }

            ApplySceneLayout();
        }

        private BattleHUD EnsureBattleHud(Canvas canvas)
        {
            var hud = battleHud != null ? battleHud : FindFirstSceneComponent<BattleHUD>();
            if (hud == null)
            {
                hud = GetOrAddComponent<BattleHUD>(gameObject);
            }

            var runtimeRoot = GetOrCreateChildRect(canvas.transform as RectTransform, "RuntimeBattlePresentation");
            StretchRect(runtimeRoot);

            var turnText = FindComponentByName<TMP_Text>("TurnText");
            var playerStatsText = FindComponentByName<TMP_Text>("PlayerStatsText");
            var enemyStatsText = FindComponentByName<TMP_Text>("EnemyStatsText");
            var currentDiceResultText = FindComponentByName<TMP_Text>("PlayerDiceText");
            var summaryText = FindComponentByName<TMP_Text>("BattleLogText");
            var sampleText = turnText != null ? turnText : FindFirstSceneText();

            if (turnText == null)
            {
                turnText = CreateRuntimeText(runtimeRoot, "RuntimeTurnText", sampleText, "턴 1", 30f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 860f), new Vector2(320f, 54f));
            }

            if (playerStatsText == null)
            {
                playerStatsText = CreateRuntimeText(runtimeRoot, "RuntimePlayerStatsText", sampleText, "플레이어", 26f, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(-250f, -760f), new Vector2(420f, 120f));
            }

            if (enemyStatsText == null)
            {
                enemyStatsText = CreateRuntimeText(runtimeRoot, "RuntimeEnemyStatsText", sampleText, "적", 26f, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(250f, 760f), new Vector2(420f, 120f));
            }

            if (currentDiceResultText == null)
            {
                currentDiceResultText = CreateRuntimeText(runtimeRoot, "RuntimeDiceResultText", sampleText, "이번 턴 주사위 결과", 24f, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -530f), new Vector2(520f, 120f));
            }

            if (summaryText == null)
            {
                summaryText = CreateRuntimeText(runtimeRoot, "RuntimeBattleLogText", sampleText, "전투 로그", 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Vector2(0f, 520f), new Vector2(760f, 260f));
            }

            hud.Configure(turnText, playerStatsText, enemyStatsText, currentDiceResultText, summaryText);
            return hud;
        }

        private BattlePresenter EnsureBattlePresenter(Canvas canvas, BattleHUD hud)
        {
            var presenter = battlePresenter != null ? battlePresenter : FindFirstSceneComponent<BattlePresenter>();
            if (presenter == null)
            {
                presenter = GetOrAddComponent<BattlePresenter>(gameObject);
            }

            var runtimeRoot = GetOrCreateChildRect(canvas.transform as RectTransform, "RuntimeBattlePresentation");
            StretchRect(runtimeRoot);
            var unitsRoot = GetOrCreateChildRect(runtimeRoot, "RuntimeUnits");
            StretchRect(unitsRoot);

            var sampleText = FindFirstSceneText();
            var playerView = CreateRuntimeUnitView(unitsRoot, "RuntimePlayerUnitView", new Vector2(-220f, -220f), sampleText);
            var enemyView01 = CreateRuntimeUnitView(unitsRoot, "RuntimeEnemyUnitView01", new Vector2(220f, 170f), sampleText);
            var enemyView02 = CreateRuntimeUnitView(unitsRoot, "RuntimeEnemyUnitView02", new Vector2(380f, 270f), sampleText);
            var enemyView03 = CreateRuntimeUnitView(unitsRoot, "RuntimeEnemyUnitView03", new Vector2(380f, 120f), sampleText);
            enemyView02.gameObject.SetActive(false);
            enemyView03.gameObject.SetActive(false);

            var floatingTextSpawner = EnsureFloatingTextSpawner(runtimeRoot, sampleText);
            presenter.Configure(playerView, new[] { enemyView01, enemyView02, enemyView03 }, hud, floatingTextSpawner);
            return presenter;
        }

        private FloatingTextSpawner EnsureFloatingTextSpawner(RectTransform runtimeRoot, TMP_Text sampleText)
        {
            var floatingCanvas = GetOrCreateChildRect(runtimeRoot, "RuntimeFloatingTextCanvas");
            StretchRect(floatingCanvas);

            var spawner = floatingCanvas.GetComponent<FloatingTextSpawner>();
            if (spawner == null)
            {
                spawner = floatingCanvas.gameObject.AddComponent<FloatingTextSpawner>();
            }

            var template = CreateRuntimeText(floatingCanvas, "RuntimeFloatingTextTemplate", sampleText, "-99", 34f, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, new Vector2(240f, 60f));
            template.gameObject.SetActive(false);
            spawner.Configure(floatingCanvas, template);
            return spawner;
        }

        private UnitView CreateRuntimeUnitView(RectTransform parent, string objectName, Vector2 anchoredPosition, TMP_Text sampleText)
        {
            var root = GetOrCreateChildRect(parent, objectName);
            SetAnchoredRect(root, anchoredPosition, new Vector2(280f, 360f));

            var unitView = GetOrAddComponent<UnitView>(root.gameObject);
            var canvasGroup = GetOrAddComponent<CanvasGroup>(root.gameObject);

            var highlightImage = CreateRuntimeImage(root, "Highlight", new Vector2(0f, 48f), new Vector2(232f, 232f), new Color(1f, 1f, 1f, 0.16f), false);
            var spriteImage = CreateRuntimeImage(root, "SpriteImage", new Vector2(0f, 48f), new Vector2(220f, 220f), Color.white, true);
            var popupAnchor = GetOrCreateChildRect(root, "PopupAnchor");
            SetAnchoredRect(popupAnchor, new Vector2(0f, 170f), Vector2.zero);

            var nameText = CreateRuntimeText(root, "NameText", sampleText, objectName, 28f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 180f), new Vector2(240f, 38f));
            var hpBar = CreateRuntimeSlider(root, "HpBar", new Vector2(0f, -74f), new Vector2(220f, 18f));
            var hpText = CreateRuntimeText(root, "HpText", sampleText, "HP 0/0", 20f, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -102f), new Vector2(240f, 28f));
            var shieldArmorText = CreateRuntimeText(root, "ShieldArmorText", sampleText, "방어도 0 / 방어력 0", 18f, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -132f), new Vector2(260f, 28f));
            var rageText = CreateRuntimeText(root, "RageText", sampleText, "분노 0", 18f, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -160f), new Vector2(220f, 28f));

            unitView.ConfigureRuntime(root, popupAnchor, canvasGroup, spriteImage, highlightImage, hpBar, nameText, hpText, shieldArmorText, rageText);
            return unitView;
        }

        private Slider CreateRuntimeSlider(RectTransform parent, string objectName, Vector2 anchoredPosition, Vector2 size)
        {
            var sliderRoot = GetOrCreateChildRect(parent, objectName);
            SetAnchoredRect(sliderRoot, anchoredPosition, size);

            var background = CreateStretchImage(sliderRoot, "Background", new Color(0.12f, 0.12f, 0.12f, 0.9f));
            var fillArea = GetOrCreateChildRect(sliderRoot, "Fill Area");
            StretchRect(fillArea, 2f, 2f, 2f, 2f);
            var fill = CreateStretchImage(fillArea, "Fill", new Color(0.3f, 0.95f, 0.45f, 1f));

            var slider = GetOrAddComponent<Slider>(sliderRoot.gameObject);
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

        private TMP_Text CreateRuntimeText(
            RectTransform parent,
            string objectName,
            TMP_Text sampleText,
            string defaultValue,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var rect = GetOrCreateChildRect(parent, objectName);
            SetAnchoredRect(rect, anchoredPosition, size);

            var text = rect.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            }

            if (sampleText != null)
            {
                text.font = sampleText.font;
                text.fontSharedMaterial = sampleText.fontSharedMaterial;
                text.color = sampleText.color;
            }

            text.text = defaultValue;
            text.fontSize = fontSize * RuntimeTextScaleMultiplier;
            text.fontSizeMax = fontSize * RuntimeTextScaleMultiplier;
            text.fontSizeMin = Mathf.Max(18f, fontSize * RuntimeTextScaleMultiplier * 0.6f);
            text.enableAutoSizing = true;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private Image CreateRuntimeImage(RectTransform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Color color, bool preserveAspect)
        {
            var rect = GetOrCreateChildRect(parent, objectName);
            SetAnchoredRect(rect, anchoredPosition, size);

            var image = GetOrAddComponent<Image>(rect.gameObject);
            image.sprite = GetRuntimeWhiteSprite();
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        private Image CreateStretchImage(RectTransform parent, string objectName, Color color)
        {
            var rect = GetOrCreateChildRect(parent, objectName);
            StretchRect(rect);

            var image = GetOrAddComponent<Image>(rect.gameObject);
            image.sprite = GetRuntimeWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Canvas FindSceneCanvas()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < canvases.Length; index++)
            {
                var canvas = canvases[index];
                if (canvas == null || !canvas.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (canvas.isRootCanvas)
                {
                    return canvas;
                }
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        private TMP_Text FindFirstSceneText()
        {
            return FindFirstSceneComponent<TMP_Text>();
        }

        private static T FindComponentByName<T>(string objectName) where T : Component
        {
            var components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < components.Length; index++)
            {
                var component = components[index];
                if (component != null && component.name == objectName && component.gameObject.scene.IsValid())
                {
                    return component;
                }
            }

            return null;
        }

        private static T FindFirstSceneComponent<T>() where T : Component
        {
            var components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < components.Length; index++)
            {
                if (components[index] != null && components[index].gameObject.scene.IsValid())
                {
                    return components[index];
                }
            }

            return null;
        }

        private static RectTransform GetOrCreateChildRect(RectTransform parent, string objectName)
        {
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index) as RectTransform;
                if (child != null && child.name == objectName)
                {
                    return child;
                }
            }

            var childObject = new GameObject(objectName, typeof(RectTransform));
            var rectTransform = childObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }

        private static void SetAnchoredRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = Vector3.one;
        }

        private static void StretchRect(RectTransform rectTransform, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
            rectTransform.localScale = Vector3.one;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static Sprite GetRuntimeWhiteSprite()
        {
            if (cachedUiSprite == null)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };

                texture.SetPixels(new[]
                {
                    Color.white, Color.white,
                    Color.white, Color.white
                });
                texture.Apply();

                cachedUiSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                cachedUiSprite.name = "RuntimeWhiteUiSprite";
                cachedUiSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return cachedUiSprite;
        }
    }
}
