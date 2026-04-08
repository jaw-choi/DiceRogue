using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceRogue
{
    public class RewardSystem
    {
        private readonly Random random = new Random();

        public List<RewardOptionRuntime> BuildRewards(RunConfig runConfig, IReadOnlyCollection<string> unlockedSkillIds)
        {
            var rewards = new List<RewardOptionRuntime>();

            var lockedSkills = runConfig.SkillLibrary
                .Where(skill => skill != null && !unlockedSkillIds.Contains(skill.Id))
                .OrderBy(_ => random.Next())
                .Take(2)
                .ToList();

            foreach (var skill in lockedSkills)
            {
                rewards.Add(new RewardOptionRuntime
                {
                    RewardType = RewardType.LearnSkill,
                    SkillDefinition = skill,
                    Title = $"{skill.DisplayName} 습득",
                    Description = $"{skill.GetSummary()} 효과를 획득하고 주사위 면 하나에 장착합니다."
                });
            }

            rewards.Add(new RewardOptionRuntime
            {
                RewardType = RewardType.UpgradeFace,
                SkillDefinition = null,
                Title = "면 강화",
                Description = "현재 주사위 면 하나를 선택해 강화 레벨을 1 올립니다."
            });

            return rewards;
        }
    }
}
