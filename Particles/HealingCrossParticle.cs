namespace CalamityFables.Particles
{
    public class HealingCrossParticle : Particle
    {
        public override string Texture => AssetDirectory.Particles + "HealingCross";
        public static Asset<Texture2D> BloomAsset;

        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public Color bloomColor;
        public float random;

        public HealingCrossParticle(Vector2 position, Vector2 velocity, Color color, Color bloomColor, float scale, int lifeTime)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            this.bloomColor = bloomColor;
            Scale = scale;
            Lifetime = lifeTime;

            random = Main.rand.NextFloat();
        }

        public override void Update()
        {
            Velocity.Y -= 0.02f;
            Velocity.X += MathF.Sin(Main.GlobalTimeWrappedHourly + random * MathHelper.TwoPi) * 0.02f;
            Velocity *= 0.98f;

            Scale *= 0.99f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D dustTexture = ParticleTexture;
            BloomAsset ??= ModContent.Request<Texture2D>(Texture + "Bloom");
            Texture2D bloom = BloomAsset.Value;

            Vector2 origin = dustTexture.Size() / 2f;

            float scale = Scale;
            scale *= (float)Math.Pow(Utils.GetLerpValue(0f, 0.1f, LifetimeCompletion, true) * Utils.GetLerpValue(1f, 0.8f, LifetimeCompletion, true), 0.5f);

            spriteBatch.Draw(bloom, Position - basePosition, null, bloomColor with { A = 0 }, Rotation, origin, scale, SpriteEffects.None, 0);
            spriteBatch.Draw(dustTexture, Position - basePosition, null, Color with { A = 0 }, Rotation, origin, scale, SpriteEffects.None, 0);
        }
    }
}
