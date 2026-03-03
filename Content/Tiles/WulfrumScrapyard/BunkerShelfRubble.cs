using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.BurntDesert;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class BunkerShelfRubble : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileTable[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.Height = 5;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(2, 4);
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16, 18];
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.addTile(Type);

            DustType = DustID.Crimson;

            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(153, 64, 36), name);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    public class BunkerShelfRubbleEcho : BunkerShelfRubble
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + "BunkerShelfRubble";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            RegisterItemDrop(ModContent.ItemType<RustySheetsItem>());
            FlexibleTileWand.RubblePlacementLarge.AddVariations(ModContent.ItemType<RustySheetsItem>(), Type, 0, 1, 2);
        }
    }
}
