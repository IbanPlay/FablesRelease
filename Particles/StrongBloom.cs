namespace CalamityFables.Particles
{
    public class StrongBloom : Particle
    {
        public override string Texture => AssetDirectory.Particles + "BloomCircle";
        public override bool UseAdditiveBlend => true;
        public override bool SetLifetime => true;

        private float opacity;
        private Color BaseColor;

        public StrongBloom(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime)
        {
            Position = position;
            Velocity = velocity;
            BaseColor = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void Update()
        {
            opacity = (float)Math.Sin(LifetimeCompletion * MathHelper.Pi);
            Color = BaseColor * opacity;
            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
            Velocity *= 0.95f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D tex = ParticleTexture;
            spriteBatch.Draw(tex, Position - basePosition, null, Color * opacity, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
        }

    }
}
