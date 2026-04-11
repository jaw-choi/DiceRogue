using UnityEngine;

namespace DiceRogue
{
    [System.Serializable]
    public class BattleHitEffectSettings
    {
        public GameObject prefab;
        public Vector3 localScale = new Vector3(0.34f, 0.34f, 0.34f);
        public float impactLerpFromActorToTarget = 0.78f;
        public Vector2 screenOffset = new Vector2(0f, 40f);
        public float depthOffsetTowardsCamera = 2f;
        public float durationOverride = 0f;
    }
}
