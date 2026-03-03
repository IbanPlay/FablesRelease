using Terraria.Graphics.CameraModifiers;

namespace CalamityFables.Core
{
    public class CameraManager : ModSystem
    {
        public static float Shake;
        public static float Quake;
        public static CameraPanMagnet PanMagnet = new CameraPanMagnet();
        public static LockOnCameraModifier LockOn = new LockOnCameraModifier();

        public static bool PreviousUIVisibility;
        public static bool UIHidden;
        public static int UIRubberbandSafetyTimer;

        #region Camera lock on caching
        public static Main.InfoToSetBackColor cachedInfo;

        public override void Load()
        {
            On_Main.SetBackColor += LockBackColor;
            FablesPlayer.ImmuneToEvent += ImmuneWhenInCutscene;
            FablesPlayer.NaturalLifeRegenEvent += StopRegenInCutscene;
        }

        public delegate bool ImmunityExceptionDelegate(Player player, Terraria.DataStructures.PlayerDeathReason damageSource);

        /// <summary>
        /// Can be used to ignore cutscene immunity from certain damage sources. <br/>
        /// Return true to ignore cutscene immunity.
        /// </summary>
        public static event ImmunityExceptionDelegate ImmunityExceptionEvent;
        private bool ImmuneWhenInCutscene(Player player, Terraria.DataStructures.PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
        {
            // Check for immunity exception using event
            bool exception = false;
            if (ImmunityExceptionEvent != null)
                foreach (ImmunityExceptionDelegate immunityCheck in ImmunityExceptionEvent.GetInvocationList())
                    if (ImmunityExceptionEvent(player, damageSource))
                    {
                        exception = true;
                        break;
                    }

            if (CutsceneImmunity(player) && !exception)
                return true;
            return false;
        }

        private void StopRegenInCutscene(Player player, ref float regen)
        {
            if (CutsceneImmunity(player))
            {
                regen *= 0f;
                player.lifeRegen = 0;
            }
        }


        public static bool CutsceneImmunity(Player player) => PanMagnet.cutsceneImmunity[player.whoAmI] > 0;

        private void LockBackColor(On_Main.orig_SetBackColor orig, Main.InfoToSetBackColor info, out Color sunColor, out Color moonColor)
        {
            //Keep the variables locked if theres a lock on
            if (LockOn.timer > 0)
            {
                info.CorruptionBiomeInfluence = cachedInfo.CorruptionBiomeInfluence;
                info.CrimsonBiomeInfluence = cachedInfo.CrimsonBiomeInfluence;
                info.GraveyardInfluence = cachedInfo.GraveyardInfluence;
                info.JungleBiomeInfluence = cachedInfo.JungleBiomeInfluence;
                info.MushroomBiomeInfluence = cachedInfo.MushroomBiomeInfluence;
            }

            orig(info, out sunColor, out moonColor);

            //Cache the info if theres no lock on
            if (LockOn.timer == 0)
                cachedInfo = info;
        }
        #endregion

        public override void ModifyScreenPosition()
        {
            float mult = 1.125f; //Used to be: Main.screenWidth / 2048f * 1.2f. Keeping the value that resulted from my screen width.
            mult *= FablesConfig.Instance.ScreenshakeMultiplier;

            Main.instance.CameraModifiers.Add(PanMagnet);
            Main.instance.CameraModifiers.Add(new ShakeCamera(Shake * mult, 2, "Fabled Shake"));
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(Main.LocalPlayer.position, Main.rand.NextFloat(3.14f).ToRotationVector2(), Quake * mult, 15f, 30, 2000, "Fabled Quake"));
            Main.instance.CameraModifiers.Add(LockOn);

            if (Shake > 0)
                Shake = Math.Max(Shake - 1, 0);
            if (Quake > 0)
                Quake = Math.Max(Quake - 1, 0);

            if (float.IsNaN(Quake))
                Quake = 0;
        }

        /// <summary>
        /// Shorthand for Main.instance.CameraModifiers.Add()
        /// </summary>
        /// <param name="modifier"></param>
        public static void AddCameraEffect(ICameraModifier modifier) => Main.instance.CameraModifiers.Add(modifier);

        public override void PostUpdateEverything()
        {
            PanMagnet = PanMagnet ?? new CameraPanMagnet();
            LockOn = LockOn ?? new LockOnCameraModifier();

            //Smoothly tick down the camera magnet
            if (!PanMagnet.inUse && PanMagnet.PanProgress > 0)
            {
                PanMagnet.PanProgress -= 1 / (60f * 0.5f);
                if (PanMagnet.PanProgress <= 0)
                {
                    PanMagnet.Reset();
                }
            }
            PanMagnet.inUse = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (PanMagnet.cutsceneImmunity[i] > 0)
                    PanMagnet.cutsceneImmunity[i]--;
            }
            PanMagnet.currentHighestPriority = 0f;

            if (LockOn.timer > 0)
            {
                LockOn.timer--;
                if (LockOn.timer == 0)
                    LockOn.Reset();
            }

            if (UIHidden)
            {
                UIRubberbandSafetyTimer--;
                if (UIRubberbandSafetyTimer <= 0)
                    UnHideUI();
            }
        }

        public static void HideUI(int rubberbandSafety = 120)
        {
            if (!UIHidden)
                PreviousUIVisibility = Main.hideUI;

            Main.hideUI = true;
            UIHidden = true;

            UIRubberbandSafetyTimer = rubberbandSafety;
        }

        public static void UnHideUI()
        {
            Main.hideUI = PreviousUIVisibility;
            UIHidden = false;
        }
    }

}