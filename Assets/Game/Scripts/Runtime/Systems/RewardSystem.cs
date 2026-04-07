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
                    Title = $"Learn {skill.DisplayName}",
                    Description = $"{skill.GetSummary()} and equip it into one die slot."
                });
            }

            rewards.Add(new RewardOptionRuntime
            {
                RewardType = RewardType.UpgradeFace,
                SkillDefinition = null,
                Title = "Upgrade One Face",
                Description = "Choose one current die face to improve by +1."
            });

            return rewards;
        }
    }
}
