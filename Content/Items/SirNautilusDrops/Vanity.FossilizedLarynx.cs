using CalamityFables.Content.Boss.SeaKnightMiniboss;

namespace CalamityFables.Content.Items.SirNautilusDrops
{
    public class FossilizedLarynx : ModItem
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fossilized Larynx");
            Tooltip.SetDefault("Changes your voice\n" +
                "'Great for impersonating dried out fossil friends!'");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;

            // Same values as a boss mask
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 75);
            Item.vanity = true;
            Item.accessory = true;
            Item.hasVanityEffects = true;
            Item.maxStack = 1;
        }

        public override void UpdateVanity(Player player)
        {
            player.SetCustomHurtSound(SirNautilus.HitSound, 10);
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.SetCustomHurtSound(SirNautilus.HitSound, 10);
        }
    }
}
