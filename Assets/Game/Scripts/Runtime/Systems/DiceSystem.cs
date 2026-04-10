using System;

namespace DiceRogue
{
    public class DiceSystem
    {
        private readonly Random random;

        public DiceSystem(int seed = 0)
        {
            random = seed == 0 ? new Random() : new Random(seed);
        }

        public DiceFaceState RollFace(CombatantRuntimeState combatant)
        {
            if (combatant == null || combatant.DiceFaces.Count == 0)
            {
                return null;
            }

            var index = NextIndex(combatant.DiceFaces.Count);
            var face = combatant.DiceFaces[index];
            combatant.SetLastRolledFace(face);
            return face;
        }

        public int NextIndex(int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return random.Next(0, count);
        }
    }
}
