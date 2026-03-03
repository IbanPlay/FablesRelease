namespace CalamityFables.Core.RenderTargets
{
    public class EasyRenderTarget
    {
        public RenderTarget2D RenderTarget { get; set; }

        /// <summary>
		/// The method called whenever this render target is rendered.
		/// </summary>
		public Action<SpriteBatch> DrawAction;

        /// <summary>
        /// A function that determines when this render target is considered active. Should be as restrictive as possible to limit unnecessary drawing.
        /// </summary>
        public Func<bool> Active;

        /// <summary>
        /// Determines the size of this render target. Ran whenever the target is initialized. Return null to use screen size.
        /// </summary>
        public Func<Point?> SizeFunction;

        /// <summary>
        /// Determines if this render target will be fully deleted when inactive. Useful for temporary targets that won't be reused.
        /// </summary>
        public bool RemoveWhenInactive = false;

        public int AutoDisposeTime => 120;
        public int TimeSinceLastUpdate = 0;
        public bool Initialized = false;

        public EasyRenderTarget(Action<SpriteBatch> drawAction, Func<bool> active, Func<Point?> sizeFunction = null, bool removeWhenInactive = false)
        {
            if (Main.dedServ)
                return;

            DrawAction = drawAction;
            Active = active ?? (() => true);
            SizeFunction = sizeFunction;
            RemoveWhenInactive = removeWhenInactive;

            EasyRenderTargetHandler.AddTarget(this);
        }

        public void Initialize()
        {
            Initialized = true;
            Point size = SizeFunction is null ? Main.ScreenSize : SizeFunction() ?? Main.ScreenSize;
            Main.QueueMainThreadAction(() => RenderTarget = new RenderTarget2D(Main.instance.GraphicsDevice, size.X, size.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents));
        }

        public void Dispose()
        {
            Initialized = false;
            RenderTarget.Dispose();
        }

        /// <summary>
		/// Resizes the render target to the given dimensions.
		/// </summary>
		/// <param name="size"></param>
		public void Resize(Point size)
        {
            if (Main.dedServ)
                return;

            RenderTarget.Dispose();
            RenderTarget = new RenderTarget2D(Main.instance.GraphicsDevice, size.X, size.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }
    }

    public class EasyRenderTargetHandler : ModSystem
    {
        internal static List<EasyRenderTarget> Targets;

        public override void Load()
        {
            Targets = [];

            RenderTargetsManager.DrawToRenderTargetsEvent += DrawToRenderTarget;
            RenderTargetsManager.ResizeRenderTargetEvent += ResizeRenderTargets;
        }

        public override void Unload()
        {
            Targets = null;

            RenderTargetsManager.DrawToRenderTargetsEvent -= DrawToRenderTarget;
            RenderTargetsManager.ResizeRenderTargetEvent -= ResizeRenderTargets;
        }

        /// <summary>
        /// Attempts to add to add the specified render target to the current list of active targets.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool TryAddTarget(EasyRenderTarget target)
        {
            if (!Targets.Contains(target) && target != null)
            {
                AddTarget(target);
                return true;
            }

            return false;
        }

        public static void AddTarget(EasyRenderTarget target) => Targets.Add(target);

        public static void RemoveTarget(EasyRenderTarget target)
        {
            if (target.Initialized)
                target.Dispose();
            Targets.Remove(target);
        }

        public override void PreUpdateItems()
        {
            if(Main.dedServ || Targets.Count <= 0)
                return;

            for (int i = Targets.Count - 1; i >= 0; i--)
            {
                var target = Targets[i];
                if (target is null)
                    return;

                // Initialize active targets that havent been initialized yet
                if (target.Active() && !target.Initialized)
                    target.Initialize();

                // Update time since last update
                if (target.Active())
                    target.TimeSinceLastUpdate = 0;
                else
                {
                    target.TimeSinceLastUpdate++;

                    // Dispose when a target has been inactive for too long. Fully remove if marked to do so
                    if (target.TimeSinceLastUpdate >= target.AutoDisposeTime && target.Initialized)
                    {
                        target.Dispose();
                        if (target.RemoveWhenInactive)
                            RemoveTarget(target);
                    }
                }
            }
        }

        private static void DrawToRenderTarget()
        {
            if (Main.dedServ || Targets.Count <= 0)
                return;

            // Create enumberable of active targets
            EasyRenderTarget[] activeTargets = [.. Targets.Where(target => target != null && target.Active())];

            // Stop if there are no active targets
            if (activeTargets.Length == 0)
                return;

            // Track current bindings 
            RenderTargetBinding[] bindings = Main.graphics.GraphicsDevice.GetRenderTargets();

            SpriteBatch spriteBatch = Main.spriteBatch;

            for (int i = activeTargets.Length - 1; i >= 0; i--)
            {
                var target = activeTargets[i];
                if (target is null || !target.Initialized || !target.Active())
                    continue;

                // Switch to each target and run their draw action
                RenderTargetsManager.SwitchToRenderTarget(target.RenderTarget);
                RenderTargetsManager.NoViewMatrixPrims = true;

                target.DrawAction(spriteBatch);

                RenderTargetsManager.NoViewMatrixPrims = false;
            }

            // Reset render target after drawing
            Main.graphics.GraphicsDevice.SetRenderTargets(bindings);
        }

        private static void ResizeRenderTargets()
        {
            // Stop if there are no targets
            if (Main.dedServ || Targets.Count <= 0)
                return;

            // Resize each target
            Main.QueueMainThreadAction(() =>
            {
                for (int i = Targets.Count - 1; i >= 0; i--)
                {
                    var target = Targets[i];
                    if (target is null || !target.Initialized)
                        return;

                    Point newSize = target.SizeFunction is null ? Main.ScreenSize : target.SizeFunction() ?? Main.ScreenSize;
                    target.Resize(newSize);
                }
            });
        }
    }
}