namespace CalamityFables.Particles
{
    public class BlastStreak : Particle
    {
        public override string Texture => AssetDirectory.Particles + "StreakBloom";
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public Vector2 Direction;
        public float MaxRadius;
        private float opacity;
        private Color Bloom;
        private Color ColorStart;
        private Color ColorEnd;

        private Color LightColor => Bloom * opacity;
        private float BloomScale;

        public BlastStreak(Vector2 center, Vector2 direction, float maxRadius, Color colorStart, Color colorEnd, Color bloom, float scale, int lifeTime, float bloomScale = 1f)
        {
            Position = center;
            Velocity = Vector2.Zero;
            Direction = direction;
            MaxRadius = maxRadius;

            Color = colorStart;
            ColorStart = colorStart;
            ColorEnd = colorEnd;
            Bloom = bloom;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Direction.ToRotation() + MathHelper.PiOver2;
            BloomScale = bloomScale;
        }

        public override void Update()
        {
            Color = Color.Lerp(ColorStart, ColorEnd, LifetimeCompletion);
            opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);

            Scale *= 0.94f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D sparkTexture = ParticleTexture;
            Rectangle streakFrame = new Rectangle(0, 15, sparkTexture.Width, sparkTexture.Height / 2 - 15);
            Vector2 streakOrigin = new Vector2(streakFrame.Width / 2, streakFrame.Height);

            Texture2D bloomTexture = AssetDirectory.CommonTextures.BloomCircle.Value;
            Rectangle bloomFrame = new Rectangle(0, 0, bloomTexture.Width, bloomTexture.Height / 2);
            Vector2 bloomOrigin = new Vector2(bloomFrame.Width / 2, bloomFrame.Height);

            //Ajust the bloom's texture to be the same size as the star's
            float properBloomSize = (float)(sparkTexture.Height - 30) / (float)bloomTexture.Height;

            Vector2 drawOrigin = Position + Direction;
            float squishyScale = Direction.Length() / MaxRadius;

            Vector2 squishScale = new Vector2(0.8f, 8f * Scale * squishyScale);
            Vector2 squishScaleBloom = squishScale with { X = squishScale.X * BloomScale };

            spriteBatch.Draw(bloomTexture, drawOrigin - basePosition, bloomFrame, Bloom * opacity * 0.5f, Rotation, bloomOrigin, squishScaleBloom * properBloomSize, SpriteEffects.None, 0);
            spriteBatch.Draw(sparkTexture, drawOrigin - basePosition, streakFrame, Color * opacity, Rotation, streakOrigin, squishScale, SpriteEffects.None, 0);
        }
    }
}
