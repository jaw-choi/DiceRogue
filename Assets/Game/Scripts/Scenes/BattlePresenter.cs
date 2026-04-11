using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRogue
{
    public class BattlePresenter : MonoBehaviour
    {
        [SerializeField] private UnitView playerView;
        [SerializeField] private UnitView[] enemyViews = new UnitView[3];
        [SerializeField] private BattleHUD battleHud;
        [SerializeField] private FloatingTextSpawner floatingTextSpawner;
        [SerializeField] private BattleHitEffectSettings hitEffectSettings = new BattleHitEffectSettings();
        [SerializeField] private float preActionDelay = 0.08f;
        [SerializeField] private float betweenActionsDelay = 0.1f;
        [SerializeField] private float endTurnDelay = 0.15f;

        private readonly Dictionary<CombatantRuntimeState, UnitView> viewLookup = new Dictionary<CombatantRuntimeState, UnitView>();
        private Canvas effectCanvas;
        private Camera effectCamera;

        public void Configure(UnitView runtimePlayerView, UnitView[] runtimeEnemyViews, BattleHUD runtimeBattleHud, FloatingTextSpawner runtimeFloatingTextSpawner)
        {
            Configure(runtimePlayerView, runtimeEnemyViews, runtimeBattleHud, runtimeFloatingTextSpawner, null, null);
        }

        public void Configure(
            UnitView runtimePlayerView,
            UnitView[] runtimeEnemyViews,
            BattleHUD runtimeBattleHud,
            FloatingTextSpawner runtimeFloatingTextSpawner,
            Canvas runtimeEffectCanvas,
            BattleHitEffectSettings runtimeHitEffectSettings)
        {
            playerView = runtimePlayerView;
            enemyViews = runtimeEnemyViews ?? Array.Empty<UnitView>();
            battleHud = runtimeBattleHud;
            floatingTextSpawner = runtimeFloatingTextSpawner;
            effectCanvas = runtimeEffectCanvas;
            if (runtimeHitEffectSettings != null)
            {
                hitEffectSettings = runtimeHitEffectSettings;
            }

            PrepareHitEffectCanvas();
        }

        public void RefreshHitEffectSettings(Canvas runtimeEffectCanvas, BattleHitEffectSettings runtimeHitEffectSettings)
        {
            effectCanvas = runtimeEffectCanvas != null ? runtimeEffectCanvas : effectCanvas;
            if (runtimeHitEffectSettings != null)
            {
                hitEffectSettings = runtimeHitEffectSettings;
            }

            PrepareHitEffectCanvas();
        }

        public void PreviewConfiguredHitEffect()
        {
            var previewTarget = GetPreviewTargetView();
            var previewActor = GetPreviewActorView(previewTarget);
            if (previewTarget == null || previewActor == null)
            {
                return;
            }

            SpawnHitEffect(previewActor, previewTarget);
        }

        public void BindBattle(BattleSystem battleSystem)
        {
            viewLookup.Clear();

            if (battleSystem == null)
            {
                return;
            }

            if (playerView != null)
            {
                playerView.Bind(battleSystem.Player);
                viewLookup[battleSystem.Player] = playerView;
            }

            var activeEnemies = new List<CombatantRuntimeState>();
            for (var enemyIndex = 0; enemyIndex < battleSystem.Enemies.Count; enemyIndex++)
            {
                if (battleSystem.Enemies[enemyIndex] != null && battleSystem.Enemies[enemyIndex].IsAlive)
                {
                    activeEnemies.Add(battleSystem.Enemies[enemyIndex]);
                }
            }

            for (var index = 0; index < enemyViews.Length; index++)
            {
                if (enemyViews[index] == null)
                {
                    continue;
                }

                if (index < activeEnemies.Count)
                {
                    enemyViews[index].Bind(activeEnemies[index]);
                    viewLookup[activeEnemies[index]] = enemyViews[index];
                }
                else
                {
                    enemyViews[index].Bind(null);
                }
            }

            battleHud?.Refresh(battleSystem);
        }

        public IEnumerator PlayTurnReport(BattleSystem battleSystem, BattleTurnReport report, Action onComplete)
        {
            if (battleSystem == null || report == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            BindBattle(battleSystem);
            battleHud?.Refresh(battleSystem, report);
            yield return new WaitForSeconds(preActionDelay);

            foreach (var actionResult in report.ActionResults)
            {
                yield return PlayActionResult(battleSystem, report, actionResult);
                BindBattle(battleSystem);
                yield return new WaitForSeconds(betweenActionsDelay);
            }

            battleHud?.Refresh(battleSystem, report);
            yield return new WaitForSeconds(endTurnDelay);
            onComplete?.Invoke();
        }

        private IEnumerator PlayActionResult(BattleSystem battleSystem, BattleTurnReport report, BattleActionResult actionResult)
        {
            if (actionResult?.Actor == null || !viewLookup.TryGetValue(actionResult.Actor, out var actorView))
            {
                yield break;
            }

            battleHud?.Refresh(battleSystem, report, actionResult.Actor.DisplayName);
            actorView.SetHighlighted(true);
            actorView.Refresh();
            yield return new WaitForSeconds(0.06f);

            switch (actionResult.ActionType)
            {
                case SkillActionType.Attack:
                    yield return PlayAttackAction(battleSystem, report, actorView, actionResult);
                    break;
                case SkillActionType.Defense:
                    yield return PlayGuardAction(battleSystem, report, actorView, actionResult);
                    break;
                case SkillActionType.Buff:
                    yield return PlayBuffAction(battleSystem, report, actorView, actionResult);
                    break;
                case SkillActionType.Debuff:
                    yield return PlayDebuffAction(battleSystem, report, actorView, actionResult);
                    break;
            }

            actorView.SetHighlighted(false);
            actorView.Refresh();
        }

        private IEnumerator PlayAttackAction(BattleSystem battleSystem, BattleTurnReport report, UnitView actorView, BattleActionResult actionResult)
        {
            var primaryTargetResult = actionResult.Targets.Count > 0 ? actionResult.Targets[0] : null;
            var primaryTargetView = GetTargetView(primaryTargetResult);
            var totalHpDamage = 0;
            var totalShieldBlocked = 0;
            var targetWasDefeated = false;

            for (var index = 0; index < actionResult.Targets.Count; index++)
            {
                totalHpDamage += actionResult.Targets[index].HpDamage;
                totalShieldBlocked += actionResult.Targets[index].ShieldBlocked;
                targetWasDefeated |= actionResult.Targets[index].WasDefeated;
            }

            yield return actorView.PlayAttackLunge(primaryTargetView != null ? primaryTargetView.transform : null);

            if (primaryTargetView != null && primaryTargetResult != null)
            {
                SpawnHitEffect(actorView, primaryTargetView);
                yield return primaryTargetView.PlayHitReaction();

                if (totalHpDamage > 0)
                {
                    floatingTextSpawner?.Spawn(primaryTargetView.PopupAnchor, $"-{totalHpDamage}", new Color(1f, 0.35f, 0.35f));
                }
                else
                {
                    floatingTextSpawner?.Spawn(primaryTargetView.PopupAnchor, "Blocked", new Color(0.75f, 0.9f, 1f));
                }

                if (totalShieldBlocked > 0)
                {
                    floatingTextSpawner?.Spawn(primaryTargetView.PopupAnchor, $"Shield {totalShieldBlocked}", new Color(0.55f, 0.85f, 1f));
                }

                if (actionResult.HealAmount > 0)
                {
                    floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"+{actionResult.HealAmount} Heal", new Color(0.45f, 1f, 0.55f));
                }

                if (actionResult.ReflectedDamageTaken > 0)
                {
                    floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"-{actionResult.ReflectedDamageTaken} Reflect", new Color(1f, 0.65f, 0.2f));
                    actorView.Refresh();
                }

                primaryTargetView.Refresh();

                if (targetWasDefeated)
                {
                    yield return primaryTargetView.PlayDeathFade();
                }
            }

            battleHud?.Refresh(battleSystem, report, actionResult.Actor != null ? actionResult.Actor.DisplayName : null);
        }

        private void PrepareHitEffectCanvas()
        {
            if (hitEffectSettings == null || hitEffectSettings.prefab == null)
            {
                return;
            }

            effectCanvas ??= SceneUILayoutHelper.FindRootCanvas();
            if (effectCanvas == null)
            {
                return;
            }

            effectCamera = ResolveEffectCamera(effectCanvas);
            if (effectCamera == null)
            {
                return;
            }

            effectCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            effectCanvas.worldCamera = effectCamera;
            effectCanvas.planeDistance = Mathf.Max(effectCanvas.planeDistance, 100f);
        }

        private void SpawnHitEffect(UnitView actorView, UnitView targetView)
        {
            if (actorView?.HitEffectAnchor == null || targetView?.HitEffectAnchor == null || hitEffectSettings == null || hitEffectSettings.prefab == null)
            {
                return;
            }

            PrepareHitEffectCanvas();
            var instance = Instantiate(
                hitEffectSettings.prefab,
                GetHitEffectSpawnPosition(actorView.HitEffectAnchor, targetView.HitEffectAnchor),
                hitEffectSettings.prefab.transform.rotation);
            instance.transform.localScale = hitEffectSettings.localScale;

            var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (var index = 0; index < particleSystems.Length; index++)
            {
                particleSystems[index].Play(true);
            }

            var renderers = instance.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (var index = 0; index < renderers.Length; index++)
            {
                renderers[index].sortingLayerID = effectCanvas != null ? effectCanvas.sortingLayerID : renderers[index].sortingLayerID;
                renderers[index].sortingOrder = effectCanvas != null ? effectCanvas.sortingOrder + 10 : renderers[index].sortingOrder;
            }

            Destroy(instance, EstimateHitEffectLifetime(instance));
        }

        private Vector3 GetHitEffectSpawnPosition(RectTransform actorAnchor, RectTransform targetAnchor)
        {
            if (effectCanvas != null && effectCanvas.renderMode == RenderMode.ScreenSpaceCamera && effectCamera != null)
            {
                var actorScreenPoint = RectTransformUtility.WorldToScreenPoint(effectCamera, actorAnchor.position);
                var targetScreenPoint = RectTransformUtility.WorldToScreenPoint(effectCamera, targetAnchor.position);
                var screenPoint = Vector2.Lerp(actorScreenPoint, targetScreenPoint, Mathf.Clamp01(hitEffectSettings.impactLerpFromActorToTarget));
                screenPoint += hitEffectSettings.screenOffset;
                var distance = Mathf.Max(0.1f, effectCanvas.planeDistance - Mathf.Max(0f, hitEffectSettings.depthOffsetTowardsCamera));
                return effectCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, distance));
            }

            var worldPoint = Vector3.Lerp(actorAnchor.position, targetAnchor.position, Mathf.Clamp01(hitEffectSettings.impactLerpFromActorToTarget));
            return worldPoint + new Vector3(hitEffectSettings.screenOffset.x * 0.01f, hitEffectSettings.screenOffset.y * 0.01f, 0f);
        }

        private float EstimateHitEffectLifetime(GameObject effectInstance)
        {
            if (hitEffectSettings.durationOverride > 0f)
            {
                return hitEffectSettings.durationOverride;
            }

            var maxDuration = 0.8f;
            var particleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (var index = 0; index < particleSystems.Length; index++)
            {
                var main = particleSystems[index].main;
                var startDelay = main.startDelay;
                var startLifetime = main.startLifetime;
                var duration = main.duration + startDelay.constantMax + startLifetime.constantMax;
                if (duration > maxDuration)
                {
                    maxDuration = duration;
                }
            }

            return maxDuration + 0.2f;
        }

        private static Camera ResolveEffectCamera(Canvas canvas)
        {
            if (canvas != null && canvas.worldCamera != null)
            {
                return canvas.worldCamera;
            }

            if (Camera.main != null)
            {
                return Camera.main;
            }

            return FindFirstObjectByType<Camera>();
        }

        private IEnumerator PlayGuardAction(BattleSystem battleSystem, BattleTurnReport report, UnitView actorView, BattleActionResult actionResult)
        {
            yield return actorView.PlayShieldPulse();

            if (actionResult.ShieldGain > 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"+{actionResult.ShieldGain} Shield", new Color(0.5f, 0.8f, 1f));
            }

            if (actionResult.ArmorGain > 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"+{actionResult.ArmorGain} Armor", new Color(0.85f, 0.9f, 1f));
            }

            if (actionResult.NextTurnShieldGain > 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"Next +{actionResult.NextTurnShieldGain} Shield", new Color(0.5f, 1f, 1f));
            }

            actorView.Refresh();
            battleHud?.Refresh(battleSystem, report, actionResult.Actor != null ? actionResult.Actor.DisplayName : null);
        }

        private IEnumerator PlayBuffAction(BattleSystem battleSystem, BattleTurnReport report, UnitView actorView, BattleActionResult actionResult)
        {
            yield return actorView.PlayRagePulse();

            if (actionResult.RageGain > 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"+{actionResult.RageGain} Rage", new Color(1f, 0.7f, 0.2f));
            }

            if (actionResult.SummonedCount > 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"+{actionResult.SummonedCount} Summon", new Color(0.7f, 1f, 0.8f));
            }

            if (actionResult.SummonedAllyAttackBonusGranted > 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"+{actionResult.SummonedAllyAttackBonusGranted} Aura", new Color(1f, 0.82f, 0.3f));
            }

            if (actionResult.ActivatedBerserk)
            {
                yield return actorView.PlayBerserkPulse();
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, "Berserk!", new Color(1f, 0.45f, 0.95f));
            }

            actorView.Refresh();
            battleHud?.Refresh(battleSystem, report, actionResult.Actor != null ? actionResult.Actor.DisplayName : null);
        }

        private IEnumerator PlayDebuffAction(BattleSystem battleSystem, BattleTurnReport report, UnitView actorView, BattleActionResult actionResult)
        {
            yield return actorView.PlayShieldPulse();

            if (actionResult.AttackModifier != 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"Attack {actionResult.AttackModifier:+#;-#;0}", new Color(1f, 0.7f, 0.2f));
            }

            if (actionResult.DicePointModifier != 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"DP {actionResult.DicePointModifier:+#;-#;0}", new Color(0.6f, 0.9f, 1f));
            }

            actorView.Refresh();

            if (actionResult.ActorWasDefeated)
            {
                yield return actorView.PlayDeathFade();
            }

            battleHud?.Refresh(battleSystem, report, actionResult.Actor != null ? actionResult.Actor.DisplayName : null);
        }

        private UnitView GetTargetView(BattleTargetResult targetResult)
        {
            if (targetResult?.Target == null)
            {
                return null;
            }

            viewLookup.TryGetValue(targetResult.Target, out var targetView);
            return targetView;
        }

        private UnitView GetPreviewTargetView()
        {
            for (var index = 0; index < enemyViews.Length; index++)
            {
                if (enemyViews[index] != null && enemyViews[index].gameObject.activeInHierarchy && enemyViews[index].BoundState != null)
                {
                    return enemyViews[index];
                }
            }

            return playerView != null && playerView.gameObject.activeInHierarchy ? playerView : null;
        }

        private UnitView GetPreviewActorView(UnitView previewTarget)
        {
            if (previewTarget != null && previewTarget != playerView && playerView != null && playerView.gameObject.activeInHierarchy)
            {
                return playerView;
            }

            for (var index = 0; index < enemyViews.Length; index++)
            {
                if (enemyViews[index] != null && enemyViews[index] != previewTarget && enemyViews[index].gameObject.activeInHierarchy)
                {
                    return enemyViews[index];
                }
            }

            return playerView;
        }
    }
}
