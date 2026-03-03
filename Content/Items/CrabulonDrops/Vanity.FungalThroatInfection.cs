using Terraria.Graphics.Renderers;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    public class FungalThroatInfection : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;
        public static readonly SoundStyle FungalHurtSound = new SoundStyle(SoundDirectory.CrabulonDrops + "FungalInfectionHurt", 3);

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fungal Throat Infection");
            Tooltip.SetDefault("Changes your voice\n" +
                "'Great for impersonating a zombified corpse!'");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;

            // Same values as a boss mask
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(silver: 75);
            Item.vanity = true;
            Item.accessory = true;
            Item.hasVanityEffects = true;
            Item.maxStack = 1;
        }


        public override void UpdateAccessory(Player player, bool hideVisual) => player.SetCustomHurtSound(FungalHurtSound, 10);
        public override void UpdateVanity(Player player) => player.SetCustomHurtSound(FungalHurtSound, 10);
    }
}