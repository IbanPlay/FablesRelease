namespace CalamityFables.Particles
{
    public class GoopBlotParticle : Particle
    {
        public override string Texture => AssetDirectory.Particles + "GoopBlot";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;


        public GoopBlotParticle(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Variant = Main.rand.Next(14);
        }

        internal Color lightColor;

        public override void Update()
        {
            Velocity *= 0.97f;
            Scale *= 0.94f;
            Rotation += Velocity.X * 0.03f;

            lightColor = Lighting.GetColor(Position.ToTileCoordinates());
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D dustTexture = ParticleTexture;
            Rectangle frame = dustTexture.Frame(1, 14, 0, Variant, 0, -2);
            spriteBatch.Draw(dustTexture, Position - basePosition, frame, Color.MultiplyRGB(lightColor), Rotation, frame.Size() / 2f, Scale, SpriteEffects.None, 0);
        }
    }
}
