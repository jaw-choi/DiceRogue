using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceRogue
{
    public class UnitView : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private RectTransform animatedRoot;
        [SerializeField] private RectTransform popupAnchor;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image spriteImage;
        [SerializeField] private Image highlightImage;
        [SerializeField] private Slider hpBar;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text shieldArmorText;
        [SerializeField] private TMP_Text rageText;
        [SerializeField] private Sprite fallbackSprite;
        [SerializeField] private Color defaultSpriteColor = Color.white;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.45f, 0.45f);
        [SerializeField] private Color healFlashColor = new Color(0.45f, 1f, 0.55f);
        [SerializeField] private Color shieldFlashColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField] private Color berserkFlashColor = new Color(1f, 0.45f, 0.9f);

        private static Sprite generatedFallbackSprite;
        private Vector2 initialAnchoredPosition;
        private Vector3 initialScale;
        private Color baseColor;

        public CombatantRuntimeState BoundState { get; private set; }
        public RectTransform PopupAnchor => popupAnchor != null ? popupAnchor : (RectTransform)transform;

        private void Awake()
        {
            if (animatedRoot == null)
            {
                animatedRoot = transform as RectTransform;
            }

            if (popupAnchor == null)
            {
                popupAnchor = transform as RectTransform;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (animatedRoot != null)
            {
                initialAnchoredPosition = animatedRoot.anchoredPosition;
                initialScale = animatedRoot.localScale;
            }

            if (spriteImage != null)
            {
                baseColor = spriteImage.color;
                spriteImage.preserveAspect = true;
            }
        }

        public void ConfigureRuntime(
            RectTransform runtimeAnimatedRoot,
            RectTransform runtimePopupAnchor,
            CanvasGroup runtimeCanvasGroup,
            Image runtimeSpriteImage,
            Image runtimeHighlightImage,
            Slider runtimeHpBar,
            TMP_Text runtimeNameText,
            TMP_Text runtimeHpText,
            TMP_Text runtimeShieldArmorText,
            TMP_Text runtimeRageText)
        {
            animatedRoot = runtimeAnimatedRoot;
            popupAnchor = runtimePopupAnchor;
            canvasGroup = runtimeCanvasGroup;
            spriteImage = runtimeSpriteImage;
            highlightImage = runtimeHighlightImage;
            hpBar = runtimeHpBar;
            nameText = runtimeNameText;
            hpText = runtimeHpText;
            shieldArmorText = runtimeShieldArmorText;
            rageText = runtimeRageText;

            if (animatedRoot != null)
            {
                initialAnchoredPosition = animatedRoot.anchoredPosition;
                initialScale = animatedRoot.localScale;
            }

            if (spriteImage != null)
            {
                spriteImage.sprite = ResolveDisplaySprite(null);
                spriteImage.preserveAspect = true;
                baseColor = spriteImage.color;
            }
        }

        public void Bind(CombatantRuntimeState combatantState)
        {
            BoundState = combatantState;
            gameObject.SetActive(combatantState != null);

            if (combatantState == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = combatantState.DisplayName;
            }

            if (spriteImage != null)
            {
                spriteImage.sprite = ResolveDisplaySprite(combatantState);
                spriteImage.color = combatantState.Template.BattleTint == default ? defaultSpriteColor : combatantState.Template.BattleTint;
                baseColor = spriteImage.color;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (highlightImage != null)
            {
                highlightImage.enabled = false;
            }

            if (animatedRoot != null)
            {
                animatedRoot.anchoredPosition = initialAnchoredPosition;
                animatedRoot.localScale = initialScale == Vector3.zero ? Vector3.one : initialScale;
            }

            Refresh();
        }

        public void Refresh()
        {
            if (BoundState == null)
            {
                return;
            }

            if (hpBar != null)
            {
                hpBar.value = BoundState.MaxHp <= 0 ? 0f : (float)BoundState.CurrentHp / BoundState.MaxHp;
            }

            if (hpText != null)
            {
                hpText.text = $"HP {BoundState.CurrentHp}/{BoundState.MaxHp}";
            }

            if (shieldArmorText != null)
            {
                shieldArmorText.text = $"Shield {BoundState.Shield} / Armor {BoundState.Armor}";
            }

            if (rageText != null)
            {
                rageText.text = $"Rage {BoundState.Rage}";
            }
        }

        public void SetHighlighted(bool isHighlighted)
        {
            if (highlightImage != null)
            {
                highlightImage.enabled = isHighlighted;
            }
        }

        public IEnumerator PlayAttackLunge(Transform target, float duration = 0.18f, float distance = 48f)
        {
            if (animatedRoot == null)
            {
                yield break;
            }

            var start = initialAnchoredPosition;
            var direction = Vector2.right;

            if (target != null)
            {
                direction = ((Vector2)(target.position - animatedRoot.position)).normalized;
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = Vector2.right;
                }
            }

            var peak = start + direction * distance;
            yield return MoveAnchoredPosition(start, peak, duration);
            yield return MoveAnchoredPosition(peak, start, duration);
        }

        public IEnumerator PlayHitReaction(float duration = 0.22f)
        {
            yield return FlashAndPunch(hitFlashColor, 1.08f, duration);
        }

        public IEnumerator PlayShieldPulse(float duration = 0.25f)
        {
            yield return FlashAndPunch(shieldFlashColor, 1.06f, duration);
        }

        public IEnumerator PlayHealPulse(float duration = 0.25f)
        {
            yield return FlashAndPunch(healFlashColor, 1.06f, duration);
        }

        public IEnumerator PlayRagePulse(float duration = 0.25f)
        {
            yield return FlashAndPunch(new Color(1f, 0.65f, 0.2f), 1.06f, duration);
        }

        public IEnumerator PlayBerserkPulse(float duration = 0.32f)
        {
            yield return FlashAndPunch(berserkFlashColor, 1.1f, duration);
        }

        public IEnumerator PlayDeathFade(float duration = 0.45f)
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(false);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private IEnumerator FlashAndPunch(Color flashColor, float scaleMultiplier, float duration)
        {
            if (animatedRoot == null || spriteImage == null)
            {
                yield break;
            }

            var elapsed = 0f;
            var startScale = initialScale == Vector3.zero ? Vector3.one : initialScale;
            var peakScale = startScale * scaleMultiplier;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                var pulse = t <= 0.5f ? t / 0.5f : 1f - ((t - 0.5f) / 0.5f);
                spriteImage.color = Color.Lerp(baseColor, flashColor, pulse);
                animatedRoot.localScale = Vector3.Lerp(startScale, peakScale, pulse);
                yield return null;
            }

            spriteImage.color = baseColor;
            animatedRoot.localScale = startScale;
        }

        private IEnumerator MoveAnchoredPosition(Vector2 from, Vector2 to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                animatedRoot.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }

            animatedRoot.anchoredPosition = to;
        }

        private Sprite ResolveDisplaySprite(CombatantRuntimeState combatantState)
        {
            if (combatantState?.Template != null && combatantState.Template.BattleSprite != null)
            {
                return combatantState.Template.BattleSprite;
            }

            return fallbackSprite != null ? fallbackSprite : GetGeneratedFallbackSprite();
        }

        private static Sprite GetGeneratedFallbackSprite()
        {
            if (generatedFallbackSprite != null)
            {
                return generatedFallbackSprite;
            }

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[size * size];
            var radius = (size - 8) * 0.5f;
            var center = (size - 1) * 0.5f;
            var edgeFade = 3f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var alpha = Mathf.Clamp01((radius - distance) / edgeFade);
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            generatedFallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            generatedFallbackSprite.name = "RuntimeSpherePlaceholder";
            generatedFallbackSprite.hideFlags = HideFlags.HideAndDontSave;
            return generatedFallbackSprite;
        }
    }
}
