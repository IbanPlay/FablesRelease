using NetEasy;

namespace CalamityFables.Packets
{
    [Serializable]
    public class DeactivateProjectilePacket : Module
    {
        internal readonly int identity;
        internal readonly byte whoAmI;
        internal readonly byte owner;

        public DeactivateProjectilePacket(Projectile projectile)
        {
            identity = projectile.identity;
            owner = (byte)projectile.owner;
            whoAmI = (byte)Main.myPlayer;
        }

        protected override void Receive()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].owner == owner && Main.projectile[i].identity == identity && Main.projectile[i].active)
                {
                    Main.projectile[i].active = false;
                    break;
                }
            }

            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
        }
    }
}