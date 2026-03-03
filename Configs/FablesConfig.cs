using System.ComponentModel;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using CalamityFables.Content.UI;

namespace CalamityFables
{
    [BackgroundColor(49, 32, 36, 216)]
    public class FablesConfig : ModConfig
    {
        public static FablesConfig Instance;

        //TODO eventually make serverside stuff serverside
        public override ConfigScope Mode => ConfigScope.ClientSide;
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message) => true;

        #region Graphics Changes
        [Header("$Mods.CalamityFables.Configs.FablesConfig.SectionTitle.Graphics")]


        [LabelArgs(ItemID.BlizzardinaBottle)]
        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(true)]
        public bool FluidSimVFXEnabled { get; set; }

        [LabelArgs(ItemID.SoulofLight)]
        [BackgroundColor(192, 54, 64, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 1000)]
        [DefaultValue(500)]
        public int ParticleLimit { get; set; }

        [LabelArgs(ItemID.DrumSet)]
        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(1f)]
        [Range(0f, 2f)]
        public float ScreenshakeMultiplier { get; set; }

        [LabelArgs(ItemID.AviatorSunglasses)]
        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(1f)]
        [Range(0f, 2f)]
        public float ChromaticAbberationMultiplier { get; set; }

        #endregion

        #region UI Changes
        [Header("$Mods.CalamityFables.Configs.FablesConfig.SectionTitle.UI")]

        [LabelArgs(ItemID.TatteredWoodSign)]
        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(true)]
        public bool BossIntroCardsActivated { get; set; }

        [BackgroundColor(0, 0, 0, 0)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0f, 6f)]
        [DefaultValue(1)]
        //[Increment(1f)]
        [CustomModConfigItem(typeof(CooldownStyleConfigElement))]
        public float CooldownDisplay { get; set; }


        [BackgroundColor(192, 54, 64, 192)]
        [LabelArgs(ItemID.HealingPotion)]
        [DefaultValue(true)]
        public bool VanillaCooldownDisplay { get; set; }
        #endregion
    }
}
