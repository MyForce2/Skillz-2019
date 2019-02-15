using System.Collections.Generic;
using System.Linq;
using ElfKingdom;

namespace MyBot
{
    public static class LocationFinder
    {

        /// <summary>
        ///     returns the optimal location for elf e to build a portal.
        ///     defensive portal is a portal near our castle, attacking portal is
        ///     a portal near the enemy's castle.
        ///     if no suitable location is found, null is returned.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="isDefensive"></param>
        /// <returns></returns>
        public static Location GetOptimalPortalLocation(this Elf e, bool isDefensive)
        {
            int maximumRadius = isDefensive ? ElfExtensions.DEFENSIVE_RADIUS : ElfExtensions.ATTACKING_RADIUS;
            Castle castle = isDefensive ? GameState.MyCastle : GameState.EnemyCastle;
            List<Portal> portals = isDefensive ? GameState.MyPortals : GameState.EnemyPortals;
            var locations = new HashSet<Location>();

            for (int radius = GameState.Game.CastleSize + GameState.Game.PortalSize + 10;
                 radius <= maximumRadius;
                 radius += 200)
            {
                var circle = new Circle(castle.Location, radius);
                locations.UnionWith(circle.GetCircleLocations());
            }

            return (from location in locations
                    where GameState.Game.CanBuildPortalAt(location)
                    orderby e.Distance(location) <= e.MaxSpeed ? e.MaxSpeed : e.Distance(location),
                        DistanceSum(portals, location) descending,
                        location.Distance(castle)
                    select location).FirstOrDefault();
        }

        /// <summary>
        ///     returns the optimal location for Elf e to build a mana fountain.
        ///     if no suitable location is found, null is returned.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static Location GetOptimalFountainLocation(this Elf e)
        {
            const int maximumRadius = ElfExtensions.DEFENSIVE_RADIUS;
            for (int radius = GameState.Game.CastleSize + GameState.Game.ManaFountainSize + 10;
                 radius <= maximumRadius;
                 radius += 200)
            {
                var circle = new Circle(GameState.MyCastle.Location, radius);
                Location best = (from location in circle.GetCircleLocations()
                                 where GameState.Game.CanBuildManaFountainAt(location)
                                 orderby e.Distance(location) <= e.MaxSpeed ? e.MaxSpeed : e.Distance(location),
                                     DistanceSum(GameState.EnemyPortals, location) descending,
                                     location.Distance(GameState.EnemyCastle) descending
                                 select location).FirstOrDefault();
                if (best != null)
                    return best;
            }

            return null;

        }

        /// <summary>
        ///     Returns the safest path (the next step) for obj to reach destination.
        ///     Any enemy exceptions given will not be taken into consideration,
        ///     for example pass exceptions when your destination is an enemy.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="destination"></param>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        public static Location GetSafestPath(this GameObject obj, MapObject destination, params GameObject[] exceptions)
        {
            var circle = new Circle(obj.Location, obj.GetMaxSpeed());
            return (from location in circle.GetCircleLocations()
                    orderby GetPossibleAttackers(location, obj, exceptions).Count,
                        location.Distance(destination)
                    select location).FirstOrDefault();

        }


        /// <summary>
        ///     returns the possible attackers that can attack the
        ///     given location next turn if they move towards enemy destination, exceptions are ignored
        ///     and not returned in the list.
        ///     if enemyDestination is null, location is used instead.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="enemyDestination"></param>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        public static List<GameObject> GetPossibleAttackers(MapObject location, MapObject enemyDestination = null, params GameObject[] exceptions)
        {
            enemyDestination = enemyDestination ?? location;
            var enemies = ((IEnumerable<GameObject>)GameState.EnemyLivingElves).Concat(GameState.EnemyLivingIceTrolls);
            return (from enemy in enemies
                    let nextTurnLocation = enemy.Location.Towards(enemyDestination, enemy.GetMaxSpeed())
                    where !exceptions.Contains(enemy) &&
                          nextTurnLocation.InRange(location, enemy.GetAttackRange()) &&
                          enemy.CurrentHealth != enemy.GetSuffocationPerTurn()
                    select enemy).ToList();
        }

        /// <summary>
        ///     returns the sum of distances of objects to location
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        private static int DistanceSum(IEnumerable<MapObject> objects, Location location) =>
            objects.Select(o => o.Distance(location)).DefaultIfEmpty(0).Sum();
    }
}
