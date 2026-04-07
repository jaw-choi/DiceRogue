using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DiceRogue
{
    [Serializable]
    public class DiceFaceState
    {
        [SerializeField] private SkillDefinition skill;
        [SerializeField] private int upgradeLevel;

        public DiceFaceState(SkillDefinition skill)
        {
            this.skill = skill;
        }

        public SkillDefinition Skill => skill;
        public int UpgradeLevel => upgradeLevel;

        public int AttackValue => skill == null ? 0 : skill.AttackAmount + (skill.ActionType == SkillActionType.Attack ? upgradeLevel : 0);
        public int ShieldValue => skill == null ? 0 : skill.ShieldAmount + (skill.ActionType == SkillActionType.Guard ? upgradeLevel : 0);
        public int HealValue => skill == null ? 0 : skill.HealAmount + (skill.ActionType == SkillActionType.Heal ? upgradeLevel : 0);
        public int SelfDamageValue => skill == null ? 0 : skill.SelfDamageAmount;
        public int RageGainValue => skill == null ? 0 : skill.RageGainAmount + (skill.ActionType == SkillActionType.Berserk ? upgradeLevel : 0);

        public void ReplaceSkill(SkillDefinition newSkill)
        {
            skill = newSkill;
            upgradeLevel = 0;
        }

        public void ApplyUpgrade()
        {
            upgradeLevel += 1;
        }

        public string GetSummary()
        {
            return skill == null ? "Empty" : $"{skill.DisplayName} Lv.{upgradeLevel} / {skill.GetSummary(upgradeLevel)}";
        }
    }

    [Serializable]
    public class CombatantRuntimeState
    {
        private readonly List<DiceFaceState> diceFaces = new List<DiceFaceState>(6);

        public CombatantRuntimeState(CombatantTemplate template)
        {
            Template = template;
            DisplayName = template.DisplayName;
            MaxHp = template.MaxHp;
            CurrentHp = template.MaxHp;
            Armor = template.StartingArmor;
            Rage = template.StartingRage;

            foreach (var skill in template.DiceSkills)
            {
                diceFaces.Add(new DiceFaceState(skill));
            }
        }

        public CombatantTemplate Template { get; }
        public string DisplayName { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int Shield { get; private set; }
        public int Armor { get; private set; }
        public int Rage { get; private set; }
        public DiceFaceState LastRolledFace { get; private set; }
        public IReadOnlyList<DiceFaceState> DiceFaces => diceFaces;
        public bool IsAlive => CurrentHp > 0;

        public void ResetForBattle(bool restoreHp)
        {
            if (restoreHp)
            {
                CurrentHp = MaxHp;
            }

            Shield = 0;
            Rage = Template.StartingRage;
            Armor = Template.StartingArmor;
            LastRolledFace = null;
        }

        public void SetLastRolledFace(DiceFaceState face)
        {
            LastRolledFace = face;
        }

        public void GainShield(int amount)
        {
            Shield += Mathf.Max(0, amount);
        }

        public void GainRage(int amount)
        {
            Rage += Mathf.Max(0, amount);
        }

        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + Mathf.Max(0, amount));
        }

        public void LoseHpDirect(int amount)
        {
            CurrentHp = Mathf.Max(0, CurrentHp - Mathf.Max(0, amount));
        }

        public DamageResolution TakeAttack(int rawDamage)
        {
            var damageAfterArmor = Mathf.Max(0, rawDamage - Armor);
            var shieldBlocked = Mathf.Min(Shield, damageAfterArmor);
            Shield -= shieldBlocked;

            var hpDamage = damageAfterArmor - shieldBlocked;
            if (hpDamage > 0)
            {
                CurrentHp = Mathf.Max(0, CurrentHp - hpDamage);
            }

            return new DamageResolution(rawDamage, damageAfterArmor, shieldBlocked, hpDamage);
        }

        public void ReplaceFace(int slotIndex, SkillDefinition skill)
        {
            if (slotIndex < 0 || slotIndex >= diceFaces.Count)
            {
                return;
            }

            diceFaces[slotIndex].ReplaceSkill(skill);
        }

        public void UpgradeFace(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= diceFaces.Count)
            {
                return;
            }

            diceFaces[slotIndex].ApplyUpgrade();
        }

        public string GetStatsText()
        {
            return $"{DisplayName} | HP {CurrentHp}/{MaxHp} | Shield {Shield} | Armor {Armor} | Rage {Rage}";
        }

        public string GetDiceText()
        {
            var builder = new StringBuilder();

            for (var index = 0; index < diceFaces.Count; index++)
            {
                builder.Append(index + 1);
                builder.Append(". ");
                builder.Append(diceFaces[index].GetSummary());

                if (index < diceFaces.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }
    }

    public readonly struct DamageResolution
    {
        public DamageResolution(int rawDamage, int damageAfterArmor, int shieldBlocked, int hpDamage)
        {
            RawDamage = rawDamage;
            DamageAfterArmor = damageAfterArmor;
            ShieldBlocked = shieldBlocked;
            HpDamage = hpDamage;
        }

        public int RawDamage { get; }
        public int DamageAfterArmor { get; }
        public int ShieldBlocked { get; }
        public int HpDamage { get; }
    }

    [Serializable]
    public class MapNodeRuntimeState
    {
        public int Index;
        public MapNodeDefinition Definition;
        public bool IsUnlocked;
        public bool IsCompleted;
    }

    [Serializable]
    public class RewardOptionRuntime
    {
        public RewardType RewardType;
        public SkillDefinition SkillDefinition;
        public string Title;
        public string Description;
    }

    public class BattleTurnReport
    {
        public int TurnNumber;
        public readonly List<string> LogLines = new List<string>();
        public BattleResultType BattleResult = BattleResultType.Ongoing;
        public DiceFaceState PlayerFace;
        public DiceFaceState EnemyFace;

        public string GetJoinedLog()
        {
            return string.Join("\n", LogLines);
        }
    }
}
