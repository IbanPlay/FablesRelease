using CalamityFables.Content.Dusts;

namespace CalamityFables.Content.Buffs
{
    public class Overamped : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Over-Amped");
            Description.SetDefault("All the hair on your body is raising");
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
        }

        public override string Texture => AssetDirectory.Buffs + Name;

        public override void Update(NPC NPC, ref int buffIndex)
        {
            if (Main.rand.NextBool(5))
            {
                Vector2 dustPos = NPC.Center + Vector2.UnitX * Main.rand.NextFloat(-NPC.width, NPC.width) * 0.5f + Vector2.UnitX * Main.rand.NextFloat(-NPC.height, NPC.height) * 0.2f;
                int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();

                Dust d = Dust.NewDustPerfect(dustPos, dusType, -Vector2.UnitY * Main.rand.NextFloat(1f, 4f));
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.3f, 0.5f);
            }
        }
    }
}
