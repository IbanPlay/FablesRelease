using Terraria.GameContent.UI.ResourceSets;

namespace CalamityFables.Core.Visuals
{
    public class FablesResourceOverlay : ModResourceOverlay
    {
        public enum ResourceType
        {
            Classic,            //Classic star
            Fancy,          //Fancy star
            BarPanel,           //Corner of the bar UI
            BarFill,            //Fill in the bar
            Other
        }

        #region Loading the vanilla textures we compare it to
        private static Asset<Texture2D> Fancy_Star;
        private static Asset<Texture2D> Bars_PanelRight;
        private static Asset<Texture2D> Bars_Fill;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                Fancy_Star = Main.Assets.Request<Texture2D>("Images/UI/PlayerResourceSets/FancyClassic/Star_Fill");
                Bars_PanelRight = Main.Assets.Request<Texture2D>("Images/UI/PlayerResourceSets/HorizontalBars/MP_Panel_Right");
                Bars_Fill = Main.Assets.Request<Texture2D>("Images/UI/PlayerResourceSets/HorizontalBars/MP_Fill");
            }
        }
        #endregion

        public delegate bool ShouldDrawManaStarOverlayDelegate(PlayerStatsSnapshot snapshot, bool drawingLife);
        public delegate void DrawManaStarOverlayDelegate(ResourceOverlayDrawContext context);

        internal static ShouldDrawManaStarOverlayDelegate ShouldDrawManaStarFunc;
        internal static DrawManaStarOverlayDelegate DrawManaStarOverlayAction;
        internal static float CurrentOverlayPriority;

        /// <summary>
        /// Sets a max mana overlay for the resource bars with the provided textures
        /// </summary>
        /// <param name="overlay">Texture drawn over the final star in the resource bar</param>
        /// <param name="outline">Texture drawn around the final few stars as a glowing pulsing outline</param>
        /// <param name="priority">Priority for this overlay in case multiple overlays try to draw at once</param>
        public static void SetOverlay(ShouldDrawManaStarOverlayDelegate shouldDrawManaStarFunc, DrawManaStarOverlayDelegate drawManaStarOverlayAction, float priority = 1f)
        {
            if (priority < CurrentOverlayPriority)
                return;

            CurrentOverlayPriority = priority;
            ShouldDrawManaStarFunc = shouldDrawManaStarFunc;
            DrawManaStarOverlayAction = drawManaStarOverlayAction;
        }

        private static bool DrawOverlay = false;
        public override bool PreDrawResourceDisplay(PlayerStatsSnapshot snapshot, IPlayerResourcesDisplaySet displaySet, bool drawingLife, ref Color textColor, out bool drawText)
        {
            // Determine if the overlay should be drawn
            DrawOverlay = ShouldDrawManaStarFunc != null ? ShouldDrawManaStarFunc(snapshot, drawingLife) && CurrentOverlayPriority > 0 : false;

            drawText = true;
            return true;
        }


        public override void PostDrawResource(ResourceOverlayDrawContext context)
        {
            if (!DrawOverlay)
                return;

            // Draw from action
            DrawManaStarOverlayAction?.Invoke(context);
        }


        public static ResourceType GetAssetType(Asset<Texture2D> asset)
        {
            if (asset == TextureAssets.Mana)
                return ResourceType.Classic;
            if (asset == Fancy_Star)
                return ResourceType.Fancy;
            if (asset == Bars_Fill)
                return ResourceType.BarFill;
            if (asset == Bars_PanelRight)
                return ResourceType.BarPanel;
            return ResourceType.Other;
        }
    }
}