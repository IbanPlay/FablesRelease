using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    /*
    public class FreezeFrames : ModSystem
    {
        public override void Load()
        {
           // On_Main.DoUpdate += FreezeGame;
        }


        public static int frozenFrames = 0;
        public static Action unfreezeEffect;

        private void FreezeGame(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
        {
            if (Main.netMode == 0 && frozenFrames > 0)
            {
                frozenFrames--;
                if (frozenFrames == 0 && unfreezeEffect != null)
                {
                    unfreezeEffect();
                    unfreezeEffect = null;
                }
            }
            else
                orig(self, ref gameTime);
        }

        public static void FreezeScreen(int duration, Action unfreezeEffect = null)
        {
            frozenFrames = Math.Max(frozenFrames, duration);
            FreezeFrames.unfreezeEffect = unfreezeEffect;
        }
    }
    */
}