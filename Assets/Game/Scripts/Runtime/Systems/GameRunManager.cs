using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiceRogue
{
    public class GameRunManager : MonoBehaviour
    {
        private static GameRunManager instance;

        [SerializeField] private RunConfig defaultRunConfig;
        [SerializeField] private bool dontDestroyAcrossScenes = true;

        private readonly HashSet<string> unlockedSkillIds = new HashSet<string>();

        public static GameRunManager Instance => instance;

        public RunConfig Config { get; private set; }
        public CombatantRuntimeState PlayerState { get; private set; }
        public MapSystem MapSystem { get; private set; }
        public RewardSystem RewardSystem { get; private set; }
        public DiceSystem DiceSystem { get; private set; }
        public BattleSystem BattleSystem { get; private set; }
        public List<RewardOptionRuntime> CurrentRewards { get; private set; } = new List<RewardOptionRuntime>();
        public int CurrentBattleNodeIndex { get; private set; } = -1;
        public int CurrentMapNodeIndex { get; private set; } = -1;
        public int CurrentRewardNodeIndex { get; private set; } = -1;
        public MapNodeType CurrentRewardSourceType { get; private set; } = MapNodeType.Reward;
        public bool AutoBattleEnabled { get; set; } = true;
        public string LastRunMessage { get; private set; } = "Start a run and grow your die faces.";

        public IReadOnlyCollection<string> UnlockedSkillIds => unlockedSkillIds;

        public static GameRunManager EnsureInstance(RunConfig preferredConfig = null)
        {
            if (instance != null)
            {
                if (preferredConfig != null && instance.Config == null)
                {
                    instance.Initialize(preferredConfig);
                }

                return instance;
            }

            var existing = FindAnyObjectByType<GameRunManager>();
            if (existing != null)
            {
                instance = existing;
                if (preferredConfig != null && instance.Config == null)
                {
                    instance.Initialize(preferredConfig);
                }

                return instance;
            }

            var gameObject = new GameObject("GameRunManager");
            instance = gameObject.AddComponent<GameRunManager>();
            instance.Initialize(preferredConfig);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (dontDestroyAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (Config == null)
            {
                Initialize(defaultRunConfig);
            }
        }

        public void Initialize(RunConfig preferredConfig = null)
        {
            var sourceConfig = preferredConfig != null ? preferredConfig : Resources.Load<RunConfig>("DiceRogue/RunConfig");
            Config = RunContentFactory.BuildRuntimeConfig(sourceConfig);
            RewardSystem = new RewardSystem();
            DiceSystem = new DiceSystem();
            MapSystem ??= new MapSystem();
        }

        public void StartNewRun()
        {
            Initialize(Config);

            PlayerState = new CombatantRuntimeState(Config.PlayerTemplate);
            MapSystem.BuildMap(Config.MapNodes);
            CurrentRewards = new List<RewardOptionRuntime>();
            CurrentBattleNodeIndex = -1;
            CurrentMapNodeIndex = -1;
            CurrentRewardNodeIndex = -1;
            CurrentRewardSourceType = MapNodeType.Reward;
            AutoBattleEnabled = true;
            unlockedSkillIds.Clear();

            foreach (var skillId in PlayerState.DiceFaces
                         .Where(face => face.Skill != null)
                         .Select(face => face.Skill.Id))
            {
                unlockedSkillIds.Add(skillId);
            }

            LastRunMessage = "A new run has started. Route through the dungeon and reach the boss.";
        }

        public void StartRunFromMenu()
        {
            StartNewRun();
            SceneManager.LoadScene(Config.MapSceneName);
        }

        public void StartDebugBattle()
        {
            StartNewRun();
            var node = MapSystem.GetAvailableNodes().FirstOrDefault(runtimeNode => runtimeNode.Definition.IsCombatNode);
            if (node != null)
            {
                PrepareBattle(node.Index);
                SceneManager.LoadScene(Config.BattleSceneName);
            }
        }

        public void StartDebugReward()
        {
            StartNewRun();
            CurrentRewardNodeIndex = -1;
            CurrentRewardSourceType = MapNodeType.Reward;
            CurrentRewards = RewardSystem.BuildRewards(Config, unlockedSkillIds, PlayerState, CurrentRewardSourceType);
            SceneManager.LoadScene(Config.RewardSceneName);
        }

        public void EnsureDebugRunForScene()
        {
            if (PlayerState == null)
            {
                StartNewRun();
            }
        }

        public void EnsureRewardChoices()
        {
            EnsureDebugRunForScene();

            if (CurrentRewards == null || CurrentRewards.Count == 0)
            {
                CurrentRewards = RewardSystem.BuildRewards(Config, unlockedSkillIds, PlayerState, CurrentRewardSourceType);
            }
        }

        public void SelectMapNode(int nodeIndex)
        {
            EnsureDebugRunForScene();

            var node = MapSystem.GetNode(nodeIndex);
            if (node == null || !node.IsUnlocked || node.IsCompleted)
            {
                return;
            }

            CurrentMapNodeIndex = nodeIndex;

            switch (node.Definition.NodeType)
            {
                case MapNodeType.Reward:
                case MapNodeType.Shop:
                    OpenRewardNode(node);
                    break;
                case MapNodeType.Battle:
                case MapNodeType.EliteBattle:
                case MapNodeType.Boss:
                default:
                    PrepareBattle(node.Index);
                    SceneManager.LoadScene(Config.BattleSceneName);
                    break;
            }
        }

        public void PrepareBattle(int nodeIndex)
        {
            EnsureDebugRunForScene();

            var node = MapSystem.GetNode(nodeIndex);
            if (node == null || !node.IsUnlocked || node.IsCompleted || !node.Definition.IsCombatNode)
            {
                return;
            }

            CurrentMapNodeIndex = nodeIndex;
            CurrentBattleNodeIndex = nodeIndex;
            CurrentRewardNodeIndex = -1;
            CurrentRewards.Clear();
            BattleSystem = new BattleSystem(DiceSystem);
            if (node.Definition.EncounterDefinition != null)
            {
                BattleSystem.BeginBattle(PlayerState, node.Definition.EncounterDefinition, Config.MaxBattleTurns);
            }
            else
            {
                BattleSystem.BeginBattle(PlayerState, node.Definition.EnemyTemplate, Config.MaxBattleTurns);
            }
        }

        public void BeginBattleFromMap(int nodeIndex)
        {
            SelectMapNode(nodeIndex);
        }

        public void CompleteBattleAndAdvance()
        {
            if (BattleSystem == null)
            {
                return;
            }

            if (BattleSystem.BattleResult == BattleResultType.Defeat)
            {
                LastRunMessage = "The run ended. Rework your route and die faces, then try again.";
                SceneManager.LoadScene(Config.MainMenuSceneName);
                return;
            }

            var node = MapSystem.GetNode(CurrentBattleNodeIndex);
            if (node == null)
            {
                SceneManager.LoadScene(Config.MapSceneName);
                return;
            }

            MapSystem.CompleteNode(CurrentBattleNodeIndex);

            if (node.Definition.NodeType == MapNodeType.Boss)
            {
                LastRunMessage = "The stage boss was defeated. The run is complete.";
                ClearRewardState();
                SceneManager.LoadScene(Config.MainMenuSceneName);
                return;
            }

            CurrentRewardNodeIndex = CurrentBattleNodeIndex;
            CurrentRewardSourceType = node.Definition.NodeType;
            CurrentRewards = RewardSystem.BuildRewards(Config, unlockedSkillIds, PlayerState, CurrentRewardSourceType);
            LastRunMessage = $"{node.Definition.DisplayName} cleared. Choose one reward.";
            SceneManager.LoadScene(Config.RewardSceneName);
        }

        public void ApplyReward(RewardOptionRuntime reward, int slotIndex)
        {
            if (reward == null || PlayerState == null)
            {
                return;
            }

            switch (reward.RewardType)
            {
                case RewardType.LearnSkill:
                    if (reward.SkillDefinition != null)
                    {
                        unlockedSkillIds.Add(reward.SkillDefinition.Id);
                        PlayerState.ReplaceFace(slotIndex, reward.SkillDefinition);
                        LastRunMessage = $"{reward.SkillDefinition.DisplayName} was added to the die.";
                    }
                    break;
                case RewardType.UpgradeFace:
                    PlayerState.UpgradeFace(slotIndex);
                    LastRunMessage = $"Face {slotIndex + 1} was upgraded.";
                    break;
            }

            ClearRewardState();
        }

        public void SkipCurrentReward()
        {
            LastRunMessage = CurrentRewardSourceType == MapNodeType.Shop
                ? "You left the shop without taking a forge reward."
                : "You skipped the current reward.";
            ClearRewardState();
            SceneManager.LoadScene(Config.MapSceneName);
        }

        public void ReturnToMap()
        {
            SceneManager.LoadScene(Config.MapSceneName);
        }

        public bool ApplyPlayerBuildIdentity(DiceBuildIdentity identity)
        {
            if (PlayerState == null || Config == null)
            {
                return false;
            }

            var applied = PlayerState.ApplyDiceBuildIdentity(identity, Config.SkillLibrary);
            if (!applied)
            {
                return false;
            }

            unlockedSkillIds.Clear();
            foreach (var skillId in PlayerState.DiceFaces
                         .Where(face => face.Skill != null)
                         .Select(face => face.Skill.Id))
            {
                unlockedSkillIds.Add(skillId);
            }

            LastRunMessage = identity switch
            {
                DiceBuildIdentity.Defensive => "The die was rebuilt into a Defensive identity.",
                DiceBuildIdentity.Berserker => "The die was rebuilt into a Berserker identity.",
                _ => "The die was rebuilt into a Balanced identity."
            };
            return true;
        }

        public string GetUnlockedSkillSummary()
        {
            return string.Join(", ", Config.SkillLibrary
                .Where(skill => skill != null && unlockedSkillIds.Contains(skill.Id))
                .Select(skill => skill.DisplayName));
        }

        public string GetRewardContextLabel()
        {
            return CurrentRewardSourceType switch
            {
                MapNodeType.EliteBattle => "Elite Reward",
                MapNodeType.Shop => "Forge Shop",
                MapNodeType.Reward => "Treasure Reward",
                _ => "Battle Reward"
            };
        }

        private void OpenRewardNode(MapNodeRuntimeState node)
        {
            if (node == null)
            {
                return;
            }

            BattleSystem = null;
            CurrentBattleNodeIndex = -1;
            CurrentRewardNodeIndex = node.Index;
            CurrentRewardSourceType = node.Definition.NodeType;
            MapSystem.CompleteNode(node.Index);
            CurrentRewards = RewardSystem.BuildRewards(Config, unlockedSkillIds, PlayerState, CurrentRewardSourceType);
            LastRunMessage = $"{node.Definition.DisplayName} entered. Choose a reward.";
            SceneManager.LoadScene(Config.RewardSceneName);
        }

        private void ClearRewardState()
        {
            CurrentRewards.Clear();
            CurrentRewardNodeIndex = -1;
        }
    }
}
