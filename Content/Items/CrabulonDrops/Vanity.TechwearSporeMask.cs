using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;
using CalamityFables.Content.Boss.MushroomCrabBoss;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    public class TechwearSporeMask : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void Load()
        {
            FablesNPC.ModifyNPCLootEvent += DropFromSporeSkeletons;
        }

        private void DropFromSporeSkeletons(NPC npc, NPCLoot npcloot)
        {
            if (npc.type == NPCID.SporeSkeleton)
            {
                npcloot.Add(Type, new Fraction(1, 25));
            }
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Techwear Spore Mask");
            Tooltip.SetDefault("'Due to aesthetics being prioritized over functionality, this mask does not actually protect against spores...'");
            FablesSets.IsGogglesVanity[Type] = true;
            FablesPlayer.UpdateVisibleAccessoryEvent.Add(Type, UpdateVisibleAccessory);
        }

        public override void SetDefaults()
        {
            Item.DefaultToAccessory();
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 10);
            Item.vanity = true;
        }

        public void UpdateVisibleAccessory(Player player, int itemSlot, Item item, bool modded)
        {
             player.SetPlayerFlag(Name);
        }
    }
}