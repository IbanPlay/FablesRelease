using CalamityFables.Content.Items.Wulfrum;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.Wulfrum
{
    public class Wulfrum1x1Piles : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumTiles + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(0, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 18 };
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.AnchorValidTiles = new int[] { TileID.Dirt, TileID.LivingMahogany, TileID.Stone, ModContent.TileType<WulfrumLandfill>(), ModContent.TileType<WulfrumPipes>() };
            TileObjectData.newTile.RandomStyleRange = 5;

            TileObjectData.newTile.CoordinatePaddingFix = new Point16(0, 2);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;

            TileObjectData.addTile(Type);

            TileID.Sets.DisableSmartCursor[Type] = true;
            DustType = DustID.Tungsten;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Scrap");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    public class Wulfrum3x2Piles : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumTiles + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(0, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 18 };
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.RandomStyleRange = 5;

            TileObjectData.newTile.CoordinatePaddingFix = new Point16(0, 2);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;

            TileObjectData.addTile(Type);

            TileID.Sets.DisableSmartCursor[Type] = true;
            DustType = DustID.Tungsten;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Scrap");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    #region Rubblemaker duplicates
    public class Wulfrum1x1PilesEcho : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumTiles + "Wulfrum1x1Piles";

        public override void SetStaticDefaults()
        {
            RegisterItemDrop(ModContent.ItemType<WulfrumMetalScrap>());
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(0, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 18 };
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinatePaddingFix = new Point16(0, 2);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;

            TileObjectData.addTile(Type);
            TileID.Sets.DisableSmartCursor[Type] = true;
            DustType = DustID.Tungsten;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Scrap");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
            FlexibleTileWand.RubblePlacementSmall.AddVariations(ModContent.ItemType<WulfrumMetalScrap>(), Type, 0, 1, 2, 3, 4);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    public class Wulfrum3x2PilesEcho : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumTiles + "Wulfrum3x2Piles";

        public override void SetStaticDefaults()
        {
            RegisterItemDrop(ModContent.ItemType<WulfrumMetalScrap>());
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 18 };
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);

            TileObjectData.newTile.CoordinatePaddingFix = new Point16(0, 2);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;

            TileObjectData.addTile(Type);

            TileID.Sets.DisableSmartCursor[Type] = true;
            DustType = DustID.Tungsten;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Scrap");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);

            FlexibleTileWand.RubblePlacementLarge.AddVariations(ModContent.ItemType<WulfrumMetalScrap>(), Type, 0, 1, 2, 3, 4);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }
    #endregion
}
