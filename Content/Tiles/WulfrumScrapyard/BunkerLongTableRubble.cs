using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.BurntDesert;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class BunkerLongTableRubble : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLighted[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 5;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(2, 2);
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.addTile(Type);

            DustType = DustID.Iron;
            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(114, 110, 101), name);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            Tile t = Main.tile[i, j];
            if (t.TileFrameY != 0)
                return;
            if ((t.TileFrameX >= 18 && t.TileFrameX < 72) || t.TileFrameX == 144 || t.TileFrameX == 198)
            {
                float colorMult = 1 / 265f * 0.6f * CommonColors.WulfrumLightMultiplier;

                r = CommonColors.WulfrumGreen.R * colorMult;
                g = CommonColors.WulfrumGreen.G * colorMult;
                b = CommonColors.WulfrumGreen.B * colorMult;
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];

            Texture2D texture = TextureAssets.Tile[Type].Value;
            if (t.TileColor != PaintID.None)
            {
                Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, t.TileColor);
                texture = paintedTex ?? texture;
            }

            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y + 2) + zero;
            Color drawColour = Color.White * CommonColors.WulfrumLightMultiplier;
            Main.spriteBatch.Draw(texture, drawOffset, new Rectangle(t.TileFrameX, t.TileFrameY + 54, 16, 16), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
        }
    }

    public class BunkerLongTableRubbleEcho : BunkerLongTableRubble
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + "BunkerLongTableRubble";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            RegisterItemDrop(ModContent.ItemType<DullPlatingItem>());
            FlexibleTileWand.RubblePlacementLarge.AddVariations(ModContent.ItemType<DullPlatingItem>(), Type, 0, 1, 2);
        }
    }
}
