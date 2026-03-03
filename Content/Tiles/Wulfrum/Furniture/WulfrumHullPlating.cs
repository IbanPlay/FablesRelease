using CalamityFables.Content.Items.Wulfrum;
using Terraria.DataStructures;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumHull : ModTile, ICustomPaintable
    {
        public static Dictionary<int, Asset<Texture2D>> EdgeTextures = new Dictionary<int, Asset<Texture2D>>();

        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

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
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;

            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            AddMapEntry(CommonColors.WulfrumMetalLight);
            FablesSets.CustomPaintedSprites[Type] = true;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            return FablesUtils.BetterGemsparkFraming(i, j, resetFrame);
        }

        public static readonly int[,] HullPlatingPattern = new int[,]
        {
            {  1,  0,  0,  0,  2,  3,  4,  3 },
            {  5,  0,  0,  0,  0,  0,  0,  0 },
            {  6,  0,  7,  8,  8,  8,  9,  8 },
            {  0,  0, 10,  0,  0,  0, 10,  0 },
            {  8,  8, 11,  0, 12,  0, 13,  8 },
            {  0,  0,  0,  0, 10,  0, 10,  0 },
            {  0,  0,  0,  0, 10,  0, 10,  0 },
            {  0,  0,  0,  0, 10,  0, 10,  0 }
        };

        public static Vector2 FrameOffsets(int i, int j, Texture2D texture)
        {
            int frame = HullPlatingPattern[j % 8, i % 8];
            int xOffset = frame % 5;
            int yOffset = frame / 5;
            return new Vector2(xOffset * (texture.Width / 5), yOffset * (texture.Height / 3));
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            Vector2 offsets = FrameOffsets(i, j, Terraria.GameContent.TextureAssets.Tile[Type].Value);
            frameXOffset += (int)offsets.X;
            frameYOffset += (int)offsets.Y;
        }


        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (Main.tile[i, j - 1].TileType != Type && Main.tile[i, j].Slope == SlopeType.Solid && !Main.tile[i, j].IsHalfBlock)
                Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
        }

        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero;

            int paintColor = Main.tile[i, j].TileColor;
            if (paintColor >= 17)
                paintColor = 0;
            if (!EdgeTextures.TryGetValue(Main.tile[i, j].TileColor, out Asset<Texture2D> texture))
            {
                string texturePath = Texture + "Edges";
                if (paintColor != 0)
                    texturePath = AssetDirectory.WulfrumFurniturePaint + Name + "Edges_Paint" + paintColor.ToString();
                texture = Request<Texture2D>(texturePath);
                EdgeTextures.Add(Main.tile[i, j].TileColor, texture);
            }

            Texture2D edges = texture.Value;
            Color drawColor = GetDrawColour(i, j, Lighting.GetColor(i, j));

            if (Main.tile[i - 1, j].TileType != Type)
                Main.spriteBatch.Draw(edges, drawOffset - Vector2.UnitX * 16, new Rectangle(0, 0, 18, 18), drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            if (Main.tile[i + 1, j].TileType != Type)
                Main.spriteBatch.Draw(edges, drawOffset + Vector2.UnitX * 16, new Rectangle(18, 0, 18, 18), drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];

            int xOffset = 0;
            int yOffset = 0;
            AnimateIndividualTile(Type, i, j, ref xOffset, ref yOffset);

            int xPos = tile.TileFrameX;
            int yPos = tile.TileFrameY;
            xPos += xOffset;
            yPos += yOffset;

            Texture2D glowmask = GlowMask.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero;
            Color drawColour = GetDrawColour(i, j, Color.White) * CommonColors.WulfrumLightMultiplierWithFlickerOffset(i * 3 + j);

            Main.spriteBatch.Draw(glowmask, drawOffset, new Rectangle(xPos, yPos, 18, 18), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
        }

        private Color GetDrawColour(int i, int j, Color col)
        {
            int colType = Main.tile[i, j].TileColor;
            Color paintCol = WorldGen.paintColor(colType);

            if (colType >= 13)
            {
                if (colType < 13)
                {
                    paintCol.R = (byte)((paintCol.R / 2f) + 128);
                    paintCol.G = (byte)((paintCol.G / 2f) + 128);
                    paintCol.B = (byte)((paintCol.B / 2f) + 128);
                }
                if (colType == 29)
                {
                    paintCol = Color.Black;
                }
            }
            else
                paintCol = Color.White;

            col.R = (byte)(paintCol.R / 255f * col.R);
            col.G = (byte)(paintCol.G / 255f * col.G);
            col.B = (byte)(paintCol.B / 255f * col.B);
            return col;
        }
    }

    public class WulfrumHullItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Hull Plating");
            Tooltip.SetDefault("'Not to be confused with Wulfrum Dull Plating!'");
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumHull>());
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe(10).
                AddIngredient<WulfrumMetalScrap>().
                AddTile(TileID.Anvils).
                Register();

            CreateRecipe().
                AddIngredient<WulfrumHullWallItem>(4).
                AddTile<WulfrumWorkshop>().
                Register();

            CreateRecipe().
                AddIngredient<WulfrumPlatformItem>(4).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumHullWall : ModWall, ICustomPaintable, IWallFrameAnimatable
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public static Asset<Texture2D> GlowMask;

        public override void Load()
        {
            if (!Main.dedServ)
                GlowMask = Request<Texture2D>(Texture + "Glow");
        }

        public override void SetStaticDefaults()
        {
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;
            Main.wallHouse[Type] = true;
            Main.wallBlend[Type] = Type;

            AddMapEntry(CommonColors.WulfrumMetalDark);
            FablesSets.CustomPaintedWalls[Type] = true;
            FablesSets.IndividuallyAnimatedWall[Type] = true;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;


        public void AnimateIndividualWall(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            Vector2 offsets = WulfrumHull.FrameOffsets(i, j, Terraria.GameContent.TextureAssets.Wall[Type].Value);
            frameXOffset += (int)offsets.X;
            frameYOffset += (int)offsets.Y;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];

            int xOffset = 0;
            int yOffset = 0;
            AnimateIndividualWall(Type, i, j, ref xOffset, ref yOffset);

            int xPos = tile.WallFrameX;
            int yPos = tile.WallFrameY;
            xPos += xOffset;
            yPos += yOffset;

            Texture2D glowmask = GlowMask.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero - Vector2.One * 8;
            Color drawColour = GetDrawColour(i, j) * CommonColors.WulfrumLightMultiplierWithFlickerOffset(i * 3 + j);

            Main.spriteBatch.Draw(glowmask, drawOffset, new Rectangle(xPos, yPos, 36, 36), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
        }

        private Color GetDrawColour(int i, int j)
        {
            int colType = Main.tile[i, j].WallColor;
            Color paintCol = WorldGen.paintColor(colType);

            if (colType >= 13)
            {
                if (colType < 13)
                {
                    paintCol.R = (byte)((paintCol.R / 2f) + 128);
                    paintCol.G = (byte)((paintCol.G / 2f) + 128);
                    paintCol.B = (byte)((paintCol.B / 2f) + 128);
                }
                if (colType == 29)
                {
                    paintCol = Color.Black;
                }
            }
            else
                paintCol = Color.White;
            return paintCol;
        }
    }


    public class WulfrumHullWallItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Hull Plating Wall");
            Item.ResearchUnlockCount = 400;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableWall((ushort)WallType<WulfrumHullWall>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(4).
                AddIngredient<WulfrumHullItem>().
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }
}