using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public class BloomParticle : Particle, IDrawPixelated
    {
        public override string Texture => AssetDirectory.Particles + "BigLight";
        public Texture2D CurrentTexture => OverrideTexture ?? ParticleTexture;
        private Texture2D OverrideTexture;
        public override bool UseCustomDraw => true;

        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;
        public bool ShoulDrawPixelated => Pixelated;

        public bool Pixelated { get; set; } = false;

        #region Easings
        public EasingFunction ScaleEasing { get; set; } = ExpOutEasing;
        public bool InvertScaleEasing { get; set; } = true;
        public float ScaleEasingDegree { get; set; } = 1f;
        public EasingFunction OpacityEasing { get; set; } = PolyOutEasing;
        public bool InvertOpacityEasing { get; set; } = true;
        public float OpacityEasingDegree { get; set; } = 1f;
        #endregion

        private readonly Color BloomColor;
        private readonly float MaxScale;
        private readonly float MaxOpactiy;
        private readonly float Gravity;

        private float Opacity;

        public BloomParticle(Vector2 position, Vector2 velocity, Color color, Color bloomColor, float scale = 1f, float opacity = 1f, int? lifetime = null, float gravity = -0.05f)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            BloomColor = bloomColor;
            MaxScale = Scale = scale;
            MaxOpactiy = Opacity = opacity;
            Lifetime = lifetime is null ? Main.rand.Next(20, 30) : lifetime.Value;
            Gravity = gravity;
        }

        public override void Update()
        {
            Velocity *= 0.98f;
            Velocity.Y += Gravity;

            float scaleProgress = InvertScaleEasing ? 1f - LifetimeCompletion : LifetimeCompletion;
            Scale = MaxScale * ScaleEasing(scaleProgress, ScaleEasingDegree);

            float opacityProgress = InvertOpacityEasing ? 1f - LifetimeCompletion : LifetimeCompletion;
            Opacity = MaxOpactiy * OpacityEasing(opacityProgress, OpacityEasingDegree);
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            if (!Pixelated)
                Draw(spriteBatch);
        }

        public void DrawPixelated(SpriteBatch spriteBatch) => Draw(spriteBatch);

        private void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ParticleTexture;
            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

            // Setup values
            Vector2 position = Position - Main.screenPosition;

            Color centerColor = Color with { A = 0 };
            float centerScale = Scale * 0.04f;

            Color bloomColor = BloomColor with { A = 0 };
            float bloomScale = Scale;

            // Change position and scale if pixelated
            if (Pixelated)
            {
                position *= 0.5f;
                centerScale *= 0.5f;
                bloomScale *= 0.5f;
            }

            // Draw center
            spriteBatch.Draw(texture, position, null, centerColor, 0, texture.Size() / 2, centerScale, SpriteEffects.None, 0f);

            // Draw two bloom layers
            spriteBatch.Draw(bloom, position, null, bloomColor * 0.4f, 0, bloom.Size() / 2, bloomScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(bloom, position, null, bloomColor, 0, bloom.Size() / 2, bloomScale * 0.1f, SpriteEffects.None, 0f);
        }
    }
}