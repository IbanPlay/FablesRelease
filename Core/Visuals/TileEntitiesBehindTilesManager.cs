using Terraria.DataStructures;

namespace CalamityFables.Core
{
    public interface IDrawBehindTiles
    {
        public void DrawBehindTiles(SpriteBatch spriteBatch) { }
        public bool IsOnScreen() => true;
    }

    public class BackgroundManager : ModSystem
    {
        public override void Load()
        {
            FablesDrawLayers.DrawThingsBehindSolidTilesAndBackgroundNPCsEvent += DrawBackgroundTEs;
        }

        private void DrawBackgroundTEs()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (var item in TileEntity.ByID)
            {
                if (item.Value is IDrawBehindTiles drawer && drawer.IsOnScreen())
                {
                    drawer.DrawBehindTiles(Main.spriteBatch);
                }
            }

            Main.spriteBatch.End();
        }
    }
}