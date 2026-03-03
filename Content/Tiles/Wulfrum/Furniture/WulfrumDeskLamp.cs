using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumDeskLampItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Desk Lamp");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumDeskLamp>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 50);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(2).
                AddIngredient(ItemID.Torch).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumDeskLamp : ModTile, ICustomPaintable, ICustomPlaceSounds
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public SoundStyle PlaceSound => SoundDirectory.CommonSounds.WulfrumTilePlaceSound;


        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public static Asset<Texture2D> GlowMask;

        public override void Load()
        {
            if (!Main.dedServ)
                GlowMask = Request<Texture2D>(Texture + "Glow");
        }

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.DynastyWood;
            HitSound = SoundID.Shatter;

            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = false;
            Main.tileWaterDeath[Type] = false;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.Table | AnchorType.SolidSide, 2, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft; // Player faces to the left
            TileObjectData.newTile.StyleHorizontal = true;

            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile); // Copy everything from above, saves us some code
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight; // Player faces to the right
            TileObjectData.addAlternate(1);

            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
            AdjTiles = new int[] { TileID.Candelabras };
            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Desk Lamp");
            AddMapEntry(CommonColors.FurnitureLightYellow, name);
            FablesSets.CustomPaintedSprites[Type] = true;
            FablesSets.CustomPlaceSound[Type] = true;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Main.tile[i, j].TileFrameY == 0)
            {
                float colMult = 1 / 265f * 0.9f * CommonColors.WulfrumLightMultiplierWithFlickerOffset((i - Main.tile[i, j].TileFrameX / 18) * 3 + j);

                r = CommonColors.WulfrumGreen.R * colMult;
                g = CommonColors.WulfrumGreen.G * colMult;
                b = CommonColors.WulfrumGreen.B * colMult;
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            int xPos = tile.TileFrameX;
            int yPos = tile.TileFrameY;

            Texture2D glowmask = GlowMask.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero;
            Color drawColour = GetDrawColour(i, j, Color.White) * CommonColors.WulfrumLightMultiplierWithFlickerOffset((i - Main.tile[i, j].TileFrameX / 18) * 3 + j);
            Main.spriteBatch.Draw(glowmask, drawOffset, new Rectangle(xPos, yPos, 18, 18), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
        }

        private Color GetDrawColour(int i, int j, Color colour)
        {
            int colType = Main.tile[i, j].TileColor;
            Color paintCol = WorldGen.paintColor(colType);
            if (colType >= 13 && colType <= 24)
            {
                colour.R = (byte)(paintCol.R / 255f * colour.R);
                colour.G = (byte)(paintCol.G / 255f * colour.G);
                colour.B = (byte)(paintCol.B / 255f * colour.B);
            }
            return colour;
        }
    }
}
