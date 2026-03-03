using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumCandleItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Candle");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumCandle>());
            Item.maxStack = 9999;
            Item.value = 0;
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(2).
                AddIngredient(ItemID.Torch).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumCandle : ModTile, ICustomPaintable
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);


        public static int AnimFrame;

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public static Asset<Texture2D> GlowMask;

        public override void Load()
        {
            if (!Main.dedServ)
                GlowMask = Request<Texture2D>(Texture + "Glow");
        }

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.CursedTorch;
            HitSound = SoundID.Shatter;

            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = false;
            Main.tileWaterDeath[Type] = false;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.Table | AnchorType.SolidSide, 1, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 18 };
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
            AdjTiles = new int[] { TileID.Candles };

            AddMapEntry(CommonColors.WulfrumGreen);
            FablesSets.CustomPaintedSprites[Type] = true;
        }

        public override void HitWire(int i, int j)
        {
            Tile tile = Main.tile[i, j];

            if (tile.TileFrameX == 0)
                tile.TileFrameX = 18;
            else
                tile.TileFrameX = 0;

            Wiring.SkipWire(i, j);

            if (Main.dedServ)
                NetMessage.SendTileSquare(Main.myPlayer, i, j, TileChangeType.None);
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ItemType<WulfrumCandleItem>();
        }

        public override bool RightClick(int i, int j)
        {

            Tile tile = Main.tile[i, j];
            if (tile.TileFrameX == 0)
                tile.TileFrameX = 18;
            else
                tile.TileFrameX = 0;

            if (Main.dedServ)
                NetMessage.SendTileSquare(Main.myPlayer, i, j, TileChangeType.None);

            //WorldGen.KillTile(i, j, false, false, false);
            //if (!Main.tile[i, j].HasTile && Main.netMode != NetmodeID.SinglePlayer)
            //    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, (float)i, (float)j, 0f, 0, 0, 0);

            return true;
        }


        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Main.tile[i, j].TileFrameX != 0)
                return;

            float colorMult = 1 / 265f * CommonColors.WulfrumLightMultiplierWithFlickerOffset(i * 3 + j);
            r = CommonColors.WulfrumGreen.R * colorMult;
            g = CommonColors.WulfrumGreen.G * colorMult;
            b = CommonColors.WulfrumGreen.B * colorMult;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            if (tile.TileFrameX != 0)
                return;
            int xPos = tile.TileFrameX;
            int yPos = tile.TileFrameY;


            Texture2D glowmask = GlowMask.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero;

            Color drawColour = GetDrawColour(i, j, Color.White) * CommonColors.WulfrumLightMultiplierWithFlickerOffset(i * 3 + j);
            Main.spriteBatch.Draw(glowmask, drawOffset, new Rectangle(xPos, yPos, 18, 18), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);

            float widthOscillate = 2f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
            drawColour *= 0.6f;
            drawColour.A = 0;

            for (int x = -1; x <= 1; x += 2)
                Main.spriteBatch.Draw(glowmask, drawOffset + Vector2.UnitX * x * widthOscillate, new Rectangle(xPos, yPos, 18, 18), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
        }

        private Color GetDrawColour(int i, int j, Color colour)
        {
            int colType = Main.tile[i, j].TileColor;
            Color paintCol = WorldGen.paintColor(colType);
            if (colType >= 13 && colType <= 24)
            {
                colour.R = (byte)(paintCol.R / 255f * colour.R);
                colour.G = (byte)(paintCol.G / 255f * colour.G);
                colour.B = (byte)(paintCol.B / 255f * colour.B);
            }
            return colour;
        }
    }
}
