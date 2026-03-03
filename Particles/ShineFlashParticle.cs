namespace CalamityFables.Particles
{
    public class ShineFlashParticle : Particle, IDrawPixelated
    {
        public override string Texture => AssetDirectory.Particles + "StreakBloom";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;
        public bool ShoulDrawPixelated => Pixelated;
        public override bool Important => Needed;

        public bool Pixelated { get; set; }
        public bool Needed { get; set; } = false;
        public bool EmitLight { get; set; } = false;

        protected float BloomScale;
        protected float AngularVelocity;

        public ShineFlashParticle(Vector2 position, Vector2 velocity, Color color, float scale = 1f, float bloomScale = 0.5f, float angularVelocity = 0f, int lifetime = 20)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            BloomScale = bloomScale;
            AngularVelocity = angularVelocity;
            Lifetime = lifetime;
            Rotation = angularVelocity != 0f ? Main.rand.NextFloat(MathHelper.TwoPi) : 0f;
        }

        public override void Update()
        {
            Velocity *= 0.95f;
            Rotation += Rotation.NonZeroSign() * LifetimeCompletion * AngularVelocity;

            if (EmitLight)
            {
                Color lightColor = Color * 0.5f * FablesUtils.SineBumpEasing(LifetimeCompletion);
                Lighting.AddLight(Position, lightColor.ToVector3());
            }
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            if (!Pixelated)
                DrawFlash(spriteBatch, basePosition);
        }

        public void DrawPixelated(SpriteBatch spriteBatch) => DrawFlash(spriteBatch, Main.screenPosition);

        public void DrawFlash(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D streakFlash = ParticleTexture;
            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

            Vector2 position = Position - basePosition;

            // Flare scale
            Vector2 lensFlareScale = new(MathF.Pow(1 - LifetimeCompletion, 0.5f) * 2f, 3.5f);
            lensFlareScale *= 0.2f + 0.8f * MathF.Pow(LifetimeCompletion, 0.75f);
            lensFlareScale *= Utils.GetLerpValue(1f, 0.65f, LifetimeCompletion, true);

            float bloomScale = BloomScale;

            if (Pixelated)
            {
                lensFlareScale *= 0.5f;
                bloomScale *= 0.5f;
                position *= 0.5f;
            }

            // Draw bloom
            float bloomOpacity = MathF.Pow(1 - LifetimeCompletion, 0.7f);
            spriteBatch.Draw(bloom, position, null, Color with { A = 0 } * bloomOpacity, 0f, bloom.Size() / 2f, bloomScale * Scale, 0, 0);

            // Draw a white and colored lens flare layer
            for (int i = 0; i < 2; i++)
            {
                Color lensFlareColor = (i == 0 ? Color : Color.White) * MathF.Pow(1 - LifetimeCompletion, 0.3f);
                lensFlareScale *= 1f - i * 0.44f;

                spriteBatch.Draw(streakFlash, position, null, lensFlareColor with { A = 0 }, Rotation, streakFlash.Size() / 2f, lensFlareScale * Scale, SpriteEffects.None, 0);
                spriteBatch.Draw(streakFlash, position, null, lensFlareColor with { A = 0 }, Rotation + MathHelper.PiOver2, streakFlash.Size() / 2f, lensFlareScale * Scale, SpriteEffects.None, 0);
            }
        }
    }
}