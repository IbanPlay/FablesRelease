using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class WulfrumBigConduitCouplingVertical : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Iron;
            Main.tileSolid[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileBlockLight[Type] = true;
            TileID.Sets.DrawsWalls[Type] = true;
            TileID.Sets.CanBeSloped[Type] = false;
            FablesSets.ConduitTiles3xVertical[Type] = true;
            TileID.Sets.DontDrawTileSliced[Type] = true; //Necessary or else the extra with thingy breaks

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.StyleHorizontal = false;
            TileObjectData.newTile.Origin = new Point16(1, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16 };
            TileObjectData.newTile.CoordinateWidth = 20;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.FlattenAnchors = true;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.AnchorBottom = default;

            //Anchored to the top
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidBottom, TileObjectData.newTile.Width, 0);
            TileObjectData.addAlternate(0);

            //Anchored to the floor
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.Platform, TileObjectData.newTile.Width, 0);
            TileObjectData.addTile(Type);
            
            RegisterItemDrop(ModContent.ItemType<WulfrumBigConduitCouplingItem>());
            AddMapEntry(new Color(131, 128, 122));

            FablesPlayer.GetPlacementOverlapCheckDimensionsEvent.Add(Type, OverlapCheckDimensions);
        }

        public void OverlapCheckDimensions(ref int width, ref int height)
        {
            width = 3;
            height = 1;
        }

        public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
        {
            Tile t = Main.tile[i, j];
            //Failsafe necessary due to manually calling tileframe
            if (!t.HasTile)
                return;

            int alignmentH = (t.TileFrameX / 18) % 3;
            i -= alignmentH;

            //Dust.QuickDust(i, j, Color.Red);

            int originalFrameY = t.TileFrameY;

            Tile tileTop = Main.tile[i, j - 1];
            Tile tileBot = Main.tile[i, j + 1];

            t.TileFrameY = 0;
            short tileFrameOffset = 0;

            if (tileBot.HasTile && FablesSets.ConduitTiles3xVertical[tileBot.TileType] && (tileBot.TileFrameX / 18) % 3 == 0)
                tileFrameOffset += 18;
            if (tileTop.HasTile && FablesSets.ConduitTiles3xVertical[tileTop.TileType] && (tileTop.TileFrameX / 18) % 3 == 0)
                tileFrameOffset += 36;
            t.TileFrameY += tileFrameOffset;

            //Apply the new frame for every tile making up the pipe
            if (t.TileFrameY != originalFrameY)
            {
                for (int x = i; x < i + 3; x++)
                {
                    t = Main.tile[x, j];
                    t.TileFrameY = tileFrameOffset;
                }

                WorldGen.TileFrame(i - 1, j);
                WorldGen.TileFrame(i + 3, j);
                WorldGen.TileFrame(i, j - 1);
                WorldGen.TileFrame(i, j + 1);
            }
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            width = 20;
        }

        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            Tile t = Main.tile[i, j];
            int alignmentH = (t.TileFrameX / 18) % 3;
            i -= alignmentH;
            for (int x = i; x < i + 3; x++)
            {
                Tile tileAbove = Main.tile[x, j - 1];
                if (WorldGen.IsAContainer(tileAbove))
                    return false;
            }
            return true;
        }

        public override bool Slope(int i, int j) => false;
    }

    public class WulfrumBigConduitCouplingHorizontal : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Iron;
            Main.tileSolid[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileBlockLight[Type] = true;
            TileID.Sets.DrawsWalls[Type] = true;
            TileID.Sets.CanBeSloped[Type] = false;
            FablesSets.ConduitTiles3xHorizontal[Type] = true;
            TileID.Sets.DontDrawTileSliced[Type] = true; //Necessary or else the extra with thingy breaks

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.StyleHorizontal = false;
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 20, 20, 20 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.FlattenAnchors = true;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.AnchorBottom = default;
            TileObjectData.newTile.DrawYOffset = -2;

            //Anchored to the right
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Height, 0);
            TileObjectData.addAlternate(0);

            //Anchored to the left
            TileObjectData.newTile.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Height, 0);
            TileObjectData.addTile(Type);

            ModContent.GetInstance<WulfrumBigConduitCouplingItem>().HorizontalConnectorType = Type;

            RegisterItemDrop(ModContent.ItemType<WulfrumBigConduitCouplingItem>());
            AddMapEntry(new Color(131, 128, 122));

            FablesPlayer.GetPlacementOverlapCheckDimensionsEvent.Add(Type, OverlapCheckDimensions);
        }

        public void OverlapCheckDimensions(ref int width, ref int height)
        {
            width = 1;
            height = 3;
        }

        public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
        {
            Tile t = Main.tile[i, j];
            //Failsafe necessary due to manually calling tileframe
            if (!t.HasTile)
                return;

            int alignmentV = (t.TileFrameY / 18) % 3;
            j -= alignmentV;

            int originalFrameX = t.TileFrameX;

            Tile tileLeft = Main.tile[i - 1, j];
            Tile tileRight = Main.tile[i + 1, j];

            t.TileFrameX = 0;
            short tileFrameOffset = 0;

            if (tileRight.HasTile && FablesSets.ConduitTiles3xHorizontal[tileRight.TileType] && (tileRight.TileFrameY / 18) % 3 == 0)
                tileFrameOffset += 18;
            if (tileLeft.HasTile && FablesSets.ConduitTiles3xHorizontal[tileLeft.TileType] && (tileLeft.TileFrameY / 18) % 3 == 0)
                tileFrameOffset += 36;
            t.TileFrameX += tileFrameOffset;

            //Apply the new frame for every tile making up the pipe
            if (t.TileFrameX != originalFrameX)
            {
                for (int y = j; y < j + 3; y++)
                {
                    t = Main.tile[i, y];
                    t.TileFrameX = tileFrameOffset;
                }

                WorldGen.TileFrame(i - 1, j);
                WorldGen.TileFrame(i + 1, j);
                WorldGen.TileFrame(i, j - 1);
                WorldGen.TileFrame(i, j + 3);
            }
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            //offsetY -= 2;
        }

        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            Tile tileAbove = Main.tile[i, j - 1];
            if (WorldGen.IsAContainer(tileAbove))
                return false;
            return true;
        }

        public override bool Slope(int i, int j) => false;
    }

    public class WulfrumBigConduitCouplingItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;
        public override void Load()
        {
            FablesPlayer.OverridePlacedTileEvent += DecideOrientation;
        }

        public int HorizontalConnectorType;

        private void DecideOrientation(Player player, Tile targetTile, Item item, ref int tileToPlace, ref int previewPlaceStyle, ref bool? overrideCanPlace)
        {
            if (tileToPlace != Item.createTile)
                return;

            int i = Player.tileTargetX;
            int j = Player.tileTargetY;
            Tile tileTop = Main.tile[i, j - 1];
            Tile tileBot = Main.tile[i, j + 1];
            Tile tileLeft = Main.tile[i - 1, j];
            Tile tileRight = Main.tile[i + 1, j];

            //Can't be horizontal if we have a solid tile below
            if (tileBot.HasTile && !TileID.Sets.BreakableWhenPlacing[tileBot.TileType])
                return;

            //Won't turn horizontal if theres a full pipe above and the space to place it 
            if ((tileTop.HasTile && FablesSets.ConduitTiles3xVertical[tileTop.TileType]) &&
                (!tileRight.HasTile || TileID.Sets.BreakableWhenPlacing[tileRight.TileType]))
            {
                Tile tileTopRight = Main.tile[i + 1, j - 1];
                if (tileTopRight.HasTile && FablesSets.ConduitTiles3xVertical[tileTop.TileType])
                    return;
            }

            //Turn horizontal if theres a pipe to the left or right
            if (tileRight.HasTile && FablesSets.ConduitTiles3xHorizontal[tileRight.TileType])
            {
                Tile tileBotRight = Main.tile[i + 1, j + 1];
                if (tileBotRight.HasTile && FablesSets.ConduitTiles3xHorizontal[tileBotRight.TileType])
                {
                    tileToPlace = HorizontalConnectorType;
                    return;
                }
            }

            if (tileLeft.HasTile && FablesSets.ConduitTiles3xHorizontal[tileLeft.TileType])
            {
                Tile tileBotLeft = Main.tile[i - 1, j + 1];
                if (tileBotLeft.HasTile && FablesSets.ConduitTiles3xHorizontal[tileBotLeft.TileType])
                {
                    tileToPlace = HorizontalConnectorType;
                    return;
                }
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Large Wulfrum Conduit Coupling");
            Item.ResearchUnlockCount = 10;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<WulfrumBigConduitCouplingVertical>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddIngredient(ModContent.ItemType<DullPlatingItem>(), 2).
                AddTile<BunkerWorkshop>().
                Register();
        }
    }
}