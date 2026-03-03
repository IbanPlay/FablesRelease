using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumGlass : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;

        public override void Load()
        {
            FablesGeneralSystemHooks.PostSetupContentEvent += SetTileMerge;
        }

        public void SetTileMerge()
        {
            FablesUtils.SetMerge(Type, TileType<WulfrumHull>());
            FablesUtils.SetMerge(Type, TileType<WulfrumComplementPlastic>());
        }

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Glass;
            HitSound = SoundID.Shatter;
            Main.tileBlockLight[Type] = false;
            TileID.Sets.DrawsWalls[Type] = true;
            TileID.Sets.ChecksForMerge[Type] = true;
            Main.tileBrick[54] = true;
            TileID.Sets.BlocksWaterDrawingBehindSelf[Type] = true;
            TileID.Sets.AllowLightInWater[Type] = true;
            TileID.Sets.AllBlocksWithSmoothBordersToResolveHalfBlockIssue[Type] = true;
            Main.tileSolid[Type] = true;
            AddMapEntry(Color.Cornsilk);
        }

        public override void PostSetDefaults()
        {
            Main.tileNoSunLight[Type] = false;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            return FablesUtils.BetterGemsparkFraming(i, j, resetFrame);
        }
    }

    public class WulfrumGlassItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Tinted Glass");
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumGlass>());
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(2).
                AddIngredient(ItemID.Glass).
                AddTile<WulfrumWorkshop>().
                Register();

            CreateRecipe().
                AddIngredient<WulfrumGlassWallItem>(4).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumGlassWall : ModWall
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Glass;
            HitSound = SoundID.Shatter;
            Main.wallHouse[Type] = true;
            Main.wallLight[Type] = true;
            WallID.Sets.Transparent[Type] = true;
            Main.wallBlend[Type] = Type;

            AddMapEntry(Color.Tan);
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }


    public class WulfrumGlassWallItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Tinted Glass Wall");
            Item.ResearchUnlockCount = 400;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableWall((ushort)WallType<WulfrumGlassWall>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(4).
                AddIngredient<WulfrumGlassItem>().
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }
}