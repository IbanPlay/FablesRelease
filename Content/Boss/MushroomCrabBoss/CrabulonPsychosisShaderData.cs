using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public class CrabulonPsychosisShaderData : ScreenShaderData
    {
        public CrabulonPsychosisShaderData(Asset<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Update(GameTime gameTime)
        {
            UseIntensity(Crabulon.TrippyIntensity);
            UseOpacity(Crabulon.ShaderOpacity);

            //Taken from sepia dst screenshader
            float screenPositionInTiles = (Main.screenPosition.Y + Main.screenHeight / 2f) / 16f;
            //Calculates how much on the surface we are
            float surfaceValue = 1f - Utils.SmoothStep((float)Main.worldSurface, (float)Main.worldSurface + 30f, screenPositionInTiles);
            Vector2 midnightDirection = Utils.GetDayTimeAsDirectionIn24HClock(0f);

            //Use the dot product between clock hand directions to find if its night or not and have a smooth transition
            //Then multiply the "surface value" by it so that during the night , surface is 0 as if we were undeground
            surfaceValue *= 1 - Utils.SmoothStep(0.2f, 0.4f, Vector2.Dot(midnightDirection, Utils.GetDayTimeAsDirectionIn24HClock()));

            //Lower opacity when on surface at day
            UseProgress(1 - surfaceValue * 0.7f);
        }

        public override void Apply()
        {
            base.Apply();
        }
    }
}
