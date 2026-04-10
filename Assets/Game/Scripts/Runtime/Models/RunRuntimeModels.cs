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

        public int AttackValue => skill == null ? 0 : skill.GetAttackAmount(upgradeLevel);
        public int ShieldValue => skill == null ? 0 : skill.GetShieldAmount(upgradeLevel);
        public int ArmorValue => skill == null ? 0 : skill.GetArmorAmount(upgradeLevel);
        public int NextTurnShieldValue => skill == null ? 0 : skill.GetNextTurnShieldAmount(upgradeLevel);
        public int SelfDamageValue => skill == null ? 0 : skill.GetSelfDamageAmount(upgradeLevel);
        public int RageGainValue => skill == null ? 0 : skill.GetRageGainAmount(upgradeLevel);
        public int RageCostValue => skill == null ? 0 : skill.GetRageCostAmount(upgradeLevel);
        public int LifestealPercent => skill == null ? 0 : skill.GetLifestealPercent(upgradeLevel);
        public int ShieldDamagePercent => skill == null ? 0 : skill.GetShieldDamagePercent(upgradeLevel);
        public int AttackModifierValue => skill == null ? 0 : skill.GetAttackModifierAmount(upgradeLevel);
        public int DicePointModifierValue => skill == null ? 0 : skill.GetDicePointModifierAmount(upgradeLevel);
        public int RepeatCount => skill == null ? 1 : skill.GetRepeatCount(upgradeLevel);
        public int BonusDicePointsOnFirstRoll => skill == null ? 0 : skill.GetBonusDicePointsOnFirstRoll(upgradeLevel);

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
            return skill == null ? "비어 있음" : $"{skill.DisplayName} Lv.{upgradeLevel} / {skill.GetSummary(upgradeLevel)}";
        }

        public string GetInspectText(int faceIndex)
        {
            if (skill == null)
            {
                return $"{faceIndex + 1}. Empty Face | No skill assigned | Empty";
            }

            var upgradeState = upgradeLevel > 0 ? $"Upgraded Lv.{upgradeLevel}" : "Base";
            return $"{faceIndex + 1}. {skill.DisplayName} | {skill.GetSummary(upgradeLevel)} | {upgradeState}";
        }
    }

    [Serializable]
    public class DiceLoadoutState
    {
        private readonly List<DiceFaceState> faces = new List<DiceFaceState>(DiceLoadoutDefinition.FaceCount);

        public DiceLoadoutState(IEnumerable<SkillDefinition> skills)
        {
            SetFaces(skills);
        }

        public IReadOnlyList<DiceFaceState> Faces => faces;
        public int FaceCount => faces.Count;
        public bool IsValid => faces.Count == DiceLoadoutDefinition.FaceCount;

        public void SetFaces(IEnumerable<SkillDefinition> skills)
        {
            faces.Clear();

            foreach (var skill in DiceLoadoutDefinition.Normalize(skills))
            {
                faces.Add(new DiceFaceState(skill));
            }
        }

        public bool ReplaceFace(int slotIndex, SkillDefinition skill)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return false;
            }

            faces[slotIndex].ReplaceSkill(skill);
            return true;
        }

        public bool UpgradeFace(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex) || faces[slotIndex].Skill == null)
            {
                return false;
            }

            faces[slotIndex].ApplyUpgrade();
            return true;
        }

        public bool ApplyPrototypeIdentity(DiceBuildIdentity identity, IReadOnlyList<SkillDefinition> skillLibrary)
        {
            var basicAttack = FindSkill(skillLibrary, "basic_attack");
            var defensiveStance = FindSkill(skillLibrary, "defensive_stance");
            var focusedDefense = FindSkill(skillLibrary, "focused_defense");
            var counter = FindSkill(skillLibrary, "counter");
            var shieldBurst = FindSkill(skillLibrary, "shield_burst");
            var bloodSlash = FindSkill(skillLibrary, "blood_slash");
            var fury = FindSkill(skillLibrary, "fury");
            var savageStrike = FindSkill(skillLibrary, "savage_strike");
            var vampiricSlash = FindSkill(skillLibrary, "vampiric_slash");

            SkillDefinition[] preset;
            switch (identity)
            {
                case DiceBuildIdentity.Defensive:
                    preset = new[]
                    {
                        defensiveStance,
                        focusedDefense,
                        defensiveStance,
                        counter,
                        shieldBurst,
                        basicAttack
                    };
                    break;
                case DiceBuildIdentity.Berserker:
                    preset = new[]
                    {
                        bloodSlash,
                        fury,
                        savageStrike,
                        vampiricSlash,
                        bloodSlash,
                        basicAttack
                    };
                    break;
                case DiceBuildIdentity.Balanced:
                default:
                    preset = new[]
                    {
                        basicAttack,
                        defensiveStance,
                        basicAttack,
                        focusedDefense,
                        counter,
                        fury
                    };
                    break;
            }

            if (preset.Any(skill => skill == null))
            {
                return false;
            }

            for (var index = 0; index < preset.Length; index++)
            {
                faces[index].ReplaceSkill(preset[index]);
            }

            return true;
        }

        public string GetInspectionText()
        {
            var builder = new StringBuilder();

            for (var index = 0; index < faces.Count; index++)
            {
                builder.Append(faces[index].GetInspectText(index));

                if (index < faces.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < faces.Count;
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
    }

    [Serializable]
    public class CombatantRuntimeState
    {
        private const int MaxRageValue = 15;

        public CombatantRuntimeState(CombatantTemplate template)
        {
            Template = template;
            DisplayName = template.DisplayName;
            MaxHp = template.MaxHp;
            CurrentHp = template.MaxHp;
            DiceLoadout = new DiceLoadoutState(template.DiceSkills);
        }

        public CombatantTemplate Template { get; }
        public string DisplayName { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int Shield { get; private set; }
        public int Armor { get; private set; }
        public int Rage { get; private set; }
        public int CurrentAttackModifier { get; private set; }
        public int PendingNextTurnShield { get; private set; }
        public int PendingNextTurnAttackModifier { get; private set; }
        public int PendingNextTurnDicePointModifier { get; private set; }
        public int BerserkTurnsRemaining { get; private set; }
        public int PersistentAttackBonus { get; private set; }
        public bool IsSummoned { get; private set; }
        public string SummonerId { get; private set; }
        public DiceFaceState LastRolledFace { get; private set; }
        public DiceLoadoutState DiceLoadout { get; }
        public IReadOnlyList<DiceFaceState> DiceFaces => DiceLoadout.Faces;
        public bool IsAlive => CurrentHp > 0;
        public bool IsBerserkActive => BerserkTurnsRemaining > 0;
        public int BaseDicePoints => Template.BaseDicePoints;
        public int PassiveReflectPercent => Template.PassiveReflectPercent;
        public int BerserkAttackBonus => IsBerserkActive ? 5 : 0;
        public int BerserkLifestealPercent => IsBerserkActive ? 30 : 0;

        public void ResetForBattle(bool restoreHp)
        {
            if (restoreHp)
            {
                CurrentHp = MaxHp;
            }

            Shield = 0;
            Armor = 0;
            Rage = 0;
            CurrentAttackModifier = 0;
            PendingNextTurnShield = 0;
            PendingNextTurnAttackModifier = 0;
            PendingNextTurnDicePointModifier = 0;
            BerserkTurnsRemaining = 0;
            PersistentAttackBonus = 0;
            IsSummoned = false;
            SummonerId = null;
            LastRolledFace = null;
        }

        public void ResetTemporaryCombatState()
        {
            Shield = 0;
            Armor = 0;
            Rage = 0;
            CurrentAttackModifier = 0;
            PendingNextTurnShield = 0;
            PendingNextTurnAttackModifier = 0;
            PendingNextTurnDicePointModifier = 0;
            BerserkTurnsRemaining = 0;
            PersistentAttackBonus = 0;
            IsSummoned = false;
            SummonerId = null;
            LastRolledFace = null;
        }

        public void BeginTurn()
        {
            Shield = 0;
            Armor = 0;
            CurrentAttackModifier = PendingNextTurnAttackModifier;
            PendingNextTurnAttackModifier = 0;

            if (PendingNextTurnShield > 0)
            {
                GainShield(PendingNextTurnShield);
            }

            PendingNextTurnShield = 0;

            if (Template.PassiveShieldPerTurn > 0)
            {
                GainShield(Template.PassiveShieldPerTurn);
            }
        }

        public void EndTurn()
        {
            if (BerserkTurnsRemaining <= 0)
            {
                return;
            }

            BerserkTurnsRemaining -= 1;
            if (BerserkTurnsRemaining <= 0)
            {
                Rage = 0;
            }
        }

        public void SetLastRolledFace(DiceFaceState face)
        {
            LastRolledFace = face;
        }

        public int ConsumeTurnDicePointModifier()
        {
            var modifier = PendingNextTurnDicePointModifier;
            PendingNextTurnDicePointModifier = 0;
            return modifier;
        }

        public int GetTurnDicePoints(int bonusDicePoints = 0)
        {
            return Mathf.Max(0, BaseDicePoints + bonusDicePoints + (IsBerserkActive ? 1 : 0));
        }

        public void GainShield(int amount)
        {
            Shield += Mathf.Max(0, amount);
        }

        public void ConsumeAllShield()
        {
            Shield = 0;
        }

        public int GetShieldValue()
        {
            return Shield;
        }

        public void GainArmor(int amount)
        {
            Armor += Mathf.Max(0, amount);
        }

        public void QueueNextTurnShield(int amount)
        {
            PendingNextTurnShield += Mathf.Max(0, amount);
        }

        public void ApplyNextTurnAttackModifier(int amount)
        {
            PendingNextTurnAttackModifier += amount;
        }

        public void ApplyNextTurnDicePointModifier(int amount)
        {
            PendingNextTurnDicePointModifier += amount;
        }

        public void AddPersistentAttackBonus(int amount)
        {
            PersistentAttackBonus += amount;
        }

        public void MarkAsSummoned(string summonerId)
        {
            IsSummoned = true;
            SummonerId = summonerId;
        }

        public bool GainRage(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            Rage = Mathf.Clamp(Rage + amount, 0, MaxRageValue);
            if (Rage >= MaxRageValue && !IsBerserkActive)
            {
                BerserkTurnsRemaining = 2;
                return true;
            }

            return false;
        }

        public void SpendRage(int amount)
        {
            Rage = Mathf.Clamp(Rage - Mathf.Max(0, amount), 0, MaxRageValue);
        }

        public int GetAttackBonus()
        {
            return Rage + CurrentAttackModifier + PersistentAttackBonus + BerserkAttackBonus;
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

            var hpDamage = Mathf.Max(0, damageAfterArmor - shieldBlocked);
            if (hpDamage > 0)
            {
                CurrentHp = Mathf.Max(0, CurrentHp - hpDamage);
            }

            return new DamageResolution(rawDamage, damageAfterArmor, shieldBlocked, hpDamage);
        }

        public void ReplaceFace(int slotIndex, SkillDefinition skill)
        {
            DiceLoadout.ReplaceFace(slotIndex, skill);
        }

        public void UpgradeFace(int slotIndex)
        {
            DiceLoadout.UpgradeFace(slotIndex);
        }

        public bool ApplyDiceBuildIdentity(DiceBuildIdentity identity, IReadOnlyList<SkillDefinition> skillLibrary)
        {
            return DiceLoadout.ApplyPrototypeIdentity(identity, skillLibrary);
        }

        public string GetStatsText()
        {
            var berserkText = IsBerserkActive ? $" | 광분 {BerserkTurnsRemaining}턴" : string.Empty;
            var summonText = IsSummoned ? " | Summoned" : string.Empty;
            var auraText = PersistentAttackBonus != 0 ? $" | 공격 보너스 {PersistentAttackBonus:+#;-#;0}" : string.Empty;
            return $"{DisplayName} | HP {CurrentHp}/{MaxHp} | 방어도 {Shield} | 방어력 {Armor} | 분노 {Rage}{berserkText}{auraText}{summonText}";
        }

        public string GetDiceText()
        {
            return DiceLoadout.GetInspectionText();
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
        public int PlayerTurnDicePoints;
        public string CurrentActingUnitName;
        public readonly List<string> LogLines = new List<string>();
        public readonly List<string> TurnOrderEntries = new List<string>();
        public readonly List<BattleActionResult> ActionResults = new List<BattleActionResult>();
        public readonly List<DiceFaceState> PlayerFaces = new List<DiceFaceState>();
        public readonly List<DiceFaceState> EnemyFaces = new List<DiceFaceState>();
        public BattleResultType BattleResult = BattleResultType.Ongoing;
        public DiceFaceState PlayerFace;
        public DiceFaceState EnemyFace;

        public string GetJoinedLog()
        {
            return string.Join("\n", LogLines);
        }
    }
}
