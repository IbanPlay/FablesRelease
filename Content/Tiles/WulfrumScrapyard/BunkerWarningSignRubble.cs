using CalamityFables.Content.Items.Wulfrum;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class BunkerWarningSignRubble : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.addTile(Type);
            DustType = 85;
            TileID.Sets.FramesOnKillWall[Type] = true;

            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(205, 181, 83), name);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    public class BunkerWarningSignRubbleEcho : BunkerWarningSignRubble
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + "BunkerWarningSignRubble";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            RegisterItemDrop(ModContent.ItemType<RustySheetsItem>());
            FlexibleTileWand.RubblePlacementMedium.AddVariations(ModContent.ItemType<RustySheetsItem>(), Type, 0, 1);
        }
    }
}
