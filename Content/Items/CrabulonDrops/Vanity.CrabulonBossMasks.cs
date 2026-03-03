using CalamityFables.Content.NPCs.Sky;
using Terraria.DataStructures;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    [AutoloadEquip(EquipType.Head)]
    public class CrabulonBossMask : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;
        public static int equipSlot;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crabulon Mask");

            //Cycled through shimmer
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<CrabulonBossMaskAlt1>();
            ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<CrabulonBossMaskAlt1>()] = ModContent.ItemType<CrabulonBossMaskAlt2>();
            ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<CrabulonBossMaskAlt2>()] = Type;

            if (Main.dedServ)
                return;
            equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;

            // Common values for every boss mask
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 75);
            Item.vanity = true;
            Item.maxStack = 1;
        }
    }

    [AutoloadEquip(EquipType.Head)]
    public class CrabulonBossMaskAlt1 : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public static int equipSlot;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fungal Crabulon Mask");
            if (Main.dedServ)
                return;
            equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;

            // Common values for every boss mask
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 75);
            Item.vanity = true;
            Item.maxStack = 1;
        }
    }

    [AutoloadEquip(EquipType.Head)]
    public class CrabulonBossMaskAlt2 : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;
        public static int equipSlot;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hairy Crabulon Mask");
            if (Main.dedServ)
                return;
            equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;

            // Common values for every boss mask
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 75);
            Item.vanity = true;
            Item.maxStack = 1;
        }
    }
}
