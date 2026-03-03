namespace CalamityFables.Core
{
    //Noiter
    public struct BloomInfo
    {
        public Color bloomColor;
        public Vector2 position;
        public float scale;
        public float opacity;

        public BloomInfo(Color bloomColor, Vector2 position, float scale, float opacity)
        {
            this.bloomColor = bloomColor;
            this.position = position;
            this.scale = scale;
            this.opacity = opacity;
        }
    }

    public class NoitaBloomLayer : ModSystem
    {
        public static RenderTarget2D bloomRenderTarget;
        public static float bloomOpacity = 0.4f;
        public static List<BloomInfo> bloomedDust = new List<BloomInfo>();

        public override void Load()
        {
            if (Main.dedServ)
                return;

            RenderTargetsManager.ResizeRenderTargetEvent += ResizeRenderTarget;
            RenderTargetsManager.DrawToRenderTargetsEvent += DrawToRenderTarget;
            FablesDrawLayers.DrawBehindDustEvent += DrawNoitaBlooms;
            RenderTargetsManager.ClearDustCachesEvent += ClearNoitaDust;
            ResizeRenderTarget();
        }

        private void ClearNoitaDust()
        {
            bloomedDust.Clear();
        }

        private void DrawToRenderTarget()
        {
            SwapToBloomRT();
            if (bloomedDust.Count == 0)
            {
                Main.graphics.GraphicsDevice.SetRenderTargets(null);
                return;
            }

            Texture2D glow = AssetDirectory.CommonTextures.BloomCircle.Value;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

            foreach (BloomInfo bloomSource in bloomedDust)
            {
                Main.spriteBatch.Draw(glow, (bloomSource.position - Main.screenPosition) / 2, null, bloomSource.bloomColor * 0.2f, 0f, glow.Size() / 2, 2f * bloomSource.scale, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(glow, (bloomSource.position - Main.screenPosition) / 2, null, bloomSource.bloomColor * 0.5f, 0f, glow.Size() / 2, bloomSource.scale, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(glow, (bloomSource.position - Main.screenPosition) / 2, null, bloomSource.bloomColor * 0.6f, 0f, glow.Size() / 2, 0.2f * bloomSource.scale, SpriteEffects.None, 0);
            }

            Main.spriteBatch.End();
            Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            Main.graphics.GraphicsDevice.SetRenderTargets(null);
        }

        private void ResizeRenderTarget()
        {
            Main.QueueMainThreadAction(() =>
            {
                if (bloomRenderTarget != null && !bloomRenderTarget.IsDisposed)
                    bloomRenderTarget.Dispose();

                bloomRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
            });
        }

        private void DrawNoitaBlooms()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, CustomBlendStates.AdditiveNoAlpha, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(bloomRenderTarget, Vector2.Zero, null, Color.White * bloomOpacity, 0, new Vector2(0, 0), 2f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }

        public static bool SwapToBloomRT()
        {
            GraphicsDevice gD = Main.graphics.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;

            if (Main.gameMenu || Main.dedServ || spriteBatch is null || bloomRenderTarget is null || gD is null)
                return false;

            gD.SetRenderTarget(bloomRenderTarget);
            gD.Clear(Color.Transparent);
            return true;
        }

        public override void PostUpdateEverything()
        {
            bloomOpacity = MathHelper.Lerp(bloomOpacity, 0.5f, 0.2f);
            base.PostUpdateEverything();
        }
    }
}