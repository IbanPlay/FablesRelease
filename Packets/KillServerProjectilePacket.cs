using NetEasy;

namespace CalamityFables.Packets
{
    [Serializable]
    public class KillServerProjectilePacket : Module
    {
        internal readonly int identity;
        internal readonly byte whoAmI;
        internal readonly Vector2 killPosition;

        public KillServerProjectilePacket(Projectile projectile)
        {
            identity = projectile.identity;
            whoAmI = (byte)Main.myPlayer;
            killPosition = projectile.Center;
        }

        protected override void Receive()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].owner == 255 && Main.projectile[i].identity == identity && Main.projectile[i].active)
                {
                    Main.projectile[i].Center = killPosition;
                    Main.projectile[i].Kill();
                    break;
                }
            }
        }
    }
}