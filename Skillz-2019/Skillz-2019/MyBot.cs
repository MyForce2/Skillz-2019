using System.Linq;
using ElfKingdom;

namespace MyBot
{
    public class Bot : ISkillzBot
    {
        public static bool IsAngryBird { get; set; }

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
        ///     logic for Angry Bird Bot from week 1
        /// </summary>
        /// <param name="game"></param>
        public void AngryBirdLogic(Game game)
        {
            foreach (Elf e in game.GetMyLivingElves())
            {
                if (game.GetMyPortals().Length < 1)
                {
                    Location blockLocation = game.GetMyCastle().Location
                                                 .Towards(game.GetEnemyLivingElves()[0],
                                                          game.CastleSize + game.PortalSize + 50);
                    if (e.Location.Equals(blockLocation) && e.CanBuildPortal())
                        e.BuildPortal();
                    else e.MoveTo(blockLocation);
                    continue;
                }
                if (game.GetEnemyPortals().Length == 0 && game.GetMyPortals().Length < 2)
                {
                    Location blockLocation = game.GetMyPortals().FirstOrDefault().Location.Towards(game.GetMyCastle(), -game.PortalSize * 2 - 50);
                    if (e.Location.Equals(blockLocation) && e.CanBuildPortal())
                        e.BuildPortal();
                    else e.MoveTo(blockLocation);
                    continue;
                }

                GameObject attack = game.GetEnemyPortals().FirstOrDefault() ??
                                    (GameObject)game.GetEnemyLavaGiants().FirstOrDefault() ??
                                    (GameObject)game.GetEnemyLivingElves().FirstOrDefault();
                if (attack != null)
                {
                    if (e.InAttackRange(attack))
                        e.Attack(attack);
                    else e.MoveTo(attack);
                    continue;
                }

                e.MoveTo(game.GetMyCastle().Location.Towards(e, e.AttackRange));
            }

            foreach (Portal myPortal in game.GetMyPortals())
            {
                if (myPortal.CanSummonLavaGiant())
                    myPortal.SummonLavaGiant();
            }
        }

        /// <summary>
        ///     checks if we are facing destroy bot
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public bool IsDestroyBot(Game game) => Bot.UseSpecificStrategies && game.DefaultManaPerTurn == 0 && game.GetMyMana() == 0;

        public bool IsAngryBirdBot(Game game)
        {
            if (!Bot.UseSpecificStrategies)
                return false;
            Elf myElf = game.GetMyLivingElves().FirstOrDefault();
            Elf enemyElf = game.GetEnemyLivingElves().FirstOrDefault();
            if (myElf == null || enemyElf == null)
                return IsAngryBird;
            return (game.Turn == 1 && myElf.Distance(enemyElf) < 1500) || IsAngryBird;
        }


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
                // this will only happen when we face the AngryBird bot
                if (IsAngryBirdBot(game))
                {
                    System.Console.WriteLine("AngryBird");
                    IsAngryBird = true;
                    GameState.Update(game);
                    Bot.Game = game;
                    AngryBirdLogic(game);
                    return;
                }
                System.Console.WriteLine(IsAngryBirdBot(game));

                System.Console.WriteLine("dist: {0}", game.GetMyCastle().Distance(game.GetEnemyCastle()));
                Bot.Game = game;
                GameState.Update(game);
                System.Console.WriteLine("elf count: {0}", game.GetMyLivingElves().Length);
                foreach (Elf e in game.GetMyLivingElves())
                    e.DoElfTurn();
                PortalExtensions.DoPortalsTurn();
            }
            catch (System.Exception e) { System.Console.WriteLine(e); }

        }
    }
}
