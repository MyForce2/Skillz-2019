using ElfKingdom;


namespace MyBot
{



    public class Bot : ISkillzBot
    {

        /* constants (not in API for some reason) */
        public const int PORTAL_SIZE = 300;


        public static Game Game
        {
            get; private set;
        }


        public void DoTurn(Game game)
        {
            try
            {
                System.Console.WriteLine("dist: {0}", game.GetMyCastle().Distance(game.GetEnemyCastle()));
                Bot.Game = game;
                System.Console.WriteLine("elf count: {0}", game.GetMyLivingElves().Length);
                foreach (Elf e in game.GetMyLivingElves())
                    e.DoElfTurn();
                PortalExtensions.DoPortalsTurn();
            }
            catch (System.Exception) { }

        }




    }


}
