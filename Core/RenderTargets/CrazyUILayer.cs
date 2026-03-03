using CalamityFables.Content.UI;

namespace CalamityFables.Core
{
    public class CrazyUIDrawingSystem : ModSystem
    {
        public static RenderTarget2D MainRenderTarget;
        public static RenderTarget2D PixelationTarget;
        public static RenderTarget2D DecalTarget;

        public override void Load()
        {
            RenderTargetsManager.ResizeRenderTargetEvent += ResizeRenderTarget;
            RenderTargetsManager.DrawToRenderTargetsEvent += DrawToRenderTarget;

            if (Main.dedServ)
                return;
            ResizeRenderTarget();
        }

        private void DrawToRenderTarget()
        {
            if (CoolDialogueUIManager.userInterface.CurrentState == null)
                return;

            if (!TurnOnRenderTarget(MainRenderTarget))
                return;

            //Draw the NPC portrait to the renderTarget
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null);
            CoolDialogueUIManager.theUI.mainBox.DrawUnpixelatedBackground(Main.spriteBatch);
            Main.spriteBatch.End();
            Main.graphics.GraphicsDevice.SetRenderTargets(null);

            if (!TurnOnRenderTarget(PixelationTarget))
                return;

            //Draws the main box and the buttons
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null);
            CoolDialogueUIManager.theUI.mainBox.DrawPixelated(Main.spriteBatch);
            CoolDialogueUIManager.theUI.DrawButtonsPixelated(Main.spriteBatch);

            Main.spriteBatch.End();
            Main.graphics.GraphicsDevice.SetRenderTargets(null);

            Color selectedButtonColor = CoolDialogueUIManager.theUI.SelectedButtonColor;

            //Draws the decal to the decal RT
            if (selectedButtonColor != Color.Transparent)
            {
                if (!TurnOnRenderTarget(DecalTarget))
                    return;

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null);
                CoolDialogueUIManager.theUI.DrawButtonsDecals(Main.spriteBatch);
                Main.spriteBatch.End();
                Main.graphics.GraphicsDevice.SetRenderTargets(null);
            }

            //Composite everything together
            if (!TurnOnRenderTarget(MainRenderTarget, false))
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null);
            Main.spriteBatch.Draw(PixelationTarget, Vector2.Zero, null, Color.White, 0, new Vector2(0, 0), 2f, SpriteEffects.None, 0);

            //Draws the decals on the buttons
            if (selectedButtonColor != Color.Transparent)
            {
                Effect effect = Scene["UIButtonDecal"].GetShader().Shader;
                effect.Parameters["greenScreenColor"].SetValue(selectedButtonColor.ToVector4());
                effect.Parameters["canvas"].SetValue(PixelationTarget);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect);

                Main.spriteBatch.Draw(DecalTarget, Vector2.Zero, null, Color.White, 0, new Vector2(0, 0), 2f, SpriteEffects.None, 0);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null);
            }

            //Draws the text in the textbox, the NPC name, and the button labels
            CoolDialogueUIManager.theUI.mainBox.DrawUnpixelatedForeground(Main.spriteBatch);
            CoolDialogueUIManager.theUI.DrawButtonsLabels(Main.spriteBatch);

            Main.spriteBatch.End();
            Main.graphics.GraphicsDevice.SetRenderTargets(null);
        }

        private void ResizeRenderTarget()
        {
            Main.QueueMainThreadAction(() =>
            {
                if (MainRenderTarget != null && !MainRenderTarget.IsDisposed)
                    MainRenderTarget.Dispose();

                MainRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            });
            Main.QueueMainThreadAction(() =>
            {
                if (PixelationTarget != null && !PixelationTarget.IsDisposed)
                    PixelationTarget.Dispose();

                PixelationTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
            });
            Main.QueueMainThreadAction(() =>
            {
                if (DecalTarget != null && !DecalTarget.IsDisposed)
                    DecalTarget.Dispose();

                DecalTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2);
            });
        }

        public static bool TurnOnRenderTarget(RenderTarget2D target, bool clear = true)
        {
            GraphicsDevice gD = Main.graphics.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;

            if (Main.gameMenu || Main.dedServ || spriteBatch is null || target is null || gD is null)
                return false;


            gD.SetRenderTarget(target);

            if (clear)
                gD.Clear(Color.Transparent);

            return true;
        }
    }
}