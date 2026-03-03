using NetEasy;

namespace CalamityFables.Packets
{
    [Serializable]
    public class RequestServerTeleportationPacket : Module
    {
        internal readonly int playerToTeleport;
        internal readonly Vector2 position; 
        internal readonly int style;

        public RequestServerTeleportationPacket(Player player, Vector2 position, int style)
        {
            this.playerToTeleport = player.whoAmI;
            this.position = position;
            this.style = style;
        }

        protected override void Receive()
        {
            Main.player[playerToTeleport].Teleport(position, style);
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, playerToTeleport, position.X, position.Y, style);
        }
    }
}