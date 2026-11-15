using System;

namespace ArtemisX.Core
{
    /// <summary>An amount of one resource, used for costs, rewards and daily outputs in configs.</summary>
    [Serializable]
    public struct ResourceAmount
    {
        public ResourceType resource;
        public int amount;

        public ResourceAmount(ResourceType resource, int amount)
        {
            this.resource = resource;
            this.amount = amount;
        }
    }
}
