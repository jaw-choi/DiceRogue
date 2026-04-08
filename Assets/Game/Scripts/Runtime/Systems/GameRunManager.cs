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
        public bool AutoBattleEnabled { get; set; } = true;
        public string LastRunMessage { get; private set; } = "새 게임을 시작해 주사위를 성장시키세요.";

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
            AutoBattleEnabled = true;
            unlockedSkillIds.Clear();

            foreach (var skillId in PlayerState.DiceFaces
                         .Where(face => face.Skill != null)
                         .Select(face => face.Skill.Id))
            {
                unlockedSkillIds.Add(skillId);
            }

            LastRunMessage = "런이 시작되었습니다. 주사위 면을 성장시키며 보스를 향해 이동하세요.";
        }

        public void StartRunFromMenu()
        {
            StartNewRun();
            SceneManager.LoadScene(Config.MapSceneName);
        }

        public void StartDebugBattle()
        {
            StartNewRun();
            var node = MapSystem.GetAvailableNodes().FirstOrDefault();
            if (node != null)
            {
                PrepareBattle(node.Index);
                SceneManager.LoadScene(Config.BattleSceneName);
            }
        }

        public void StartDebugReward()
        {
            StartNewRun();
            CurrentRewards = RewardSystem.BuildRewards(Config, unlockedSkillIds);
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
                CurrentRewards = RewardSystem.BuildRewards(Config, unlockedSkillIds);
            }
        }

        public void PrepareBattle(int nodeIndex)
        {
            EnsureDebugRunForScene();

            var node = MapSystem.GetNode(nodeIndex);
            if (node == null || !node.IsUnlocked || node.IsCompleted)
            {
                return;
            }

            CurrentBattleNodeIndex = nodeIndex;
            BattleSystem = new BattleSystem(DiceSystem);
            BattleSystem.BeginBattle(PlayerState, node.Definition.EnemyTemplate, Config.MaxBattleTurns);
        }

        public void BeginBattleFromMap(int nodeIndex)
        {
            PrepareBattle(nodeIndex);
            SceneManager.LoadScene(Config.BattleSceneName);
        }

        public void CompleteBattleAndAdvance()
        {
            if (BattleSystem == null)
            {
                return;
            }

            if (BattleSystem.BattleResult == BattleResultType.Defeat)
            {
                LastRunMessage = "런이 종료되었습니다. 방어와 분노 타이밍을 다시 조정해 보세요.";
                SceneManager.LoadScene(Config.MainMenuSceneName);
                return;
            }

            var node = MapSystem.GetNode(CurrentBattleNodeIndex);
            MapSystem.CompleteNode(CurrentBattleNodeIndex);

            if (node != null && node.Definition.NodeType == MapNodeType.Boss)
            {
                LastRunMessage = "보스를 처치했습니다. 새로운 빌드로 다시 도전해 보세요.";
                SceneManager.LoadScene(Config.MainMenuSceneName);
                return;
            }

            CurrentRewards = RewardSystem.BuildRewards(Config, unlockedSkillIds);
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
                    }
                    break;
                case RewardType.UpgradeFace:
                    PlayerState.UpgradeFace(slotIndex);
                    break;
            }

            CurrentRewards.Clear();
        }

        public void ReturnToMap()
        {
            SceneManager.LoadScene(Config.MapSceneName);
        }

        public string GetUnlockedSkillSummary()
        {
            return string.Join(", ", Config.SkillLibrary
                .Where(skill => skill != null && unlockedSkillIds.Contains(skill.Id))
                .Select(skill => skill.DisplayName));
        }
    }
}
