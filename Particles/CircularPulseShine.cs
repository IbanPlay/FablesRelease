namespace CalamityFables.Particles
{
    public class CircularPulseShine : Particle
    {
        public override string Texture => AssetDirectory.Particles + "StreakBloom";
        public override bool SetLifetime => true;
        public override bool UseCustomDraw => true;

        PrimitiveClosedLoop CirclePrim;

        public CircularPulseShine(Vector2 position, Color color, float scale = 1f)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Color = color;
            Lifetime = 20;
            Scale = scale;
        }

        public override void Update()
        {
            CirclePrim = CirclePrim ?? new PrimitiveClosedLoop(50, ChargeLoopWidthFunction, ChargeLoopColorFunction);
            CirclePrim.SetPositionsCircle(Position, (30f - 20f * (float)Math.Pow(1 - LifetimeCompletion, 2f)) * Scale);
        }

        internal float ChargeLoopWidthFunction(float completionRatio)
        {
            float baseWidth = 5f * (1 - LifetimeCompletion);  //Width tapers off at the end
            baseWidth *= (1 + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7) * 0.3f); //Width oscillates
            return baseWidth * Scale;
        }

        internal Color ChargeLoopColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.White, Color, LifetimeCompletion);
            return color;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Effect effect = AssetDirectory.PrimShaders.IntensifiedTextureMap;
            effect.Parameters["repeats"].SetValue(4);
            effect.Parameters["scroll"].SetValue(Main.GlobalTimeWrappedHourly * 3f);
            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            CirclePrim?.Render(effect, -basePosition);

            Texture2D lensFlare = ParticleTexture;
            Vector2 lensFlareScale = new Vector2(2f - 1.6f * (float)Math.Pow(LifetimeCompletion, 0.3f), 2f + 3f * LifetimeCompletion);
            Color lensFlareColor = Color.Lerp(Color, Color.White, (float)Math.Pow(1 - LifetimeCompletion, 3f));
            Main.EntitySpriteDraw(lensFlare, Position - basePosition, null, (lensFlareColor with { A = 120 }) * (1 - LifetimeCompletion), MathHelper.PiOver2, lensFlare.Size() / 2, lensFlareScale * Scale, 0, 0);
        }
    }
}
