using NetEasy;

namespace CalamityFables.Packets
{
    [Serializable]
    public class NPCFromStatuePacket : Module, IPatientPacket
    {
        private readonly int npc;
        internal readonly int npcType;

        public bool CanApply => Main.npc[npc].active && Main.npc[npc].type == npcType;

        //Max 1 second
        private int _timeLeft = 60 * 5;
        public int TimeLeft
        {
            get => _timeLeft;
            set => _timeLeft = value;
        }

        public NPCFromStatuePacket(NPC npc)
        {
            this.npc = npc.whoAmI;
            npcType = npc.type;
        }

        public void ReceivePublic() => Receive();
        protected override void Receive()
        {
            Main.npc[npc].SpawnedFromStatue = true;
        }
    }
}