namespace CalamityFables.Cooldowns
{
    public class ChaosStateCooldown : CooldownHandler, ILoadable
    {
        public void Load(Mod mod)
        {
            FablesGeneralSystemHooks.BuffVisibilityChecks += HidePotionSickness;
        }

        private bool HidePotionSickness(int index)
        {
            if (Main.LocalPlayer.buffType[index] == BuffID.ChaosState && FablesConfig.Instance.VanillaCooldownDisplay && FablesConfig.Instance.CooldownDisplay > 0)
                return false;
            return true;
        }

        public void Unload() { }

        public static string ID => "ChaosState";
        public override string LocalizationKey => ID;
        public override bool ShouldDisplay => FablesConfig.Instance.VanillaCooldownDisplay && instance.player.chaosState;
        public override string Texture => "CalamityFables/Cooldowns/ChaosState" + skinTexture;
        public override Color OutlineColor => outlineColor;
        public override SoundStyle? EndSound => new(SoundDirectory.Cooldowns + "ChaosStateOver");

        //It's the same cooldown with different skins each time, basically.
        public string skinTexture;
        public Color outlineColor;
        public Color cooldownColorStart;
        public Color cooldownColorEnd;


        public override Color HighlightColor => new Color(246, 116, 181);
        public override Color GradientOutlineColor => new Color(92, 18, 52);
        public override Color GradientTopColor => new Color(125, 34, 76);
        public override Color GradientBotColor => new Color(206, 33, 113);


        public ChaosStateCooldown() : this("") { }
        public ChaosStateCooldown(string skin)
        {
            switch (skin)
            {
                default:
                    skinTexture = "";
                    outlineColor = new Color(246, 116, 181);
                    cooldownColorStart = new Color(223, 58, 140);
                    cooldownColorEnd = new Color(255, 179, 218);
                    break;
            }
        }
    }
}
