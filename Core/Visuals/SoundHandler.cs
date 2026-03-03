using ReLogic.Utilities;
using Steamworks;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public class SoundHandler : ModSystem
    {
        public static Dictionary<SlotId, int> managedSounds;
        public static Dictionary<SlotId, int> managedSoundsWithFade;

        private static List<SlotId> soundsToClear;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            managedSounds = new Dictionary<SlotId, int>();
            managedSoundsWithFade = new Dictionary<SlotId, int>();
            soundsToClear = new List<SlotId>();

            Terraria.On_Main.UpdateAudio_DecideOnNewMusic += SpoofNewMusic;
            Terraria.On_Main.UpdateAudio += ClearTrackedSounds;

            calamityMusicMod = null;
            ModLoader.TryGetMod("CalamityModMusic", out calamityMusicMod);
        }


        public delegate void ModifyMusicChoiceDelegate();
        public static event ModifyMusicChoiceDelegate ModifyMusicChoiceEvent;

        private void SpoofNewMusic(Terraria.On_Main.orig_UpdateAudio_DecideOnNewMusic orig, Main self)
        {
            orig(self);
            ModifyMusicChoiceEvent?.Invoke();
        }

        public override void Unload()
        {
            managedSoundsWithFade = null;
            managedSounds = null;
            soundsToClear = null;
        }

        #region Get music
        internal static Mod calamityMusicMod = null;
        internal static bool UseCalamityMusic => !(calamityMusicMod is null) && CalCompatConfig.Instance.UseCalamityMusic;

        //Only there for the future in the case of splitting a music mod
        internal static bool UseVanillaMusic => false;

        public enum MusicSource
        {
            Fables,
            Calamity,
            Vanilla
        }
        internal static MusicSource UsedMusicSource => UseCalamityMusic ? MusicSource.Calamity : UseVanillaMusic ? MusicSource.Vanilla : MusicSource.Fables;



        public static int GetMusic(string musicName, int vanillaFallback) => GetMusic(musicName, musicName, vanillaFallback);

        public static int GetMusic(string musicName, string calamityName, int vanillaFallback)
        {
            MusicSource usedMusicSource = UsedMusicSource;
            if (usedMusicSource == MusicSource.Fables && musicName == "")
                usedMusicSource = MusicSource.Vanilla;

            switch (usedMusicSource)
            {
                case MusicSource.Calamity:
                    return MusicLoader.GetMusicSlot(calamityMusicMod, "Sounds/Music/" + calamityName);
                case MusicSource.Vanilla:
                    return vanillaFallback;
                default:
                    return MusicLoader.GetMusicSlot(CalamityFables.Instance, "Sounds/Music/" + musicName);
            }
        }
        #endregion

        #region Track sound effects to end them
        public static void TrackSound(SlotId sound, int refreshTime = 4)
        {
            if (!Main.dedServ)
                managedSounds[sound] = refreshTime;
        }

        public static void TrackSoundWithFade(SlotId sound, int refreshTime = 4)
        {
            if (!Main.dedServ)
                managedSoundsWithFade[sound] = 10 + refreshTime;
        }

        private void ClearTrackedSounds(Terraria.On_Main.orig_UpdateAudio orig, Main self)
        {
            if (SoundEngine.IsAudioSupported && !Main.dedServ)
            {

                soundsToClear.Clear();
                foreach (SlotId slotID in managedSounds.Keys)
                {
                    if (SoundEngine.TryGetActiveSound(slotID, out var sound))
                    {
                        if (!sound.IsPlaying)
                            soundsToClear.Add(slotID);

                        else
                        {
                            managedSounds[slotID]--;
                            if (managedSounds[slotID] < 0)
                            {
                                sound.Stop();
                                soundsToClear.Add(slotID);
                            }
                        }
                    }

                    else
                        soundsToClear.Add(slotID);
                }

                foreach (SlotId slotID in managedSoundsWithFade.Keys)
                {
                    if (SoundEngine.TryGetActiveSound(slotID, out var sound))
                    {
                        if (!sound.IsPlaying)
                            soundsToClear.Add(slotID);

                        else
                        {
                            managedSoundsWithFade[slotID]--;

                            if (managedSoundsWithFade[slotID] < 10)
                            {
                                sound.Volume *= 0.9f;
                                sound.Update();
                            }

                            if (managedSoundsWithFade[slotID] < 0)
                            {
                                sound.Stop();
                                soundsToClear.Add(slotID);
                            }
                        }
                    }

                    else
                        soundsToClear.Add(slotID);
                }


                managedSounds.RemoveAll(soudn => soundsToClear.Contains(soudn));
                managedSoundsWithFade.RemoveAll(soudn => soundsToClear.Contains(soudn));
            }

            orig(self);
        }
        #endregion


        /*
        /// <summary>
        /// Spoof of <see cref="SoundPlayer.Play(in SoundStyle, Vector2?, SoundUpdateCallback?)"/> that starts the sound with zero volume for fading in SFX
        /// </summary>
        /// <param name="style"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static SlotId PlaySoundQuietStart(in SoundStyle style, Vector2? position = null)
        {
            if (Main.dedServ)
                return SlotId.Invalid;
            if (position.HasValue && Vector2.DistanceSquared(Main.screenPosition + new Vector2(Main.screenWidth / 2, Main.screenHeight / 2), position.Value) > 100000000f)
                return SlotId.Invalid;
            if (style.PlayOnlyIfFocused && !Main.hasFocus)
                return SlotId.Invalid;

            if (!Program.IsMainThread)
            {
                var styleCopy = style;
                return Main.RunOnMainThread(() => Play_Inner(styleCopy, position, updateCallback)).GetAwaiter().GetResult();
            }

            return Play_Inner(in style, position, updateCallback);
        }

        /// <summary>
        /// Spoof of SoundPlayer.Play_Inner that starts the sound with zero volume for fading in SFX
        /// </summary>
        private SlotId Play_Inner(in SoundStyle style, Vector2? position, SoundUpdateCallback? updateCallback)
        {

            public ActiveSound(SoundStyle style, Vector2? position = null, SoundUpdateCallback? updateCallback = null)
            {
                Position = position;
                Volume = 1f;
                Pitch = style.PitchVariance;
                //IsGlobal = false;
                Style = style;
                Callback = updateCallback;

                Play();
            }

            ActiveSound value = new ActiveSound(style, position, updateCallback);
            return _trackedSounds.Add(value);
        }
        */
    }
}