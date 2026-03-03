namespace CalamityFables.Cooldowns
{
    public class PotionSicknessCooldown : CooldownHandler, ILoadable
    {
        public void Load(Mod mod)
        {
            FablesGeneralSystemHooks.BuffVisibilityChecks += HidePotionSickness;
        }

        private bool HidePotionSickness(int index)
        {
            if (Main.LocalPlayer.buffType[index] == BuffID.PotionSickness && FablesConfig.Instance.VanillaCooldownDisplay && FablesConfig.Instance.CooldownDisplay > 0)
                return false;
            return true;
        }

        public void Unload() { }

        public static string ID => "PotionSickness";
        public override string LocalizationKey => ID;
        public override bool ShouldDisplay => FablesConfig.Instance.VanillaCooldownDisplay && instance.player.potionDelay > 0;
        public override string Texture => "CalamityFables/Cooldowns/PotionSickness";
        public override SoundStyle? EndSound => new(SoundDirectory.Cooldowns + "PotionSicknessOver");

        public override void Tick()
        {
            instance.timeLeft = instance.player.potionDelay;
            //instance.duration = instance.player.potionDelayTime;

            if (instance.player.potionDelay <= 0)
                instance.timeLeft = -1;
        }


        public override Color OutlineColor => new Color(255, 142, 165);


        public override Color HighlightColor => new Color(188, 9, 9);
        public override Color GradientOutlineColor => new Color(122, 5, 5);
        public override Color GradientTopColor => new Color(87, 5, 5);
        public override Color GradientBotColor => new Color(188, 9, 9);
    }
}
