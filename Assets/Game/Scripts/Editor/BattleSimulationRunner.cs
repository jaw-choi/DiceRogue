#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DiceRogue
{
    public static class BattleSimulationRunner
    {
        private const int SimulationSeed = 424242;

        [MenuItem("Tools/DiceRogue/Simulate Player vs Slime")]
        public static void SimulatePlayerVsSlime()
        {
            var config = RunContentFactory.CreateFallbackConfig();
            var slime = config.NormalEnemies.FirstOrDefault(enemy => enemy != null && enemy.Id == "slime");
            if (config.PlayerTemplate == null || slime == null)
            {
                Debug.LogError("DiceRogue simulation failed: missing player template or slime template.");
                return;
            }

            var player = new CombatantRuntimeState(config.PlayerTemplate);
            var battleSystem = new BattleSystem(new DiceSystem(SimulationSeed))
            {
                EnableDebugLogging = true
            };

            Debug.Log($"[Simulation] Player vs Slime started with seed {SimulationSeed}.");
            battleSystem.BeginBattle(player, slime, config.MaxBattleTurns);

            while (!battleSystem.IsFinished)
            {
                battleSystem.ResolveNextTurn();
            }

            Debug.Log(
                $"[Simulation Result] {battleSystem.BattleResult} | Player HP {player.CurrentHp}/{player.MaxHp} | " +
                $"Player Temp Stats Shield={player.Shield}, Armor={player.Armor}, Rage={player.Rage}");
        }
    }
}
#endif
