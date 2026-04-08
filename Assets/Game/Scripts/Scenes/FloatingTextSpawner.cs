using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DiceRogue
{
    public class FloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasRoot;
        [SerializeField] private TMP_Text floatingTextPrefab;
        [SerializeField] private Vector2 floatOffset = new Vector2(0f, 24f);
        [SerializeField] private Vector2 travelDistance = new Vector2(0f, 80f);
        [SerializeField] private float duration = 0.75f;
        [SerializeField] private Vector2 stackStep = new Vector2(26f, 34f);

        private readonly Dictionary<int, int> activeStacksByAnchor = new Dictionary<int, int>();

        public void Configure(RectTransform runtimeCanvasRoot, TMP_Text runtimeFloatingTextPrefab)
        {
            canvasRoot = runtimeCanvasRoot;
            floatingTextPrefab = runtimeFloatingTextPrefab;
        }

        public void Spawn(RectTransform anchor, string message, Color color)
        {
            if (canvasRoot == null || floatingTextPrefab == null || anchor == null)
            {
                return;
            }

            var anchorKey = anchor.GetInstanceID();
            var stackIndex = 0;
            if (activeStacksByAnchor.TryGetValue(anchorKey, out var activeCount))
            {
                stackIndex = activeCount;
            }

            activeStacksByAnchor[anchorKey] = stackIndex + 1;

            var instance = Instantiate(floatingTextPrefab, canvasRoot);
            instance.gameObject.SetActive(true);
            instance.text = message;
            instance.color = color;

            if (instance.transform is RectTransform rectTransform)
            {
                var screenPoint = RectTransformUtility.WorldToScreenPoint(null, anchor.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenPoint, null, out var localPoint);
                rectTransform.anchoredPosition = localPoint + floatOffset + GetStackOffset(stackIndex);
            }

            StartCoroutine(AnimateFloatingText(instance, anchorKey));
        }

        private IEnumerator AnimateFloatingText(TMP_Text instance, int anchorKey)
        {
            if (instance == null)
            {
                ReleaseStack(anchorKey);
                yield break;
            }

            var rectTransform = instance.transform as RectTransform;
            if (rectTransform == null)
            {
                ReleaseStack(anchorKey);
                yield break;
            }

            var start = rectTransform.anchoredPosition;
            var end = start + travelDistance;
            var elapsed = 0f;
            var startColor = instance.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                rectTransform.anchoredPosition = Vector2.Lerp(start, end, t);
                instance.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
                yield return null;
            }

            Destroy(instance.gameObject);
            ReleaseStack(anchorKey);
        }

        private Vector2 GetStackOffset(int stackIndex)
        {
            if (stackIndex <= 0)
            {
                return Vector2.zero;
            }

            var horizontalDirection = (stackIndex % 2 == 0) ? 1f : -1f;
            var horizontalStep = Mathf.Ceil(stackIndex * 0.5f) * stackStep.x * horizontalDirection;
            var verticalStep = stackIndex * stackStep.y;
            return new Vector2(horizontalStep, verticalStep);
        }

        private void ReleaseStack(int anchorKey)
        {
            if (!activeStacksByAnchor.TryGetValue(anchorKey, out var activeCount))
            {
                return;
            }

            activeCount -= 1;
            if (activeCount <= 0)
            {
                activeStacksByAnchor.Remove(anchorKey);
                return;
            }

            activeStacksByAnchor[anchorKey] = activeCount;
        }
    }
}
