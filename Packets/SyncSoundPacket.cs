namespace CalamityFables.Packets
{
    [Serializable]
    public abstract class SyncSoundPacket : Module
    {
        private readonly int sender =-1;
        public abstract SoundStyle SyncedSound { get; }
        private Vector2 Position;
        private float Volume;
        private float Pitch;

        public SyncSoundPacket(Vector2 pos, float vol = 1f, float pitch = 0)
        {
            sender = Main.myPlayer;
            Position = pos;
            Volume = vol;
            Pitch = pitch;

        }

        protected override void Receive()
        {
            if (Main.netMode == NetmodeID.Server)
                Send(-1, sender, false);
            else
                SoundEngine.PlaySound(SyncedSound with { Volume = SyncedSound.Volume * Volume, Pitch = Pitch }, Position);
        }
    }
}