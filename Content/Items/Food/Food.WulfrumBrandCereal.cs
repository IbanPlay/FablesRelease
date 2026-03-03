

using CalamityFables.Content.NPCs.Wulfrum;

namespace CalamityFables.Content.Items.Food
{
    public class WulfrumBrandCereal : BaseFoodItem
    {
        public static int DroprateInt = 70;
        public static float WulfrumDamageReduction = 0.5f;

        public override int FoodBuff => BuffID.WellFed;

        public override int BuffTime => 60 * 60 * 20;

        public override Point SpriteSize => new(32, 42);

        public override Color[] CrumbColors => [ new Color(133, 102, 77), new Color(156, 130, 101), new Color(100, 64, 49)];

        public override void OnConsumeItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<BrandAmbassadorBuff>(), 60 * 60 * 6);
        }
    }

    public class BrandAmbassadorBuff : ModBuff
    {
        public override string Texture => AssetDirectory.Food + Name;

        public override void Load()
        {
            FablesPlayer.ModifyHurtEvent += ReduceDamage;
        }

        private void ReduceDamage(Player player, ref Player.HurtModifiers modifiers)
        {
            if (!player.GetPlayerFlag("WulfrumAmbassador"))
                return;

            if (modifiers.DamageSource.TryGetCausingEntity(out Entity sourceEntity))
            {
                switch (sourceEntity)
                {
                    case Projectile proj:
                        if (FablesSets.WulfrumProjectiles[proj.type])
                            modifiers.FinalDamage *= WulfrumBrandCereal.WulfrumDamageReduction;
                        break;
                    case NPC npc:
                        if (FablesSets.WulrumNPCs[npc.type])
                            modifiers.FinalDamage *= WulfrumBrandCereal.WulfrumDamageReduction;
                        break;
                }
            }
        }

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            //Melee ones dont set noaggro because they'd lose all contact dmg otherwise
            //player.npcTypeNoAggro[ModContent.NPCType<WulfrumGrappler>()] = true;
            player.npcTypeNoAggro[ModContent.NPCType<WulfrumMagnetizer>()] = true;
            player.npcTypeNoAggro[ModContent.NPCType<WulfrumMortar>()] = true;
            //player.npcTypeNoAggro[ModContent.NPCType<WulfrumRoller>()] = true;
            //player.npcTypeNoAggro[ModContent.NPCType<WulfrumRover>()] = true;


            player.SetPlayerFlag("WulfrumAmbassador");
        }
    }
}
