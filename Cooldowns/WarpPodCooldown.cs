namespace CalamityFables.Cooldowns
{
    public class WarpPodCooldown : CooldownHandler
    {
        public static string ID => "WarpPod";
        public override string LocalizationKey => ID;
        public override string Texture => "CalamityFables/Cooldowns/WarpPod";
        public override bool SavedWithPlayer => true;
        public override bool PersistsThroughDeath => true;
        public override Color OutlineColor => new Color(98, 138, 255);
        public override Color HighlightColor => new Color(147, 124, 255);
        public override Color GradientOutlineColor => new Color(92, 2, 218);
        public override Color GradientTopColor => new Color(29, 26, 192);
        public override Color GradientBotColor => new Color(116, 62, 255);
    }
}
