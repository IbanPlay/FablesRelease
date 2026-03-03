using NetEasy;

namespace CalamityFables.Packets
{
    /// <summary>
    /// Packet that waits for a condition to be fulfilled client side before running, if recieved too early. 
    /// </summary>
    public interface IPatientPacket
    {
        /// <summary>
        /// Time left until the packet is considered expired and no longer valid
        /// </summary>
        public int TimeLeft { get; set; }

        /// <summary>
        /// if the packet can be received properly
        /// </summary>
        public bool CanApply { get; }

        /// <summary>
        /// Accessor for the protected receive methos
        /// </summary>
        public void ReceivePublic();
    }

    /// <summary>
    /// Keeps track of packets that have been received before their conditions have been fulfilled
    /// </summary>
    public class PacketWaitingList : ILoadable
    {
        private static readonly List<IPatientPacket> _waitingList = new();

        public void Load(Mod mod)
        {
            _waitingList.Clear();
            FablesGeneralSystemHooks.PostUpdateNPCEvent += TryApplyQueuedNPCPackets;

        }

        public static void AddToWaitingList(IPatientPacket packet) => _waitingList.Add(packet);

        private void TryApplyQueuedNPCPackets()
        {
            for (int i = _waitingList.Count - 1; i >= 0; i--)
            {
                foreach (IPatientPacket packet in _waitingList)
                {
                    packet.TimeLeft--;
                    if (packet.TimeLeft < 0)
                        _waitingList.RemoveAt(i);
                    else if (packet.CanApply)
                    {
                        packet.ReceivePublic();
                        _waitingList.RemoveAt(i);
                    }
                }
            }
        }

        public void Unload() { }


    }
}