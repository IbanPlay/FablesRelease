using NetEasy;

namespace CalamityFables.Packets
{
    [Serializable]
    public class KnockbackNPCPacket : Module
    {
        private readonly int npc;
        private Vector2 knockback;

        public KnockbackNPCPacket(int npc, Vector2 knockback)
        {
            this.npc = npc;
            this.knockback = knockback;
        }

        protected override void Receive()
        {
            Main.npc[npc].velocity += knockback;
            if (Main.netMode == Terraria.ID.NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc);
        }
    }
}