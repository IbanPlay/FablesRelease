using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;
using CalamityFables.Content.Boss.MushroomCrabBoss;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    public class OrnamentalHookstaff : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + "HookstaffPlacer";

        public override void Load()
        {
            FablesPlayer.OverridePlacedTileEvent += PlaceOnWalls;
        }

        private void PlaceOnWalls(Player player, Tile targetTile, Item item, ref int tileToPlace, ref int previewPlaceStyle, ref bool? overrideCanPlace)
        {
            if (tileToPlace != Item.createTile)
                return;
            Tile tileBelow = Main.tile[Player.tileTargetX, Player.tileTargetY + 1];
            if (!tileBelow.HasUnactuatedTile || !Main.tileSolid[tileBelow.TileType])
                tileToPlace = ModContent.TileType<HookstaffTileEcho>();
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Ornamental Hookstaff");
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<HookstaffBuriedTileEcho>());
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ModContent.ItemType<Hookstaff>()).
                AddTile(TileID.HeavyWorkBench).
                AddCondition(Condition.InGraveyard).
                Register();
        }
    }

    #region Echo tiles
    public class HookstaffTileEcho : HookstaffTile
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            RegisterItemDrop(ModContent.ItemType<Hookstaff>());
        }        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => base.PreDraw(i, j, spriteBatch);
    }

    public class HookstaffBuriedTileEcho : HookstaffBuriedTile
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            RegisterItemDrop(ModContent.ItemType<Hookstaff>(), 0);
        }
    }
    #endregion
}