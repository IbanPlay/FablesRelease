namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("EnergyCore")]
    public class EnergyCore : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
            DisplayName.SetDefault("Energy Core");
            Tooltip.SetDefault("It pulses with energy\n" +
                "Can only be obtained by destroying a Wulfrum machine while a Nexus is empowering it");
        }

        public override void SetDefaults()
        {
            Item.width = Item.height = 22;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(silver: 20);
            Item.rare = ItemRarityID.Blue;
        }
    }
}
