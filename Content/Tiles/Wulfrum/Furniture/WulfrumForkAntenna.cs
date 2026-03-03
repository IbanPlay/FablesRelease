using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumForkAntennaItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Fork Antenna");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumForkAntenna>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(4).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumForkAntenna : ModTile, ICustomPaintable
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
            MinPick = 30;
            DustType = DustID.DynastyWood;
            HitSound = SoundID.Item126;

            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = false;
            Main.tileWaterDeath[Type] = false;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.Table | AnchorType.SolidSide, 1, 1);
            TileObjectData.newTile.CoordinateHeights = Enumerable.Repeat(16, 2).ToArray();
            TileObjectData.newTile.UsesCustomCanPlace = true;

            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.AlternateTile, 1, 1);
            TileObjectData.newAlternate.AnchorAlternateTiles = new int[] { TileType<WulfrumAntenna>() };
            TileObjectData.addAlternate(0);
            TileObjectData.addTile(Type);

            AnimationFrameHeight = 18 * 2;
            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);

            AddMapEntry(CommonColors.WulfrumPipeworksBrown);
            FablesSets.CustomPaintedSprites[Type] = true;
            WulfrumAntenna.ForkAntennaType = Type;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Main.tile[i, j].TileFrameY != 0)
            {
                if (Main.tile[i, j].TileFrameX == 18 && Main.tileFrame[Type] == 0)
                {
                    float colMult = 1 / 265f * 0.5f;

                    r = CommonColors.WulfrumBlue.R * colMult;
                    g = CommonColors.WulfrumBlue.G * colMult;
                    b = CommonColors.WulfrumBlue.B * colMult;
                }
                return;
            }

            float colorMult = 1 / 265f * 0.5f * CommonColors.WulfrumLightMultiplierWithFlickerOffset((i - Main.tile[i, j].TileFrameX / 18) * 3 + j);
            r = CommonColors.WulfrumGreen.R * colorMult;
            g = CommonColors.WulfrumGreen.G * colorMult;
            b = CommonColors.WulfrumGreen.B * colorMult;
        }

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            int necessaryFrames = 7;

            frameCounter++;
            if (frameCounter > necessaryFrames)
            {
                frameCounter = 0;

                frame++;
                if (frame > 4)
                    frame = 0;
            }
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            Tile tile = Main.tile[i, j];
            i -= tile.TileFrameX / 18;
            j -= tile.TileFrameY / 18;
            int animationOffset = (int)FablesUtils.Modulo(i * 4 - j * 3 + j * i * i, 5);

            frameYOffset += animationOffset * AnimationFrameHeight;
            frameYOffset = frameYOffset % (AnimationFrameHeight * 5);
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            int xPos = tile.TileFrameX;
            int yPos = tile.TileFrameY;

            int xOffset = 0;
            int yOffset = Main.tileFrame[Type] * AnimationFrameHeight;
            AnimateIndividualTile(Type, i, j, ref xOffset, ref yOffset);
            yPos += yOffset;

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
