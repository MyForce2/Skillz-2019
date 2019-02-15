using System.Collections.Generic;
using System.Linq;
using ElfKingdom;

namespace MyBot
{

    public static class GameState
    {

        #region Properties

        public static Game Game { get; private set; }

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

        public static List<Portal> MyPortals { get; private set; }
        public static List<Portal> EnemyPortals { get; private set; }

        public static IEnumerable<GameObject> AllLivingEnemies =>
            GameState.EnemyLivingElves.Cast<GameObject>().Concat(GameState.Game.GetEnemyCreatures());

        public static int DefensivePortals => GameState.Game.GetMyPortals()
                                                       .Count(p => p.Distance(GameState.Game.GetMyCastle()) <=
                                                                   ElfExtensions.DEFENSIVE_RADIUS);

        public static int NeutralPortals => GameState.Game.GetMyPortals()
                                                     .Count(p => p.Distance(GameState.Game.GetMyCastle()) >
                                                                 ElfExtensions.DEFENSIVE_RADIUS
                                                                 && p.Distance(GameState.Game.GetMyCastle()) <=
                                                                 ElfExtensions.NEUTRAL_RADIUS);

        public static int AttackingPortals => GameState.Game.GetMyPortals()
                                                       .Count(p => p.Distance(GameState.Game.GetEnemyCastle()) <=
                                                                   ElfExtensions.ATTACKING_RADIUS);

        #endregion


        public static void Update(Game game)
        {
            GameState.Game = game;
            GameState.ReservedMana = 0;

            GameState.MyCastle = game.GetMyCastle();
            GameState.EnemyCastle = game.GetEnemyCastle();

            GameState.EnemyLivingElves = game.GetEnemyLivingElves().ToList();
            GameState.MyLivingElves = game.GetMyLivingElves().ToList();

            GameState.MyLivingLavaGiants = game.GetMyLavaGiants().ToList();
            GameState.EnemyLivingLavaGiants = game.GetEnemyLavaGiants().ToList();

            GameState.MyLivingIceTrolls = game.GetMyIceTrolls().ToList();
            GameState.EnemyLivingIceTrolls = game.GetEnemyIceTrolls().ToList();

            GameState.MyPortals = game.GetMyPortals().ToList();
            GameState.EnemyPortals = game.GetEnemyPortals().ToList();
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