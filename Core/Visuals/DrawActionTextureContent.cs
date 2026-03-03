using CalamityFables.Content.Boss.SeaKnightMiniboss;

namespace CalamityFables.Core
{
    /// <summary>
    /// A render target that can be used to draw anything
    /// </summary>
    public class DrawActionTextureContent : ARenderTargetContentByRequest
    {
        public delegate void RTAction(SpriteBatch spriteBatch);
        public RTAction drawAction;
        public Point size;
        public BlendState blendState;
        public bool startSpritebatch;

        public DrawActionTextureContent(RTAction drawAction, int width, int height, BlendState blend = null, bool startSpritebatch = true)
        {
            this.drawAction = drawAction;
            size = new Point(width, height);
            blendState = blend ?? BlendState.AlphaBlend;
            this.startSpritebatch = startSpritebatch;
        }

        protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
        {
            PrepareARenderTarget_AndListenToEvents(ref _target, device, size.X, size.Y, RenderTargetUsage.DiscardContents);
            device.SetRenderTarget(_target);
            device.Clear(Color.Transparent);
            if (startSpritebatch)
                spriteBatch.Begin(SpriteSortMode.Deferred, blendState);
            drawAction(spriteBatch);
            if (startSpritebatch)
                spriteBatch.End();
            device.SetRenderTarget(null);
            _wasPrepared = true;
        }
    }
}