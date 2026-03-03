namespace CalamityFables.Core
{
    public class BossMaskLoader
    {
        public static Mod Mod => CalamityFables.Instance;

        public static int LoadBossMask(string bossName, string bossDisplayName, string texturePath, bool hideHead = false)
        {
            //Load the relic item but cache it first
            AutoloadedBossMask maskItem = new AutoloadedBossMask(bossName, bossDisplayName, texturePath, hideHead);
            Mod.AddContent(maskItem);

            //Return the relic item's type so that npcs can drop it
            return maskItem.Type;
        }
    }

    [Autoload(false)]
    [AutoloadEquip(EquipType.Head)]
    public class AutoloadedBossMask : ModItem
    {
        public string InternalName = "";
        public string Itemname;
        public bool HideHead;

        private readonly string TexturePath;

        protected override bool CloneNewInstances => true;

        public override string Name => InternalName != "" ? InternalName : base.Name;
        public override string Texture => string.IsNullOrEmpty(TexturePath) ? AssetDirectory.DebugSquare : TexturePath + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault(Itemname ?? "ERROR");
            Item.ResearchUnlockCount = 1;

            if (HideHead && !Main.dedServ)
            {
                int headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
                ArmorIDs.Head.Sets.DrawHead[headSlot] = false;
            }
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

        public AutoloadedBossMask(string NPCName, string NPCDisplayName, string texturePath, bool hideHead = false)
        {
            InternalName = NPCName + "BossMask";
            Itemname = NPCDisplayName + " Mask";
            TexturePath = texturePath;
            HideHead = hideHead;
        }
    }
}
