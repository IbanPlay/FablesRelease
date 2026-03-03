using CalamityFables.Content.Items.CrabulonDrops;
using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.Localization;
using static CalamityFables.Content.Tiles.TileDirections;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class DullPlating : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + "DullPlating";

        internal static int registeredTypes = 0;
        internal static int[] tileTypes = new int[3];

        public override void SetStaticDefaults()
        {
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;
            //Main.tileMergeDirt[Type] = true;
            Main.tileBrick[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            tileTypes[registeredTypes++] = Type;
            FablesPlayer.TileSpecificPoundOverrideEvent.Add(Type, DullPlatingItem.PoundRandomize);

            AddMapEntry(new Color(49, 51, 39));
        }

        public override bool CanReplace(int i, int j, int tileTypeBeingPlaced)
        {
            return !tileTypes.Contains(tileTypeBeingPlaced);
        }

    }

    public class DullPlatingPlated : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + "DullPlating";

        public static int EmptyTileType;

        public virtual Color MapColor => new Color(79, 84, 64);

        public override void SetStaticDefaults()
        {
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;
            //Main.tileMergeDirt[Type] = true;
            Main.tileBrick[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            DullPlating.tileTypes[DullPlating.registeredTypes++] = Type;
            FablesPlayer.TileSpecificPoundOverrideEvent.Add(Type, DullPlatingItem.PoundRandomize);

            //Have to do this so that it drops, since the item normally only places the plateless version
            RegisterItemDrop(ModContent.ItemType<DullPlatingItem>());
            AddMapEntry(MapColor);
        }

        public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
        {
            Tile t = Main.tile[i, j];
            t.TileFrameX += 234;

            Tile tLeft = Main.tile[i - 1, j];
            Tile tRight = Main.tile[i + 1, j];
            left = -1;
            if (!tLeft.HasTile || !Main.tileBrick[tLeft.TileType] || tLeft.TileType == Type)
                left = 1;
            right = -1;
            if (!tRight.HasTile || !Main.tileBrick[tRight.TileType] || tRight.TileType == Type)
                right = 1;

            if (right == -1)
                t.TileFrameX += 234;
            if (left == -1)
                t.TileFrameX += 468;
        }

        public override bool CanReplace(int i, int j, int tileTypeBeingPlaced)
        {
            return !DullPlating.tileTypes.Contains(tileTypeBeingPlaced);
        }
    }

    public class DullPlatingPlatedAccent : DullPlatingPlated
    {
        public override Color MapColor => new Color(104, 109, 84);
    }

    public class DullPlatingItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void Load()
        {
            FablesPlayer.OverridePlacedTileEvent += DecideVariant;
        }

        public static int TypePlateless;
        public static int TypePlated;
        public static int TypeAccent;

        private void DecideVariant(Player player, Tile targetTile, Item item, ref int tileToPlace, ref int previewPlaceStyle, ref bool? overrideCanPlace)
        {
            if (tileToPlace != Item.createTile)
                return;

            int leftType = -1;
            int leftPlateLenght = 0;
            int rightType = -1;
            int rightPlateLenght = 0;

            //Check both to the left and right for plates
            int checkX = Player.tileTargetX - 1;
            while (checkX > 0 && leftPlateLenght < 6)
            {
                Tile t = Main.tile[checkX, Player.tileTargetY];
                if (t.HasTile && DullPlating.tileTypes.Contains(t.TileType))
                {
                    //If we reached another tile type, thats the end of our current plate
                    if (leftType != -1 && t.TileType != leftType)
                        break;

                    leftType = t.TileType;
                    leftPlateLenght++;
                    checkX--;
                }
                else
                    break;
            }

            checkX = Player.tileTargetX + 1;
            while (checkX < Main.maxTilesX - 1 && rightPlateLenght < 6)
            {
                Tile t = Main.tile[checkX, Player.tileTargetY];
                if (t.HasTile && DullPlating.tileTypes.Contains(t.TileType))
                {
                    //If we reached another tile type, thats the end of our current plate
                    if (rightType != -1 && t.TileType != rightType)
                        break;

                    rightType = t.TileType;
                    rightPlateLenght++;
                    checkX++;
                }
                else
                    break;
            }

            //Placing dull plating without adjacent ones
            if (rightType == -1 && leftType == -1)
            {
                //1 in 2 chance to have a plate, and from there a further 1 / 2 chance to be the accent plate or not
                if (Main.rand.NextBool())
                    tileToPlace = Main.rand.NextBool() ? ModContent.TileType<DullPlatingPlated>() : ModContent.TileType<DullPlatingPlatedAccent>();
            }

            //Middle of a plate, we just fill it out
            else if (rightType == leftType)
            {
                tileToPlace = rightType;
            }

            //Sandwitched between two different plate types, we pick one of the two at random to complete, unless one of the two is already long enough
            else if (rightType != -1 && leftType != -1 && rightType != leftType)
            {
                if (rightPlateLenght >= 6 && leftPlateLenght < 6)
                    tileToPlace = leftType;
                else if (rightPlateLenght >= 6 && rightPlateLenght < 6)
                    tileToPlace = rightType;
                else
                    tileToPlace = Main.rand.NextBool() ? leftType : rightType;
            }

            //Remaining scenario is that one of the two
            else if (rightType != 1 || leftType != -1)
            {
                int adjPlateType = leftType;
                int adjPlateLenght = leftPlateLenght;
                if (adjPlateType == -1)
                {
                    adjPlateType = rightType;
                    adjPlateLenght = rightPlateLenght;
                }

                int randomNewType;
                // Equal chance between the plated variants
                if (adjPlateType == TypePlateless)
                    randomNewType = Main.rand.NextBool() ? TypePlated : TypeAccent;
                // More likely to go back to unplated instead of the accent, otherwise it would lead to a 2/3rds majority of plated tiles
                else if (adjPlateType == TypePlated)
                    randomNewType = Main.rand.NextBool(4) ? TypeAccent : TypePlateless;
                else
                    randomNewType = Main.rand.NextBool(4) ? TypePlated : TypePlateless;

                //We don't want 1 lenght plates at all!
                if (adjPlateLenght == 1)
                    tileToPlace = adjPlateType;
                //Never get a tile thats longer than 6 long
                else if (adjPlateLenght >= 6)
                    tileToPlace = randomNewType;
                //6 long plates are rare. 1 / 8 chance
                else if (adjPlateLenght >= 5)
                    tileToPlace = Main.rand.NextBool(8) ? adjPlateType : randomNewType;
                //5 long plates are rare-ish. 1 / 4 chance
                else if (adjPlateLenght == 4)
                    tileToPlace = Main.rand.NextBool(4) ? adjPlateType : randomNewType;
                //2 long plates arent the most common, so 2/3 chance to turn it into a 3 long 
                else if (adjPlateLenght == 2)
                    tileToPlace = Main.rand.NextBool(3) ? randomNewType : adjPlateType;
                //3 and 4 long are ideal, so those can switch out at pure random
                else
                    tileToPlace = Main.rand.NextBool() ? adjPlateType : randomNewType;
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Dull Plating");
            Tooltip.SetDefault("Hammer the tile while holding down RMB to change the plate variant\n" +
                "'Not to be confused with Wulfrum Hull Plating!'");
            Item.ResearchUnlockCount = 100;
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<WulfrumHullItem>();
            ItemID.Sets.ShimmerTransformToItem[ModContent.ItemType<WulfrumHullItem>()] = Type;

            TypePlateless = ModContent.TileType<DullPlating>();
            TypePlated = ModContent.TileType<DullPlatingPlated>();
            TypeAccent = ModContent.TileType<DullPlatingPlatedAccent>();
        }

        public override bool? UseItem(Player player) => base.UseItem(player);

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<DullPlating>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(20).
                AddIngredient<WulfrumMetalScrap>().
                AddRecipeGroup(RecipeGroupID.IronBar).
                AddTile<BunkerWorkshop>().
                Register();
            CreateRecipe(1).
                AddIngredient<DullPlatingWallItem>(4).
                AddTile(TileID.WorkBenches).
                Register();
        }

        public static bool PoundRandomize(Player player, Item item, int tileHitType, ref bool hitWall, int x, int y)
        {
            if (item.hammer > 0 && player.toolTime == 0 && player.poundRelease && Main.mouseRight)
            {
                hitWall = false;
                player.ApplyItemTime(item);
                player.poundRelease = false;
                SoundEngine.PlaySound(SoundID.Tink, new Vector2(x, y) * 16);
                WorldGen.KillTile(x, y, fail: true, effectOnly: true);

                int newType;
                if (tileHitType == TypePlateless)
                    newType = TypePlated;
                else if (tileHitType == TypePlated)
                    newType = TypeAccent;
                else
                    newType = TypePlateless;

                Tile t = Main.tile[x, y];
                t.TileType = (ushort)newType;

                WorldGen.SquareTileFrame(x, y);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendTileSquare(-1, x, y, 1, 1);

                return true;
            }
            return false;
        }
    }

    public class DullPlatingWall : ModWall
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DustType = DustID.Iron;
            Main.wallHouse[Type] = true;
            AddMapEntry(new Color(90, 56, 50));
        }

        public override bool WallFrame(int i, int j, bool randomizeFrame, ref int style, ref int frameNumber)
        {
            frameNumber = WorldGen.genRand.Next(2) + (j % 2 == 0 ? 2 : 0);
            return true;
        }
    }


    public class DullPlatingWallUnsafe : DullPlatingWall
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + "DullPlatingWall";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.wallHouse[Type] = false;
        }
    }

    public class DullPlatingWallItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dull Plating Wall");
            Item.ResearchUnlockCount = 400;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableWall(ModContent.WallType<DullPlatingWall>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(4).
                AddIngredient<DullPlatingItem>().
                AddTile(TileID.WorkBenches).
                Register();
        }
    }
}