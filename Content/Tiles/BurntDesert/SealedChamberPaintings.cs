using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class ScourgekillerPainting : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + "ScourgeKiller";

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Scourgekiller");
            Tooltip.SetDefault("'Unearthed by J. Ding'");
        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<ScourgekillerPaintingTile>();
        }
    }

    public class ScourgekillerPaintingTile : ModTile
    {
        public override string Texture => AssetDirectory.BurntDesert + "ScourgeKillerTile";

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Width = 6;
            TileObjectData.newTile.Height = 4;
            TileObjectData.newTile.Origin = new Point16(2, 2);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 16 };

            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Painting");
            AddMapEntry(new Color(99, 50, 30), name);
            TileID.Sets.FramesOnKillWall[Type] = true;
            DustType = 8;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
    }

    public class ShatteredTabletPainting : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + "ShatteredTablet";

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Shattered Tablet");
            Tooltip.SetDefault("'Unearthed by J. Ding'");
        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<ShatteredTabletPaintingTile>();
        }
    }

    public class ShatteredTabletPaintingTile : ModTile
    {
        public override string Texture => AssetDirectory.BurntDesert + "ShatteredTabletTile";

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Width = 6;
            TileObjectData.newTile.Height = 4;
            TileObjectData.newTile.Origin = new Point16(2, 2);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 16 };

            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Painting");
            AddMapEntry(new Color(99, 50, 30), name);
            TileID.Sets.FramesOnKillWall[Type] = true;
            DustType = 8;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
    }
}