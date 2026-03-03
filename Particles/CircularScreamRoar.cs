namespace CalamityFables.Particles
{
    public class CircularScreamRoar : Particle
    {
        public override string Texture => AssetDirectory.Noise + "ChromaBurst";
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;

        public override bool SetLifetime => true;

        public CircularScreamRoar(Vector2 position, Color color, float scale)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Color = color;
            Scale = scale;
            Rotation = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            Lifetime = 10;
        }

        public override void Update()
        {
            Scale *= 1.1f + 0.8f * LifetimeCompletion;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D tex = ParticleTexture;
            spriteBatch.Draw(tex, Position - basePosition, null, Color, Rotation, tex.Size() * 0.5f, Scale, 0, 0);
        }
    }

    public class CircularScreamRoarNonAdditive : CircularScreamRoar
    {
        public override string Texture => AssetDirectory.Noise + "PremultBurst";
        public override bool UseAdditiveBlend => false;

        public CircularScreamRoarNonAdditive(Vector2 position, Color color, float scale) : base(position, color, scale)
        {
        }

        public override void Update()
        {
            base.Update();
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            base.CustomDraw(spriteBatch, basePosition);
        }
    }
}
