using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DiceRogue
{
    public class MainMenuSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private bool useRuntimeLayout = true;
        [SerializeField] private bool useEditorUiLayout = false;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text configText;
        [SerializeField] private TMP_Text tapToStartText;
        [SerializeField] private Button startRunButton;
        [SerializeField] private Button debugBattleButton;
        [SerializeField] private Button debugRewardButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private float startFadeDuration = 0.35f;
        [SerializeField] private float tapToStartBlinkDuration = 1.2f;
        [SerializeField] [Range(0f, 1f)] private float tapToStartMinAlpha = 0.35f;

        private GameRunManager runManager;
        private Image startFadeOverlay;
        private TMP_Text resolvedTapToStartText;
        private Color tapToStartBaseColor = Color.white;
        private bool isStartingRun;

        private void Awake()
        {
            UIInputSystemHelper.EnsureEventSystem();
            runManager = GameRunManager.EnsureInstance(runConfig);
            ApplyLayoutIfEnabled();
            resolvedTapToStartText = ResolveTapToStartText();
            CacheTapToStartColor();
            HideLegacyButtons();
        }

        private void OnEnable()
        {
            ApplyLayoutIfEnabled();
            ResetStartFadeOverlay();
            isStartingRun = false;
            resolvedTapToStartText = ResolveTapToStartText();
            CacheTapToStartColor();
            SetTapToStartAlpha(1f);
            HideLegacyButtons();
        }

        private void Update()
        {
            UpdateTapToStartBlink();

            if (isStartingRun)
            {
                return;
            }

            if (WasStartTapTriggered())
            {
                OnStartRunRequested();
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

            SceneUILayoutHelper.EnsureFullscreenImage(canvas.transform, "RuntimeMainMenuBackdrop", new Color(0.08f, 0.12f, 0.2f, 1f));
            SceneUILayoutHelper.EnsurePanel(
                canvas.transform,
                "RuntimeMainMenuHeroCard",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -108f),
                new Vector2(980f, 300f),
                new Color(0.11f, 0.17f, 0.29f, 0.94f));

            if (titleText != null && titleText.transform is RectTransform titleRect)
            {
                SceneUILayoutHelper.SetRect(
                    titleRect,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -126f),
                    new Vector2(920f, 96f));
                SceneUILayoutHelper.StyleText(titleText, 88f, TextAlignmentOptions.Center, FontStyles.Bold);
                titleText.color = Color.white;
            }

            if (statusText != null && statusText.transform is RectTransform statusRect)
            {
                SceneUILayoutHelper.SetRect(
                    statusRect,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -218f),
                    new Vector2(860f, 96f));
                SceneUILayoutHelper.StyleText(statusText, 26f, TextAlignmentOptions.Center, FontStyles.Bold);
                statusText.color = new Color(0.85f, 0.92f, 1f, 1f);
            }

            if (configText != null && configText.transform is RectTransform configRect)
            {
                SceneUILayoutHelper.SetRect(
                    configRect,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -274f),
                    new Vector2(860f, 84f));
                SceneUILayoutHelper.StyleText(configText, 22f, TextAlignmentOptions.Center);
                configText.color = new Color(0.74f, 0.84f, 0.96f, 1f);
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

        private void OnStartRunRequested()
        {
            if (isStartingRun)
            {
                return;
            }

            StartCoroutine(FadeOutAndStartRun());
        }

        private IEnumerator FadeOutAndStartRun()
        {
            isStartingRun = true;
            SetMenuButtonsInteractable(false);

            var overlay = EnsureStartFadeOverlay();
            if (overlay == null)
            {
                runManager.StartRunFromMenu();
                yield break;
            }

            var color = overlay.color;
            color.a = 0f;
            overlay.color = color;
            overlay.gameObject.SetActive(true);

            var duration = Mathf.Max(0.05f, startFadeDuration);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Clamp01(elapsed / duration);
                overlay.color = color;
                yield return null;
            }

            color.a = 1f;
            overlay.color = color;
            runManager.StartRunFromMenu();
        }

        private void SetMenuButtonsInteractable(bool interactable)
        {
            if (startRunButton != null)
            {
                startRunButton.interactable = interactable;
            }

            if (debugBattleButton != null)
            {
                debugBattleButton.interactable = interactable;
            }

            if (debugRewardButton != null)
            {
                debugRewardButton.interactable = interactable;
            }

            if (quitButton != null)
            {
                quitButton.interactable = interactable;
            }
        }

        private Image EnsureStartFadeOverlay()
        {
            var canvas = SceneUILayoutHelper.FindRootCanvas();
            if (canvas == null)
            {
                return null;
            }

            if (startFadeOverlay == null)
            {
                startFadeOverlay = SceneUILayoutHelper.EnsureFullscreenImage(canvas.transform, "RuntimeStartFadeOverlay", new Color(0f, 0f, 0f, 0f));
                startFadeOverlay.color = new Color(0f, 0f, 0f, 0f);
                startFadeOverlay.raycastTarget = true;
            }

            return startFadeOverlay;
        }

        private void ResetStartFadeOverlay()
        {
            var overlay = EnsureStartFadeOverlay();
            if (overlay == null)
            {
                return;
            }

            var color = overlay.color;
            color.a = 0f;
            overlay.color = color;
            overlay.gameObject.SetActive(false);
            SetMenuButtonsInteractable(true);
        }

        private TMP_Text ResolveTapToStartText()
        {
            if (tapToStartText != null)
            {
                return tapToStartText;
            }

            if (statusText != null)
            {
                return statusText;
            }

            var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var text in texts)
            {
                if (text == null)
                {
                    continue;
                }

                var objectName = text.gameObject.name;
                if (objectName.IndexOf("tap", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    objectName.IndexOf("start", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return text;
                }
            }

            return null;
        }

        private void CacheTapToStartColor()
        {
            if (resolvedTapToStartText == null)
            {
                return;
            }

            tapToStartBaseColor = resolvedTapToStartText.color;
        }

        private void UpdateTapToStartBlink()
        {
            if (resolvedTapToStartText == null)
            {
                return;
            }

            var duration = Mathf.Max(0.1f, tapToStartBlinkDuration);
            var phase = Mathf.PingPong(Time.unscaledTime / duration, 1f);
            var alpha = Mathf.Lerp(tapToStartMinAlpha, 1f, phase);
            SetTapToStartAlpha(alpha);
        }

        private void SetTapToStartAlpha(float alpha)
        {
            if (resolvedTapToStartText == null)
            {
                return;
            }

            var color = tapToStartBaseColor;
            color.a = alpha;
            resolvedTapToStartText.color = color;
        }

        private bool WasStartTapTriggered()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        private void HideLegacyButtons()
        {
            HideLegacyButton(startRunButton);
            HideLegacyButton(debugBattleButton);
            HideLegacyButton(debugRewardButton);
            HideLegacyButton(quitButton);
        }

        private static void HideLegacyButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = false;
            button.gameObject.SetActive(false);
        }
    }
}
