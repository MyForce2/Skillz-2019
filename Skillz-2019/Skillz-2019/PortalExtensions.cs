using System.Collections.Generic;
using System.Linq;
using ElfKingdom;

namespace MyBot
{
    public static class PortalExtensions
    {

        public static bool HasPortalAdvantage()
        {
            Portal closestEnemyPortal = GetBestPortal(true);
            if (closestEnemyPortal == null)
                return true;

            int enemyTurnsToReach =
                closestEnemyPortal.TurnsToReach(GameState.MyCastle.Location.Towards(closestEnemyPortal,
                                                                                    GameState.MyCastle.Size +
                                                                                    GameState
                                                                                        .Game.LavaGiantAttackRange));
            Portal closestPortal = GetBestPortal(false);
            int turnsToReach = closestPortal?.TurnsToReach(GameState.EnemyCastle.Location.Towards(closestPortal,
                                                                                                  GameState.EnemyCastle.Size +
                                                                                                  GameState.Game.LavaGiantAttackRange)) ?? int.MaxValue;
            return turnsToReach < enemyTurnsToReach && (GameState.Game.LavaGiantMaxHealth - turnsToReach * GameState.Game.LavaGiantSuffocationPerTurn) > GameState.Game.LavaGiantMaxHealth / 4;

        }

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

        public static Portal GetBestPortal(bool isEnemy)
        {
            return isEnemy ? GameState.EnemyPortals.OrderBy(portal => portal.TurnsToReach(GameState.MyCastle.Location.Towards(portal, GameState.MyCastle.Size + GameState.Game.LavaGiantAttackRange))).FirstOrDefault() :
                GameState.MyPortals.OrderBy(portal => portal.TurnsToReach(GameState.EnemyCastle.Location.Towards(portal, GameState.EnemyCastle.Size + GameState.Game.LavaGiantAttackRange))).FirstOrDefault();
        }

        /// <summary>
        ///     returns true if this location is a viable place for an attacking portal
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool CanAttackFrom(this MapObject obj)
        {
            return (obj.TurnsToReach(GameState.EnemyCastle.Location.Towards(obj, GameState.EnemyCastle.Size + GameState.Game.LavaGiantAttackRange),
                                    GameState.Game.LavaGiantMaxSpeed) + 1) * GameState.Game.LavaGiantSuffocationPerTurn < GameState.Game.LavaGiantMaxHealth * 3 / 4;
        }

        /// <summary>
        /// attempts to start summoning lava giant from portal p 
        /// return whether summoned lava giant or not
        /// </summary>
        /// <param name="p"> portal to summon from </param>
        /// <returns></returns>
        private static bool TrySummonLavaGiant(this Portal p)
        {
            if (GameState.Game.GetMyself().ManaPerTurn == 0 && GameState.Game.GetMyMana() >= CreatableObject.ManaFountain.GetCost())
                return false;
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
        private static bool TrySummonIceTroll(this Portal p, bool againstElves = true)
        {
            if (GameState.Game.GetMyself().ManaPerTurn == 0 && GameState.Game.GetMyMana() >= CreatableObject.ManaFountain.GetCost())
                return false;
            if (!(p.CanSummonIceTroll() && GameState.HasManaFor(CreatableObject.IceTroll) && (!againstElves || GameState.EnemyLivingElves.Count != 0)))
                return false;
            p.SummonIceTroll();
            return true;
        }

        public static bool TrySummonTornado(this Portal p)
        {
            if (!(p.CanSummonTornado() && GameState.HasManaFor(CreatableObject.Tornado)))
                return false;
            p.SummonTornado();
            return true;
        }

        //returns portals not currently summoning
        private static List<Portal> GetFreePortals() => Bot.Game.GetMyPortals().Where(x => (!x.IsSummoning)).ToList();

        public static bool IsGettingAttacked(this Portal portal)
        {
            double savingDuration = 1.0 * (GameState.Game.IceTrollCost - GameState.CurrentMana) /
                                    GameState.Game.GetMyself().ManaPerTurn;
            savingDuration = System.Math.Ceiling(savingDuration);
            int duration = System.Math.Max(0, (int)savingDuration);
            int possibleElfAttackers = (from elf in GameState.EnemyLivingElves
                                        where elf.InRange(portal.Location.Towards(elf, elf.AttackRange + portal.Size),
                                                          elf.MaxSpeed * (GameState.Game.IceTrollSummoningDuration + duration))
                                        select elf).Count();

            int possibleTornadoesAttackers = (from tornado in GameState.EnemyLivingTornadoes
                                              where portal.Equals(tornado.GetTornadoTarget(true)) &&
                                                    tornado.WillReachTarget()
                                              select tornado).Count();

            return possibleElfAttackers + possibleTornadoesAttackers > 0;
        }

        public static bool ShouldDefendCastle()
        {
            if (GameState.AttackingPortals > 0 || !ElfExtensions.CastleUnderAttack())
                return false;

            Portal closestEnemyPortal = GetBestPortal(true);
            if (closestEnemyPortal == null)
                return false;

            int enemyTurnsToReach =
                closestEnemyPortal.TurnsToReach(GameState.MyCastle.Location.Towards(closestEnemyPortal,
                                                                                    GameState.MyCastle.Size +
                                                                                    GameState
                                                                                        .Game.LavaGiantAttackRange));
            Portal closestPortal = GetBestPortal(false);
            int turnsToReach = closestPortal?.TurnsToReach(GameState.EnemyCastle.Location.Towards(closestEnemyPortal,
                                                                                                  GameState.EnemyCastle.Size +
                                                                                                  GameState
                                                                                                      .Game.LavaGiantAttackRange)) ?? int.MaxValue;
            return GameState.MyCastle.CurrentHealth < GameState.EnemyCastle.CurrentHealth;
        }

        public static bool ShouldUseTornado(this Portal p, bool aggressiveUse)
        {
            Building target = p.GetTornadoTarget();
            if (target == null || target?.HasTornadoIsEnRoute() == true || HasPortalAdvantage())
                return false;

            int tornadoDamage = p.TornadoDamage();

            return tornadoDamage == target.CurrentHealth;
        }

        public static void UseTornadoes(bool aggressiveUse)
        {
            var portals = Enumerable.Empty<Portal>();
            if (GameState.EnemyPortals.Count > 0)
            {
                portals = from portal in GetFreePortals()
                          orderby portal.Distance(GetBestPortal(true))
                          select portal;
            }

            foreach (Portal p in portals)
            {
                if (!p.ShouldUseTornado(aggressiveUse))
                {
                    continue;
                }

                if (!p.TrySummonTornado())
                {
                    GameState.SaveManaFor(CreatableObject.Tornado);
                }
            }
        }


        /// <summary>
        /// does portals turn logic
        /// PLACEHOLDER
        /// </summary>
        public static void DoPortalsTurn()
        {
            // summon at least one lava giant if no giants (summon from portal closest to enemy castle)
            if (GetFreePortals().Count == 0)
                return;

            UseTornadoes(true);

            IEnumerable<Portal> portals;

            if (ShouldDefendCastle())
            {
                var defendLocation =
                    GameState.MyCastle.Location.Towards(GetBestPortal(true),
                                                        GameState.MyCastle.Size + GameState.Game.LavaGiantAttackRange);
                portals = from portal in GetFreePortals()
                          where portal.InRange(defendLocation, ElfExtensions.ATTACKING_RADIUS)
                          orderby portal.Distance(defendLocation)
                          select portal;

                foreach (Portal portal in portals)
                {
                    if (!portal.TrySummonIceTroll(false))
                    {
                        GameState.SaveManaFor(CreatableObject.IceTroll);
                    }
                }

            }

            portals = from portal in GetFreePortals()
                      orderby portal.Distance(GameState.EnemyCastle)
                      select portal;

            foreach (Portal portal in portals)
            {
                if (portal.IsGettingAttacked())
                {
                    if (!portal.TrySummonIceTroll())
                    {
                        GameState.SaveManaFor(CreatableObject.IceTroll);
                    }
                    continue;
                }

                if (portal.InRange(GameState.MyCastle, ElfExtensions.DEFENSIVE_RADIUS))
                    continue;

                portal.TrySummonLavaGiant();
            }
        }
    }
}