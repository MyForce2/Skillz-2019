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

        /// <summary>
        /// returns true if elf should build portal
        /// </summary>
        /// <param name="e"> elf object </param>
        /// <returns></returns>
        private static bool ShouldBuildPortal(this Elf e)
        {
            var portalCount = Bot.Game.GetMyPortals().Length;
            if (portalCount > 2)
                return false;
            if (portalCount == 2 && Bot.Game.GetMyMana() < 200)
                return false;

            // PLACEHOLDER: check if elf distance to enemy castle is less than 60% of castles distance
            var elfDistance = e.Distance(Bot.Game.GetEnemyCastle());
            var castleDistance = Bot.Game.GetMyCastle().Distance(Bot.Game.GetEnemyCastle());
            return (elfDistance < 3000);
        }

        /// <summary>
        /// moves elf safely toward target while avoiding enemy ice trolls (if any)
        /// </summary>
        /// <param name="e"> elf object </param>
        /// <param name="target"> target MapObject to move to </param>
        private static void MoveSafely(this Elf e, MapObject target)
        {
            e.MoveTo(target);
            return;
            if (Bot.Game.GetEnemyIceTrolls().Length == 0)
            { // no enemy ice trolls
                e.MoveTo(target);
                return;
            }
            // get nearest enemy ice troll and move to target while avoiding him
            // TODO: replace this with safest path for all ice trolls
            //  IceTroll nearestEnemyTroll = Bot.Game.GetEnemyIceTrolls().OrderBy(x => x.Distance(e)).First();
            // TODO: create "away" function for location (reversed towards) (function does nothing for now)
            e.MoveTo(target);
        }




        /// <summary>
        /// returns best target to attack or null if shouldn't attack or no targets in range
        /// </summary>
        /// <param name="elf"> elf object </param>
        /// <returns></returns>
        public static GameObject GetAttackTarget(this Elf elf)
        {
            IEnumerable<GameObject> allTargets = ((GameObject[])Bot.Game.GetEnemyLivingElves())
                                                 .Concat(Bot.Game.GetEnemyCreatures()).Concat(Bot.Game.GetEnemyPortals());
            return (from target in allTargets
                    where elf.InAttackRange(target)
                    orderby target is Elf ? 0 : 1,
                        target is Portal ? 0 : 1,
                        target.CurrentHealth,
                        elf.Distance(target)
                    select target).FirstOrDefault();
        }

        /// <summary>
        /// returns best location to move to or null if shouldn't move
        /// </summary>
        /// <param name="e"> elf object </param>
        /// <returns></returns>
        private static MapObject GetMoveTarget(this Elf e)
        {
            // TODO: replace Move logic entirely
            switch (Bot.Game.GetMyPortals().Length)
            {
                case 0: // no portals
                    return Bot.Game.GetEnemyCastle();
                case 1: // move toward enemy castle until far enough to build another portal then do nothing
                    return (e.Distance(Bot.Game.GetEnemyCastle()) > 2000) ? (Bot.Game.GetEnemyCastle()) : (null);
                default: // max portal count reached, move to portal closest to city
                         // TODO: move to attack with elves
                         //move to nearest target
                    if (e.ShouldBuildPortal())
                        return Bot.Game.GetEnemyCastle();
                    var targets = Bot.Game.GetEnemyLivingElves().Cast<GameObject>();
                    targets = targets.Concat(Bot.Game.GetEnemyIceTrolls().Cast<GameObject>());
                    targets = targets.Concat(Bot.Game.GetEnemyLavaGiants().Cast<GameObject>());
                    targets = targets.Concat(Bot.Game.GetEnemyPortals().Cast<GameObject>());
                    return (targets.Count() == 0) ? (null) : (targets.OrderBy(x => x.Distance(e)).ThenBy(x => x.CurrentHealth).First());
            }
        }
        public static Portal PortalInMySide()
        {
            Portal[] portals = Bot.Game.GetEnemyPortals();

            foreach (Portal portal in portals)
            {
                if (portal.Distance(Bot.Game.GetMyCastle()) < 2500)
                {
                    return portal;
                }
            }
            return null;
        }
        /// <summary>
        /// does elf turn logic
        /// PLACEHOLDER
        /// </summary>
        public static void DoElfTurn(this Elf e)
        {
            System.Console.WriteLine("here");
            if (e.IsBuilding)
            { //already building
                System.Console.WriteLine("elf does nothing");
                return;
            }
            if (PortalInMySide() != null)
            {
                if (e.InAttackRange(PortalInMySide()))
                {
                    e.Attack(PortalInMySide());
                }
                else
                {
                    e.MoveTo(PortalInMySide());
                }
                return;
            }
            // build portal
            if (e.CanBuildPortal() && e.ShouldBuildPortal())
            {
                System.Console.WriteLine("elf built portal");
                e.BuildPortal();
                return;
            }
            // attack
            var nearestPortal = Bot.Game.GetEnemyPortals().OrderBy(p => e.Distance(p)).FirstOrDefault();
            var attackTarget = e.GetAttackTarget();
            if (attackTarget != null)
            {
                if (nearestPortal != null)
                {
                    if (!(attackTarget is Portal) && e.Distance(nearestPortal) < 600)
                    {
                        System.Console.WriteLine("Elf priority is portal");
                        e.MoveTo(nearestPortal);
                        return;
                    }
                }
                System.Console.WriteLine("elf attacked");
                e.Attack(attackTarget);
                return;
            }
            var moveTarget = e.GetMoveTarget();
            // move
            if (moveTarget != null)
            {
                System.Console.WriteLine("elf moved");
                e.MoveSafely(moveTarget);
                return;
            }
            System.Console.WriteLine("elf did nothing");
        }


    }
}
