using CalamityFables.Content.Items.Cursed;
using CalamityFables.Content.Items.Wulfrum;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Cooldowns
{

    public class PeculiarPotCooldown : CooldownHandler
    {
        public bool BrokenPot => Main.LocalPlayer.GetModPlayer<PeculiarPotPlayer>().potBroken;
        public bool PotEquipped => Main.LocalPlayer.miscEquips[Player.miscSlotMount] != null && Main.LocalPlayer.miscEquips[Player.miscSlotMount].type == ItemType<PeculiarPot>();

        public float DurabilityPercent => (instance.timeLeft - WulfrumHat.BastionCooldown) / (float)WulfrumHat.BastionTime;


        public static string ID => "PeculiarPotLife";
        public override string LocalizationKey => ID;
        public override bool CanTickDown => false && !Main.LocalPlayer.GetModPlayer<PeculiarPotPlayer>().ShouldDisplayCooldown();
        public override bool ShouldDisplay => Main.LocalPlayer.GetModPlayer<PeculiarPotPlayer>().ShouldDisplayCooldown();
        public override bool DrawTimer => true;

        public const string TexBase = "CalamityFables/Cooldowns/PeculiarPotHealth";

        public override string Texture => BrokenPot ? TexBase + "Broken" : (Main.LocalPlayer.GetModPlayer<PeculiarPotPlayer>().PotHealth / (float)PeculiarPot.MAX_POT_HEALTH) <= 0.5f ? TexBase + "Cracked" : TexBase;
        public override string OutlineTexture => BrokenPot ? TexBase + "BrokenOutline" : TexBase + "Outline";
        public override string OverlayTexture => BrokenPot ? TexBase + "BrokenOverlay" : TexBase + "Overlay";
        public override Color OutlineColor => new Color(196, 152, 103);

        public override Color HighlightColor => new Color(146, 99, 40);
        public override Color GradientOutlineColor => new Color(58, 35, 27);
        public override Color GradientTopColor => new Color(62, 37, 28);
        public override Color GradientBotColor => new Color(147, 78, 55);

        public override Color BackgroundColor => new Color(24, 16, 18);
        public override Color BackgroundEdgeColor => new Color(59, 28, 34);


        public override void ModifyTextDrawn(ref string text, ref Vector2 position, ref Color textColor, ref float scale)
        {
            text = instance.timeLeft.ToString();
            textColor = Color.Lerp(new Color(255, 238, 129), Color.OrangeRed, 1 - instance.Completion);
        }
    }
}
