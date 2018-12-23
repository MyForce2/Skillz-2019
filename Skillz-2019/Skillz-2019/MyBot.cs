using System.Collections.Generic;
using System.Linq;
using System.Collections;
using ElfKingdom;

namespace MyBot
{
    /// <summary>
    ///     Holds all the variables for the current turn
    /// </summary>
    public static class GameVariables
    {
        #region Properties

        public static Game CurrentGame { get; private set; }

        public static Castle MyCastle { get; private set; }
        public static Castle EnemyCastle { get; private set; }

        public static Elf DefendingElf { get; private set; }
        public static Elf AttackingElf { get; private set; }
        public static Elf[] EnemyLivingElves { get; private set; }

        public static LavaGiant[] MyLivingLavaGiants { get; private set; }
        public static LavaGiant[] EnemyLivingLavaGiants { get; private set; }

        public static IceTroll[] MyLivingIceTrolls { get; private set; }
        public static IceTroll[] EnemyLivingIceTrolls { get; private set; }

        public static Portal[] MyPortals { get; private set; }
        public static Portal[] EnemyPortals { get; private set; }

        public static IEnumerable<GameObject> AllLivingEnemies =>
            ((GameObject[]) GameVariables.EnemyLivingElves).Concat(GameVariables.CurrentGame.GetEnemyCreatures());

        #endregion

        public static void UpdateCurrentGame(Game game)
        {
            GameVariables.CurrentGame = game;

            GameVariables.MyCastle = game.GetMyCastle();
            GameVariables.EnemyCastle = game.GetEnemyCastle();

            GameVariables.EnemyLivingElves = game.GetEnemyLivingElves();
            Elf[] myElfElves = game.GetMyLivingElves();
            switch (myElfElves.Length)
            {
                case 2:
                    myElfElves = myElfElves.OrderBy(elf => elf.Distance(GameVariables.MyCastle)).ToArray();
                    GameVariables.DefendingElf = myElfElves[0];
                    GameVariables.AttackingElf = myElfElves[1];
                    break;
                case 1:
                {
                    Elf elf = myElfElves[0];
                    bool isAttacking = elf.Distance(GameVariables.EnemyCastle) < elf.Distance(GameVariables.MyCastle);
                    GameVariables.DefendingElf = isAttacking ? null : elf;
                    GameVariables.AttackingElf = isAttacking ? elf : null;
                    break;
                }
                default:
                    GameVariables.AttackingElf = null;
                    GameVariables.DefendingElf = null;
                    break;
            }

            GameVariables.MyLivingLavaGiants = game.GetMyLavaGiants();
            GameVariables.EnemyLivingLavaGiants = game.GetEnemyLavaGiants();

            GameVariables.MyLivingIceTrolls = game.GetMyIceTrolls();
            GameVariables.EnemyLivingIceTrolls = game.GetEnemyIceTrolls();

            GameVariables.MyPortals = game.GetMyPortals();
            GameVariables.EnemyPortals = game.GetEnemyPortals();
        }
    }

    public static class Extensions
    {
        /// <summary>
        ///     returns the attack range of this game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetAttackRange(this GameObject obj)
        {
            switch (obj)
            {
                case Creature creature:
                    return creature.AttackRange;
                case Elf elf:
                    return elf.AttackRange;
                default:
                    return 0;
            }
        }
        
        /// <summary>
        ///     Returns a list of the enemies in the given range of this game object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static List<GameObject> GetEnemiesInRange(this GameObject obj, int range)
        {
            List<GameObject> enemiesInRange = new List<GameObject>();
            foreach(Elf enemyElf in GameVariables.EnemyLivingElves)
            {
                if (obj.InRange(enemyElf, range))
                    enemiesInRange.Add(enemyElf);
            }
            foreach (IceTroll iceTroll in GameVariables.EnemyLivingIceTrolls)
            {
                if (obj.InRange(iceTroll, range))
                    enemiesInRange.Add(iceTroll);
            }
            foreach (LavaGiant lavaGiant in GameVariables.EnemyLivingLavaGiants)
            {
                if (obj.InRange(lavaGiant, range))
                    enemiesInRange.Add(lavaGiant);
            }
            return enemiesInRange;
        }

        /// <summary>
        ///     returns the max speed of this game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetMaxSpeed(this GameObject obj)
        {
            switch (obj)
            {
                case Creature creature:
                    return creature.MaxSpeed;
                case Elf elf:
                    return elf.MaxSpeed;
                default:
                    return 0;
            }
        }

        /// <summary>
        ///     returns the suffocation per turn of this game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetSuffocationPerTurn(this GameObject obj)
        {
            switch (obj)
            {
                case IceTroll troll:
                    return troll.SuffocationPerTurn;
                case LavaGiant lavaGiant:
                    return lavaGiant.SuffocationPerTurn;
                default:
                    return 0;
            }
        }

        /// <summary>
        ///     returns the attack multiplier of this game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int GetAttackMultiplier(this GameObject obj)
        {
            switch (obj)
            {
                case Creature creature:
                    return creature.AttackMultiplier;
                case Elf elf:
                    return elf.AttackMultiplier;
                default:
                    return 0;
            }
        }

        /// <summary>
        ///     Returns whether the attacker can attack the attacked object according to the rules of the game
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="attackedObject"></param>
        /// <returns></returns>
        public static bool CanAttack(this GameObject attacker, GameObject attackedObject)
        {
            // elf can attack everything
            if (attacker is Elf)
                return true;

            // ice troll can only attack elves and other ice trolls
            if (attacker is IceTroll && (attackedObject is Elf || attackedObject is Creature))
                return true;

            // the only remaining option is that the attacker is a lava giant and
            // the attacked object is a castle
            return attacker is LavaGiant && attackedObject is Castle;
        }

        /// <summary>
        ///     returns the amount of damage the lava giant can do to the castle if he is left alone.
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="attackedCastle"></param>
        /// <returns></returns>
        public static int PossibleDamageTo(this LavaGiant attacker, Castle attackedCastle)
        {
            int distanceToTarget = attacker.Distance(attackedCastle.Location.Towards(attacker, attacker.AttackRange));
            int turnsToReach = distanceToTarget / attacker.GetMaxSpeed();

            int attackerHealthAfterTravel = attacker.CurrentHealth - turnsToReach * attacker.SuffocationPerTurn;

            // if the attacker will die before reaching its target it will not do any damage
            if (attackerHealthAfterTravel <= 0)
                return 0;

            int turnsForAttackerToDie = attackerHealthAfterTravel / attacker.SuffocationPerTurn;

            // we return the amount of damage the attacker can dish out to the castle
            return turnsForAttackerToDie * attacker.AttackMultiplier;
        }

        /// <summary>
        ///     returns whether the attacker will be able to kill the attacked object if no other object interferes.
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="attackedObject"></param>
        /// <returns></returns>
        public static bool WillKill(this GameObject attacker, GameObject attackedObject)
        {
            // if the attacker cant even attack, we return false
            if (!attacker.CanAttack(attackedObject))
                return false;

            int distanceToTarget =
                attacker.Distance(attackedObject.Location.Towards(attacker, attacker.GetAttackRange()));
            int turnsToReach = distanceToTarget / attacker.GetMaxSpeed();

            int attackerHealthAfterTravel = attacker.CurrentHealth - turnsToReach * attacker.GetSuffocationPerTurn();

            // if the attacker will die before reaching its target it will not kill it
            if (attackerHealthAfterTravel <= 0)
                return false;

            // the damage to the attacker is the damage by the attacked and the suffocation damage
            int damageToAttacker = attackedObject.GetAttackMultiplier() + attacker.GetSuffocationPerTurn();
            int turnsToKillAttacker = attackerHealthAfterTravel / damageToAttacker;

            // the damage to the attacked is the damage by the attacker and the suffocation damage
            int damageToAttacked = attacker.GetAttackMultiplier() + attackedObject.GetSuffocationPerTurn();
            int attackedObjectHealthAfterTravel =
                attackedObject.CurrentHealth - turnsToReach * attacker.GetSuffocationPerTurn();
            int turnsToKillAttacked = attackedObjectHealthAfterTravel / damageToAttacked;

            // if the number of turns to kill the attacked object are smaller then the number of turns to kill the attacker
            // then attacker will kill the attacked
            return turnsToKillAttacked < turnsToKillAttacker;
        }
    }

    public class Bot : ISkillzBot
    {
        public void DoTurn(Game game)
        {
            GameVariables.UpdateCurrentGame(game);

            DefendWith(GameVariables.DefendingElf);
            AttackWith(GameVariables.AttackingElf);

            // if our portal still stands
            Portal myPortal = GameVariables.MyPortals.FirstOrDefault();
            if (myPortal != null && myPortal.CanSummonLavaGiant())
            {
                myPortal.SummonLavaGiant();
            }
        }

        public void AttackWith(Elf attacker)
        {
            GameObject destination = null;
            if (GameVariables.EnemyPortals.Length != 0)
            {
                if (GameVariables.EnemyPortals.Length == 1)
                {
                    destination = GameVariables.EnemyPortals[0];
                }
                else
                {
                    Dictionary<Portal, int> enemiesNearPortals = new Dictionary<Portal, int>();
                    var nearestPortal = GameVariables.EnemyPortals.OrderBy(portal => portal.Distance(attacker)).First();
                    foreach(Portal p in GameVariables.EnemyPortals)
                    {
                        enemiesNearPortals[p] = p.GetEnemiesInRange(attacker.AttackRange).Count;
                    }
                    var safestPortal = enemiesNearPortals.Keys.OrderBy(portal => enemiesNearPortals[portal]).First();
                    if (safestPortal != nearestPortal && enemiesNearPortals[safestPortal] != enemiesNearPortals[nearestPortal])
                        destination = safestPortal;
                }
            }
            else
            {
                destination = GameVariables.EnemyCastle;
            }
            if (attacker.InAttackRange(destination))
                attacker.Attack(destination);
            else
                attacker.MoveTo(destination);
        }

        public void DefendWith(Elf defender)
        {
            Portal myPortal = GameVariables.MyPortals.FirstOrDefault();

            // if our portal was destroyed
            if (myPortal is null)
            {
                Location newPortalLocation =
                    GameVariables.MyCastle.Location.Towards(GameVariables.EnemyCastle,
                                                            GameVariables.MyCastle.Size +
                                                            GameVariables.CurrentGame.PortalSize);

                if (defender.Location.Equals(newPortalLocation) && defender.CanBuildPortal())
                    defender.BuildPortal();

                // if were at the location but short on mana, we try to find an enemy to attack in the mean time
                else if (defender.Location.Equals(newPortalLocation))
                {
                    GameObject enemyToAttack = (from enemy in GameVariables.AllLivingEnemies
                                                where defender.InAttackRange(enemy)
                                                orderby enemy is LavaGiant ? 0 : 1,
                                                    enemy.Distance(GameVariables.MyCastle),
                                                    enemy is Elf ? 0 : 1,
                                                    enemy.CurrentHealth
                                                select enemy).FirstOrDefault();

                    // if we have an enemy to attack we attack it
                    if (enemyToAttack != null)
                        defender.Attack(enemyToAttack);
                }
                // if were not at the portal location we need to go there
                else
                {
                    defender.MoveTo(newPortalLocation);
                }

                return;
            }

            // these are the default locations and enemies
            MapObject locationToDefend = myPortal;
            GameObject enemyAttacker = GameVariables.EnemyLivingElves.OrderBy(attacker => attacker.Distance(myPortal))
                                             .FirstOrDefault();

            // if there are no enemy elves we seek to change the default values that were set above
            if (enemyAttacker is null)
            {
                LavaGiant giantToAttack = GameVariables
                                          .EnemyLivingLavaGiants
                                          .OrderBy(giant => giant.Distance(GameVariables.MyCastle)).FirstOrDefault();
                
                // if there are no lava giants we do nothing
                if (giantToAttack is null)
                    return;

                // else we move to attack it
                enemyAttacker = giantToAttack;
                locationToDefend = GameVariables.MyCastle;
            }

            // if we can attack the enemy, we attack it
            if (defender.InAttackRange(enemyAttacker))
                defender.Attack(enemyAttacker);
            // if we cant attack the enemy but he can attack the location were defending, we move towards him
            else if (enemyAttacker.InRange(locationToDefend, enemyAttacker.GetAttackRange()))
                defender.MoveTo(enemyAttacker);
            // if the attacker is not close to the location we defend, we position the defender in the enemy's path to the defended location
            else
            {
                Location defensivePosition = locationToDefend.GetLocation().Towards(enemyAttacker, enemyAttacker.GetAttackRange());
                defender.MoveTo(defensivePosition);
            }
        }
    }
}