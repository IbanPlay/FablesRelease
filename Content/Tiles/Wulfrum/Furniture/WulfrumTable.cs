using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumTableItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Table");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumTable>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 3, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(8).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumTable : ModTile, ICustomPaintable
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
            Main.tileSolidTop[Type] = true;
            Main.tileTable[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.LavaDeath = true;
            //TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
            TileObjectData.addTile(Type);
            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Table");
            AddMapEntry(CommonColors.WulfrumMetalLight, name);
            AdjTiles = new int[] { TileID.Tables };
            FablesSets.CustomPaintedSprites[Type] = true;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }
}
