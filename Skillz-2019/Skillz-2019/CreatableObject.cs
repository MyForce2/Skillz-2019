namespace MyBot
{

    public static class CreatableObjectExtensions
    {

        public static int GetCost(this CreatableObject obj)
        {
            switch (obj)
            {
                case CreatableObject.Portal:
                    return GameState.Game.PortalCost;
                case CreatableObject.ManaFountain:
                    return GameState.Game.ManaFountainCost;
                case CreatableObject.IceTroll:
                    return GameState.Game.IceTrollCost;
                case CreatableObject.LavaGiant:
                    return GameState.Game.LavaGiantCost;
                case CreatableObject.SpeedUp:
                    return GameState.Game.SpeedUpCost;
                case CreatableObject.Invisibility:
                    return GameState.Game.InvisibilityCost;
                case CreatableObject.Tornado:
                    return GameState.Game.TornadoCost;
                default:
                    return 0;
            }
        }

    }

    public enum CreatableObject
    {
        Portal = 0,
        ManaFountain = 1,
        IceTroll = 2,
        LavaGiant = 3,
        SpeedUp = 4,
        Invisibility = 5,
        Tornado = 6
    }
}
