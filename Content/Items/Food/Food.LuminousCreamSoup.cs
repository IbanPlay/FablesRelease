

using CalamityFables.Content.Boss.MushroomCrabBoss;

namespace CalamityFables.Content.Items.Food
{
    public class LuminousCreamSoup : BaseFoodItem
    {
        public override void Load()
        {
            FablesNPC.ModifyNPCLootEvent += ModifyShroomEnemyLoot;
        }

        private void ModifyShroomEnemyLoot(NPC npc, NPCLoot npcloot)
        {
            if (npc.type != NPCID.AnomuraFungus &&
                npc.type != NPCID.MushiLadybug &&
                npc.type != NPCID.SporeBat &&
                npc.type != NPCID.SporeSkeleton &&
                npc.type != NPCID.FungoFish &&
                npc.type != NPCID.ZombieMushroomHat &&
                npc.type != NPCID.ZombieMushroom &&
                npc.type != NPCID.FungiBulb &&
                npc.type != NPCID.GiantFungiBulb
                 )
                return;

            npcloot.Add(Type, 80);
        }

        public override int FoodBuff => BuffID.WellFed2;

        public override bool SolidFood => false;

        public override int BuffTime => 60 * 60 * 20;

        public override Point SpriteSize => new(36, 26);

        public override Color[] CrumbColors => [ new Color(95, 98, 215), new Color(123, 151, 237), new Color(51, 135, 255)];

        public override void OnConsumeItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<CrabulonDOT>(), 60 * 2);
        }
    }
}
