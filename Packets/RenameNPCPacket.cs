using NetEasy;

namespace CalamityFables.Packets
{
    [Serializable]
    public class RenameNPCPacket : Module, IPatientPacket
    {
        internal readonly int npc;
        internal readonly int npcType;
        private readonly string newName;

        public bool CanApply => Main.npc[npc].active && Main.npc[npc].type == npcType;

        //Max 1 second
        private int _timeLeft = 60 * 5;
        public int TimeLeft
        {
            get => _timeLeft;
            set => _timeLeft = value;
        }

        public RenameNPCPacket(NPC npc)
        {
            this.npc = npc.whoAmI;
            npcType = npc.type;
            newName = npc.GivenName;
        }

        public void ReceivePublic() => Receive();

        protected override void Receive()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && !CanApply)
            {
                PacketWaitingList.AddToWaitingList(this);
                return;
            }

            Main.npc[npc].GivenName = newName;
            if (Main.netMode == NetmodeID.Server)
                Send(-1, -1, false);
        }
    }
}