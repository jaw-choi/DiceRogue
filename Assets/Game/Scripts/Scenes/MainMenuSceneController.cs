using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceRogue
{
    public class MainMenuSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private bool useRuntimeLayout = true;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text configText;
        [SerializeField] private Button startRunButton;
        [SerializeField] private Button debugBattleButton;
        [SerializeField] private Button debugRewardButton;
        [SerializeField] private Button quitButton;

        private GameRunManager runManager;
        private RectTransform buttonStackRoot;

        private void Awake()
        {
            UIInputSystemHelper.EnsureEventSystem();
            runManager = GameRunManager.EnsureInstance(runConfig);
            ApplyLayoutIfEnabled();

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
            ApplyLayoutIfEnabled();

            if (titleText != null)
            {
                titleText.text = "다이스 로그";
            }

            if (statusText != null)
            {
                statusText.text = runManager.LastRunMessage;
            }

            if (configText != null)
            {
                configText.text = "주사위를 성장시키며 보스를 쓰러뜨리세요";
            }

            SceneUILayoutHelper.SetButtonLabel(startRunButton, "새 게임 시작");
            SceneUILayoutHelper.SetButtonLabel(debugBattleButton, "전투 테스트");
            SceneUILayoutHelper.SetButtonLabel(debugRewardButton, "보상 테스트");
            SceneUILayoutHelper.SetButtonLabel(quitButton, "게임 종료");
        }

        private void ApplyLayout()
        {
            var canvas = SceneUILayoutHelper.FindRootCanvas();
            SceneUILayoutHelper.ConfigureCanvas(canvas);

            if (canvas == null)
            {
                return;
            }

            SceneUILayoutHelper.EnsureFullscreenImage(canvas.transform, "RuntimeMainMenuBackdrop", new Color(0.08f, 0.12f, 0.2f, 1f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeMainMenuHeroCard", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(980f, 300f), new Color(0.11f, 0.17f, 0.29f, 0.94f));
            SceneUILayoutHelper.EnsurePanel(canvas.transform, "RuntimeMainMenuButtonCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(940f, 900f), new Color(0.13f, 0.19f, 0.31f, 0.9f));

            if (titleText != null && titleText.transform is RectTransform titleRect)
            {
                SceneUILayoutHelper.SetRect(titleRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -126f), new Vector2(920f, 96f));
                SceneUILayoutHelper.StyleText(titleText, 88f, TextAlignmentOptions.Center, FontStyles.Bold);
                titleText.color = Color.white;
            }

            if (statusText != null && statusText.transform is RectTransform statusRect)
            {
                SceneUILayoutHelper.SetRect(statusRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -218f), new Vector2(860f, 96f));
                SceneUILayoutHelper.StyleText(statusText, 26f, TextAlignmentOptions.Center, FontStyles.Bold);
                statusText.color = new Color(0.85f, 0.92f, 1f, 1f);
            }

            if (configText != null && configText.transform is RectTransform configRect)
            {
                SceneUILayoutHelper.SetRect(configRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -274f), new Vector2(860f, 84f));
                SceneUILayoutHelper.StyleText(configText, 22f, TextAlignmentOptions.Center);
                configText.color = new Color(0.74f, 0.84f, 0.96f, 1f);
            }

            buttonStackRoot = SceneUILayoutHelper.EnsureVerticalListRoot(
                canvas.transform,
                "RuntimeMainMenuButtonStack",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 24f),
                new Vector2(760f, 760f),
                30f,
                new RectOffset(0, 0, 18, 18));

            LayoutButton(startRunButton, new Color(0.18f, 0.55f, 0.92f));
            LayoutButton(debugBattleButton, new Color(0.91f, 0.52f, 0.18f));
            LayoutButton(debugRewardButton, new Color(0.36f, 0.68f, 0.31f));
            LayoutButton(quitButton, new Color(0.45f, 0.45f, 0.45f));
        }

        private void LayoutButton(Button button, Color backgroundColor)
        {
            if (button == null || buttonStackRoot == null)
            {
                return;
            }

            if (button.transform.parent != buttonStackRoot)
            {
                button.transform.SetParent(buttonStackRoot, false);
            }

            SceneUILayoutHelper.StyleButton(button, new Vector2(680f, 138f), 34f, backgroundColor, Color.white);
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
    }
}
