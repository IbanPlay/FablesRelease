using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumLanternItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Lantern");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumLantern>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 50);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(3).
                AddIngredient(ItemID.Torch).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumLantern : ModTile, ICustomPaintable
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

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
            DustType = DustID.DynastyWood;
            HitSound = SoundID.Shatter;

            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = false;
            Main.tileWaterDeath[Type] = false;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
            AdjTiles = new int[] { TileID.HangingLanterns };
            AddMapEntry(CommonColors.FurnitureLightYellow, Language.GetText("MapObject.Lantern"));
            FablesSets.CustomPaintedSprites[Type] = true;
        }

        public override void HitWire(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            j -= tile.TileFrameY / 18;

            for (int y = 0; y <= 1; y++)
            {
                tile = Main.tile[i, j + y];

                if (tile.TileFrameX == 0)
                    tile.TileFrameX = 18;
                else
                    tile.TileFrameX = 0;

                Wiring.SkipWire(i, j + y);
            }

            if (Main.dedServ)
                NetMessage.SendTileSquare(Main.myPlayer, i, j, 1, 2, TileChangeType.None);
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Main.tile[i, j].TileFrameY == 18 && Main.tile[i, j].TileFrameX == 0)
            {
                float colMult = 1 / 265f * CommonColors.WulfrumLightMultiplierWithFlickerOffset(i * 3 + j);

                r = CommonColors.WulfrumGreen.R * colMult;
                g = CommonColors.WulfrumGreen.G * colMult;
                b = CommonColors.WulfrumGreen.B * colMult;
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];

            if (tile.TileFrameX != 0 || tile.TileFrameY == 0)
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
