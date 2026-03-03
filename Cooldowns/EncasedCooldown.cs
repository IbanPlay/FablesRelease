using CalamityFables.Content.Items.Snow;

namespace CalamityFables.Cooldowns;

public class EncasedCooldown : CooldownHandler
{
    public static string ID => "EncasedHits";
    public override string LocalizationKey => !IceHatAbilityHandler.IsEncased(Main.LocalPlayer) && GetCubeState(Main.LocalPlayer) != IceCubeState.Normal ? "EncasedCooldown" : ID;
    public override bool CanTickDown => false;

    private readonly string BaseTexturePath = "Calamityfables/Cooldowns/" + ID;
    public override string Texture => BaseTexturePath + (GetCubeState(Main.LocalPlayer) == IceCubeState.Broken ? "Broken" : GetCubeState(Main.LocalPlayer) == IceCubeState.Cracked ? "Cracked" : "");

    private enum IceCubeState
    {
        Normal,
        Cracked,
        Broken
    }

    private static IceCubeState GetCubeState(Player player)
    {
        if (player.GetPlayerData(out IceHatAbilityHandler.IceHatData data))
        {
            if (data.CubeBroken)
                return IceCubeState.Broken;
            else if (data.CubeCracked)
                return IceCubeState.Cracked;
        }

        return IceCubeState.Normal;
    }

    #region Colors
    public override Color OutlineColor => new Color(163, 226, 255);

    public override Color HighlightColor => new Color(163, 226, 255);
    public override Color GradientOutlineColor => new Color(44, 47, 176);
    public override Color GradientTopColor => new Color(200, 200, 200);
    public override Color GradientBotColor => new Color(50, 80, 190);

    public override Color BackgroundColor => new Color(0, 30, 80);
    public override Color BackgroundEdgeColor => new Color(5, 70, 126);

    #endregion

    public override void ModifyTextDrawn(ref string text, ref Vector2 position, ref Color textColor, ref float scale)
    {
        text = ((int)(3f * instance.Completion)).ToString();
        textColor = Color.Lerp(Color.White, Color.OrangeRed, 1 - instance.Completion);
    }
}