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
        public IReadOnlyList<MapNodeDefinition> MapNodes => mapNodes;
        public float AutoTurnDelay => autoTurnDelay;
        public int MaxBattleTurns => maxBattleTurns;
    }

    [Serializable]
    public class MapNodeDefinition
    {
        [SerializeField] private string id = "node_id";
        [SerializeField] private string displayName = "Battle";
        [SerializeField] private MapNodeType nodeType = MapNodeType.Battle;
        [SerializeField] private CombatantTemplate enemyTemplate;
        [SerializeField] private List<int> nextNodeIndices = new List<int>();

        public string Id => id;
        public string DisplayName => displayName;
        public MapNodeType NodeType => nodeType;
        public CombatantTemplate EnemyTemplate => enemyTemplate;
        public IReadOnlyList<int> NextNodeIndices => nextNodeIndices;
    }
}
