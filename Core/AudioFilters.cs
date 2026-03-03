using CalamityFables.Content.Boss.SeaKnightMiniboss;
using Microsoft.Xna.Framework.Audio;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json.Linq;
using NVorbis;
using ReLogic.Content.Sources;
using ReLogic.Utilities;
using System.IO;
using System.Reflection;

namespace CalamityFables.Core
{
    /*
    public class AudioFilters : ILoadable
    {
        public static LegacyAudioSystem AudioSystem;

        public Dictionary<OGGAudioTrack, OGGFilter> filteredTracks = new Dictionary<OGGAudioTrack, OGGFilter>();

        public void Load(Mod mod)
        {
            if (Main.dedServ)
                return;
            FablesGeneralSystemHooks.PostSetupContentEvent += SetupFilteredMusic;
            FablesGeneralSystemHooks.PostUpdateEverythingEvent += EditFilters;
        }

        private void SetupFilteredMusic()
        {
            AudioSystem = null;
            if (Main.audioSystem is LegacyAudioSystem audioSystem)
                AudioSystem = audioSystem;
            else
                return;

            filteredTracks.Clear();

            for (int i = 0; i < AudioSystem.AudioTracks.Length; i++)
            {
                IAudioTrack track = AudioSystem.AudioTracks[i];
                if (track is OGGAudioTrack oggTrack)
                {
                    OGGFilter filter = new OGGFilter(oggTrack);
                    filteredTracks.Add(oggTrack, filter);
                }
            }

            foreach (OGGFilter track in filteredTracks.Values)
            {
                track.SetLowPassFilter(0.5f);
            }

            On_OGGAudioTrack.PrepareBufferToSubmit += EditBuffer;
        }

        public void Unload() { }

        private void EditBuffer(On_OGGAudioTrack.orig_PrepareBufferToSubmit orig, OGGAudioTrack self)
        {
            orig(self);

            if (filteredTracks[self] != null)
                filteredTracks[self].EditBuffer();
        }

        private void EditFilters()
        {
            foreach (OGGFilter track in filteredTracks.Values)
            {
                track.SetLowPassFilter(820);
            }
        }
    }


    public class OGGFilter
    {
        public OGGAudioTrack parent;
        private byte[] _bufferToSubmit;

        public float lowPassValue = 1f;

        private byte lastInputValue = 0;
        private float lastOutputValue = 0;
        private int sampleRate;

        private int sampleCount = 0;

        public OGGFilter(OGGAudioTrack parent)
        {
            this.parent = parent;
            FieldInfo fieldInfo = typeof(ASoundEffectBasedAudioTrack).GetField("_sampleRate", BindingFlags.Instance | BindingFlags.NonPublic);
            sampleRate = (int)fieldInfo.GetValue(parent);

            fieldInfo = typeof(ASoundEffectBasedAudioTrack).GetField("_bufferToSubmit", BindingFlags.Instance | BindingFlags.NonPublic);
            _bufferToSubmit = (byte[])fieldInfo.GetValue(parent);
        }

        public byte lastBufferValue = 0;
        
        public void SetLowPassFilter(float frequency)
        {
            float freq = 20 + 20000 * frequency;
            lowPassValue = MathF.Tan(MathHelper.Pi * freq / sampleRate);
        }
        
        public void EditBuffer()
        {
            int waveLength = 1000 + (int)(MathF.Sin(Main.GlobalTimeWrappedHourly) * 500) + (int)(MathF.Sin(Main.GlobalTimeWrappedHourly * 4f) * 200);

            float effectIntensity = 0f;


            int naut = NPC.FindFirstNPC(ModContent.NPCType<SirNautilusPassive>());

            if (naut != -1)
            {
                NPC nautilus = Main.npc[naut];
                effectIntensity = Utils.GetLerpValue(300f, 60f, Main.LocalPlayer.Distance(nautilus.Center), true);
            }

            for (int i = 0; i < _bufferToSubmit.Length; i++)
            {
                float waveValue = 128f + 10 * MathF.Sin((sampleCount + i) / 400f);
                float newValue = MathHelper.Lerp(_bufferToSubmit[i], waveValue, effectIntensity);

                _bufferToSubmit[i] = (byte)newValue;
                continue;

            }

            sampleCount += _bufferToSubmit.Length;
        }
    }
    */
}
