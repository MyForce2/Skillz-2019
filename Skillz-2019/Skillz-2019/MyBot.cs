using System.Collections.Generic;
using System.Linq;
using ElfKingdom;

namespace MyBot
{
    public class Bot : ISkillzBot
    {
        public static Game Game
        {
            get; private set;
        }

        public static Dictionary<Elf, Dictionary<Elf, int>> LastTurnDistances =
            new Dictionary<Elf, Dictionary<Elf, int>>();

        private static void UpdateLastTurnDistances()
        {
            Bot.LastTurnDistances.Clear();
            foreach (Elf e in GameState.MyLivingElves.Concat(GameState.EnemyLivingElves))
            {
                Bot.LastTurnDistances[e] = new Dictionary<Elf, int>();
            }

            foreach (Elf myLivingElf in GameState.MyLivingElves)
            {
                foreach (Elf enemyLivingElf in GameState.EnemyLivingElves)
                {
                    if (enemyLivingElf.Invisible) continue;

                    int distance = myLivingElf.Distance(enemyLivingElf);
                    Bot.LastTurnDistances[myLivingElf][enemyLivingElf] = distance;
                    Bot.LastTurnDistances[enemyLivingElf][myLivingElf] = distance;
                }
            }
        }


        public void DoTurn(Game game)
        {
            try
            {
                Bot.Game = game;
                GameState.Update(game);
                System.Console.WriteLine(ElfExtensions.ATTACKING_RADIUS);
                MissionExtensions.UpdateOptimalLocations();
                System.Console.WriteLine("elf count: {0}", game.GetMyLivingElves().Length);

                //PortalExtensions.DefendFountains();

                var executedMissions = game.GetMyLivingElves().ExecuteMissions();
                foreach (Elf e in executedMissions.Keys)
                {
                    Mission m = executedMissions[e];
                    System.Console.WriteLine($"{e} : {m}");
                }

                PortalExtensions.DoPortalsTurn();
                UpdateLastTurnDistances();
            }
            catch (System.Exception e) { System.Console.WriteLine(e); }

        }
    }
}
