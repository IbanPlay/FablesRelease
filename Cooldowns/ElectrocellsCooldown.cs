namespace CalamityFables.Cooldowns
{
    public class ElectrocellsCooldown : CooldownHandler
    {
        public static string ID => "Electrocells";
        public override string LocalizationKey => ID;
        public override string Texture => "CalamityFables/Cooldowns/Electrocells";
        public override bool SavedWithPlayer => false;
        public override bool PersistsThroughDeath => false;
        public override Color OutlineColor => Color.Lerp(new Color(255, 239, 99), Color.White, 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly* 8f));
        public override Color HighlightColor => new Color(143, 255, 238);
        public override Color GradientOutlineColor => new Color(0, 127, 144);
        public override Color GradientTopColor => new Color(34, 143, 137);
        public override Color GradientBotColor => new Color(88, 202, 58);
    }
}
