using System.Collections.Generic;
using UnityEngine;

namespace DiceRogue
{
    public static class RunContentFactory
    {
        public static RunConfig CreateFallbackConfig()
        {
            return BuildRuntimeConfig();
        }

        public static RunConfig LoadOrCreateConfig()
        {
            var config = Resources.Load<RunConfig>("DiceRogue/RunConfig");
            return BuildRuntimeConfig(config);
        }

        public static RunConfig BuildRuntimeConfig(RunConfig sourceConfig = null)
        {
            var basicAttack = CreateSkill(
                "basic_attack",
                "기본 공격",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 6,
                attackUpgradeAmount: 3,
                colorHex: "#F59F00",
                description: "기본 공격 6, 강화 시 9.");

            var guardStance = CreateSkill(
                "guard_stance",
                "수비 태세",
                SkillActionType.Defense,
                SkillTargetType.Self,
                shieldAmount: 10,
                shieldUpgradeAmount: 4,
                armorAmount: 3,
                armorUpgradeAmount: 2,
                colorHex: "#339AF0",
                description: "방어도와 방어력을 함께 올립니다.");

            var focusedDefense = CreateSkill(
                "focused_defense",
                "집중 방어",
                SkillActionType.Defense,
                SkillTargetType.Self,
                shieldAmount: 8,
                shieldUpgradeAmount: 3,
                nextTurnShieldAmount: 8,
                nextTurnShieldUpgradeAmount: 3,
                colorHex: "#74C0FC",
                description: "이번 턴과 다음 턴 방어도를 확보합니다.");

            var counter = CreateSkill(
                "counter",
                "반격",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                shieldDamagePercent: 50,
                shieldDamageUpgradePercent: 20,
                colorHex: "#FFB300",
                description: "현재 방어도에 비례한 피해를 줍니다.");

            var shieldBurst = CreateSkill(
                "shield_burst",
                "방패 폭발",
                SkillActionType.Attack,
                SkillTargetType.AllEnemies,
                shieldDamagePercent: 60,
                shieldDamageUpgradePercent: 25,
                consumeAllShield: true,
                colorHex: "#90E0EF",
                description: "방어도를 모두 소모해 큰 피해를 줍니다.");

            var bloodSlash = CreateSkill(
                "blood_slash",
                "혈기 베기",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 8,
                attackUpgradeAmount: 3,
                selfDamageAmount: 4,
                selfDamageUpgradeAmount: 1,
                rageGainAmount: 2,
                rageGainUpgradeAmount: 1,
                colorHex: "#FA5252",
                description: "체력을 깎아 분노를 얻는 공격입니다.");

            var fury = CreateSkill(
                "fury",
                "격노",
                SkillActionType.Buff,
                SkillTargetType.Self,
                rageGainAmount: 5,
                rageGainUpgradeAmount: 2,
                colorHex: "#F76707",
                description: "분노를 크게 얻습니다.");

            var ragingStrike = CreateSkill(
                "raging_strike",
                "광폭 일격",
                SkillActionType.Attack,
                SkillTargetType.HighestHpEnemy,
                attackAmount: 12,
                attackUpgradeAmount: 10,
                rageCostAmount: 5,
                rageCostReductionPerUpgrade: 1,
                colorHex: "#FF6B6B",
                description: "분노를 소모해 강하게 내려칩니다.");

            var vampiricStrike = CreateSkill(
                "vampiric_strike",
                "흡혈 베기",
                SkillActionType.Attack,
                SkillTargetType.HighestHpEnemy,
                attackAmount: 8,
                attackUpgradeAmount: 3,
                lifestealPercent: 50,
                lifestealUpgradePercent: 20,
                colorHex: "#E64980",
                description: "피해의 일부를 체력으로 회복합니다.");

            var slimeAttack4 = CreateSkill(
                "slime_attack_4",
                "점액 타격",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 4,
                colorHex: "#69DB7C",
                description: "슬라임의 기본 공격입니다.");

            var slimeAttack5 = CreateSkill(
                "slime_attack_5",
                "강한 점액",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 5,
                colorHex: "#51CF66",
                description: "조금 더 강한 슬라임 공격입니다.");

            var slimeGuard = CreateSkill(
                "slime_guard",
                "점액 보호막",
                SkillActionType.Defense,
                SkillTargetType.Self,
                shieldAmount: 6,
                colorHex: "#74C69D",
                description: "슬라임이 몸을 굳혀 방어합니다.");

            var slimeBind = CreateSkill(
                "slime_bind",
                "구속",
                SkillActionType.Debuff,
                SkillTargetType.RandomEnemy,
                dicePointModifierAmount: -1,
                colorHex: "#38D9A9",
                description: "다음 턴 상대 DP를 감소시킵니다.");

            var goblinAttack5 = CreateSkill(
                "goblin_attack_5",
                "단검 찌르기",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 5,
                colorHex: "#FCC419",
                description: "고블린의 날카로운 공격입니다.");

            var goblinAttack6 = CreateSkill(
                "goblin_attack_6",
                "기습 베기",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 6,
                colorHex: "#FAB005",
                description: "고블린의 강한 일격입니다.");

            var goblinDoubleStrike = CreateSkill(
                "goblin_double_strike",
                "연속 공격",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 3,
                repeatCount: 2,
                colorHex: "#FD7E14",
                description: "4 피해를 2회 가합니다.");

            var goblinDefense = CreateSkill(
                "goblin_defense",
                "재빠른 수비",
                SkillActionType.Defense,
                SkillTargetType.Self,
                shieldAmount: 4,
                colorHex: "#9775FA",
                description: "간단한 방어 자세를 취합니다.");

            var golemDefense = CreateSkill(
                "golem_defense",
                "암석 방패",
                SkillActionType.Defense,
                SkillTargetType.Self,
                shieldAmount: 6,
                colorHex: "#A5D8FF",
                description: "골렘이 단단한 방어막을 세웁니다.");

            var golemArmorUp = CreateSkill(
                "golem_armor_up",
                "장갑 강화",
                SkillActionType.Defense,
                SkillTargetType.Self,
                armorAmount: 3,
                colorHex: "#DEE2E6",
                description: "이번 턴 방어력을 높입니다.");

            var golemAttack = CreateSkill(
                "golem_attack",
                "바위 강타",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 7,
                colorHex: "#868E96",
                description: "무거운 바위 주먹으로 공격합니다.");

            var shamanAttack = CreateSkill(
                "shaman_attack",
                "저주 화살",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 7,
                colorHex: "#AE3EC9",
                description: "주술사의 기본 공격입니다.");

            var shamanWeaken = CreateSkill(
                "shaman_weaken",
                "약화 저주",
                SkillActionType.Debuff,
                SkillTargetType.RandomEnemy,
                attackModifierAmount: -3,
                colorHex: "#C2255C",
                description: "다음 턴 상대 공격력을 감소시킵니다.");

            var shamanCurse = CreateSkill(
                "shaman_curse",
                "속박 저주",
                SkillActionType.Debuff,
                SkillTargetType.RandomEnemy,
                dicePointModifierAmount: -1,
                attackModifierAmount: -1,
                colorHex: "#9C36B5",
                description: "다음 턴 DP와 공격을 함께 낮춥니다.");

            var shamanFury = CreateSkill(
                "shaman_fury",
                "주술 분노",
                SkillActionType.Buff,
                SkillTargetType.Self,
                rageGainAmount: 4,
                colorHex: "#D9480F",
                description: "분노를 끌어올려 공격을 준비합니다.");

            var player = CreateCombatant(
                "player_knight",
                "주사위 기사",
                100,
                2,
                0,
                false,
                0,
                basicAttack,
                guardStance,
                basicAttack,
                guardStance,
                basicAttack,
                guardStance);

            var slime = CreateCombatant(
                "slime",
                "슬라임",
                15,
                1,
                0,
                false,
                0,
                slimeAttack4,
                slimeAttack5,
                slimeAttack4,
                slimeGuard,
                slimeGuard,
                slimeBind);

            var goblin = CreateCombatant(
                "goblin",
                "고블린",
                14,
                1,
                0,
                false,
                0,
                goblinAttack5,
                goblinAttack6,
                goblinAttack5,
                goblinDoubleStrike,
                goblinDoubleStrike,
                goblinDefense);

            var golem = CreateCombatant(
                "golem",
                "골렘",
                42,
                1,
                5,
                false,
                10,
                golemDefense,
                golemDefense,
                golemArmorUp,
                golemArmorUp,
                golemAttack,
                counter);

            var shaman = CreateCombatant(
                "shaman",
                "주술사",
                80,
                2,
                0,
                true,
                0,
                shamanAttack,
                shamanWeaken,
                shamanWeaken,
                shamanCurse,
                shamanFury,
                shamanAttack);

            var config = ScriptableObject.CreateInstance<RunConfig>();
            SetPrivateField(config, "bootSceneName", sourceConfig != null ? sourceConfig.BootSceneName : "Boot");
            SetPrivateField(config, "mainMenuSceneName", sourceConfig != null ? sourceConfig.MainMenuSceneName : "MainMenu");
            SetPrivateField(config, "mapSceneName", sourceConfig != null ? sourceConfig.MapSceneName : "MapScene");
            SetPrivateField(config, "battleSceneName", sourceConfig != null ? sourceConfig.BattleSceneName : "BattleScene");
            SetPrivateField(config, "rewardSceneName", sourceConfig != null ? sourceConfig.RewardSceneName : "RewardScene");
            SetPrivateField(config, "playerTemplate", player);
            SetPrivateField(config, "skillLibrary", new List<SkillDefinition>
            {
                basicAttack,
                guardStance,
                focusedDefense,
                counter,
                shieldBurst,
                bloodSlash,
                fury,
                ragingStrike,
                vampiricStrike
            });
            SetPrivateField(config, "normalEnemies", new List<CombatantTemplate> { slime, goblin, golem });
            SetPrivateField(config, "bossEnemy", shaman);
            SetPrivateField(config, "mapNodes", new List<MapNodeDefinition>
            {
                CreateNode("node_a", "슬라임 늪", MapNodeType.Battle, slime, 1),
                CreateNode("node_b", "고블린 야영지", MapNodeType.Battle, goblin, 2),
                CreateNode("node_c", "암석 전당", MapNodeType.Battle, golem, 3),
                CreateNode("node_d", "의식의 방", MapNodeType.Boss, shaman)
            });
            SetPrivateField(config, "autoTurnDelay", 0.65f);
            SetPrivateField(config, "maxBattleTurns", 0);
            return config;
        }

        private static SkillDefinition CreateSkill(
            string id,
            string displayName,
            SkillActionType actionType,
            SkillTargetType targetType,
            int attackAmount = 0,
            int attackUpgradeAmount = 0,
            int shieldAmount = 0,
            int shieldUpgradeAmount = 0,
            int armorAmount = 0,
            int armorUpgradeAmount = 0,
            int nextTurnShieldAmount = 0,
            int nextTurnShieldUpgradeAmount = 0,
            int selfDamageAmount = 0,
            int selfDamageUpgradeAmount = 0,
            int rageGainAmount = 0,
            int rageGainUpgradeAmount = 0,
            int rageCostAmount = 0,
            int rageCostReductionPerUpgrade = 0,
            int lifestealPercent = 0,
            int lifestealUpgradePercent = 0,
            int shieldDamagePercent = 0,
            int shieldDamageUpgradePercent = 0,
            int attackModifierAmount = 0,
            int attackModifierUpgradeAmount = 0,
            int dicePointModifierAmount = 0,
            int dicePointModifierUpgradeAmount = 0,
            int repeatCount = 1,
            int repeatCountUpgradeAmount = 0,
            int bonusDicePointsOnFirstRoll = 0,
            int bonusDicePointsUpgradeAmount = 0,
            bool consumeAllShield = false,
            string colorHex = "#FFFFFF",
            string description = "")
        {
            var definition = ScriptableObject.CreateInstance<SkillDefinition>();
            ColorUtility.TryParseHtmlString(colorHex, out var color);

            SetPrivateField(definition, "id", id);
            SetPrivateField(definition, "displayName", displayName);
            SetPrivateField(definition, "actionType", actionType);
            SetPrivateField(definition, "targetType", targetType);
            SetPrivateField(definition, "attackAmount", attackAmount);
            SetPrivateField(definition, "attackUpgradeAmount", attackUpgradeAmount);
            SetPrivateField(definition, "shieldAmount", shieldAmount);
            SetPrivateField(definition, "shieldUpgradeAmount", shieldUpgradeAmount);
            SetPrivateField(definition, "armorAmount", armorAmount);
            SetPrivateField(definition, "armorUpgradeAmount", armorUpgradeAmount);
            SetPrivateField(definition, "nextTurnShieldAmount", nextTurnShieldAmount);
            SetPrivateField(definition, "nextTurnShieldUpgradeAmount", nextTurnShieldUpgradeAmount);
            SetPrivateField(definition, "selfDamageAmount", selfDamageAmount);
            SetPrivateField(definition, "selfDamageUpgradeAmount", selfDamageUpgradeAmount);
            SetPrivateField(definition, "rageGainAmount", rageGainAmount);
            SetPrivateField(definition, "rageGainUpgradeAmount", rageGainUpgradeAmount);
            SetPrivateField(definition, "rageCostAmount", rageCostAmount);
            SetPrivateField(definition, "rageCostReductionPerUpgrade", rageCostReductionPerUpgrade);
            SetPrivateField(definition, "lifestealPercent", lifestealPercent);
            SetPrivateField(definition, "lifestealUpgradePercent", lifestealUpgradePercent);
            SetPrivateField(definition, "shieldDamagePercent", shieldDamagePercent);
            SetPrivateField(definition, "shieldDamageUpgradePercent", shieldDamageUpgradePercent);
            SetPrivateField(definition, "attackModifierAmount", attackModifierAmount);
            SetPrivateField(definition, "attackModifierUpgradeAmount", attackModifierUpgradeAmount);
            SetPrivateField(definition, "dicePointModifierAmount", dicePointModifierAmount);
            SetPrivateField(definition, "dicePointModifierUpgradeAmount", dicePointModifierUpgradeAmount);
            SetPrivateField(definition, "repeatCount", repeatCount);
            SetPrivateField(definition, "repeatCountUpgradeAmount", repeatCountUpgradeAmount);
            SetPrivateField(definition, "bonusDicePointsOnFirstRoll", bonusDicePointsOnFirstRoll);
            SetPrivateField(definition, "bonusDicePointsUpgradeAmount", bonusDicePointsUpgradeAmount);
            SetPrivateField(definition, "consumeAllShield", consumeAllShield);
            SetPrivateField(definition, "accentColor", color);
            SetPrivateField(definition, "description", description);
            return definition;
        }

        private static CombatantTemplate CreateCombatant(
            string id,
            string displayName,
            int maxHp,
            int baseDicePoints,
            int passiveShieldPerTurn,
            bool isBoss,
            int passiveReflectPercent,
            params SkillDefinition[] diceSkills)
        {
            var definition = ScriptableObject.CreateInstance<CombatantTemplate>();
            SetPrivateField(definition, "id", id);
            SetPrivateField(definition, "displayName", displayName);
            SetPrivateField(definition, "maxHp", maxHp);
            SetPrivateField(definition, "baseDicePoints", baseDicePoints);
            SetPrivateField(definition, "passiveShieldPerTurn", passiveShieldPerTurn);
            SetPrivateField(definition, "passiveReflectPercent", passiveReflectPercent);
            SetPrivateField(definition, "isBoss", isBoss);
            SetPrivateField(definition, "battleTint", isBoss ? new Color(1f, 0.8f, 0.85f) : Color.white);
            SetPrivateField(definition, "diceSkills", new List<SkillDefinition>(diceSkills));
            return definition;
        }

        private static MapNodeDefinition CreateNode(
            string id,
            string displayName,
            MapNodeType type,
            CombatantTemplate enemyTemplate,
            params int[] nextNodes)
        {
            var definition = new MapNodeDefinition();
            SetPrivateField(definition, "id", id);
            SetPrivateField(definition, "displayName", displayName);
            SetPrivateField(definition, "nodeType", type);
            SetPrivateField(definition, "enemyTemplate", enemyTemplate);
            SetPrivateField(definition, "nextNodeIndices", new List<int>(nextNodes));
            return definition;
        }

        private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            var field = typeof(TTarget).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
