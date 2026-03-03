namespace CalamityFables.Content.Buffs
{
    public class ScourgePoison : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Scourge Poison");
            Description.SetDefault("Extremely fast damage to internal organs");
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            BuffID.Sets.CanBeRemovedByNetMessage[Type] = true;
        }

        public override string Texture => AssetDirectory.Buffs + Name;

        public override void Update(NPC NPC, ref int buffIndex)
        {
            NPC.GetGlobalNPC<DOTNPC>().scourgePoison = 10;
        }
    }

}

