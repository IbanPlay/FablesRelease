using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Core
{
    public class BossTrophyLoader
    {
        public static Mod Mod => CalamityFables.Instance;

        public static int LoadBossTrophy(string bossName, string bossDisplayName, string texturePath)
        {
            //Load the relic item but cache it first
            AutoloadedBossTrophyItem trophyItem = new AutoloadedBossTrophyItem(bossName, bossDisplayName, texturePath);
            Mod.AddContent(trophyItem);

            //Load the tile using the item's tile type
            AutoloadedBossTrophy trophyTile = new AutoloadedBossTrophy(bossName, trophyItem.Type, texturePath);
            Mod.AddContent(trophyTile);

            //Set the relic item's type to be the one of the relic tile (so it can properly place it)
            trophyItem.TileType = trophyTile.Type;

            //Return the relic item's type so that npcs can drop it
            return trophyItem.Type;
        }
    }

    [Autoload(false)]
    public class AutoloadedBossTrophy : ModTile
    {
        public override string Texture => TexturePath + InternalName;
        public override string Name => InternalName != "" ? InternalName : base.Name;

        public string InternalName;
        protected readonly int ItemType;
        protected readonly string TexturePath;


        public AutoloadedBossTrophy(string NPCname, int dropType, string path = null)
        {
            InternalName = NPCname + "TrophyTile";
            ItemType = dropType;
            TexturePath = path;
        }

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.FramesOnKillWall[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(120, 85, 60), Language.GetText("MapObject.Trophy"));
            DustType = 7;
        }
    }

    [Autoload(false)]
    public class AutoloadedBossTrophyItem : ModItem
    {
        public string InternalName = "";
        public string Itemname;
        public int TileType;

        private readonly string TexturePath;

        protected override bool CloneNewInstances => true;

        public override string Name => InternalName != "" ? InternalName : base.Name;
        public override string Texture => string.IsNullOrEmpty(TexturePath) ? AssetDirectory.DebugSquare : TexturePath + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault(Itemname ?? "ERROR");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType);

            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(0, 1);
        }

        public AutoloadedBossTrophyItem(string NPCName, string NPCDisplayName, string texturePath)
        {
            InternalName = NPCName + "TrophyItem";
            Itemname = NPCDisplayName + " Trophy";
            TexturePath = texturePath;
        }
    }
}
