using Terraria.DataStructures;
using Terraria.ModLoader.Utilities;
using System.Linq;

namespace CalamityFables.Content.NPCs.Wulfrum
{
    public class WulfrumBunkerRaidScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.Event;

        public static readonly int NexusMusic = MusicLoader.GetMusicSlot(SoundDirectory.Music + "Nexus");
        public static int NexusType;

        private static int PreviousMusicBox = 0;

        public override int Music => NexusMusic;



        public override bool IsSceneEffectActive(Player player)
        {
            bool disabledBoombox = false;

            //No fade in for the boombox music, so it booms instantly
            if (Main.SceneMetrics.ActiveMusicBox == NexusMusic && Main.curMusic == NexusMusic)
            {
                Main.musicFade[NexusMusic] = 1f;

                //Tone down all other music tracks
                if (PreviousMusicBox != NexusMusic)
                {
                    for (int i = 0; i < Main.maxMusic; i++)
                    {
                        if (i != NexusMusic)
                            Main.musicFade[i] *= 0.4f;
                    }
                }
            }
            else if (PreviousMusicBox == NexusMusic)
                disabledBoombox = true;

            PreviousMusicBox = Main.SceneMetrics.ActiveMusicBox;

            foreach (var npc in Main.ActiveNPCs)
            {
                if (npc.type != NexusType)
                    continue;
                //Ignore non bunker nexuses
                if (npc.ai[2] == 0 && npc.ai[3] == 0)
                    continue;
                return true;
            }

            if (disabledBoombox)
                Main.musicFade[NexusMusic] *= 0.3f;

            return false;
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
        }
    }
}
