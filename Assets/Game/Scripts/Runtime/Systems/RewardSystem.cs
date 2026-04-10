using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceRogue
{
    public class RewardSystem
    {
        private readonly Random random = new Random();

        public List<RewardOptionRuntime> BuildRewards(
            RunConfig runConfig,
            IReadOnlyCollection<string> unlockedSkillIds,
            CombatantRuntimeState playerState,
            MapNodeType sourceNodeType)
        {
            var rewards = new List<RewardOptionRuntime>();
            if (runConfig == null)
            {
                return rewards;
            }

            var skillOfferCount = GetSkillOfferCount(sourceNodeType);
            var skillOffers = SelectSkillOffers(runConfig, unlockedSkillIds, playerState, sourceNodeType, skillOfferCount);

            foreach (var skill in skillOffers)
            {
                rewards.Add(new RewardOptionRuntime
                {
                    RewardType = RewardType.LearnSkill,
                    SkillDefinition = skill,
                    Title = $"New Face: {skill.DisplayName}",
                    Description = $"{skill.GetSummary()} Replace one die face with this skill."
                });
            }

            rewards.Add(new RewardOptionRuntime
            {
                RewardType = RewardType.UpgradeFace,
                SkillDefinition = null,
                Title = GetUpgradeTitle(sourceNodeType),
                Description = GetUpgradeDescription(sourceNodeType)
            });

            return rewards;
        }

        private int GetSkillOfferCount(MapNodeType sourceNodeType)
        {
            return sourceNodeType switch
            {
                MapNodeType.EliteBattle => 3,
                MapNodeType.Shop => 2,
                _ => 2
            };
        }

        private string GetUpgradeTitle(MapNodeType sourceNodeType)
        {
            return sourceNodeType switch
            {
                MapNodeType.EliteBattle => "Elite Upgrade",
                MapNodeType.Shop => "Forge Upgrade",
                MapNodeType.Reward => "Treasure Upgrade",
                _ => "Upgrade a Face"
            };
        }

        private string GetUpgradeDescription(MapNodeType sourceNodeType)
        {
            return sourceNodeType switch
            {
                MapNodeType.Shop => "Upgrade one current die face through the forge.",
                MapNodeType.Reward => "Upgrade one current die face from the treasure room.",
                MapNodeType.EliteBattle => "Upgrade one current die face after the elite fight.",
                _ => "Upgrade one current die face by 1 level."
            };
        }

        private List<SkillDefinition> SelectSkillOffers(
            RunConfig runConfig,
            IReadOnlyCollection<string> unlockedSkillIds,
            CombatantRuntimeState playerState,
            MapNodeType sourceNodeType,
            int offerCount)
        {
            var lockedSkills = runConfig.SkillLibrary
                .Where(skill => skill != null && (unlockedSkillIds == null || !unlockedSkillIds.Contains(skill.Id)))
                .ToList();

            var candidatePool = lockedSkills.Count > 0
                ? lockedSkills
                : runConfig.SkillLibrary.Where(skill => skill != null).ToList();

            var remaining = new List<SkillDefinition>(candidatePool);
            Shuffle(remaining);

            var selected = new List<SkillDefinition>(offerCount);
            var buildIdentity = GetCurrentBuildBias(playerState);

            if (sourceNodeType == MapNodeType.Reward || sourceNodeType == MapNodeType.EliteBattle)
            {
                TryTakeByTag(remaining, selected, IsDefensiveSkill);
                TryTakeByTag(remaining, selected, IsBerserkerSkill);
            }
            else if (sourceNodeType == MapNodeType.Shop)
            {
                if (buildIdentity == DiceBuildIdentity.Defensive)
                {
                    TryTakeByTag(remaining, selected, IsDefensiveSkill);
                    TryTakeByTag(remaining, selected, IsBerserkerSkill);
                }
                else if (buildIdentity == DiceBuildIdentity.Berserker)
                {
                    TryTakeByTag(remaining, selected, IsBerserkerSkill);
                    TryTakeByTag(remaining, selected, IsDefensiveSkill);
                }
            }
            else
            {
                if (buildIdentity == DiceBuildIdentity.Defensive)
                {
                    TryTakeByTag(remaining, selected, IsDefensiveSkill);
                }
                else if (buildIdentity == DiceBuildIdentity.Berserker)
                {
                    TryTakeByTag(remaining, selected, IsBerserkerSkill);
                }
            }

            while (selected.Count < offerCount && remaining.Count > 0)
            {
                selected.Add(remaining[0]);
                remaining.RemoveAt(0);
            }

            return selected;
        }

        private DiceBuildIdentity GetCurrentBuildBias(CombatantRuntimeState playerState)
        {
            if (playerState == null)
            {
                return DiceBuildIdentity.Balanced;
            }

            var defensiveScore = 0;
            var berserkerScore = 0;

            foreach (var face in playerState.DiceFaces)
            {
                if (face?.Skill == null)
                {
                    continue;
                }

                if (IsDefensiveSkill(face.Skill))
                {
                    defensiveScore += 1;
                }

                if (IsBerserkerSkill(face.Skill))
                {
                    berserkerScore += 1;
                }
            }

            if (defensiveScore > berserkerScore)
            {
                return DiceBuildIdentity.Defensive;
            }

            if (berserkerScore > defensiveScore)
            {
                return DiceBuildIdentity.Berserker;
            }

            return DiceBuildIdentity.Balanced;
        }

        private bool IsDefensiveSkill(SkillDefinition skill)
        {
            if (skill == null)
            {
                return false;
            }

            return skill.Id == "defensive_stance"
                || skill.Id == "focused_defense"
                || skill.Id == "counter"
                || skill.Id == "shield_burst";
        }

        private bool IsBerserkerSkill(SkillDefinition skill)
        {
            if (skill == null)
            {
                return false;
            }

            return skill.Id == "blood_slash"
                || skill.Id == "fury"
                || skill.Id == "savage_strike"
                || skill.Id == "vampiric_slash";
        }

        private void TryTakeByTag(List<SkillDefinition> remaining, List<SkillDefinition> selected, Func<SkillDefinition, bool> predicate)
        {
            for (var index = 0; index < remaining.Count; index++)
            {
                if (!predicate(remaining[index]))
                {
                    continue;
                }

                selected.Add(remaining[index]);
                remaining.RemoveAt(index);
                return;
            }
        }

        private void Shuffle(List<SkillDefinition> skills)
        {
            for (var index = skills.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(0, index + 1);
                (skills[index], skills[swapIndex]) = (skills[swapIndex], skills[index]);
            }
        }
    }
}
