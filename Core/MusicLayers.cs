using ReLogic.Utilities;
using NVorbis;
using System.Reflection;
using Microsoft.Xna.Framework.Audio;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace CalamityFables.Core
{
    public class MusicLayers : ModSystem
    {
        public static LegacyAudioSystem AudioSystem;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            AudioSystem = null;
            if (Main.audioSystem is LegacyAudioSystem audioSystem)
                AudioSystem = audioSystem;
            else
                return;

            layeredTrackLayers = new List<int>();
            LayerHandlers = new Dictionary<int, MusicLayerHandler>();

            IL_Main.UpdateAudio += AddLayeredAudio;
        }

        public override void PostSetupContent()
        {
            //TestMusicScene.testTrack = LoadMusicLayer(MusicLoader.GetMusicSlot(Mod, "Sounds/Music/TestBeat"), MusicLoader.GetMusicSlot(Mod, "Sounds/Music/TestBeatLayer"));
            //EOCMusicLayers.orchestraLayer = LoadMusicLayer(MusicLoader.GetMusicSlot(Mod, "Sounds/Music/Boss1Base"), MusicLoader.GetMusicSlot(Mod, "Sounds/Music/Boss1Orchestra"));

        }

        private void AddLayeredAudio(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            int activeLocalIndex = 0;
            int trackIndexIndex = 3;
            int volumeIndex = 4;
            int tempFadeIndex = 19;

            ILLabel breakLabel = null;

            //Go get the list of buttons
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>("audioSystem"),
                i => i.MatchLdloc(out activeLocalIndex),
                i => i.MatchLdloc(out trackIndexIndex),
                i => i.MatchLdloc(out volumeIndex),
                i => i.MatchLdloca(out tempFadeIndex),
                i => i.MatchCallvirt<IAudioSystem>("UpdateCommonTrack"),
                i => i.MatchBr(out breakLabel)
                ))
            {
                FablesUtils.LogILEpicFail("Add layered music", "Could not find the UpdateCommonTrack call");
                return;
            }

            cursor.MoveAfterLabels();

            ILLabel goToElseLabel = cursor.DefineLabel();
            goToElseLabel.Target = cursor.Next;

            cursor.Emit(OpCodes.Ldloc, trackIndexIndex);
            cursor.EmitDelegate(IsAudioTrackALayer);
            cursor.Emit(OpCodes.Brfalse, goToElseLabel);

            cursor.Emit(OpCodes.Ldloc, activeLocalIndex);
            cursor.Emit(OpCodes.Ldloc, trackIndexIndex);
            cursor.Emit(OpCodes.Ldloc, volumeIndex);
            cursor.Emit(OpCodes.Ldloca, tempFadeIndex);
            cursor.EmitDelegate(UpdateLayerTrack);
            cursor.Emit(OpCodes.Br, breakLabel);

        }

        public static bool IsAudioTrackALayer(int trackIndex) => layeredTrackLayers.Contains(trackIndex);

        /// <summary>
        /// All the music tracks that are extra layers for other tracks
        /// </summary>
        public static List<int> layeredTrackLayers;

        public static Dictionary<int, MusicLayerHandler> LayerHandlers;


        /// <summary>
        /// Registers a new music layer that plays ontop of a specific base track
        /// </summary>
        /// <param name="baseTrack"></param>
        /// <param name="layerTrack"></param>
        /// <returns></returns>
        public static MusicLayerHandler LoadMusicLayer(int baseTrack, int layerTrack)
        {
            if (AudioSystem == null)
                return null;

            MusicLayerHandler layer = new MusicLayerHandler(baseTrack, layerTrack);
            LayerHandlers.Add(layerTrack, layer);
            return layer;
        }

        /// <summary>
        /// Registers a new music track that plays in sync with another track. This track plays in silence when its parent track is active, and can then be played as a regular track otherwise
        /// </summary>
        /// <param name="baseTrack"></param>
        /// <param name="layerTrack"></param>
        /// <returns></returns>
        public static MusicLayerHandler LoadMusicSync(int baseTrack, int layerTrack)
        {
            if (AudioSystem == null)
                return null;

            MusicLayerHandler layer = new MusicSyncTransitionHandler(baseTrack, layerTrack);
            LayerHandlers.Add(layerTrack, layer);
            return layer;
        }


        public static void UpdateLayerTrack(bool gameActive, int i, float totalVolume, ref float tempFade)
        {
            if (AudioSystem == null || !AudioSystem.WaveBank.IsPrepared)
                return;

            MusicLayerHandler trackLayer = LayerHandlers[i];
            trackLayer.UpdateTrack(gameActive, i, totalVolume, ref tempFade);

            /*
            if (i == MusicLoader.GetMusicSlot(SoundDirectory.Music+ "TestBeatLayer") && totalVolume > 0)
                tracklayer.audioTrack.

                Main.NewText("Parent sample : " + tracklayer.parentReader.SamplePosition.ToString() + " - Layer sample : " + tracklayer.reader.SamplePosition.ToString() + " - Difference : " + (tracklayer.parentReader.SamplePosition - tracklayer.reader.SamplePosition).ToString());
            */
        }
    }

    public class MusicLayerHandler
    {
        /// <summary>
        /// Index of the layer's track
        /// </summary>
        public readonly int trackIndex;

        /// <summary>
        /// Index of the base music layer
        /// </summary>
        public readonly int parentIndex;

        /// <summary>
        /// The audio track associated with the parent
        /// </summary>
        public readonly OGGAudioTrack parentTrack;

        /// <summary>
        /// The audio track associated with this track
        /// </summary>
        public readonly OGGAudioTrack audioTrack;

        private readonly FieldInfo _vorbisReaderField = typeof(OGGAudioTrack).GetField("_vorbisReader", BindingFlags.Instance | BindingFlags.NonPublic);
        public readonly VorbisReader reader;
        public readonly VorbisReader parentReader;

        public long previousParentSamplePosition = 0L;

        /// <summary>
        /// The volume multiplier this track has over the base one
        /// </summary>
        public float Volume {
            get => _volume;
            set => _volume = MathHelper.Clamp(value, 0, 1);
        }

        private float _volume;

        /// <summary>
        /// The pitch this track is playing at
        /// </summary>
        public float Pitch {
            get => _pitch;
            set {
                if (_pitch != value)
                    _updatedPitchOrPan = true;
                _pitch = value;
            }
        }
        private float _pitch;

        /// <summary>
        /// The panning this track has
        /// </summary>
        public float Pan {
            get => _pan;
            set {
                if (_pan != value)
                    _updatedPitchOrPan = true;
                _pan = value;
            }
        }
        private float _pan;

        private bool _updatedPitchOrPan = false;

        public virtual void UpdateTrack(bool gameActive, int i, float totalVolume, ref float tempFade)
        {
            //Use the same fade as the parent
            tempFade = Main.musicFade[parentIndex];
            totalVolume *= Volume;

            //Only plays if parent track is playing
            bool wantsToPlay = !parentTrack.IsStopped;
            //wantsToPlay &= tracklayer.Volume > 0;

            if (wantsToPlay)
            {
                //Start the audio if we want the track to play and its not already playing
                if (!audioTrack.IsPlaying && gameActive)
                {
                    audioTrack.Reuse();
                    //Syncs its time with the parents time
                    SyncWithParent();

                    audioTrack.SetVariable("Volume", totalVolume);
                    UpdateTrackVariables(audioTrack);
                    audioTrack.Play();
                }

                //Update the track to use the variables we need
                else
                {
                    audioTrack.SetVariable("Volume", totalVolume);
                    UpdateTrackVariables(audioTrack);
                }
            }

            else
            {
                //Quiet itself instantly
                if (audioTrack.IsPlaying || !audioTrack.IsStopped)
                {
                    tempFade = 0;
                    audioTrack.SetVariable("Volume", 0f);
                    audioTrack.Stop(AudioStopOptions.Immediate);
                }
                else
                {
                    tempFade = 0f;
                }
            }
        }

        public void UpdateTrackVariables(IAudioTrack track)
        {
            if (_updatedPitchOrPan)
            {
                track.SetVariable("Pitch", Pitch);
                track.SetVariable("Pan", Pan);
                _updatedPitchOrPan = false;
            }
        }

        public void SyncWithParent()
        {
            reader.SamplePosition = parentReader.SamplePosition % reader.TotalSamples;
        }

        public MusicLayerHandler(int parentIndex, int layerIndex)
        {
            this.trackIndex = layerIndex;
            this.parentIndex = parentIndex;

            audioTrack = MusicLayers.AudioSystem.AudioTracks[layerIndex] as OGGAudioTrack;
            parentTrack = MusicLayers.AudioSystem.AudioTracks[parentIndex] as OGGAudioTrack;

            if (_vorbisReaderField != null)
            {
                reader = (VorbisReader)_vorbisReaderField.GetValue(audioTrack);
                parentReader = (VorbisReader)_vorbisReaderField.GetValue(parentTrack);
            }

            MusicLayers.layeredTrackLayers.Add(layerIndex);
        }
    }

    public class MusicSyncTransitionHandler : MusicLayerHandler
    {
        public MusicSyncTransitionHandler(int parentIndex, int layerIndex) : base(parentIndex, layerIndex)
        {
        }

        public override void UpdateTrack(bool gameActive, int i, float totalVolume, ref float tempFade)
        {
            bool forceShouldPlay = !parentTrack.IsStopped;

            //Force the track to play if the parent track is playing
            if (forceShouldPlay)
            {
                if (!audioTrack.IsPaused && parentTrack.IsPaused)
                    audioTrack.Pause();
                else if (audioTrack.IsPaused && !parentTrack.IsPaused)
                    audioTrack.Resume();

                tempFade -= 0.04f;
			    if (tempFade <= 0f)
				    tempFade = 0f;

                //Start the audio if we want the track to play and its not already playing
                if (!audioTrack.IsPlaying && gameActive)
                {
                    audioTrack.Reuse();
                    SyncWithParent();
                    audioTrack.SetVariable("Volume", totalVolume);
                    audioTrack.Play();
                }

                //Update the track to use the variables we need
                else
                {
                    audioTrack.SetVariable("Volume", totalVolume);
                }
            }

            //Fade out otherwise. The regular play is handled by main already
            else
                Main.audioSystem.UpdateCommonTrackTowardStopping(trackIndex, totalVolume, ref tempFade, Main.musicFade[Main.curMusic] > 0.25f);
        }
    }

    /*
    public class TestMusicScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/TestBeat");

        public override bool IsSceneEffectActive(Player player)
        {
            return !Main.mouseRight;
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            testTrack.Volume = Utils.GetLerpValue(120f, 700f, Main.LocalPlayer.Distance(Main.MouseWorld), true);
        }
        public static MusicLayerHandler testTrack;
    }


    public class EOCMusicLayers : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossLow;

        public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/Boss1Base");

        public static bool orchestraLayerActive;
        public static float orchestraLayerVolume;

        public override bool IsSceneEffectActive(Player player)
        {
            int padding = 5000;
            Rectangle screenRectangle = new Rectangle((int)Main.screenPosition.X - padding, (int)Main.screenPosition.Y - padding, Main.screenWidth + 2 * padding, Main.screenHeight + 2 * padding);

            bool foundEoc = false;
            orchestraLayerActive = false;

            for (int i = 0; i < 200; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.type != NPCID.EyeofCthulhu)
                    continue;

                if (screenRectangle.Contains((int)npc.Center.X, (int)npc.Center.Y))
                {
                    foundEoc = true;
                    if (npc.ai[0] > 0)
                        orchestraLayerActive = true;
                }
            }

            return foundEoc;
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            if (isActive && orchestraLayerActive)
                orchestraLayer.Volume += 0.008f;
            else
                orchestraLayer.Volume -= 0.01f;

        }

        public static MusicLayerHandler orchestraLayer;
    }
    */
}
