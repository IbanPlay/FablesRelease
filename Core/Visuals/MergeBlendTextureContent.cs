using CalamityFables.Content.Boss.SeaKnightMiniboss;

namespace CalamityFables.Core
{
    /// <summary>
    /// A render target that can be used to draw things with the lighten blend mode
    /// </summary>
    public class MergeBlendTextureContent : ARenderTargetContentByRequest
    {
        public delegate void RTAction(SpriteBatch spriteBatch, bool backgroundPass);
        public RTAction drawAction;
        public Point size;
        public bool darken;
        public bool linearSampler = false;

        public MergeBlendTextureContent(RTAction drawAction, int width, int height, bool darken = false, bool linearSampler = false)
        {
            this.drawAction = drawAction;
            size = new Point(width, height);
            this.linearSampler = linearSampler;
            this.darken = darken;
        }

        protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
        {
            PrepareARenderTarget_AndListenToEvents(ref _target, device, size.X, size.Y, RenderTargetUsage.DiscardContents);
            device.SetRenderTarget(_target);
            device.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, linearSampler ? SamplerState.LinearClamp : SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            drawAction(spriteBatch, true);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Immediate, darken? CustomBlendStates.Darken : CustomBlendStates.Lighten, linearSampler ? SamplerState.LinearClamp : SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            drawAction(spriteBatch, false);
            spriteBatch.End();

            device.SetRenderTarget(null);
            _wasPrepared = true;
        }
    }
}