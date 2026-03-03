using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public class ScreenDesaturation : ModSystem
    {
        private static int framesToLast;
        private static int framesLasted;

        private static EasingFunction easingType;
        private static float easingPower = 1f;

        private static float desaturation;

        public static float desaturationOverride = 0f;

        public override void PostUpdateEverything()
        {
            if (Main.dedServ)
                return;

            if (framesToLast > 0 && framesLasted > framesToLast)
                framesToLast = 0;

            if (framesToLast == 0 && desaturationOverride <= 0f)
            {
                if (Scene["ScreenDesaturation"].IsActive())
                    Scene["ScreenDesaturation"].Deactivate();
                return;
            }

            Effect shader = Scene["ScreenDesaturation"].GetShader().Shader;
            float desat = desaturationOverride;
            if (desaturationOverride == 0)
            {
                desat = easingType(1 - (framesLasted / (float)framesToLast), easingPower) * desaturation;
            }
            shader.Parameters["desatPercent"].SetValue(desat);


            if (Main.netMode != NetmodeID.Server && !Scene["ScreenDesaturation"].IsActive())
            {
                Scene.Activate("ScreenDesaturation").GetShader().UseProgress(0f).UseColor(Color.White.ToVector3());
            }

            framesLasted++;
            desaturationOverride = 0f;
        }

        public static void SetDesaturation(int lenght, float desaturationStrenght, EasingFunction easing = null, float easingStrenght = 1f, bool overridePrevious = false)
        {
            if (!overridePrevious && framesToLast > 0)
                return;

            framesLasted = 0;
            framesToLast = lenght;
            desaturation = desaturationStrenght;
            easingType = easing;
            if (easingType == null)
                easingType = LinearEasing;
            easingPower = easingStrenght;
        }
    }
}