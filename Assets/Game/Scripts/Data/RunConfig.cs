using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceRogue
{
    [CreateAssetMenu(menuName = "DiceRogue/Run Config", fileName = "RunConfig")]
    public class RunConfig : ScriptableObject
    {
        [SerializeField] private string bootSceneName = "Boot";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string mapSceneName = "MapScene";
        [SerializeField] private string battleSceneName = "BattleScene";
        [SerializeField] private string rewardSceneName = "RewardScene";
        [SerializeField] private CombatantTemplate playerTemplate;
        [SerializeField] private List<SkillDefinition> skillLibrary = new List<SkillDefinition>();
        [SerializeField] private List<CombatantTemplate> normalEnemies = new List<CombatantTemplate>();
        [SerializeField] private CombatantTemplate bossEnemy;
        [SerializeField] private List<EncounterDefinition> encounterTable = new List<EncounterDefinition>();
        [SerializeField] private List<MapNodeDefinition> mapNodes = new List<MapNodeDefinition>();
        [SerializeField] private float autoTurnDelay = 0.8f;
        [SerializeField] private int maxBattleTurns = 12;

        public string BootSceneName => bootSceneName;
        public string MainMenuSceneName => mainMenuSceneName;
        public string MapSceneName => mapSceneName;
        public string BattleSceneName => battleSceneName;
        public string RewardSceneName => rewardSceneName;
        public CombatantTemplate PlayerTemplate => playerTemplate;
        public IReadOnlyList<SkillDefinition> SkillLibrary => skillLibrary;
        public IReadOnlyList<CombatantTemplate> NormalEnemies => normalEnemies;
        public CombatantTemplate BossEnemy => bossEnemy;
        public IReadOnlyList<EncounterDefinition> EncounterTable => encounterTable;
        public IReadOnlyList<MapNodeDefinition> MapNodes => mapNodes;
        public float AutoTurnDelay => autoTurnDelay;
        public int MaxBattleTurns => maxBattleTurns;
    }

    [Serializable]
    public class EncounterDefinition
    {
        [SerializeField] private string id = "encounter_id";
        [SerializeField] private string displayName = "Encounter";
        [SerializeField] private bool isBossEncounter;
        [SerializeField] private List<CombatantTemplate> enemyTemplates = new List<CombatantTemplate>();

        public string Id => id;
        public string DisplayName => displayName;
        public bool IsBossEncounter => isBossEncounter;
        public IReadOnlyList<CombatantTemplate> EnemyTemplates => enemyTemplates;

        public string GetEnemySummary()
        {
            return string.Join(", ", enemyTemplates.FindAll(template => template != null).ConvertAll(template => template.DisplayName));
        }
    }

    [Serializable]
    public class MapNodeDefinition
    {
        [SerializeField] private string id = "node_id";
        [SerializeField] private string displayName = "Battle";
        [SerializeField] private MapNodeType nodeType = MapNodeType.Battle;
        [SerializeField] private CombatantTemplate enemyTemplate;
        [SerializeField] private EncounterDefinition encounterDefinition;
        [SerializeField] private List<int> nextNodeIndices = new List<int>();

        public string Id => id;
        public string DisplayName => displayName;
        public MapNodeType NodeType => nodeType;
        public CombatantTemplate EnemyTemplate => enemyTemplate;
        public EncounterDefinition EncounterDefinition => encounterDefinition;
        public IReadOnlyList<int> NextNodeIndices => nextNodeIndices;
        public bool IsCombatNode => nodeType == MapNodeType.Battle || nodeType == MapNodeType.EliteBattle || nodeType == MapNodeType.Boss;

        public IReadOnlyList<CombatantTemplate> GetEnemyTemplates()
        {
            if (encounterDefinition != null && encounterDefinition.EnemyTemplates != null && encounterDefinition.EnemyTemplates.Count > 0)
            {
                return encounterDefinition.EnemyTemplates;
            }

            return enemyTemplate != null ? new[] { enemyTemplate } : Array.Empty<CombatantTemplate>();
        }

        public string GetEncounterSummary()
        {
            if (encounterDefinition != null)
            {
                return encounterDefinition.GetEnemySummary();
            }

            if (enemyTemplate != null)
            {
                return enemyTemplate.DisplayName;
            }

            return nodeType switch
            {
                MapNodeType.Reward => "Gain a free reward.",
                MapNodeType.Shop => "Choose a forge reward.",
                MapNodeType.EliteBattle => "Elite combat encounter.",
                MapNodeType.Boss => "Boss encounter.",
                _ => "No encounter."
            };
        }

        public string GetNodeTypeLabel()
        {
            return nodeType switch
            {
                MapNodeType.Battle => "Battle",
                MapNodeType.EliteBattle => "Elite",
                MapNodeType.Reward => "Reward",
                MapNodeType.Shop => "Shop",
                MapNodeType.Boss => "Boss",
                _ => "Node"
            };
        }
    }
}
