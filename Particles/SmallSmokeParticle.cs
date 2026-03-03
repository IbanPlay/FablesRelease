namespace CalamityFables.Particles
{
    public class SmallSmokeParticle : Particle
    {
        public override string Texture => AssetDirectory.Particles + "SmallSmoke";

        private float Opacity;
        private Color ColorFire;
        private Color ColorFade;
        private float Spin;
        private bool Lighted;

        public override bool SetLifetime => false;

        public SmallSmokeParticle(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, float scale, float opacity, float rotationSpeed = 0f, bool lighted = false)
        {
            Position = position;
            Velocity = velocity;
            ColorFire = colorFire;
            ColorFade = colorFade;
            Scale = scale;
            Opacity = opacity;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationSpeed;
            Lighted = lighted;
        }

        public override void Update()
        {

            Rotation += Spin * ((Velocity.X > 0) ? 1f : -1f);
            Velocity *= 0.85f;

            if (Opacity > 90)
            {
                if (!Lighted)
                    Lighting.AddLight(Position, Color.ToVector3() * 0.1f);
                Scale += 0.01f;
                Opacity -= 3;
            }
            else
            {
                Scale *= 0.975f;
                Opacity -= 2;
            }
            if (Opacity < 0)
                Kill();

            Color = Color.Lerp(ColorFire, ColorFade, MathHelper.Clamp((float)((255 - Opacity) - 100) / 80, 0f, 1f)) * (Opacity / 255f);
            if (Lighted)
                Color = Color.MultiplyRGBA(Lighting.GetColor(Position.ToTileCoordinates()));

        }
    }
}
