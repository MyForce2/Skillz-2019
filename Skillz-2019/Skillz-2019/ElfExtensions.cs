using System.Linq;
using System.Collections.Generic;
using ElfKingdom;

namespace MyBot
{
    public static class ElfExtensions
    {

        /* constants */
        //TODO: replace max portal count placeholder for now
        public const double DISTANCE_RATIO = 0.261781; // on normal maps this gives radius of 1500 which is good

        public const int EXCESS_MANA = 200;

        public const int ATTACKING_THRESHOLD = 4;

        public static int MAX_FOUNTAINS => GameState.MyPortals.Count > 0 ? 3 : 2;

        public const int MAX_DEFENSIVE_PORTALS = 1;
        public const int MAX_NEUTRAL_PORTALS = 0;
        public const int MAX_ATTACKING_PORTALS = 2;

        public static int DEFENSIVE_RADIUS =>
            (int)System.Math.Ceiling(GameState.MyCastle.Distance(GameState.EnemyCastle) *
                                      ElfExtensions.DISTANCE_RATIO);
        public const int NEUTRAL_RADIUS = 4000;
        public static int ATTACKING_RADIUS => (int)System.Math.Ceiling(GameState.MyCastle.Distance(GameState.EnemyCastle) *
            ElfExtensions.DISTANCE_RATIO);

        public static int MaxPortals
        {
            get {
                bool isDefensive =
                    GameState.EnemyLivingElves.All(e => e.InRange(GameState.EnemyCastle,
                                                                  ElfExtensions.DEFENSIVE_RADIUS +
                                                                  GameState.Game.ElfMaxSpeed));
                bool portalAdvantage = GameState.EnemyDefensivePortals > GameState.MyPortals.Count;
                if (isDefensive || portalAdvantage)
                    return 3;
                return 1;
            }
        }

        public static bool CastleUnderAttack()
        {
            var elfAttacking = GameState.EnemyLivingElves.Count(elf => elf.InAttackRange(GameState.MyCastle));
            var giantsAttacking =
                GameState.EnemyLivingLavaGiants.Count(g => g.InRange(GameState.MyCastle, g.AttackRange + GameState.MyCastle.Size));

            return elfAttacking + giantsAttacking > 0;
        }

        /// <summary>
        ///     Returns true if it is safe to stay at the current location.
        ///     Exceptions are ignored
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="turns"></param>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        public static bool SafeNotToMove(this GameObject obj, int turns = 1, params GameObject[] exceptions)
        {
            if (turns == 1)
            {
                return LocationFinder.GetPossibleAttackers(obj, exceptions: exceptions).Count == 0;
            }

            int health = obj.CurrentHealth;
            int size = 0;
            if (obj is Building b)
                size = b.Size;
            var enemies = GameState.EnemyLivingElves.Cast<GameObject>()
                                   .Concat(GameState.EnemyLivingIceTrolls.Where(troll => troll.GetTarget().Equals(obj))).ToList();
            var locations = GameState.EnemyLivingElves.Cast<GameObject>()
                                                       .Concat(GameState.EnemyLivingIceTrolls)
                                                       .ToDictionary(g => g, g => g.Location);
            // we simulate the turns
            for (int turn = 1; turn <= turns; turn++)
            {
                var enemiesToRemove = new List<GameObject>();
                foreach (var enemy in enemies)
                {
                    if (locations[enemy].InRange(obj, enemy.GetAttackRange() + size))
                        health -= enemy.GetAttackMultiplier();
                    else locations[enemy] = locations[enemy].Towards(obj, enemy.GetMaxSpeed());

                    if (enemy.CurrentHealth - enemy.GetSuffocationPerTurn() * turn <= 0)
                        enemiesToRemove.Add(enemy);
                }
                enemies.RemoveAll(enemiesToRemove.Contains);
            }

            return (health > 0 && obj.CurrentHealth - health <= obj.MaxHealth / 2);
        }

        public static bool ShouldAttack(this Elf e, GameObject target)
        {
            var possibleAttackers = LocationFinder.GetPossibleAttackers(e);

            // the damage to the target is the damage by the elf and the suffocation damage
            int damageToTarget = e.AttackMultiplier + target.GetSuffocationPerTurn();
            int turnsToKillTarget = (int)System.Math.Ceiling(1.0 * target.CurrentHealth / damageToTarget);

            // the damage done to the elf can be different every turn
            int health = e.CurrentHealth;
            for (int i = 0; i < turnsToKillTarget; i++)
            {
                foreach (var attacker in possibleAttackers)
                {
                    if (attacker.CurrentHealth <= attacker.GetSuffocationPerTurn() * i)
                        continue;
                    health -= attacker.GetAttackMultiplier();
                }
            }

            System.Console.WriteLine($"{health}:{turnsToKillTarget}:{target.Type}:{possibleAttackers.Count}");

            // if the number of turns to kill the target are smaller then the number of turns to kill the elf
            // then elf will win the fight
            return health == e.CurrentHealth || health >= GameState.Game.ElfMaxHealth / 2 || (target is Elf && health >= 0);
        }

        /// <summary>
        /// returns true if elf should build portal
        /// </summary>
        /// <param name="e"> elf object </param>
        /// <param name="attackOptimal"></param>
        /// <returns></returns>
        public static bool ShouldBuildPortal(this Elf e, Location attackOptimal)
        {
            if (!e.SafeNotToMove())
                return false;

            int defendingDistance = e.Distance(GameState.MyCastle);

            return GameState.AttackingPortals == 0 && defendingDistance > ElfExtensions.DEFENSIVE_RADIUS && e.Location.CanAttackFrom();
        }

        public static bool ShouldSaveForPortal(this Elf e)
        {
            double savingDuration = 1.0 * (GameState.Game.PortalCost - GameState.CurrentMana) / GameState.Game.GetMyself().ManaPerTurn;
            savingDuration = System.Math.Ceiling(savingDuration);
            return (!PortalExtensions.HasPortalAdvantage() || e.InRange(GameState.EnemyCastle, PortalExtensions.GetBestPortal(false)?.Distance(GameState.EnemyCastle) ?? int.MaxValue)) &&
                   GameState.ReservedMana == 0 & GameState.AttackingPortals == 0 && (GameState.MyPortals.Count < ElfExtensions.MaxPortals || (e.InRange(GameState.EnemyCastle, ElfExtensions.ATTACKING_RADIUS) && e.SafeNotToMove((int)savingDuration + GameState.Game.PortalBuildingDuration))) &&
                   GameState.Game.GetMyself().ManaPerTurn * MissionExtensions.MAXIMUM_SAVING_MANA_TURNS >=
                   CreatableObject.Portal.GetCost() - GameState.CurrentMana;
        }

        public static bool ShouldSaveForFountain(this Elf e)
        {
            return !PortalExtensions.HasPortalAdvantage() && GameState.Game.GetMyManaFountains().Length == 0 && !CastleUnderAttack() && e.SafeNotToMove() &&
                   GameState.CurrentlyBuiltFountains == 0
                   && e.Distance(GameState.EnemyCastle) > ElfExtensions.ATTACKING_RADIUS && e.Distance(GameState.MyCastle) > ElfExtensions.DEFENSIVE_RADIUS &&
                   GameState.Game.GetMyself().ManaPerTurn < 5;
        }

        /// <summary>
        ///     returns weather the elf should build a fountain in his current position
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool ShouldBuildFountain(this Elf e)
        {
            int portalCount = GameState.MyPortals.Count;
            return GameState.Game.GetMyManaFountains().Length < ElfExtensions.MAX_FOUNTAINS && !CastleUnderAttack() && e.SafeNotToMove() && GameState.CurrentlyBuiltFountains == 0
                                                   && e.Distance(GameState.EnemyCastle) > ElfExtensions.ATTACKING_RADIUS;

        }

        /// <summary>
        /// moves elf safely toward target while avoiding enemy ice trolls (if any)
        /// </summary>
        /// <param name="e"> elf object </param>
        /// <param name="target"> target MapObject to move to </param>
        public static void MoveSafely(this Elf e, MapObject target)
        {
            Location location;
            if (target is IceTroll || target is Elf)
            {
                location = e.GetSafestPath(target, (GameObject)target);
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
                                                 .Concat(GameState.Game.GetEnemyPortals())
                                                 .Concat(GameState.Game.GetEnemyManaFountains())
                                                 .Concat(GameState.EnemyLivingTornadoes);

            return (from target in allTargets
                    where elf.InAttackRange(target)
                    orderby target is Elf ? 0 : 1,
                        target is Portal ? 0 : 1,
                        target is Tornado ? 0 : 1,
                        target.CurrentHealth
                    select target).FirstOrDefault();
        }

        /// <summary>
        ///     returns the target the elf should go and attack. exceptions are ignored
        /// </summary>
        /// <param name="e"> elf object </param>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        public static GameObject GetMoveTarget(this Elf e, params GameObject[] exceptions)
        {
            IEnumerable<GameObject> allTargets = ((GameObject[])GameState.Game.GetEnemyPortals())
                                                 .Concat(GameState.Game.GetEnemyManaFountains())
                                                 .Concat(GameState.EnemyLivingElves);

            if (e.ShouldTargetFountain(out ManaFountain fountain))
            {
                return fountain;
            }

            return (from target in allTargets
                    let attackLocation = e.GetAttackLocation(target)
                    where !exceptions.Contains(target) &&
                          (!(target is Elf) || target.CurrentHealth <= e.CurrentHealth)
                    orderby LocationFinder.GetPossibleAttackers(e, attackLocation, exceptions: target).Count,
                        e.Distance(attackLocation),
                        target is Portal ? target.Distance(GameState.MyCastle) : int.MaxValue,
                        target is ManaFountain ? 0 : 1,
                        target.CurrentHealth
                    select target).FirstOrDefault();
        }

        public static Location GetAttackLocation(this Elf e, GameObject target)
        {
            int size = 0;
            if (target is Building b)
                size = b.Size;
            return target.Location.Towards(e, size + e.AttackRange);
        }

        public static Portal PortalInMySide()
        {
            var enemyPortal = GameState.EnemyPortals
                                       .Where(p => p.InRange(GameState.MyCastle, ElfExtensions.DEFENSIVE_RADIUS))
                                       .OrderBy(p => p.Distance(GameState.MyCastle)).FirstOrDefault();
            return enemyPortal;
        }

        public static bool IsInChaseWith(this Elf e, Elf enemy)
        {
            int delta = e.MaxSpeed / 2;
            int distance = e.Distance(enemy);

            if (!Bot.LastTurnDistances.TryGetValue(e, out Dictionary<Elf, int> dict))
            {
                return false;
            }

            if (!dict.TryGetValue(enemy, out int lastTurnDistance))
            {
                return false;
            }
            System.Console.WriteLine(distance + " : " + lastTurnDistance);



            return System.Math.Abs(distance - lastTurnDistance) < e.MaxSpeed / 2;
        }

        public static bool IsSpedUp(this Elf e)
        {
            return e.CurrentSpells.Any(spell => spell.GetType() == typeof(SpeedUp));
        }

        public static bool ShouldTargetFountain(this Elf e, out ManaFountain fountain)
        {
            var closestFountain = GameState.Game.GetEnemyManaFountains().OrderBy(e.Distance).FirstOrDefault();
            if (closestFountain == null || true)
            {
                fountain = null;
                return false;
            }

            int range = GameState.Game.SpeedUpMultiplier * GameState.Game.ElfMaxSpeed * GameState.Game.SpeedUpExpirationTurns;
            if (!e.InRange(e.GetAttackLocation(closestFountain), range))
            {
                fountain = null;
                return false;
            }

            fountain = closestFountain;
            return true;
        }
    }
}