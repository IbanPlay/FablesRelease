using CalamityFables.Content.Boss.SeaKnightMiniboss;
using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.NPCs.Wulfrum;
using CalamityFables.Content.Tiles.BurntDesert;
using CalamityFables.Content.Tiles.Misc;
using CalamityFables.Content.Tiles.Wulfrum;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using CalamityFables.Content.Tiles.WulfrumScrapyard;
using CalamityFables.Noise;
using Microsoft.VisualBasic;
using Terraria.DataStructures;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace CalamityFables.Core
{
    public class WulfrumScrapyard : ILoadable
    {
        public void Load(Mod mod)
        {
            FablesDrawLayers.DrawThingsBehindWallsEvent += DrawBunkerBackground;
            FablesWall.KillWallEvent += PreventBunkerWallBreak;
            FablesWall.CanExplodeEvent += PreventBunkerWallInteraction;
            FablesWall.CanPlaceEvent += PreventBunkerWallInteraction;
            FablesWorld.PostWorldgenEvent += FinalCleanup;
        }

        private void FinalCleanup()
        {
            //Put the water back because it got washed away fsr
            foreach ((Point, byte) puddleInfo in puddlePositions)
            {
                Main.tile[puddleInfo.Item1].LiquidAmount = puddleInfo.Item2;
            }

            //Try to make sure each wire and chain is even placeable and if not remove em
            foreach (var item in WireSpoolItem.wireManager.PlaceablesByPosition)
            {
                //...Failsafe..?
                item.Value.Anchor = item.Key;
                if (!item.Value.Update())
                    WireSpoolItem.wireManager.RemovePlaceable(item.Value);
            }

            foreach (var item in RustyWulfrumChainsItem.chainManager.PlaceablesByPosition)
            {
                //...Failsafe..?
                item.Value.Anchor = item.Key;
                if (!item.Value.Update())
                    RustyWulfrumChainsItem.chainManager.RemovePlaceable(item.Value);
            }
        }

        public void Unload() { }

        private void PreventBunkerWallBreak(int i, int j, int type, ref bool fail)
        {
            if (PointOfInterestMarkerSystem.WulfrumBunkerWallProtectionRectangle.Contains(i, j))
                fail = true;
        }
        private bool PreventBunkerWallInteraction(int i, int j, int type) => !PointOfInterestMarkerSystem.WulfrumBunkerWallProtectionRectangle.Contains(i, j);

        public static FastNoise genNoise => FablesWorld.genNoise;

        public static ushort landfillType;
        public static ushort landfillSmallRotorType;
        public static ushort landfillBigRotorType;

        public static ushort dullPlatingType;
        public static ushort dustyBrickType;
        public static ushort rustySheetType;

        public static ushort dustyWallType;
        public static ushort dullWallType;
        public static ushort sheetWallType;

        public static readonly List<(Point, byte)> puddlePositions = new();

        public static void TryGenerate()
        {
            StructureMap test = GenVars.structures;
            puddlePositions.Clear();

            int bunkerWidthClearance = BunkerSize.X + 6;
            List<Point> worldTopography = new List<Point>(Main.maxTilesX - bunkerWidthClearance * 2);
            int[] validScrapyardTiles = new int[] { TileID.Stone, TileID.Grass, TileID.Copper, TileID.Tin, TileID.SnowBlock, TileID.IceBlock };
            int[] validScrapyardFarTiles = new int[] { TileID.JungleGrass, TileID.Sand };

            bool tileFound = false;
            float innerCrop = 0.33f;
            float farCrop = 0.18f;

            int validTiles = 0;
            int highestContinuousValidTileSpan = 0;
            int currentContinuousValidTileSpan = 0;

            //Map the world
            for (int x = 0 + WorldGen.beachDistance + 30; x < Main.maxTilesX - WorldGen.beachDistance + 30; x++)
            {
                tileFound = false;

                //don't take tiles in the center of the world
                if (x > Main.maxTilesX * innerCrop && x < Main.maxTilesX * (1 - innerCrop))
                {
                    highestContinuousValidTileSpan = Math.Max(highestContinuousValidTileSpan, currentContinuousValidTileSpan);
                    currentContinuousValidTileSpan = 0;

                    worldTopography.Add(new Point(0, 0));
                    continue;
                }

                for (int y = (int)(Main.worldSurface * 0.5f); y <= Main.worldSurface; y++)
                {
                    Tile t = Main.tile[x, y];

                    //Don't place it in lakes
                    if (t.LiquidAmount > 0 && Main.tile[x, y + 1].LiquidAmount > 0)
                        break;

                    if (t.HasTile && Main.tileSolid[t.TileType])
                    {
                        if (validScrapyardTiles.Contains(t.TileType) || (x <= Main.maxTilesX * farCrop || x > Main.maxTilesX * (1 - innerCrop)) && validScrapyardFarTiles.Contains(t.TileType))
                        {
                            currentContinuousValidTileSpan++;
                            validTiles++;
                            worldTopography.Add(new Point(x, y));
                        }
                        else
                        {
                            highestContinuousValidTileSpan = Math.Max(highestContinuousValidTileSpan, currentContinuousValidTileSpan);
                            currentContinuousValidTileSpan = 0;
                            worldTopography.Add(new Point(0, 0));
                        }

                        tileFound = true;
                        break;
                    }
                }

                if (!tileFound)
                {
                    highestContinuousValidTileSpan = Math.Max(highestContinuousValidTileSpan, currentContinuousValidTileSpan);
                    currentContinuousValidTileSpan = 0;
                    worldTopography.Add(new Point(0, 0));
                }
            }

            int tileCropTimer = 0;
            //Get a good margin
            int cropSize = highestContinuousValidTileSpan / 5;
            List<Point> tilesToShaveOff = new List<Point>(Main.maxTilesX - bunkerWidthClearance * 2);

            //"Erode" valid tiles by removing those that are near invalid tiles
            for (int i = 0; i < worldTopography.Count; i++)
            {
                //If the tile is "invalid" aka no tile at the surface or no tile of the right kind
                if (worldTopography[i] == Point.Zero)
                {
                    //If the crop timer is zero (aka the tile at its left wasnt cropped out or empty already)
                    if (tileCropTimer == 0)
                    {
                        //Move backwards and mark all the tiles before us to be deleted
                        for (int j = Math.Max(0, i - cropSize); j < i; j++)
                            tilesToShaveOff.Add(worldTopography[j]);
                    }

                    //Reset the crop timer so that when we meet valid ground again, some of it will still get shaved
                    tileCropTimer = cropSize;
                    tilesToShaveOff.Add(worldTopography[i]);
                    continue;
                }

                //Else if its a solid tile, if the crop "timer" is above zero, shave it off for being too close to another empty tile
                else if (tileCropTimer > 0)
                {
                    tilesToShaveOff.Add(worldTopography[i]);
                    tileCropTimer--;
                    continue;
                }
            }
            //Crop out all the parts i don't like
            worldTopography.RemoveAll(p => tilesToShaveOff.Contains(p));


            int tries = 0;
            while (tries < 1000)
            {
                tries++;
                //Rip
                if (worldTopography.Count <= 0)
                    return;

                Point randomPoint = WorldGen.genRand.Next(worldTopography);
                //High standards that erode after a while
                float standards = Utils.GetLerpValue(800f, 150f, tries, true);

                if (CheckXCoordinate(randomPoint.X, out int surfaceheight, out int bunkerHeight, standards))
                {
                    PlaceScrapyard(new Point(randomPoint.X, surfaceheight), bunkerHeight);
                    PlaceBunker(new Point(randomPoint.X, bunkerHeight), surfaceheight);
                    return;
                }


                //PlaceScrapyard(new Point16(randomPoint.X, randomPoint.Y));
            }
        }

        /// <summary>
        /// Check if this X coordinate is fine to have a wulfrum bunker, by looking at the floor slope and whats below
        /// </summary>
        /// <param name="x"></param>
        /// <param name="standards"></param>
        /// <returns></returns>
        public static bool CheckXCoordinate(int x, out int surfaceHeight, out int bunkerHeight, float standards = 1f)
        {
            surfaceHeight = -1;
            bunkerHeight = -1;

            for (int y = (int)(Main.worldSurface * 0.5f); y <= Main.worldSurface; y++)
            {
                if (Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType])
                {
                    surfaceHeight = y;
                    break;
                }
            }

            //Somehow???
            if (surfaceHeight == -1)
                return false;

            //Too much slope
            float nearbyCurve = FablesUtils.AverageTerrainSlope(FablesUtils.GetSurroundingTopography(new Point(x, surfaceHeight), 19, 15, 70, true));

            //At max standards we try to find super flat ground. As it goes we relax the conditions
            if (nearbyCurve >= 3f - 2.5f * standards)
                return false;

            // Check for fairy trunks if we have high standards
            if (standards > 0.5f)
            {
                for (int i = x - 10; i <= x + 10; i++)
                {
                    for (int j = surfaceHeight - 5; j < surfaceHeight + 5; j++)
                    {
                        Tile t = Main.tile[i, j];
                        if (t.HasTile && t.TileType == TileID.FallenLog)
                            return false;
                    }
                }
            }

            //Try for bunker 
            bunkerHeight = surfaceHeight + 40;

            //More and more attempts the more desperate we get
            int attempts = 1;
            if (standards < 0.8f)
                attempts = 2;
            if (standards < 0.5f)
                attempts = 3;
            if (standards < 0.3f)
                attempts = 4;

            for (int i = 0; i < attempts; i++)
            {
                if (EnoughSpaceForBunker(new Point(x, bunkerHeight)))
                    return true;
                bunkerHeight += 8;
            }
            return false;
        }

        /// <summary>
        /// Checks a big area to make sure its buried underground and not overlapping any important tiles
        /// </summary>
        private static bool EnoughSpaceForBunker(Point center)
        {
            int allowedEmptyWallCount = 30;
            //Always generate on a odd height, tis is for the ideal look with the dull plating walls
            if (center.Y % 2 == 0)
                center.Y++;

            int extraBannedType = -1;
            if (CalamityFables.SpiritEnabled && ModContent.TryFind("SpiritReforged/ButterflyStump", out ModTile butterflyStump))
                extraBannedType = butterflyStump.Type;

            for (int j = center.Y - 10; j <= center.Y + BunkerSize.Y; j++)
            {
                int width = j < center.Y ? 27 : BunkerSize.X / 2; //Antechamber half width
                for (int i = center.X - width; i <= center.X + width; i++)
                {
                    Dust.QuickDust(i, j, Color.Red);
                    Tile t = Main.tile[i, j];

                    //Can place inside solids!
                    if (t.HasTile && Main.tileSolid[t.TileType])
                    {
                        if (Main.tileSolid[t.TileType])
                        {
                            //DONT PLACE IN THE DUNGEON or crimson or corruption
                            if (TileID.Sets.DungeonBiome[t.TileType] > 0 || t.TileType == TileID.Crimstone || t.TileType == TileID.Ebonstone)
                                return false;

                            continue;
                        }

                        //Don't go over chests please
                        if (t.HasTile && (TileID.Sets.BasicChest[t.TileType] || TileID.Sets.IsAContainer[t.TileType]))
                            return false;

                        //Don't go next to ench sword shrines
                        if (t.HasTile && t.TileType == TileID.LargePiles && t.TileFrameX >= 918 && t.TileFrameX < 972)
                            return false;

                        //dont grief spirit butterfly cave
                        if (t.HasTile && t.TileType == extraBannedType)
                            return false;
                    }


                    //If no tile and no background wall, we give up
                    if (j <= Main.worldSurface && (t.WallType <= 0 || Main.wallHouse[t.WallType]))
                    {
                        Dust.QuickDust(i, j, Color.White);
                        allowedEmptyWallCount--;
                        if (allowedEmptyWallCount <= 0)
                            return false;
                    }
                }
            }

            return true;
        }

        #region Scrapyard
        public static void PlaceScrapyard(Point center, int bunkerHeight)
        {
            landfillType = (ushort)ModContent.TileType<WulfrumLandfill>();
            landfillBigRotorType = (ushort)ModContent.TileType<WulfrumBigRotorLandfill>();
            landfillSmallRotorType = (ushort)ModContent.TileType<WulfrumRotorLandfill>();
            dustyBrickType = (ushort)ModContent.TileType<DustyBricks>();
            dustyWallType = (ushort)ModContent.WallType<DustyBrickWallUnsafe>();

            //Place down big brick pillars with lampposts
            List<Point> lampPostPositions = new List<Point>();

            for (int s = -1; s <= 1; s += 2)
            {
                int maxPillars = WorldGen.genRand.NextBool(2) ? 3 : 2;
                int pillarX = 6 * s;
                int lastPillarFloorY = -1;
                int lastPillarMinX = -1;
                int lastPillarMaxX = -1;

                int pillarMaxX;
                int pillarMinX;
                if (s < 0)
                {
                    pillarMaxX = center.X - 4;
                    pillarMinX = pillarMaxX - WorldGen.genRand.Next(4, 7);
                }
                else
                {
                    pillarMinX = center.X + 4;
                    pillarMaxX = pillarMinX + WorldGen.genRand.Next(4, 7);
                }

                for (int p = 0; p < maxPillars; p++)
                {
                    int pillarIdealY = center.Y;
                    if (WorldGen.genRand.NextBool(2))
                        pillarIdealY--;
                    if (WorldGen.genRand.NextBool(4))
                        pillarIdealY--;

                    //Track if we're the last pillar in this side. This is so lichen doesnt grow when inside tiles
                    int pillarEdge = 0;
                    if (p == maxPillars - 1)
                        pillarEdge = s;

                    if (!PlaceBrickPillar(pillarIdealY, out int floorY, bunkerHeight, pillarMinX, pillarMaxX, p, ref lampPostPositions, pillarEdge))
                    {
                        if (p > 0)
                        {
                            //If we had a huge jump before, try a shorter distance jump just in case
                            if (s < 0 && pillarMaxX < lastPillarMinX - 6)
                            {
                                pillarMaxX += 2;
                                pillarMinX += 2;
                                //its over even after the redjustment
                                if (!PlaceBrickPillar(pillarIdealY, out floorY, bunkerHeight, pillarMinX, pillarMaxX, p, ref lampPostPositions, pillarEdge))
                                    break;
                            }
                            else if (s > 0 && pillarMinX > lastPillarMaxX + 6)
                            {
                                pillarMaxX -= 2;
                                pillarMinX -= 2;
                                if (!PlaceBrickPillar(pillarIdealY, out floorY, bunkerHeight, pillarMinX, pillarMaxX, p, ref lampPostPositions, pillarEdge))
                                    break;
                            }
                            else
                                break;
                        }
                        else
                            break;
                    }

                    //Fill spot between pillars
                    if (p > 0)
                    {
                        int inbetweenMinX;
                        int inbetweenMaxX;
                        int fullInbetweenMinX;
                        int fullInbetweenMaxX;

                        if (s < 0)
                        {
                            inbetweenMinX = pillarMaxX + 1;
                            inbetweenMaxX = lastPillarMinX - 1;

                            fullInbetweenMinX = pillarMinX + 1;
                            fullInbetweenMaxX = lastPillarMaxX - 1;
                        }
                        else
                        {
                            inbetweenMinX = lastPillarMaxX + 1;
                            inbetweenMaxX = pillarMinX - 1;

                            fullInbetweenMinX = lastPillarMinX - 1;
                            fullInbetweenMaxX = pillarMaxX - 1;
                        }

                        int lowestPillarFloor = Math.Max(floorY, lastPillarFloorY);
                        FillBetweenPillars(inbetweenMinX, inbetweenMaxX, lowestPillarFloor, 1, fullInbetweenMinX, fullInbetweenMaxX, bunkerHeight);
                    }

                    lastPillarMaxX = pillarMaxX;
                    lastPillarMinX = pillarMinX;
                    lastPillarFloorY = floorY;

                    if (s < 0)
                    {
                        pillarMaxX = pillarMinX - WorldGen.genRand.Next(5, 14);
                        pillarMinX = pillarMaxX - WorldGen.genRand.Next(4, 7);
                    }
                    else
                    {
                        pillarMinX = pillarMaxX + WorldGen.genRand.Next(5, 14);
                        pillarMaxX = pillarMinX + WorldGen.genRand.Next(4, 7);
                    }
                }
            }

            WireLamppostsTogether(lampPostPositions);
            return;
        }

        private static bool PlaceBrickPillar(int targetFloorY, out int floorY, int pillarMaxY, int minX, int maxX, int indexFromCenter, ref List<Point> lamppostPositions, int areaEdge = 0)
        {
            int i = (minX + maxX) / 2;
            floorY = -1;

            for (int j = targetFloorY - 15; j < pillarMaxY - 8; j++)
            {
                Tile t = Main.tile[i, j];
                if (t.HasTile && Main.tileSolid[t.TileType])
                {
                    floorY = j;
                    break;
                }
            }

            if (floorY == -1)
                return false;

            //Rise above the floor if too low
            if (floorY > targetFloorY)
                floorY = Math.Max(targetFloorY + indexFromCenter, floorY - 4);

            for (i = minX; i <= maxX; i++)
            {
                for (int j = floorY; j < pillarMaxY; j++)
                {
                    Tile t = Main.tile[i, j];
                    t.HasTile = true;
                    t.TileType = dustyBrickType;
                    t.Slope = SlopeType.Solid;
                    t.IsHalfBlock = false;
                }

                for (int j = floorY - 1; j >= floorY - 12; j--)
                {
                    Tile t = Main.tile[i, j];
                    t.HasTile = false;
                    t.Slope = SlopeType.Solid;
                    t.IsHalfBlock = false;
                }
            }

            ushort lichenBrickType = (ushort)ModContent.TileType<DustyBricksLichen>();

            //put the lichen in the bag man

            int topLichenStart = 0;
            int toplichenEnd = 0;
            bool lichenOnTop = WorldGen.genRand.NextBool();
            if (lichenOnTop)
            {
                int width = maxX - minX;
                topLichenStart = minX + WorldGen.genRand.Next(-3, width + 1);
                toplichenEnd = topLichenStart + WorldGen.genRand.Next(3, width + 1);
            }

            for (i = minX; i <= maxX; i++)
            {
                if (lichenOnTop && i >= topLichenStart && i <= toplichenEnd)
                {
                    Tile t = Main.tile[i, floorY];
                    t.TileType = lichenBrickType;
                }

                //only do the edges
                if (i != minX && i != maxX)
                    continue;

                float baseLichenProbability = WorldGen.genRand.NextFloat(-0.2f, 0.2f);

                for (int j = floorY + 1; j < pillarMaxY; j++)
                {
                    Tile t = Main.tile[i, j];

                    //Leftmost side, don't put lichen if its gonna be inside a wall, for tileblend reasons
                    if (areaEdge == -1 && i == minX)
                    {
                        Tile leftTile = Main.tile[i - 1, j];
                        if (leftTile.HasTile && Main.tileSolid[leftTile.TileType])
                            continue;
                    }
                    //rightmost side
                    else if (areaEdge == 1 && i == maxX)
                    {
                        Tile rightTile = Main.tile[i + 1, j];
                        if (rightTile.HasTile && Main.tileSolid[rightTile.TileType])
                            continue;
                    }

                    float lichenprobability = baseLichenProbability + 0.7f * (1 - (j - floorY) / (float)(pillarMaxY - floorY));
                    if (WorldGen.genRand.NextFloat() < lichenprobability)
                        t.TileType = lichenBrickType;
                }
            }



            if (maxX - minX >= 2)
            {
                maxX--;
                minX++;
            }
            int lampposX = WorldGen.genRand.Next(minX, maxX);

            TileObject lamppost = new();
            lamppost.xCoord = lampposX;
            lamppost.yCoord = floorY - 8;
            lamppost.type = ModContent.TileType<WulfrumLamppost>();
            lamppost.random = WorldGen.genRand.Next(4);
            TileObject.Place(lamppost);

            lamppostPositions.Add(new Point(lampposX, floorY - 8));

            return true;
        }

        public static void FillBetweenPillars(int minX, int maxX, int startY, int indentDepth, int fullMinX, int fullMaxX, int bunkerHeight)
        {
            for (int i = minX; i <= maxX; i++)
            {
                for (int j = startY + indentDepth; j < bunkerHeight; j++)
                {
                    Tile t = Main.tile[i, j];
                    t.HasTile = true;
                    t.TileType = dustyBrickType;
                    t.Slope = SlopeType.Solid;
                    t.IsHalfBlock = false;
                }

                //Carve insides out
                for (int j = startY + indentDepth - 1; j >= startY + indentDepth - 12; j--)
                {
                    Tile t = Main.tile[i, j];
                    t.HasTile = false;
                    t.Slope = SlopeType.Solid;
                    t.IsHalfBlock = false;
                }
            }

            byte puddleDepth = (byte)(WorldGen.genRand.Next(120, 200));

            //Puddle of water in small indented ones, with a pile of scrap inside
            if (indentDepth == 1)
            {
                for (int i = minX; i <= maxX; i++)
                {
                    Tile t = Main.tile[i, startY];
                    t.LiquidAmount = puddleDepth;
                    puddlePositions.Add((new Point(i, startY), puddleDepth));
                }

                int pileMinX = WorldGen.genRand.Next(minX, maxX);
                int pileMaxX = WorldGen.genRand.Next(minX + 1, maxX + 1);

                if (pileMaxX < pileMinX && WorldGen.genRand.NextBool())
                {
                    int placeholder = pileMinX;
                    pileMinX = pileMaxX;
                    pileMaxX = placeholder;
                }

                PlaceDebrisPilesInSection(pileMinX, pileMaxX, startY);
            }

            if (!WorldGen.genRand.NextBool(4))
            {
                int scrapPatchStartY = startY + WorldGen.genRand.Next(1, 3);
                int scrapyardpatchCenter = (minX + maxX) / 2;
                int patchWidth = WorldGen.genRand.Next((maxX - minX) / 2, (maxX - minX) / 2 + 2);

                PlaceScrapyardWulfrumPatch(new Point(scrapyardpatchCenter, scrapPatchStartY), patchWidth, WorldGen.genRand.Next(4, 7), minX, maxX);

            }

            //put fence
            if (!WorldGen.genRand.NextBool(3))
            {
                int fenceStartX = WorldGen.genRand.Next(fullMinX, minX + 2);
                int fenceEndX = WorldGen.genRand.Next(maxX - 2, fullMaxX);
                PlaceScrapyardFence(new Point(fenceStartX, startY - WorldGen.genRand.Next(5, 8)), fenceEndX - fenceStartX, 9);
            }

            bool hasPipe = WorldGen.genRand.NextBool(2);
            int width = maxX - minX - 1;
            if (!hasPipe || width < 4)
                return;

            //0 = straight up pipe with exhaust, 1 = S pipe, 2 = coiling down pipe
            int pipeFormation = WorldGen.genRand.Next(3);
            if (pipeFormation == 2 && width <= 5)
                pipeFormation = WorldGen.genRand.Next(2);

            int pipeType = ModContent.TileType<WulfrumConduit>();
            int verticalConnector = ModContent.TileType<WulfrumConduitCouplingVertical>();
            int horizontalConnector = ModContent.TileType<WulfrumConduitCouplingHorizontal>();
            int exhaustType = ModContent.TileType<WulfrumConduitExhaustVertical>();

            //Pipe going straight up
            if (pipeFormation == 0)
            {
                int pipeX = WorldGen.genRand.Next(minX + 1, maxX - 1);
                //Dig out a canal for the pipe
                for (int i = pipeX; i < pipeX + 2; i++)
                {
                    for (int j = startY - 2; j <= startY + 16; j++)
                    {
                        Tile t = Main.tile[i, j];
                        t.HasTile = false;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;

                        //put walls since pipes dont block light
                        if (j >= startY)
                            t.WallType = dustyWallType;
                    }
                }

                int pipeY = startY - 1;
                TileObject exhaust = new();
                exhaust.xCoord = pipeX;
                exhaust.yCoord = pipeY;
                exhaust.type = exhaustType;
                TileObject.Place(exhaust);
                pipeY++;

                TileObject pipe = new();
                pipe.xCoord = pipeX;
                pipe.yCoord = pipeY;
                pipe.type = pipeType;
                pipe.random = WorldGen.genRand.Next(3);
                TileObject.Place(pipe);
                pipeY+=2;

                for (int i = 0; i < 9; i++)
                {
                    pipe = new();
                    pipe.xCoord = pipeX;
                    pipe.yCoord = pipeY;
                    pipe.type = i % 3 == 0 ? verticalConnector : pipeType;
                    pipe.random = i % 3 == 0 ? 0 : WorldGen.genRand.Next(3);
                    TileObject.Place(pipe);

                    if (pipe.type == pipeType)
                        pipeY += 2;
                    else
                        pipeY += 1;
                }
            }
            //Pipe in a S
            else if (pipeFormation == 1)
            {
                int pipeX = WorldGen.genRand.Next(minX + 1, maxX - 1);
                int pipeOffset = WorldGen.genRand.Next(2, 5) * (WorldGen.genRand.NextBool() ? -1 : 1);


                //Dig out a canal for the pipe. Gotta do 2 canals
                for (int i = pipeX; i < pipeX + 2; i++)
                {
                    //Surface one
                    for (int j = startY - 4; j <= startY + 3; j++)
                    {
                        Tile t = Main.tile[i, j];
                        t.HasTile = false;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;

                        //put walls since pipes dont block light
                        if (j >= startY)
                            t.WallType = dustyWallType;
                    }

                    //the lil middle canal for the divergence
                    for (int j = startY + 2; j <= startY + 3; j++)
                    {
                        Tile t = Main.tile[i + pipeOffset / 2, j];
                        t.HasTile = false;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                        //put walls since pipes dont block light
                        if (j >= startY)
                            t.WallType = dustyWallType;
                    }

                    //Diverged one
                    for (int j = startY + 2; j <= startY + 16; j++)
                    {
                        Tile t = Main.tile[i + pipeOffset, j];
                        t.HasTile = false;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;

                        //put walls since pipes dont block light
                        if (j >= startY)
                            t.WallType = dustyWallType;
                    }
                }

                //place the top pipe
                int pipeY = startY - 3;
                for (int i = 0; i < 4; i++)
                {
                    TileObject pipe = new();
                    pipe.xCoord = pipeX;
                    pipe.yCoord = pipeY;
                    pipe.type = i % 3 == 2 ? verticalConnector : pipeType;
                    pipe.random = i % 3 == 2 ? 0 : WorldGen.genRand.Next(3);
                    TileObject.Place(pipe);
                    if (pipe.type == pipeType)
                        pipeY += 2;
                    else
                        pipeY += 1;
                }

                //pipe that makes the juncture inbetween
                if (Math.Abs(pipeOffset) > 3)
                {
                    TileObject pipe = new();
                    pipe.xCoord = pipeX + pipeOffset / 2;
                    pipe.yCoord = startY + 2;
                    pipe.type = pipeType;
                    pipe.random = WorldGen.genRand.Next(3);
                    TileObject.Place(pipe);
                }
                else if (Math.Abs(pipeOffset) > 2)
                {
                    TileObject pipe = new();
                    pipe.xCoord = pipeX + (pipeOffset < 0 ? -1 : 2);
                    pipe.yCoord = startY + 2;
                    pipe.type = horizontalConnector;
                    TileObject.Place(pipe);
                }

                //place the bottom pipe
                pipeY = startY + 2;
                for (int i = 0; i < 9; i++)
                {
                    TileObject pipe = new();
                    pipe.xCoord = pipeX + pipeOffset;
                    pipe.yCoord = pipeY;
                    pipe.type = i % 3 == 1 ? verticalConnector : pipeType;
                    pipe.random = i % 3 == 1 ? 0 : WorldGen.genRand.Next(3);
                    TileObject.Place(pipe);
                    if (pipe.type == pipeType)
                        pipeY += 2;
                    else
                        pipeY += 1;
                }
            }
            //Pipe in a downward U
            else
            {
                int pipeX = WorldGen.genRand.Next(minX + 1, maxX - 4);

                //Dig out two canals for the pipes
                for (int i = pipeX; i < pipeX + 2; i++)
                {
                    //left canal
                    for (int j = startY - 2; j <= startY + 17; j++)
                    {
                        Tile t = Main.tile[i, j];
                        t.HasTile = false;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;

                        //put walls since pipes dont block light
                        if (j >= startY)
                            t.WallType = dustyWallType;
                    }

                    //right canal
                    for (int j = startY - 2; j <= startY + 17; j++)
                    {
                        Tile t = Main.tile[i + 3, j];
                        t.HasTile = false;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;

                        //put walls since pipes dont block light
                        if (j >= startY)
                            t.WallType = dustyWallType;
                    }
                }

                //Carve the middle bit out and place the connector that allows for the curve
                Tile midTile = Main.tile[pipeX + 2, startY - 2];
                midTile.HasTile = false;
                midTile.Slope = SlopeType.Solid;
                midTile.IsHalfBlock = false;
                midTile = Main.tile[pipeX + 2, startY - 1];
                midTile.HasTile = false;
                midTile.Slope = SlopeType.Solid;
                midTile.IsHalfBlock = false;
                TileObject connector = new();
                connector.xCoord = pipeX + 2;
                connector.yCoord = startY - 2;
                connector.type = horizontalConnector;
                TileObject.Place(connector);

                //place the two downward pipes
                for (int s = 0; s <= 3; s += 3)
                {
                    TileObject pipe = new();
                    pipe.xCoord = pipeX + s;
                    pipe.yCoord = startY - 2;
                    pipe.type = pipeType;
                    pipe.random = WorldGen.genRand.Next(3);
                    TileObject.Place(pipe);
                    int pipeY = startY;
                    for (int i = 0; i < 11; i++)
                    {
                        pipe = new();
                        pipe.xCoord = pipeX + s;
                        pipe.yCoord = pipeY;
                        pipe.type = i % 3 == 0 ? verticalConnector : pipeType;
                        pipe.random = i % 3 == 0 ? 0 : WorldGen.genRand.Next(3);
                        TileObject.Place(pipe);
                        if (pipe.type == pipeType)
                            pipeY += 2;
                        else
                            pipeY += 1;
                    }
                }
            }
        }

        public static void WireLamppostsTogether(List<Point> lamppostPositions)
        {
            lamppostPositions = lamppostPositions.OrderBy(p => p.X).ToList();

            if (lamppostPositions.Count == 0)
                return;
                
            //Random chance to connect the side lampposts to the ground

            if (WorldGen.genRand.NextBool(3))
            {
                Point connectionPoint = lamppostPositions[0] + new Point(-WorldGen.genRand.Next(4, 10), 0);
                while (!Main.tile[connectionPoint].HasTile || !Main.tileSolid[Main.tile[connectionPoint].TileType])
                    connectionPoint.Y++;

                ScrapyardWire wire = new ScrapyardWire();
                wire.Anchor = lamppostPositions[0] + new Point(0, 3);
                wire.EndPoint = connectionPoint;
                wire.WireSagValue = WorldGen.genRand.Next(1, 5);
                WireSpoolItem.wireManager.TryPlaceNewObject(wire);
            }

            for (int i = 1; i < lamppostPositions.Count; i++)
            {
                if (!WorldGen.genRand.NextBool(2))
                    continue;
                ScrapyardWire wire = new ScrapyardWire();
                wire.Anchor = lamppostPositions[i] + new Point(0, 2);
                wire.EndPoint = lamppostPositions[i - 1] + new Point(0, 2);
                wire.WireSagValue = WorldGen.genRand.Next(1, 5);
                WireSpoolItem.wireManager.TryPlaceNewObject(wire);

                lamppostPositions[i] += new Point(0, 1);
            }

            if (WorldGen.genRand.NextBool(3))
            {
                Point connectionPoint = lamppostPositions[^1] + new Point(WorldGen.genRand.Next(4, 10), 0);
                while (!Main.tile[connectionPoint].HasTile || !Main.tileSolid[Main.tile[connectionPoint].TileType])
                    connectionPoint.Y++;

                ScrapyardWire wire = new ScrapyardWire();
                wire.Anchor = lamppostPositions[^1] + new Point(0, 3);
                wire.EndPoint = connectionPoint;
                wire.WireSagValue = WorldGen.genRand.Next(1, 5);
                WireSpoolItem.wireManager.TryPlaceNewObject(wire);
            }
        }

        #region Tile decoration
        public static void PlaceScrapyardFence(Point origin, int width, int height)
        {
            int[] fenceTypes = new int[] { ModContent.WallType<LatticeFence>(), ModContent.WallType<LatticeFenceTattered>() };

            for (int i = 0; i <= width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    //Edges are lattice, middle is tattered
                    ushort fenceType = (ushort)ModContent.WallType<LatticeFence>();
                    if (i > 0 && i < width && j > 0)
                        fenceType = (ushort)ModContent.WallType<LatticeFenceTattered>();

                    Tile t = Main.tile[origin.X + i, origin.Y + j];
                    if (t.TileType != dustyWallType)
                        t.WallType = fenceType;
                }
            }

            bool punctured = false;
            if (width > 2 && height > 3)
                punctured = WorldGen.genRand.NextBool(2);

            if (!punctured)
                return;

            Point fencePucturePoint = new Point(WorldGen.genRand.Next(0, width + 1), WorldGen.genRand.Next(0, height - 2));
            float fencePuctureRadius = WorldGen.genRand.NextFloat(3f, 5f);

            //Puncture the fences
            for (int i = 0; i <= width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (fenceTypes.Contains(Main.tile[origin.X + i, origin.Y + j].WallType))
                    {
                        float perlinpinpin = Math.Abs(genNoise.GetPerlin(origin.X + i * 8f, origin.Y + j * 8f));
                        float distanceFromPuncturePoint = new Vector2(fencePucturePoint.X - i, fencePucturePoint.Y - j).Length();

                        if (perlinpinpin + distanceFromPuncturePoint < fencePuctureRadius)
                        {
                            Main.tile[origin.X + i, origin.Y + j].WallType = 0;
                            WorldGen.SquareWallFrame(origin.X + i, origin.Y + j, true);
                        }

                        //Borders get tattered
                        else if (perlinpinpin + distanceFromPuncturePoint < fencePuctureRadius + 1)
                            Main.tile[origin.X + i, origin.Y + j].WallType = (ushort)ModContent.WallType<LatticeFenceTattered>();
                    }
                }
            }
        }

        public static void PlaceScrapyardWulfrumPatch(Point center, int width, int depth, int minX, int maxX)
        {
            int even = WorldGen.genRand.Next(0, 2);
            bool placedRotor = false;

            for (int i = -width; i < width + even; i++)
            {
                bool reachedTopPortion = false;
                if (center.X + i < minX || center.X + i > maxX)
                    continue;

                for (int j = -2; j <= depth; j++)
                {
                    Tile t = Main.tile[center.X + i, center.Y + j];
                    if (j < 0 || !t.HasTile || t.TileType == dustyBrickType || !Main.tileSolid[t.TileType])
                    {
                        float perlin = Math.Abs(genNoise.GetPerlin(center.X * 3 + i * 7, center.Y * 3));
                        float height = j < 0 ? 2f : depth;

                        float radius = FablesUtils.RadiusAtEllipsePoint(width, height, new Vector2(i, j));

                        if (new Vector2(i, j).Length() - perlin * 2f < radius)
                        {
                            Point tilePos = center + new Point(i, j);
                            Tile tile = Main.tile[tilePos];
                            tile.ClearTile();
                            tile.TileColor = 0;
                            tile.LiquidAmount = 0;
                            tile.HasTile = true;
                            tile.TileType = landfillType;
                            if (!reachedTopPortion && !placedRotor && WorldGen.genRand.NextBool(5))
                            {
                                placedRotor = true;
                                tile.TileType = WorldGen.genRand.NextBool() ? landfillSmallRotorType : landfillBigRotorType;
                            }

                            Tile.SmoothSlope(tilePos.X, tilePos.Y);
                            WorldGen.SquareTileFrame(tilePos.X, tilePos.Y, true);
                            reachedTopPortion = true;
                        }
                    }
                }
            }

            //Avoid weird spikes by turning /\ slope formations into half blocks instead!
            for (int i = -width; i < width; i++)
            {
                for (int j = -2; j <= depth; j++)
                {
                    Point tilePos = center + new Point(i, j);

                    if (Main.tile[tilePos].TileType == landfillType && Main.tile[tilePos].Slope != SlopeType.Solid)
                    {
                        Tile tile = Main.tile[tilePos];
                        Tile tileLeft = Main.tile[center + new Point(i - 1, j)];
                        Tile tileRight = Main.tile[center + new Point(i + 1, j)];

                        if (tile.Slope == SlopeType.SlopeDownLeft && tileLeft.Slope == SlopeType.SlopeDownRight)
                        {
                            tile.Slope = SlopeType.Solid;
                            tile.IsHalfBlock = true;

                            tileLeft.Slope = SlopeType.Solid;
                            tileLeft.IsHalfBlock = true;
                        }

                        else if (tile.Slope == SlopeType.SlopeDownRight && tileRight.Slope == SlopeType.SlopeDownLeft)
                        {
                            tile.Slope = SlopeType.Solid;
                            tile.IsHalfBlock = true;

                            tileRight.Slope = SlopeType.Solid;
                            tileRight.IsHalfBlock = true;
                        }

                    }
                }
            }
        }
        #endregion
        #endregion

        #region Bunker
        public static Point BunkerSize => new Point(89, 40);
        public static void PlaceBunker(Point bunkerCenter, int surfaceHeight)
        {
            int mainChamberRectHeight = 34;
            int mainChamberRectHalfWidth = 44;
            int antechamberHalfWidth = 27;

            //Always generate on a odd height, tis is for the ideal look with the dull plating walls
            if (bunkerCenter.Y % 2 == 0)
                bunkerCenter.Y++;

            dullPlatingType = (ushort)ModContent.TileType<DullPlating>();
            dustyBrickType = (ushort)ModContent.TileType<DustyBricks>();
            rustySheetType = (ushort)ModContent.TileType<RustySheets>();

            dustyWallType = (ushort)ModContent.WallType<DustyBrickWallUnsafe>();
            dullWallType = (ushort)ModContent.WallType<DullPlatingWallUnsafe>();
            sheetWallType = (ushort)ModContent.WallType<WulfrumPlasticWallUnsafe>();

            PlaceMainBunkerChamber(bunkerCenter, mainChamberRectHalfWidth, mainChamberRectHeight);
            PlaceAntechamberFloor(bunkerCenter, antechamberHalfWidth, 7);
            PlaceAntechamberTop(bunkerCenter, antechamberHalfWidth, 11, 7);

            PlaceElevator(bunkerCenter, surfaceHeight);
            DecorateAntechamber(bunkerCenter, 5, antechamberHalfWidth - 2 - 7, out bool placedWorkshop);
            PlaceVault(bunkerCenter, placedWorkshop);
            PlaceChainsAndDebrisPiles(bunkerCenter, mainChamberRectHalfWidth, antechamberHalfWidth - 7 - 2, antechamberHalfWidth - 3 - 2);
            InstallUndergroundPipes(bunkerCenter);

            //Turn dull plating into the proper types
            for (int j = bunkerCenter.Y - 50; j < bunkerCenter.Y + 50; j++)
            {
                int dullPlatingSubtype = WorldGen.genRand.Next(3);
                int platedType = ModContent.TileType<DullPlatingPlated>();
                int accentType = ModContent.TileType<DullPlatingPlatedAccent>();

                switch (dullPlatingSubtype)
                {
                    case 0:
                        dullPlatingSubtype = platedType;
                        break;
                    case 1:
                        dullPlatingSubtype = accentType;
                        break;
                    default:
                        dullPlatingSubtype = dullPlatingType;
                        break;
                }

                int dullPlateLenght = WorldGen.genRand.Next(3, 8);
                if (dullPlatingSubtype == dullPlatingType)
                    dullPlateLenght--;

                for (int i = bunkerCenter.X - 50; i < bunkerCenter.X + 50; i++)
                {
                    Tile t = Main.tile[i, j];
                    if (t.TileType == dullPlatingType)
                    {
                        //Get new type
                        if (dullPlateLenght < 0)
                        {
                            if (dullPlatingSubtype == dullPlatingType)
                                dullPlatingSubtype = WorldGen.genRand.NextBool() ? platedType : accentType;
                            else if (dullPlatingSubtype == platedType)
                                dullPlatingSubtype = WorldGen.genRand.NextBool(3) ? accentType : dullPlatingType;
                            else
                                dullPlatingSubtype = WorldGen.genRand.NextBool(3) ? platedType : dullPlatingType;

                            dullPlateLenght = WorldGen.genRand.Next(3, 8);
                            if (dullPlatingSubtype == dullPlatingType)
                                dullPlateLenght--;
                        }

                        t.TileType = (ushort)dullPlatingSubtype;
                    }

                    dullPlateLenght--;
                }
            }

            PointOfInterestMarkerSystem.WulfrumBunkerPos = bunkerCenter + new Point(0, mainChamberRectHeight - 5);
            //Placeholder pass that recolors wulfrum plastic walls
            for (int i = bunkerCenter.X - 50; i < bunkerCenter.X + 50; i++)
            {
                for (int j = bunkerCenter.Y - 50; j < bunkerCenter.Y + 50; j++)
                {
                    Tile t = Main.tile[i, j];
                    if (t.WallType == sheetWallType)
                        t.WallColor = PaintID.BlackPaint;
                }
            }
            WorldGen.RangeFrame(bunkerCenter.X - 50, bunkerCenter.Y - 50, bunkerCenter.X + 50, bunkerCenter.Y + 50);
        }

        #region Bunker blockout stage
        /// <summary>
        /// Carves out the main section of the bunker, the large central chamber with the water grate below and more
        /// </summary>
        private static void PlaceMainBunkerChamber(Point bunkerCenter, int mainChamberRectHalfWidth, int mainChamberRectHeight)
        {
            ushort emptyWallType = (ushort)ModContent.WallType<InvisibleBunkerWall>();

            //Create the main chamber of the structure
            for (int i = bunkerCenter.X - mainChamberRectHalfWidth; i <= bunkerCenter.X + mainChamberRectHalfWidth; i++)
            {
                for (int j = bunkerCenter.Y; j < bunkerCenter.Y + mainChamberRectHeight; j++)
                {
                    int distanceToCenter = Math.Abs(i - bunkerCenter.X);
                    int heightInChamber = j - bunkerCenter.Y;
                    Tile t = Main.tile[i, j];
                    t.LiquidAmount = 0;
                    t.WallFrameX = 0;
                    t.WallFrameY = 0;
                    t.WallFrameNumber = 0;

                    #region Walls

                    //Vertical rim around the drawn background section
                    if (distanceToCenter <= 18 && distanceToCenter >= 16)
                        t.WallType = sheetWallType;
                    //Top walls are plating walls otherwise
                    else if (heightInChamber < 6)
                        t.WallType = dullWallType;

                    //Trim of sheet walls
                    else
                    {
                        //Outer part of the chamber, to the sides of the main grated portion
                        if (distanceToCenter > 17)
                        {
                            t.WallType = dustyWallType;
                        }

                        //Inner part of the chamber
                        else
                        {
                            //Either more sheet walls as a trim for the parallax, or nothing at all
                            if (heightInChamber <= 10)
                                t.WallType = sheetWallType;
                            else
                                t.WallType = emptyWallType;
                        }
                    }

                    #endregion

                    #region Tiles
                    //Borders and the bottom 3 floor tiles are made of dull plating
                    if (distanceToCenter == mainChamberRectHalfWidth || heightInChamber >= 31)
                    {
                        t.TileType = dullPlatingType;
                        t.HasTile = true;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                        continue;
                    }

                    //2 thick rusty sheets after the 1 thick dull plating borders ont he side, and 7 thick rusty plating on the top
                    if (distanceToCenter >= mainChamberRectHalfWidth - 2 ||
                        (distanceToCenter > mainChamberRectHalfWidth - 17 && heightInChamber < 7))
                    {
                        t.TileType = rustySheetType;
                        t.HasTile = true;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                        continue;
                    }

                    //2 thick dusty brick flooring
                    if (heightInChamber >= mainChamberRectHeight - 5)
                    {
                        t.TileType = dustyBrickType;
                        t.HasTile = true;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                        continue;
                    }

                    //Clear the rest to make the empty space. We'll fill out the rest afterwards
                    t.HasTile = false;
                    t.Slope = SlopeType.Solid;
                    t.IsHalfBlock = false;
                    t.TileFrameX = 0;
                    t.TileFrameY = 0;
                    #endregion
                }
            }

            //Place the big grate
            int grateHalfWidth = 17;
            ushort grateType = (ushort)ModContent.TileType<GrimyGrate>();

            for (int i = bunkerCenter.X - grateHalfWidth - 3; i <= bunkerCenter.X + grateHalfWidth + 3; i++)
            {
                for (int j = bunkerCenter.Y + 28; j < bunkerCenter.Y + 38; j++)
                {
                    int distanceToCenter = Math.Abs(i - bunkerCenter.X);
                    int heightInGrate = j - bunkerCenter.Y - 28;
                    Tile t = Main.tile[i, j];

                    if (distanceToCenter <= grateHalfWidth)
                    {
                        t.WallType = dustyWallType;
                        WorldGen.SquareWallFrame(i, j);

                        if (heightInGrate < 7)
                        {
                            //No liquid at the very top, partial liquid in the first tile below that, and then full liquid
                            byte liquidAmount = (byte)(heightInGrate == 0 ? 0 : heightInGrate > 1 ? 255 : 150);
                            t.LiquidAmount = liquidAmount;
                            t.TileType = grateType;
                            t.HasTile = true;
                            t.Slope = SlopeType.Solid;
                            t.IsHalfBlock = false;
                            continue;
                        }
                        else
                        {
                            t.LiquidAmount = 0;
                            t.TileType = dullPlatingType;
                            t.HasTile = true;
                            t.Slope = SlopeType.Solid;
                            t.IsHalfBlock = false;
                            continue;
                        }
                    }

                    //3 wide border at the bottom
                    else
                    {
                        if (heightInGrate >= 3)
                        {
                            t.TileType = dullPlatingType;
                            t.HasTile = true;
                            t.Slope = SlopeType.Solid;
                            t.IsHalfBlock = false;
                        }
                        //Slope to the sides
                        else if (heightInGrate == 0 && distanceToCenter == grateHalfWidth + 1)
                        {
                            t.TileType = dustyBrickType;
                            t.HasTile = true;
                            t.Slope = i - bunkerCenter.X < 0 ? SlopeType.SlopeDownRight : SlopeType.SlopeDownLeft;
                            t.IsHalfBlock = false;
                        }
                        continue;
                    }
                }
            }

            //place two rails on either side of the big grate, and two long tube lights
            int railType = ModContent.TileType<RustyWulfrumElevatorRail>();
            int tubeLightType = ModContent.TileType<IndustrialWulfrumTubeLight>();

            for (int s = -1; s <= 1; s += 2)
            {
                int i = bunkerCenter.X + s * grateHalfWidth;
                for (int j = bunkerCenter.Y + 4; j < bunkerCenter.Y + mainChamberRectHeight - 6; j++)
                {
                    TileObject beam = new();
                    beam.xCoord = i - 1;
                    beam.yCoord = j;
                    beam.type = railType;
                    TileObject.Place(beam);
                }

                i = bunkerCenter.X + s * 34;
                for (int j = bunkerCenter.Y + 13; j < bunkerCenter.Y + mainChamberRectHeight - 10; j++)
                {
                    TileObject tubeLight = new();
                    tubeLight.xCoord = i;
                    tubeLight.yCoord = j;
                    tubeLight.type = tubeLightType;
                    tubeLight.alternate = 3;
                    TileObject.Place(tubeLight);
                }
            }
        }

        /// <summary>
        /// Creates the little platform that acts as the floor of the antechamber
        /// </summary>
        private static void PlaceAntechamberFloor(Point bunkerCenter, int antechamberHalfWidth, int openingWidth = 7)
        {
            ushort platformType = (ushort)ModContent.TileType<RustyHandrailPlatform>();

            for (int i = bunkerCenter.X - antechamberHalfWidth; i <= bunkerCenter.X + antechamberHalfWidth; i++)
            {
                for (int j = bunkerCenter.Y; j < bunkerCenter.Y + 5; j++)
                {
                    int distanceToCenter = Math.Abs(i - bunkerCenter.X);
                    int heightInChamber = j - bunkerCenter.Y;
                    Tile t = Main.tile[i, j];

                    bool insideMainPlatform = distanceToCenter <= antechamberHalfWidth - 2 - openingWidth;

                    //2 wide sheet metal walls, alongside metal sheets on the bottom of the main platform
                    if (distanceToCenter > antechamberHalfWidth - 2 || (insideMainPlatform && heightInChamber == 3))
                    {
                        t.TileType = rustySheetType;
                        t.HasTile = true;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                        continue;
                    }

                    if (heightInChamber == 4)
                        continue;

                    //Platforms and then bricks
                    if (heightInChamber == 0)
                    {
                        if (insideMainPlatform)
                            t.TileType = dustyBrickType;
                        else
                        {
                            //Gotta reset frame for platform so it properly recalculates when generating in tiles
                            t.TileType = platformType;
                            t.TileFrameY = 0;
                            t.TileFrameX = 0;
                        }

                        t.HasTile = true;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;

                        continue;
                    }

                    //Dull plating in the middle
                    if (insideMainPlatform)
                    {
                        t.TileType = dullPlatingType;
                        t.HasTile = true;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Carves out the top part of the antechamber, including walls and ceiling
        /// </summary>
        private static void PlaceAntechamberTop(Point bunkerCenter, int antechamberHalfWidth, int anteChamberHeight, int openingWidth = 7)
        {
            for (int i = bunkerCenter.X - antechamberHalfWidth; i <= bunkerCenter.X + antechamberHalfWidth; i++)
            {
                for (int j = bunkerCenter.Y - 1; j >= bunkerCenter.Y - anteChamberHeight; j--)
                {
                    int distanceToCenter = Math.Abs(i - bunkerCenter.X);
                    int heightInChamber = j - (bunkerCenter.Y - anteChamberHeight);
                    Tile t = Main.tile[i, j];

                    t.WallFrameX = 0;
                    t.WallFrameY = 0;
                    t.WallFrameNumber = 0;

                    //Walls : Top part with plate walls
                    if (heightInChamber <= 3 || (distanceToCenter < antechamberHalfWidth - 2 && distanceToCenter > antechamberHalfWidth - 1 - openingWidth))
                        t.WallType = dullWallType;
                    else
                        t.WallType = distanceToCenter <= 4 ? sheetWallType : dustyWallType;

                    //top layer of dull plating
                    if (heightInChamber == 0)
                    {
                        t.TileType = dullPlatingType;
                        t.HasTile = true;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                        continue;
                    }

                    //2 wide sheet metal walls and ceiling
                    if (distanceToCenter > antechamberHalfWidth - 2 || heightInChamber <= 2)
                    {
                        t.TileType = rustySheetType;
                        t.HasTile = true;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                        continue;
                    }

                    //Empty out the rest
                    t.HasTile = false;
                    t.Slope = SlopeType.Solid;
                    t.IsHalfBlock = false;
                }
            }

            //place tube lights on the ceiling, and the warning sign rubble on the sides
            int tubeLightType = ModContent.TileType<IndustrialWulfrumTubeLight>();
            int warningSignType = ModContent.TileType<BunkerWarningSignRubble>();
            int warningSignStyle = WorldGen.genRand.Next(2);

            for (int s = -1; s <= 1; s += 2)
            {
                int j = bunkerCenter.Y - anteChamberHeight + 3;
                for (int i = 7; i <= (antechamberHalfWidth - 4 - openingWidth); i++)
                {
                    TileObject tubeLight = new();
                    tubeLight.xCoord = bunkerCenter.X + i * s;
                    tubeLight.yCoord = j;
                    tubeLight.type = tubeLightType;
                    TileObject.Place(tubeLight);
                }

                j += 2;
                TileObject warningSign = new();
                warningSign.xCoord = bunkerCenter.X + (antechamberHalfWidth - 5) * s - 1;
                warningSign.yCoord = j;
                warningSign.type = warningSignType;
                warningSign.style = warningSignStyle;
                TileObject.Place(warningSign);
            }
        }
        #endregion

        #region Bunker deco
        private static void DecorateAntechamber(Point bunkerCenter, int antechamberHallMinX, int antechamberHallMaxX, out bool placedWorkshop)
        {
            placedWorkshop = false;

            for (int s = -1; s <= 1; s += 2)
            {
                int bigDeco = WorldGen.genRand.Next(3);
                int bigDecoStyleRand;
                int bigDecoCount = 1;
                int bigDecoWidth = 5;
                int bigDecoheight;
                int bigDecoAlternate = 0;

                if (bigDeco == 0)
                {
                    bigDeco = ModContent.TileType<BunkerShelfRubble>();
                    bigDecoWidth = 4;
                    bigDecoStyleRand = 3;
                    if (WorldGen.genRand.NextBool())
                        bigDecoCount++;
                    bigDecoheight = 5;
                }
                else if (bigDeco == 1)
                {
                    bigDeco = ModContent.TileType<BunkerLongTableRubble>();
                    bigDecoStyleRand = 3;
                    bigDecoheight = 3;
                }
                else
                {
                    bigDeco = ModContent.TileType<BunkerWorkshop>();
                    bigDecoStyleRand = 1;
                    if (s <= 0)
                        bigDecoAlternate = 1;
                    bigDecoheight = 4;
                }

                //maximum x position accounting for the width of the tile and the possibility of duplicates
                int bigDecoMaxX = antechamberHallMaxX - antechamberHallMinX - bigDecoWidth * bigDecoCount;
                int bigDecoX;
                bool decoFar;

                //Place on either side
                if (WorldGen.genRand.NextBool())
                {
                    bigDecoX = WorldGen.genRand.Next(2);
                    decoFar = s < 0;
                }
                else
                {
                    bigDecoX = bigDecoMaxX - WorldGen.genRand.Next(2);
                    decoFar = s > 0;
                }

                if (s < 0)
                    bigDecoX = bunkerCenter.X - antechamberHallMaxX + bigDecoX + 1;
                else
                    bigDecoX = bunkerCenter.X + antechamberHallMinX + bigDecoX;

                //Generate the big decoration piece
                bool placedWallDeco = false;

                for (int d = 0; d < bigDecoCount; d++)
                {
                    TileObject decorationBig = new();
                    decorationBig.xCoord = bigDecoX;
                    decorationBig.yCoord = bunkerCenter.Y - bigDecoheight;
                    decorationBig.type = bigDeco;
                    decorationBig.style = WorldGen.genRand.Next(bigDecoStyleRand);
                    decorationBig.alternate = bigDecoAlternate;
                    TileObject.Place(decorationBig);

                    //Can place a screen or some other poster ontop of short enough big decos
                    if (bigDecoheight == 3 && !WorldGen.genRand.NextBool(3))
                    {
                        TileObject wallDecoTop = new();
                        wallDecoTop.xCoord = bigDecoX + 1;
                        wallDecoTop.yCoord = bunkerCenter.Y - bigDecoheight - 3;
                        wallDecoTop.style = 0;

                        int wallDecoType = WorldGen.genRand.Next(4);
                        switch (wallDecoType)
                        {
                            case 0:
                                wallDecoType = ModContent.TileType<WulfrumMortarPoster>();
                                break;
                            case 1:
                                wallDecoType = ModContent.TileType<WulfrumRoverPoster>();
                                break;
                            case 2:
                                wallDecoType = ModContent.TileType<WulfrumWashingMachinePoster>();
                                break;
                            case 3:
                                wallDecoType = ModContent.TileType<BunkerScreenRubble>();
                                wallDecoTop.style = WorldGen.genRand.Next(4);
                                break;
                        }
                        wallDecoTop.type = wallDecoType;
                        TileObject.Place(wallDecoTop);
                        placedWallDeco = true;
                    }

                    bigDecoX += bigDecoWidth;
                }

                if (bigDeco == ModContent.TileType<BunkerWorkshop>())
                    placedWorkshop = true;

                //Chance for no small deco
                if (WorldGen.genRand.NextBool(6))
                    continue;

                int smallDecoMinX;
                int smallDecoMaxX;

                if (s < 0)
                {
                    smallDecoMinX = bunkerCenter.X - antechamberHallMaxX;
                    smallDecoMaxX = bunkerCenter.X - antechamberHallMinX;

                    if (decoFar)
                        smallDecoMinX = bigDecoX;
                    else
                        smallDecoMaxX = bigDecoX - bigDecoWidth * bigDecoCount - 1;
                }
                else
                {
                    smallDecoMinX = bunkerCenter.X + antechamberHallMinX;
                    smallDecoMaxX = bunkerCenter.X + antechamberHallMaxX;

                    if (decoFar)
                        smallDecoMaxX = bigDecoX - bigDecoWidth * bigDecoCount - 1;
                    else
                        smallDecoMinX = bigDecoX;
                }

                int availableWidth = smallDecoMaxX - smallDecoMinX;
                int smallDeco = WorldGen.genRand.Next(4);

                //Not enough space, oh well
                if (availableWidth <= 2)
                    continue;

                //Force it to be chair only if not enough space
                if (availableWidth <= 3)
                    smallDeco = 2;
                else if (availableWidth > 5 && smallDeco == 2)
                    smallDeco = WorldGen.genRand.NextBool() ? 0 : 3;


                int smallDecoStyle = 0;
                int smallDecoWidth = 2;
                int smallDecoHeight = 1;
                int smallDecoAlternate = 0;

                switch (smallDeco)
                {
                    //Tables or chair & workbench
                    case 0:
                    case 1:
                    case 3:
                        smallDecoMaxX -= 3;

                        //Trim the edges for breathing room if we have enough space!
                        if (smallDecoMaxX - smallDecoMinX > 2)
                        {
                            smallDecoMaxX--;
                            smallDecoMinX++;
                        }
                        else if (smallDecoMaxX - smallDecoMinX > 1)
                        {
                            if (s < 0 == decoFar)
                                smallDecoMaxX--;
                            else
                                smallDecoMinX++;
                        }
                        smallDecoWidth = 4;

                        if (smallDeco <= 1)
                        {
                            smallDeco = ModContent.TileType<BunkerTableRubble>();
                            smallDecoStyle = WorldGen.genRand.Next(2);
                            smallDecoHeight = 2;
                        }
                        else
                        {
                            smallDeco = ModContent.TileType<WulfrumWorkbench>();
                            smallDecoHeight = 1;
                        }
                        break;

                    case 2:
                        smallDecoMaxX -= 2;
                        //Trim the edges for breathing room if we have enough space!
                        if (smallDecoMaxX - smallDecoMinX > 2)
                        {
                            smallDecoMaxX--;
                            smallDecoMinX++;
                        }
                        else if (smallDecoMaxX - smallDecoMinX > 1)
                        {
                            if (s < 0 == decoFar)
                                smallDecoMaxX--;
                            else
                                smallDecoMinX++;
                        }
                        smallDecoWidth = 2;
                        smallDeco = ModContent.TileType<BunkerChairRubble>();
                        smallDecoStyle = 0;
                        smallDecoHeight = 3;
                        if (s > 0)
                            smallDecoAlternate = 1;

                        break;

                }

                //failsafe
                if (smallDecoMinX >= smallDecoMaxX)
                    continue;

                int smallDecoX = WorldGen.genRand.Next(smallDecoMinX, smallDecoMaxX);

                TileObject decorationSmall = new();
                decorationSmall.xCoord = smallDecoX;
                decorationSmall.yCoord = bunkerCenter.Y - smallDecoHeight;
                decorationSmall.type = smallDeco;
                decorationSmall.style = smallDecoStyle;
                decorationSmall.alternate = smallDecoAlternate;
                TileObject.Place(decorationSmall);

                //place the chair to go along with the workbench
                if (smallDeco == ModContent.TileType<WulfrumWorkbench>())
                {
                    decorationSmall = new();
                    decorationSmall.xCoord = smallDecoX + 2;
                    decorationSmall.yCoord = bunkerCenter.Y - 3;
                    decorationSmall.type = ModContent.TileType<BunkerChairRubble>();
                    decorationSmall.style = 0;
                    decorationSmall.alternate = 1;
                    TileObject.Place(decorationSmall);
                }

                //place poster and such
                if (availableWidth >= 3 && !placedWallDeco && !WorldGen.genRand.NextBool(4))
                {
                    TileObject wallDecoTop = new();
                    wallDecoTop.xCoord = smallDecoX;
                    wallDecoTop.yCoord = bunkerCenter.Y - 6;
                    wallDecoTop.style = 0;

                    int wallDecoType = WorldGen.genRand.Next(4);
                    switch (wallDecoType)
                    {
                        case 0:
                            wallDecoType = ModContent.TileType<WulfrumMortarPoster>();
                            break;
                        case 1:
                            wallDecoType = ModContent.TileType<WulfrumRoverPoster>();
                            break;
                        case 2:
                            wallDecoType = ModContent.TileType<WulfrumWashingMachinePoster>();
                            break;
                        case 3:
                            wallDecoType = ModContent.TileType<BunkerScreenRubble>();
                            wallDecoTop.style = WorldGen.genRand.Next(4);
                            break;
                    }
                    wallDecoTop.type = wallDecoType;
                    TileObject.Place(wallDecoTop);
                }
            }
        }

        /// <summary>
        /// Places down chains hanging from the antechamber and puts debris in the main room
        /// </summary>
        /// <param name="bunkerCenter"></param>
        private static void PlaceChainsAndDebrisPiles(Point bunkerCenter, int mainChamberRectHalfWidth, int antechamberHalfWidth, int platformChainDistanceX)
        {
            //Don't care about the very edges cuz there's the rails there
            antechamberHalfWidth -= 2;

            //Place down the chains hanging from the platforms: easy because consistent
            for (int s = -1; s <= 1; s += 2)
            {
                int chainLength = WorldGen.genRand.Next(8, 17);

                RustyWulfrumChain chain = new RustyWulfrumChain();
                chain.Anchor = new Point(bunkerCenter.X + platformChainDistanceX * s, bunkerCenter.Y);
                chain.EndPoint = new Point(bunkerCenter.X + platformChainDistanceX * s, bunkerCenter.Y + chainLength);
                chain.randomSeed = WorldGen.genRand.Next(20000000);
                chain.ChainSagValue = 0;
                RustyWulfrumChainsItem.chainManager.TryPlaceNewObject(chain);
            }

            bool[] bannedChainXPositions = new bool[antechamberHalfWidth * 2 + 1];
            int placedChains = 0;
            bool placedStraightDownAlready = false;

            bool placedLongDownAlready = false;

            //Place chains below the main antechamber
            while (placedChains < 3)
            {
                int randomX = WorldGen.genRand.Next(0, antechamberHalfWidth * 2 + 1);

                //oopsy fail!
                if (bannedChainXPositions[randomX])
                {
                    placedChains++;
                    continue;
                }

                int posX = bunkerCenter.X - antechamberHalfWidth + randomX;

                //Only 1 max straight down chain
                bool straightDown = !placedStraightDownAlready && WorldGen.genRand.NextBool(3);
                if (straightDown)
                {
                    placedStraightDownAlready = true;

                    int chainLength = WorldGen.genRand.Next(5, 13);
                    RustyWulfrumChain chain = new RustyWulfrumChain();
                    chain.Anchor = new Point(posX, bunkerCenter.Y + 3);
                    chain.EndPoint = new Point(posX, bunkerCenter.Y + 3 + chainLength);
                    chain.randomSeed = WorldGen.genRand.Next(20000000);
                    chain.ChainSagValue = 0;
                    RustyWulfrumChainsItem.chainManager.TryPlaceNewObject(chain);
                    bannedChainXPositions[randomX] = true;
                    placedChains++;
                }
                else
                {
                    int chainDistX = WorldGen.genRand.Next(5, 19);
                    int chainDir = WorldGen.genRand.NextBool() ? -1 : 1;
                    int arrayIndex = randomX + chainDistX * chainDir;

                    //If the chain would land outside the antechamber bottom
                    if (arrayIndex < 0 || arrayIndex >= antechamberHalfWidth * 2 + 1)
                        chainDir *= -1;

                    arrayIndex = randomX + chainDistX * chainDir;
                    //Couldn't find a good placement pos, rip
                    if (arrayIndex < 0 || arrayIndex >= antechamberHalfWidth * 2 + 1)
                    {
                        placedChains++;
                        continue;
                    }


                    RustyWulfrumChain chain = new RustyWulfrumChain();
                    chain.Anchor = new Point(posX, bunkerCenter.Y + 3);
                    chain.EndPoint = new Point(posX + chainDistX * chainDir, bunkerCenter.Y + 3);
                    chain.ChainSagValue = WorldGen.genRand.Next(1, 5);

                    //Long chain downwards
                    if (!placedLongDownAlready && WorldGen.genRand.NextBool(6))
                    {
                        int chainEndX = posX + chainDistX * chainDir;
                        chainEndX += chainDir * WorldGen.genRand.Next(0, 9);

                        placedLongDownAlready = true;
                        for (int j = bunkerCenter.Y + 4; j < bunkerCenter.Y + 40; j++)
                        {
                            if (Main.tile[chainEndX, j].HasTile)
                            {
                                chain.EndPoint = new Point(chainEndX, j);
                                chain.ChainSagValue = WorldGen.genRand.Next(2);

                                break;
                            }
                        }
                    }

                    chain.randomSeed = WorldGen.genRand.Next(20000000);
                    RustyWulfrumChainsItem.chainManager.TryPlaceNewObject(chain);

                    bannedChainXPositions[arrayIndex] = true;
                    bannedChainXPositions[randomX] = true;
                    placedChains++;
                }
            }

            //Left side debris
            if (!WorldGen.genRand.NextBool(6))
            {
                int leftX = bunkerCenter.X - mainChamberRectHalfWidth + 3;
                int pileX = WorldGen.genRand.Next(leftX, leftX + WorldGen.genRand.Next(20));

                PlaceDebrisPilesInSection(pileX, Math.Min(pileX + WorldGen.genRand.Next(2, 11), bunkerCenter.X - 18), bunkerCenter.Y + 28);
            }
            //Right side debris
            if (!WorldGen.genRand.NextBool(6))
            {
                int rightX = bunkerCenter.X + mainChamberRectHalfWidth - 3;
                int pileX = WorldGen.genRand.Next(rightX - WorldGen.genRand.Next(20), rightX);

                PlaceDebrisPilesInSection(Math.Max(pileX - WorldGen.genRand.Next(2, 11), bunkerCenter.X + 18), pileX, bunkerCenter.Y + 28);
            }

            //Middle-left debris pile
            if (!WorldGen.genRand.NextBool(3))
            {
                int leftX = bunkerCenter.X - 15;
                int pileX = WorldGen.genRand.Next(leftX, leftX + WorldGen.genRand.Next(10));

                PlaceDebrisPilesInSection(pileX, Math.Min(pileX + WorldGen.genRand.Next(3, 8), bunkerCenter.X - 4), bunkerCenter.Y + 27);
            }
            //Middle-right
            if (!WorldGen.genRand.NextBool(3))
            {
                int leftX = bunkerCenter.X + 4;
                int pileX = WorldGen.genRand.Next(leftX, leftX + WorldGen.genRand.Next(10));

                PlaceDebrisPilesInSection(pileX, Math.Min(pileX + WorldGen.genRand.Next(3, 8), bunkerCenter.X + 15), bunkerCenter.Y + 27);
            }


            #region Placing rubble piles <3
            List<Point> validRubblePositions = new();
            int rubbleType = ModContent.TileType<Wulfrum3x2Piles>();
            int smallRubbleType = ModContent.TileType<Wulfrum1x1Piles>();

            for (int i = bunkerCenter.X - mainChamberRectHalfWidth + 3; i <= bunkerCenter.X + mainChamberRectHalfWidth - 3; i++)
            {
                int distanceToCenter = Math.Abs(bunkerCenter.X - i);
                //Not too close to the center please
                if (distanceToCenter < 3)
                    continue;

                int placeY = bunkerCenter.Y + 28;
                Tile tileAtPos = Main.tile[i, placeY];
                if (tileAtPos.HasTile)
                {
                    //Move up if inside floor (grate)
                    if (Main.tileSolid[tileAtPos.TileType])
                        placeY--;
                    //Ignore if inside already placed prop
                    else
                        continue;
                }

                if (TileObject.CanPlace(i, placeY - 1, rubbleType, 0, 0, out _))
                    validRubblePositions.Add(new Point(i, placeY - 1));
            }

            int rubbleCount = WorldGen.genRand.Next(1, 5);
            for (int i = 0; i < rubbleCount; i++)
            {
                if (validRubblePositions.Count <= 0)
                    break;

                Point chosenRubblePos = WorldGen.genRand.Next(validRubblePositions);

                int placedType = WorldGen.genRand.NextBool(4) ? smallRubbleType : rubbleType;
                int placedStyle = WorldGen.genRand.Next(5);
                WorldGen.PlaceObject(chosenRubblePos.X, chosenRubblePos.Y, placedType, true, placedStyle);
                //Clear nearby positions
                validRubblePositions.RemoveAll(p => Math.Abs(p.X - chosenRubblePos.X) <= 3);
            }
            #endregion
        }

        private static void PlaceDebrisPilesInSection(int minX, int maxX, int floorY)
        {
            ushort pileType = (ushort)ModContent.TileType<WulfrumScrapTile>();
            //Rare chance to get a silver pile instead $$$
            if (WorldGen.genRand.NextBool(10))
                pileType = TileID.SilverCoinPile;

            bool bigPile = maxX - minX > 3;
            int originalPileMin = minX;
            int originalPileMax = maxX;


            //Stop once the pile gets too small
            while (minX < maxX && floorY > 0)
            {
                for (int i = minX; i < maxX; i++)
                {
                    Tile t = Main.tile[i, floorY];
                    t.TileType = pileType;
                    t.HasTile = true;
                    t.Slope = SlopeType.Solid;
                    t.IsHalfBlock = false;
                }

                int shrinkLeft = WorldGen.genRand.Next(1, 4);
                int shrinkRight = WorldGen.genRand.Next(1, 4);
                //Thats ugly we dont like that!
                if (shrinkLeft == shrinkRight && shrinkLeft == 1)
                {
                    if (WorldGen.genRand.NextBool())
                        shrinkLeft++;
                    else
                        shrinkRight++;
                }


                minX += shrinkLeft;
                maxX -= shrinkRight;

                //wide piles cant end in 1 height
                if (bigPile && minX < maxX - 1 && floorY > 0)
                {
                    bigPile = false;
                    minX = originalPileMin + WorldGen.genRand.Next(1, 3);
                    maxX = originalPileMax - WorldGen.genRand.Next(1, 3);
                }

                if (minX == maxX - 1)
                {
                    if (WorldGen.genRand.NextBool())
                        minX--;
                    else
                        maxX++;
                }
                floorY--;
            }
        }

        private static void PlaceElevator(Point bunkerCenter, int elevatorTopY)
        {
            //Carve out a spot for the elevator base
            for (int i = bunkerCenter.X - 2; i <= bunkerCenter.X + 2; i++)
            {
                for (int j = bunkerCenter.Y; j < bunkerCenter.Y + 2; j++)
                {
                    Tile t = Main.tile[i, j];
                    t.HasTile = false;
                    t.Slope = SlopeType.Solid;
                    t.IsHalfBlock = false;
                }
            }

            //place down the elevator base
            TileObject elevatorBase = new();
            elevatorBase.xCoord = bunkerCenter.X - 2;
            elevatorBase.yCoord = bunkerCenter.Y;
            elevatorBase.type = ModContent.TileType<RustyWulfrumElevatorBase>();
            TileObject.Place(elevatorBase);
            ModContent.GetInstance<RustyWulfrumElevatorController>().Place(bunkerCenter.X, bunkerCenter.Y);

            //Place down the little chamber at the entrance
            for (int i = bunkerCenter.X - 4; i <= bunkerCenter.X + 4; i++)
            {
                for (int j = bunkerCenter.Y - 1; j >= bunkerCenter.Y - 11; j--)
                {
                    Tile t = Main.tile[i, j];
                    int distanceToCenter = Math.Abs(i - bunkerCenter.X);

                    if (distanceToCenter == 3)
                        t.WallType = dullWallType;

                    if (j == bunkerCenter.Y - 11)
                        t.WallType = sheetWallType;

                    if (distanceToCenter >= 3 && j < bunkerCenter.Y - 6)
                    {
                        t.TileType = rustySheetType;
                        t.HasTile = true;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                        continue;
                    }

                    if (distanceToCenter < 3)
                        t.HasTile = false;

                    //place down main rail
                    if (i == bunkerCenter.X + 1)
                    {
                        TileObject elevatorRail = new();
                        elevatorRail.xCoord = bunkerCenter.X - 1;
                        elevatorRail.yCoord = j;
                        elevatorRail.type = ModContent.TileType<RustyWulfrumElevatorRail>();
                        TileObject.Place(elevatorRail);
                    }
                }
            }

            //Place down the shaft
            int tilesClimbed = 1;
            for (int j = bunkerCenter.Y - 12; j >= elevatorTopY - 5; j--)
            {
                for (int i = bunkerCenter.X - 3; i <= bunkerCenter.X + 3; i++)
                {
                    Tile t = Main.tile[i, j];
                    int distanceToCenter = Math.Abs(i - bunkerCenter.X);
                    //Clear out the top portion
                    if (j < elevatorTopY)
                    {
                        t.HasTile = false;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                        continue;
                    }

                    t.WallType = tilesClimbed % 8 == 0 ? sheetWallType : dullWallType;
                    //place down the brodering of sheet metal
                    if (distanceToCenter == 3)
                    {
                        t.TileType = rustySheetType;
                        t.HasTile = true;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                        continue;
                    }

                    if (distanceToCenter < 3)
                    {
                        t.HasTile = false;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                    }

                    //place down main rail
                    if (i == bunkerCenter.X + 1 && j >= elevatorTopY + 2)
                    {
                        TileObject elevatorRail = new();
                        elevatorRail.xCoord = bunkerCenter.X - 1;
                        elevatorRail.yCoord = j;
                        elevatorRail.type = ModContent.TileType<RustyWulfrumElevatorRail>();
                        TileObject.Place(elevatorRail);
                    }
                    //Place down station
                    if (i == bunkerCenter.X + 2 && j == elevatorTopY)
                    {
                        TileObject elevatorStation = new();
                        elevatorStation.xCoord = bunkerCenter.X - 2;
                        elevatorStation.yCoord = j;
                        elevatorStation.type = ModContent.TileType<RustyWulfrumElevatorStation>();
                        TileObject.Place(elevatorStation);
                    }
                }

                tilesClimbed++;
            }
        }

        private static void InstallUndergroundPipes(Point bunkerCenter)
        {
            int couplingVerticalType = ModContent.TileType<WulfrumBigConduitCouplingVertical>();
            int couplingHorizontalType = ModContent.TileType<WulfrumBigConduitCouplingHorizontal>();
            int conduitType = ModContent.TileType<WulfrumBigConduit>();
            int exhaustType = ModContent.TileType<WulfrumBigConduitExhaustVertical>();

            for (int s = -1; s <= 1; s += 2)
            {
                //Carve out path for main horizontal pipe bit
                for (int i = -1; i <= 1; i++)
                {
                    int funnelX = bunkerCenter.X + 29 * s + i;
                    for (int j = bunkerCenter.Y - 16; j < bunkerCenter.Y + 5; j++)
                    {
                        Tile t = Main.tile[funnelX, j];
                        t.HasTile = false;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;

                        //Brick the pipes if far enough down
                        if (j >= bunkerCenter.Y - 10)
                            t.WallType = dustyWallType;
                    }
                }

                int pipeY = bunkerCenter.Y + 4;
                //place junction bit, then 2 pipes, then repeat
                for (int i = 0; i < 9; i++)
                {
                    TileObject straightPipe = new();
                    straightPipe.xCoord = bunkerCenter.X + 29 * s - 1;
                    straightPipe.yCoord = pipeY;
                    straightPipe.type = i % 3 == 0 ? couplingVerticalType : conduitType;
                    straightPipe.random = i % 3 == 0 ? 0 : WorldGen.genRand.Next(3);
                    TileObject.Place(straightPipe);

                    pipeY--;
                    if ((i + 1) % 3 != 0)
                        pipeY -= 2;

                    //Opening into plain air, potentially from a big slope, place an exhaust
                    if (i == 8 && !Main.tile[bunkerCenter.X + 29 * s - 1, pipeY].HasTile)
                    {
                        TileObject outdoorExhaust = new();
                        outdoorExhaust.xCoord = bunkerCenter.X + 29 * s - 1;
                        outdoorExhaust.yCoord = pipeY;
                        outdoorExhaust.type = exhaustType;
                        TileObject.Place(outdoorExhaust);

                    }
                }

                //place bottom section. Carve out opening first
                for (int i = -1; i <= 1; i++)
                {
                    int funnelX = bunkerCenter.X + 29 * s + i;
                    for (int j = bunkerCenter.Y + 5; j < bunkerCenter.Y + 9; j++)
                    {
                        Tile t = Main.tile[funnelX, j];
                        t.HasTile = false;
                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                    }
                }

                //place big pipe, then exhaust
                TileObject straightEnd = new();
                straightEnd.xCoord = bunkerCenter.X + 29 * s - 1;
                straightEnd.yCoord = bunkerCenter.Y + 5;
                straightEnd.type = conduitType;
                straightEnd.random = WorldGen.genRand.Next(3);
                TileObject.Place(straightEnd);
                straightEnd = new();
                straightEnd.xCoord = bunkerCenter.X + 29 * s - 1;
                straightEnd.yCoord = bunkerCenter.Y + 8;
                straightEnd.type = exhaustType;
                TileObject.Place(straightEnd);


                //Put grates under the pipe
                for (int i = -2; i <= 2; i++)
                {
                    for (int j = bunkerCenter.Y + 29; j < bunkerCenter.Y + 31; j++)
                    {
                        Tile t = Main.tile[bunkerCenter.X + 29 * s + i, j];
                        t.TileType = (ushort)ModContent.TileType<GrimyGrate>();
                        t.LiquidAmount = 200;
                    }
                }

                //Carve out side path now
                for (int i = 31; i < 38; i++)
                {
                    int funnelX = bunkerCenter.X + i * s;
                    for (int j = bunkerCenter.Y - 6; j < bunkerCenter.Y - 3; j++)
                    {
                        Tile t = Main.tile[funnelX, j];
                        t.HasTile = false;

                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                    }
                }
                for (int i = -1; i <= 1; i++)
                {
                    int funnelX = bunkerCenter.X + 36 * s + i;
                    for (int j = bunkerCenter.Y - 3; j < bunkerCenter.Y + 5; j++)
                    {
                        Tile t = Main.tile[funnelX, j];
                        t.HasTile = false;

                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                    }
                }
                for (int i = -1; i <= 1; i++)
                {
                    int funnelX = bunkerCenter.X + 39 * s + i;
                    for (int j = bunkerCenter.Y + 2; j < bunkerCenter.Y + 7; j++)
                    {
                        Tile t = Main.tile[funnelX, j];
                        t.HasTile = false;

                        t.Slope = SlopeType.Solid;
                        t.IsHalfBlock = false;
                    }
                }

                //Place the tube now. What ugly hardcoding. Thats what i get for not using structurehelper, but oh well!
                TileObject sidePipe = new();
                sidePipe.xCoord = bunkerCenter.X + 31 * s;
                sidePipe.yCoord = bunkerCenter.Y - 6;
                sidePipe.type = couplingHorizontalType;
                TileObject.Place(sidePipe);
                sidePipe = new();
                sidePipe.xCoord = bunkerCenter.X + 33 * s - 1;
                sidePipe.yCoord = bunkerCenter.Y - 6;
                sidePipe.type = conduitType;
                sidePipe.random = WorldGen.genRand.Next(3);
                TileObject.Place(sidePipe);
                sidePipe = new();
                sidePipe.xCoord = bunkerCenter.X + 36 * s - 1;
                sidePipe.yCoord = bunkerCenter.Y - 6;
                sidePipe.type = conduitType;
                sidePipe.random = WorldGen.genRand.Next(3);
                TileObject.Place(sidePipe);
                sidePipe = new();
                sidePipe.xCoord = bunkerCenter.X + 36 * s - 1;
                sidePipe.yCoord = bunkerCenter.Y - 3;
                sidePipe.type = couplingVerticalType;
                TileObject.Place(sidePipe);
                sidePipe = new();
                sidePipe.xCoord = bunkerCenter.X + 36 * s - 1;
                sidePipe.yCoord = bunkerCenter.Y - 2;
                sidePipe.type = conduitType;
                sidePipe.random = WorldGen.genRand.Next(3);
                TileObject.Place(sidePipe);
                sidePipe = new();
                sidePipe.xCoord = bunkerCenter.X + 36 * s - 1;
                sidePipe.yCoord = bunkerCenter.Y + 1;
                sidePipe.type = couplingVerticalType;
                TileObject.Place(sidePipe);
                sidePipe = new();
                sidePipe.xCoord = bunkerCenter.X + 36 * s - 1;
                sidePipe.yCoord = bunkerCenter.Y + 2;
                sidePipe.type = conduitType;
                sidePipe.random = WorldGen.genRand.Next(3);
                TileObject.Place(sidePipe);

                sidePipe = new();
                sidePipe.xCoord = bunkerCenter.X + 39 * s - 1;
                sidePipe.yCoord = bunkerCenter.Y + 2;
                sidePipe.type = conduitType;
                sidePipe.random = WorldGen.genRand.Next(3);
                TileObject.Place(sidePipe);
                sidePipe = new();
                sidePipe.xCoord = bunkerCenter.X + 39 * s - 1;
                sidePipe.yCoord = bunkerCenter.Y + 5;
                sidePipe.type = conduitType;
                sidePipe.random = WorldGen.genRand.Next(3);
                TileObject.Place(sidePipe);
            }
        }
        #endregion

        #endregion


        #region chest
        public static void PlaceVault(Point bunkerCenter, bool placedWorkshop)
        {
            WorldGen.PlaceObject(bunkerCenter.X, bunkerCenter.Y + 27, ModContent.TileType<WulfrumVault>(), true, 1);
            int c = Chest.CreateChest(bunkerCenter.X - 2, bunkerCenter.Y + 25);
            ModContent.GetInstance<WulfrumNexusSpawner>().Place(bunkerCenter.X - 2, bunkerCenter.Y + 25);

            if (c > -1)
            {
                Chest chest = Main.chest[c];
                uint itemIndex = 0;

                void PutItemInChest(int id, int minQuantity = 0, int maxQuantity = 0, bool condition = true)
                {
                    if (!condition)
                        return;
                    chest.item[itemIndex].SetDefaults(id, false);

                    // Don't set quantity unless quantity is specified
                    if (minQuantity > 0)
                    {
                        // Max quantity cannot be less than min quantity. It's zero if not specified, meaning you get exactly minQuantity.
                        if (maxQuantity < minQuantity)
                            maxQuantity = minQuantity;
                        chest.item[itemIndex].stack = WorldGen.genRand.Next(minQuantity, maxQuantity + 1);
                    }
                    itemIndex++;
                }

                //Place the life crystals
                PutItemInChest(ItemID.LifeCrystal, 2, 2);

                //Acropack
                PutItemInChest(ModContent.ItemType<WulfrumAcrobaticsPack>(), 1, 1);

                // Voicebox vanity
                PutItemInChest(ModContent.ItemType<WulfrumVoiceBox>(), 1, 1);

                int wulfrumLoot = WorldGen.genRand.Next(5);
                switch (wulfrumLoot)
                {
                    case 0:
                        PutItemInChest(ModContent.ItemType<AbandonedWulfrumHelmet>(), 1, 1);
                        break;
                    case 1:
                    case 2:
                        PutItemInChest(ModContent.ItemType<RoverDrive>(), 1, 1);
                        break;
                    case 3:
                    case 4:
                        PutItemInChest(ModContent.ItemType<WulfrumScaffoldKit>(), 1, 1);
                        break;
                }

                //Put an item in the vault if not crafted
                PutItemInChest(ModContent.ItemType<BunkerWorkshopItem>(), 1, 1, !placedWorkshop);

                if (CalamityFables.SpiritEnabled)
                {
                    if (ModContent.TryFind("SpiritReforged/ZiplineGun", out ModItem railgun) && WorldGen.genRand.NextBool(3))
                        PutItemInChest(railgun.Type, 1, 1);
                    if (ModContent.TryFind("SpiritReforged/TornMapPiece", out ModItem tatteredMap))
                        PutItemInChest(tatteredMap.Type, 1, 1);
                }

                PutItemInChest(ModContent.ItemType<DullPlatingItem>(), 20, 100);

                //Grenades
                PutItemInChest(ItemID.Grenade, 3, 5, WorldGen.genRand.NextBool(3));

                // Bars
                PutItemInChest(GenVars.goldBar, 10, 20);

                //Ropes
                PutItemInChest(ItemID.Rope, 50, 100, WorldGen.genRand.NextBool());

                //Healing pots
                PutItemInChest(ItemID.HealingPotion, 3, 5, WorldGen.genRand.NextBool());

                //Recall
                PutItemInChest(ItemID.RecallPotion, 3, 5, !WorldGen.genRand.NextBool(3));


                // 50% chance of 1 or 2 of the following potions
                int[] potions = new int[] {
                    ItemID.IronskinPotion, ItemID.RegenerationPotion, ItemID.EndurancePotion,
                    ItemID.SwiftnessPotion, ItemID.MiningPotion, ItemID.BuilderPotion
                };
                PutItemInChest(WorldGen.genRand.Next(potions), 1, 2, !WorldGen.genRand.NextBool(3));


                PutItemInChest(ItemID.GoldCoin, 5, 10);
                PutItemInChest(ItemID.SilverCoin, 10, 70);
            }
        }
        #endregion

        #region Background drawing
        public static Asset<Texture2D> ParallaxTexture1;
        private void LoadBackgroundTexturesBackgroundTextures()
        {
            ParallaxTexture1 ??= ModContent.Request<Texture2D>(AssetDirectory.WulfrumScrapyard + "BunkerParallax");
        }

        private void DrawBunkerBackground()
        {
            //No bunker, no background
            if (PointOfInterestMarkerSystem.WulfrumBunkerPos == Point.Zero)
                return;

            Vector2 bunkerCenter = PointOfInterestMarkerSystem.WulfrumBunkerPos.ToWorldCoordinates();
            Vector2 viewCenter = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
            Vector2 difference = (bunkerCenter - viewCenter);

            if (Math.Abs(difference.X) > Main.screenWidth * 1.5f || Math.Abs(difference.Y) > Main.screenHeight * 1.5f)
                return;

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
            Matrix view = Main.GameViewMatrix.TransformationMatrix;
            Matrix renderMatrix = Matrix.CreateTranslation(-Main.screenPosition.Vec3()) * view * projection;
            Effect effect = Scene["BunkerParallax"].GetShader().Shader;

            LoadBackgroundTexturesBackgroundTextures();

            effect.Parameters["layer1Texture"].SetValue(ParallaxTexture1.Value);
            effect.Parameters["parallaxStrenght"].SetValue(0.003f);
            difference /= 160f;
            difference.Y *= 0f;

            effect.Parameters["parallaxDisplace"].SetValue(difference);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin();

            LightedMeshRendering.Render(effect, renderMatrix, new Rectangle((int)PointOfInterestMarkerSystem.WulfrumBunkerPos.X - 18, (int)PointOfInterestMarkerSystem.WulfrumBunkerPos.Y - 18, 37, 17));

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        #endregion
    }
}