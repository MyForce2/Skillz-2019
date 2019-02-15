using System.Collections.Generic;
using System.Linq;
using ElfKingdom;

namespace MyBot
{
    public static class PortalExtensions
    {

        /// <summary>
        ///     returns the number of turns it takes to travel the given distance
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="destination"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        private static int TurnsToReach(this MapObject obj, MapObject destination, int speed)
        {
            int distance = obj.Distance(destination);
            if (distance % speed == 0)
                return distance / speed;

            // we account for the remainder with the + 1
            return distance / speed + 1;
        }

        /// <summary>
        ///     returns true if this location is a viable place for an attacking portal
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool CanAttackFrom(this MapObject obj)
        {
            return obj.TurnsToReach(GameState.EnemyCastle, 
                                    GameState.Game.LavaGiantMaxSpeed) * GameState.Game.LavaGiantSuffocationPerTurn < GameState.Game.LavaGiantMaxHealth;
        }

        /// <summary>
        /// attempts to start summoning lava giant from portal p 
        /// return whether summoned lava giant or not
        /// </summary>
        /// <param name="p"> portal to summon from </param>
        /// <returns></returns>
        private static bool TrySummonLavaGiant(this Portal p)
        {
            if (!p.CanAttackFrom())
                return false;
            if (!(p.CanSummonLavaGiant() && GameState.HasManaFor(CreatableObject.LavaGiant)))
                return false;
            p.SummonLavaGiant();
            return true;
        }


        /// <summary>
        /// attempts to start summoning Ice Troll from portal p 
        /// return whether summoned Ice Troll or not
        /// </summary>
        /// <param name="p"> portal to summon from </param>
        /// <returns></returns>
        private static bool TrySummonIceTroll(this Portal p)
        {
            if (!(p.CanSummonIceTroll() && GameState.HasManaFor(CreatableObject.IceTroll)))
                return false;
            p.SummonIceTroll();
            return true;
        }

        //returns amount of (living and in summon) friendly lava giants
        private static int GetMyTotalGiantsCount() => Bot.Game.GetMyLavaGiants().Length + Bot.Game.GetMyPortals().Count(x => (x.IsSummoning && x.CurrentlySummoning.Equals("LavaGiant")));
        //returns amount of (living and in summon) friendly lava giants                                                                                     
        private static int GetMyTotalTrollsCount() => Bot.Game.GetMyIceTrolls().Length + Bot.Game.GetMyPortals().Count(x => (x.IsSummoning && x.CurrentlySummoning.Equals("IceTroll")));

        //returns amount of (living and in summon) enemy lava giants
        private static int GetEnemyTotalGiantsCount() => Bot.Game.GetEnemyLavaGiants().Length + Bot.Game.GetEnemyPortals().Count(x => (x.IsSummoning && x.CurrentlySummoning.Equals("LavaGiant")));
        //returns amount of (living and in summon) enemy lava giants                                                                                     
        private static int GetEnemyTotalTrollsCount() => Bot.Game.GetEnemyIceTrolls().Length + Bot.Game.GetEnemyPortals().Count(x => (x.IsSummoning && x.CurrentlySummoning.Equals("IceTroll")));

        //returns portals not currently summoning
        private static List<Portal> GetFreePortals() => Bot.Game.GetMyPortals().Where(x => (!x.IsSummoning)).ToList();

        /// <summary>
        /// does portals turn logic
        /// PLACEHOLDER
        /// </summary>
        public static void DoPortalsTurn()
        {
            // summon at least one lava giant if no giants (summon from portal closest to enemy castle)
            if (GetFreePortals().Count == 0)
                return;
            if (GetMyTotalGiantsCount() == 0)
                GetFreePortals().OrderBy(x => x.Distance(Bot.Game.GetEnemyCastle())).First().TrySummonLavaGiant();
            // summon ice trolls according to enemy portal count and ice trolls count
            if (GetFreePortals().Count == 0)
                return;
            var trollReq = GetEnemyTotalTrollsCount() + Bot.Game.GetEnemyPortals().Length;
            var targets = Bot.Game.GetEnemyLivingElves().Cast<GameObject>();
            targets = targets.Concat(Bot.Game.GetEnemyIceTrolls());
            targets = targets.Concat(Bot.Game.GetEnemyLavaGiants());
            targets = targets.Concat(Bot.Game.GetEnemyPortals());
            //order troll portals by distance to nearest target
            List<Portal> trollPortals;
            if (targets.Any())
                trollPortals = GetFreePortals().OrderBy(x => x.Distance(targets.OrderBy(v => v.Distance(x)).First())).ToList();
            else
                trollPortals = GetFreePortals();
            foreach (Portal p in trollPortals)
                if (GetMyTotalTrollsCount() < trollReq)
                    p.TrySummonIceTroll();
            if (GetMyTotalTrollsCount() >= trollReq || Bot.Game.GetMyMana() > 150)
                foreach (Portal p in GetFreePortals().OrderBy(x => x.Distance(Bot.Game.GetEnemyCastle())))
                {
                    if (p.Distance(Bot.Game.GetEnemyCastle()) < 3000)
                        p.TrySummonLavaGiant();
                }
        }

    }
}
