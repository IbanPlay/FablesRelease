namespace CalamityFables.Content.Items.Marnite
{
    public class MarniteLiftBuff : ModBuff
    {
        public override string Texture => AssetDirectory.Marnite + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Marnite Lift");
            Description.SetDefault("Do you even..?");
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.mount.SetMount(ModContent.MountType<MarniteLift>(), player);
            player.buffTime[buffIndex] = 10; // reset buff time
            player.GetModPlayer<MarniteArchitectPlayer>().mounted = true;
        }
    }
}
