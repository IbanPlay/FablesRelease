using System.Reflection;
using static Terraria.GameContent.TilePaintSystemV2;

namespace CalamityFables.Core
{
    public interface ICustomPaintable
    {
        public bool PaintColorHasCustomTexture(int paintColor) => false;
        public string PaintedTexturePath(int paintColor);
    }

    public class TilePaintOverrideSystem : ModSystem
    {
        private static readonly FieldInfo tilePaintingTileRenders = typeof(TilePaintSystemV2).GetField("_tilesRenders", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo wallPaintingTileRenders = typeof(TilePaintSystemV2).GetField("_wallsRenders", BindingFlags.NonPublic | BindingFlags.Instance);

        private static IDictionary<TileVariationkey, TileRenderTargetHolder> tileRendersDictionary;
        private static IDictionary<WallVariationKey, WallRenderTargetHolder> wallRendersDictionary;

        public override void Load()
        {
            Terraria.GameContent.On_TilePaintSystemV2.RequestTile += HijackTileRequests;
            Terraria.GameContent.On_TilePaintSystemV2.RequestWall += HijackWallRequests;
            GetRendersDict();
        }

        public void GetRendersDict()
        {
            tileRendersDictionary = tilePaintingTileRenders.GetValue(Main.instance.TilePaintSystem) as IDictionary<TileVariationkey, TileRenderTargetHolder>;
            wallRendersDictionary = wallPaintingTileRenders.GetValue(Main.instance.TilePaintSystem) as IDictionary<WallVariationKey, WallRenderTargetHolder>;
        }

        private void HijackTileRequests(Terraria.GameContent.On_TilePaintSystemV2.orig_RequestTile orig, TilePaintSystemV2 self, ref TileVariationkey lookupKey)
        {
            //Only check if modded tile & in the custom paint set
            if (lookupKey.TileType > TileID.Count &&
                FablesSets.CustomPaintedSprites[lookupKey.TileType])
            {
                if (tileRendersDictionary == null)
                    GetRendersDict();

                if (!tileRendersDictionary.TryGetValue(lookupKey, out _))
                {
                    //Override the RT if not present already, and if the paint is overriden
                    if (ModContent.GetModTile(lookupKey.TileType) is ICustomPaintable paintedTile &&
                        paintedTile.PaintColorHasCustomTexture(lookupKey.PaintColor))
                    {
                        TileRenderTargetHolder targetHolder = new OverridenTileRenderTargetHolder
                        {
                            Key = lookupKey,
                            textureName = paintedTile.PaintedTexturePath(lookupKey.PaintColor)
                        };
                        tileRendersDictionary[lookupKey] = targetHolder;
                    }
                }
            }
            orig(self, ref lookupKey);
        }

        private void HijackWallRequests(Terraria.GameContent.On_TilePaintSystemV2.orig_RequestWall orig, TilePaintSystemV2 self, ref WallVariationKey lookupKey)
        {
            if (lookupKey.WallType > WallID.Count &&
                   FablesSets.CustomPaintedWalls[lookupKey.WallType])
            {
                if (wallRendersDictionary == null)
                    GetRendersDict();

                if (!wallRendersDictionary.TryGetValue(lookupKey, out _))
                {
                    //Override the RT if not present already, and if the paint is overriden
                    if (ModContent.GetModWall(lookupKey.WallType) is ICustomPaintable paintedTile &&
                        paintedTile.PaintColorHasCustomTexture(lookupKey.PaintColor))
                    {
                        WallRenderTargetHolder targetHolder = new OverridenWallRenderTargetHolder
                        {
                            Key = lookupKey,
                            textureName = paintedTile.PaintedTexturePath(lookupKey.PaintColor)
                        };
                        wallRendersDictionary[lookupKey] = targetHolder;
                    }
                }
            }

            orig(self, ref lookupKey);
        }

    }

    public class OverridenTileRenderTargetHolder : TileRenderTargetHolder
    {
        public string textureName;

        public override void Prepare()
        {
            Asset<Texture2D> asset = ModContent.Request<Texture2D>(textureName);
            asset.Wait?.Invoke();
            PrepareTextureIfNecessary(asset.Value);
        }
        public override void PrepareShader() { } //No shader gets applied!
    }

    public class OverridenWallRenderTargetHolder : WallRenderTargetHolder
    {
        public string textureName;

        public override void Prepare()
        {
            Asset<Texture2D> asset = ModContent.Request<Texture2D>(textureName);
            asset.Wait?.Invoke();
            PrepareTextureIfNecessary(asset.Value);
        }
        public override void PrepareShader() { } //No shader gets applied!
    }
}
