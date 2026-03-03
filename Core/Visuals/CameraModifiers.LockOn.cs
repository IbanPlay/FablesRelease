using Terraria.Graphics.CameraModifiers;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public class LockOnCameraModifier : ICameraModifier
    {
        public string UniqueIdentity => "Fables Camera Lock";
        public bool Finished => false; //The effect never ends

        public float currentHighestPriority = 0f;
        public Vector2 lockOnPosition;
        public int timer = 0;
        public int fadeTimer = 0;
        public bool furtherThanAScreenAway;
        public bool onlyFadeIfTooFar = true;

        private Vector2 normalCameraPosition;

        public void SetLockPosition(Vector2 lockPosition, int duration, int fadeTimer = 0, float priority = 1f)
        {
            if (currentHighestPriority < priority)
            {
                lockOnPosition = lockPosition;
                currentHighestPriority = priority;
                timer = duration;
                this.fadeTimer = fadeTimer;
            }
        }

        public void Update(ref CameraInfo cameraInfo)
        {
            if (lockOnPosition == default || timer <= 0)
                return;

            //Prevents the background from changing
            Main.bgDelay = 0;
            Main.instantBGTransitionCounter = 0;
            Vector2 halfScreen = new Vector2(-Main.screenWidth / 2f, -Main.screenHeight / 2f);

            //Cache the normal camera position, and if the camera is further than a screen away
            normalCameraPosition = cameraInfo.CameraPosition;
            
            Vector2 difference = lockOnPosition - (normalCameraPosition - halfScreen);
            if ((Math.Abs(difference.X) > Main.screenWidth / 2f || Math.Abs(difference.Y) > Main.screenHeight / 2f) && difference.Length() >= new Vector2(Main.screenWidth, Main.screenHeight).Length() / 2f + 100f)
                furtherThanAScreenAway = true;
            else
                furtherThanAScreenAway = false;

            onlyFadeIfTooFar = true;

            cameraInfo.CameraPosition = lockOnPosition + halfScreen;
            if (fadeTimer > 0 && timer < fadeTimer && (!onlyFadeIfTooFar || furtherThanAScreenAway))
                VignetteFadeEffects.fadeOpacityOverride = Utils.GetLerpValue(fadeTimer, 0f, timer, true);
        }

        public void Reset()
        {
            Vector2 difference = lockOnPosition - (normalCameraPosition - new Vector2(-Main.screenWidth / 2f, -Main.screenHeight / 2f));

            //Taken from Main, stuff that happens when the camera instantly snaps back
            if (furtherThanAScreenAway)
            {
                Main.renderNow = true;
                NPC.ResetNetOffsets();
                Main.maxQ = true;
                Main.instantBGTransitionCounter = 10;
                Main.LocalPlayer.ForceUpdateBiomes();
            }
            else if (fadeTimer <= 0 || onlyFadeIfTooFar)
                CameraManager.AddCameraEffect(new SlideBackCameraModifier(difference));

            currentHighestPriority = 0f;
            lockOnPosition = default;
            timer = 0;

            if (fadeTimer > 0 && (!onlyFadeIfTooFar || furtherThanAScreenAway))
            {
                VignetteFadeEffects.AddFadeEffect(new VignettePunchModifier(fadeTimer, 1f, PolyOutEasing) { easingPower = 2f});
                fadeTimer = 0;
            }
        }
    }

    public class SlideBackCameraModifier : ICameraModifier
    {
        public string UniqueIdentity => "Fables Camera Slide Back";

        public bool Finished { get; set; }

        public int timer = 0;
        public Vector2 offset;

        public SlideBackCameraModifier(Vector2 offset)
        {
            this.offset = offset;
            timer = 50;
        }

        public void Update(ref CameraInfo cameraInfo)
        {
            cameraInfo.CameraPosition += offset * PolyInOutEasing(timer / 50f);
            timer--;
            if (timer <= 0)
                Finished = true;
        }
    }
}