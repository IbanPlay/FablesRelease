using NetEasy;
using Terraria.DataStructures;

namespace CalamityFables.Packets
{
    [Serializable]
    public class KillTileEntityPacket : Module
    {
        internal readonly short x;
        internal readonly short y;

        public KillTileEntityPacket(Point16 position)
        {
            x = position.X;
            y = position.Y;
        }

        protected override void Receive()
        {
            if (TileEntity.ByPosition.TryGetValue(new Point16(x, y), out var TE) && TE is ModTileEntity tileEntity)
                tileEntity.Kill(x, y);

            if (Main.netMode == NetmodeID.Server)
                Send(-1, -1, false);
        }
    }
}