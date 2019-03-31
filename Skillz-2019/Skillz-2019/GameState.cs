using System.Collections.Generic;
using System.Linq;
using ElfKingdom;

namespace MyBot
{

    public static class GameState
    {

        #region Properties

        public static Game Game { get; private set; }

        public static int StartingMana { get; private set; }

        public static int CurrentMana => GameState.Game.GetMyMana() - GameState.ReservedMana;

        public static int ReservedMana { get; private set; }

        public static Castle MyCastle { get; private set; }
        public static Castle EnemyCastle { get; private set; }


        public static List<Elf> MyLivingElves { get; private set; }
        public static List<Elf> EnemyLivingElves { get; private set; }

        public static List<LavaGiant> MyLivingLavaGiants { get; private set; }
        public static List<LavaGiant> EnemyLivingLavaGiants { get; private set; }

        public static List<IceTroll> MyLivingIceTrolls { get; private set; }
        public static List<IceTroll> EnemyLivingIceTrolls { get; private set; }

        public static List<Tornado> MyLivingTornadoes { get; private set; }
        public static List<Tornado> EnemyLivingTornadoes { get; private set; }

        public static List<Portal> MyPortals { get; private set; }
        public static List<Portal> EnemyPortals { get; private set; }

        public static List<ManaFountain> MyManaFountains { get; private set; }
        public static List<ManaFountain> EnemyManaFountains { get; private set; }

        public static int CurrentlyBuiltFountains { get; set; }
        public static int TotalFountains => GameState.Game.GetMyManaFountains().Length + GameState.CurrentlyBuiltFountains;

        public static IEnumerable<GameObject> AllLivingEnemies =>
            GameState.EnemyLivingElves.Cast<GameObject>().Concat(GameState.Game.GetEnemyCreatures());

        public static int AttackingPortals => GameState.Game.GetMyPortals()
                                                       .Count(p => p.Distance(GameState.Game.GetEnemyCastle()) <=
                                                                   ElfExtensions.ATTACKING_RADIUS);

        public static int EnemyDefensivePortals =>
            GameState.EnemyPortals.Count(p => p.InRange(GameState.EnemyCastle, ElfExtensions.ATTACKING_RADIUS));

        #endregion


        public static void Update(Game game)
        {
            GameState.Game = game;
            GameState.ReservedMana = 0;
            GameState.CurrentlyBuiltFountains = 0;

            if (game.Turn == 1)
            {
                GameState.StartingMana = GameState.CurrentMana - game.DefaultManaPerTurn;
            }

            GameState.MyCastle = game.GetMyCastle();
            GameState.EnemyCastle = game.GetEnemyCastle();

            GameState.EnemyLivingElves = game.GetEnemyLivingElves().ToList();
            GameState.MyLivingElves = game.GetMyLivingElves().ToList();

            GameState.MyLivingLavaGiants = game.GetMyLavaGiants().ToList();
            GameState.EnemyLivingLavaGiants = game.GetEnemyLavaGiants().ToList();

            GameState.MyLivingIceTrolls = game.GetMyIceTrolls().ToList();
            GameState.EnemyLivingIceTrolls = game.GetEnemyIceTrolls().ToList();

            GameState.MyLivingTornadoes = game.GetMyTornadoes().ToList();
            GameState.EnemyLivingTornadoes = game.GetEnemyTornadoes().ToList();

            GameState.MyPortals = game.GetMyPortals().ToList();
            GameState.EnemyPortals = game.GetEnemyPortals().ToList();

            GameState.MyManaFountains = game.GetMyManaFountains().ToList();
            GameState.EnemyManaFountains = game.GetEnemyManaFountains().ToList();
        }

        /// <summary>
        ///     reserves the mana that is needed to build the given building
        /// </summary>
        /// <param name="obj"></param>
        public static void SaveManaFor(CreatableObject obj) => GameState.ReservedMana += obj.GetCost();

        /// <summary>
        ///     returns true if we have enough mana to create
        ///     the given obj. This method takes into consideration mana reserving.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool HasManaFor(CreatableObject obj) => obj.GetCost() <= GameState.CurrentMana;

    }

}