using NetEasy;

namespace CalamityFables.Packets
{
    [Serializable]
    public class KnockbackPlayerPacket : Module
    {
        private readonly int player;
        private Vector2 knockback;

        public KnockbackPlayerPacket(int player, Vector2 knockback)
        {
            this.player = player;
            this.knockback = knockback;
        }

        protected override void Receive()
        {
            Main.player[player].velocity += knockback;
            Main.player[player].fallStart = (int)(Main.player[player].Bottom.Y / 16);
            if (Main.netMode == NetmodeID.Server)
                Send( -1, -1, false);
        }
    }
}