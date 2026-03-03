using CalamityFables.Content.Boss.SeaKnightMiniboss;
using CalamityFables.Cooldowns;
using CalamityFables.Particles;
using ReLogic.Graphics;

namespace CalamityFables.Content.Debug
{
    public class AmnesiaItem : ModItem
    {
        public override string Texture => AssetDirectory.Debug + "Amnesia";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sometime at the Verge of Space");
            Item.ResearchUnlockCount = 0;
        }

        public override void SetDefaults()
        {
            Item.useTime = Item.useAnimation = 8;
            Item.width = 30;
            Item.height = 38;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item1;
            Item.value = Item.buyPrice(0, 0, 10, 0);
            Item.rare = ItemRarityID.Blue;
        }

        public override bool? UseItem(Player player)
        {
            SirNautilusDialogue.ReadThroughDesertScourgeTopic = false;
            SirNautilusDialogue.ReadThroughDoppelgangerEasterEgg = false;
            SirNautilusDialogue.ReadThroughHardmodeTopic = false;
            SirNautilusDialogue.ReadThroughHowDidYouKeepYourSanity = false;
            SirNautilusDialogue.ReadThroughTheFirstRundown = false;
            SirNautilusDialogue.ReadThroughWhatAreYouDoing = false;
            SirNautilusDialogue.ReadThroughWhatHappenedToYou = false;
            SirNautilusDialogue.ReadThroughWhatNowPostDefeat = false;
            SirNautilusDialogue.ReadThroughWhatNowPreDefeat = false;
            SirNautilusDialogue.ReadThroughWhatReallyHappened = false;
            SirNautilusDialogue.ReadThroughWhoAreYou = false;
            SirNautilusDialogue.ReadThroughWhyHere = false;

            SirNautilusDialogue.ResetAllFlags();
            return true;
        }

    }

}