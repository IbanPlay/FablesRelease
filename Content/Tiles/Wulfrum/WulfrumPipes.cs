using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using CalamityFables.Content.Tiles.WulfrumScrapyard;
using Terraria.Localization;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum
{
    public class WulfrumPipes : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumTiles + Name;

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;
            Main.tileMergeDirt[Type] = true;
            Main.tileMerge[Type][ModContent.TileType<WulfrumSmokingPipes>()] = true;

            Main.tileSolid[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = true;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Pipe");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
        }
    }

    public class WulfrumPipesItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumTiles + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Pipes");
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<WulfrumPipes>();
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(10).
                AddIngredient<WulfrumMetalScrap>(1).
                AddTile<BunkerWorkshop>().
                AddCustomShimmerResult(ItemType<WulfrumSmokingPipesItem>()).
                Register();
        }
    }

}