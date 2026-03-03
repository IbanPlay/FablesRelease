using CalamityFables.Content.Items.Wulfrum;

namespace CalamityFables.Content.Buffs.Summon
{
    public class WulfrumDroidBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Droid");
            Description.SetDefault("The wulfrum droid will protect you");
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override string Texture => AssetDirectory.Buffs + Name;

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<WulfrumDroid>()] > 0)
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
