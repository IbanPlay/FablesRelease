using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Core
{
    public class BannerLoader
    {
        public static Mod Mod => CalamityFables.Instance;

        public static int LoadBanner(string NPCName, string NPCDisplayName, string texturePath, out AutoloadedBanner bannerTile, Color? mapColor = null, int killCount = 50)
        {
            //Load the banner item but cache it first
            AutoloadedBannerItem bannerItem = new AutoloadedBannerItem(NPCName, NPCDisplayName, texturePath, killCount);
            Mod.AddContent(bannerItem);

            //Load the tile using the item's tile type
            bannerTile = new AutoloadedBanner(NPCName, bannerItem.Type, texturePath, mapColor);
            Mod.AddContent(bannerTile);

            //Set the banner item's type to be the one of the banner tile (so it can properly place it)
            bannerItem.TileType = bannerTile.Type;

            //Return the banner item's type so that npcs can drop it
            return bannerItem.Type;
        }
    }

    [Autoload(false)]
    public class AutoloadedBanner : ModTile, ICustomLayerTile
    {
        public override string Texture => TexturePath + Name;
        public override string Name => InternalName != "" ? InternalName : base.Name;

        public string InternalName;
        protected readonly int ItemType;
        public int NPCType = -1;
        protected readonly Color? MapColor;
        protected readonly string TexturePath;

        public AutoloadedBanner(string NPCname, int dropType, string path = null, Color? mapColor = null)
        {
            InternalName = NPCname + "Banner";
            ItemType = dropType;
            MapColor = mapColor;
            TexturePath = path;
        }

        public override void SetStaticDefaults()
        {
            //FablesSets.SwayingBanners[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.CoordinateHeights = Enumerable.Repeat(16, 3).ToArray();
            TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.SolidBottom | AnchorType.PlanterBox, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.DrawYOffset = -2;

            //Attached to platform
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.Platform, TileObjectData.newTile.Width, 0);
            TileObjectData.newAlternate.DrawYOffset = -10;
            TileObjectData.addAlternate(0);
            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Banner");
            AddMapEntry(MapColor ?? new Color(13, 88, 130), name);
            DustType = -1;
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (NPCType == -1)
                return;

            Main.SceneMetrics.hasBanner = true;
            Main.SceneMetrics.NPCBannerBuff[NPCType] = true;
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
           
        }


        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            if (t.TileFrameX == 0 && t.TileFrameY == 0)
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.PostDrawTiles, true);
            return false;
        }

        public void DrawSpecialLayer(int tileX, int tileY, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            Tile baseTile = Main.tile[tileX, tileY];
            Vector2 screenPosition = Main.Camera.UnscaledPosition;
            int type = baseTile.TileType;
            int width = 1;
            int height = 3;

            float windCycle = Main.instance.TilesRenderer.GetWindCycle(tileX, tileY, WindHelper.sunflowerWindCounter);
            float originalWindCycle = windCycle;

            int totalPushTime = 60;
            float pushForcePerFrame = 1.26f;
            float highestWindGridPushComplex = WindHelper.GetHighestWindGridPushComplex(tileX, tileY, width, height, totalPushTime, pushForcePerFrame, 3, true);
            windCycle += highestWindGridPushComplex;

            Vector2 baseDrawPos = new Vector2(tileX * 16 + 8, tileY * 16) - screenPosition;
            Vector2 tileDataOffset = -Vector2.UnitY * 2; //This emulates the draw offset from the TileObjectData stuff: By default pushed up 2px up
            //Push the offset to -10 pixels up if there's a platform above, to match the alternate placement style's offset
            if (WorldGen.IsBelowANonHammeredPlatform(tileX, tileY))
                tileDataOffset.Y -= 8f;

            baseDrawPos += tileDataOffset;

            //Remove the base wind from the wind offset, retaining only the player push
            if (!WorldGen.InAPlaceWithWind(tileX, tileY, 1, 3))
                windCycle -= originalWindCycle;

            for (int j = tileY; j < tileY + height; j++)
            {
                Tile t = Main.tile[tileX, j];
                ushort currentTileType = t.TileType;
                if (currentTileType != type || !TileDrawing.IsVisible(t))
                    continue;

                short tileFrameX = t.TileFrameX;
                short tileFrameY = t.TileFrameY;
                float heightAlongRope = (float)(j - tileY + 1) / height;

                Color tileLight = Lighting.GetColor(tileX, j);
                if (t.IsTileFullbright)
                    tileLight = Color.White;

                Vector2 currentTileDrawPos = new Vector2(tileX * 16, j * 16) - screenPosition;
                currentTileDrawPos += tileDataOffset;

                Vector2 windModifier = new Vector2(windCycle, Math.Abs(windCycle) * -4f * heightAlongRope);
                Vector2 offsetFromOrigin = baseDrawPos - currentTileDrawPos;
                Texture2D tileDrawTexture = Main.instance.TilesRenderer.GetTileDrawTexture(t, tileX, j);
                if (tileDrawTexture != null)
                {
                    Vector2 finalDrawPosition = baseDrawPos + Vector2.UnitY * windModifier;
                    Rectangle drawFrame = new Rectangle(tileFrameX, tileFrameY , 16, 16);
                    float rotation = windCycle * -0.15f * heightAlongRope;
                    Main.spriteBatch.Draw(tileDrawTexture, finalDrawPosition, drawFrame, tileLight, rotation, offsetFromOrigin, 1f, 0, 0f);
                }
            }
        }

    }


    [Autoload(false)]
    public class AutoloadedBannerItem : ModItem
    {
        public string InternalName = "";
        public string Itemname;
        public string Itemtooltip;
        public int KillsPerBanner;
        public int TileType;

        private readonly string Tilename;
        private readonly string TexturePath;

        protected override bool CloneNewInstances => true;

        public override string Name => InternalName != "" ? InternalName : base.Name;
        public override string Texture => string.IsNullOrEmpty(TexturePath) ? AssetDirectory.DebugSquare : TexturePath + Name;

       //public override LocalizedText Tooltip => Language.GetText("CommonItemTooltip.BannerBonus");

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault(Itemname ?? "ERROR");
            Tooltip.SetDefault(Itemtooltip ?? "Report me please!");
            ItemID.Sets.KillsToBanner[Type] = KillsPerBanner;
        }

        public override void SetDefaults()
        {
            if (Tilename is null)
                return;

            Item.width = 12;
            Item.height = 30;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = TileType;
            Item.rare = ItemRarityID.Blue;
            Item.value = 1000;
        }

        public AutoloadedBannerItem(string NPCName, string NPCDisplayName, string texturePath, int killsNeeded = 50)
        {
            InternalName = NPCName + "BannerItem";
            Itemname = NPCDisplayName + " Banner";
            Itemtooltip = "{$CommonItemTooltip.BannerBonus}" + NPCDisplayName;
            Tilename = NPCName + "Banner";
            TexturePath = texturePath;
            KillsPerBanner = killsNeeded;
        }
    }
}
