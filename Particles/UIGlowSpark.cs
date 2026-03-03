namespace CalamityFables.Particles
{
    public class UIGlowSpark : Particle
    {
        public override string Texture => AssetDirectory.Particles + "ThinSparkle";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        private float Spin;
        private float opacity;
        private Color Bloom;
        private Color LightColor => Bloom * opacity;
        private float BloomScale;
        private float HueShift;

        public UIGlowSpark(Vector2 position, Vector2 velocity, Color color, Color bloom, float scale, int lifeTime, float rotationSpeed = 1f, float bloomScale = 1f, float hueShift = 0f)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Bloom = bloom;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationSpeed;
            BloomScale = bloomScale;
            HueShift = hueShift;
            Type = ParticleHandler.particleTypes[typeof(UIGlowSpark)];
        }

        public override void Update()
        {
            opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);
            Velocity.Y -= 0.03f;
            Rotation += Spin;
            Color.A = 0;
            Bloom.A = 0;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D sparkTexture = ParticleTexture;
            Texture2D bloomTexture = AssetDirectory.CommonTextures.BloomCircle.Value;
            Texture2D reverseBloomTexture = AssetDirectory.CommonTextures.PixelBloomCircle.Value;
            //Ajust the bloom's texture to be the same size as the star's
            float properBloomSize = (float)sparkTexture.Height / (float)bloomTexture.Height;
            float properUnBloomSize = (float)sparkTexture.Height / (float)reverseBloomTexture.Height;

            spriteBatch.Draw(reverseBloomTexture, Position, null, Color.Black * opacity * 0.2f, 0, reverseBloomTexture.Size() / 2f, Scale * 1.3f * properUnBloomSize, SpriteEffects.None, 0);

            spriteBatch.Draw(bloomTexture, Position, null, Bloom * opacity * 0.5f, 0, bloomTexture.Size() / 2f, Scale * BloomScale * properBloomSize, SpriteEffects.None, 0);
            spriteBatch.Draw(sparkTexture, Position, null, Color * opacity, Rotation, sparkTexture.Size() / 2f, Scale, SpriteEffects.None, 0);
        }
    }
}
