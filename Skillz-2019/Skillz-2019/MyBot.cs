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

        #endregion
        public static setup(GameVariables game)
        {
            GameVariables.CurrentGame = game;
            GameVariables.MyCastle = game.GetMyCastle();
            GameVariables.EnemyCastle = game.GetEnemyCastle();
            GameVariables.EnemyLivingElves = game.GetEnemyLivingElves();
        }

        public static void UpdateCurrentGame(Game game)
        {
            setup(game);
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
            if (attacker is IceTroll && (attackedObject is Elf || attackedObject is IceTroll))
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
        }
    }
}