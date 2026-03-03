using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class RustyWulfrumElevatorRailItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Derelict Wulfrum Elevator Rail");
            Item.ResearchUnlockCount = 1;

            ItemID.Sets.ShimmerTransformToItem[Type] = ItemType<WulfrumElevatorRailItem>();
            ItemID.Sets.ShimmerTransformToItem[ItemType<WulfrumElevatorRailItem>()] = Type;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<RustyWulfrumElevatorRail>());
            Item.maxStack = 9999;
            Item.value = 0;
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe(4).
                AddIngredient<DullPlatingItem>().
                AddTile<BunkerWorkshop>().
                Register();
        }
    }

    public class RustyWulfrumElevatorRail : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public static int ElevatorBaseType;
        public static int ElevatorStationType;

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;

            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = false;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.Origin = new Point16(1, 0);
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16 };
            TileObjectData.newTile.AnchorAlternateTiles = new int[] { Type, ElevatorStationType }; //Can attach to self

            //Attach from top of a block
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide | AnchorType.Table | AnchorType.AlternateTile, 1, 1);
            TileObjectData.newAlternate.AnchorWall = false;
            TileObjectData.addAlternate(0);
            //Attach from bottom of a block
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidBottom | AnchorType.SolidSide | AnchorType.AlternateTile, 1, 1);
            TileObjectData.newAlternate.AnchorWall = false;
            TileObjectData.addAlternate(0);

            TileObjectData.addTile(Type);

            AddMapEntry(CommonColors.WulfrumMetalDark);
            RustyWulfrumElevatorController._elevatorRailType = Type;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustType, 0f, 0f, 1, new Color(255, 255, 255), 1f);
            return false;
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            i -= Main.tile[i, j].TileFrameX / 18 - 1;

            Tile tile = Main.tile[i, j];
            Tile tileAbove = Framing.GetTileSafely(i, j - 1);
            Tile tileBelow = Framing.GetTileSafely(i, j + 1);

            if (tileAbove.HasTile)
            {
                if ((tileAbove.TileType == Type && tileAbove.TileFrameX == tile.TileFrameX) ||
                    (tileAbove.TileType == ElevatorBaseType && tileAbove.TileFrameX - 18 == tile.TileFrameX) ||
                    (tileAbove.TileType == ElevatorStationType && tileAbove.TileFrameX - 18 == tile.TileFrameX))
                    frameYOffset += 36;
                //Connect to any solid block in general
                else if (Main.tileSolid[tileAbove.TileType] && tileAbove.Slope == SlopeType.Solid && !tileAbove.IsHalfBlock)
                    frameYOffset += 36;
            }
            if (tileBelow.HasTile)
            {
                if (tileBelow.TileType == Type && tileBelow.TileFrameX == tile.TileFrameX ||
                    (tileBelow.TileType == ElevatorBaseType && tileBelow.TileFrameX - 18 == tile.TileFrameX) ||
                    (tileBelow.TileType == ElevatorStationType && tileBelow.TileFrameX - 18 == tile.TileFrameX))
                    frameYOffset += 18;
                else if (Main.tileSolid[tileBelow.TileType] && tileBelow.Slope == SlopeType.Solid && !tileBelow.IsHalfBlock)
                    frameYOffset +=18;
            }
        }
    }
}
