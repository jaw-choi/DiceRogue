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

        public static void Stretch(RectTransform rectTransform, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            if (rectTransform == null)
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

        public static void StyleButton(Button button, Vector2 size, float labelFontSize, Color backgroundColor, Color labelColor)
        {
            if (button == null)
            {
                return;
            }

            if (button.transform is RectTransform rectTransform)
            {
                rectTransform.sizeDelta = size;
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
            }

            var layoutElement = button.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = button.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minWidth = size.x;
            layoutElement.preferredWidth = size.x;
            layoutElement.minHeight = size.y;
            layoutElement.preferredHeight = size.y;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

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
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.pivot = new Vector2(0.5f, 0.5f);
                    labelRect.offsetMin = new Vector2(24f, 12f);
                    labelRect.offsetMax = new Vector2(-24f, -12f);
                    labelRect.localScale = Vector3.one;
                    labelRect.localRotation = Quaternion.identity;
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

        public static Image EnsureFullscreenImage(Transform parent, string objectName, Color color, int siblingIndex = 0)
        {
            var rectTransform = EnsureRuntimeRect(parent, objectName);
            Stretch(rectTransform);

            var image = GetOrAddImage(rectTransform.gameObject);
            image.sprite = GetRuntimeWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;

            rectTransform.SetSiblingIndex(Mathf.Max(0, siblingIndex));
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
            Color color)
        {
            var rectTransform = EnsureRuntimeRect(parent, objectName);
            SetRect(rectTransform, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);

            var image = GetOrAddImage(rectTransform.gameObject);
            image.sprite = GetRuntimeWhiteSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            rectTransform.SetSiblingIndex(Mathf.Min(1, rectTransform.parent.childCount - 1));
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
}
