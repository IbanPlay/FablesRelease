namespace CalamityFables.Particles
{
    public class PixelStreaks : Particle, IDrawPixelated
    {
        public DrawhookLayer layer => DrawhookLayer.AbovePlayer;

        public override string Texture => AssetDirectory.Particles + "PixelStreak";

        public static Asset<Texture2D> BloomTex;

        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        private float opacity;
        private Color bloom;

        public PixelStreaks(Vector2 center,  Color color, Color bloom, float scale)
        {
            Position = center;
            Velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 3.6f);

            this.bloom = bloom;
            Color = color;
            Scale = scale;
            opacity = 1f;

            Lifetime = Main.rand.Next(20, 30);
        }

        public override void Update()
        {
            opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);
            Velocity *= 0.99f;

            Scale *= 0.94f;
        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Texture2D coreTex = ParticleTexture;
            BloomTex ??= ModContent.Request<Texture2D>(AssetDirectory.Particles + "PixelStreakGlow");
            Texture2D outlineTex = BloomTex.Value;

            //Shrink in all dimensions at the end
            float finalShrink = MathF.Pow(Utils.GetLerpValue(1f, 0.8f, LifetimeCompletion, true), 0.6f);
            float streakHeight = Scale * 25f * MathF.Pow(Utils.GetLerpValue(0f, 0.36f, LifetimeCompletion, true), 1.7f);


            Rectangle topFrame = new Rectangle(0, 0, coreTex.Width, 5);
            Rectangle bottomFrame = new Rectangle(0, coreTex.Height - 5, coreTex.Width, 5);
            Rectangle middleFrame = new Rectangle(0, 5, coreTex.Width, 2);

            Vector2 topPosition = (Position - Main.screenPosition) / 2f;
            Vector2 botPosition = (Position + Vector2.UnitY * streakHeight - Main.screenPosition) / 2f;
            Vector2 middleScale = new Vector2(0.5f, (streakHeight * 0.5f) / 2f);

            spriteBatch.Draw(outlineTex, topPosition, topFrame, bloom with { A = 0 } * opacity * 0.3f, 0f, new Vector2(5, 5), 0.5f * finalShrink, SpriteEffects.None, 0);
            spriteBatch.Draw(outlineTex, botPosition, bottomFrame, bloom with { A = 0 } * opacity * 0.3f, 0f, new Vector2(5, 0), 0.5f * finalShrink, SpriteEffects.None, 0);
            spriteBatch.Draw(outlineTex, topPosition, middleFrame, bloom with { A = 0 } * opacity * 0.3f, 0f, new Vector2(5, 0), middleScale * finalShrink, SpriteEffects.None, 0);

            for (int i = 0; i < 2; i++)
            {
                spriteBatch.Draw(coreTex, topPosition, topFrame, Color with { A = 0 } * opacity, 0f, new Vector2(5, 5), 0.5f * finalShrink, SpriteEffects.None, 0);
                spriteBatch.Draw(coreTex, botPosition, bottomFrame, Color with { A = 0 } * opacity, 0f, new Vector2(5, 0), 0.5f * finalShrink, SpriteEffects.None, 0);
                spriteBatch.Draw(coreTex, topPosition, middleFrame, Color with { A = 0 } * opacity, 0f, new Vector2(5, 0), middleScale * finalShrink, SpriteEffects.None, 0);
            }

            Vector2 bloomCenter = (botPosition + topPosition) / 2f;
            Texture2D circleBloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            Vector2 bloomOrigin = circleBloom.Size() / 2f;

            Vector2 bloomScale = new Vector2(24f / (float)circleBloom.Width, (streakHeight + 10) / (float)circleBloom.Height * 2.5f) * finalShrink * 0.5f;

            spriteBatch.Draw(circleBloom, bloomCenter, null, bloom with { A = 0 } * opacity * 0.15f, 0f, bloomOrigin, bloomScale, SpriteEffects.None, 0);
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            /*
            Texture2D coreTex = ParticleTexture;
            Rectangle frame = coreTex.Frame(1, 2, 0, Variant);
            Vector2 origin = frame.Size() / 2f;

            Texture2D bloomTexture = ModContent.Request<Texture2D>(AssetDirectory.Particles + "BloomCircle").Value;
            Vector2 bloomOrigin = bloomTexture.Size() / 2f;

            //Ajust the bloom's texture to be the same size as the star's
            float properBloomSize = (float)coreTex.Height / (float)bloomTexture.Height * 1.5f;

            Vector2 drawScale = new Vector2(0.6f, 1.8f);
            drawScale.X *= MathF.Pow(Utils.GetLerpValue(1f, 0.8f, LifetimeCompletion, true), 0.7f);
            drawScale.Y *= 1 + MathF.Pow(Utils.GetLerpValue(0.7f, 1f, LifetimeCompletion, true), 1.7f);


            drawScale *= Scale;


            spriteBatch.Draw(bloomTexture, Position - basePosition, null, bloom with { A = 0 } * opacity, 0f, bloomOrigin, drawScale * properBloomSize, SpriteEffects.None, 0);
            spriteBatch.Draw(coreTex, Position - basePosition, frame, Color with { A = 0 } * opacity, 0f, origin, drawScale, SpriteEffects.None, 0);
            spriteBatch.Draw(coreTex, Position - basePosition, frame, Color with { A = 0 } * opacity, 0f, origin, drawScale, SpriteEffects.None, 0);
            spriteBatch.Draw(coreTex, Position - basePosition, frame, Color with { A = 0 } * opacity, 0f, origin, drawScale, SpriteEffects.None, 0);
            */
        }
    }
}
