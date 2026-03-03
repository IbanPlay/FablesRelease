namespace CalamityFables.Particles
{
    public class ExplosionSmoke : Particle
    {
        public override string Texture => AssetDirectory.Particles + "Smoke";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => false;

        private Texture2D CurrentTexture => OverrideTexture ?? ParticleTexture;
        public Texture2D OverrideTexture { get; set; }

        internal int alpha;
        internal Color ColorFire;
        internal Color ColorFade;
        internal Color ColorEnd;
        internal float Spin;
        internal bool Lighted;

        public override int FrameVariants => 3;

        public ExplosionSmoke(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, float scale, float rotationSpeed = 0f, bool lighted = false)
        {
            Position = position;
            Velocity = velocity;
            ColorFire = colorFire;
            ColorFade = colorFade;
            ColorEnd = new Color(25, 25, 25);
            Scale = scale;
            alpha = Main.rand.Next(60);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationSpeed;
            Lighted = lighted;
            Variant = Main.rand.Next(3);
        }

        public ExplosionSmoke(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, Color colorEnd, float scale, float rotationSpeed = 0f, bool lighted = false)
        {
            Position = position;
            Velocity = velocity;
            ColorFire = colorFire;
            ColorFade = colorFade;
            ColorEnd = colorEnd;
            Scale = scale;
            alpha = Main.rand.Next(60);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationSpeed;
            Lighted = lighted;
            Variant = Main.rand.Next(3);
        }

        public override void Update()
        {
            Rotation += Spin * ((Velocity.X > 0) ? 1f : -1f);

            if (Math.Abs(Velocity.X) > 7)
                Velocity.X *= 0.85f;
            else
                Velocity.X *= 0.92f;

            //Fly up
            if (Velocity.Y > -2)
                Velocity.Y -= 0.05f;
            if (Velocity.Y > 3.5f)
                Velocity.Y = 3.5f;
            else
                Velocity.Y *= 0.95f;

            if (alpha > 100)
            {
                Scale += 0.01f;
                alpha += 2;
            }

            else
            {
                Lighting.AddLight(Position, Color.ToVector3() * 0.1f);
                Scale *= 0.985f;
                alpha += 4;
            }

            if (alpha >= 255)
                Kill();

            //Shifts from the fire color to the gray color
            if (alpha < 80)
                Color = Color.Lerp(ColorFire, ColorFade, alpha / 80f);
            else if (alpha < 140)
                Color = Color.Lerp(ColorFade, ColorEnd, (alpha - 80) / 60f);
            else
                Color = ColorEnd;

            Color.A = (byte)alpha;
            Color *= (255 - alpha) / 255f; //Fades with alpha

            if (Lighted)
                Color = Color.MultiplyRGBA(Lighting.GetColor(Position.ToTileCoordinates()));
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D texture = CurrentTexture;
            Rectangle frame = texture.Frame(1, FrameVariants, 0, Variant);

            spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color, Rotation, frame.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
        }
    }
}