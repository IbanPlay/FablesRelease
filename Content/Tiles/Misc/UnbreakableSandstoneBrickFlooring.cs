using CalamityFables.Content.Tiles.Wulfrum.Furniture;

namespace CalamityFables.Content.Tiles.Wulfrum
{
    /// <summary>
    /// Sandstone brick if it was swag and merged with sandstone
    /// </summary>
    public class UnbreakableSandstoneBrickFlooring : ModTile
    {
        public override string Texture => AssetDirectory.MiscTiles + "SandstoneBrick_SandstoneMerge";

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Sand;
            HitSound = SoundID.Dig;
            RegisterItemDrop(ItemID.SandstoneBrick);
            TileID.Sets.ChecksForMerge[Type] = true;
            Main.tileMerge[TileID.Sandstone][Type] = true;

            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true; 
            Main.tileBrick[Type] = true;

            Main.tileMerge[Type][TileID.HardenedSand] = true;
            Main.tileMerge[Type][TileID.Sand] = true;
            Main.tileMerge[Type][TileID.CorruptHardenedSand] = true;
            Main.tileMerge[Type][TileID.Ebonsand] = true;
            Main.tileMerge[Type][TileID.CrimsonHardenedSand] = true;
            Main.tileMerge[Type][TileID.Crimsand] = true;
            Main.tileMerge[Type][TileID.HallowHardenedSand] = true;
            Main.tileMerge[Type][TileID.Pearlsand] = true;
            Main.tileMerge[Type][TileID.CorruptSandstone] = true;
            Main.tileMerge[Type][TileID.CrimsonSandstone] = true;
            Main.tileMerge[Type][TileID.HallowSandstone] = true;

            AddMapEntry(new Color(233, 227, 130));
        }

        public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
        {
            WorldGen.TileMergeAttempt(-2, TileID.Sandstone, ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
        }

        public override bool CanReplace(int i, int j, int tileTypeBeingPlaced) => false;
        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;
        public override bool CanExplode(int i, int j) => false;
    }

    public class SandstoneAccentBrick : ModTile
    {
        public override string Texture => AssetDirectory.MiscTiles + "SandstoneBrick_SandstoneMerge";

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Sand;
            HitSound = SoundID.Dig;
            TileID.Sets.ChecksForMerge[Type] = true;
            Main.tileMerge[TileID.Sandstone][Type] = true;

            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileBrick[Type] = true;

            FablesUtils.SetMerge(Type, TileID.Sand);
            FablesUtils.SetMerge(Type, TileID.HardenedSand);
            FablesUtils.SetMerge(Type, TileID.Ebonsand);
            FablesUtils.SetMerge(Type, TileID.CorruptHardenedSand);
            FablesUtils.SetMerge(Type, TileID.Crimsand);
            FablesUtils.SetMerge(Type, TileID.CrimsonHardenedSand);
            FablesUtils.SetMerge(Type, TileID.Pearlsand);
            FablesUtils.SetMerge(Type, TileID.HallowHardenedSand);
            FablesUtils.SetMerge(Type, TileID.CorruptSandstone);
            FablesUtils.SetMerge(Type, TileID.CrimsonSandstone);
            FablesUtils.SetMerge(Type, TileID.HallowSandstone);

            AddMapEntry(new Color(233, 227, 130));
        }

        public override void ModifyFrameMerge(int i, int j, ref int up, ref int down, ref int left, ref int right, ref int upLeft, ref int upRight, ref int downLeft, ref int downRight)
        {
            WorldGen.TileMergeAttempt(-2, TileID.Sandstone, ref up, ref down, ref left, ref right, ref upLeft, ref upRight, ref downLeft, ref downRight);
        }
    }

    public class SandstoneAccentBrickItem : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sandstone Accent Brick");
            Tooltip.SetDefault("A Sandstone Brick variant that merges better with sandy tiles\n" +
                "Favored by advanced builders");
            Item.ResearchUnlockCount = 100;
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.SandstoneBrick;
            ItemID.Sets.ShimmerTransformToItem[ItemID.SandstoneBrick] = Type;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<SandstoneAccentBrick>());
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }
    }
}