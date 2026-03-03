namespace CalamityFables.Core
{
    interface IForegroundTile
    {
        void ForegroundDraw(int x, int y, SpriteBatch spriteBatch);
    }

    public class ForegroundManager : ModSystem
    {
        private static List<Point> _foregroundElements;
        private static int _foregroundElementCount;
        private static List<Point> _nonSolidforegroundElements;
        private static int _nonSolidforegroundElementCount;

        public override void Load()
        {
            _foregroundElements = new List<Point>(1000);
            _foregroundElementCount = 0;
            _nonSolidforegroundElements = new List<Point>(1000);
            _nonSolidforegroundElementCount = 0;

            Terraria.On_Main.DrawGore += DrawForegroundStuff;
            FablesDrawLayers.ClearTileDrawingCachesEvent += ClearTiles;
        }

        private static void DrawForegroundStuff(On_Main.orig_DrawGore orig, Main self)
        {
            orig(self);
            if (Main.PlayerLoaded && !Main.gameMenu)
                DrawTiles();
        }

        public static void AddForegroundDrawingPoint(int x, int y, bool nonSolid = false)
        {
            if (nonSolid)
            {
                _nonSolidforegroundElements.Add(new Point(x, y));
                _nonSolidforegroundElementCount++;
            }
            else
            {
                _foregroundElements.Add(new Point(x, y));
                _foregroundElementCount++;
            }
        }

        public static void DrawTiles()
        {
            for (int i = 0; i < _nonSolidforegroundElementCount; i++)
            {
                ushort type = Main.tile[_nonSolidforegroundElements[i]].TileType;
                if (TileLoader.GetTile(type) is IForegroundTile fgTile)
                    fgTile.ForegroundDraw(_nonSolidforegroundElements[i].X, _nonSolidforegroundElements[i].Y, Main.spriteBatch);
            }

            for (int i = 0; i < _foregroundElementCount; i++)
            {
                ushort type = Main.tile[_foregroundElements[i]].TileType;
                if (TileLoader.GetTile(type) is IForegroundTile fgTile)
                    fgTile.ForegroundDraw(_foregroundElements[i].X, _foregroundElements[i].Y, Main.spriteBatch);
            }
        }

        public static void ClearTiles(bool nonSolid)
        {
            if (!nonSolid)
            {
                _foregroundElements.Clear();
                _foregroundElementCount = 0;
            }
            else
            {
                _nonSolidforegroundElements.Clear();
                _nonSolidforegroundElementCount = 0;
            }
        }
    }
}
