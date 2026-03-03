using CalamityFables.Content.Items.BurntDesert;
using CalamityFables.Core.DrawLayers;

namespace CalamityFables.Content.Items.VanityMisc
{
    [AutoloadEquip(EquipType.Head)]
    [ReplacingCalamity("OldHunterHat")]
    public class OldHunterHat : DesertProwlerHat
    {
        public override string Texture => AssetDirectory.MiscVanity + Name;

        public override void Load() { }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Old Hunter Hat");
            Tooltip.SetDefault("'Attire fashioned after the appearance of hunters from a faraway land'");

            //Shimmers into the desert prowler set
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<DesertProwlerHat>();

            if (Main.netMode == NetmodeID.Server)
                return;

            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHatHair[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            //Item.vanity = true;
        }

        public override void AddRecipes()
        {
            //No recipe
        }
    }

    [AutoloadEquip(EquipType.Body)]
    [ReplacingCalamity("OldHunterShirt")]
    public class OldHunterShirt : DesertProwlerShirt, IBulkyArmor
    {
        public override string Texture => AssetDirectory.MiscVanity + Name;
        public string BulkTexture => Texture + "_Bulk";

        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            EquipLoader.AddEquipTexture(Mod, Texture + "_Back", EquipType.Back, this);
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Old Hunter Shirt");
            Tooltip.SetDefault("'Attire fashioned after the appearance of hunters from a faraway land'");

            //Shimmers into the desert prowler set
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<DesertProwlerShirt>();

            if (Main.netMode == NetmodeID.Server)
                return;

            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);

            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            //Item.vanity = true;
        }

        public override void AddRecipes()
        {
            //No recipe
        }

        public override void EquipFrameEffects(Player player, EquipType type)
        {
            player.back = (sbyte)EquipLoader.GetEquipSlot(Mod, Name, EquipType.Back);
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    [ReplacingCalamity("OldHunterPants")]
    public class OldHunterPants : DesertProwlerPants
    {
        public override string Texture => AssetDirectory.MiscVanity + Name;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Old Hunter Pants");
            Tooltip.SetDefault("'Attire fashioned after the appearance of hunters from a faraway land'");

            //Shimmers into the desert prowler set
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<DesertProwlerPants>();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            //Item.vanity = true;
        }

        public override void AddRecipes()
        {
            //No recipe
        }
    }
}
