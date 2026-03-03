using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.BurntDesert;
using CalamityFables.Content.Tiles.VanityTrees;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class BunkerWorkshop : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileNoAttach[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 5;
            TileObjectData.newTile.Height = 4;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(2, 3);
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 18];
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight; 
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile); 
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft; 
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
            DustType = DustID.Iron;
            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(114, 110, 101), name);
            RegisterItemDrop(ModContent.ItemType<BunkerWorkshopItem>());
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    public class BunkerWorkshopItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void Load()
        {
            FablesNPC.ModifyShopEvent += AddToMechanic;
        }

        private void AddToMechanic(NPCShop shop)
        {
            if (shop.NpcType == NPCID.Mechanic)
            {
                shop.Add(Type, Condition.InDirtLayerHeight);
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Bunker Workshop");
            Tooltip.SetDefault("Used for special crafting");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<BunkerWorkshop>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 5, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.CraftingObjects;
        }
    }
}
