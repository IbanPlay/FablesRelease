using CalamityFables.Core.DrawLayers;

namespace CalamityFables.Content.Items.SirNautilusDrops
{
    public class DriedFishTail : ModItem, ILongBackAccessory
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;
        public string EquipTexture => AssetDirectory.SirNautilusDrops + Name + "_Back";

        public override void Load()
        {
            if (Main.dedServ)
                return;
            EquipLoader.AddEquipTexture(Mod, AssetDirectory.EarlyGameMisc + "PontiffsPiper_Head", EquipType.Back, this);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dried Fish Tail");
            Item.ResearchUnlockCount = 1;

            if (Main.dedServ)
                return;

            int slot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Back);
            ArmorIDs.Back.Sets.DrawInTailLayer[slot] = true;
            FablesSets.AddLongBackAccessory(Type, EquipType.Back, slot, this);
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;

            // Same values as a boss mask
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 75);
            Item.vanity = true;
            Item.accessory = true;

            if (!Main.dedServ)
                Item.backSlot = (sbyte)EquipLoader.GetEquipSlot(Mod, Name, EquipType.Back);

            Item.hasVanityEffects = true;
            Item.maxStack = 1;
        }
    }
}
