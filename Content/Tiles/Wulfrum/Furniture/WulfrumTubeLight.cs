using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumTubeLightItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Tube Light");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumTubeLight>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(4).
                AddIngredient(ItemID.Torch).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumTubeLight : ModTile, ICustomTileDrawOffset
    {
        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        internal Asset<Texture2D> GlowMask;

        public override void Load()
        {
            if (!Main.dedServ)
                GlowMask = Request<Texture2D>(Texture + "Glow");
        }

        public virtual int DropType => ItemType<WulfrumTubeLightItem>();

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.CursedTorch;
            HitSound = SoundID.Shatter;

            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = false;
            Main.tileWaterDeath[Type] = false;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.CoordinateHeights = [16];
            TileObjectData.newTile.AnchorTop = default;

            //Anchored to the left
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.Tree | AnchorType.AlternateTile, 1, 0);
            TileObjectData.newAlternate.AnchorAlternateTiles = new int[7] { 124, 561, 574, 575, 576, 577, 578 }; //Beams
            TileObjectData.newAlternate.DrawXOffset = -2;
            TileObjectData.newAlternate.DrawYOffset = 0;
            TileObjectData.addAlternate(1);
            //Anchored to the right
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.Tree | AnchorType.AlternateTile, 1, 0);
            TileObjectData.newAlternate.AnchorAlternateTiles = new int[7] { 124, 561, 574, 575, 576, 577, 578 };
            TileObjectData.newAlternate.DrawXOffset = 2;
            TileObjectData.newAlternate.DrawYOffset = 0;
            TileObjectData.addAlternate(2);

            //Wall anchored
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorWall = true;
            TileObjectData.newAlternate.DrawYOffset = 0;
            TileObjectData.addAlternate(3);

            TileObjectData.newTile.DrawYOffset = -2;
            TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.PlanterBox, 1, 0);
            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
            RegisterItemDrop(DropType);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Tube Light");
            AddMapEntry(CommonColors.FurnitureLightYellow, name);
            FablesSets.CustomDrawOffset[Type] = true;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float colorMult = 1 / 265f * 0.5f * CommonColors.WulfrumLightMultiplier;

            r = CommonColors.WulfrumGreen.R * colorMult;
            g = CommonColors.WulfrumGreen.G * colorMult;
            b = CommonColors.WulfrumGreen.B * colorMult;
        }

        public void DrawOffset(TileDrawInfo drawData, int i, int j, ref Vector2 drawPosition)
        {
            switch (drawData.tileFrameY / 18)
            {
                case 1:
                    drawPosition.X += -2f;
                    break;
                case 2:
                    drawPosition.X += 2f;
                    break;
            }

            //Counteract the tiloedrawoffset which applies to every style instead of jus the one
            if (drawData.tileFrameY > 0)
                drawPosition.Y += 2;
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            Tile t = Main.tile[i, j];

            //Horizontal ceiling lamps
            if (t.TileFrameY == 0)
            {
                Tile leftTile = Framing.GetTileSafely(i - 1, j);
                Tile rightTile = Framing.GetTileSafely(i + 1, j);
                if (leftTile.HasTile && leftTile.TileType == type)
                    frameXOffset += 36;
                if (rightTile.HasTile && rightTile.TileType == type)
                    frameXOffset += 18;
            }
            //Vertical wall lamps
            else
            {
                Tile topTile = Framing.GetTileSafely(i, j - 1);
                Tile botTile = Framing.GetTileSafely(i, j + 1);
                if (topTile.HasTile && topTile.TileType == type)
                    frameXOffset += 36;
                if (botTile.HasTile && botTile.TileType == type)
                    frameXOffset += 18;
            }
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
            Color drawColour = GetDrawColour(i, j, Color.White);

            switch (tile.TileFrameY / 18)
            {
                case 0:
                    drawOffset.Y -= 2;
                    break;
                case 1:
                    drawOffset.X += -2f;
                    break;
                case 2:
                    drawOffset.X += 2f;
                    break;
            }

            Main.spriteBatch.Draw(glowmask, drawOffset, new Rectangle(xPos, yPos, 18, 18), drawColour, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
        }

        protected Color GetDrawColour(int i, int j, Color colour)
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
