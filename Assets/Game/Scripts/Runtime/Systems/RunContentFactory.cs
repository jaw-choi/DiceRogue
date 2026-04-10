using System.Collections.Generic;
using System.Linq;
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

        public static SkillDatabase CreatePrototypeSkillDatabase(RunConfig config)
        {
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            var playerSkills = config != null ? config.SkillLibrary : new List<SkillDefinition>();
            var enemySkills = new List<SkillDefinition>();

            if (config != null)
            {
                foreach (var enemy in config.NormalEnemies)
                {
                    if (enemy == null)
                    {
                        continue;
                    }

                    enemySkills.AddRange(enemy.DiceSkills);
                }

                if (config.BossEnemy != null)
                {
                    enemySkills.AddRange(config.BossEnemy.DiceSkills);
                }
            }

            database.Configure(playerSkills, enemySkills);
            return database;
        }

        public static List<DiceLoadoutDefinition> CreatePrototypePlayerLoadouts(RunConfig config)
        {
            var loadouts = new List<DiceLoadoutDefinition>();
            var starterLoadout = config?.PlayerTemplate != null ? config.PlayerTemplate.DiceLoadout : null;
            if (starterLoadout != null)
            {
                loadouts.Add(starterLoadout);
            }

            var defensiveLoadout = CreateLoadout(
                "defensive_preset",
                "Defensive Preset",
                DiceBuildIdentity.Defensive,
                FindSkill(config?.SkillLibrary, "defensive_stance"),
                FindSkill(config?.SkillLibrary, "focused_defense"),
                FindSkill(config?.SkillLibrary, "defensive_stance"),
                FindSkill(config?.SkillLibrary, "counter"),
                FindSkill(config?.SkillLibrary, "shield_burst"),
                FindSkill(config?.SkillLibrary, "basic_attack"));

            var berserkerLoadout = CreateLoadout(
                "berserker_preset",
                "Berserker Preset",
                DiceBuildIdentity.Berserker,
                FindSkill(config?.SkillLibrary, "blood_slash"),
                FindSkill(config?.SkillLibrary, "fury"),
                FindSkill(config?.SkillLibrary, "savage_strike"),
                FindSkill(config?.SkillLibrary, "vampiric_slash"),
                FindSkill(config?.SkillLibrary, "blood_slash"),
                FindSkill(config?.SkillLibrary, "basic_attack"));

            loadouts.Add(defensiveLoadout);
            loadouts.Add(berserkerLoadout);
            return loadouts.Where(loadout => loadout != null).Distinct().ToList();
        }

        public static RunConfig BuildRuntimeConfig(RunConfig sourceConfig = null)
        {
            var basicAttack = CreateSkill(
                "basic_attack",
                "Basic Attack",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 6,
                attackUpgradeAmount: 3,
                colorHex: "#F59F00",
                description: "Deal 6 damage. Upgrade: 9 damage.");

            var defensiveStance = CreateSkill(
                "defensive_stance",
                "Defensive Stance",
                SkillActionType.Defense,
                SkillTargetType.Self,
                shieldAmount: 10,
                shieldUpgradeAmount: 4,
                armorAmount: 3,
                armorUpgradeAmount: 2,
                colorHex: "#339AF0",
                description: "Shield +10 and Armor +3. Upgrade: Shield +14 and Armor +5.");

            var focusedDefense = CreateSkill(
                "focused_defense",
                "Focus Defense",
                SkillActionType.Defense,
                SkillTargetType.Self,
                shieldAmount: 8,
                shieldUpgradeAmount: 3,
                nextTurnShieldAmount: 8,
                nextTurnShieldUpgradeAmount: 3,
                colorHex: "#74C0FC",
                description: "Shield +8 and next turn Shield +8. Upgrade: +11 / +11.");

            var counter = CreateSkill(
                "counter",
                "Counter",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                shieldDamagePercent: 50,
                shieldDamageUpgradePercent: 20,
                colorHex: "#FFB300",
                description: "Deal damage equal to 50% of current Shield. Upgrade: 70%.");

            var shieldBurst = CreateSkill(
                "shield_burst",
                "Shield Burst",
                SkillActionType.Attack,
                SkillTargetType.AllEnemies,
                shieldDamagePercent: 60,
                shieldDamageUpgradePercent: 25,
                consumeAllShield: true,
                colorHex: "#90E0EF",
                description: "Remove all current Shield and deal 60% of it to all enemies. Upgrade: 85%.");

            var bloodSlash = CreateSkill(
                "blood_slash",
                "Blood Slash",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 8,
                attackUpgradeAmount: 3,
                selfDamageAmount: 4,
                selfDamageUpgradeAmount: 1,
                rageGainAmount: 2,
                rageGainUpgradeAmount: 1,
                colorHex: "#FA5252",
                description: "Deal 8 damage, lose 4 HP, gain 2 Rage. Upgrade: 11 damage, 5 self damage, 3 Rage.");

            var fury = CreateSkill(
                "fury",
                "Fury",
                SkillActionType.Buff,
                SkillTargetType.Self,
                rageGainAmount: 5,
                rageGainUpgradeAmount: 2,
                colorHex: "#F76707",
                description: "Gain 5 Rage. Upgrade: 7 Rage.");

            var savageStrike = CreateSkill(
                "savage_strike",
                "Savage Strike",
                SkillActionType.Attack,
                SkillTargetType.HighHpEnemy,
                attackAmount: 12,
                attackUpgradeAmount: 10,
                rageCostAmount: 5,
                rageCostReductionPerUpgrade: 1,
                colorHex: "#FF6B6B",
                description: "Deal 12 + Rage damage and spend 5 Rage. Upgrade: 22 damage and spend 4 Rage.");

            var vampiricSlash = CreateSkill(
                "vampiric_slash",
                "Vampiric Slash",
                SkillActionType.Attack,
                SkillTargetType.HighHpEnemy,
                attackAmount: 8,
                attackUpgradeAmount: 3,
                lifestealPercent: 50,
                lifestealUpgradePercent: 20,
                colorHex: "#E64980",
                description: "Deal 8 damage and heal 50% of dealt damage. Upgrade: 11 damage and heal 70%.");

            var slimeAttack4 = CreateSkill(
                "slime_attack_4",
                "Slime Jab",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 4,
                colorHex: "#69DB7C",
                description: "Deal 4 damage.");

            var slimeAttack5 = CreateSkill(
                "slime_attack_5",
                "Slime Slam",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 5,
                colorHex: "#51CF66",
                description: "Deal 5 damage.");

            var slimeGuard = CreateSkill(
                "slime_guard",
                "Slime Guard",
                SkillActionType.Defense,
                SkillTargetType.Self,
                shieldAmount: 6,
                colorHex: "#74C69D",
                description: "Gain 6 Shield.");

            var slimeBind = CreateSkill(
                "slime_bind",
                "Bind",
                SkillActionType.Debuff,
                SkillTargetType.RandomEnemy,
                dicePointModifierAmount: -1,
                colorHex: "#38D9A9",
                description: "Apply DP -1 next turn.");

            var goblinAttack5 = CreateSkill(
                "goblin_attack_5",
                "Goblin Stab",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 5,
                colorHex: "#FCC419",
                description: "Deal 5 damage.");

            var goblinAttack6 = CreateSkill(
                "goblin_attack_6",
                "Goblin Rush",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 6,
                colorHex: "#FAB005",
                description: "Deal 6 damage.");

            var goblinDoubleStrike = CreateSkill(
                "goblin_double_strike",
                "Double Attack",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 4,
                repeatCount: 2,
                colorHex: "#FD7E14",
                description: "Deal 4 damage twice.");

            var goblinDefense = CreateSkill(
                "goblin_defense",
                "Goblin Guard",
                SkillActionType.Defense,
                SkillTargetType.Self,
                shieldAmount: 6,
                colorHex: "#9775FA",
                description: "Gain 6 Shield.");

            var golemDefense = CreateSkill(
                "golem_defense",
                "Stone Guard",
                SkillActionType.Defense,
                SkillTargetType.Self,
                shieldAmount: 10,
                colorHex: "#A5D8FF",
                description: "Gain 10 Shield.");

            var golemArmorUp = CreateSkill(
                "golem_armor_up",
                "Armor Up",
                SkillActionType.Defense,
                SkillTargetType.Self,
                armorAmount: 5,
                colorHex: "#DEE2E6",
                description: "Gain 5 Armor.");

            var golemAttack = CreateSkill(
                "golem_attack",
                "Stone Punch",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 8,
                colorHex: "#868E96",
                description: "Deal 8 damage.");

            var summonedGoblinAttack = CreateSkill(
                "summoned_goblin_attack",
                "Summoned Stab",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 5,
                colorHex: "#F59F00",
                description: "Deal 5 damage.");

            var summonedGoblinDoubleAttack = CreateSkill(
                "summoned_goblin_double_attack",
                "Summoned Double",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 3,
                repeatCount: 2,
                colorHex: "#FF922B",
                description: "Deal 3 damage twice.");

            var summonedGoblin = CreateCombatant(
                "summoned_goblin",
                "Summoned Goblin",
                11,
                1,
                0,
                false,
                0,
                summonedGoblinAttack,
                summonedGoblinAttack,
                summonedGoblinAttack,
                summonedGoblinAttack,
                summonedGoblinDoubleAttack,
                summonedGoblinDoubleAttack);

            var shamanAttack = CreateSkill(
                "shaman_attack",
                "Hex Bolt",
                SkillActionType.Attack,
                SkillTargetType.RandomEnemy,
                attackAmount: 7,
                colorHex: "#AE3EC9",
                description: "Deal 7 damage.");

            var shamanWeaken = CreateSkill(
                "shaman_weaken",
                "Weaken",
                SkillActionType.Debuff,
                SkillTargetType.RandomEnemy,
                attackModifierAmount: -3,
                colorHex: "#C2255C",
                description: "Apply Attack -3 next turn.");

            var shamanSummonGoblin = CreateSkill(
                "shaman_summon_goblin",
                "Summon Goblin",
                SkillActionType.Buff,
                SkillTargetType.Self,
                summonTemplate: summonedGoblin,
                summonCount: 1,
                maxSummonedAllies: 2,
                colorHex: "#845EF7",
                description: "Summon 1 goblin. Maximum 2 summoned goblins.");

            var shamanAttackAura = CreateSkill(
                "shaman_attack_aura",
                "Attack Aura",
                SkillActionType.Buff,
                SkillTargetType.Self,
                summonedAllyAttackBonusAmount: 3,
                colorHex: "#D9480F",
                description: "Summoned goblins gain +3 attack.");

            var starterLoadout = CreateLoadout(
                "balanced_starter",
                "Balanced Starter",
                DiceBuildIdentity.Balanced,
                basicAttack,
                defensiveStance,
                basicAttack,
                focusedDefense,
                counter,
                fury);

            var player = CreateCombatant(
                "player_knight",
                "Dice Knight",
                100,
                2,
                0,
                false,
                0,
                starterLoadout,
                starterLoadout.Faces.ToArray());

            var slime = CreateCombatant(
                "slime",
                "Slime",
                14,
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
                "Goblin",
                16,
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
                "Golem",
                60,
                2,
                10,
                false,
                30,
                golemDefense,
                golemDefense,
                golemArmorUp,
                golemArmorUp,
                golemAttack,
                counter);

            var shaman = CreateCombatant(
                "shaman",
                "Shaman",
                80,
                2,
                0,
                true,
                0,
                shamanAttack,
                shamanWeaken,
                shamanWeaken,
                shamanSummonGoblin,
                shamanSummonGoblin,
                shamanAttackAura);

            var slimeSingleEncounter = CreateEncounter("encounter_slime_single", "Slime Pressure", false, slime);
            var slimePairEncounter = CreateEncounter("encounter_slime_pair", "Slime Swarm", false, slime, slime);
            var goblinRaidEncounter = CreateEncounter("encounter_goblin_raid", "Goblin Raiders", false, goblin, goblin);
            var golemWatchEncounter = CreateEncounter("encounter_golem_watch", "Stone Sentinel", false, golem);
            var shamanBossEncounter = CreateEncounter("encounter_shaman_boss", "Ritual Chamber", true, shaman);

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
                defensiveStance,
                focusedDefense,
                counter,
                shieldBurst,
                bloodSlash,
                fury,
                savageStrike,
                vampiricSlash
            });
            SetPrivateField(config, "normalEnemies", new List<CombatantTemplate> { slime, goblin, golem, summonedGoblin });
            SetPrivateField(config, "bossEnemy", shaman);
            SetPrivateField(config, "encounterTable", new List<EncounterDefinition>
            {
                slimeSingleEncounter,
                slimePairEncounter,
                goblinRaidEncounter,
                golemWatchEncounter,
                shamanBossEncounter
            });
            SetPrivateField(config, "mapNodes", CreateGridMapNodes(new List<GridMapNodeSeed>
            {
                CreateSeed("node_01", "Bone Trail", MapNodeType.Battle, 1, 0, slimePairEncounter),
                CreateSeed("node_02", "Ancient Cache", MapNodeType.Reward, 2, 0),
                CreateSeed("node_03", "Traveling Forge", MapNodeType.Shop, 3, 0),
                CreateSeed("node_04", "Slime Crossing", MapNodeType.Battle, 0, 1, slimeSingleEncounter),
                CreateSeed("node_05", "Silent Crypt", MapNodeType.EliteBattle, 1, 1, golemWatchEncounter),
                CreateSeed("node_06", "Forked Tunnel", MapNodeType.Battle, 2, 1, goblinRaidEncounter),
                CreateSeed("node_07", "Forgotten Script", MapNodeType.Reward, 3, 1),
                CreateSeed("node_08", "Hidden Shrine", MapNodeType.Reward, 0, 2),
                CreateSeed("node_09", "Raider Nest", MapNodeType.Battle, 1, 2, goblinRaidEncounter),
                CreateSeed("node_10", "Stone Ward", MapNodeType.EliteBattle, 2, 2, golemWatchEncounter),
                CreateSeed("node_11", "Ritual Stairs", MapNodeType.Battle, 3, 2, slimePairEncounter),
                CreateSeed("node_12", "Upper Path", MapNodeType.Battle, 0, 3, slimeSingleEncounter),
                CreateSeed("node_13", "Old Reliquary", MapNodeType.Reward, 1, 3),
                CreateSeed("node_14", "Black Market", MapNodeType.Shop, 2, 3),
                CreateSeed("node_15", "Overlord Nest", MapNodeType.Boss, 3, 3, shamanBossEncounter)
            }));
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
            CombatantTemplate summonTemplate = null,
            int summonCount = 0,
            int maxSummonedAllies = 0,
            int summonedAllyAttackBonusAmount = 0,
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
            SetPrivateField(definition, "summonTemplate", summonTemplate);
            SetPrivateField(definition, "summonCount", summonCount);
            SetPrivateField(definition, "maxSummonedAllies", maxSummonedAllies);
            SetPrivateField(definition, "summonedAllyAttackBonusAmount", summonedAllyAttackBonusAmount);
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
            DiceLoadoutDefinition loadout,
            params SkillDefinition[] diceSkills)
        {
            var definition = CreateCombatant(
                id,
                displayName,
                maxHp,
                baseDicePoints,
                passiveShieldPerTurn,
                isBoss,
                passiveReflectPercent,
                diceSkills);
            SetPrivateField(definition, "diceLoadout", loadout);
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

        private static GridMapNodeSeed CreateSeed(
            string id,
            string displayName,
            MapNodeType type,
            int gridX,
            int gridY,
            EncounterDefinition encounterDefinition = null,
            CombatantTemplate enemyTemplate = null)
        {
            return new GridMapNodeSeed
            {
                Id = id,
                DisplayName = displayName,
                NodeType = type,
                GridX = gridX,
                GridY = gridY,
                EncounterDefinition = encounterDefinition,
                EnemyTemplate = enemyTemplate
            };
        }

        private static List<MapNodeDefinition> CreateGridMapNodes(IReadOnlyList<GridMapNodeSeed> seeds)
        {
            var orderedSeeds = seeds
                .OrderBy(seed => seed.GridY)
                .ThenBy(seed => seed.GridX)
                .ToList();
            var indexByPosition = new Dictionary<Vector2Int, int>();

            for (var index = 0; index < orderedSeeds.Count; index++)
            {
                indexByPosition[new Vector2Int(orderedSeeds[index].GridX, orderedSeeds[index].GridY)] = index;
            }

            var definitions = new List<MapNodeDefinition>(orderedSeeds.Count);

            for (var index = 0; index < orderedSeeds.Count; index++)
            {
                var seed = orderedSeeds[index];
                var nextNodeIndices = new List<int>();

                if (indexByPosition.TryGetValue(new Vector2Int(seed.GridX + 1, seed.GridY), out var rightIndex))
                {
                    nextNodeIndices.Add(rightIndex);
                }

                if (indexByPosition.TryGetValue(new Vector2Int(seed.GridX, seed.GridY + 1), out var upperIndex))
                {
                    nextNodeIndices.Add(upperIndex);
                }

                definitions.Add(CreateNode(seed, nextNodeIndices));
            }

            return definitions;
        }

        private static MapNodeDefinition CreateNode(GridMapNodeSeed seed, List<int> nextNodes)
        {
            var definition = new MapNodeDefinition();
            SetPrivateField(definition, "id", seed.Id);
            SetPrivateField(definition, "displayName", seed.DisplayName);
            SetPrivateField(definition, "nodeType", seed.NodeType);
            SetPrivateField(definition, "gridX", seed.GridX);
            SetPrivateField(definition, "gridY", seed.GridY);
            SetPrivateField(definition, "encounterDefinition", seed.EncounterDefinition);
            SetPrivateField(definition, "enemyTemplate", seed.EncounterDefinition?.EnemyTemplates != null && seed.EncounterDefinition.EnemyTemplates.Count > 0
                ? seed.EncounterDefinition.EnemyTemplates[0]
                : seed.EnemyTemplate);
            SetPrivateField(definition, "nextNodeIndices", nextNodes);
            return definition;
        }

        private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            var field = typeof(TTarget).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private static DiceLoadoutDefinition CreateLoadout(
            string id,
            string displayName,
            DiceBuildIdentity identity,
            params SkillDefinition[] skills)
        {
            var loadout = ScriptableObject.CreateInstance<DiceLoadoutDefinition>();
            loadout.Configure(id, displayName, identity, skills);
            return loadout;
        }

        private static SkillDefinition FindSkill(IReadOnlyList<SkillDefinition> skillLibrary, string skillId)
        {
            if (skillLibrary == null)
            {
                return null;
            }

            for (var index = 0; index < skillLibrary.Count; index++)
            {
                if (skillLibrary[index] != null && skillLibrary[index].Id == skillId)
                {
                    return skillLibrary[index];
                }
            }

            return null;
        }

        private static EncounterDefinition CreateEncounter(
            string id,
            string displayName,
            bool isBossEncounter,
            params CombatantTemplate[] enemyTemplates)
        {
            var encounter = new EncounterDefinition();
            SetPrivateField(encounter, "id", id);
            SetPrivateField(encounter, "displayName", displayName);
            SetPrivateField(encounter, "isBossEncounter", isBossEncounter);
            SetPrivateField(encounter, "enemyTemplates", new List<CombatantTemplate>(enemyTemplates));
            return encounter;
        }

        private sealed class GridMapNodeSeed
        {
            public string Id;
            public string DisplayName;
            public MapNodeType NodeType;
            public int GridX;
            public int GridY;
            public EncounterDefinition EncounterDefinition;
            public CombatantTemplate EnemyTemplate;
        }
    }
}
