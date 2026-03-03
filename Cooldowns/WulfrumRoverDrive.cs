using CalamityFables.Content.Items.Wulfrum;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Cooldowns
{
    public class WulfrumRoverDriveDurability : CooldownHandler
    {
        private static Color ringColorLerpStart = new Color(49, 220, 221);
        public override float AdjustedCompletion => 1 - (instance.timeLeft) / (float)RoverDrive.ProtectionMatrixDurabilityMax;

        public static string ID => "WulfrumRoverDriveDurability";
        public override string LocalizationKey => ID;
        public override bool CanTickDown => !instance.player.GetModPlayer<RoverDrivePlayer>().RoverDriveOn || instance.timeLeft <= 0;
        public override bool ShouldDisplay => instance.player.GetModPlayer<RoverDrivePlayer>().RoverDriveOn;
        public override string Texture => "CalamityFables/Cooldowns/WulfrumRoverDriveActive";
        public override string OutlineTexture => "CalamityFables/Cooldowns/WulfrumRoverDriveOutline";
        public override string OverlayTexture => "CalamityFables/Cooldowns/WulfrumRoverDriveOverlay";
        public override Color OutlineColor => new Color(112, 244, 244);
        public override bool SavedWithPlayer => false;
        public override bool PersistsThroughDeath => false;
        public override bool DrawTimer => true;

        public override Color HighlightColor => new Color(108, 230, 255);
        public override Color GradientOutlineColor => new Color(54, 97, 198);
        public override Color GradientTopColor => new Color(76, 95, 89);
        public override Color GradientBotColor => new Color(54, 177, 221);


        public override void ModifyTextDrawn(ref string text, ref Vector2 position, ref Color textColor, ref float scale)
        {
            text = instance.timeLeft.ToString();
            textColor = Color.Lerp(ringColorLerpStart, Color.OrangeRed, 1 - instance.Completion);
        }
    }

    public class WulfrumRoverDriveRecharge : CooldownHandler
    {
        public static string ID => "WulfrumRoverDriveRecharge";
        public override string LocalizationKey => ID;
        public override string Texture => "CalamityFables/Cooldowns/WulfrumRoverDrive";
        public override bool SavedWithPlayer => false;
        public override bool PersistsThroughDeath => false;
        public override Color OutlineColor => new Color(194, 255, 67);

        public override Color HighlightColor => new Color(147, 237, 121);
        public override Color GradientOutlineColor => new Color(42, 124, 88);
        public override Color GradientTopColor => new Color(65, 69, 71);
        public override Color GradientBotColor => new Color(92, 187, 99);

        public override bool MultiplayerSynced => false;


        public override void OnCompleted() => instance.player.GetModPlayer<RoverDrivePlayer>().ProtectionMatrixDurability = RoverDrive.ProtectionMatrixDurabilityMax;
        public override SoundStyle? EndSound => null;
    }

    [Serializable]
    public class ChargeProtectionMatrixPacket : Module
    {
        public readonly byte whoAmI;
        public ChargeProtectionMatrixPacket(Player player)
        {
            whoAmI = (byte)player.whoAmI;
        }

        protected override void Receive()
        {
            Player player = Main.player[whoAmI];
            player.GetModPlayer<RoverDrivePlayer>().ProtectionMatrixDurability = RoverDrive.ProtectionMatrixDurabilityMax;
            if (Main.netMode == NetmodeID.Server)
            {
                Send(-1, whoAmI, false);
                return;
            }
        }
    }
}
