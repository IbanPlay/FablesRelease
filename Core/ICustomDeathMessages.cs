using Terraria.DataStructures;

namespace CalamityFables.Core
{
    public interface ICustomDeathMessages
    {
        /// <summary>
        /// The priority this death message has over any other death messages. Used when the player dies with multiple DOT effects
        /// </summary>
        public float DoTDeathMessagePriority => 1;

        public bool CustomDeathMessage(Player player, ref PlayerDeathReason deathMessage)
        {
            return false;
        }
    }
}
