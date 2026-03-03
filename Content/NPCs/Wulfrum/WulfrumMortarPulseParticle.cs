namespace CalamityFables.Content.NPCs.Wulfrum
{
    public class WulfrumMortarPulseParticle : Particle
    {
        public override bool SetLifetime => true;
        public override string Texture => AssetDirectory.WulfrumNPC + "WulfrumMortarPulse";
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;

        public float Opacity;
        public float OpacityMult;

        public WulfrumMortarPulseParticle(Vector2 position, Vector2 speed, float scale, Color color, int lifetime, float opacity = 1f)
        {
            Position = position;
            Scale = scale;
            Color = color;
            Velocity = speed;
            Opacity = opacity;
            OpacityMult = opacity;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Opacity = (float)Math.Pow(LifetimeCompletion, 0.5f) * OpacityMult;
            Lighting.AddLight(Position, CommonColors.WulfrumBlue.ToVector3() * Opacity);
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;

            Velocity *= 0.875f;
            Scale *= 0.96f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D baseTex = ParticleTexture;

            FablesUtils.DrawChromaticAberration(Vector2.UnitX.RotatedBy(Rotation), 1.5f, delegate (Vector2 offset, Color colorMod) {
                spriteBatch.Draw(baseTex, Position + offset - basePosition, null, Color.MultiplyRGB(colorMod) * Opacity, Rotation, baseTex.Size() / 2, Scale, SpriteEffects.None, 0);
            });
        }
    }
}
