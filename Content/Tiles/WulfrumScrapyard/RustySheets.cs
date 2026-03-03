using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.Localization;
using static CalamityFables.Content.Tiles.TileDirections;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class RustySheets : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Iron;
            HitSound = SoundID.Tink;
            Main.tileMergeDirt[Type] = true;
            Main.tileBrick[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;

            AddMapEntry(new Color(124, 122, 118));
        }
    }

    public class RustySheetsItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rusty Sheets");
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<RustySheets>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(10).
                AddRecipeGroup(RecipeGroupID.IronBar).
                AddTile<BunkerWorkshop>().
                Register();

            CreateRecipe().
                AddIngredient<RustyPlatformItem>(2).
                AddTile(TileID.WorkBenches).
                Register();

            CreateRecipe().
                AddIngredient<RustyHandrailPlatformItem>(2).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }
}