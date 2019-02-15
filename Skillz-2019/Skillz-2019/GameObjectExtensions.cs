using ElfKingdom;

namespace MyBot
{
    public static class GameObjectExtensions
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
