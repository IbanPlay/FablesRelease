using Terraria.Localization;

namespace CalamityFables.Core
{
    public class FablesRecipes : ModSystem
    {
        public static LocalizedText OrText;
        public override void Load()
        {
            OrText = Mod.GetLocalization("Extras.Misc.or");
        }

        public static int AnyCopperBarGroup, AnyGoldBarGroup, AnyGoldCrownGroup, LowTierGemGroup, MidTierGemGroup, HighTierGemGroup;

        public override void AddRecipeGroups()
        {
            // Gold and Platinum
            RecipeGroup goldBarGroup = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.GoldBar)}", new int[]
            {
                ItemID.GoldBar,
                ItemID.PlatinumBar
            });
            AnyGoldBarGroup = RecipeGroup.RegisterGroup("AnyGoldBar", goldBarGroup);

            // Copper and Tin
            RecipeGroup copperBarGroup = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.CopperBar)}", new int[]
            {
                ItemID.CopperBar,
                ItemID.TinBar
            });
            AnyCopperBarGroup = RecipeGroup.RegisterGroup("AnyCopperBar", copperBarGroup);

            RecipeGroup goldCrownGroup = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.GoldCrown)}", new int[]
            {
                ItemID.GoldCrown,
                ItemID.PlatinumCrown
            });
            AnyGoldCrownGroup = RecipeGroup.RegisterGroup("AnyGoldCrown", goldCrownGroup);

            // Amethyst and Topaz
            RecipeGroup lowTierGemGroup = new RecipeGroup(() => $"{Lang.GetItemNameValue(ItemID.Amethyst)} {OrText.Value} {Lang.GetItemNameValue(ItemID.Topaz)}", new int[]
            {
                ItemID.Amethyst,
                ItemID.Topaz
            });
            LowTierGemGroup = RecipeGroup.RegisterGroup("AmethystOrTopaz", lowTierGemGroup);

            // Sapphire and Emerald
            RecipeGroup midTierGemGroup = new RecipeGroup(() => $"{Lang.GetItemNameValue(ItemID.Sapphire)} {OrText.Value} {Lang.GetItemNameValue(ItemID.Emerald)}", new int[]
            {
                ItemID.Sapphire,
                ItemID.Emerald
            });
            MidTierGemGroup = RecipeGroup.RegisterGroup("SapphireOrEmerald", midTierGemGroup);

            // Ruby and Diamond
            RecipeGroup highTierGemGroup = new RecipeGroup(() => $"{Lang.GetItemNameValue(ItemID.Ruby)} {OrText.Value} {Lang.GetItemNameValue(ItemID.Diamond)}", new int[]
            {
                ItemID.Ruby,
                ItemID.Diamond
            });
            HighTierGemGroup = RecipeGroup.RegisterGroup("RubyOrDiamond", highTierGemGroup);
        }
    }
}
