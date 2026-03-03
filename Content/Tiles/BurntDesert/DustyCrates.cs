using CalamityFables.Content.Items.Wulfrum;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class DustyCrates : ModTile
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 2;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.AlternateTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.AnchorAlternateTiles = new int[] { ModContent.TileType<DustyCrates>(), ModContent.TileType<DustyCratesEcho>()};
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;

            TileObjectData.addTile(Type);
            DustType = DustID.BorealWood;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Crate");
            AddMapEntry(new Color(117, 92, 69), name);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    public class DustyCratesEcho : DustyCrates
    {
        public override string Texture => AssetDirectory.BurntDesert + "DustyCrates";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            RegisterItemDrop(ItemID.PalmWood);
            FlexibleTileWand.RubblePlacementMedium.AddVariations(ItemID.PalmWood, Type, 0, 1, 2, 3, 4, 5, 6, 7);
        }
    }
}
