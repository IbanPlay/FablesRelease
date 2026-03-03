using CalamityFables.Content.Tiles.WulfrumScrapyard;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumPlastic : ModTile, ICustomPaintable, ICustomPlaceSounds
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;

        public SoundStyle PlaceSound => SoundDirectory.CommonSounds.WulfrumTilePlaceSound;


        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.DynastyShingle_Red;
            HitSound = SoundID.Dig;

            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            AddMapEntry(CommonColors.WulfrumLeatherRed);

            FablesSets.CustomPaintedSprites[Type] = true;
            FablesSets.CustomPlaceSound[Type] = true;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            return FablesUtils.BetterGemsparkFraming(i, j, resetFrame);
        }
    }

    public class WulfrumPlasticItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Plastic Block");
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumPlastic>());
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(20).
                AddIngredient(ItemID.ClayBlock).
                AddTile<WulfrumWorkshop>().
                Register();

            CreateRecipe().
                AddIngredient<WulfrumPlasticWallItem>(4).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumComplementPlasticItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + "WulfrumPlasticItem";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Complement Plastic Block");
            Tooltip.SetDefault("A Wulfrum Plastic variant that merges with more blocks\n" +
                "Favored by advanced builders");
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumComplementPlastic>());
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(20).
                AddIngredient(ItemID.ClayBlock).
                AddTile<WulfrumWorkshop>().
                AddCondition(Condition.InGraveyard).
                Register();

            CreateRecipe(1).
                AddIngredient(ItemType<WulfrumPlasticItem>()).
                AddTile<WulfrumWorkshop>().
                AddCondition(Condition.InGraveyard).
                Register();
        }
    }

    public class WulfrumComplementPlastic : ModTile, ICustomPaintable
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, "WulfrumPlastic");

        public override string Texture => AssetDirectory.WulfrumFurniture + "WulfrumPlastic";

        public override void Load()
        {
            FablesGeneralSystemHooks.PostSetupContentEvent += SetTileMerge;
        }

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.DynastyShingle_Red;
            HitSound = SoundID.Dig;

            Main.tileBrick[Type] = true;
            FablesUtils.MergeWithGeneral(Type);
            FablesUtils.MergeWithDesert(Type);
            FablesUtils.MergeWithSnow(Type);
            TileID.Sets.ChecksForMerge[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            AddMapEntry(CommonColors.WulfrumLeatherRed);

            FablesSets.CustomPaintedSprites[Type] = true;
        }

        public void SetTileMerge()
        {
            FablesUtils.SetMerge(Type, TileType<WulfrumHull>());
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            return FablesUtils.BetterGemsparkFraming(i, j, resetFrame);
        }
    }

    public class WulfrumPlasticWall : ModWall, ICustomPaintable
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public virtual string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.DynastyShingle_Red;
            HitSound = SoundID.Dig;
            Main.wallHouse[Type] = true;
            Main.wallBlend[Type] = Type;

            AddMapEntry(CommonColors.WulfrumLeatherDarkMaroon);
            FablesSets.CustomPaintedWalls[Type] = true;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }


    public class WulfrumPlasticWallUnsafe : WulfrumPlasticWall
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + "WulfrumPlasticWall";
        public override string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, "WulfrumPlasticWall");

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.wallHouse[Type] = false;
        }
    }


    public class WulfrumPlasticWallItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Plastic Wall");
            Item.ResearchUnlockCount = 400;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableWall((ushort)WallType<WulfrumPlasticWall>());
        }

        public override void AddRecipes()
        {

            CreateRecipe(4).
                AddIngredient<WulfrumPlasticItem>().
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }
}