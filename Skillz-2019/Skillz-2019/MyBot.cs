using System.Collections.Generic;
using System.Linq;
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
            ((GameObject[])GameVariables.EnemyLivingElves).Concat(GameVariables.CurrentGame.GetEnemyCreatures());

        #endregion

        /// <summary>
        ///     Updates the current game variables with the most recent ones
        /// </summary>
        /// <param name="game"></param>
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
            if (obj is Creature creature)
                return creature.AttackRange;
            if (obj is Elf elf)
                return elf.AttackRange;
            return 0;
        }

        /// <summary>
        ///     Returns a list of the enemies in the given range of this game object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static List<GameObject> GetEnemiesInRange(this GameObject obj, int range)
        {
            return (from enemy in GameVariables.AllLivingEnemies
                    where obj.InRange(enemy, range)
                    select enemy).ToList();
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
            if (obj is Elf elf)
                return elf.AttackRange;
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

        /// <summary>
        ///     returns the distance of the attacker to a position he can attack the attacked object from
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="attackedMapObject"></param>
        /// <returns></returns>
        public static int DistanceToAttackPosition(this GameObject attacker, MapObject attackedMapObject) =>
            attacker.Distance(attackedMapObject.GetLocation().Towards(attacker, attacker.GetAttackRange()));

        /// <summary>
        ///     returns the number of turns it takes to travel the given distance
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static int TurnsToReach(this GameObject obj, int distance)
        {
            int speed = obj.GetMaxSpeed();
            if (distance % speed == 0)
                return distance / speed;

            // we account for the remainder with the + 1
            return distance / speed + 1;
        }

        /// <summary>
        ///     returns the amount of damage the lava giant can do to the castle if he is left alone.
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="attackedCastle"></param>
        /// <returns></returns>
        public static int PossibleDamageTo(this LavaGiant attacker, Castle attackedCastle)
        {
            int distanceToTarget = attacker.DistanceToAttackPosition(attackedCastle);
            int turnsToReach = attacker.TurnsToReach(distanceToTarget);

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
            int distanceToTarget =
                attacker.DistanceToAttackPosition(attackedObject);
            int turnsToReach = attacker.TurnsToReach(distanceToTarget);

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
        /// <summary>
        ///     The method that performs the current turn for our bot.
        ///     We act by the strategy that lava giants can't be stopped from reaching
        ///     and hurting the castle so we must only summon lava giants and attempt to create more
        ///     lava giants then the enemy in order to win.
        ///     We do this by defending our portals and destroying the enemy portals.
        /// </summary>
        /// <param name="game"></param>
        public void DoTurn(Game game)
        {
            try
            {
                GameVariables.UpdateCurrentGame(game);

                // perform actions with the elves
                AttackWith(GameVariables.AttackingElf);

                // if our portal still stands we try to summon lava giants
                Portal myPortal = GameVariables.MyPortals.FirstOrDefault();
                if (myPortal != null && myPortal.CanSummonLavaGiant()) myPortal.SummonLavaGiant();
            }
            catch (System.Exception)
            { } // make sure the bot doesn't crash
        }

        /// <summary>
        ///     performs the attacking actions with the attacker elf
        /// </summary>
        /// <param name="attacker"></param>
        public void AttackWith(Elf attacker)
        {
            if (attacker == null)
                return;

            GameObject destination = null;
            if (GameVariables.EnemyPortals.Length != 0)
            {
                if (GameVariables.EnemyPortals.Length == 1)
                    destination = GameVariables.EnemyPortals[0];
                else
                {
                    var enemiesNearPortals = new Dictionary<Portal, int>();
                    Portal nearestPortal = GameVariables.EnemyPortals.OrderBy(portal => portal.Distance(attacker)).First();
                    foreach (Portal p in GameVariables.EnemyPortals)
                    {
                        enemiesNearPortals[p] = p.GetEnemiesInRange(attacker.AttackRange).Count;
                    }

                    Portal safestPortal = enemiesNearPortals.Keys.OrderBy(portal => enemiesNearPortals[portal]).First();
                    if (safestPortal != nearestPortal &&
                        enemiesNearPortals[safestPortal] != enemiesNearPortals[nearestPortal])
                        destination = safestPortal;
                }
            }
            else
                destination = GameVariables.EnemyCastle;

            if (attacker.InAttackRange(destination))
                attacker.Attack(destination);
            else
                attacker.MoveTo(destination);
        }
    }
}
