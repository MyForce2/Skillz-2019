using System.Collections.Generic;
using System.Linq;
using ElfKingdom;

namespace MyBot
{

    public enum Mission
    {
        BuildPortal = 0,
        MoveToBuildPortal = 1,
        BuildFountain = 2,
        MoveTBuildFountain = 3,
        MoveToTarget = 4,
        AttackTarget = 5,
        MoveToCastle = 6
    }

    public static class MissionExtensions
    {

        private static readonly Dictionary<Elf, Location> OptimalDefensivePortalLocations =
            new Dictionary<Elf, Location>();

        private static readonly Dictionary<Elf, Location> OptimalAttackingPortalLocations =
            new Dictionary<Elf, Location>();

        private static readonly Dictionary<Elf, Location> OptimalFountainLocations =
            new Dictionary<Elf, Location>();

        /// <summary>
        ///     updates the optimal locations for builds for each elf
        /// </summary>
        public static void UpdateOptimalLocations()
        {
            foreach (Elf e in GameState.MyLivingElves)
            {
                // compute optimal location now to avoid additional computation later. 
                Location optimalFountainLocation = e.GetOptimalFountainLocation(),
                         attackOptimalLocation = e.GetOptimalPortalLocation(false),
                         defenseOptimaLocation = e.GetOptimalPortalLocation(true);
                MissionExtensions.OptimalAttackingPortalLocations[e] = attackOptimalLocation;
                MissionExtensions.OptimalDefensivePortalLocations[e] = defenseOptimaLocation;
                MissionExtensions.OptimalFountainLocations[e] = optimalFountainLocation;
            }
        }

        /// <summary>
        ///     returns the best mission for the elf
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static Mission GetBestMission(this Elf e)
        {
            // make sure only one elf focuses on fountains at the start of the game
            bool isDefensive = true;
            if (GameState.MyLivingElves.Count > 1)
            {
                isDefensive = GameState.MyLivingElves.OrderBy(elf => elf.Distance(GameState.MyCastle))
                                       .FirstOrDefault().Equals(e);
            }
            else
            {
                isDefensive = e.Distance(GameState.MyCastle) < e.Distance(GameState.EnemyCastle);
            }

            // compute optimal location now to avoid additional computation later. 
            Location optimalFountainLocation = MissionExtensions.OptimalFountainLocations[e],
                     attackOptimalLocation = MissionExtensions.OptimalAttackingPortalLocations[e],
                     defenseOptimaLocation = MissionExtensions.OptimalDefensivePortalLocations[e];


            if (isDefensive && GameState.Game.CanBuildManaFountainAt(e.Location) &&
                e.ShouldBuildFountain(optimalFountainLocation))
            {
                return Mission.BuildFountain;
            }

            if (isDefensive && e.Distance(GameState.MyCastle) <= ElfExtensions.DEFENSIVE_RADIUS &&
                GameState.Game.GetMyManaFountains().Length < ElfExtensions.MAX_FOUNTAINS &&
                e.ShouldAttemptToBuildFountain(optimalFountainLocation))
            {
                return Mission.MoveTBuildFountain;
            }

            if (GameState.Game.CanBuildPortalAt(e.Location) &&
                e.ShouldBuildPortal(defenseOptimaLocation, attackOptimalLocation))
            {
                return Mission.BuildPortal;
            }

            if (e.Distance(GameState.MyCastle) <= ElfExtensions.DEFENSIVE_RADIUS &&
                (GameState.DefensivePortals < ElfExtensions.MAX_DEFENSIVE_PORTALS ||
                 GameState.CurrentMana > ElfExtensions.EXCESS_MANA * 2) && defenseOptimaLocation != null)
            {
                return Mission.MoveToBuildPortal;
            }

            // check to see if we can attack any portal or elves or the castle or fountains
            GameObject target = e.GetAttackTarget();
            if (target != null && e.ShouldAttack(target))
            {
                return Mission.AttackTarget;
            }

            var moveTarget = e.GetMoveTarget(isDefensive);
            if (isDefensive)
            {
                System.Console.WriteLine(moveTarget);
                if (moveTarget != null && target != null &&
                    (moveTarget.Equals(target) || (moveTarget is Elf && target is Elf)))
                {
                    return Mission.AttackTarget;
                }

                if (moveTarget != null)
                {
                    return Mission.MoveToTarget;
                }
            }
            else if (moveTarget != null && !moveTarget.Equals(target) &&
                     (e.Distance(moveTarget) < e.Distance(attackOptimalLocation) ||
                      GameState.AttackingPortals > 0))
            {
                return Mission.MoveToTarget;
            }

            // if we got nothing to attack we move to build an attacking portal
            if (e.Distance(GameState.EnemyCastle) <= ElfExtensions.ATTACKING_RADIUS &&
                (GameState.AttackingPortals < ElfExtensions.MAX_ATTACKING_PORTALS ||
                 GameState.CurrentMana > ElfExtensions.EXCESS_MANA * 2) && attackOptimalLocation != null)
            {
                return Mission.MoveToBuildPortal;
            }

            return Mission.MoveToCastle;
        }

        /// <summary>
        ///     executes the mission with the given elf
        /// </summary>
        /// <param name="mission"></param>
        /// <param name="e"></param>
        public static void ExecuteWith(this Mission mission, Elf e)
        {
            switch (mission)
            {
                case Mission.BuildPortal:
                    BuildPortal(e);
                    break;
                case Mission.MoveToBuildPortal:
                    MoveToBuildPortal(e);
                    break;
                case Mission.BuildFountain:
                    BuildFountain(e);
                    break;
                case Mission.MoveTBuildFountain:
                    MoveToBuildFountain(e);
                    break;
                case Mission.MoveToTarget:
                    MoveToTarget(e);
                    break;
                case Mission.AttackTarget:
                    AttackTarget(e);
                    break;
                case Mission.MoveToCastle:
                    MoveToCastle(e);
                    break;
            }
        }

        public static void MoveToCastle(Elf e)
        {
            // if we don't have any portal/elf to attack we move towards the castle
            e.MoveSafely(GameState.EnemyCastle.Location.Towards(e, e.AttackRange));
        }

        public static void AttackTarget(Elf e)
        {
            e.Attack(e.GetAttackTarget());
        }

        public static void MoveToTarget(Elf e)
        {
            bool isDefensive = true; // make sure only one elf focuses on fountains at the start of the game
            if (GameState.MyLivingElves.Count > 1)
            {
                isDefensive = GameState.MyLivingElves.OrderBy(elf => elf.Distance(GameState.MyCastle))
                                       .FirstOrDefault().Equals(e);
            }
            else
            {
                isDefensive = e.Distance(GameState.MyCastle) < e.Distance(GameState.EnemyCastle);
            }

            e.MoveSafely(e.GetMoveTarget(isDefensive));
        }

        public static void BuildFountain(Elf e)
        {
            System.Console.WriteLine("fountain");
            // if we should and can build fountain, build it
            if (e.CanBuildManaFountain() && GameState.HasManaFor(CreatableObject.ManaFountain))
            {
                e.BuildManaFountain();
                return;
            }

            // make sure we can wait for mana
            if (!e.SafeNotToMove()) return;

            // if we should build but there is no mana, we don't move and save the mana for the next turn
            // so we can build it in the following turns
            GameState.SaveManaFor(CreatableObject.ManaFountain);
            // if we don't move from the position we at least attack
            // so we don't completely waste the turn
            GameObject attackObject = e.GetAttackTarget();
            if (attackObject != null)
            {
                e.Attack(attackObject);
            }
            System.Console.WriteLine("saved");
            return;
        }

        public static void MoveToBuildFountain(Elf e)
        {
            e.MoveSafely(MissionExtensions.OptimalFountainLocations[e]);
        }

        public static void BuildPortal(Elf e)
        {
            System.Console.WriteLine("portal");
            // if we should and can build fountain, build it
            if (e.CanBuildPortal() && GameState.HasManaFor(CreatableObject.Portal))
            {
                e.BuildPortal();
                return;
            }

            // make sure we can wait for mana
            if (!e.SafeNotToMove()) return;

            // if we should build but there is no mana, we don't move and save the mana for the next turn
            // so we can build it in the following turns
            GameState.SaveManaFor(CreatableObject.Portal);
            // if we don't move from the position we at least attack
            // so we don't completely waste the turn
            GameObject attackObject = e.GetAttackTarget();
            if (attackObject != null)
            {
                e.Attack(attackObject);
            }
            System.Console.WriteLine("saved");
        }

        public static void MoveToBuildPortal(Elf e)
        {
            bool isDefensive = e.Distance(GameState.MyCastle) <= ElfExtensions.DEFENSIVE_RADIUS;
            bool isAttacking = e.Distance(GameState.EnemyCastle) <= ElfExtensions.ATTACKING_RADIUS;

            if (isDefensive)
            {
                e.MoveSafely(MissionExtensions.OptimalDefensivePortalLocations[e]);
                return;
            }
            if (isAttacking)
            {
                e.MoveSafely(MissionExtensions.OptimalAttackingPortalLocations[e]);
            }
        }
    }
}
