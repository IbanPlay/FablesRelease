using CalamityFables.Core.RenderTargets;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Particles
{
    public class BlendedSmoke : RTParticle
    {
        public override string Texture => AssetDirectory.Particles + "BlendedSmoke";

        public Texture2D CurrentTexture => OverrideTexture ?? ParticleTexture;
        public Texture2D OverrideTexture { get; set; }

        /// <summary>
        /// Controls the progress of the animation along time, allowing some parts of the animation to progress faster or slower.
        /// </summary>
        public EasingFunction AnimationFunction { get; set; } = LinearEasing;
        public float AnimationFunctionDegree { get; set; } = 1f;
        public bool NoLight { get; set; } = false;
        public bool Flip { get; set; }

        public readonly float Gravity;

        public BlendedSmoke(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale = 1f, float gravity = -0.05f)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Lifetime = lifetime;
            Scale = scale;
            Gravity = gravity;

            Frames = new(3, 7);
            Frame.X = Main.rand.Next(3);
            Flip = Main.rand.NextBool();
        }

        /// <summary>
        /// Can be used to change which texture this particle uses, alongside the number of frames present in it's animation.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="frames"></param>
        /// <param name="frame"></param>
        public void SetTexture(Texture2D texture, Point? frames = null, Point? frame = null)
        {
            OverrideTexture = texture;
            Frames = frames ?? Frames;
            Frame = frame ?? Frame;
        }

        public override void Update()
        {
            base.Update();

            Velocity *= 0.95f;
            Velocity.Y += Gravity;

            float animationProgress = AnimationFunction(Progress, AnimationFunctionDegree);
            Frame.Y = (int)MathHelper.Lerp(0, Frames.Y, animationProgress);
        }

        public override void DrawParticle(SpriteBatch spriteBatch, Vector2 basePosition, Color passColor)
        {
            Texture2D texture = CurrentTexture;
            Rectangle frame = texture.Frame(Frames.X, Frames.Y, Frame.X, Frame.Y);
            SpriteEffects effect = Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Color color = Color.MultiplyRGB(passColor);
            if (!NoLight)
                color = color.MultiplyRGB(Lighting.GetColor(Position.ToTileCoordinates()));

            spriteBatch.Draw(texture, (Position / 2) - basePosition, frame, color, Rotation, frame.Size() / 2, Scale * 0.5f, effect, 0);
        }
    }

    public class FadingBlendedSmoke : BlendedSmoke
    {
        public override string Texture => AssetDirectory.Particles + "BlendedSmoke";

        #region Easings
        public EasingFunction FadeEasing { get; set; } = LinearEasing;
        public bool InvertFadeEasing { get; set; } = false;
        public float FadeEasingDegree { get; set; } = 1f;
        public EasingFunction LightEasing { get; set; } = ConstantEasing;
        public bool InvertLightEasing { get; set; } = false;
        public float LightEasingDegree { get; set; } = 1f;
        #endregion

        public readonly Color FadeColor;

        public FadingBlendedSmoke(Vector2 position, Vector2 velocity, Color color, Color fadeColor, int lifetime, float scale = 1, float gravity = -0.05F) : base(position, velocity, color, lifetime, scale, gravity)
        {
            FadeColor = fadeColor;
        }

        public override void DrawParticle(SpriteBatch spriteBatch, Vector2 basePosition, Color passColor)
        {
            Texture2D texture = CurrentTexture;
            Rectangle frame = texture.Frame(Frames.X, Frames.Y, Frame.X, Frame.Y);
            SpriteEffects effect = Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Fade color
            float fadeProgress = FadeEasing(InvertFadeEasing ? 1f - Progress : Progress, FadeEasingDegree);
            Color color = Color.Lerp(Color, FadeColor, fadeProgress);

            // Apply pass color
            color = color.MultiplyRGB(passColor);

            if (!NoLight)
            {
                float lightProgress = LightEasing(InvertLightEasing ? 1f - Progress : Progress, LightEasingDegree);
                Color lightColor = color.MultiplyRGB(Lighting.GetColor(Position.ToTileCoordinates()));
                color = Color.Lerp(color, lightColor, lightProgress);
            }

            spriteBatch.Draw(texture, (Position / 2) - basePosition, frame, color, Rotation, frame.Size() / 2, Scale * 0.5f, effect, 0);
        }
    }

    public class MergeBlendSmokeRenderTarget : ParticleRenderTarget
    {
        public EasyRenderTarget EasyRenderTarget;

        public override void InitializeFields()
        {
            Scale = 2f;
            Size = new Point(Main.screenWidth / 2, Main.screenHeight / 2 * 5);
            Opacity = 0.5f;
        }

        public override void Initialize()
        {
            // Easy RT used to simplify drawing particles to a target and automatic resizing
            // Easy RT also has an automatic disposal feature, which we ignore in favor of the particle systems disposal
            EasyRenderTarget = new(DrawSmoke, null, () => Size);
            EasyRenderTarget.Initialize();
        }

        public override void Dispose() => EasyRenderTarget.Dispose();

        private void DrawSmoke(SpriteBatch spriteBatch)
        {
            if (AssignedParticles is null || AssignedParticles.Count <= 0)
                return;

            // Batch particles based on draw layer
            List<RTParticle> abovePlayers = [];
            List<RTParticle> aboveProjectiles = [];
            List<RTParticle> aboveNPCs = [];
            List<RTParticle> aboveTiles = [];
            List<RTParticle> behindTiles = [];

            foreach (var particle in AssignedParticles)
            {
                switch (particle.Layer)
                {
                    case DrawhookLayer.AbovePlayer:
                        abovePlayers.Add(particle);
                        break;
                    case DrawhookLayer.AboveProjectiles:
                        aboveProjectiles.Add(particle);
                        break;
                    case DrawhookLayer.AboveNPCs:
                        aboveNPCs.Add(particle);
                        break;
                    case DrawhookLayer.AboveTiles:
                        aboveTiles.Add(particle);
                        break;
                    case DrawhookLayer.BehindTiles:
                        behindTiles.Add(particle);
                        break;
                }
            }

            // Save current scissor rect
            RasterizerState priorRasterizer = spriteBatch.GraphicsDevice.RasterizerState;
            Rectangle priorScissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
            bool scissorEnabled = priorRasterizer.ScissorTestEnable;

            // Run 5 times for each layer
            for (int i = 0; i < 5; i++)
            {
                // Select batch to process
                IEnumerable<RTParticle> batch = i switch
                {
                    0 => abovePlayers,
                    1 => aboveProjectiles,
                    2 => aboveNPCs,
                    3 => aboveTiles,
                    _ => behindTiles
                };

                // Stop here if the batch is empty
                if (!batch.Any())
                    continue;

                // Frame offset to draw everything on one render target
                float frameOffset = Size.Y / 5 * i;

                // Source rectagnle for scissor rect
                Point sourceSize = new(Size.X, Size.Y / 5);
                Rectangle source = new(0, i * sourceSize.Y, sourceSize.X, sourceSize.Y);

                // Set scissor rect
                spriteBatch.GraphicsDevice.RasterizerState.ScissorTestEnable = true;
                spriteBatch.GraphicsDevice.ScissorRectangle = source;

                for (int j = 0; j < 2; j++)
                {
                    bool backgroundPass = j == 0;

                    // Lighten blending on second pass
                    if (backgroundPass)
                        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone);
                    else
                        spriteBatch.Begin(SpriteSortMode.Immediate, CustomBlendStates.Lighten, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone);

                    // Draw each particle in batch
                    foreach (var particle in batch)
                            particle.DrawParticle(spriteBatch, (Main.screenPosition / Scale) - Vector2.UnitY * frameOffset - Origin, backgroundPass ? Color.Black : Color.White);

                    spriteBatch.End();
                }
            }

            // Set prior settings
            spriteBatch.GraphicsDevice.RasterizerState = priorRasterizer;
            spriteBatch.GraphicsDevice.RasterizerState.ScissorTestEnable = scissorEnabled;
            spriteBatch.GraphicsDevice.ScissorRectangle = priorScissorRectangle;
        }

        public override void DrawRenderTarget(SpriteBatch spritebatch, DrawhookLayer layer = DrawhookLayer.AbovePlayer, Rectangle? source = null)
        {
            if (EasyRenderTarget is null || EasyRenderTarget.RenderTarget is null)
                return;

            // Split render target into 5 sections for each layer, like frames
            Point sourceSize = new(Size.X, Size.Y / 5);

            int frameY = layer switch
            {
                DrawhookLayer.AbovePlayer => 0,
                DrawhookLayer.AboveProjectiles => 1,
                DrawhookLayer.AboveNPCs => 2,
                DrawhookLayer.AboveTiles => 3,
                DrawhookLayer.BehindTiles => 4,
                _ => 0
            };

            source = new Rectangle(0, frameY * sourceSize.Y, sourceSize.X, sourceSize.Y);

            spritebatch.Draw(EasyRenderTarget.RenderTarget, Position, source, Color.White with { A = (byte)(255 * Opacity) }, 0, Origin, Scale, 0, 0);
        }
    }
}