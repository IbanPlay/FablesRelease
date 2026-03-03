using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class WulfrumConduit : ModTile
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
            FablesSets.ConduitTiles2xVertical[Type] = true;
            FablesSets.ConduitTiles2xHorizontal[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.StyleHorizontal = false;
            TileObjectData.newTile.StyleWrapLimit = 3;
            TileObjectData.newTile.RandomStyleRange = 3;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
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

            RegisterItemDrop(ModContent.ItemType<WulfrumConduitItem>());
            AddMapEntry(new Color(102, 136, 99));

            FablesPlayer.GetPlacementOverlapCheckDimensionsEvent.Add(Type, OverlapCheckDimensions);
        }

        public void OverlapCheckDimensions(ref int width, ref int height)
        {
            width = 2;
            height = 2;
        }

        public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
        {
            Tile t = Main.tile[i, j];
            //Failsafe necessary due to manually calling tileframe
            if (!t.HasTile)
                return;

            int alignmentH = (t.TileFrameX / 18) % 2;
            int alignmentV = (t.TileFrameY / 18) % 2;

            i -= alignmentH;
            j -= alignmentV;

            //Dust.QuickDust(i, j, Color.Red);

            int originalFrameX = t.TileFrameX;

            Tile tileTop = Main.tile[i, j - 1];
            Tile tileBot = Main.tile[i, j + 2];
            Tile tileLeft = Main.tile[i - 1, j ];
            Tile tileRight = Main.tile[i + 2, j];


            t.TileFrameX = (short)(alignmentH * 18);
            short tileFrameOffset = 0;

            if (tileTop.HasTile && FablesSets.ConduitTiles2xVertical[tileTop.TileType] && (tileTop.TileFrameX / 18) % 2 == 0)
                tileFrameOffset += 36;
            if (tileLeft.HasTile && FablesSets.ConduitTiles2xHorizontal[tileLeft.TileType] && (tileLeft.TileFrameY / 18) % 2 == 0)
                tileFrameOffset += 72;
            if (tileRight.HasTile && FablesSets.ConduitTiles2xHorizontal[tileRight.TileType] && (tileRight.TileFrameY / 18) % 2 == 0)
                tileFrameOffset += 144;
            if (tileBot.HasTile && FablesSets.ConduitTiles2xVertical[tileBot.TileType] && (tileBot.TileFrameX / 18) % 2 == 0)
                tileFrameOffset += 288;
            t.TileFrameX += tileFrameOffset;

            //Apply the new frame for every tile making up the pipe
            if (t.TileFrameX != originalFrameX)
            {
                for (int x = i; x < i + 2; x++)
                {
                    for (int y = j; y < j + 2; y++)
                    {
                        t = Main.tile[x, y];
                        alignmentH = (t.TileFrameX / 18) % 2;
                        t.TileFrameX = (short)(alignmentH * 18 + tileFrameOffset);
                    }
                }

                WorldGen.TileFrame(i - 1, j);
                WorldGen.TileFrame(i + 2, j);
                WorldGen.TileFrame(i, j - 1);
                WorldGen.TileFrame(i, j + 2);
            }
        }


        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            Tile t = Main.tile[i, j];

            int alignmentH = (t.TileFrameX / 18) % 2;
            int alignmentV = (t.TileFrameY / 18) % 2;
            i -= alignmentH;
            j -= alignmentV;

            for (int x = i; x < i + 2; x++)
            {
                Tile tileAbove = Main.tile[x, j - 1];
                if (WorldGen.IsAContainer(tileAbove))
                    return false;
            }
            return true;
        }

        public override bool Slope(int i, int j) => false;
    }

    public class WulfrumConduitItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;
        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Conduit");
            Item.ResearchUnlockCount = 10;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<WulfrumConduit>());
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