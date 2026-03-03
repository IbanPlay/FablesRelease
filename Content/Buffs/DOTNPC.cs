namespace CalamityFables.Content.Buffs
{
    public class DOTNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public int scourgePoison;

        public override void ResetEffects(NPC npc)
        {
            scourgePoison = 0;
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (scourgePoison > 0)
            {
                if (npc.lifeRegen > 0)
                    npc.lifeRegen = 0;

                npc.lifeRegen -= scourgePoison * 2;
            }
        }
    }
}