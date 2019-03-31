using System.Collections.Generic;
using System.Linq;
using ElfKingdom;

namespace MyBot
{
    public static class GameObjectExtensions
    {

        public static int TurnsToReach(this GameObject obj, MapObject destination)
        {
            int speed = obj.GetMaxSpeed();
            if (obj is Portal)
                speed = GameState.Game.LavaGiantMaxSpeed;
            if (speed == 0)
                return 0;
            double turns = 1.0 * obj.Distance(destination) / speed;

            return (int) System.Math.Ceiling(turns);
        }

        public static bool WillReachTarget(this Tornado t)
        {
            bool isEnemy = GameState.EnemyLivingTornadoes.Contains(t);
            Building target = t.GetTornadoTarget(isEnemy);
            int range = target.Size + t.AttackRange;
            int health = t.CurrentHealth;
            Location location = t.Location;
            while (health > 0)
            {
                if (location.InRange(target, range))
                    break;
                location = target.Location.Towards(location, t.MaxSpeed);
                health -= t.SuffocationPerTurn;
            }
            return health > 0;
        }

        public static bool HasTornadoIsEnRoute(this Building b, bool isEnemy = true)
        {
            var tornadoes = isEnemy ? GameState.MyLivingTornadoes : GameState.EnemyLivingTornadoes;
            return tornadoes.Any(t => t.GetTornadoTarget(!isEnemy).Equals(b) && t.WillReachTarget());
        }

        public static int TornadoDamage(this MapObject tornadoLocation, bool isEnemy = false)
        {
            var target = tornadoLocation.GetTornadoTarget(isEnemy);
            Location attackLocation =
                target.Location.Towards(tornadoLocation, target.Size + GameState.Game.TornadoAttackRange);
            int health = target.CurrentHealth;
            int tornadoHealth = GameState.Game.TornadoMaxHealth;
            while (tornadoHealth > 0)
            {
                if (tornadoLocation.Equals(attackLocation))
                {
                    health -= GameState.Game.TornadoAttackMultiplier;
                }
                else
                {
                    tornadoLocation = tornadoLocation
                                      .GetLocation().Towards(attackLocation, GameState.Game.TornadoMaxSpeed);
                }

                tornadoHealth -= GameState.Game.TornadoSuffocationPerTurn;
            }

            return System.Math.Min(target.CurrentHealth, target.CurrentHealth - health);

        }

        public static Building GetTornadoTarget(this MapObject tornadoLocation, bool isEnemy = false)
        {
            var targets = isEnemy
                ? GameState.MyPortals.Cast<Building>().Concat(GameState.Game.GetMyManaFountains())
                : GameState.EnemyPortals.Cast<Building>().Concat(GameState.Game.GetEnemyManaFountains());
            return (from target in targets
                    orderby tornadoLocation.Distance(target)
                    select target).FirstOrDefault();
        }

        public static GameObject GetTarget(this IceTroll troll, bool isEnemy = true)
        {
            var enemies = isEnemy
                ? ((IEnumerable<GameObject>) GameState.MyLivingElves).Concat(GameState.Game.GetMyCreatures())
                : GameState.AllLivingEnemies;
            return (from enemy in enemies
                    orderby troll.Distance(enemy),
                        enemy is IceTroll ? 0 : 1,
                        enemy is Elf ? 0 : 1
                    select enemy).FirstOrDefault();
        }

        /// <summary>
        ///     returns the attack range of this game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetAttackRange(this GameObject obj)
        {
            if (obj is Creature creature)
                return creature.AttackRange;
            if (obj is Elf elf)
                return elf.AttackRange;
            return 0;
        }

        /// <summary>
        ///     returns the max speed of this game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetMaxSpeed(this GameObject obj)
        {
            if (obj is Creature creature)
                return creature.MaxSpeed;
            if (obj is Elf elf)
                return elf.MaxSpeed;
            return 0;
        }

        /// <summary>
        ///     returns the suffocation per turn of this game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetSuffocationPerTurn(this GameObject obj)
        {
            if (obj is IceTroll iceTroll)
                return iceTroll.SuffocationPerTurn;
            if (obj is LavaGiant lavaGiant)
                return lavaGiant.SuffocationPerTurn;
            if (obj is Tornado tornado)
                return tornado.SuffocationPerTurn;
            if (obj is Elf elf)
                return 0;
            return 0;
        }

        /// <summary>
        ///     returns the attack multiplier of this game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetAttackMultiplier(this GameObject obj)
        {
            if (obj is Creature creature)
                return creature.AttackMultiplier;
            if (obj is Elf elf)
                return elf.AttackMultiplier;
            return 0;
        }

    }
}
