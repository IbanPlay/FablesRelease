namespace CalamityFables.Cooldowns
{
    public class WarriorsAmphoraCooldown : CooldownHandler
    {
        public static string ID => "WarriorsAmphora";
        public override string LocalizationKey => ID;
        public override string Texture => "CalamityFables/Cooldowns/WarriorsAmphora";
        public override bool SavedWithPlayer => true;
        public override bool PersistsThroughDeath => true;
        public override Color OutlineColor => new Color(116, 255, 254);
        public override Color HighlightColor => new Color(141, 212, 197);
        public override Color GradientOutlineColor => new Color(77, 80, 28);
        public override Color GradientTopColor => new Color(102, 105, 47);
        public override Color GradientBotColor => new Color(58, 204, 221);
    }
}
