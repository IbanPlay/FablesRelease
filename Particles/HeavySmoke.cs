namespace CalamityFables.Particles
{
    public class HeavySmokeParticle : Particle
    {
        public override bool SetLifetime => true;
        public override int FrameVariants => 7;
        public override bool UseCustomDraw => true;
        public override bool Important => StrongVisual;
        public override bool UseAdditiveBlend => Glowing;
        public override bool UsesNonPremultipliedAlpha => !Glowing;

        public override string Texture => AssetDirectory.Particles + "HeavySmoke";

        private float Opacity;
        private float Spin;
        private bool StrongVisual;
        private bool Glowing;
        private float HueShift;
        static int FrameAmount = 6;

        public HeavySmokeParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale, float opacity, float rotationSpeed = 0f, bool glowing = false, float hueshift = 0f, bool required = false)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Variant = Main.rand.Next(7);
            Lifetime = lifetime;
            Opacity = opacity;
            Spin = rotationSpeed;
            StrongVisual = required;
            Glowing = glowing;
            HueShift = hueshift;

        }

        public override void Update()
        {
            if (Time / (float)Lifetime < 0.2f)
                Scale += 0.01f;
            else
                Scale *= 0.975f;

            Color = Main.hslToRgb((Main.rgbToHsl(Color).X + HueShift) % 1, Main.rgbToHsl(Color).Y, Main.rgbToHsl(Color).Z);
            Opacity *= 0.98f;
            Rotation += Spin * ((Velocity.X > 0) ? 1f : -1f);
            Velocity *= 0.85f;

            float opacity = Utils.GetLerpValue(1f, 0.85f, LifetimeCompletion, true);
            Color *= opacity;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D tex = ParticleTexture;
            int animationFrame = (int)Math.Floor(Time / ((float)(Lifetime / (float)FrameAmount)));
            Rectangle frame = new Rectangle(80 * Variant, 80 * animationFrame, 80, 80);


            spriteBatch.Draw(tex, Position - basePosition, frame, Color * Opacity, Rotation, frame.Size() / 2f, Scale, SpriteEffects.None, 0);
        }

    }
}
