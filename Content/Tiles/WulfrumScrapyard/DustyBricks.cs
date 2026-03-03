using CalamityFables.Content.Items.Wulfrum;
using Terraria.Localization;
using static CalamityFables.Content.Tiles.TileDirections;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class DustyBricks : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Clay;
            HitSound = SoundID.Tink;
            Main.tileMergeDirt[Type] = true;
            Main.tileBrick[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;

            AddMapEntry(new Color(128, 87, 77));
        }

        public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
        {
            if (j % 2 == 0)
            {
                Tile t = Main.tile[i, j];
                t.TileFrameX += 288;
            }
        }
    }

    public class DustyBricksItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dusty Bricks");
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<DustyBricks>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(4).
                AddIngredient(ItemID.GrayBrick).
                AddIngredient(ItemID.RedBrick).
                AddTile(TileID.Furnaces).
                Register();
            CreateRecipe(1).
                AddIngredient(ModContent.ItemType<DustyBrickWallItem>(), 4).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }


    public class DustyBrickWall : ModWall
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Clay;
            Main.wallHouse[Type] = true;
            AddMapEntry(new Color(90, 56, 50));
        }

        public override bool WallFrame(int i, int j, bool randomizeFrame, ref int style, ref int frameNumber)
        {
            frameNumber = WorldGen.genRand.Next(2) + (j % 2) * 2;
            return true;
        }
    }

    public class DustyBrickWallUnsafe : DustyBrickWall
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + "DustyBrickWall";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.wallHouse[Type] = false;
        }
    }

    public class DustyBrickWallItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dusty Brick Wall");
            Item.ResearchUnlockCount = 400;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableWall(ModContent.WallType<DustyBrickWall>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(4).
                AddIngredient(ItemID.GrayBrickWall).
                AddIngredient(ItemID.RedBrickWall).
                AddTile(TileID.Furnaces).
                Register();
            CreateRecipe(4).
                AddIngredient(ModContent.ItemType<DustyBricksItem>()).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }
}