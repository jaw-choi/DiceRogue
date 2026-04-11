using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiceRogue
{
    public static class DynamicButtonLayoutHelper
    {
        public static void EnsureVerticalButtonLayout(Transform root, Button template, float spacing = 16f, float minHeight = 100f)
        {
            if (root == null)
            {
                return;
            }

            if (root is RectTransform rootRectTransform)
            {
                var layoutGroup = root.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = root.gameObject.AddComponent<VerticalLayoutGroup>();
                }

                layoutGroup.spacing = spacing;
                layoutGroup.childAlignment = TextAnchor.UpperCenter;
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = true;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = false;

                var fitter = root.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                {
                    fitter = root.gameObject.AddComponent<ContentSizeFitter>();
                }

                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                rootRectTransform.localScale = Vector3.one;
            }

            if (template == null)
            {
                return;
            }

            var templateRect = template.transform as RectTransform;
            if (templateRect != null)
            {
                templateRect.anchorMin = new Vector2(0.5f, 1f);
                templateRect.anchorMax = new Vector2(0.5f, 1f);
                templateRect.pivot = new Vector2(0.5f, 1f);
                templateRect.anchoredPosition = Vector2.zero;
                templateRect.localScale = Vector3.one;
                templateRect.localRotation = Quaternion.identity;
            }

            var layoutElement = template.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = template.gameObject.AddComponent<LayoutElement>();
            }

            if (layoutElement.minHeight <= 0f)
            {
                layoutElement.minHeight = minHeight;
            }
        }

        public static void ArrangeChildrenVertically(Transform root, Button template, float spacing = 16f)
        {
            if (root == null || template == null)
            {
                return;
            }

            if (root is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                Canvas.ForceUpdateCanvases();
                return;
            }

            var templateRect = template.transform as RectTransform;
            var height = templateRect != null && templateRect.rect.height > 0f ? templateRect.rect.height : 100f;

            var currentY = 0f;

            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (child == template.transform || !child.gameObject.activeSelf)
                {
                    continue;
                }

                if (child is RectTransform childRect)
                {
                    childRect.anchorMin = new Vector2(0.5f, 1f);
                    childRect.anchorMax = new Vector2(0.5f, 1f);
                    childRect.pivot = new Vector2(0.5f, 1f);
                    childRect.anchoredPosition = new Vector2(0f, -currentY);
                    childRect.localScale = Vector3.one;
                    childRect.localRotation = Quaternion.identity;
                }
                else
                {
                    child.localPosition = new Vector3(0f, -currentY, 0f);
                    child.localScale = Vector3.one;
                    child.localRotation = Quaternion.identity;
                }

                currentY += height + spacing;
            }
        }
    }

    public static class SceneUILayoutHelper
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
        private const float TextScaleMultiplier = 1.5f;
        private static Sprite runtimeWhiteSprite;

        public static Canvas FindRootCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var index = 0; index < canvases.Length; index++)
            {
                if (canvases[index] != null && canvases[index].isRootCanvas && canvases[index].gameObject.scene.IsValid())
                {
                    return canvases[index];
                }
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        public static void ConfigureCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                return;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        public static RectTransform EnsureRuntimeRect(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                if (parent.GetChild(index) is RectTransform existing && existing.name == objectName)
                {
                    return existing;
                }
            }

            var gameObject = new GameObject(objectName, typeof(RectTransform));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }

        public static void Stretch(RectTransform rectTransform, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f, bool preserveExistingLayout = false)
        {
            if (rectTransform == null)
            {
                return;
            }

            if (preserveExistingLayout && HasMeaningfulExistingLayout(rectTransform))
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        public static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        public static void SetRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            bool preserveExistingLayout)
        {
            if (rectTransform == null)
            {
                return;
            }

            if (preserveExistingLayout && HasMeaningfulExistingLayout(rectTransform))
            {
                return;
            }

            SetRect(rectTransform, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
        }

        public static Vector2 ResolveRectSize(RectTransform rectTransform, Vector2 fallbackSize)
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

        private static bool HasMeaningfulExistingLayout(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return false;
            }

            if (Mathf.Abs(rectTransform.sizeDelta.x) > 0.01f || Mathf.Abs(rectTransform.sizeDelta.y) > 0.01f)
            {
                return true;
            }

            if (rectTransform.anchoredPosition.sqrMagnitude > 0.01f)
            {
                return true;
            }

            var centeredAnchor = new Vector2(0.5f, 0.5f);
            if ((rectTransform.anchorMin - centeredAnchor).sqrMagnitude > 0.0001f || (rectTransform.anchorMax - centeredAnchor).sqrMagnitude > 0.0001f)
            {
                return true;
            }

            if ((rectTransform.pivot - centeredAnchor).sqrMagnitude > 0.0001f)
            {
                return true;
            }

            if ((rectTransform.localScale - Vector3.one).sqrMagnitude > 0.0001f)
            {
                return true;
            }

            return Quaternion.Angle(rectTransform.localRotation, Quaternion.identity) > 0.01f;
        }

        public static void StyleText(TMP_Text text, float fontSize, TextAlignmentOptions alignment, FontStyles fontStyle = FontStyles.Normal)
        {
            if (text == null)
            {
                return;
            }

            var scaledFontSize = fontSize * TextScaleMultiplier;
            text.fontSize = scaledFontSize;
            text.fontSizeMax = scaledFontSize;
            text.fontSizeMin = Mathf.Max(18f, scaledFontSize * 0.6f);
            text.enableAutoSizing = true;
            text.alignment = alignment;
            text.fontStyle = fontStyle;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        public static void StyleButton(Button button, Vector2 size, float labelFontSize, Color backgroundColor, Color labelColor, bool preserveExistingLayout = false)
        {
            if (button == null)
            {
                return;
            }

            if (button.transform is RectTransform rectTransform)
            {
                if (!preserveExistingLayout || !HasMeaningfulExistingLayout(rectTransform))
                {
                    rectTransform.sizeDelta = size;
                    rectTransform.localScale = Vector3.one;
                    rectTransform.localRotation = Quaternion.identity;
                }
            }

            var layoutElement = button.GetComponent<LayoutElement>();
            if (!preserveExistingLayout || layoutElement == null)
            {
                layoutElement ??= button.gameObject.AddComponent<LayoutElement>();

                if (!preserveExistingLayout)
                {
                    layoutElement.minWidth = size.x;
                    layoutElement.preferredWidth = size.x;
                    layoutElement.minHeight = size.y;
                    layoutElement.preferredHeight = size.y;
                    layoutElement.flexibleWidth = 0f;
                    layoutElement.flexibleHeight = 0f;
                }
            }

            var image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = backgroundColor;
                image.type = Image.Type.Simple;
            }

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                if (label.transform is RectTransform labelRect)
                {
                    if (!preserveExistingLayout || !HasMeaningfulExistingLayout(labelRect))
                    {
                        labelRect.anchorMin = Vector2.zero;
                        labelRect.anchorMax = Vector2.one;
                        labelRect.pivot = new Vector2(0.5f, 0.5f);
                        labelRect.offsetMin = new Vector2(24f, 12f);
                        labelRect.offsetMax = new Vector2(-24f, -12f);
                        labelRect.localScale = Vector3.one;
                        labelRect.localRotation = Quaternion.identity;
                    }
                }

                StyleText(label, labelFontSize, TextAlignmentOptions.Center, FontStyles.Bold);
                label.color = labelColor;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        public static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
            }
        }

        public static Image EnsureFullscreenImage(Transform parent, string objectName, Color color, int siblingIndex = 0, bool preserveExistingLayout = false)
        {
            var rectTransform = EnsureRuntimeRect(parent, objectName);
            Stretch(rectTransform, preserveExistingLayout: preserveExistingLayout);

            var image = GetOrAddImage(rectTransform.gameObject);
            image.sprite = GetRuntimeWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Image EnsurePanel(
            Transform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color,
            bool preserveExistingLayout = false)
        {
            var rectTransform = EnsureRuntimeRect(parent, objectName);
            SetRect(rectTransform, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, preserveExistingLayout);

            var image = GetOrAddImage(rectTransform.gameObject);
            image.sprite = GetRuntimeWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static RectTransform EnsureVerticalListRoot(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, float spacing, RectOffset padding)
        {
            var rectTransform = EnsureRuntimeRect(parent, objectName);
            SetRect(rectTransform, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);

            var layoutGroup = rectTransform.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layoutGroup.spacing = spacing;
            layoutGroup.padding = padding ?? new RectOffset();
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            var fitter = rectTransform.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = rectTransform.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rectTransform;
        }

        private static Image GetOrAddImage(GameObject target)
        {
            var image = target.GetComponent<Image>();
            return image != null ? image : target.AddComponent<Image>();
        }

        private static Sprite GetRuntimeWhiteSprite()
        {
            if (runtimeWhiteSprite == null)
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

                runtimeWhiteSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                runtimeWhiteSprite.name = "RuntimeSharedWhiteSprite";
                runtimeWhiteSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return runtimeWhiteSprite;
        }
    }

    public static class SkillIconLibrary
    {
        private const string SkillIconResourcePath = "skill_icon";

        private static readonly Dictionary<string, int> IconIndexBySkillId = new Dictionary<string, int>
        {
            { "basic_attack", 0 },
            { "heavy_slash", 0 },
            { "guard", 1 },
            { "focused_defense", 2 },
            { "fortify", 2 },
            { "defensive_stance", 3 },
            { "counter", 4 },
            { "shield_burst", 5 },
            { "blood_slash", 6 },
            { "rage_burst", 7 },
            { "fury", 7 },
            { "savage_strike", 8 },
            { "vampiric_slash", 9 }
        };

        private static Dictionary<string, Sprite> spritesByName;

        public static Sprite GetSkillIcon(SkillDefinition skill)
        {
            return skill == null ? null : GetSkillIcon(skill.Id);
        }

        public static Sprite GetSkillIcon(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return null;
            }

            EnsureLoaded();
            if (!IconIndexBySkillId.TryGetValue(skillId, out var iconIndex))
            {
                return null;
            }

            return spritesByName != null && spritesByName.TryGetValue($"skill_icon_{iconIndex}", out var sprite)
                ? sprite
                : null;
        }

        private static void EnsureLoaded()
        {
            if (spritesByName != null)
            {
                return;
            }

            spritesByName = Resources.LoadAll<Sprite>(SkillIconResourcePath)
                .GroupBy(sprite => sprite.name)
                .ToDictionary(group => group.Key, group => group.First());
        }
    }

    public static class MonsterSpriteLibrary
    {
        private const string MonsterResourcePath = "monster";

        private static readonly Dictionary<string, string> SpriteNameByCombatantId = new Dictionary<string, string>
        {
            { "slime", "monster_1" },
            { "goblin", "monster_2" },
            { "summoned_goblin", "monster_2" },
            { "golem", "monster_3" },
            { "shaman", "monster_4" }
        };

        private static Dictionary<string, Sprite> spritesByName;

        public static Sprite GetBattlefieldSprite()
        {
            return GetSpriteByName("monster_0");
        }

        public static Sprite GetCombatantSprite(string combatantId)
        {
            if (string.IsNullOrWhiteSpace(combatantId))
            {
                return null;
            }

            return !SpriteNameByCombatantId.TryGetValue(combatantId, out var spriteName)
                ? null
                : GetSpriteByName(spriteName);
        }

        private static Sprite GetSpriteByName(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return null;
            }

            EnsureLoaded();
            return spritesByName != null && spritesByName.TryGetValue(spriteName, out var sprite)
                ? sprite
                : null;
        }

        private static void EnsureLoaded()
        {
            if (spritesByName != null)
            {
                return;
            }

            spritesByName = Resources.LoadAll<Sprite>(MonsterResourcePath)
                .GroupBy(sprite => sprite.name)
                .ToDictionary(group => group.Key, group => group.First());
        }
    }

    public static class UiFrameSpriteLibrary
    {
        private const string UiResourcePath = "UI";

        private static Dictionary<string, Sprite> spritesByName;

        public static Sprite GetTopHudSprite()
        {
            return GetSpriteByName("UI_0");
        }

        public static Sprite GetDicePanelSprite()
        {
            return GetSpriteByName("UI_1");
        }

        public static Sprite GetSkillDetailSprite()
        {
            return GetSpriteByName("UI_2");
        }

        public static Vector2 ResolveScaledSize(Sprite sprite, Vector2 scale, Vector2 extraSize)
        {
            if (sprite == null)
            {
                return extraSize;
            }

            return Vector2.Scale(sprite.rect.size, new Vector2(Mathf.Max(0.05f, scale.x), Mathf.Max(0.05f, scale.y))) + extraSize;
        }

        private static Sprite GetSpriteByName(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return null;
            }

            EnsureLoaded();
            return spritesByName != null && spritesByName.TryGetValue(spriteName, out var sprite)
                ? sprite
                : null;
        }

        private static void EnsureLoaded()
        {
            if (spritesByName != null)
            {
                return;
            }

            spritesByName = Resources.LoadAll<Sprite>(UiResourcePath)
                .GroupBy(sprite => sprite.name)
                .ToDictionary(group => group.Key, group => group.First());
        }
    }

    public sealed class RuntimeSkillCardView
    {
        public RectTransform Root;
        public Image BackgroundImage;
        public Image IconImage;
        public TMP_Text TitleText;
        public TMP_Text BodyText;
    }

    public static class RuntimeSkillCardFactory
    {
        public static RuntimeSkillCardView EnsureSkillCard(
            Transform parent,
            string objectName,
            TMP_Text sampleText,
            Vector2 anchoredPosition,
            Vector2 size,
            bool preserveExistingLayout = false)
        {
            var root = SceneUILayoutHelper.EnsureRuntimeRect(parent, objectName);
            SceneUILayoutHelper.SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size, preserveExistingLayout);

            var background = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            background.sprite = UiFrameSpriteLibrary.GetSkillDetailSprite();
            background.type = Image.Type.Simple;
            background.color = Color.white;
            background.preserveAspect = false;
            background.raycastTarget = false;

            var iconSize = Mathf.Min(size.y * 0.52f, size.x * 0.23f);
            var iconRoot = SceneUILayoutHelper.EnsureRuntimeRect(root, "Icon");
            SceneUILayoutHelper.SetRect(
                iconRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-size.x * 0.335f, 0f),
                new Vector2(iconSize, iconSize),
                preserveExistingLayout);
            var iconImage = iconRoot.GetComponent<Image>() ?? iconRoot.gameObject.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var titleRoot = SceneUILayoutHelper.EnsureRuntimeRect(root, "Title");
            SceneUILayoutHelper.SetRect(
                titleRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(size.x * 0.13f, size.y * 0.18f),
                new Vector2(size.x * 0.56f, size.y * 0.23f),
                preserveExistingLayout);
            var titleText = titleRoot.GetComponent<TextMeshProUGUI>() ?? titleRoot.gameObject.AddComponent<TextMeshProUGUI>();
            CopyFont(sampleText, titleText);
            SceneUILayoutHelper.StyleText(titleText, 20f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            titleText.color = new Color(0.16f, 0.12f, 0.05f, 1f);
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Ellipsis;

            var bodyRoot = SceneUILayoutHelper.EnsureRuntimeRect(root, "Body");
            SceneUILayoutHelper.SetRect(
                bodyRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(size.x * 0.13f, -size.y * 0.05f),
                new Vector2(size.x * 0.58f, size.y * 0.45f),
                preserveExistingLayout);
            var bodyText = bodyRoot.GetComponent<TextMeshProUGUI>() ?? bodyRoot.gameObject.AddComponent<TextMeshProUGUI>();
            CopyFont(sampleText, bodyText);
            SceneUILayoutHelper.StyleText(bodyText, 15f, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            bodyText.color = new Color(0.2f, 0.15f, 0.08f, 1f);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.overflowMode = TextOverflowModes.Ellipsis;

            return new RuntimeSkillCardView
            {
                Root = root,
                BackgroundImage = background,
                IconImage = iconImage,
                TitleText = titleText,
                BodyText = bodyText
            };
        }

        public static void ApplySkillPresentation(
            Image iconImage,
            TMP_Text titleText,
            TMP_Text bodyText,
            DiceFaceState face,
            string titlePrefix,
            string emptyTitle,
            string emptyBody)
        {
            var skill = face?.Skill;
            var icon = SkillIconLibrary.GetSkillIcon(skill);
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
            }

            if (skill == null)
            {
                if (titleText != null)
                {
                    titleText.text = emptyTitle;
                }

                if (bodyText != null)
                {
                    bodyText.text = emptyBody;
                }

                return;
            }

            if (titleText != null)
            {
                var levelLabel = face.UpgradeLevel > 0 ? $" Lv.{face.UpgradeLevel}" : string.Empty;
                titleText.text = string.IsNullOrWhiteSpace(titlePrefix)
                    ? $"{skill.DisplayName}{levelLabel}"
                    : $"{titlePrefix} | {skill.DisplayName}{levelLabel}";
            }

            if (bodyText != null)
            {
                bodyText.text = BuildSkillBody(face);
            }
        }

        private static string BuildSkillBody(DiceFaceState face)
        {
            var skill = face?.Skill;
            if (skill == null)
            {
                return string.Empty;
            }

            var summary = skill.GetSummary(face.UpgradeLevel)?.Trim();
            var description = skill.Description?.Trim();

            if (string.IsNullOrWhiteSpace(description))
            {
                return summary;
            }

            if (string.IsNullOrWhiteSpace(summary) || string.Equals(summary, description))
            {
                return description;
            }

            return $"{summary}\n{description}";
        }

        private static void CopyFont(TMP_Text source, TMP_Text target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.font = source.font;
            target.fontSharedMaterial = source.fontSharedMaterial;
        }
    }
}
