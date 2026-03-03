using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumBathtubItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Bathtub");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumBathtub>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 3, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(8).
                AddIngredient(ItemID.WaterBucket).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumBathtub : ModTile, ICustomPaintable
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;

            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = false;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style4x2);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft; // Player faces to the left

            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile); // Copy everything from above, saves us some code
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight; // Player faces to the right
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Bathtub");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
            FablesSets.CustomPaintedSprites[Type] = true;
            AdjTiles = new int[] { TileID.Bathtubs };
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }
}
