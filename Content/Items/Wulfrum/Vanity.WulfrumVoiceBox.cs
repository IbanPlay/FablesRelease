using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalamityFables.Content.Items.Wulfrum
{
    public class WulfrumVoiceBox : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumItems + Name;
        public static readonly SoundStyle WulfrumHurtSound = new SoundStyle(SoundDirectory.Wulfrum + "WulfrumVoiceboxHurt", 4);

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Voicebox");
            Tooltip.SetDefault("Changes your voice\n" +
                "'Great for impersonating cheap junk!'");
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 32;

            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 75);
            Item.vanity = true;
            Item.accessory = true;
            Item.hasVanityEffects = true;
            Item.maxStack = 1;
        }


        public override void UpdateAccessory(Player player, bool hideVisual) => player.SetCustomHurtSound(WulfrumHurtSound, 10);
        public override void UpdateVanity(Player player) => player.SetCustomHurtSound(WulfrumHurtSound, 10);
    }
}
