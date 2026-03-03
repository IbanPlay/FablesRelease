using CalamityFables.Content.Boss.MushroomCrabBoss;
using Terraria.GameContent.Biomes.CaveHouse;
using Terraria.WorldBuilding;
using Terraria.DataStructures;
using CalamityFables.Content.NPCs.Wulfrum;
using CalamityFables.Content.NPCs.Cursed;
using log4net.Repository.Hierarchy;
using CalamityFables.Content.Tiles.BurntDesert;

namespace CalamityFables.Core
{
    public partial class FablesWorld : ModSystem
    {
        public static bool PlacedPeculiarPot = false;
        public Point PotPosition;
        /// <summary>
        /// 1 if we enter from the right, -1 if we enter from the left
        /// </summary>
        public int chamberEntranceDirection;
        public int chamberCenterX;

        public static readonly List<Point> PyramidPositions = new List<Point>();

        private bool PeculiarPotGeneration(On_WorldGen.orig_Pyramid orig, int i, int j)
        {
            if (!orig(i, j))
                return false;

            //Scan the entire area
            Point[] potPositions = new Point[20];
            int potIndex = 0;

            for (int y = j; y < j + 75; y++)
            {
                for (int x = i - 70; x < i + 70; x++)
                {
                    //Check for pyramid pots
                    Tile currentTile = Main.tile[x, y];

                    if (currentTile.HasTile && currentTile.TileType == TileID.Pots &&
                        currentTile.TileFrameY / 36 >= 25 && currentTile.TileFrameY / 36 < 28 && //Pyramid pot style
                        currentTile.TileFrameX % 36 == 0 && currentTile.TileFrameY % 36 == 0) //Top left corner of the tile
                    {
                        potPositions[potIndex++] = new Point(x, y);
                    }

                    //Only store the first 20 pots (should be enough but just in case)
                    if (potIndex == 20)
                        break;
                }

                //If we found a pot on this Y level, we can return early (all the pots are on the same layer)
                if (potIndex > 0)
                    break;
            }

            //No pots??? How
            if (potIndex == 0)
                return true;

            //Save the first of the pot positions
            PotPosition = potPositions[0];
            //Yass up the pyramid
            PrettifyPyramid(i, j);

            int replacedPotIndex ;
            switch (potIndex)
            {
                //If theres just one pot, we pick it
                case 1:
                    replacedPotIndex = 0;
                    break;
                //If there's two pots, we pick the closest one
                case 2:
                    replacedPotIndex = chamberEntranceDirection == -1 ? 0 : 1;
                    break;
                //If there's more than 2 pots
                default:
                    //Find the pot closest to the entrance, and then find if theres any other ones after it
                    int closestPot = chamberEntranceDirection == -1 ? 0 : potIndex - 1;
                    int testIndex = closestPot;

                    int potsFound = 1;

                    //Find the pot that's closest to the center by going pot per pot
                    while (testIndex - chamberEntranceDirection < potIndex  && testIndex - chamberEntranceDirection >= 0 && (potPositions[testIndex - chamberEntranceDirection].X - chamberCenterX) * chamberEntranceDirection > 0)
                    {
                        testIndex -= chamberEntranceDirection;
                        potsFound++;
                    }


                    //If there's only one pot after the edgemost one, we use it to avoid having the pot be directly after the entrace
                    if (potsFound == 2)
                        replacedPotIndex = testIndex;

                    //If there's more , we pick randomly between them
                    else if (potsFound > 2)
                    {
                        int leftmostPot = Math.Min(closestPot, testIndex);
                        int rightmostPot = Math.Max(closestPot, testIndex);
                        replacedPotIndex = Main.rand.Next(leftmostPot, rightmostPot + 1);
                    }
                    //If we didnt find any pots before the center, we used the closest pot to the entrance
                    else
                        replacedPotIndex = closestPot;
                    break;
            }

            //Pick one random pot to replace
            Point replacedPot = potPositions[replacedPotIndex];


            //Remove the actual pot
            for (int x = replacedPot.X; x < replacedPot.X + 2; x++)
                for (int y = replacedPot.Y; y < replacedPot.Y + 2; y++)
                {
                    Tile potTile = Main.tile[x, y];
                    potTile.HasTile = false;
                }

            //Spawn the pot TE
            ModContent.GetInstance<PeculiarPotTileEntity>().Place(replacedPot.X, replacedPot.Y);

            return true;
        }

        private void CalculateChamberPosition(out int chamberLeft, out int chamberRight, out int chamberTop, out int chamberBottom)
        {
            //Calculate the size of the chamber
            Point floorPosition = PotPosition + new Point(0, 2);
            chamberBottom = floorPosition.Y;

            //Move towards the left as long as we are on the floor of the chamber (aka sandstone brick for floor, and air above
            while (Main.tile[floorPosition].HasTile && Main.tile[floorPosition].TileType == TileID.SandstoneBrick && !Main.tile[floorPosition - new Point(0, 1)].IsTileSolid())
            {
                floorPosition.X -= 1;
            }

            //If the leftmost point of the room still has floor, that means our crawl stopped from hitting the rising stairs
            //This means we enter the chamber on the left side
            if (Main.tile[floorPosition].HasTile && Main.tile[floorPosition].TileType == TileID.SandstoneBrick)
                chamberEntranceDirection = -1;
            //Otherwise, it means we stopped on the descending stairway, therefore we enter the chamber on the right side
            else
                chamberEntranceDirection = 1;

            floorPosition.X += 1;

            chamberLeft = floorPosition.X;

            //Crawl along the floor of the chamber towards the right to find the other side
            while (Main.tile[floorPosition].HasTile && Main.tile[floorPosition].TileType == TileID.SandstoneBrick && !Main.tile[floorPosition - new Point(0, 1)].IsTileSolid())
            {
                floorPosition.X += 1;
            }

            floorPosition.X -= 1;
            chamberRight = floorPosition.X;

            //Move to the center of the chamber, then move upwards until we hit the ceiling
            floorPosition.X = (chamberLeft + chamberRight) / 2;
            floorPosition.Y -= 1;
            while (!Main.tile[floorPosition].IsTileSolid())
                floorPosition.Y -= 1;
            chamberTop = floorPosition.Y;

            chamberCenterX = (chamberLeft + chamberRight) / 2;
        }

        public const int pyramidCeilingShaveHeight = 2;

        //Clears out a few tiles from the top of the top of the ceiling of the pyramid corridors
        public void ShavePyramidCeiling(int x, int y)
        {
            Tile t = Main.tile[x, y];
            if (!t.IsTileSolid())
            {
                //Dont shave flat ceilings, this is to avoid shaving the bottom of the pyramid
                bool leftSideAbove = Main.tile[x - 1, y - 1].IsTileSolid();
                bool rightSideAbove = Main.tile[x + 1, y - 1].IsTileSolid();
                if (leftSideAbove && rightSideAbove)
                    return;

                //Repeat for however many tiles we wanna shave off the ceiling
                for (int shaveY = y - 1; shaveY > y - 1 - pyramidCeilingShaveHeight; shaveY--)
                {
                    Tile tileAbove = Main.tile[x, shaveY];
                    //Shave off the tile above if its sandstone
                    if (tileAbove.HasTile && tileAbove.TileType == TileID.SandstoneBrick)
                    {
                        tileAbove.HasTile = false;
                        tileAbove.WallType = WallID.SandstoneBrick;
                    }
                    else
                        break;
                }
            }
        }

        //Adds slopes to the ceiling of the pyramid corridors to make it smooth
        public void SlopePyramidCeiling(int x, int y)
        {
            Tile t = Main.tile[x, y];
            if (!t.IsTileSolid())
            {
                Tile ceilingTile = Main.tile[x, y - 1];

                //Slope the ceiling tile
                if (ceilingTile.HasTile && ceilingTile.TileType == TileID.SandstoneBrick)
                {
                    //Check for neightbrs to determine the direction of the slope
                    bool ceilingLeft = Main.tile[x - 1, y - 1].IsTileSolid();
                    bool ceilingRight = Main.tile[x + 1, y - 1].IsTileSolid();

                    if (ceilingLeft && !ceilingRight)
                        ceilingTile.Slope = SlopeType.SlopeUpLeft;
                    else if (!ceilingLeft && ceilingRight)
                        ceilingTile.Slope = SlopeType.SlopeUpRight;

                    //Set the wall to avoid issues when theres sandstone walls next to it
                    ceilingTile.WallType = WallID.SandstoneBrick;
                }
            }
        }



        //Add painted gold floor for the pyramid
        public const ushort pyramidFloorTileType = TileID.GoldBrick;
        public const byte pyramidFloorPaint = PaintID.BrownPaint;

        public void GemifyPyramidFloor(int x, int y)
        {
            //Convert tiles under empty air into copper flooring
            Tile t = Main.tile[x, y];
            if (!t.IsTileSolid() && t.WallType == WallID.SandstoneBrick)
            {
                Tile bottomTile = Main.tile[x, y + 1];
                if (bottomTile.HasTile && bottomTile.TileType == TileID.SandstoneBrick)
                {
                    Tile bottomerTile = Main.tile[x, y + 2];

                    //Clip out any 1 tile ledges
                    if (!bottomerTile.HasTile)
                    {
                        bottomTile.HasTile = false;
                        return;
                    }

                    bottomTile.TileType = pyramidFloorTileType;
                    bottomTile.TileColor = pyramidFloorPaint;

                    /*
                    //Smoothen
                    bool floorLeft = Main.tile[x - 1, y + 1].IsTileSolid();
                    bool floorRight = Main.tile[x + 1, y + 1].IsTileSolid();

                    if (floorLeft && !floorRight)
                        bottomTile.Slope = SlopeType.SlopeDownLeft;
                    else if (!floorLeft && floorRight)
                        bottomTile.Slope = SlopeType.SlopeDownRight;
                    */


                    //2 deep for stair parts
                    if (!Main.tile[x + 1, y + 1].HasTile || !(Main.tile[x + 1, y + 1].TileType == TileID.SandstoneBrick && Main.tile[x + 1, y + 1].TileType == pyramidFloorTileType) ||
                        !Main.tile[x - 1, y + 1].HasTile || !(Main.tile[x - 1, y + 1].TileType == TileID.SandstoneBrick && Main.tile[x - 1, y + 1].TileType == pyramidFloorTileType))
                    {
                        bottomerTile.TileType = pyramidFloorTileType;
                        bottomerTile.TileColor = pyramidFloorPaint;
                    }
                }
            }
        }


        private void PrettifyPyramid(int i, int j)
        {
            int pyramidTop = j - 6; //When generating pyramids, the top can be offset by up to 6 tiles vertically
            //Loop down until we find the actual top of the pyramid
            while (Main.tile[i, pyramidTop].TileType != TileID.SandstoneBrick)
                pyramidTop++;

            PyramidPositions.Add(new Point(i, pyramidTop));

            CalculateChamberPosition(out int chamberLeft, out int chamberRight, out int chamberTop, out int chamberBottom);

            //Place two torches in the chamber
            int torchHeight = (chamberTop + chamberBottom) / 2;
            int torchX = chamberLeft + (chamberRight - chamberLeft) / 3;
            for (int t = 0; t < 2; t++)
            {
                WorldGen.PlaceTile(torchX, torchHeight, ModContent.TileType<DesertBrazierTile>(), true, false, -1, 0);
                torchX += (chamberRight - chamberLeft) / 3 + 1;
            }

            //Crop out the edge of the chamber so ti can be affected
            if (chamberEntranceDirection == -1)
                chamberRight -= 1;
            else
                chamberLeft += 1;


            //Shave ceiling
            PyramidCrawl(i, j, ShavePyramidCeiling, 0, true, chamberLeft, chamberRight, chamberTop, chamberBottom);
            //Slope ceiling
            PyramidCrawl(i, j, SlopePyramidCeiling, 0, true, chamberLeft, chamberRight, chamberTop, chamberBottom, falloffAtBottom: true);

            //Turn floor into bronze
            PyramidCrawl(i, pyramidTop + 8, GemifyPyramidFloor, falloffAtBottom:true);
        }


        //Go through all pyramids and remove slopes from the gold bricks we used
        private void CleanupPyramidDeco()
        {
            foreach (Point pyramidCap in PyramidPositions)
            {
                PyramidCrawl(pyramidCap.X, pyramidCap.Y, UnslopeGoldDeco, 20);

                //Electrum (gold) cap
                int width = 1;
                for (int y = pyramidCap.Y - 1; y < pyramidCap.Y + 7; y++)
                {
                    for (int x = pyramidCap.X - width; x < pyramidCap.X + width + 1; x++)
                    {
                        Tile topTile = Main.tile[x, y];
                        if (topTile.TileType == TileID.SandstoneBrick && topTile.HasTile)
                            topTile.TileType = TileID.GoldBrick;
                    }
                    width++;
                }
            }
        }

        public void UnslopeGoldDeco(int x, int y)
        {
            if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.GoldBrick)
            {
                Tile t = Main.tile[x, y];
                t.Slope = SlopeType.Solid;
                t.IsHalfBlock = false;
            }
        }

        public delegate void PyramidCrawlAction(int i, int j);

        public static int PyramidTopPosition;
        public static int PyramidBottomPosition;
        public void PyramidCrawl(int i, int j, PyramidCrawlAction action, int extraHeight = 0, bool ignoreChamber = false, int chamberLeft = 0, int chamberRight = 0, int chamberTop = 0, int chamberBottom = 0, bool falloffAtBottom = false)
        {
            PyramidTopPosition = j - 6; //When generating pyramids, the top can be offset by up to 6 tiles vertically
            PyramidBottomPosition = j + 124 + extraHeight; //Max 124 offset to the bottom
            int scanWidth = 1;


            for (int y = PyramidTopPosition; y < PyramidBottomPosition; y++)
            {
                for (int x = i - scanWidth; x < i + scanWidth + 1; x++)
                {
                    //Ignore tiles within the chamber
                    if (ignoreChamber && x >= chamberLeft && x <= chamberRight && y < chamberBottom && y > chamberTop)
                        continue;

                    if (falloffAtBottom && EffectFadeOut(y))
                        continue;

                    action(x, y);
                }

                scanWidth++;
            }

            //Loop through the tunnel at the bottom
            for (int y = PyramidBottomPosition; y < PyramidBottomPosition + 180; y++)
            {
                for (int x = i - scanWidth; x < i + scanWidth + 1; x++)
                    action(x, y);
            }
        }

        public bool EffectFadeOut(int y)
        {
            if (y < PyramidBottomPosition + 170)
                return false;

            //The deeper the more the fade works
            if (Main.rand.NextFloat() > Utils.GetLerpValue(PyramidBottomPosition + 170, PyramidBottomPosition + 180, y, true))
                return true;

            return false;
        }
    }


    public class PeculiarPotTileEntity : ModTileEntity
    {
        public Vector2 WorldPosition => Position.ToVector2() * 16;
        public static int PeculiarPotTileType;
        public Player closestPlayer;
        public NPC spawnedPotlet;

        #region Breaking behavior
        public override void Update()
        {
            //Dont spawn another potlet if theres already one spawned
            if (spawnedPotlet != null)
            {
                if (!spawnedPotlet.active)
                    spawnedPotlet = null;
                else
                    return;
            }
            CheckForNearbyPlayers();
        }

        public override bool IsTileValidForEntity(int x, int y) => true;

        public void CheckForNearbyPlayers()
        {
            closestPlayer = null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.Distance(WorldPosition) < 1400)
                {
                    closestPlayer = player;
                    break;
                }
            }

            if (closestPlayer == null)
                return;

            SpawnPotlet();
        }

        public void SpawnPotlet()
        {
            spawnedPotlet = NPC.NewNPCDirect(new EntitySource_TileEntity(this), (int)WorldPosition.X + 16, (int)WorldPosition.Y, ModContent.NPCType<Potlet>(), ai2: Position.X, ai3: Position.Y);
        }
        #endregion
    }

}