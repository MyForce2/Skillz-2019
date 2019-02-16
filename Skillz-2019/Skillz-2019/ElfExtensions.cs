using System.Linq;
using System.Collections.Generic;
using ElfKingdom;

namespace MyBot
{
    public static class ElfExtensions
    {

        /* constants */
        //TODO: replace max portal count placeholder for now
        public const int MAX_PORTAL_COUNT = 2;

        public const int EXCESS_MANA = 200;

        public const int ATTACKING_THRESHOLD = 4;

        public const int MAX_FOUNTAINS = 2;

        public const int MAX_DEFENSIVE_PORTALS = 1;
        public const int MAX_NEUTRAL_PORTALS = 0;
        public const int MAX_ATTACKING_PORTALS = 2;

        public const int DEFENSIVE_RADIUS = 2000;
        public const int NEUTRAL_RADIUS = 4000;
        public const int ATTACKING_RADIUS = 1500;

        public static bool CastleUnderAttack()
        {
            var elfAttacking = GameState.EnemyLivingElves.Count(elf => elf.InAttackRange(GameState.MyCastle));
            var giantsAttacking =
                GameState.EnemyLivingLavaGiants.Count(g => g.InRange(GameState.MyCastle, g.AttackRange));

            return (elfAttacking + giantsAttacking) >= ElfExtensions.ATTACKING_THRESHOLD ||
                   elfAttacking == GameState.EnemyLivingElves.Count || PortalInMySide() != null;
        }

        /// <summary>
        ///     Returns true if it is safe to stay at the current location.
        ///     Exceptions are ignored
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        public static bool SafeNotToMove(this MapObject obj, params GameObject[] exceptions) =>
            LocationFinder.GetPossibleAttackers(obj, exceptions: exceptions).Count == 0;

        public static bool ShouldAttack(this Elf e, GameObject target)
        {
            var possibleAttackers = LocationFinder.GetPossibleAttackers(e, exceptions: target);
            if (possibleAttackers.Count > 0)
                return false;

            // the damage to the elf is the damage done by the target
            int damageToAttacker = target.GetAttackMultiplier();
            if (damageToAttacker == 0) // if the target can't fight back the elf will win
                return true;

            double turnsToKillAttacker = System.Math.Ceiling(1.0 * e.CurrentHealth / target.GetAttackMultiplier());

            // the damage to the target is the damage by the elf and the suffocation damage
            int damageToTarget = e.AttackMultiplier + target.GetSuffocationPerTurn();
            double turnsToKillTarget = System.Math.Ceiling(1.0 * target.CurrentHealth / damageToTarget);

            System.Console.WriteLine($"{e.AttackMultiplier}:{target.GetSuffocationPerTurn()}");

            // if the number of turns to kill the target are smaller then the number of turns to kill the elf
            // then elf will win the fight
            return turnsToKillTarget < turnsToKillAttacker || (target is Elf && turnsToKillTarget == turnsToKillAttacker);
        }

        /// <summary>
        /// returns true if elf should build portal
        /// </summary>
        /// <param name="e"> elf object </param>
        /// <param name="optimal"></param>
        /// <param name="defenceOptimal"></param>
        /// <param name="attackOptimal"></param>
        /// <returns></returns>
        public static bool ShouldBuildPortal(this Elf e, Location defenseOptimal, Location attackOptimal)
        {
            if (!e.SafeNotToMove())
                return false;

            int defendingDistance = e.Distance(Bot.Game.GetMyCastle());
            int attackingDistance = e.Distance(Bot.Game.GetEnemyCastle());

            if (defendingDistance <= ElfExtensions.DEFENSIVE_RADIUS)
            {
                return (!CastleUnderAttack() && GameState.DefensivePortals < ElfExtensions.MAX_DEFENSIVE_PORTALS ||
                        GameState.CurrentMana > ElfExtensions.EXCESS_MANA * 2) &&
                       e.Location.Distance(defenseOptimal) < e.MaxSpeed;
            }
            else if (attackingDistance <= ElfExtensions.ATTACKING_RADIUS)
            {
                return (GameState.AttackingPortals < ElfExtensions.MAX_ATTACKING_PORTALS ||
                        GameState.CurrentMana > ElfExtensions.EXCESS_MANA * 2) &&
                       e.Location.Distance(attackOptimal) < e.MaxSpeed;
            }

            return false;
        }

        /// <summary>
        ///     returns true if the elf should even attempt to build the fountain (attempting is to move to an optimal position)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="optimal"></param>
        public static bool ShouldAttemptToBuildFountain(this Elf e, Location optimal)
        {
            return GameState.AttackingPortals == 0 &&
                   (GameState.Game.GetMyManaFountains().Length < ElfExtensions.MAX_FOUNTAINS ||
                    GameState.CurrentMana > ElfExtensions.EXCESS_MANA) && !CastleUnderAttack() && optimal != null;
        }

        /// <summary>
        ///     returns weather the elf should build a fountain in his current position
        /// </summary>
        /// <param name="e"></param>
        /// <param name="optimal"></param>
        /// <returns></returns>
        public static bool ShouldBuildFountain(this Elf e, Location optimal)
        {
            return e.ShouldAttemptToBuildFountain(optimal) &&
                   e.Location.Distance(optimal) < e.MaxSpeed;

        }

        /// <summary>
        /// moves elf safely toward target while avoiding enemy ice trolls (if any)
        /// </summary>
        /// <param name="e"> elf object </param>
        /// <param name="target"> target MapObject to move to </param>
        public static void MoveSafely(this Elf e, MapObject target)
        {
            Location location;
            if (target is IceTroll || target is Elf && !CastleUnderAttack())
            {
                location = e.GetSafestPath(target, (GameObject)target);
            }
            else if (target is Elf || target is LavaGiant && CastleUnderAttack())
            {
                location = target.GetLocation();
            }
            else location = e.GetSafestPath(target);
            e.MoveTo(location);
        }

        /// <summary>
        /// returns best target to attack or null if shouldn't attack or no targets in range
        /// </summary>
        /// <param name="elf"> elf object </param>
        /// <returns></returns>
        public static GameObject GetAttackTarget(this Elf elf)
        {
            IEnumerable<GameObject> allTargets = ((GameObject[])GameState.Game.GetEnemyLivingElves())
                                                 .Concat(GameState.Game.GetEnemyCreatures()).Concat(GameState.Game.GetEnemyPortals())
                                                 .Concat(GameState.Game.GetEnemyManaFountains())
                                                 .Concat(new[] { GameState.Game.GetEnemyCastle() });
            return (from target in allTargets
                    where elf.InAttackRange(target)
                    orderby target is Elf ? 0 : 1,
                        target is Portal ? 0 : 1,
                        target is IceTroll ? 0 : 1,
                        target is Castle ? 0 : 1,
                        target.CurrentHealth,
                        elf.Distance(target)
                    select target).FirstOrDefault();
        }

        /// <summary>
        ///     returns the target the elf should go and attack. exceptions are ignored
        /// </summary>
        /// <param name="e"> elf object </param>
        /// <param name="isDefensive"></param>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        public static MapObject GetMoveTarget(this Elf e, bool isDefensive, params GameObject[] exceptions)
        {
            IEnumerable<GameObject> allTargets = ((GameObject[])GameState.Game.GetEnemyPortals())
                                                 .Concat(GameState.Game.GetEnemyManaFountains())
                                                 .Concat(GameState.EnemyLivingElves)
                                                 .Concat(GameState.EnemyLivingLavaGiants)
                                                 .Concat(new[] { GameState.Game.GetEnemyCastle() });
            if (isDefensive)
            {
                return (from target in allTargets
                        where !exceptions.Contains(target) &&
                              (!(target is Elf) || 
                               target.CurrentHealth <= e.CurrentHealth && e.Distance(target) <= e.MaxSpeed + e.AttackRange ||
                               (CastleUnderAttack() && ((Elf) target).InAttackRange(GameState.MyCastle))) &&
                              (!(target is LavaGiant) || CastleUnderAttack() &&
                               ((LavaGiant) target).InRange(GameState.MyCastle, ((LavaGiant) target).AttackRange))
                        orderby target is Castle ? int.MaxValue : e.Distance(target),
                            target is Portal ? 0 : 1,
                            LocationFinder.GetPossibleAttackers(target).Count,
                            target.Distance(GameState.MyCastle),
                            e.Distance(target),
                            target.CurrentHealth
                        select target).FirstOrDefault();
            }

            return (from target in allTargets
                    where !exceptions.Contains(target) && e.InRange(target, ElfExtensions.ATTACKING_RADIUS) &&
                          !(target is Elf || target is LavaGiant) &&
                          LocationFinder.GetPossibleAttackers(target, exceptions: target).Count == 0
                    orderby target is Castle ? int.MaxValue : e.Distance(target),
                        target is Portal ? target.Distance(GameState.MyCastle) : int.MaxValue,
                        target is ManaFountain ? 0 : 1,
                        target.CurrentHealth
                    select target).FirstOrDefault();
        }

        public static Portal PortalInMySide()
        {
            var enemyPortal = GameState.EnemyPortals
                                       .Where(p => p.InRange(GameState.MyCastle, ElfExtensions.DEFENSIVE_RADIUS))
                                       .OrderBy(p => p.Distance(GameState.MyCastle)).FirstOrDefault();
            return enemyPortal;
        }
    }
}
