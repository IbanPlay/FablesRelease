using CalamityFables.Content.Items.CrabulonDrops;
using CalamityFables.Content.Items.Cursed;
using CalamityFables.Content.Items.Wulfrum;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Cooldowns
{

    public class ShroomHookpointLifespan : CooldownHandler
    {
        public override bool MultiplayerSynced => false;
        public static string ID => "ShroomHookpointTimer";
        public override string LocalizationKey => ID;
        public override bool CanTickDown => SerratedFiberCloak.HookpointDeployed <= 0;
        public override bool ShouldDisplay => SerratedFiberCloak.HookpointDeployed > 0;
        public override bool DrawTimer => true;

        public override string Texture => "CalamityFables/Cooldowns/ShroomHookpointLifespan";
        public override string OutlineTexture => "CalamityFables/Cooldowns/ShroomHookpointLifespanOutline";
        public override string OverlayTexture => "CalamityFables/Cooldowns/ShroomHookpointLifespanOverlay";
        public override Color OutlineColor => new Color(248, 234, 185);

        public override Color HighlightColor => new Color(163, 226, 255);
        public override Color GradientOutlineColor => new Color(44, 47, 176);
        public override Color GradientTopColor => new Color(127, 123, 147);
        public override Color GradientBotColor => new Color(64, 69, 255);

        public override Color BackgroundColor => new Color(0, 21, 80);
        public override Color BackgroundEdgeColor => new Color(5, 29, 156);


        public override void ModifyTextDrawn(ref string text, ref Vector2 position, ref Color textColor, ref float scale)
        {
            //text = instance.timeLeft.ToString();
        }
    }
}
