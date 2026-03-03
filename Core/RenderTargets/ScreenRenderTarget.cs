using CalamityFables.Content.Boss.SeaKnightMiniboss;

namespace CalamityFables.Core
{
    /// <summary>
    /// Represents a full-screen rendertarget that's automatically loaded into <see cref="RenderTargetsManager"/>
    /// </summary>
    public abstract class ScreenRenderTarget : ILoadable, IDisposable
    {
        public RenderTarget2D target;
        public int framesSinceLastDrawn;
        public virtual bool ShouldDraw => true;
        /// <summary>
        /// The size of the target compared to the screen's size <br/>
        /// Leave at 1 for a screen sized target, set to 2 for a half size pixelated target
        /// </summary>
        public virtual int targetSizeDivisor => 1;

        /// <summary>
        /// Disposes of the previous target if it exists, and recreates a new one with the desired size
        /// </summary>
        public void InitializeTarget()
        {
            Main.QueueMainThreadAction(() =>
            {
                if (target != null && !target.IsDisposed)
                    target.Dispose();

                target = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / targetSizeDivisor, Main.screenHeight / targetSizeDivisor);
            });
        }

        /// <summary>
        /// Draws everything you want on the target <br/>
        /// Returns wether or not anything was actually drawn on the target
        /// </summary>
        /// <returns></returns>
        public abstract bool DrawToTarget();
        

        /// <summary>
        /// Utility method that switches to the render target
        /// </summary>
        public bool SwitchToRenderTarget()
        {
            GraphicsDevice gD = Main.graphics.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;

            if (Main.gameMenu || Main.dedServ || spriteBatch is null || target is null || gD is null)
                return false;

            gD.SetRenderTarget(target);
            gD.Clear(Color.Transparent);
            return true;
        }

        public abstract void Load(Mod mod);
        public abstract void Unload();
        public void Dispose()
        {
            target?.Dispose();
            framesSinceLastDrawn = 0;
            GC.SuppressFinalize(this);
        }

        public static implicit operator RenderTarget2D(ScreenRenderTarget rt) => rt.target;
    }
}