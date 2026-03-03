namespace CalamityFables.Core
{
    public interface IDrawOverTileMask
    {
        public bool MaskDrawActive => true;
        public bool UsesSolidMask => true;
        public bool UsesNonsolidMask => true;
        public void DrawOverMask(SpriteBatch spriteBatch, bool solidLayer) { }
    }

    public class DrawOverTileMaskLoader : ModSystem
    {
        public static RenderTarget2D maskNonsolidTarget;
        public static RenderTarget2D maskTarget;

        public static RenderTarget2D nonsolidTilesTarget;
        public static RenderTarget2D solidTilesTarget;

        public override void Load()
        {
            RenderTargetsManager.ResizeRenderTargetEvent += ResizeRenderTargets;
            RenderTargetsManager.DrawToRenderTargetsEvent += DrawToRenderTargets;
            On_Main.DrawProjectiles += DrawSolidMask;
            FablesDrawLayers.DrawThingsBehindSolidTilesAndBackgroundNPCsEvent += DrawNonsolidTarget;
        }

        private void ResizeRenderTargets()
        {
            if (Main.dedServ)
                return;

            Main.QueueMainThreadAction(() =>
            {
                DisposeTargets();

                maskNonsolidTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
                maskTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);

                nonsolidTilesTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
                solidTilesTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
            });
        }

        public void DisposeTargets()
        {
            if (maskNonsolidTarget != null && !maskNonsolidTarget.IsDisposed)
                maskNonsolidTarget.Dispose();
            if (maskTarget != null && !maskTarget.IsDisposed)
                maskTarget.Dispose();

            if (nonsolidTilesTarget != null && !nonsolidTilesTarget.IsDisposed)
                nonsolidTilesTarget.Dispose();
            if (solidTilesTarget != null && !solidTilesTarget.IsDisposed)
                solidTilesTarget.Dispose();

        }

        public override void Unload()
        {
            if (Main.dedServ)
                return;
            Main.QueueMainThreadAction(() =>
            {
                DisposeTargets();
            });
        }

        public static readonly List<IDrawOverTileMask> renderNonsolidQueue = new List<IDrawOverTileMask>();
        public static readonly List<IDrawOverTileMask> renderQueue = new List<IDrawOverTileMask>();

        private static bool refreshedNonsolidTarget = false;
        private static bool refreshTarget = false;

        private static void DrawToRenderTargets()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];

                if (proj.active && proj.ModProjectile is IDrawOverTileMask maskDraw && maskDraw.MaskDrawActive)
                {
                    if (maskDraw.UsesSolidMask)
                        renderQueue.Add(maskDraw);
                    if (maskDraw.UsesNonsolidMask)
                        renderNonsolidQueue.Add(maskDraw);
                }
            }
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (npc.active && npc.ModNPC is IDrawOverTileMask maskDraw && maskDraw.MaskDrawActive)
                {
                    if (maskDraw.UsesSolidMask)
                        renderQueue.Add(maskDraw);
                    if (maskDraw.UsesNonsolidMask)
                        renderNonsolidQueue.Add(maskDraw);
                }
            }

            refreshTarget = renderQueue.Count > 0;
            refreshedNonsolidTarget = renderNonsolidQueue.Count > 0;

            if (!refreshTarget && !refreshedNonsolidTarget)
                return;

            if (refreshTarget)
            {
                RenderTargetsManager.SwitchToRenderTarget(solidTilesTarget);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null);
                Main.spriteBatch.Draw(Main.instance.tileTarget, Main.sceneTilePos - Main.screenPosition, Color.White);
                Main.spriteBatch.End();

                RenderTargetsManager.SwitchToRenderTarget(maskTarget);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);

                for (int i = 0; i < renderQueue.Count; i++)
                    renderQueue[i].DrawOverMask(Main.spriteBatch, true);

                Main.spriteBatch.End();
                renderQueue.Clear();
            }

            if (refreshedNonsolidTarget)
            {
                RenderTargetsManager.SwitchToRenderTarget(nonsolidTilesTarget);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null);
                Main.spriteBatch.Draw(Main.instance.tile2Target, Main.sceneTile2Pos - Main.screenPosition, Color.White);
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
                RenderTargetsManager.SwitchToRenderTarget(maskNonsolidTarget);

                for (int i = 0; i < renderNonsolidQueue.Count; i++)
                    renderNonsolidQueue[i].DrawOverMask(Main.spriteBatch, false);

                Main.spriteBatch.End();
                renderNonsolidQueue.Clear();
            }
        }

        private void DrawSolidMask(On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);

            if (!refreshTarget || maskTarget == null || solidTilesTarget == null)
                return;

            Effect effect = Scene["LayerMask"].GetShader().Shader;

            if (effect is null)
                return;


            effect.Parameters["mask"].SetValue(solidTilesTarget);

            //GRAHHHH DOESNT WORK WELL THERES A JITTER WHYYYY
            /*
            Vector2 screenDiff = Main.screenPosition - Main.screenLastPosition;
            Vector2 drawCorner = (Main.instance.tileTarget.Size() - maskTarget.Size()) / 2f + screenDiff;
            Vector2 offset = drawCorner / Main.instance.tileTarget.Size();
            effect.Parameters["maskOffset"].SetValue(offset);
            effect.Parameters["maskScale"].SetValue(maskTarget.Size() / Main.instance.tileTarget.Size());
            */

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(maskTarget, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);
            Main.spriteBatch.End();
        }

        private void DrawNonsolidTarget()
        {
            if (!refreshedNonsolidTarget || maskNonsolidTarget == null || nonsolidTilesTarget == null)
                return;

            Effect effect = Scene["LayerMask"].GetShader().Shader;
            if (effect is null)
                return;
            effect.Parameters["mask"].SetValue(nonsolidTilesTarget);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(maskNonsolidTarget, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);
            Main.spriteBatch.End();
        }
    }
}