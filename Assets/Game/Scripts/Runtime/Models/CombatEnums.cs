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
        HighestHpEnemy = 2,
        AllEnemies = 3
    }

    public enum RewardType
    {
        LearnSkill = 0,
        UpgradeFace = 1
    }

    public enum MapNodeType
    {
        Battle = 0,
        Boss = 1
    }

    public enum BattleResultType
    {
        Ongoing = 0,
        Victory = 1,
        Defeat = 2
    }
}
