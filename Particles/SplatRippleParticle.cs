namespace CalamityFables.Particles
{
    public class SplatRippleParticle : Particle
    {
        public override string Texture => AssetDirectory.Particles + "SplatRipple";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public Color startColor;
        public Color endColor;

        public SplatRippleParticle(Vector2 position, Color color, Color endColor)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Color = color;
            startColor = color;
            this.endColor = endColor;

            Scale = 1;

            Lifetime = 30;
        }

        public override void Update()
        {

        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D ripple = ParticleTexture;


            for (int scaleIteration = 0; scaleIteration < 3; scaleIteration++)
            {
                float scaleMult = Scale * (1f - scaleIteration * 0.14f);
                if (scaleIteration == 2)
                    scaleMult *= 0.5f;

                for (int i = 0; i < 2; i++)
                {
                    Vector2 offset = Vector2.Zero;
                    offset.X += 4 + MathF.Pow(MathHelper.Max(0, LifetimeCompletion - i * 0.4f), 0.65f) * 50f * scaleMult;
                    offset.Y += i * 6f * scaleMult;

                    Rectangle frame = ripple.Frame(1, 2, 0, 1 - i, 0, -2);

                    Color baseColor = Color.Lerp(startColor, endColor, LifetimeCompletion);
                    Color color = Color.Lerp(baseColor * 0.2f, baseColor, LifetimeCompletion + (1 - i) * 0.2f);
                    color *= Utils.GetLerpValue(1f, 0.8f, LifetimeCompletion, true) * Utils.GetLerpValue(0f, 0.4f, LifetimeCompletion, true);

                    color *= scaleMult / Scale;

                    Vector2 origin = frame.Size() * new Vector2(0.2f, 0.6f);

                    Vector2 scale = new Vector2(2, 0.6f);
                    scale.X *= 0.2f + 0.8f * MathF.Pow(Utils.GetLerpValue(1f, 0f, LifetimeCompletion + i * 0.2f, true), 0.8f);
                    scale.Y *= 0.1f + 0.8f * MathF.Pow(Utils.GetLerpValue(0.1f, 0.8f, LifetimeCompletion + i * 0.2f, true), 1.6f);

                    scale.Y *= 1f - 0.7f * i;
                    scale.X *= 1f + 0.4f * i;

                    for (int j = -1; j <= 1; j += 2)
                    {
                        SpriteEffects effects = j == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                        Vector2 usedOffset = offset with { X = offset.X * j };
                        usedOffset = usedOffset.RotatedBy(Rotation);
                        Vector2 usedOrigin = origin;
                        if (j == 1)
                            usedOrigin.X = frame.Size().X - usedOrigin.X;

                        spriteBatch.Draw(ripple, Position - basePosition + usedOffset, frame, color, Rotation, usedOrigin, scale * scaleMult, effects, 0);
                    }
                }
            }
        }
    }
}
