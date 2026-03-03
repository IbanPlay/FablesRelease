using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class WulfrumBigConduit : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Tungsten;
            Main.tileSolid[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileBlockLight[Type] = false;
            TileID.Sets.DrawsWalls[Type] = true;
            TileID.Sets.CanBeSloped[Type] = false;
            FablesSets.ConduitTiles3xVertical[Type] = true;
            FablesSets.ConduitTiles3xHorizontal[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.StyleHorizontal = false;
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.StyleWrapLimit = 3;
            TileObjectData.newTile.RandomStyleRange = 3;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.FlattenAnchors = true;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.AnchorBottom = default;

            //Anchored to the from the left
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Height, 0);
            TileObjectData.addAlternate(0);
            //Anchored to the right
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Height, 0);
            TileObjectData.addAlternate(0);
            //Anchored to the top
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidBottom, TileObjectData.newTile.Width, 0);
            TileObjectData.addAlternate(0);

            //Anchored to the floor
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.Platform, TileObjectData.newTile.Width, 0);
            TileObjectData.addTile(Type);

            RegisterItemDrop(ModContent.ItemType<WulfrumBigConduitItem>());
            AddMapEntry(new Color(55, 64, 58));

            FablesPlayer.GetPlacementOverlapCheckDimensionsEvent.Add(Type, OverlapCheckDimensions);
        }

        public void OverlapCheckDimensions(ref int width, ref int height)
        {
            width = 3;
            height = 3;
        }

        public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
        {
            Tile t = Main.tile[i, j];
            //Failsafe necessary due to manually calling tileframe
            if (!t.HasTile)
                return;

            int alignmentH = (t.TileFrameX / 18) % 3;
            int alignmentV = (t.TileFrameY / 18) % 3;

            i -= alignmentH;
            j -= alignmentV;

            //Dust.QuickDust(i, j, Color.Red);

            int originalFrameX = t.TileFrameX;

            Tile tileTop = Main.tile[i, j - 1];
            Tile tileBot = Main.tile[i, j + 3];
            Tile tileLeft = Main.tile[i - 1, j ];
            Tile tileRight = Main.tile[i + 3, j];

            t.TileFrameX = (short)(alignmentH * 18);
            short tileFrameOffset = 0;

            if (tileTop.HasTile && FablesSets.ConduitTiles3xVertical[tileTop.TileType] && (tileTop.TileFrameX / 18) % 3 == 0)
                tileFrameOffset += 54;
            if (tileLeft.HasTile && FablesSets.ConduitTiles3xHorizontal[tileLeft.TileType] && (tileLeft.TileFrameY / 18) % 3 == 0)
                tileFrameOffset += 108;
            if (tileRight.HasTile && FablesSets.ConduitTiles3xHorizontal[tileRight.TileType] && (tileRight.TileFrameY / 18) % 3 == 0)
                tileFrameOffset += 216;
            if (tileBot.HasTile && FablesSets.ConduitTiles3xVertical[tileBot.TileType] && (tileBot.TileFrameX / 18) % 3 == 0)
                tileFrameOffset += 432;
            t.TileFrameX += tileFrameOffset;

            //Apply the new frame for every tile making up the pipe
            if (t.TileFrameX != originalFrameX)
            {
                for (int x = i; x < i + 3; x++)
                {
                    for (int y = j; y < j + 3; y++)
                    {
                        t = Main.tile[x, y];
                        alignmentH = (t.TileFrameX / 18) % 3;
                        t.TileFrameX = (short)(alignmentH * 18 + tileFrameOffset);
                    }
                }

                WorldGen.TileFrame(i - 1, j);
                WorldGen.TileFrame(i + 3, j);
                WorldGen.TileFrame(i, j - 1);
                WorldGen.TileFrame(i, j + 3);
            }
        }


        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            Tile t = Main.tile[i, j];

            int alignmentH = (t.TileFrameX / 18) % 3;
            int alignmentV = (t.TileFrameY / 18) % 3;
            i -= alignmentH;
            j -= alignmentV;

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

    public class WulfrumBigConduitItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;
        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Large Wulfrum Conduit");
            Item.ResearchUnlockCount = 10;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<WulfrumBigConduit>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddIngredient(ModContent.ItemType<DullPlatingItem>(), 3).
                AddTile<BunkerWorkshop>().
                Register();
        }
    }
}