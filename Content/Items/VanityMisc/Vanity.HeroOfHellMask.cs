using CalamityFables.Content.NPCs.Sky;
using Terraria.DataStructures;
using Terraria.Localization;

namespace CalamityFables.Content.Items.VanityMisc
{
    [AutoloadEquip(EquipType.Head)]
    public class HeroOfHellMask : ModItem
    {
        public override string Texture => AssetDirectory.MiscVanity + Name;
        public static int equipSlot;
        public static int equipSlotHurt;

        public override void Load()
        {
            FablesItem.IsArmorSetEvent += SpoofAshWoodSet;
            FablesItem.UpdateArmorSetEvent += SpoofAshwoodSetBonus;

            if (Main.dedServ)
                return;

            equipSlotHurt = EquipLoader.AddEquipTexture(Mod, Texture + "_HeadHurt", EquipType.Head, this, "HeroOfHellHurt");
        }

        private string SpoofAshWoodSet(Item head, Item body, Item legs)
        {
            if ((head.headSlot == 278 || head.type == Type ) &&
                (body.bodySlot == 246 || body.type == ModContent.ItemType<HeroOfHellChestplate>()) && 
                (legs.legSlot == 234 || legs.type == ModContent.ItemType<HeroOfHellGreaves>()))
                return "CalamityFables.AshwoodSpoof";
            return "";
        }

        private void SpoofAshwoodSetBonus(Player player, string set)
        {
            if (set == "CalamityFables.AshwoodSpoof")
            {
                player.setBonus = Language.GetTextValue("ArmorSetBonus.AshWood");
                player.ashWoodBonus = true;
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hero of Hell Mask");
            Tooltip.SetDefault("'Save me...'");

            ItemID.Sets.ShimmerTransformToItem[ItemID.AshWoodHelmet] = Type;

            if (Main.dedServ)
                return;
            equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.AshWoodHelmet);
            Item.width = 24;
            Item.height = 24;
            Item.headSlot = equipSlot;
        }

        public override void EquipFrameEffects(Player player, EquipType type)
        {
            if (player.GetModPlayer<FablesPlayer>().JustHurtTimer > 0.5f)
                player.head = equipSlotHurt;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class HeroOfHellChestplate: ModItem
    {
        public override string Texture => AssetDirectory.MiscVanity + Name;
        public static int regularEquipSlot;
        public static int altEquipSlot;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            altEquipSlot = EquipLoader.AddEquipTexture(Mod, Texture + "_Body2", EquipType.Body, this, "HeroOfHellBody2");
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hero of Hell Chestplate");
            ItemID.Sets.ShimmerTransformToItem[ItemID.AshWoodBreastplate] = Type;


            if (Main.dedServ)
                return;

            ArmorIDs.Body.Sets.HidesArms[altEquipSlot] = true;
            ArmorIDs.Body.Sets.HidesTopSkin[altEquipSlot] = true;
            regularEquipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);

            ArmorIDs.Body.Sets.HidesArms[regularEquipSlot] = true;
            ArmorIDs.Body.Sets.HidesTopSkin[regularEquipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.AshWoodBreastplate);
            Item.bodySlot = regularEquipSlot;
        }

        public override void EquipFrameEffects(Player player, EquipType type)
        {
            if (player.miscCounter % 7 >= 4)
                player.body = altEquipSlot;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class HeroOfHellGreaves : ModItem
    {
        public override string Texture => AssetDirectory.MiscVanity + Name;
        public static int equipSlot;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hero of Hell Greaves");
            ItemID.Sets.ShimmerTransformToItem[ItemID.AshWoodGreaves] = Type;
            if (Main.dedServ)
                return;

            equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.AshWoodGreaves);
            Item.legSlot = equipSlot;
        }
    }
}
