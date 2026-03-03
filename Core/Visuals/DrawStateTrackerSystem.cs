namespace CalamityFables.Core
{
    public class DrawStateTrackerSystem : ModSystem
    {
        public static bool drawingBossHPBar;
        public static bool drawingMap;
        public static bool drawingCachedProjectiles;

        public override void Load()
        {
            Terraria.On_Main.DrawInterface_15_InvasionProgressBars += TrackBosshpBarDrawing;
            Terraria.On_Main.DrawMap += TrackMapDrawing;
            Terraria.On_Main.DrawCachedProjs += TrackCachedProjectiles;
        }

        private void TrackCachedProjectiles(Terraria.On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache, bool startSpriteBatch)
        {
            drawingCachedProjectiles = true;
            orig(self, projCache, startSpriteBatch);
            drawingCachedProjectiles = false;
        }

        private void TrackMapDrawing(Terraria.On_Main.orig_DrawMap orig, Main self, GameTime gameTime)
        {
            drawingMap = true;
            orig(self, gameTime);
            drawingMap = false;
        }

        private void TrackBosshpBarDrawing(Terraria.On_Main.orig_DrawInterface_15_InvasionProgressBars orig)
        {
            drawingBossHPBar = true;
            orig();
            drawingBossHPBar = false;
        }
    }
}