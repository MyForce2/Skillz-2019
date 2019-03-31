using System.Collections.Generic;
using System.Linq;
using ElfKingdom;

namespace MyBot
{

    public enum Mission
    {
        CurrentlyBuilding,
        BuildPortal,
        SaveForPortal,
        MoveToBuildPortal,
        BuildFountain,
        SaveForFountain,
        MoveToBuildFountain,
        MoveToTarget,
        AttackTarget,
        MoveToCastle,
        RushFountain
    }

    public static class MissionExtensions
    {

        public static int MAXIMUM_SAVING_MANA_TURNS => GameState.Game.PortalBuildingDuration;

        private static readonly Dictionary<Elf, MapObject> MoveTargets =
            new Dictionary<Elf, MapObject>();

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
                Location attackOptimalLocation = e.GetOptimalPortalLocation(false);
                MissionExtensions.OptimalAttackingPortalLocations[e] = attackOptimalLocation;
                MissionExtensions.MoveTargets[e] = e.GetMoveTarget();
            }
        }

        public static Dictionary<Elf, Mission> ExecuteMissions(this IEnumerable<Elf> elves)
        {
            var missions = new Dictionary<Elf, Mission>();

            ManaFountain fountain;
            var fountainRusher = elves.Where(e => e.ShouldTargetFountain(out fountain) &&
                                                  e.GetBestMission() != Mission.BuildFountain &&
                                                  e.GetBestMission() != Mission.SaveForFountain)
                                       .OrderBy(e => e.ShouldTargetFountain(out fountain)
                                                    ? e.Distance(fountain)
                                                    : int.MaxValue).FirstOrDefault();
            if (fountainRusher != null)
            {
                missions[fountainRusher] = Mission.RushFountain;
                Mission.RushFountain.ExecuteWith(fountainRusher);
            }

            elves = elves.Where(e => !missions.ContainsKey(e));
            foreach (Elf e in elves)
            {
                Mission mission = e.GetBestMission();
                if (mission == Mission.BuildFountain || mission == Mission.SaveForFountain || mission == Mission.BuildPortal ||
                    mission == Mission.SaveForPortal || mission == Mission.CurrentlyBuilding ||
                    mission == Mission.AttackTarget)
                {
                    missions[e] = mission;
                    mission.ExecuteWith(e);
                }
            }

            elves = elves.Where(e => !missions.ContainsKey(e));
            if (!GameState.EnemyLivingElves.Any() || GameState.AttackingPortals > 0) // conditions in which we allow two elves tp chase the same enemy elf
            {
                foreach (Elf e in elves)
                {
                    Mission mission = e.GetBestMission();
                    missions[e] = mission;
                    mission.ExecuteWith(e);
                }

                return missions;
            }

            // doing all of this so both our elves won't chase the same enemy elf
            elves = from e in elves
                    where !missions.ContainsKey(e)
                    let closestElf = GameState.EnemyLivingElves.OrderBy(enemy => enemy.Distance(e)).FirstOrDefault()
                    orderby e.Distance(closestElf)
                    select e;

            foreach (Elf e in elves)
            {
                var exceptions = new List<GameObject>();
                while (true)
                {
                    GameObject target = e.GetMoveTarget(exceptions.ToArray());
                    GameObject attackTarget = e.GetAttackTarget();
                    Location attackOptimalLocation = MissionExtensions.OptimalAttackingPortalLocations[e];

                    if (!(target != null && !target.Equals(attackTarget) &&
                         (attackOptimalLocation == null || e.Distance(target) < e.Distance(attackOptimalLocation) || target is ManaFountain ||
                          GameState.AttackingPortals > 0)))
                    {
                        break;
                    }

                    if (!(target is Elf))
                    {
                        var mission = Mission.MoveToTarget;
                        missions[e] = mission;
                        MissionExtensions.MoveTargets[e] = target;
                        mission.ExecuteWith(e);
                        break;
                    }

                    bool otherChasers = false;
                    foreach (Elf elf in missions.Keys)
                    {
                        if (missions[elf] != Mission.MoveToTarget)
                            continue;
                        if (MissionExtensions.MoveTargets.TryGetValue(elf, out MapObject t) && t.Equals(target))
                            otherChasers = true;
                    }

                    if (!otherChasers)
                    {
                        var mission = Mission.MoveToTarget;
                        missions[e] = mission;
                        MissionExtensions.MoveTargets[e] = target;
                        mission.ExecuteWith(e);
                        break;
                    }

                    exceptions.Add(target);
                }
            }

            elves = elves.Where(e => !missions.ContainsKey(e));
            foreach (Elf e in elves)
            {
                Mission mission = e.GetBestMission();
                missions[e] = mission;
                System.Console.WriteLine(mission);
                mission.ExecuteWith(e);
            }

            return missions;
        }

        /// <summary>
        ///     returns the best mission for the elf
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static Mission GetBestMission(this Elf e)
        {
            if (e.IsBuilding)
                return Mission.CurrentlyBuilding;

            // compute optimal location now to avoid additional computation later. 
            Location attackOptimalLocation = MissionExtensions.OptimalAttackingPortalLocations[e];

            if (GameState.Game.CanBuildManaFountainAt(e.Location))
            {
                if (e.ShouldBuildFountain() && e.CanBuildManaFountain() &&
                    GameState.HasManaFor(CreatableObject.ManaFountain))
                {
                    return Mission.BuildFountain;
                }

                if (e.ShouldSaveForFountain())
                {
                    return Mission.SaveForFountain;
                }
            }

            if (GameState.Game.CanBuildPortalAt(e.Location) &&
                e.ShouldBuildPortal(attackOptimalLocation) && e.SafeNotToMove())
            {
                System.Console.WriteLine(e.SafeNotToMove());
                if (e.CanBuildPortal() && GameState.HasManaFor(CreatableObject.Portal))
                {
                    return Mission.BuildPortal;
                }

                // make sure we can wait for mana and that it wouldn't take so long and that its a good portal
                if (e.ShouldSaveForPortal())
                {
                    return Mission.SaveForPortal;
                }
            }

            GameObject target = e.GetAttackTarget();
            if (target != null && e.ShouldAttack(target))
            {
                return Mission.AttackTarget;
            }

            var moveTarget = e.GetMoveTarget();
            if (moveTarget != null && !moveTarget.Equals(target) &&
                     (attackOptimalLocation == null || e.Distance(moveTarget) < e.Distance(attackOptimalLocation) || moveTarget is ManaFountain ||
                      GameState.AttackingPortals > 0))
            {
                return Mission.MoveToTarget;
            }

            if (e.Distance(GameState.EnemyCastle) <= ElfExtensions.ATTACKING_RADIUS &&
                (GameState.AttackingPortals < ElfExtensions.MAX_ATTACKING_PORTALS ||
                 GameState.CurrentMana > ElfExtensions.EXCESS_MANA * 2) &&
                attackOptimalLocation != null && (e.ShouldSaveForPortal() || GameState.MyPortals.Count == 0))

            {
                System.Console.WriteLine(e.ShouldBuildPortal(attackOptimalLocation));
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
                case Mission.CurrentlyBuilding:
                    if (e.CurrentlyBuilding.Equals(CreatableObject.ManaFountain.ToString()))
                        GameState.CurrentlyBuiltFountains++;
                    break;
                case Mission.BuildPortal:
                    BuildPortal(e);
                    break;
                case Mission.MoveToBuildPortal:
                    MoveToBuildPortal(e);
                    break;
                case Mission.BuildFountain:
                    BuildFountain(e);
                    break;
                case Mission.MoveToBuildFountain:
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
                case Mission.SaveForPortal:
                    SaveForPortal(e);
                    break;
                case Mission.SaveForFountain:
                    SaveForFountain(e);
                    break;
                case Mission.RushFountain:
                    RushFountain(e);
                    break;
            }
        }

        public static void RushFountain(Elf e)
        {
            ManaFountain fountain;
            e.ShouldTargetFountain(out fountain);
            if (e.InRange(fountain, e.MaxSpeed * 2))
            {
                double savingDuration = 1.0 * (GameState.Game.InvisibilityCost - GameState.CurrentMana) / GameState.Game.GetMyself().ManaPerTurn;
                savingDuration = System.Math.Ceiling(savingDuration);
                int duration = (int)System.Math.Max(savingDuration, 1);

                if (!e.SafeNotToMove(duration))
                {
                    if (GameState.HasManaFor(CreatableObject.Invisibility))
                    {
                        e.CastInvisibility();
                    }
                    else
                    {
                        GameState.SaveManaFor(CreatableObject.Invisibility);
                    }
                }
            }
            if (!e.AlreadyActed && e.ShouldAttack(fountain) && e.InAttackRange(fountain))
            {
                e.Attack(fountain);
            }
            else if (!e.AlreadyActed)
            {
                MissionExtensions.MoveTargets[e] = fountain;
                Mission.MoveToTarget.ExecuteWith(e);
            }
        }

        public static void MoveToCastle(Elf e)
        {
            Location attackLocation =
                GameState.EnemyCastle.Location.Towards(e, e.AttackRange + GameState.EnemyCastle.Size);
            if (e.Location.Equals(attackLocation))
            {
                if (e.ShouldAttack(GameState.EnemyCastle))
                {
                    e.Attack(GameState.EnemyCastle);
                    return;
                }

                var target = e.GetAttackTarget();
                if (target is Elf && LocationFinder.GetPossibleAttackers(e, exceptions: target).Count == 0)
                {
                    e.Attack(target);
                    return;
                }
            }
            e.MoveSafely(attackLocation);
        }

        public static void AttackTarget(Elf e)
        {
            e.Attack(e.GetAttackTarget());
        }

        public static void MoveToTarget(Elf e)
        {
            var target = MissionExtensions.MoveTargets[e];
            bool elfTarget = target is Elf && !e.IsSpedUp() && e.IsInChaseWith((Elf)target);
            bool fountainTarget = e.ShouldTargetFountain(out ManaFountain fountain) && fountain?.Equals(target) == true;
            if (GameState.AttackingPortals == 0 && (elfTarget || fountainTarget))
            {
                if (GameState.HasManaFor(CreatableObject.SpeedUp) && e.CanCastSpeedUp())
                {
                    e.CastSpeedUp();
                    return;
                }

                GameState.SaveManaFor(CreatableObject.SpeedUp);
            }

            target = target ?? e.GetMoveTarget();
            if (target == null)
                return;

            e.MoveSafely(target);
        }

        public static void BuildFountain(Elf e)
        {
            GameState.CurrentlyBuiltFountains++;
            e.BuildManaFountain();
        }

        public static void SaveForFountain(Elf e)
        {
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
        }

        public static void MoveToBuildFountain(Elf e)
        {
            System.Console.WriteLine("Fountain");
            e.MoveSafely(MissionExtensions.OptimalFountainLocations[e]);
        }

        public static void BuildPortal(Elf e)
        {
            System.Console.WriteLine("portal");
            e.BuildPortal();
        }

        public static void SaveForPortal(Elf e)
        {
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
            e.MoveSafely(MissionExtensions.OptimalAttackingPortalLocations[e]);
        }
    }
}
