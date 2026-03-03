using CalamityFables.Content.Items.EarlyGameMisc;

namespace CalamityFables.Cooldowns
{
    public class OpulentSyringeReload : CooldownHandler
    {
        public static string ID => "SyringeReload";
        public override string LocalizationKey => ID;
        public override string Texture => "CalamityFables/Cooldowns/SyringeReload";
        public override SoundStyle? EndSound => OpulentDartgun.SyringeReloadSound;
        public override bool SavedWithPlayer => false;
        public override bool PersistsThroughDeath => false;
        public override Color OutlineColor => new Color(255, 255, 100);
        public override Color HighlightColor => Color.Gold;
        public override Color GradientOutlineColor => new Color(208, 82, 36);
        public override Color GradientTopColor => new Color(227, 80, 26);
        public override Color GradientBotColor => new Color(255, 130, 85);
    }
}