using System.Linq;
using ElfKingdom;

namespace MyBot
{
    public class Bot : ISkillzBot
    {
        public const bool UseSpecificStrategies = false;

        public static Game Game
        {
            get; private set;
        }

        /// <summary>
        ///     logic for the Destroy bot from week 1
        /// </summary>
        /// <param name="game"></param>
        public void DestroyLogic(Game game)
        {
            
            foreach (Elf elf in game.GetMyLivingElves())
            { 
                ManaFountain fountain = game.GetEnemyManaFountains().FirstOrDefault();
                if (fountain == null)
                    return;
                if (elf.InAttackRange(fountain))
                    elf.Attack(fountain);
                else elf.MoveTo(fountain);
            }
        }

        /// <summary>
        ///     checks if we are facing destroy bot
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public bool IsDestroyBot(Game game) => Bot.UseSpecificStrategies && game.DefaultManaPerTurn == 0 && game.GetMyMana() == 0;


        public void DoTurn(Game game)
        {
            try
            {
                // this will only happen when we face Destroy Bot
                if (IsDestroyBot(game))
                {
                    DestroyLogic(game);
                    return;
                }

                System.Console.WriteLine("dist: {0}", game.GetMyCastle().Distance(game.GetEnemyCastle()));
                Bot.Game = game;
                GameState.Update(game);
                MissionExtensions.UpdateOptimalLocations();
                System.Console.WriteLine("elf count: {0}", game.GetMyLivingElves().Length);
                foreach (Elf e in game.GetMyLivingElves())
                {
                    if(e.IsBuilding)
                        continue;
                    Mission m = e.GetBestMission();
                    m.ExecuteWith(e);
                    System.Console.WriteLine($"{e} : {m}");
                }

                PortalExtensions.DoPortalsTurn();
            }
            catch (System.Exception e) { System.Console.WriteLine(e); }

        }
    }
}
