using CalamityFables.Content.Items.Wulfrum;
using Terraria.Localization;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    public class MyceliumMoldEcho : MyceliumMold
    {
        public override void Load() { }

        public override string Texture => AssetDirectory.CrabulonDrops + "MyceliumMold";

        //Doesnt shatter
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
        }
    }

    public class MyceliumMoldItem : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Mycelium Mold");
            Tooltip.SetDefault("Less frail than the regular variety");
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
            Item.createTile = TileType<MyceliumMoldEcho>();
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(2).
                AddIngredient(ItemID.GlowingMushroom).
                AddTile(TileID.WorkBenches).
                AddCondition(Condition.InGlowshroom).
                Register();
        }
    }

}