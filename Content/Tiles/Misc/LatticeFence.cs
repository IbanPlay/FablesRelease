using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Misc
{
    public class LatticeFence : ModWall
    {
        public override void Load()
        {
            Terraria.On_WorldGen.DefaultTreeWallTest += MakeLatticeFenceNotCuckTreeGrowth;
        }
        public override void Unload()
        {
            Terraria.On_WorldGen.DefaultTreeWallTest -= MakeLatticeFenceNotCuckTreeGrowth;
        }

        private bool MakeLatticeFenceNotCuckTreeGrowth(Terraria.On_WorldGen.orig_DefaultTreeWallTest orig, int wallType)
        {
            if (wallType == Type)
                return true;

            return orig(wallType);
        }

        public override string Texture => AssetDirectory.MiscTiles + Name;
        public override void SetStaticDefaults()
        {
            WallID.Sets.AllowsWind[Type] = true;
            Main.wallLight[Type] = true;

            DustType = 8;
            Main.wallHouse[Type] = true;
            HitSound = SoundID.Tink;
            AddMapEntry(new Color(94, 94, 94));
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

    }

    public class LatticeFenceItem : ModItem
    {
        public override string Texture => AssetDirectory.MiscTiles + Name;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 400;
            DisplayName.SetDefault("Lattice Fence");
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 7;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createWall = ModContent.WallType<LatticeFence>();
            Item.rare = 1;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(4).
                AddRecipeGroup(RecipeGroupID.IronBar).
                AddTile(TileID.HeavyWorkBench).
                AddCustomShimmerResult(ItemType<LatticeFenceTatteredItem>()).
                Register();
        }
    }

}