using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.Localization;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class GrimyGrate : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void Load()
        {
            FablesTile.MakeWaterIgnoreTilesEvent += SetSolidState;
        }

        private void SetSolidState(bool ignoreSolids) => Main.tileSolid[Type] = !ignoreSolids;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Iron;
            HitSound = SoundID.Tink;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = false;
            Main.tileObsidianKill[Type] = true;
            Main.tileNoSunLight[Type] = false;
            Main.tileLighted[Type] = true;
            TileID.Sets.DrawsWalls[Type] = true;
            TileID.Sets.AllBlocksWithSmoothBordersToResolveHalfBlockIssue[Type] = true;
            FablesSets.ActsAsAGrate[Type] = true;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Grimy Grate");
            AddMapEntry(new Color(138, 125, 104), name);
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            return FablesUtils.BetterGemsparkFraming(i, j, resetFrame);
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            Tile t = Main.tile[i, j];
            //Glow when full of grimy water
            if (t.LiquidAmount > 0 && t.LiquidType == 0 && Main.SceneMetrics.ActiveFountainColor == ModContent.GetInstance<GrimyWaterStyle>().Slot)
            {
                float lightStrenght = t.LiquidAmount / 255f;
                r += 0.29f * lightStrenght;
                g += 0.4f * lightStrenght;
                b += 0.3f * lightStrenght;
            }
        }
    }

    public class GrimyGrateItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Grimy Grate");
            Tooltip.SetDefault("Allows only liquids through\n" +
                "'Also makes for grate looking cages!'");
            Item.ResearchUnlockCount = 100;

            //Back and forth transformation into grates
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.Grate;
            ItemID.Sets.ShimmerTransformToItem[ItemID.Grate] = Type;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<GrimyGrate>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddRecipeGroup(RecipeGroupID.IronBar).
                AddTile<BunkerWorkshop>().
                AddCondition(Condition.InGraveyard).
                Register();
        }
    }
}