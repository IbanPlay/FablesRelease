using System.ComponentModel;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using CalamityFables.Content.UI;

namespace CalamityFables
{
    [BackgroundColor(49, 32, 36, 216)]
    public class CalCompatConfig : ModConfig
    {
        public static CalCompatConfig Instance;

        //TODO eventually make serverside stuff serverside
        public override ConfigScope Mode => ConfigScope.ClientSide;
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message) => true;

        #region Calamity Overrides
        [LabelArgs(ItemID.SparkleGuitar)]
        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(false)]
        public bool UseCalamityMusic { get; set; }

        [ReloadRequired]
        [LabelArgs(ItemID.StylistKilLaKillScissorsIWish)]
        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(true)]
        public bool ReplaceCalamity { get; set; }

        //[ReloadRequired]
        //[LabelArgs(ItemID.Book)]
        //[BackgroundColor(192, 54, 64, 192)]
        //[DefaultValue(true)]
        //public bool RemoveCalamityLoreFluff { get; set; }
        #endregion

    }
}
