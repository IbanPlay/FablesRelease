using CalamityFables.Content.Items.Wulfrum;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class SandyTent : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sandy Tent");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<SandyTentTile>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.Silk, 10).
                AddIngredient(ItemID.PalmWood, 5).
                AddTile(TileID.Loom).
                Register();
        }
    }

    public class SandyTentTile : ModTile
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Width = 6;
            TileObjectData.newTile.Height = 4;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleWrapLimit = 2;
            TileObjectData.newTile.Origin = new Point16(3, 3);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16, 16 };
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop , TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight;


            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile); 
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft; 
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);
            TileID.Sets.DisableSmartCursor[Type] = true;
            DustType = DustID.Hay;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Tent");
            AddMapEntry(new Color(200, 175, 114), name);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            offsetY = 2;
        }
    }
}
