namespace CalamityFables.Core
{
    public interface IDrawPixelated
    {
        public DrawhookLayer layer {
            get;
        }

        public bool ShoulDrawPixelated => true;
        public void DrawPixelated(SpriteBatch spriteBatch);
    }

    public class PixelatedDrawingLayer : ModSystem
    {
        private static RenderTarget2D abovePlayerTarget;
        private static RenderTarget2D aboveProjectilesTarget;
        private static RenderTarget2D aboveNPCsTarget;
        private static RenderTarget2D aboveTilesTarget;
        private static RenderTarget2D behindTilesTarget;

        private static List<IDrawPixelated> behindTileDraws;
        private static List<IDrawPixelated> aboveTileDraws;
        private static List<IDrawPixelated> aboveNPCsDraws;
        private static List<IDrawPixelated> aboveProjectilesDraws;
        private static List<IDrawPixelated> foregroundDraws;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            RenderTargetsManager.ResizeRenderTargetEvent += ResizeRenderTarget;
            RenderTargetsManager.DrawToRenderTargetsEvent += DrawToRenderTarget;

            //Hooking on the diff layers
            FablesDrawLayers.DrawBehindDustEvent += DrawAbovePlayerTarget;
            FablesDrawLayers.DrawAboveProjectilesEvent += DrawAboveProjectilesTarget;
            FablesDrawLayers.DrawThingsAboveSolidTilesEvent += DrawAboveTilesTarget;
            FablesDrawLayers.DrawThingsBehindSolidTilesEvent += DrawUnderTilesTarget;
            FablesDrawLayers.DrawThingsAboveNPCsEvent += DrawAboveNPCsTarget;

            behindTileDraws = new List<IDrawPixelated>();
            aboveTileDraws = new List<IDrawPixelated>();
            aboveNPCsDraws = new List<IDrawPixelated>();
            aboveProjectilesDraws = new List<IDrawPixelated>();
            foregroundDraws = new List<IDrawPixelated>();
            ResizeRenderTarget();
        }
        
        /// <summary>
        /// Subscribe to this event to register any custom drawing task to be drawn with the pixelated render targets
        /// </summary>
        public static event Func<IDrawPixelated> AddToPixelQueueEvent;

        private void DrawToRenderTarget()
        {
            foregroundDraws.Clear();
            aboveProjectilesDraws.Clear();
            aboveNPCsDraws.Clear();
            aboveTileDraws.Clear();
            behindTileDraws.Clear();

            foreach (Particle particle in ParticleHandler.particles)
            {
                if (particle is IDrawPixelated pixelParticle && pixelParticle.ShoulDrawPixelated)
                    QueuePixelDrawing(pixelParticle);
            }

            foreach (GhostTrail lingeringTrail in GhostTrailsHandler.trails)
            {
                if (lingeringTrail is IDrawPixelated pixelTrail && pixelTrail.ShoulDrawPixelated)
                    QueuePixelDrawing(pixelTrail);
            }

            for (int i = 0; i < Main.projectile.Length; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.ModProjectile != null && proj.ModProjectile is IDrawPixelated pixelProjectile && pixelProjectile.ShoulDrawPixelated)
                    QueuePixelDrawing(pixelProjectile);
            }

            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.ModNPC != null && npc.ModNPC is IDrawPixelated pixelNPC && pixelNPC.ShoulDrawPixelated)
                    QueuePixelDrawing(pixelNPC);
            }

            if (AddToPixelQueueEvent != null)
            {
                foreach (Func<IDrawPixelated> eventEntry in AddToPixelQueueEvent.GetInvocationList())
                    QueuePixelDrawing(eventEntry.Invoke());
            }

            DrawQueuedEntitiesToTarget(abovePlayerTarget, foregroundDraws);
            DrawQueuedEntitiesToTarget(aboveProjectilesTarget, aboveProjectilesDraws);
            DrawQueuedEntitiesToTarget(aboveNPCsTarget, aboveNPCsDraws);
            DrawQueuedEntitiesToTarget(aboveTilesTarget, aboveTileDraws);
            DrawQueuedEntitiesToTarget(behindTilesTarget, behindTileDraws);

            Main.graphics.GraphicsDevice.SetRenderTargets(null);
        }

        public void QueuePixelDrawing(IDrawPixelated pixelDrawer)
        {
            switch (pixelDrawer.layer)
            {
                case DrawhookLayer.AbovePlayer:
                    foregroundDraws.Add(pixelDrawer);
                    break;
                case DrawhookLayer.AboveProjectiles:
                    aboveProjectilesDraws.Add(pixelDrawer);
                    break;
                case DrawhookLayer.AboveNPCs:
                    aboveNPCsDraws.Add(pixelDrawer);
                    break;
                case DrawhookLayer.AboveTiles:
                    aboveTileDraws.Add(pixelDrawer);
                    break;
                case DrawhookLayer.BehindTiles:
                    behindTileDraws.Add(pixelDrawer);
                    break;
            }
        }

        public void DrawQueuedEntitiesToTarget(RenderTarget2D target, List<IDrawPixelated> queuedEntities)
        {
            if (queuedEntities.Count != 0)
            {
                SwapToTarget(target);
                RenderTargetsManager.NoViewMatrixPrims = true;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null);
                //Do this for prim reasons
                Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;


                foreach (IDrawPixelated primDraw in queuedEntities)
                    primDraw.DrawPixelated(Main.spriteBatch);

                Main.spriteBatch.End();
                RenderTargetsManager.NoViewMatrixPrims = false;
            }
        }

        private void ResizeRenderTarget()
        {
            Main.QueueMainThreadAction(() =>
            {
                if (abovePlayerTarget != null && !abovePlayerTarget.IsDisposed)
                    abovePlayerTarget.Dispose();
                if (aboveProjectilesTarget != null && !aboveProjectilesTarget.IsDisposed)
                    aboveProjectilesTarget.Dispose();
                if (aboveNPCsTarget != null && !aboveNPCsTarget.IsDisposed)
                    aboveNPCsTarget.Dispose();
                if (aboveTilesTarget != null && !aboveTilesTarget.IsDisposed)
                    aboveTilesTarget.Dispose();
                if (behindTilesTarget != null && !behindTilesTarget.IsDisposed)
                    behindTilesTarget.Dispose();

                abovePlayerTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
                aboveProjectilesTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
                aboveNPCsTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
                aboveTilesTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
                behindTilesTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
            });
        }

        private void DrawAbovePlayerTarget()
        {
            if (foregroundDraws.Count > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(abovePlayerTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }
        }
        private void DrawAboveProjectilesTarget()
        {
            if (aboveProjectilesDraws.Count > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(aboveProjectilesTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }
        }


        private void DrawUnderTilesTarget()
        {
            if (behindTileDraws.Count > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(behindTilesTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }
        }

        private void DrawAboveTilesTarget()
        {
            if (aboveTileDraws.Count > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(aboveTilesTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }
        }

        private void DrawAboveNPCsTarget()
        {
            if (aboveNPCsDraws.Count > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(aboveNPCsTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }
        }


        public static bool SwapToTarget(RenderTarget2D rt)
        {
            GraphicsDevice gD = Main.graphics.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;

            if (Main.gameMenu || Main.dedServ || spriteBatch is null || rt is null || gD is null)
                return false;

            gD.SetRenderTarget(rt);
            gD.Clear(Color.Transparent);
            return true;
        }
    }
}