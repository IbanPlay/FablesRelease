namespace CalamityFables.Particles
{
    public class WooshingStreakParticle : Particle
    {
        public override string Texture => AssetDirectory.Particles + "StreakBloom";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        private float opacity;
        private Color Bloom;
        private Color OGColor;
        private Color FadeColor;
        private Color LightColor => Bloom * opacity;
        private float BloomScale;

        public WooshingStreakParticle(Vector2 position, float rotation, float speed, Color? color = null, Color? fadeColor = null, Color? bloom = null, float scale = 1f, int lifeTime = 0)
        {
            Position = position;
            Velocity = (rotation + MathHelper.PiOver2).ToRotationVector2() * speed;
            Rotation = rotation;
            Color = color.HasValue ? color.Value : Color.White;
            OGColor = Color;
            Bloom = bloom.HasValue ? bloom.Value : Color.Gold;
            FadeColor = fadeColor.HasValue ? fadeColor.Value : Color;
            BloomScale = 3f;
            Scale = scale;
            Lifetime = lifeTime == 0 ? 8 : lifeTime;
        }

        public override void Update()
        {
            Color = Color.Lerp(OGColor, FadeColor, LifetimeCompletion);
            opacity = 1f;
            Lighting.AddLight(Position, LightColor.R / 255f, LightColor.G / 255f, LightColor.B / 255f);

            Velocity *= 1f - 0.3f * LifetimeCompletion;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D streakTexture = ParticleTexture;
            Texture2D bloomTexture = AssetDirectory.CommonTextures.BloomCircle.Value;
            //Ajust the bloom's texture to be the same size as the star's
            float properBloomSize = (float)streakTexture.Height / (float)bloomTexture.Height;

            Vector2 motionSquish = new Vector2(1f, 5f - 4.7f * (float)Math.Pow(LifetimeCompletion, 1.3f));
            Vector2 streakOrigin = streakTexture.Size() / 2;

            spriteBatch.Draw(bloomTexture, Position - basePosition, null, Bloom * opacity, Rotation, bloomTexture.Size() / 2f, motionSquish * Scale * BloomScale * properBloomSize, SpriteEffects.None, 0);
            spriteBatch.Draw(streakTexture, Position - basePosition, null, Color * opacity, Rotation, streakOrigin, motionSquish * Scale, SpriteEffects.None, 0);
        }
    }
}
