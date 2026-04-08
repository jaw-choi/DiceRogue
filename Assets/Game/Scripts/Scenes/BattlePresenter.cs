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
        [SerializeField] private float preActionDelay = 0.08f;
        [SerializeField] private float betweenActionsDelay = 0.1f;
        [SerializeField] private float endTurnDelay = 0.15f;

        private readonly Dictionary<CombatantRuntimeState, UnitView> viewLookup = new Dictionary<CombatantRuntimeState, UnitView>();

        public void Configure(UnitView runtimePlayerView, UnitView[] runtimeEnemyViews, BattleHUD runtimeBattleHud, FloatingTextSpawner runtimeFloatingTextSpawner)
        {
            playerView = runtimePlayerView;
            enemyViews = runtimeEnemyViews ?? Array.Empty<UnitView>();
            battleHud = runtimeBattleHud;
            floatingTextSpawner = runtimeFloatingTextSpawner;
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

            for (var index = 0; index < enemyViews.Length; index++)
            {
                if (enemyViews[index] == null)
                {
                    continue;
                }

                // TODO: 전투 데이터가 다중 적을 지원하게 되면 enemyViews[1..2]에도 런타임 적 상태를 바인딩한다.
                if (index == 0 && battleSystem.Enemy != null)
                {
                    enemyViews[index].Bind(battleSystem.Enemy);
                    viewLookup[battleSystem.Enemy] = enemyViews[index];
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

            battleHud?.Refresh(battleSystem, report);
            yield return new WaitForSeconds(preActionDelay);

            foreach (var actionResult in report.ActionResults)
            {
                yield return PlayActionResult(battleSystem, report, actionResult);
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
                    yield return PlayHealAction(battleSystem, report, actorView, actionResult);
                    break;
                case SkillActionType.Debuff:
                    yield return PlayBerserkAction(battleSystem, report, actorView, actionResult);
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
                yield return primaryTargetView.PlayHitReaction();

                if (totalHpDamage > 0)
                {
                    floatingTextSpawner?.Spawn(primaryTargetView.PopupAnchor, $"-{totalHpDamage}", new Color(1f, 0.35f, 0.35f));
                }
                else
                {
                    floatingTextSpawner?.Spawn(primaryTargetView.PopupAnchor, "막힘", new Color(0.75f, 0.9f, 1f));
                }

                if (totalShieldBlocked > 0)
                {
                    floatingTextSpawner?.Spawn(primaryTargetView.PopupAnchor, $"방어도 {totalShieldBlocked}", new Color(0.55f, 0.85f, 1f));
                }

                if (actionResult.HealAmount > 0)
                {
                    floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"+{actionResult.HealAmount} 회복", new Color(0.45f, 1f, 0.55f));
                }

                if (actionResult.ReflectedDamageTaken > 0)
                {
                    floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"-{actionResult.ReflectedDamageTaken} 반사", new Color(1f, 0.65f, 0.2f));
                    actorView.Refresh();
                }

                primaryTargetView.Refresh();

                if (targetWasDefeated)
                {
                    yield return primaryTargetView.PlayDeathFade();
                }
            }

            battleHud?.Refresh(battleSystem, report);
        }

        private IEnumerator PlayGuardAction(BattleSystem battleSystem, BattleTurnReport report, UnitView actorView, BattleActionResult actionResult)
        {
            yield return actorView.PlayShieldPulse();
            if (actionResult.ShieldGain > 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"+{actionResult.ShieldGain} 방어도", new Color(0.5f, 0.8f, 1f));
            }

            if (actionResult.ArmorGain > 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"+{actionResult.ArmorGain} 방어력", new Color(0.85f, 0.9f, 1f));
            }

            if (actionResult.NextTurnShieldGain > 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"다음 턴 +{actionResult.NextTurnShieldGain}", new Color(0.5f, 1f, 1f));
            }

            actorView.Refresh();
            battleHud?.Refresh(battleSystem, report);
        }

        private IEnumerator PlayHealAction(BattleSystem battleSystem, BattleTurnReport report, UnitView actorView, BattleActionResult actionResult)
        {
            yield return actorView.PlayRagePulse();

            if (actionResult.RageGain > 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"+{actionResult.RageGain} 분노", new Color(1f, 0.7f, 0.2f));
            }

            if (actionResult.ActivatedBerserk)
            {
                yield return actorView.PlayBerserkPulse();
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, "광분!", new Color(1f, 0.45f, 0.95f));
            }

            actorView.Refresh();
            battleHud?.Refresh(battleSystem, report);
        }

        private IEnumerator PlayBerserkAction(BattleSystem battleSystem, BattleTurnReport report, UnitView actorView, BattleActionResult actionResult)
        {
            yield return actorView.PlayShieldPulse();

            if (actionResult.AttackModifier != 0)
            {
                floatingTextSpawner?.Spawn(actorView.PopupAnchor, $"공격 {actionResult.AttackModifier:+#;-#;0}", new Color(1f, 0.7f, 0.2f));
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

            battleHud?.Refresh(battleSystem, report);
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
    }
}
