namespace DiceRogue
{
    public enum SkillActionType
    {
        Attack = 0,
        Defense = 1,
        Buff = 2,
        Debuff = 3
    }

    public enum SkillTargetType
    {
        Self = 0,
        RandomEnemy = 1,
        HighHpEnemy = 2,
        AllEnemies = 3,
        HighestHpEnemy = HighHpEnemy
    }

    public enum RewardType
    {
        LearnSkill = 0,
        UpgradeFace = 1
    }

    public enum DiceBuildIdentity
    {
        Balanced = 0,
        Defensive = 1,
        Berserker = 2
    }

    public enum MapNodeType
    {
        Battle = 0,
        EliteBattle = 1,
        Reward = 2,
        Shop = 3,
        Boss = 4
    }

    public enum BattleResultType
    {
        Ongoing = 0,
        Victory = 1,
        Defeat = 2
    }
}
