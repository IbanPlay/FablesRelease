namespace CalamityFables.Particles
{
    public class TwinklySparkle : Particle, IDrawPixelated
    {
        public override string Texture => AssetDirectory.Particles + "StreakBloom";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;
        public bool ShoulDrawPixelated => Pixelated;
        public override bool Important => Needed;

        public bool Pixelated { get; set; }
        public bool Needed { get; set; } = false;

        protected float AngularVelocity;
        protected float MaxOpacity;
        protected float Opacity;

        private float FlickerOpacity;
        private int FlickerTime;
        private int FlickerTimer;

        public TwinklySparkle(Vector2 position, Vector2 velocity, Color color, float scale = 1f, float opacity = 1f, float angularVelocity = 0f, int? lifetime = null)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            MaxOpacity = opacity;
            AngularVelocity = angularVelocity;
            Lifetime = lifetime ?? Main.rand.Next(20, 30);
            Rotation = angularVelocity != 0f ? Main.rand.NextFloat(MathHelper.TwoPi) : 0f;
        }

        public override void Update()
        {
            float flickerLerp = 1 - FlickerTimer / (float)FlickerTime;
            FlickerOpacity = 0.4f + 0.6f * MathF.Sin(flickerLerp * MathHelper.Pi);
            FlickerTimer--;
            if (FlickerTimer <= 0)
                FlickerTime = FlickerTimer = Main.rand.Next(10, 30);

            Opacity = (float)MathF.Sin(LifetimeCompletion * MathHelper.Pi) * MaxOpacity * FlickerOpacity;

            Lighting.AddLight(Position, (Color * Opacity * 0.75f).ToVector3());
            Velocity *= 0.95f;
            Rotation += AngularVelocity * ((Velocity.X > 0) ? 1f : -1f);
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            if (!Pixelated)
                DrawFlash(spriteBatch, basePosition);
        }

        public void DrawPixelated(SpriteBatch spriteBatch) => DrawFlash(spriteBatch, Main.screenPosition);

        public void DrawFlash(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D flashStreak = ParticleTexture;

            Vector2 position = Position - basePosition;
            Vector2 lensFlareScale = Vector2.One * 2f;

            if (Pixelated)
            {
                lensFlareScale *= 0.5f;
                position *= 0.5f;
            }

            // Draw a white and colored lens flare layer
            for (int i = 0; i < 2; i++)
            {
                Color lensFlareColor = (i == 0 ? Color : Color.White) * MathF.Pow(1 - LifetimeCompletion, 0.3f);
                lensFlareScale *= 1f - i * 0.44f;
                lensFlareScale.X *= 1f - i * 0.5f;

                spriteBatch.Draw(flashStreak, position, null, lensFlareColor with { A = 0 } * Opacity, Rotation, flashStreak.Size() / 2f, lensFlareScale * Scale, SpriteEffects.None, 0);
                spriteBatch.Draw(flashStreak, position, null, lensFlareColor with { A = 0 } * Opacity, Rotation + MathHelper.PiOver2, flashStreak.Size() / 2f, lensFlareScale * Scale, SpriteEffects.None, 0);
            }
        }
    }
}
