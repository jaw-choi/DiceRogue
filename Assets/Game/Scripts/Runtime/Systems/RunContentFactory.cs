using System.Collections.Generic;
using UnityEngine;

namespace DiceRogue
{
    public static class RunContentFactory
    {
        public static RunConfig LoadOrCreateConfig()
        {
            var config = Resources.Load<RunConfig>("DiceRogue/RunConfig");
            return config != null ? config : CreateFallbackConfig();
        }

        public static RunConfig CreateFallbackConfig()
        {
            var basicAttack = CreateSkill("basic_attack", "Basic Attack", SkillActionType.Attack, 2, 0, 0, 0, 0, "#F59F00", "Basic attack face.");
            var guard = CreateSkill("guard", "Guard", SkillActionType.Guard, 0, 3, 0, 0, 0, "#339AF0", "Basic shield face.");
            var firstAid = CreateSkill("first_aid", "First Aid", SkillActionType.Heal, 0, 0, 3, 0, 0, "#51CF66", "Recover hp.");
            var rageBurst = CreateSkill("rage_burst", "Rage Burst", SkillActionType.Berserk, 0, 0, 0, 1, 2, "#FA5252", "Lose hp and gain rage.");
            var heavySlash = CreateSkill("heavy_slash", "Heavy Slash", SkillActionType.Attack, 4, 0, 0, 0, 0, "#FF922B", "Heavy attack face.");
            var fortify = CreateSkill("fortify", "Fortify", SkillActionType.Guard, 0, 5, 0, 0, 0, "#4DABF7", "Gain a large shield.");

            var player = CreateCombatant(
                "player_knight",
                "Dice Knight",
                32,
                0,
                0,
                false,
                basicAttack,
                guard,
                basicAttack,
                guard,
                basicAttack,
                guard);

            var slime = CreateCombatant(
                "slime",
                "Jelly Slime",
                18,
                0,
                0,
                false,
                basicAttack,
                guard,
                basicAttack,
                guard,
                basicAttack,
                guard);

            var raider = CreateCombatant(
                "raider",
                "Wild Raider",
                24,
                0,
                1,
                false,
                basicAttack,
                heavySlash,
                basicAttack,
                rageBurst,
                guard,
                basicAttack);

            var guardian = CreateCombatant(
                "guardian",
                "Shield Warden",
                28,
                1,
                0,
                false,
                guard,
                fortify,
                guard,
                basicAttack,
                firstAid,
                guard);

            var boss = CreateCombatant(
                "boss",
                "Dice Overlord",
                44,
                1,
                1,
                true,
                heavySlash,
                rageBurst,
                fortify,
                basicAttack,
                firstAid,
                heavySlash);

            var config = ScriptableObject.CreateInstance<RunConfig>();
            SetPrivateField(config, "bootSceneName", "Boot");
            SetPrivateField(config, "mainMenuSceneName", "MainMenu");
            SetPrivateField(config, "mapSceneName", "MapScene");
            SetPrivateField(config, "battleSceneName", "BattleScene");
            SetPrivateField(config, "rewardSceneName", "RewardScene");
            SetPrivateField(config, "playerTemplate", player);
            SetPrivateField(config, "skillLibrary", new List<SkillDefinition> { basicAttack, guard, firstAid, rageBurst, heavySlash, fortify });
            SetPrivateField(config, "normalEnemies", new List<CombatantTemplate> { slime, raider, guardian });
            SetPrivateField(config, "bossEnemy", boss);
            SetPrivateField(config, "mapNodes", new List<MapNodeDefinition>
            {
                CreateNode("node_a", "Training Hall", MapNodeType.Battle, slime, 2),
                CreateNode("node_b", "Raid Camp", MapNodeType.Battle, raider, 2),
                CreateNode("node_c", "Stone Gate", MapNodeType.Battle, guardian, 3),
                CreateNode("node_d", "Boss Room", MapNodeType.Boss, boss)
            });
            SetPrivateField(config, "autoTurnDelay", 0.85f);
            SetPrivateField(config, "maxBattleTurns", 12);
            return config;
        }

        private static SkillDefinition CreateSkill(
            string id,
            string displayName,
            SkillActionType actionType,
            int attackAmount,
            int shieldAmount,
            int healAmount,
            int selfDamageAmount,
            int rageGainAmount,
            string colorHex,
            string description)
        {
            var definition = ScriptableObject.CreateInstance<SkillDefinition>();
            ColorUtility.TryParseHtmlString(colorHex, out var color);

            SetPrivateField(definition, "id", id);
            SetPrivateField(definition, "displayName", displayName);
            SetPrivateField(definition, "actionType", actionType);
            SetPrivateField(definition, "attackAmount", attackAmount);
            SetPrivateField(definition, "shieldAmount", shieldAmount);
            SetPrivateField(definition, "healAmount", healAmount);
            SetPrivateField(definition, "selfDamageAmount", selfDamageAmount);
            SetPrivateField(definition, "rageGainAmount", rageGainAmount);
            SetPrivateField(definition, "accentColor", color);
            SetPrivateField(definition, "description", description);
            return definition;
        }

        private static CombatantTemplate CreateCombatant(
            string id,
            string displayName,
            int maxHp,
            int armor,
            int rage,
            bool isBoss,
            params SkillDefinition[] diceSkills)
        {
            var definition = ScriptableObject.CreateInstance<CombatantTemplate>();
            SetPrivateField(definition, "id", id);
            SetPrivateField(definition, "displayName", displayName);
            SetPrivateField(definition, "maxHp", maxHp);
            SetPrivateField(definition, "startingArmor", armor);
            SetPrivateField(definition, "startingRage", rage);
            SetPrivateField(definition, "isBoss", isBoss);
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
