using CalamityFables.Content.Items.Wulfrum;

namespace CalamityFables.Cooldowns
{

    public class WulfrumBastionCooldown : CooldownHandler
    {
        public bool PowerActive => instance.timeLeft > WulfrumHat.BastionCooldown;
        public float DurabilityPercent => (instance.timeLeft - WulfrumHat.BastionCooldown) / (float)WulfrumHat.BastionTime;

        public static string ID => "WulfrumBastion";
        public override bool ShouldDisplay => true;
        public override bool DrawTimer => !PowerActive;
        public override string LocalizationKey => PowerActive ? "WulfrumBastionDurability" : "WulfrumBastionRecharge";
        public override string Texture => PowerActive ? "CalamityFables/Cooldowns/WulfrumBastionActive" : "CalamityFables/Cooldowns/WulfrumBastion";
        public override string OutlineTexture => "CalamityFables/Cooldowns/WulfrumBastionOutline";
        public override string OverlayTexture => "CalamityFables/Cooldowns/WulfrumBastionOverlay";
        public override Color OutlineColor => PowerActive ? new Color(194, 255, 67) : new Color(206, 201, 170);

        public override Color HighlightColor => PowerActive ? new Color(112, 244, 244) : new Color(155, 171, 117);
        public override Color GradientOutlineColor => PowerActive ? new Color(26, 75, 182) : new Color(77, 91, 83);
        public override Color GradientTopColor => PowerActive ? new Color(54, 100, 199) : new Color(78, 93, 83);
        public override Color GradientBotColor => PowerActive ? new Color(54, 177, 221) : new Color(103, 137, 100);


        public override SoundStyle? EndSound => new(SoundDirectory.Wulfrum + "WulfrumBastionRecharge");
        //public override bool SavedWithPlayer => false;

        public override void OnCompleted()
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 dustDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
                Dust d = Dust.NewDustPerfect(instance.player.Center + dustDirection * Main.rand.NextFloat(0.4f, 10f), 226, dustDirection * Main.rand.NextFloat(1f, 4f), 100, Color.Transparent, Main.rand.NextFloat(0.8f, 1.2f));
                d.noGravity = true;
                d.noLight = true;
                d.fadeIn = 1f;
            }
        }

        //Charge down at first, and then charge back up
        public override float AdjustedCompletion => PowerActive ? (instance.timeLeft - WulfrumHat.BastionCooldown) / (float)WulfrumHat.BastionTime : 1 - (instance.timeLeft / (float)WulfrumHat.BastionCooldown);
    }

}
