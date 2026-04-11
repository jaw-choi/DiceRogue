using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiceRogue
{
    [ExecuteAlways]
    public class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private bool useRuntimeLayout = true;
        [SerializeField] private bool useEditorUiLayout = false;
        [SerializeField] private UIStateController stateController;
        [SerializeField] private string battleStateId = "Battle";
        [SerializeField] private string resultStateId = "Result";
        [SerializeField] private BattleHUD battleHud;
        [SerializeField] private BattlePresenter battlePresenter;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Toggle autoBattleToggle;
        [SerializeField] private Button rollButton;
        [SerializeField] private Button debugKillButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button backToMenuButton;
        [Header("Battle Hit Effect")]
        [SerializeField] private BattleHitEffectSettings battleHitEffectSettings = new BattleHitEffectSettings();
        [Header("Battle Bottom UI Layout")]
        [SerializeField] private BattleBottomUiLayoutSettings battleBottomUiLayout = new BattleBottomUiLayoutSettings();
        [Header("Runtime Bottom UI Tuner")]
        [SerializeField] private RuntimeBottomUiTunerSettings runtimeBottomUiTunerSettings = new RuntimeBottomUiTunerSettings();

        private static Sprite cachedUiSprite;
        private const float RuntimeTextScaleMultiplier = 1.5f;

        private GameRunManager runManager;
        private Coroutine autoBattleRoutine;
        private bool isResolvingPresentation;
        private bool pendingRuntimeBottomUiRefresh;
        private int lastBottomUiLayoutSignature;
        private BottomUiTunerTarget bottomUiTunerTarget = BottomUiTunerTarget.BottomCard;
        private RuntimeBottomUiTunerView runtimeBottomUiTunerView;

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

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

            if (debugKillButton != null)
            {
                debugKillButton.onClick.AddListener(ResolveDebugKill);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(runManager.CompleteBattleAndAdvance);
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene(runManager.Config.MainMenuSceneName));
            }

            SceneUILayoutHelper.SetButtonLabel(rollButton, "Resolve Turn");
            SceneUILayoutHelper.SetButtonLabel(debugKillButton, "Debug Kill");
            SceneUILayoutHelper.SetButtonLabel(continueButton, "Continue");
            SceneUILayoutHelper.SetButtonLabel(backToMenuButton, "Main Menu");
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                EnsureRuntimePresentation();
                ApplySceneLayoutIfEnabled();
                RefreshRuntimeBottomUiTuner();
                CacheRuntimeBottomUiSignature();
                return;
            }

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
            CacheRuntimeBottomUiSignature();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (autoBattleRoutine != null)
            {
                StopCoroutine(autoBattleRoutine);
                autoBattleRoutine = null;
            }

            pendingRuntimeBottomUiRefresh = false;
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (!pendingRuntimeBottomUiRefresh)
                {
                    return;
                }

                pendingRuntimeBottomUiRefresh = false;
                EnsureRuntimePresentation();
                ApplySceneLayoutIfEnabled();
                RefreshRuntimeBottomUiTuner();
                CacheRuntimeBottomUiSignature();
                return;
            }

            if (!pendingRuntimeBottomUiRefresh)
            {
                return;
            }

            pendingRuntimeBottomUiRefresh = false;
            ApplyRuntimeBottomUiRefresh();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureRuntimePresentation();
                ApplySceneLayoutIfEnabled();
                RefreshRuntimeBottomUiTuner();
                CacheRuntimeBottomUiSignature();
                return;
            }

            pendingRuntimeBottomUiRefresh = true;
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

            if (debugKillButton != null)
            {
                debugKillButton.interactable = !isBattleFinished && !isResolvingPresentation;
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

        private void ResolveDebugKill()
        {
            if (runManager.BattleSystem == null || runManager.BattleSystem.IsFinished || isResolvingPresentation)
            {
                return;
            }

            if (autoBattleToggle != null && autoBattleToggle.isOn)
            {
                autoBattleToggle.isOn = false;
            }
            else
            {
                runManager.AutoBattleEnabled = false;
            }

            var report = runManager.BattleSystem.ForceVictoryForDebug();
            battlePresenter?.BindBattle(runManager.BattleSystem);
            battleHud?.Refresh(runManager.BattleSystem, report);
            ApplyBattleResultState();
            RefreshAutoFlow();
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

            ApplyBattleResultState();

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
            debugKillButton = EnsureDebugKillButton(canvas);
            EnsureRuntimeBottomUiTuner(canvas);
        }

        private void ApplySceneLayout()
        {
            var canvas = FindSceneCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);
            if (canvas == null)
            {
                return;
            }

            SyncBattleBottomLayoutFromExistingRects();

            SceneUILayoutHelper.EnsureFullscreenImage(canvas.transform, "RuntimeBattleBackdrop", new Color(0.07f, 0.1f, 0.17f, 1f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeBattleTopCard", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(980f, 470f), new Color(0.11f, 0.17f, 0.28f, 0.95f));
            SceneUILayoutHelper.EnsurePanel(
                canvas.transform,
                "RuntimeBattleBottomCard",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                battleBottomUiLayout.BottomCardPosition,
                battleBottomUiLayout.BottomCardSize,
                new Color(0.12f, 0.18f, 0.3f, 0.94f));

            LayoutHudTexts();
            LayoutActionControls();
            RefreshRuntimeBottomUiTuner();
        }

        private void LayoutHudTexts()
        {
            var turnText = FindComponentByName<TMP_Text>("TurnText");
            var actingUnitText = FindComponentByName<TMP_Text>("ActingUnitText");
            var turnQueueText = FindComponentByName<TMP_Text>("TurnQueueText");
            var playerStatsText = FindComponentByName<TMP_Text>("PlayerStatsText");
            var enemyStatsText = FindComponentByName<TMP_Text>("EnemyStatsText");
            var rollLogText = FindComponentByName<TMP_Text>("RollLogText");
            var battleLogText = FindComponentByName<TMP_Text>("BattleLogText");

            if (turnText != null && turnText.transform is RectTransform turnRect)
            {
                SceneUILayoutHelper.SetRect(turnRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(360f, 64f));
                SceneUILayoutHelper.StyleText(turnText, 34f, TextAlignmentOptions.Center, FontStyles.Bold);
                turnText.color = Color.white;
            }

            if (actingUnitText != null && actingUnitText.transform is RectTransform actingRect)
            {
                SceneUILayoutHelper.SetRect(actingRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(520f, 50f));
                SceneUILayoutHelper.StyleText(actingUnitText, 22f, TextAlignmentOptions.Center, FontStyles.Bold);
                actingUnitText.color = new Color(1f, 0.92f, 0.65f, 1f);
            }

            if (turnQueueText != null && turnQueueText.transform is RectTransform queueRect)
            {
                SceneUILayoutHelper.SetRect(queueRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(880f, 84f));
                SceneUILayoutHelper.StyleText(turnQueueText, 17f, TextAlignmentOptions.Center, FontStyles.Bold);
                turnQueueText.color = new Color(0.84f, 0.9f, 0.98f, 1f);
            }

            if (playerStatsText != null && playerStatsText.transform is RectTransform playerStatsRect)
            {
                SceneUILayoutHelper.SetRect(playerStatsRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, -290f), new Vector2(300f, 190f));
                SceneUILayoutHelper.StyleText(playerStatsText, 18f, TextAlignmentOptions.TopLeft, FontStyles.Bold);
                playerStatsText.color = Color.white;
            }

            if (enemyStatsText != null && enemyStatsText.transform is RectTransform enemyStatsRect)
            {
                SceneUILayoutHelper.SetRect(enemyStatsRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-72f, -290f), new Vector2(320f, 210f));
                SceneUILayoutHelper.StyleText(enemyStatsText, 17f, TextAlignmentOptions.TopRight, FontStyles.Bold);
                enemyStatsText.color = Color.white;
            }

            if (rollLogText != null && rollLogText.transform is RectTransform rollRect)
            {
                SceneUILayoutHelper.SetRect(
                    rollRect,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    battleBottomUiLayout.RollLogPosition,
                    battleBottomUiLayout.RollLogSize);
                SceneUILayoutHelper.StyleText(rollLogText, 18f, TextAlignmentOptions.Center, FontStyles.Bold);
                rollLogText.color = new Color(0.88f, 0.93f, 1f, 1f);
            }

            if (battleLogText != null && battleLogText.transform is RectTransform logRect)
            {
                SceneUILayoutHelper.SetRect(
                    logRect,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    battleBottomUiLayout.BattleLogPosition,
                    battleBottomUiLayout.BattleLogSize);
                SceneUILayoutHelper.StyleText(battleLogText, 16f, TextAlignmentOptions.TopLeft);
                battleLogText.color = new Color(0.86f, 0.92f, 0.98f, 1f);
            }
        }

        private void LayoutActionControls()
        {
            if (rollButton != null && rollButton.transform is RectTransform manualRect)
            {
                SceneUILayoutHelper.SetRect(manualRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(340f, 96f));
                SceneUILayoutHelper.StyleButton(rollButton, new Vector2(340f, 96f), 24f, new Color(0.17f, 0.55f, 0.98f), Color.white);
            }

            if (backToMenuButton != null && backToMenuButton.transform is RectTransform backRect)
            {
                SceneUILayoutHelper.SetRect(backRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(56f, 72f), new Vector2(260f, 96f));
                SceneUILayoutHelper.StyleButton(backToMenuButton, new Vector2(260f, 96f), 22f, new Color(0.2f, 0.24f, 0.34f), Color.white);
            }

            if (debugKillButton != null && debugKillButton.transform is RectTransform debugKillRect)
            {
                SceneUILayoutHelper.SetRect(debugKillRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 72f), new Vector2(260f, 96f));
                SceneUILayoutHelper.StyleButton(debugKillButton, new Vector2(260f, 96f), 22f, new Color(0.83f, 0.25f, 0.25f), Color.white);
            }

            if (continueButton != null && continueButton.transform is RectTransform continueRect)
            {
                SceneUILayoutHelper.SetRect(continueRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(360f, 96f));
                SceneUILayoutHelper.StyleButton(continueButton, new Vector2(360f, 96f), 24f, new Color(0.22f, 0.72f, 0.42f), Color.white);
            }

            if (autoBattleToggle != null && autoBattleToggle.transform is RectTransform toggleRect)
            {
                SceneUILayoutHelper.SetRect(toggleRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 190f), new Vector2(360f, 84f));
                if (autoBattleToggle.targetGraphic is Image toggleBackground)
                {
                    toggleBackground.color = new Color(0.16f, 0.22f, 0.34f, 0.96f);
                }

                var toggleLabel = autoBattleToggle.GetComponentInChildren<TMP_Text>(true);
                if (toggleLabel != null)
                {
                    SceneUILayoutHelper.StyleText(toggleLabel, 22f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                    toggleLabel.color = Color.white;
                    toggleLabel.text = "Auto Battle";
                }
            }

            if (resultText != null && resultText.transform is RectTransform resultRect)
            {
                SceneUILayoutHelper.SetRect(resultRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(860f, 220f));
                SceneUILayoutHelper.StyleText(resultText, 36f, TextAlignmentOptions.Center, FontStyles.Bold);
                resultText.color = Color.white;
            }
        }

        private void ApplySceneLayoutIfEnabled()
        {
            var canvas = FindSceneCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);

            if (!useRuntimeLayout || useEditorUiLayout)
            {
                return;
            }

            ApplySceneLayout();
        }

        private void ApplyBattleResultState()
        {
            if (runManager?.BattleSystem == null || !runManager.BattleSystem.IsFinished)
            {
                return;
            }

            stateController?.Show(resultStateId);

            if (resultText != null)
            {
                resultText.text = runManager.BattleSystem.BattleResult == BattleResultType.Victory
                    ? "Victory. Take your reward and continue the run."
                    : "Defeat. Return to the main menu.";
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }
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
            var actingUnitText = FindComponentByName<TMP_Text>("ActingUnitText");
            var turnQueueText = FindComponentByName<TMP_Text>("TurnQueueText");
            var playerStatsText = FindComponentByName<TMP_Text>("PlayerStatsText");
            var enemyStatsText = FindComponentByName<TMP_Text>("EnemyStatsText");
            var rollLogText = FindComponentByName<TMP_Text>("RollLogText");
            var summaryText = FindComponentByName<TMP_Text>("BattleLogText");
            var sampleText = turnText != null ? turnText : FindFirstSceneText();

            if (turnText == null)
            {
                turnText = CreateRuntimeText(runtimeRoot, "TurnText", sampleText, "Turn 1", 30f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 860f), new Vector2(320f, 54f));
            }

            if (actingUnitText == null)
            {
                actingUnitText = CreateRuntimeText(runtimeRoot, "ActingUnitText", sampleText, "Acting: Waiting", 20f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 800f), new Vector2(520f, 50f));
            }

            if (turnQueueText == null)
            {
                turnQueueText = CreateRuntimeText(runtimeRoot, "TurnQueueText", sampleText, "Turn Queue", 16f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 734f), new Vector2(900f, 84f));
            }

            if (playerStatsText == null)
            {
                playerStatsText = CreateRuntimeText(runtimeRoot, "PlayerStatsText", sampleText, "Player", 18f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Vector2(-280f, 640f), new Vector2(300f, 190f));
            }

            if (enemyStatsText == null)
            {
                enemyStatsText = CreateRuntimeText(runtimeRoot, "EnemyStatsText", sampleText, "Enemies", 18f, FontStyles.Bold, TextAlignmentOptions.TopRight, new Vector2(280f, 640f), new Vector2(320f, 210f));
            }

            if (rollLogText == null)
            {
                rollLogText = CreateRuntimeText(runtimeRoot, "RollLogText", sampleText, "Roll Log", 18f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -620f), new Vector2(900f, 90f));
            }

            if (summaryText == null)
            {
                summaryText = CreateRuntimeText(runtimeRoot, "BattleLogText", sampleText, "Battle Log", 16f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Vector2(0f, -758f), new Vector2(900f, 170f));
            }

            var playerSkillCard = RuntimeSkillCardFactory.EnsureSkillCard(
                runtimeRoot,
                "PlayerSkillCard",
                sampleText,
                battleBottomUiLayout.PlayerSkillCardPosition,
                battleBottomUiLayout.PlayerSkillCardSize,
                true);
            var enemySkillCard = RuntimeSkillCardFactory.EnsureSkillCard(
                runtimeRoot,
                "EnemySkillCard",
                sampleText,
                battleBottomUiLayout.EnemySkillCardPosition,
                battleBottomUiLayout.EnemySkillCardSize,
                true);

            hud.Configure(
                turnText,
                actingUnitText,
                turnQueueText,
                playerStatsText,
                enemyStatsText,
                rollLogText,
                summaryText,
                playerSkillCard.IconImage,
                enemySkillCard.IconImage,
                playerSkillCard.TitleText,
                playerSkillCard.BodyText,
                enemySkillCard.TitleText,
                enemySkillCard.BodyText);
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
            var playerView = CreateRuntimeUnitView(unitsRoot, "RuntimePlayerUnitView", new Vector2(-230f, -120f), sampleText);
            var enemyView01 = CreateRuntimeUnitView(unitsRoot, "RuntimeEnemyUnitView01", new Vector2(220f, 70f), sampleText);
            var enemyView02 = CreateRuntimeUnitView(unitsRoot, "RuntimeEnemyUnitView02", new Vector2(390f, 170f), sampleText);
            var enemyView03 = CreateRuntimeUnitView(unitsRoot, "RuntimeEnemyUnitView03", new Vector2(390f, 10f), sampleText);
            enemyView02.gameObject.SetActive(false);
            enemyView03.gameObject.SetActive(false);

            var floatingTextSpawner = EnsureFloatingTextSpawner(runtimeRoot, sampleText);
            presenter.Configure(playerView, new[] { enemyView01, enemyView02, enemyView03 }, hud, floatingTextSpawner, canvas, battleHitEffectSettings);
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

        private Button EnsureDebugKillButton(Canvas canvas)
        {
            if (debugKillButton != null || !useRuntimeLayout || useEditorUiLayout || canvas == null)
            {
                return debugKillButton;
            }

            var runtimeRoot = GetOrCreateChildRect(canvas.transform as RectTransform, "RuntimeBattlePresentation");
            var sampleText = FindFirstSceneText();
            debugKillButton = CreateRuntimeButton(runtimeRoot, "RuntimeDebugKillButton", sampleText, "Debug Kill");
            return debugKillButton;
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
            var shieldArmorText = CreateRuntimeText(root, "ShieldArmorText", sampleText, "Shield 0 / Armor 0", 18f, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -132f), new Vector2(260f, 28f));
            var rageText = CreateRuntimeText(root, "RageText", sampleText, "Rage 0", 18f, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -160f), new Vector2(220f, 28f));

            unitView.ConfigureRuntime(root, popupAnchor, canvasGroup, spriteImage, highlightImage, hpBar, nameText, hpText, shieldArmorText, rageText);
            return unitView;
        }

        private Button CreateRuntimeButton(RectTransform parent, string objectName, TMP_Text sampleText, string labelText)
        {
            var root = GetOrCreateChildRect(parent, objectName);
            SetAnchoredRect(root, Vector2.zero, new Vector2(260f, 96f));

            var image = GetOrAddComponent<Image>(root.gameObject);
            image.sprite = GetRuntimeWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(0.83f, 0.25f, 0.25f);
            image.raycastTarget = true;

            var button = GetOrAddComponent<Button>(root.gameObject);
            button.targetGraphic = image;

            var label = CreateRuntimeText(root, "Label", sampleText, labelText, 22f, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, new Vector2(220f, 44f));
            label.raycastTarget = false;
            return button;
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

        private void SyncBattleBottomLayoutFromExistingRects()
        {
            SyncBattleBottomRect(ref battleBottomUiLayout.BottomCardPosition, ref battleBottomUiLayout.BottomCardSize, FindComponentByName<RectTransform>("RuntimeBattleBottomCard"));
            SyncBattleBottomRect(ref battleBottomUiLayout.RollLogPosition, ref battleBottomUiLayout.RollLogSize, FindComponentByName<RectTransform>("RollLogText"));
            SyncBattleBottomRect(ref battleBottomUiLayout.BattleLogPosition, ref battleBottomUiLayout.BattleLogSize, FindComponentByName<RectTransform>("BattleLogText"));
            SyncBattleBottomRect(ref battleBottomUiLayout.PlayerSkillCardPosition, ref battleBottomUiLayout.PlayerSkillCardSize, FindComponentByName<RectTransform>("PlayerSkillCard"));
            SyncBattleBottomRect(ref battleBottomUiLayout.EnemySkillCardPosition, ref battleBottomUiLayout.EnemySkillCardSize, FindComponentByName<RectTransform>("EnemySkillCard"));
        }

        private static void SyncBattleBottomRect(ref Vector2 anchoredPosition, ref Vector2 size, RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            anchoredPosition = rectTransform.anchoredPosition;
            size = SceneUILayoutHelper.ResolveRectSize(rectTransform, size);
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

        private void EnsureRuntimeBottomUiTuner(Canvas canvas)
        {
            if (!useRuntimeLayout || useEditorUiLayout)
            {
                return;
            }

            if (!runtimeBottomUiTunerSettings.ShowRuntimeTuner)
            {
                if (runtimeBottomUiTunerView?.Root != null)
                {
                    runtimeBottomUiTunerView.Root.gameObject.SetActive(false);
                }

                return;
            }

            var root = GetOrCreateChildRect(canvas.transform as RectTransform, "RuntimeBattleBottomUiTuner");
            SetAnchoredRect(root, runtimeBottomUiTunerSettings.PanelPosition, runtimeBottomUiTunerSettings.PanelSize);

            var background = GetOrAddComponent<Image>(root.gameObject);
            background.sprite = GetRuntimeWhiteSprite();
            background.type = Image.Type.Simple;
            background.color = new Color(0.08f, 0.12f, 0.2f, 0.9f);
            background.raycastTarget = true;

            var sampleText = FindFirstSceneText();
            var title = CreateRuntimeText(root, "Title", sampleText, "Bottom UI Tuner", 14f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 82f), new Vector2(220f, 32f));
            title.color = Color.white;
            var info = CreateRuntimeText(root, "Info", sampleText, string.Empty, 11f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 38f), new Vector2(220f, 52f));
            info.color = new Color(0.84f, 0.91f, 1f, 1f);

            var targetButton = EnsureTunerButton(root, "TargetButton", "Target", new Vector2(0f, 0f), new Vector2(176f, 38f), new Color(0.2f, 0.42f, 0.72f));
            var xMinus = EnsureTunerButton(root, "XMinusButton", "X-", new Vector2(-72f, -44f), new Vector2(58f, 34f), new Color(0.24f, 0.28f, 0.36f));
            var xPlus = EnsureTunerButton(root, "XPlusButton", "X+", new Vector2(-8f, -44f), new Vector2(58f, 34f), new Color(0.24f, 0.28f, 0.36f));
            var yMinus = EnsureTunerButton(root, "YMinusButton", "Y-", new Vector2(56f, -44f), new Vector2(58f, 34f), new Color(0.24f, 0.28f, 0.36f));
            var yPlus = EnsureTunerButton(root, "YPlusButton", "Y+", new Vector2(120f, -44f), new Vector2(58f, 34f), new Color(0.24f, 0.28f, 0.36f));
            var wMinus = EnsureTunerButton(root, "WMinusButton", "W-", new Vector2(-72f, -86f), new Vector2(58f, 34f), new Color(0.18f, 0.46f, 0.66f));
            var wPlus = EnsureTunerButton(root, "WPlusButton", "W+", new Vector2(-8f, -86f), new Vector2(58f, 34f), new Color(0.18f, 0.46f, 0.66f));
            var hMinus = EnsureTunerButton(root, "HMinusButton", "H-", new Vector2(56f, -86f), new Vector2(58f, 34f), new Color(0.18f, 0.46f, 0.66f));
            var hPlus = EnsureTunerButton(root, "HPlusButton", "H+", new Vector2(120f, -86f), new Vector2(58f, 34f), new Color(0.18f, 0.46f, 0.66f));

            BindTunerButton(targetButton, CycleBottomUiTunerTarget);
            BindTunerButton(xMinus, () => AdjustBottomUiTarget(-runtimeBottomUiTunerSettings.PositionStep, 0f, 0f, 0f));
            BindTunerButton(xPlus, () => AdjustBottomUiTarget(runtimeBottomUiTunerSettings.PositionStep, 0f, 0f, 0f));
            BindTunerButton(yMinus, () => AdjustBottomUiTarget(0f, -runtimeBottomUiTunerSettings.PositionStep, 0f, 0f));
            BindTunerButton(yPlus, () => AdjustBottomUiTarget(0f, runtimeBottomUiTunerSettings.PositionStep, 0f, 0f));
            BindTunerButton(wMinus, () => AdjustBottomUiTarget(0f, 0f, -runtimeBottomUiTunerSettings.SizeStep, 0f));
            BindTunerButton(wPlus, () => AdjustBottomUiTarget(0f, 0f, runtimeBottomUiTunerSettings.SizeStep, 0f));
            BindTunerButton(hMinus, () => AdjustBottomUiTarget(0f, 0f, 0f, -runtimeBottomUiTunerSettings.SizeStep));
            BindTunerButton(hPlus, () => AdjustBottomUiTarget(0f, 0f, 0f, runtimeBottomUiTunerSettings.SizeStep));

            runtimeBottomUiTunerView = new RuntimeBottomUiTunerView
            {
                Root = root,
                TitleText = title,
                InfoText = info
            };

            RefreshRuntimeBottomUiTuner();
        }

        private Button EnsureTunerButton(RectTransform parent, string objectName, string labelText, Vector2 anchoredPosition, Vector2 size, Color backgroundColor)
        {
            var root = GetOrCreateChildRect(parent, objectName);
            SetAnchoredRect(root, anchoredPosition, size);

            var image = GetOrAddComponent<Image>(root.gameObject);
            image.sprite = GetRuntimeWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = backgroundColor;
            image.raycastTarget = true;

            var button = GetOrAddComponent<Button>(root.gameObject);
            button.targetGraphic = image;

            var sampleText = FindFirstSceneText();
            var label = CreateRuntimeText(root, "Label", sampleText, labelText, 12f, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(10f, 6f));
            label.color = Color.white;
            return button;
        }

        private void RefreshRuntimeBottomUiTuner()
        {
            if (runtimeBottomUiTunerView == null)
            {
                return;
            }

            runtimeBottomUiTunerView.Root.gameObject.SetActive(runtimeBottomUiTunerSettings.ShowRuntimeTuner);
            runtimeBottomUiTunerView.TitleText.text = $"Bottom UI | {bottomUiTunerTarget}";

            var position = GetBottomUiTargetPosition();
            var size = GetBottomUiTargetSize();
            runtimeBottomUiTunerView.InfoText.text = $"Pos {position.x:0}/{position.y:0}\nSize {size.x:0}/{size.y:0}";
        }

        private void CycleBottomUiTunerTarget()
        {
            bottomUiTunerTarget = bottomUiTunerTarget == BottomUiTunerTarget.EnemySkillCard
                ? BottomUiTunerTarget.BottomCard
                : bottomUiTunerTarget + 1;
            RefreshRuntimeBottomUiTuner();
        }

        private void AdjustBottomUiTarget(float deltaX, float deltaY, float deltaWidth, float deltaHeight)
        {
            switch (bottomUiTunerTarget)
            {
                case BottomUiTunerTarget.BottomCard:
                    AdjustBottomUiRect(ref battleBottomUiLayout.BottomCardPosition, ref battleBottomUiLayout.BottomCardSize, deltaX, deltaY, deltaWidth, deltaHeight);
                    break;
                case BottomUiTunerTarget.RollLog:
                    AdjustBottomUiRect(ref battleBottomUiLayout.RollLogPosition, ref battleBottomUiLayout.RollLogSize, deltaX, deltaY, deltaWidth, deltaHeight);
                    break;
                case BottomUiTunerTarget.BattleLog:
                    AdjustBottomUiRect(ref battleBottomUiLayout.BattleLogPosition, ref battleBottomUiLayout.BattleLogSize, deltaX, deltaY, deltaWidth, deltaHeight);
                    break;
                case BottomUiTunerTarget.PlayerSkillCard:
                    AdjustBottomUiRect(ref battleBottomUiLayout.PlayerSkillCardPosition, ref battleBottomUiLayout.PlayerSkillCardSize, deltaX, deltaY, deltaWidth, deltaHeight);
                    break;
                case BottomUiTunerTarget.EnemySkillCard:
                    AdjustBottomUiRect(ref battleBottomUiLayout.EnemySkillCardPosition, ref battleBottomUiLayout.EnemySkillCardSize, deltaX, deltaY, deltaWidth, deltaHeight);
                    break;
            }

            EnsureRuntimePresentation();
            ApplySceneLayoutIfEnabled();
            battlePresenter?.BindBattle(runManager.BattleSystem);
            battleHud?.Refresh(runManager.BattleSystem);
            RefreshRuntimeBottomUiTuner();
        }

        private static void AdjustBottomUiRect(ref Vector2 position, ref Vector2 size, float deltaX, float deltaY, float deltaWidth, float deltaHeight)
        {
            position += new Vector2(deltaX, deltaY);
            size += new Vector2(deltaWidth, deltaHeight);
            size = new Vector2(Mathf.Max(120f, size.x), Mathf.Max(40f, size.y));
        }

        private Vector2 GetBottomUiTargetPosition()
        {
            return bottomUiTunerTarget switch
            {
                BottomUiTunerTarget.BottomCard => battleBottomUiLayout.BottomCardPosition,
                BottomUiTunerTarget.RollLog => battleBottomUiLayout.RollLogPosition,
                BottomUiTunerTarget.BattleLog => battleBottomUiLayout.BattleLogPosition,
                BottomUiTunerTarget.PlayerSkillCard => battleBottomUiLayout.PlayerSkillCardPosition,
                BottomUiTunerTarget.EnemySkillCard => battleBottomUiLayout.EnemySkillCardPosition,
                _ => Vector2.zero
            };
        }

        private Vector2 GetBottomUiTargetSize()
        {
            return bottomUiTunerTarget switch
            {
                BottomUiTunerTarget.BottomCard => battleBottomUiLayout.BottomCardSize,
                BottomUiTunerTarget.RollLog => battleBottomUiLayout.RollLogSize,
                BottomUiTunerTarget.BattleLog => battleBottomUiLayout.BattleLogSize,
                BottomUiTunerTarget.PlayerSkillCard => battleBottomUiLayout.PlayerSkillCardSize,
                BottomUiTunerTarget.EnemySkillCard => battleBottomUiLayout.EnemySkillCardSize,
                _ => Vector2.zero
            };
        }

        private void ApplyRuntimeBottomUiRefresh()
        {
            var signature = GetBottomUiLayoutSignature();
            if (signature == lastBottomUiLayoutSignature)
            {
                return;
            }

            EnsureRuntimePresentation();
            ApplySceneLayoutIfEnabled();
            battlePresenter?.BindBattle(runManager.BattleSystem);
            battleHud?.Refresh(runManager.BattleSystem);
            RefreshRuntimeBottomUiTuner();
            lastBottomUiLayoutSignature = signature;
        }

        private void CacheRuntimeBottomUiSignature()
        {
            lastBottomUiLayoutSignature = GetBottomUiLayoutSignature();
        }

        private int GetBottomUiLayoutSignature()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + battleBottomUiLayout.BottomCardPosition.GetHashCode();
                hash = (hash * 31) + battleBottomUiLayout.BottomCardSize.GetHashCode();
                hash = (hash * 31) + battleBottomUiLayout.RollLogPosition.GetHashCode();
                hash = (hash * 31) + battleBottomUiLayout.RollLogSize.GetHashCode();
                hash = (hash * 31) + battleBottomUiLayout.BattleLogPosition.GetHashCode();
                hash = (hash * 31) + battleBottomUiLayout.BattleLogSize.GetHashCode();
                hash = (hash * 31) + battleBottomUiLayout.PlayerSkillCardPosition.GetHashCode();
                hash = (hash * 31) + battleBottomUiLayout.PlayerSkillCardSize.GetHashCode();
                hash = (hash * 31) + battleBottomUiLayout.EnemySkillCardPosition.GetHashCode();
                hash = (hash * 31) + battleBottomUiLayout.EnemySkillCardSize.GetHashCode();
                hash = (hash * 31) + (runtimeBottomUiTunerSettings.ShowRuntimeTuner ? 1 : 0);
                hash = (hash * 31) + runtimeBottomUiTunerSettings.PanelPosition.GetHashCode();
                hash = (hash * 31) + runtimeBottomUiTunerSettings.PanelSize.GetHashCode();
                hash = (hash * 31) + runtimeBottomUiTunerSettings.PositionStep.GetHashCode();
                hash = (hash * 31) + runtimeBottomUiTunerSettings.SizeStep.GetHashCode();
                return hash;
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

                cachedUiSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                cachedUiSprite.name = "RuntimeWhiteUiSprite";
                cachedUiSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return cachedUiSprite;
        }

        [System.Serializable]
        private sealed class BattleBottomUiLayoutSettings
        {
            public Vector2 BottomCardPosition = new Vector2(0f, 150f);
            public Vector2 BottomCardSize = new Vector2(980f, 360f);
            public Vector2 RollLogPosition = new Vector2(0f, 300f);
            public Vector2 RollLogSize = new Vector2(900f, 90f);
            public Vector2 BattleLogPosition = new Vector2(0f, 172f);
            public Vector2 BattleLogSize = new Vector2(900f, 170f);
            public Vector2 PlayerSkillCardPosition = new Vector2(-250f, -622f);
            public Vector2 PlayerSkillCardSize = new Vector2(466f, 167f);
            public Vector2 EnemySkillCardPosition = new Vector2(250f, -622f);
            public Vector2 EnemySkillCardSize = new Vector2(466f, 167f);
        }

        [System.Serializable]
        private sealed class RuntimeBottomUiTunerSettings
        {
            public bool ShowRuntimeTuner = true;
            public Vector2 PanelPosition = new Vector2(-400f, -620f);
            public Vector2 PanelSize = new Vector2(240f, 220f);
            public float PositionStep = 16f;
            public float SizeStep = 20f;
        }

        private enum BottomUiTunerTarget
        {
            BottomCard,
            RollLog,
            BattleLog,
            PlayerSkillCard,
            EnemySkillCard
        }

        private sealed class RuntimeBottomUiTunerView
        {
            public RectTransform Root;
            public TMP_Text TitleText;
            public TMP_Text InfoText;
        }
    }
}
