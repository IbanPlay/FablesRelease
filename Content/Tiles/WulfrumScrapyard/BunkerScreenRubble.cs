using CalamityFables.Content.Items.Wulfrum;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class BunkerScreenRubble : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileLighted[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.CoordinateHeights = [16, 16];
            TileObjectData.addTile(Type);
            DustType = DustID.Iron;
            TileID.Sets.FramesOnKillWall[Type] = true;

            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(40, 22, 18), name);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            Tile t = Main.tile[i, j];
            if (t.TileFrameY != 0 || t.TileFrameX != 162)
                return;
            float colorMult = 1 / 265f * 0.5f * CommonColors.WulfrumLightMultiplierWithFlickerOffset(i * 3 + j);

            r = CommonColors.WulfrumGreen.R * colorMult;
            g = CommonColors.WulfrumGreen.G * colorMult;
            b = CommonColors.WulfrumGreen.B * colorMult;
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
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y ) + zero;
            Color drawColour = Color.White * CommonColors.WulfrumLightMultiplierWithFlickerOffset(i * 3 + j);
            Main.spriteBatch.Draw(texture, drawOffset, new Rectangle(t.TileFrameX, t.TileFrameY + 36, 16, 16), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
        }
    }

    public class BunkerScreenRubbleEcho : BunkerScreenRubble
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + "BunkerScreenRubble";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            RegisterItemDrop(ModContent.ItemType<RustySheetsItem>());
            FlexibleTileWand.RubblePlacementSmall.AddVariations(ModContent.ItemType<RustySheetsItem>(), Type, 0, 1, 2, 3);
        }
    }
}
