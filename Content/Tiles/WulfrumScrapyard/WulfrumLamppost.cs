using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class WulfrumLamppostItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Lamppost");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumLamppost>());
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<DullPlatingItem>(3).
                AddIngredient(ItemID.Glass, 2).
                AddIngredient(ItemID.Torch).
                AddTile<BunkerWorkshop>().
                Register();
        }
    }

    public class WulfrumLamppost : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

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

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1xX);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Height = 8;
            TileObjectData.newTile.DrawYOffset = 0;
            TileObjectData.newTile.Origin = new Point16(0, 7);
            TileObjectData.newTile.RandomStyleRange = 4;
            TileObjectData.newTile.StyleMultiplier = 4;
            TileObjectData.newTile.CoordinateHeights =[ 16, 16, 16, 16, 16, 16, 16, 16, 18 ];
            TileObjectData.newTile.CoordinateWidth = 24;
            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
            AdjTiles = new int[] { TileID.Lamps };
            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Lamppost");
            AddMapEntry(CommonColors.FurnitureLightYellow, name);
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Main.tile[i, j].TileFrameY <= 18 && Main.tile[i, j].TileFrameX <= 26)
            {
                float colMult = 1 / 265f * CommonColors.WulfrumLightMultiplierWithFlickerOffset(i * 3 + j - Main.tile[i, j].TileFrameY / 18);

                r = CommonColors.WulfrumGreen.R * colMult;
                g = CommonColors.WulfrumGreen.G * colMult;
                b = CommonColors.WulfrumGreen.B * colMult;
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];

            int xPos = tile.TileFrameX;
            int yPos = tile.TileFrameY + 144;

            Texture2D glowmask = TextureAssets.Tile[Type].Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X - 4, j * 16 - Main.screenPosition.Y) + zero;
            Color drawColour = GetDrawColour(i, j, Color.White) * CommonColors.WulfrumLightMultiplierWithFlickerOffset(i * 3 + j - Main.tile[i, j].TileFrameY / 18);
            Main.spriteBatch.Draw(glowmask, drawOffset, new Rectangle(xPos, yPos, 24, 18), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);

            float widthOscillate = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
            drawColour *= 0.1f;
            drawColour.A = 0;

            for (int x = -1; x <= 1; x += 2)
                Main.spriteBatch.Draw(glowmask, drawOffset + Vector2.UnitX * x * widthOscillate, new Rectangle(xPos, yPos, 24, 18), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
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
