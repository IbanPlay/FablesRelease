using CalamityFables.Content.Boss.SeaKnightMiniboss;
using CalamityFables.Content.Tiles.BurntDesert;
using CalamityFables.Content.Tiles.Wulfrum;
using CalamityFables.Helpers;
using Microsoft.VisualBasic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;
using Terraria.WorldBuilding;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public class SealedChamber : ILoadable
    {
        public static Rectangle NautilusChamberRect;
        public static Rectangle InnerChamberRect;

        public static Vector2 ChamberSize => new Vector2(86, 40);
        public static  Vector2 ChamberInnerSize => new Vector2(74, 32);

        public void Load(Mod mod)
        {
            FablesDrawLayers.DrawThingsBehindWallsEvent += DrawSealedChamberBackground;
            FablesGeneralSystemHooks.AdditionalMineshaftChecks += PreventMinshaftsInNautilusChamber;
            FablesWall.KillWallEvent += PreventSealedChamberWallBreak;
            FablesWall.CanExplodeEvent += PreventSealedChamberWallInteraction;
            FablesWall.CanPlaceEvent += PreventSealedChamberWallInteraction;
        }

        #region Anti grief walls
        private bool PreventSealedChamberWallInteraction(int i, int j, int type) => !PointOfInterestMarkerSystem.NautilusChamberRectangle.Contains(i, j);
        private void PreventSealedChamberWallBreak(int i, int j, int type, ref bool fail)
        {
            if (PointOfInterestMarkerSystem.NautilusChamberRectangle.Contains(i, j))
                fail = true;
        }
        #endregion

        public void Unload() { }

        public static bool PreventMinshaftsInNautilusChamber(int x, int y)
        {
            Rectangle safetyRect = NautilusChamberRect;
            safetyRect.Inflate(20, 20);
            return safetyRect.Contains(new Point(x, y));
        }

        //Condition for crafting
        public static readonly Condition InNautilusChamber = new("Mods.CalamityFables.Conditions.InNautilusChamber", () => PointOfInterestMarkerSystem.NautilusChamberPos != Vector2.Zero && Main.LocalPlayer.Hitbox.Intersects(PointOfInterestMarkerSystem.NautilusChamberWorldRectangle));

        public static bool frameWalls = false;

        #region Sealed chamber
        public static void TryGenerate()
        {
            int padding = 5;
            bool chamberPlaced = false;
            int tries = 0;

            int minChamberX = GenVars.UndergroundDesertLocation.X + (int)ChamberSize.X;
            int maxChamberX = GenVars.UndergroundDesertLocation.X + GenVars.UndergroundDesertLocation.Width - (int)ChamberSize.X * 2;

            int minChamberY = (int)Main.worldSurface + 10;
            int maxChamberY = (int)Main.rockLayer + 10;

            //TODO remove this once remnants fixes it
            TileID.Sets.GeneralPlacementTiles[TileID.Granite] = true;
            TileID.Sets.GeneralPlacementTiles[TileID.Sandstone] = true;

            while (!chamberPlaced)
            {
                float lenience = -0.8f + 1.8f * Utils.GetLerpValue(800, 400, tries, true);
                int usedMinX = minChamberX + (int)(ChamberSize.X * 1.5f * lenience);
                int usedMaxX = maxChamberX - (int)(ChamberSize.X * 1.5f * lenience);
                bool tooTight = false;

                //WE GOTTA DO THIS CHECK CUZ DESERTS CAN GO THIS SMALL???
                if (usedMaxX <= usedMinX)
                {
                    int center = (minChamberX + maxChamberX) / 2;
                    usedMinX = center - (int)(ChamberSize.X * 1.5f);
                    usedMaxX = center + (int)(ChamberSize.X * 0.5f);
                    tooTight = true;
                }

                int x = WorldGen.genRand.Next(usedMinX, usedMaxX);

                //Check if we can even put a good graveyard above
                if (!TryFillGraveyardSpots(x + 10, x + (int)ChamberSize.X - 10))
                {
                    tries++;
                    if (tries > 800)
                        return;

                    //Start broadening immediately
                    if (tooTight && tries < 500)
                    {
                        tries = 500;
                    }

                    continue;
                }

                int heightIncrement = 3;

                //Pick a good initial position from the chamber near the middle of the range
                int idealHeightRangeStart = minChamberY + (maxChamberY - minChamberY) / 3;
                int idealHeightRangeEnd = maxChamberY - (maxChamberY - minChamberY) / 4;

                //A bit higher on large worlds
                if (Main.maxTilesY >= 2000)
                {
                    idealHeightRangeStart = minChamberY + (maxChamberY - minChamberY) / 4;
                    idealHeightRangeEnd = maxChamberY - (maxChamberY - minChamberY) / 3;
                }


                int initialHeight = WorldGen.genRand.Next(idealHeightRangeStart, idealHeightRangeEnd);

                for (int y = initialHeight; y < maxChamberY; y += heightIncrement)
                {
                    if (chamberPlaced)
                        break;

                    if (WorldGen.genRand.NextBool(20))
                    {
                        if (GenVars.structures.CanPlace(new Rectangle(x, y, (int)ChamberSize.X, (int)ChamberSize.Y), padding))
                        {
                            NautilusChamberRect = new Rectangle(x, y, (int)ChamberSize.X, (int)ChamberSize.Y);
                            InnerChamberRect = new Rectangle(x + (int)((ChamberSize.X - ChamberInnerSize.X) / 2f), y + (int)((ChamberSize.Y - ChamberInnerSize.Y) / 2f), (int)ChamberInnerSize.X, (int)ChamberInnerSize.Y);

                            GenVars.structures.AddProtectedStructure(NautilusChamberRect, padding);
                            chamberPlaced = true;

                            PointOfInterestMarkerSystem.NautilusChamberPos = new Vector2(NautilusChamberRect.Center.X, NautilusChamberRect.Bottom - 8);
                            PointOfInterestMarkerSystem.foundNautilusChamber = false;

                            GenerateCrakedSandstoneWallTrail();
                            PlaceChamber();
                            break;
                        }
                    }

                    //Deep enough, we reverse and start checking higher up instead
                    if (heightIncrement == 3 && y > idealHeightRangeEnd + 20)
                    {
                        //Just try another x position at first, only search in the extra areas if we couldnt do it before
                        if (tries < 100)
                            break;

                        heightIncrement = -2;
                        y = idealHeightRangeStart - 1;
                    }
                    //Reaching the max top, reverse again to go search even deeper
                    if (y <= minChamberY)
                    {
                        heightIncrement = 2;
                        y = idealHeightRangeEnd + 2;
                    }
                }

                //Start broadening immediately
                if (tooTight && tries < 500)
                {
                    tries = 500;
                }

                tries++;
                if (tries > 800)
                    return;
            }
        }

        #region Trail
        public static void GenerateCrakedSandstoneWallTrail()
        {
            Point chamberTop = new Point((int)PointOfInterestMarkerSystem.NautilusChamberPos.X, NautilusChamberRect.Y);
            Point fissurePosition = chamberTop;
            ushort crackedWallType = RockySandstoneWallItem.wallTypes[0];


            for (int y = chamberTop.Y; y > Main.worldSurface; y--)
            {
                float crackThickness = 4f + 6f * FablesWorld.genNoise.GetPerlin(fissurePosition.X, fissurePosition.Y);

                fissurePosition.Y--;
                fissurePosition.X = (int)(PointOfInterestMarkerSystem.NautilusChamberPos.X + FablesWorld.genNoise.GetPerlinFractal(fissurePosition.X, fissurePosition.Y * 4f) * 8);

                if (crackThickness <= 0)
                    continue;

                //Fill a circle around 
                for (int i = (int)(fissurePosition.X - crackThickness / 2f); i < fissurePosition.X + crackThickness / 2f; i++)
                {
                    for (int j = (int)(fissurePosition.Y - crackThickness / 2f); j < fissurePosition.Y + crackThickness / 2f; j++)
                    {
                        Tile t = Main.tile[i, j];
                        if (t.WallType != WallID.Sandstone && t.WallType != WallID.HardenedSand)
                            continue;

                        double xDistance = Math.Abs((double)i - fissurePosition.X);
                        double yDistance = Math.Abs((double)j - fissurePosition.Y);
                        double distToCenter = Math.Sqrt(xDistance * xDistance + yDistance * yDistance) * 2f;

                        //Cant replace ceiling tiles, but will replace floor tiles
                        if (distToCenter < crackThickness)
                        {
                            if (distToCenter < crackThickness * 0.5f)
                                t.WallType = crackedWallType;
                            else
                                t.WallType = Main.rand.NextFloat() < distToCenter / crackThickness ? WallID.Sandstone : crackedWallType;
                        }
                    }
                }
            }

        }
        #endregion

        #region Actually generating the chamber
        /// <summary>
        /// Does all the worldgen tasks for the chamber
        /// </summary>
        public static void PlaceChamber()
        {
            GenerateChamberBase();
            GenerateChamberCeiling();
            FillChamberWallsAndSmoothenCeiling(WorldGen.genRand.NextBool(2)); //Only smoothen the ceiling half the time, for variety
            SlantChamberFloor();
            RoundChamberCeiling();
            JandTheChamberFloor();
            ReplaceChamberFloorTiles();
            BreakWallOpen();

            AddChamberPillars();

            //Place a campfire in the middle of the arena (important)
            WorldGen.PlaceObject((int)(InnerChamberRect.X + InnerChamberRect.Width * 0.5), InnerChamberRect.Y + InnerChamberRect.Height - 1, ModContent.TileType<SandstoneCampfireTile>(), true);

            //Place the pile of rubble on the side. Extra failsafe just in case but shouldn't be needed
            int nautilusSpawnerType = ModContent.TileType<NautilusPedestal>();
            for (int i = 5; i < 8; i++)
            {
                int placeX = (int)(InnerChamberRect.X + InnerChamberRect.Width * 0.5 + i);
                int placeY = InnerChamberRect.Y + InnerChamberRect.Height - 1;

                if (TileObject.CanPlace(placeX, placeY, nautilusSpawnerType, 0, 0, out _))
                {
                    WorldGen.PlaceObject(placeX, placeY, nautilusSpawnerType, true);
                    ModContent.GetInstance<TESirNautilusSpawner>().Place(placeX, placeY);
                    break;
                }
            }

            //Place the seat for the player. Failsafe again even if not really necessary tbh
            int playerSeatType = ModContent.TileType<MakeshiftStoolTile>();
            for (int i = 4; i < 8; i++)
            {
                int placeX = (int)(InnerChamberRect.X + InnerChamberRect.Width * 0.5 - i);
                int placeY = InnerChamberRect.Y + InnerChamberRect.Height - 1;

                if (TileObject.CanPlace(placeX, placeY, playerSeatType, 0, 0, out _))
                {
                    WorldGen.PlaceObject(placeX, placeY, playerSeatType, true, 0, 0, 0, 1);
                    break;
                }
            }

            AddChamberSidePaintings();
            AddPropsToChamber();
            PlaceFallingRisingDust();
            return;
        }

        #region "Natural" gen, placing down the shape of the cave itself and carving it out of the desert
        /// <summary>
        /// Places down a rectangle made of hardened sand , with random bits of sand mixed in <br/>
        /// This step melds the chamber's sides with the surrounding caves, leaving sandstone on the left and right edges at 2 thickness
        /// </summary>
        public static void GenerateChamberBase()
        {
            int x = NautilusChamberRect.X;
            int y = NautilusChamberRect.Y;

            //Put the chamber itself as a block of hardened sand
            for (int i = 0; i < NautilusChamberRect.Width; i++)
                for (int j = 0; j < NautilusChamberRect.Height; j++)
                {
                    int tileType = TileID.HardenedSand;

                    //Edges of the chamber are 33% sand 66% hardened sand
                    if (i < 2 || i >= NautilusChamberRect.Width - 2)
                        tileType = WorldGen.genRand.NextBool(3) ? TileID.Sand : TileID.HardenedSand;

                    // The top of the chamber, in the inner portion, is 25% sand and 75% hardened sand.
                    // This leaves 2 rows on the edge of the chamber as pure hardened sand, to avoid making it annoying to dig the walls
                    else if (i + x > InnerChamberRect.X && i + x <= InnerChamberRect.X + InnerChamberRect.Width && j + y < InnerChamberRect.Y + InnerChamberRect.Height)
                        tileType = WorldGen.genRand.NextBool(4) ? TileID.Sand : TileID.HardenedSand;

                    //If at the edge of the chamber
                    if (i == 0 || i == NautilusChamberRect.Width - 1)
                    {
                        int direction = i == 0 ? -1 : 1;

                        //If the chamber isn't directly bordering another wall, extend a bit outside to avoid the weird rigid square feeling
                        if (!Main.tile[x + i + direction, y + j].IsTileSolid())
                        {
                            //First look for another sandstone wall in front of the wall
                            int extraDistance = 0;
                            for (int sideSearch = 1; sideSearch < 10; sideSearch++)
                            {
                                Tile tileAhead = Main.tile[x + i + sideSearch * direction, y + j];
                                if (tileAhead.TileType == TileID.Sandstone)
                                {
                                    extraDistance = 10 - sideSearch;
                                    break;
                                }
                            }

                            //If no wall closer than 10 tiles has been found, just make the edge of the chamber be made of sandstone, and place an extra sandstone to the side
                            if (extraDistance == 0)
                            {
                                tileType = TileID.Sandstone;
                                WorldGen.PlaceTile(x + i + direction, y + j, tileType, true, false);
                            }

                            //If a wall closer than 10 tiles has been found, add extra tiles to make it match the gap
                            else
                            {
                                for (int sidePlace = 1; sidePlace < extraDistance; sidePlace++)
                                {
                                    Tile tileAhead = Main.tile[x + i + direction * (sidePlace + 1), y + j];
                                    int boostType = WorldGen.genRand.NextBool(3) ? TileID.Sand : TileID.HardenedSand;

                                    if (sidePlace >= extraDistance - 2 && !(tileAhead.TileType == TileID.Sand || tileAhead.TileType == TileID.HardenedSand))
                                        boostType = TileID.Sandstone;

                                    WorldGen.PlaceTile(x + i + direction * sidePlace, y + j, boostType, true, true);
                                    if (tileAhead.IsTileSolid())
                                    {
                                        tileAhead.IsHalfBlock = false;
                                        tileAhead.Slope = SlopeType.Solid;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    //Place the tile and empty out liquids (can happen on small worlds if it generates too close to the sunky sea
                    WorldGen.KillTile(x + i, y + j);
                    WorldGen.PlaceTile(x + i, y + j, tileType, true, true);
                    WorldGen.EmptyLiquid(i, j); //Drain gang
                }

            //Carve out the inside
            for (int i = InnerChamberRect.X; i < InnerChamberRect.X + InnerChamberRect.Width; i++)
                for (int j = InnerChamberRect.Y; j < InnerChamberRect.Y + InnerChamberRect.Height; j++)
                {
                    WorldGen.KillTile(i, j);
                }
        }

        /// <summary>
        /// Creates the ceiling within the chamber, with the craggy stalactites on the ceiling, and the general dome shape <br/>
        /// The ceiling is lined with sandstone to prevent sandfall
        /// </summary>
        public static void GenerateChamberCeiling()
        {
            int centerX = InnerChamberRect.X + InnerChamberRect.Width / 2;
            int ceilingY = InnerChamberRect.Y;
            int halfRoomLength = (int)InnerChamberRect.Width / 2;

            //Make the ceiling look natural
            int ceilingDepth = WorldGen.genRand.Next((int)(InnerChamberRect.Height * 0.5f), (int)(InnerChamberRect.Height * 0.8f));
            int ceilingOffset = 1;
            bool stalactite = false;
            int stalactiteStart = 0;
            int stalactiteEnd = 0;


            for (int i = centerX - halfRoomLength; i < centerX + halfRoomLength; i++)
            {
                //The ceiling has some general offsets once in a while
                if (i % 8 == 3)
                    ceilingOffset += WorldGen.genRand.Next(-1, 2);
                //If the ceiling offset goes below zero, just reduce the depth of the ceiling to avoid weird stuff
                if (ceilingOffset < 0)
                {
                    ceilingDepth--;
                    ceilingOffset = 0;
                }

                //Curve strongly near the edges of the room
                if (i < centerX - 3 * halfRoomLength / 4 && ceilingDepth > 4)
                {
                    ceilingDepth -= WorldGen.genRand.Next(0, 3);
                }
                if (i >= centerX + 4 * halfRoomLength / 5 && ceilingDepth < InnerChamberRect.Height / 2)
                {
                    ceilingDepth += WorldGen.genRand.Next(0, 3);
                }
                if (i >= centerX + 7 * halfRoomLength / 8)
                {
                    ceilingDepth += WorldGen.genRand.Next(3, 3);
                }


                if (!stalactite)
                {
                    //Random change in height
                    ceilingDepth += WorldGen.genRand.Next(-1, 2);

                    //Biased to not be too low
                    if (ceilingDepth > 5)
                        ceilingDepth -= WorldGen.genRand.Next(0, 2);

                    //Random chance for a stalactite
                    if (WorldGen.genRand.NextBool(9))
                    {
                        stalactite = true;
                        stalactiteStart = i;
                        stalactiteEnd = i + WorldGen.genRand.Next(3, 6);
                        if (stalactiteEnd > centerX + halfRoomLength - 1)
                            stalactiteEnd = centerX + halfRoomLength - 1;
                    }
                }

                //Stalactite generation
                else
                {
                    int stalactiteWidth = stalactiteEnd - stalactiteStart;
                    float stalactiteProgress = (i - stalactiteEnd) / (float)stalactiteWidth;
                    int deep = (5 - stalactiteWidth);

                    if (stalactiteProgress < 0.5f)
                        ceilingDepth += WorldGen.genRand.Next(deep, deep + 2);
                    else
                        ceilingDepth -= WorldGen.genRand.Next(deep, deep + 3);

                    if (i >= stalactiteEnd)
                        stalactite = false;
                }

                //Make the ceiling depth not be too low unless youre at the start of the room, obviously
                if (ceilingDepth > InnerChamberRect.Height / 3f && i > centerX - halfRoomLength * 2f)
                    ceilingDepth--;

                //Absolutely prevent it from going below 20% of the rooms height
                if (ceilingDepth > InnerChamberRect.Height * 0.8)
                    ceilingDepth = (int)(InnerChamberRect.Height * 0.8);

                //Fill in the layer
                for (int j = ceilingY; j < ceilingY + ceilingDepth + ceilingOffset; j++)
                {
                    int tileType = j > (ceilingY + ceilingDepth + ceilingOffset) - 5 ? TileID.Sandstone : TileID.HardenedSand;
                    WorldGen.PlaceTile(i, j, tileType, true, true);
                }
            }
        }

        /// <summary>
        /// Fills in the cave walls and has a random chance of lerping the tiles towards matching "the perfect ceiling curve", where the top is flat and the edges are curved <br/>
        /// When generated, it has a one in two chance of being smoothed this way
        /// </summary>
        /// <param name="smoothCeiling"></param>
        public static void FillChamberWallsAndSmoothenCeiling(bool smoothCeiling = false)
        {
            int centerX = InnerChamberRect.X + InnerChamberRect.Width / 2;
            int halfRoomLength = (int)InnerChamberRect.Width / 2;

            //Fill gaps in the wall and also smooth the curve of the cave sometimes
            for (int i = centerX - halfRoomLength; i < centerX + halfRoomLength; i++)
            {
                //Calculate the perfect height of the chamber at the X position (curved at the edges, flat at the top)
                int perfectHeight = InnerChamberRect.Height;
                if (i < centerX - halfRoomLength * 0.4f)
                    perfectHeight = (int)(perfectHeight * (0.5f + 0.5 * Math.Sin(Math.Acos((Math.Abs(i - centerX) - halfRoomLength * 0.4f) / (halfRoomLength * 0.6f)))));
                else if (i > centerX + halfRoomLength * 0.4f)
                    perfectHeight = (int)(perfectHeight * (0.5f + 0.5 * Math.Sin(Math.Acos(((i - centerX) - halfRoomLength * 0.4f) / (halfRoomLength * 0.6f)))));

                int ceilingHeight = 1;
                bool insideCeiling = false;

                for (int j = InnerChamberRect.Y + InnerChamberRect.Height; j > InnerChamberRect.Y - 3; j--)
                {
                    //Fill in the walls with sandstone (overrides the ugly blots of hardened sand walls that are mixed in by default
                    Main.tile[i, j].WallType = WallID.Sandstone;
                    if (frameWalls)
                        WorldGen.SquareWallFrame(i, j);

                    //At this point were just continuing up for the walls
                    if (j < InnerChamberRect.Y)
                        continue;

                    if (!Main.tile[i, j].HasTile && !insideCeiling)
                        ceilingHeight++;

                    else if (j <= InnerChamberRect.Y + InnerChamberRect.Height - 2)
                    {
                        //Smoothens the ceiling if needed when the ceiling gets reached
                        if (!insideCeiling && smoothCeiling)
                        {
                            //Droop the ceiling down if its too tall
                            if (ceilingHeight > perfectHeight)
                            {
                                WorldGen.PlaceTile(i, j + 1, TileID.Sandstone, true, true);
                                if (WorldGen.genRand.NextBool(2) && ceilingHeight - 1 > perfectHeight)
                                    WorldGen.PlaceTile(i, j + 2, TileID.Sandstone, true, true);
                            }

                            //Crop the ceiling if its too high
                            else if (ceilingHeight < perfectHeight)
                            {
                                WorldGen.KillTile(i, j, false, false, true);
                            }
                        }

                        //If we're further in the ceiling than 7 tiles, turn the stalactite formations into a mosaic of hardened sand and sand so it looks more natural
                        if (Math.Abs((InnerChamberRect.Y + InnerChamberRect.Height - ceilingHeight) - j) > 7)
                        {
                            if (Main.tile[i, j].TileType == TileID.HardenedSand && WorldGen.genRand.NextBool(4))
                            {
                                Tile replacedTile = Main.tile[i, j];
                                replacedTile.TileType = TileID.Sand;
                            }
                        }

                        insideCeiling = true;
                    }
                }
            }
        }

        //(Florida style nautilus cave)
        /// <summary>
        /// Adds a slight slant onto the edges of the chamber's floor <br/>
        /// Tries to align itself towards the nearest full tile at an increased probability chance when as a half slope to maximize the flat ground ontop of which props can be placed
        /// </summary>
        public static void SlantChamberFloor()
        {
            int floorY = (int)InnerChamberRect.Y + InnerChamberRect.Height - 1;
            bool elevatedFloor = true;
            bool halfTileFloor = false;

            int chanceToStopBeingHalfTile = 3;
            int chanceForFullTileMovement = 7;

            float flatInnerFloorPercent = 0.67f; //used to be 0.5, but we have to extend it to fit in our new fancy deco tiles
            int tilesSpentAsHalfBlock = 0;

            for (int i = 0; i < InnerChamberRect.Width; i++)
            {
                int x = InnerChamberRect.X + i;

                //Adjust the floor by making the edges start 1 tile upwards, then turn into half tiles, then nothing, then back again
                if (elevatedFloor)
                {
                    WorldGen.PlaceTile(x, floorY, TileID.HardenedSand, true, true);

                    if (halfTileFloor)
                    {
                        Tile tileToHalve = Main.tile[x, floorY]; //Me when cant just Main.tile[x, y].IsHalfBlock = true;
                        tileToHalve.IsHalfBlock = true;
                        tilesSpentAsHalfBlock++;
                    }
                }

                //Right side of the chamber
                if (i > InnerChamberRect.Width * (0.5f + flatInnerFloorPercent / 2f))
                {
                    //1/6 chance to slope up, and then 1/4 chance when sloped up to become a full tile up
                    int upSlantChance = !elevatedFloor ? chanceForFullTileMovement : chanceToStopBeingHalfTile;
                    if (halfTileFloor && tilesSpentAsHalfBlock >= 4)
                        upSlantChance = 1;

                    if (WorldGen.genRand.NextBool(upSlantChance))
                    {
                        //If not elevated at all, slant up with half tiles
                        if (!elevatedFloor)
                        {
                            elevatedFloor = true;
                            halfTileFloor = true;
                            tilesSpentAsHalfBlock = 0;
                        }

                        //If already slanted with half tiles, slant into full tiles
                        else if (halfTileFloor)
                            halfTileFloor = false;
                    }
                }

                //Inner 60% of the chamber floor is flat
                else if (i > InnerChamberRect.Width * (flatInnerFloorPercent / 2f))
                    elevatedFloor = false;

                //Left side
                else
                {
                    //1/6 chance to slope down
                    int downSlantChance = !halfTileFloor ? chanceForFullTileMovement : chanceToStopBeingHalfTile;
                    if (halfTileFloor && tilesSpentAsHalfBlock >= 4)
                        downSlantChance = 1;

                    if (elevatedFloor && WorldGen.genRand.NextBool(downSlantChance))
                    {
                        //If slanted up as a full tile, become a half tile
                        if (!halfTileFloor)
                        {
                            halfTileFloor = true;
                            tilesSpentAsHalfBlock = 0;
                        }
                        //Else entuirely unslant up
                        else
                            elevatedFloor = false;
                    }
                }
            }
        }

        /// <summary>
        /// Puts on a nice dome shape of sand and sandstone ontop of the chamber to smooth out the perfectly straight top side of the box
        /// </summary>
        public static void RoundChamberCeiling()
        {
            int centerX = InnerChamberRect.X + InnerChamberRect.Width / 2;

            //Curve up the above section
            for (int i = NautilusChamberRect.X; i < NautilusChamberRect.X + NautilusChamberRect.Width; i++)
            {
                int dist = Math.Abs(i - centerX);
                float distanceFromCenter = dist / (NautilusChamberRect.Width * 0.5f);
                int topHeight = 1 + (int)(Math.Pow(1 - distanceFromCenter, 0.5f) * 5f);

                for (int j = 0; j <= topHeight; j++)
                {
                    Tile tileAhead = Main.tile[i, NautilusChamberRect.Y - j - 1];

                    int tileType = WorldGen.genRand.NextBool(3) ? TileID.Sand : TileID.HardenedSand;
                    if (topHeight - j <= 2 && !(tileAhead.TileType == TileID.Sand || tileAhead.TileType == TileID.HardenedSand))
                        tileType = TileID.Sandstone;

                    WorldGen.PlaceTile(i, NautilusChamberRect.Y - j, tileType, true, true);

                    if (tileAhead.IsTileSolid())
                        tileAhead.Slope = SlopeType.Solid;
                }
            }
        }

        /// <summary>
        /// Adds an inverse parabola on the bottom of the chamber, with code that assures it can propely meld with existing terrain
        /// </summary>
        public static void JandTheChamberFloor()
        {
            int centerX = InnerChamberRect.X + InnerChamberRect.Width / 2;

            //Makes the section underneath the chamber less straight
            for (int i = NautilusChamberRect.X; i < NautilusChamberRect.X + NautilusChamberRect.Width; i++)
            {
                int dist = Math.Abs(i - centerX);
                float distanceFromCenter = dist / (NautilusChamberRect.Width * 0.5f);
                int bottomDistance = 1 + (int)(Math.Pow(distanceFromCenter, 1.5f) * 6f);
                int extraDistance = 0;

                for (int j = 0; j <= bottomDistance + extraDistance; j++)
                {
                    Tile tileAhead = Main.tile[i, NautilusChamberRect.Y + NautilusChamberRect.Height + j + 1];

                    int tileType = WorldGen.genRand.NextBool(3) ? TileID.Sand : TileID.HardenedSand;
                    if (bottomDistance - j <= 2 && !(tileAhead.TileType == TileID.Sand || tileAhead.TileType == TileID.HardenedSand))
                        tileType = TileID.Sandstone;

                    WorldGen.PlaceTile(i, NautilusChamberRect.Y + NautilusChamberRect.Height + j, tileType, true, true);
                    if (tileAhead.IsTileSolid())
                    {
                        tileAhead.IsHalfBlock = false;
                        tileAhead.Slope = SlopeType.Solid;
                        break;
                    }

                    //If we reach the end, also check up for more in front to "round up" holes
                    if (j == bottomDistance - 1)
                    {
                        for (int jplus = 1; jplus < 10; jplus++)
                        {
                            Tile tileAheader = Main.tile[i, NautilusChamberRect.Y + NautilusChamberRect.Height + j + jplus];

                            if (tileAheader.TileType == TileID.Sandstone)
                            {
                                extraDistance = 10 - jplus;
                                break;
                            }

                        }
                    }
                }
            }
        }


        /// <summary>
        /// Replaces the floor of the chamber with sandstone for the most part, and puts own the unbreakbable sandstone bricks below nautilus's section
        /// </summary>
        public static void ReplaceChamberFloorTiles()
        {
            int centerX = InnerChamberRect.X + InnerChamberRect.Width / 2;
            int halfRoomLength = (int)InnerChamberRect.Width / 2;
            int floorCrustDepth = 2;

            //replace floor with sandstone
            for (int i = centerX - halfRoomLength; i < centerX + halfRoomLength; i++)
            {
                //Every 4 tiles the floor has a random chance to shift in height
                if (i % 4 == 0)
                    floorCrustDepth += WorldGen.genRand.Next(0, 2) * ((float)-(i - centerX)).NonZeroSign();
                floorCrustDepth = Math.Clamp(floorCrustDepth, 2, 4); //Not further than 4 tiles down or thinner than 1 tile


                for (int j = InnerChamberRect.Y + InnerChamberRect.Height - 3; j < InnerChamberRect.Y + InnerChamberRect.Height + 2; j++)
                {
                    //Swap out the hardened sand floor for sandstone
                    if (Main.tile[i, j].HasUnactuatedTile && Main.tile[i, j].TileType == TileID.HardenedSand)
                    {
                        for (int j2 = 0; j2 < floorCrustDepth; j2++)
                        {
                            Tile floorTile = Main.tile[i, j + j2];
                            floorTile.TileType = TileID.Sandstone;
                        }
                        break;
                    }
                }
            }

            int sandstoneLeftX = centerX - WorldGen.genRand.Next(14, 17);
            int sandstoneRightX = centerX + WorldGen.genRand.Next(14, 17);

            int sandstoneLeftX2 = sandstoneLeftX;
            int sandstoneRightX2 = sandstoneRightX;
            int sandstoneLeftX3 = sandstoneLeftX;
            int sandstoneRightX3 = sandstoneRightX;

            int floorBrickType = ModContent.TileType<UnbreakableSandstoneBrickFlooring>();

            for (int j = InnerChamberRect.Y + InnerChamberRect.Height - 1; j < InnerChamberRect.Y + InnerChamberRect.Height + 3; j++)
            {
                for (int i = sandstoneLeftX; i < sandstoneRightX; i++)
                {
                    //Used to "split" the floor into two 
                    if ((i < sandstoneLeftX2 || i >= sandstoneRightX2) && (i < sandstoneLeftX3 || i >= sandstoneRightX3))
                        continue;

                    //Swap out the floor with unbreakable bricks
                    if (Main.tile[i, j].HasUnactuatedTile && Main.tileSolid[Main.tile[i, j].TileType])
                    {
                        Tile floorTile = Main.tile[i, j];
                        floorTile.TileType = (ushort)floorBrickType;
                        Tile floorTileBelow = Main.tile[i, j + 1];
                        floorTileBelow.TileType = TileID.Sandstone;
                    }
                }

                //Shrink one side
                if (j == InnerChamberRect.Y + InnerChamberRect.Height)
                {
                    if (WorldGen.genRand.NextBool())
                        sandstoneLeftX += WorldGen.genRand.Next(1, 5);
                    else
                        sandstoneRightX -= WorldGen.genRand.Next(1, 5);
                }
                //Split into 2
                else if (j == InnerChamberRect.Y + InnerChamberRect.Height + 1)
                {
                    sandstoneLeftX2 = sandstoneLeftX + WorldGen.genRand.Next(2, 8);
                    sandstoneRightX2 = sandstoneLeftX2 + WorldGen.genRand.Next(5, 12);

                    sandstoneRightX3 = sandstoneRightX - WorldGen.genRand.Next(2, 8);
                    sandstoneLeftX3 = sandstoneRightX3 - WorldGen.genRand.Next(5, 12);
                }
                //Shrink the 2 split sides
                else if (j == InnerChamberRect.Y + InnerChamberRect.Height + 2)
                {
                    sandstoneLeftX2 += WorldGen.genRand.Next(0, 2);
                    sandstoneRightX2 -= WorldGen.genRand.Next(0, 3);

                    sandstoneRightX3 -= WorldGen.genRand.Next(0, 2);
                    sandstoneLeftX3 += WorldGen.genRand.Next(0, 3);
                }

            }
        }

        /// <summary>
        /// Creates the hole in the wall that lets the player see the parallax background
        /// </summary>
        public static void BreakWallOpen()
        {
            int centerX = InnerChamberRect.X + InnerChamberRect.Width / 2;
            int floorY = InnerChamberRect.Y + InnerChamberRect.Height;
            int halfWallOpenLenght = WorldGen.genRand.Next(24, 29);
            int sandstoneCurveDownSize = WorldGen.genRand.Next(17, 20);

            //Make the ceiling look natural
            ushort rockyWallType = RockySandstoneWallItem.wallTypes[0];
            int rockyWallStartWidth = WorldGen.genRand.Next(18, 21);

            bool sandTite = false;
            int sandtiteStart = 0;
            int sandTiteEnd = 0;
            int sandTiteHeight = 0;

            bool rockTite = false;
            int rocktiteStart = 0;
            int rockTiteEnd = 0;
            int rockTiteHeight = 0;

            int jagCooldown = 0;

            bool sharpDown = WorldGen.genRand.NextBool(3);
            int sharpDownSide = WorldGen.genRand.NextBool() ? -1 : 1;
            int sharpDownHeightLoss = WorldGen.genRand.Next(3, 6);
            int sharpDownStart = sharpDownSide == -1 ? centerX - halfWallOpenLenght : centerX + WorldGen.genRand.Next(4, 12);
            int sharpDownEnd = sharpDownSide == 1 ? centerX + halfWallOpenLenght : centerX - WorldGen.genRand.Next(4, 12);
            int sharpDownValue = sharpDownSide == -1 ? sharpDownHeightLoss : 0;

            for (int i = centerX - halfWallOpenLenght; i <= centerX + halfWallOpenLenght; i++)
            {
                int distFromCenter = Math.Abs(i - centerX);
                int perfectHoleHeight = (int)(11 + 8 * MathF.Sin(Utils.GetLerpValue(20, 4, distFromCenter, true) * MathHelper.PiOver2));
                perfectHoleHeight -= (int)(perfectHoleHeight * MathF.Sin(Utils.GetLerpValue(sandstoneCurveDownSize, halfWallOpenLenght, distFromCenter, true) * MathHelper.PiOver2));

                int perfectRockHeight = (int)(WorldGen.genRand.Next(8, 11) * MathF.Sin(Utils.GetLerpValue(rockyWallStartWidth - 2, 4, distFromCenter, true) * MathHelper.PiOver2));
                perfectRockHeight += (int)(MathF.Sin(Utils.GetLerpValue(rockyWallStartWidth, 16, distFromCenter, true) * MathHelper.PiOver2) * 6);

                //In a "sharp down" wall lowering 
                if (sharpDown && i > sharpDownStart && i < sharpDownEnd)
                {
                    rockTite = false;
                    if (sharpDownValue < sharpDownHeightLoss && WorldGen.genRand.NextBool())
                        sharpDownValue += WorldGen.genRand.Next(1, 3);
                }
                else if (sharpDownValue > 0 && WorldGen.genRand.NextBool())
                    sharpDownValue -= WorldGen.genRand.Next(2, 3);

                if (sharpDownValue < 0)
                    sharpDownValue = 0;

                perfectRockHeight -= sharpDownValue;
                perfectHoleHeight -= Math.Max(0, (sharpDownValue - 2));

                /*
                //The ceiling has some general offsets once in a while
                if (i % 8 == 3)
                    ceilingOffset += WorldGen.genRand.Next(-1, 2);
                //If the ceiling offset goes below zero, just reduce the depth of the ceiling to avoid weird stuff
                if (ceilingOffset < 0)
                {
                    sandstoneHoleHeight--;
                    ceilingOffset = 0;
                }
                */

                int sandstoneHoleHeight = perfectHoleHeight;
                if (!sandTite)
                {
                    //Random chance for a stalactite
                    if (WorldGen.genRand.NextBool(7))
                    {
                        sandTite = true;
                        sandtiteStart = i;
                        sandTiteHeight = WorldGen.genRand.Next(1, 5);
                        sandTiteEnd = i + WorldGen.genRand.Next(3, 7);
                        if (sandTiteEnd > centerX + halfWallOpenLenght - 1)
                            sandTiteEnd = centerX + halfWallOpenLenght - 1;

                        //Failsafe, if at the edge of the wall and we try a 1-wide sandtite , fail otherwise itll try to divide by zro
                        if (sandTiteEnd - sandtiteStart <= 0)
                            sandTite = false;
                    }
                }
                //Stalactite generation
                else
                {
                    //Chance to match th perfect hole
                    int stalactiteWidth = sandTiteEnd - sandtiteStart;
                    float stalactiteProgress = (sandTiteEnd - i) / (float)stalactiteWidth;

                    float spikeCurvature = MathF.Sin(stalactiteProgress * MathHelper.Pi);
                    if (sandTiteHeight > 1)
                        spikeCurvature = (float)Math.Pow(spikeCurvature, 2f);

                    sandstoneHoleHeight -= (int)(spikeCurvature * sandTiteHeight * 1.2f);
                    if (i >= sandTiteEnd)
                        sandTite = false;
                }

                int rockHoleHeight = perfectRockHeight;
                if (jagCooldown > 0)
                    jagCooldown--;

                //Jag down
                else if (WorldGen.genRand.NextBool(3))
                {
                    rockHoleHeight -= 1;
                    jagCooldown = WorldGen.genRand.Next(1, 3);
                }

                if (!rockTite)
                {
                    //Random chance for a stalactite
                    if (WorldGen.genRand.NextBool(7))
                    {
                        rockTite = true;
                        rocktiteStart = i;
                        rockTiteHeight = WorldGen.genRand.Next(2, 3);
                        rockTiteEnd = i + WorldGen.genRand.Next(3, 6);
                        if (rockTiteEnd > centerX + halfWallOpenLenght - 1)
                            rockTiteEnd = centerX + halfWallOpenLenght - 1;
                    }
                }
                //Stalactite generation
                else
                {
                    //Chance to match th perfect hole
                    int stalactiteWidth = rockTiteEnd - rocktiteStart;
                    float stalactiteProgress = (rockTiteEnd - i) / (float)stalactiteWidth;

                    float spikeCurvature = MathF.Sin(stalactiteProgress * MathHelper.Pi);
                    spikeCurvature = (float)Math.Pow(spikeCurvature, 3f);

                    rockHoleHeight -= (int)(spikeCurvature * rockTiteHeight);
                    if (i >= rockTiteEnd)
                        rockTite = false;
                }

                //Open up the sandstone
                int usedHoleHeight = sandstoneHoleHeight == 1 ? 0 : sandstoneHoleHeight;
                for (int j = floorY; j > floorY - usedHoleHeight; j--)
                {
                    ushort wallType = j < floorY - rockHoleHeight ? rockyWallType : WallID.None;
                    Main.tile[i, j].WallType = wallType;
                    if (frameWalls)
                        WorldGen.SquareWallFrame(i, j);
                }
            }

            //Dig a hole on a side of the cave
            bool tacticalChumbIncoming = !WorldGen.genRand.NextBool(3);
            if (tacticalChumbIncoming)
            {
                int tacticalChumbX = WorldGen.genRand.Next(14, 17);
                if (sharpDown)
                    tacticalChumbX *= -sharpDownSide;
                else if (WorldGen.genRand.NextBool())
                    tacticalChumbX *= -1;

                int tacticalChumbY = floorY - WorldGen.genRand.Next(10, 13);
                float tacticalChumbSquish = WorldGen.genRand.NextFloat(1.3f, 1.7f);

                if (tacticalChumbY > 0)
                {
                    for (int x = tacticalChumbX + centerX - 5; x <= tacticalChumbX + centerX + 5; x++)
                    {
                        for (int y = tacticalChumbY - 5; y <= tacticalChumbY + 5; y++)
                        {
                            Vector2 toCenterOfBlast = new Vector2(x * 16f + 8f, y * 16f + 8f) - new Vector2((tacticalChumbX + centerX) * 16f + 8f, tacticalChumbY * 16f + 8f);
                            toCenterOfBlast.Y *= tacticalChumbSquish;
                            float distance = toCenterOfBlast.Length() / 16f;

                            if (distance < 5.5f && Main.tile[x, y].WallType == WallID.Sandstone)
                            {
                                Main.tile[x, y].WallType = rockyWallType;
                                if (frameWalls)
                                    WorldGen.SquareWallFrame(x, y);
                            }
                        }
                    }
                }
            }

            //No rock tooth
            if (!WorldGen.genRand.NextBool(6))
            {
                int rockToothSide = WorldGen.genRand.NextBool() ? -1 : 1;
                if (sharpDown)
                    rockToothSide = -sharpDownSide;

                int rockToothCenter = centerX + (rockyWallStartWidth - WorldGen.genRand.Next(5, 7)) * rockToothSide;

                int rockToothLeft = rockToothSide == -1 ? centerX - halfWallOpenLenght : rockToothCenter - 4;
                int rockToothRight = rockToothSide == 1 ? centerX + halfWallOpenLenght : rockToothCenter + 4;
                int rockToothMaxHeight = WorldGen.genRand.Next(5, 9);

                for (int i = rockToothLeft; i <= rockToothRight; i++)
                {
                    int distFromCenter = Math.Abs(i - rockToothCenter);
                    bool innerSide = Math.Sign(rockToothCenter - i) == rockToothSide;

                    int toothHeight = rockToothMaxHeight;
                    if (innerSide)
                        toothHeight -= (int)(rockToothMaxHeight * MathF.Pow(Utils.GetLerpValue(0, 4, distFromCenter), 0.5f));
                    else
                        toothHeight -= (int)(rockToothMaxHeight * 0.6f * MathF.Sin(Utils.GetLerpValue(0, 4, distFromCenter) * 3f));


                    //Add the little snag tooth for the sandstone
                    for (int j = floorY; j > floorY - toothHeight; j--)
                    {
                        if (Main.tile[i, j].WallType == 0)
                        {
                            Main.tile[i, j].WallType = rockyWallType;
                            if (frameWalls)
                                WorldGen.SquareWallFrame(i, j);
                        }
                    }
                }
            }

            //Stripes
            int stripeHeight = WorldGen.genRand.Next(4, 12);
            bool shortStripe = WorldGen.genRand.NextBool(3);
            int shortStripeCooldown = 0;
            float stripeSlope = WorldGen.genRand.NextFloat(11, 19);

            bool didFossilStripe = false;

            while (stripeHeight < InnerChamberRect.Height - 5)
            {
                float exactStripeHeight = InnerChamberRect.Y + stripeHeight + WorldGen.genRand.NextFloat();
                float stripeThickness = WorldGen.genRand.NextFloat(0.4f, 1f);
                ushort stripeType = WallID.HardenedSand;
                if (!didFossilStripe && stripeHeight >  10 && WorldGen.genRand.NextBool(5))
                {
                    stripeType = WorldGen.genRand.NextBool() ? WallID.DesertFossilEcho : rockyWallType;
                    didFossilStripe = true;
                }

                //Make da stripe
                for (int i = InnerChamberRect.X - 3; i < InnerChamberRect.X + InnerChamberRect.Width + 3; i++)
                {
                    //Replace sandstone with hardened sand
                    if (Main.tile[i, (int)exactStripeHeight].WallType == WallID.Sandstone)
                        Main.tile[i, (int)exactStripeHeight].WallType = stripeType;

                    float distanceToLowerTile = MathF.Abs(((int)exactStripeHeight + 1) - exactStripeHeight) + WorldGen.genRand.NextFloat(-0.2f, 0.2f);
                    if (distanceToLowerTile < stripeThickness && Main.tile[i, (int)exactStripeHeight + 1].WallType == WallID.Sandstone)
                        Main.tile[i, (int)exactStripeHeight + 1].WallType = stripeType;

                    if (frameWalls)
                        WorldGen.SquareWallFrame(i, (int)exactStripeHeight);

                    //Move slope down
                    exactStripeHeight += 1 / (stripeSlope + WorldGen.genRand.NextFloat(0f, 1.1f));
                }
               

                if (shortStripe)
                {
                    stripeHeight += WorldGen.genRand.Next(3, 6);
                    shortStripe = false;
                    shortStripeCooldown = 1;
                }
                else
                    stripeHeight += WorldGen.genRand.Next(9, 18);

                if (shortStripeCooldown <= 0 && WorldGen.genRand.NextBool(3))
                    shortStripe = true;
                shortStripeCooldown--;
            }

        }
        #endregion

        #region Artificial decoration
        /// <summary>
        /// Places down one or two pairs of pillars
        /// </summary>
        public static void AddChamberPillars()
        {
            //Random chance to get 2 pillars instead of 4
            int pillarPairCount = 1;
            if (!WorldGen.genRand.NextBool(4))
                pillarPairCount++;

            int chamberCenterX = (int)(InnerChamberRect.X + InnerChamberRect.Width * 0.5f);
            int sandstonePillarType = ModContent.TileType<SandstonePillarTile>();
            int sandBrickPillarType = ModContent.TileType<DesertBrickPillarTile>();

            //Pick a random style for the pillar
            int sandstoneBaseColor = WorldGen.genRand.Next(10);
            if (sandstoneBaseColor < 4)
                sandstoneBaseColor = PaintID.RedPaint; //Crimstone paint, looks very good, more common
            else if (sandstoneBaseColor < 7)
                sandstoneBaseColor = PaintID.PinkPaint; //Deeper reddish beige, meshes well, looks fine
            else if (sandstoneBaseColor < 9)
                sandstoneBaseColor = PaintID.GrayPaint; //Light beige, doesn't stand out but looks neat
            else
                sandstoneBaseColor = PaintID.PurplePaint; //Ebonstone. Kinda unique, but has its style

            //1 in 5 chance to have no paint at all
            if (WorldGen.genRand.NextBool(5))
                sandstoneBaseColor = PaintID.None;

            //Trims have an equal chance
            int sandstoneTrimColor = WorldGen.genRand.NextBool() ? (WorldGen.genRand.NextBool() ? PaintID.LimePaint : PaintID.TealPaint) : (WorldGen.genRand.NextBool() ? PaintID.GreenPaint : PaintID.VioletPaint);
            int sandstoneBasePaintHeight = WorldGen.genRand.Next(7, 9);

            SandstonePillarTile sandstonePillarModTile = ModContent.GetInstance<SandstonePillarTile>();

            //place down pillars
            for (int side = -1; side <= 1; side += 2)
            {
                int distanceToCenter = WorldGen.genRand.Next(14, 19);
                int pillarX = chamberCenterX + distanceToCenter * side;
                int floorY = (int)InnerChamberRect.Y + InnerChamberRect.Height;

                int pillarHeight = WorldGen.genRand.Next(14, 20);
                int sandstoneCoverHeight = WorldGen.genRand.Next(5, 7);

                for (int i = 0; i < pillarPairCount; i++)
                {
                    //Place the pillar from top to bottom
                    for (int j = 0; j < pillarHeight; j++)
                    {
                        int placeY = floorY - 1 - j;

                        int tileType = j < sandstoneCoverHeight ? sandBrickPillarType : sandstonePillarType;
                        bool canPlace = TileObject.CanPlace(pillarX, placeY, tileType, 0, 0, out TileObject pillarData);

                        if (!canPlace)
                        {
                            if (j >= 15)
                                ConnectPillarToCeiling(pillarX, placeY);
                            if (j == 0)
                                continue;
                            break;
                        }

                        bool placed = TileObject.Place(pillarData);
                        WorldGen.SquareTileFrame(pillarX, placeY);
                        if (!placed)
                            break;

                        //The sand brick pillar is big chilling and we don't need to tweak it
                        if (tileType == sandBrickPillarType)
                            continue;

                        //Place down a brazier
                        if (j == 10)
                            Main.tile[pillarX, placeY + 1].TileFrameX += 1;
                        

                        //Paint it
                        for (int p = -1; p < 2; p++)
                        {
                            Tile paintedTile = Main.tile[pillarX + p, placeY];
                            if (j < sandstoneBasePaintHeight)
                                paintedTile.TileColor = (byte)sandstoneBaseColor;
                            else if (j == sandstoneBasePaintHeight)
                                paintedTile.TileColor = (byte)sandstoneTrimColor;
                        }
                    }

                    //Go back over it to place the cracks
                    int crackCount = WorldGen.genRand.Next(1, 3);
                    int crackHeight = floorY - pillarHeight + WorldGen.genRand.Next(1, 6);

                    for (int crack = 0; crack < crackCount; crack++)
                    {
                        if (Main.tile[pillarX, crackHeight].TileType != sandstonePillarType)
                            break;

                        //Place cracks
                        sandstonePillarModTile.PlaceCracksOnTile(pillarX, crackHeight, pillarX - 1);

                        crackHeight += WorldGen.genRand.Next(2, 8);
                    }

                    if (pillarPairCount == 1)
                        break;

                    //Extra pillars are stupid tiny
                    pillarHeight = WorldGen.genRand.Next(3, 8);
                    sandstoneCoverHeight = WorldGen.genRand.Next(3, 4);

                    //Move sideways
                    int sideDistance = WorldGen.genRand.Next(6, 13);
                    int lastAllowedX = -1;
                    int originalX = pillarX;

                    for (int sp = 0; sp < sideDistance; sp++)
                    {
                        pillarX += side;
                        Tile t = Main.tile[pillarX, floorY - 1];

                        //Too close to matter
                        if (sp < 2)
                            continue;

                        if (t.HasTile)
                        {
                            //Move up
                            if (!t.IsHalfBlock)
                                floorY--;
                        }
                        //Keep track of allowed positions
                        else if (TileObject.CanPlace(pillarX - 1 * side, floorY - 1, sandBrickPillarType, 0, 0, out _))
                            lastAllowedX = pillarX - 1 * side;
                    }

                    //We couldn't find a valid X position!
                    if (lastAllowedX == -1 || Math.Abs(lastAllowedX - originalX) < 3)
                        break;

                    pillarX = lastAllowedX;
                }

                //Offset for the right side of pillars
                chamberCenterX++;
            }
        }

        /// <summary>
        /// If a pillar is touching the cieling with one bit, makes it so the pillar fully connects instead
        /// </summary>
        public static void ConnectPillarToCeiling(int x, int y)
        {
            for (int i = x - 3; i <= x + 3; i++ )
            {
                int startHeight = y;
                startHeight -= (int)(Utils.GetLerpValue(1, 3, (int)MathF.Abs(i - x))) * 5;

                for (int j = startHeight; j > y - 9; y--)
                {
                    Tile t = Main.tile[i, j];
                    if (t.HasTile)
                        break;
                    t.HasTile = true;
                    t.TileType = TileID.Sandstone;
                }
            }
        }

        /// <summary>
        /// Places down the scourgekiller painting alongside the other hieroglyphics painting on the sides of the chamber
        /// </summary>
        public static void AddChamberSidePaintings()
        {
            int paintingY = (int)(InnerChamberRect.Y + InnerChamberRect.Height - 11);
            bool placedScourgekiller = false;
            int scourgeKillerType = ModContent.TileType<ScourgekillerPaintingTile>();
            int otherDecoType = ModContent.TileType<ShatteredTabletPaintingTile>();

            int startingSide = WorldGen.genRand.NextBool() ? -1 : 1;

            for (int p = 0; p < 2; p++)
            {
                bool successfulPlaceAttempt = false;
                for (int i = 27; i < 30; i++)
                {
                    int placeX = (int)(InnerChamberRect.X + InnerChamberRect.Width * 0.5 - i * startingSide);
                    int placedType = placedScourgekiller ? otherDecoType : scourgeKillerType;

                    if (TileObject.CanPlace(placeX, paintingY, placedType, 0, 0, out _))
                    {
                        WorldGen.PlaceObject(placeX, paintingY, placedType, true);
                        placedScourgekiller = true;
                        successfulPlaceAttempt = true;
                        break;
                    }
                }
                if (!successfulPlaceAttempt)
                {
                    //Do it on the other side
                    for (int i = 26; i > 24; i--)
                    {
                        int placeX = (int)(InnerChamberRect.X + InnerChamberRect.Width * 0.5 - i * startingSide);
                        int placedType = placedScourgekiller ? otherDecoType : scourgeKillerType;

                        if (TileObject.CanPlace(placeX, paintingY, placedType, 0, 0, out _))
                        {
                            WorldGen.PlaceObject(placeX, paintingY, placedType, true);
                            placedScourgekiller = true;
                            break;
                        }
                    }
                }

                startingSide *= -1;
            }
        }

        /// <summary>
        /// Adds the tent, broken dummy, tripod torches and crates all over the chamber floor
        /// </summary>
        public static void AddPropsToChamber()
        {
            int tentType = ModContent.TileType<SandyTentTile>();
            int cratesType = ModContent.TileType<DustyCrates>();
            int dummyTile = ModContent.TileType<BrokenDummy>();
            int tripodTile = ModContent.TileType<TripodTorchTile>();

            int centerX = InnerChamberRect.X + (int)InnerChamberRect.Width / 2;
            int floorY = InnerChamberRect.Y + InnerChamberRect.Height - 1; ;

            //TRY to place the tent which is the most important component

            #region Placing the tent (IMPORTANT)

            bool placedTent = false;
            List<Point> validTentPositions = new List<Point>();
            //First, the central area of the chamber. Reverse order so it stars closer to nautie
            for (int i = centerX + 16; i >= centerX - 16; i--)
            {
                //Not too close to the center please
                if (Math.Abs(centerX - i) < 10)
                    continue;

                int dir = Math.Sign(i - centerX);
                if (TileObject.CanPlace(i, floorY, tentType, 0, dir, out _))
                    validTentPositions.Add(new Point(i, floorY));
            }

            if (validTentPositions.Count > 0)
            {
                int chosenTentPosition = WorldGen.genRand.Next(validTentPositions).X;
                int dir = Math.Sign(chosenTentPosition - centerX);
                WorldGen.PlaceObject(chosenTentPosition, floorY, tentType, true, direction: dir);
                placedTent = true;
            }

            //Second try, broaden the area to the whole chamber
            if (!placedTent)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int i = InnerChamberRect.X + 3; i < InnerChamberRect.X -3 + InnerChamberRect.Width; i++)
                    {
                        //Not too close to the center please
                        if (Math.Abs(centerX - i) < 16)
                            continue;

                        int dir = Math.Sign(i - centerX);
                        if (TileObject.CanPlace(i, floorY - j, tentType, 0, dir, out _))
                            validTentPositions.Add(new Point(i, floorY - j));
                    }
                }

                if (validTentPositions.Count > 0)
                {
                    Point chosenTentPosition = WorldGen.genRand.Next(validTentPositions);
                    int dir = Math.Sign(chosenTentPosition.X - centerX);
                    WorldGen.PlaceObject(chosenTentPosition.X, chosenTentPosition.Y, tentType, true, direction: dir);
                }
            }
            #endregion

            #region Placing the tripod torches (Important)
            //Ideally we get three tripods evenly spread out across the area
            List<Point> tripodLeftPositions = new List<Point>();
            List<Point> tripodRightPositions = new List<Point>();
            List<Point> tripodCenterPositions = new List<Point>();

            //Check for all possible spots
            for (int j = 0; j < 2; j++)
            {
                for (int i = InnerChamberRect.X + 4; i < InnerChamberRect.X - 4 + InnerChamberRect.Width; i++)
                {
                    int distanceToCenter = Math.Abs(centerX - i);
                    //Not too close to the center please
                    if (distanceToCenter < 6)
                        continue;

                    int side = Math.Sign(i - centerX);
                    if (TileObject.CanPlace(i, floorY - j, tripodTile, 0, 0, out _))
                    {
                        if (distanceToCenter < 16)
                            tripodCenterPositions.Add(new Point(i, floorY - j));
                        if (distanceToCenter > 13)
                        {
                            if (side == -1)
                                tripodLeftPositions.Add(new Point(i, floorY - j));
                            else
                                tripodRightPositions.Add(new Point(i, floorY - j));
                        }
                    }
                }
            }
            
            //place tripods
            if (tripodCenterPositions.Count > 0)
            {
                Point chosenTripodPos = WorldGen.genRand.Next(tripodCenterPositions);
                WorldGen.PlaceObject(chosenTripodPos.X, chosenTripodPos.Y, tripodTile, true);

                //Clear nearby tripods
                tripodLeftPositions.RemoveAll(p => Math.Abs(p.X - chosenTripodPos.X) < 8);
                tripodRightPositions.RemoveAll(p => Math.Abs(p.X - chosenTripodPos.X) < 8);
            }
            if (tripodLeftPositions.Count > 0)
            {
                Point chosenTripodPos = WorldGen.genRand.Next(tripodLeftPositions);
                WorldGen.PlaceObject(chosenTripodPos.X, chosenTripodPos.Y, tripodTile, true);
            }
            if (tripodRightPositions.Count > 0)
            {
                Point chosenTripodPos = WorldGen.genRand.Next(tripodRightPositions);
                WorldGen.PlaceObject(chosenTripodPos.X, chosenTripodPos.Y, tripodTile, true);
            }
            #endregion

            #region Placing the dummy (funny)
            //Ideally we get three tripods evenly spread out across the area
            List<Point> dummyPositions = new List<Point>();

            //Check for all possible spots
            for (int j = 0; j < 2; j++)
            {
                for (int i = InnerChamberRect.X + 1; i < InnerChamberRect.X - 1 + InnerChamberRect.Width; i++)
                {
                    int distanceToCenter = Math.Abs(centerX - i);
                    //Not too close to the center please
                    if (distanceToCenter < 18)
                        continue;
                    if (TileObject.CanPlace(i, floorY - j, dummyTile, 0, 0, out _))
                        dummyPositions.Add(new Point(i, floorY - j));
                }
            }

            if (dummyPositions.Count > 0)
            {
                Point chosenDummyPos = WorldGen.genRand.Next(dummyPositions);
                WorldGen.PlaceObject(chosenDummyPos.X, chosenDummyPos.Y, dummyTile, true);
            }
            #endregion

            #region Placing crates (woohoo)
            List<Point> cratePositions = new();

            for (int i = InnerChamberRect.X + 2; i < InnerChamberRect.X - 2 + InnerChamberRect.Width; i++)
            {
                int distanceToCenter = Math.Abs(centerX - i);
                //Not too close to the center please
                if (distanceToCenter < 3)
                    continue;

                int placeY = floorY;
                Tile tileAtPos = Main.tile[i, placeY];
                if (tileAtPos.HasTile)
                {
                    //Move up if inside floor
                    if (Main.tileSolid[tileAtPos.TileType])
                        placeY--;
                    //Ignore if inside already placed prop
                    else
                        continue;
                }

                if (TileObject.CanPlace(i, placeY, cratesType, 0, 0, out _))
                    cratePositions.Add(new Point(i, placeY));
            }

            int crateCount = WorldGen.genRand.Next(3, 6);
            int crateStackCount = 0;
            for (int i = 0; i < crateCount; i++)
            {
                if (cratePositions.Count <= 0)
                    break;

                Point chosenCratePos = WorldGen.genRand.Next(cratePositions);

                //Wont go above 2 stacks
                bool tryToStack = crateStackCount == 0 || (crateStackCount == 1 && WorldGen.genRand.NextBool(5));
                bool stacked = false;

                if (tryToStack)
                {
                    //Position is inbetween 2 valid positions: make it into a pile of crates instead
                    if (cratePositions.Count(p => Math.Abs(p.X - chosenCratePos.X) == 1) == 2)
                    {
                        WorldGen.PlaceObject(chosenCratePos.X - 1, chosenCratePos.Y, cratesType, true, WorldGen.genRand.Next(6));
                        WorldGen.PlaceObject(chosenCratePos.X + 1, chosenCratePos.Y, cratesType, true, WorldGen.genRand.Next(6));

                        //Crate on top
                        WorldGen.PlaceObject(chosenCratePos.X, chosenCratePos.Y - 2, cratesType, true, WorldGen.genRand.Next(8));

                        crateCount--;
                        crateStackCount++;
                        stacked = true;
                    }

                    //Position is adjacent to another valid position
                    else 
                    {
                        Point potentialAdjacentCrate = cratePositions.FirstOrDefault(p => Math.Abs(p.X - chosenCratePos.X) == 2);
                        if (potentialAdjacentCrate != default)
                        {
                            WorldGen.PlaceObject(chosenCratePos.X, chosenCratePos.Y, cratesType, true, WorldGen.genRand.Next(6));
                            WorldGen.PlaceObject(potentialAdjacentCrate.X, potentialAdjacentCrate.Y, cratesType, true, WorldGen.genRand.Next(6));

                            int direction = Math.Sign(chosenCratePos.X - potentialAdjacentCrate.X);
                            WorldGen.PlaceObject(potentialAdjacentCrate.X + direction, potentialAdjacentCrate.Y - 2, cratesType, true, WorldGen.genRand.Next(8));
                            
                            crateCount--;
                            crateStackCount++;
                            stacked = true;
                        }
                    }

                    //Clear nearby positions
                    if (stacked)
                        cratePositions.RemoveAll(p => Math.Abs(p.X - chosenCratePos.X) < 5);
                }

                if (!stacked)
                {
                    WorldGen.PlaceObject(chosenCratePos.X, chosenCratePos.Y, cratesType, true, WorldGen.genRand.Next(8));
                    //Clear nearby positions
                    cratePositions.RemoveAll(p => Math.Abs(p.X - chosenCratePos.X) <= 3);
                }
            }
            #endregion
        }

        /// <summary>
        /// Adds the columns of rising/Falling sand on the ceiling and floor
        /// </summary>
        public static void PlaceFallingRisingDust()
        {
            //Place sandfalls from ceiling
            int scanStart = InnerChamberRect.X + WorldGen.genRand.Next(2, 9);
            for (int i = scanStart; i < InnerChamberRect.X + InnerChamberRect.Width; i++)
            {
                for (int j = InnerChamberRect.Y - 2; j < InnerChamberRect.Y + InnerChamberRect.Height / 2; j++)
                {
                    if (DustFallManager.PositionValidForDustFall(new Point(i, j)))
                    {
                        //RNG
                        if (WorldGen.genRand.NextBool())
                            break;

                        DustFallManager.TryPlaceDustfall(new Point(i, j), false, true);
                        i += WorldGen.genRand.Next(5, 25);
                        break;
                    }
                }
            }

            //Place ghostly rising dust from floor
            scanStart = InnerChamberRect.X + WorldGen.genRand.Next(2, 9);
            for (int i = scanStart; i < InnerChamberRect.X + InnerChamberRect.Width; i++)
            {
                for (int j = InnerChamberRect.Y + InnerChamberRect.Height + 2; j > InnerChamberRect.Y + InnerChamberRect.Height / 2; j--)
                {
                    if (DustFallManager.PositionValidForDustFall(new Point(i, j), -1))
                    {
                        //RNG
                        if (WorldGen.genRand.NextBool())
                            break;

                        DustFallManager.TryPlaceDustfall(new Point(i, j), true, true);
                        i += WorldGen.genRand.Next(5, 26);
                        break;
                    }
                }
            }
        }
        #endregion
        #endregion
        #endregion

        #region Sandstone Graveyard generation
        public static void SandstoneGraveyard()
        {
            //If the chamber somehow didn't generate, no graveyard
            if (NautilusChamberRect.IsEmpty)
                return;

            //Try to do a small graveyard, if you can't, try a larger one.. etc
            int tries = 0;
            while (tries < 15)
            {
                int centerX = NautilusChamberRect.X + NautilusChamberRect.Width / 2;
                if (PlaceGraveyard(centerX - 25 - 8 * tries, centerX + 25 + 8 * tries))
                    break;

                tries++;
            }
        }

        public const int MAX_GRAVEYARD_GRAVES = 5;
        public const int MIN_GRAVEYARD_GRAVES = 2;
        private static readonly List<Point16> graveCoordinates = new();
        private static int foundGraves = 0;

        /// <summary>
        /// Checks in the area to find all positions for a graveyard
        /// </summary>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <returns></returns>
        public static bool TryFillGraveyardSpots(int xMin, int xMax)
        {
            foundGraves = 0;
            graveCoordinates.Clear();
            int graveType = ModContent.TileType<SandstoneGraveUnsafe>();

            for (int x = xMin; x < xMax; x++)
            {
                for (int y = (int)(Main.worldSurface * 0.65f); y < Main.worldSurface; y++)
                {
                    if (Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType])
                    {
                        //Check for available space for a grave on top of sand.
                        if (TileID.Sets.Conversion.Sand[Main.tile[x, y].TileType] &&
                            TileID.Sets.Conversion.Sand[Main.tile[x + 1, y].TileType] &&
                            TileObject.CanPlace(x, y - 1, graveType, 0, 0, out _))
                        {
                            //If a valid space is found, log it
                            graveCoordinates.Add(new Point16(x, y - 1));
                            foundGraves++;
                            //Avoids placing graves right next to one another
                            x += 3;
                        }
                        else
                            break;
                    }
                }
            }

            //Return if we could find enough valid grave spots
            return foundGraves >= MIN_GRAVEYARD_GRAVES;
        }

        /// <summary>
        /// Double checks all the positions to see if theyre still valid. Removes all invalid grave coordinates
        /// </summary>
        public static void ValidateGraveyard()
        {
            int graveType = ModContent.TileType<SandstoneGraveUnsafe>();
            int validGraveCount = 0;

            //Double confirm
            for (int i = foundGraves - 1; i >= 0; i--)
            {
                int x = graveCoordinates[i].X;
                int y = graveCoordinates[i].Y;
                if (TileObject.CanPlace(x, y, graveType, 0, 0, out _))
                    validGraveCount++;
                else
                    graveCoordinates.RemoveAt(i);
            }

            foundGraves = validGraveCount;
        }

        public static bool PlaceGraveyard(int xMin, int xMax)
        {
            //ValidateGraveyard();
            TryFillGraveyardSpots(xMin, xMax);

            //Somehow. Shouldn't happen though
            if (foundGraves < MIN_GRAVEYARD_GRAVES)
                return false;

            int[] graveSignIds = new int[MAX_GRAVEYARD_GRAVES];
            #region bla bla bla
            //Commented for posterity and trivia crediting
            /*
                //Gun devil chainsaw man death list
                List<string> deadPeople = new List<string> {

                "Anemone", //Anemones
                "Abzu", //Mesopotamian, refers to a god of fresh water and also some underground water place. Spelunky level!
                "Baleen", //Baleen whales
                "Tridacna", //Giant clam genus
                "Cobalt", //Known for its blue color
                "Glaucus", //Glaucus Atlanticus, the cool dragon sea slug
                "Levy", //Leviathan, the mythological beast
                "Duke Sharptail", //More generic, just a title
                "Triton", //Son of poseidon, and sea snail
                "Azurite", //Blue gemstone
                "Bermuda", //Teal color
                "Spiralia", //Diverse clade of molluscs and friends
                "Aculeatus", //Appears as the species name for some species, like Abdopus acuelatus or Ruscus aculeatus (Derived from aculeus, spiky, prickly). HnK reference
                "Ventricosus", //Same as above. Ventrico means "bulging out
                "Pharida", //Pharidae - Razorshell type clam. Called "knives" in french because of their shape
                "Oegope", //Oegopsida - Order of squids
                "Taonii", //Taoniinae. Subfamily of squids where the collosal squid comes from
                "Platycon", //Platycopiidae, copepod family
                "Janolus", //Sea slug genus named after the 2 faced god janus
                "Costasiella", //Sea slug genus (From where the leaf sheep slug is from)
                "Elysia", //Elysia chlorotica (Chloro seaslug)
                "Caiman", //Alligator. Dorohedoro
                "Argus", //Infernum haha (I dont know the origin)
                "Officer Mora", //Remora, Moonbee

                //Contest
                "Homar", //Homard (Contest example)
                "Diasetos", //DIAdema SETOSum, long-spined sea urchin with a big eye on it (Contest example)
                "Sebae", //Sebae anemones (Contest example)
                "Aurea", //Nembrotha aurea, seaslug (Contest example)
                "Oro", //Mochi guy - Need name origin
                "Acantha", //Splet - Acanthaster starfish genus
                "Conoa", //Kam - Need name origin
            };
            */
            #endregion

            //Theres 15 possible grave inscriptions and 31 dead names
            List<int> graveMarkings = Enumerable.Range(1, 15).ToList();
            List<int> deadPeople = Enumerable.Range(1, 31).ToList();


            //Separate count for the signs specifically to avoid the worldgen crashing if we somehow couldn't place a grave
            int graveSignCount = 0;
            int graveCount = Math.Min(foundGraves, 4);
            if (WorldGen.genRand.NextBool() && foundGraves > 4)
                graveCount++;

            Point16[] placedGravePositions = new Point16[5];

            for (int i = 0; i < graveCount; i++)
            {
                int selectedGrave = WorldGen.genRand.Next(foundGraves);
                Point16 selectedPosition = graveCoordinates[selectedGrave];

                int x = selectedPosition.X;
                int y = selectedPosition.Y;

                WorldGen.PlaceObject(x, y, ModContent.TileType<SandstoneGraveUnsafe>(), true, WorldGen.genRand.Next(4));

                
                int signID = Sign.ReadSign(x, y);
                if (signID >= 0)
                {
                    graveSignIds[i] = signID;
                    graveSignCount++;
                    int sobStoryIndex = WorldGen.genRand.Next(0, graveMarkings.Count);
                    string sobStory = Language.GetText("Mods.CalamityFables.Extras.DesertGravestones.Inscription" + graveMarkings[sobStoryIndex].ToString()).Value;
                    graveMarkings.RemoveAt(sobStoryIndex); //Avoid repeating the same message twice

                    if (sobStory.Contains("{0}"))
                    {
                        int deadPersonIndex = WorldGen.genRand.Next(0, deadPeople.Count);
                        string deadPersonName = Language.GetText("Mods.CalamityFables.Extras.DesertGravestones.Name" + deadPeople[deadPersonIndex].ToString()).Value;
                        deadPeople.RemoveAt(deadPersonIndex); //Avoid repeating the same dead person twice

                        if (sobStory.Contains("{0}'s") && deadPersonName.EndsWith("s"))
                            sobStory = sobStory.Replace("{0}'s", "{0}'");

                        sobStory = sobStory.Replace("{0}", deadPersonName);
                    }

                    Sign.TextSign(signID, sobStory);
                }

                PlaceGraveyardSpikes(x, y + 1);
                PlaceGraveDebris(x, y + 1);

                placedGravePositions[i] = selectedPosition;
                graveCoordinates.RemoveAt(selectedGrave);
                foundGraves--;
            }


            //Guarantee one grave to have a mention of nautilus
            string nautilusGraveText = CalamityFables.Instance.GetLocalization("Extras.DesertGravestones.Nautilus").Value;
            int graveToOverwriteText = WorldGen.genRand.Next(graveSignCount);
            Sign.TextSign(graveSignIds[graveToOverwriteText], nautilusGraveText);

            //If more than one grave was found, check if any are close together to place larger patches of debris
            for (int g = 1; g < graveCount; g++)
            {
                Point16 position = placedGravePositions[g];
                Point16 nextPosition = placedGravePositions[g - 1];

                if (Math.Abs(position.X - nextPosition.X) < 12)
                {
                    int CenterX = (position.X + nextPosition.X) / 2;
                    int CenterY = (position.Y + nextPosition.Y) / 2;

                    //Large patch of debris around both graves
                    DebrisZone(CenterX, CenterY - 1, WorldGen.genRand.Next(12, 19), WorldGen.genRand.Next(14, 20), 0);
                    DebrisZone(CenterX, CenterY + 1, WorldGen.genRand.Next(9, 12), WorldGen.genRand.Next(10, 16), 1);
                    DebrisZone(CenterX, CenterY + 2, WorldGen.genRand.Next(5, 8), WorldGen.genRand.Next(7, 12), 2);

                    DebrisCleanupService(CenterX, CenterY + 1, 19, 20);

                    if (WorldGen.genRand.NextBool(2))
                        break;
                }
            }  

            return true;
        }

        public static void PlaceGraveyardSpikes(int x, int y)
        {
            int placementWidth = WorldGen.genRand.Next(2, 4) * 2;
            int availableSpikes = 3;

            for (int d = -placementWidth / 2; d < placementWidth / 2; d++)
            {
                if (availableSpikes <= 0)
                    break;

                if (!WorldGen.genRand.NextBool(6))
                    continue;

                bool groundFound = false;
                int spikeHeight = 0;
                int spikeBaseY = 0;

                if (!Main.tile[x + d, y + 4].HasTile) //Don't attempt to put down spikes if the ground elevation shifts too abruptly
                    continue;

                //Check for clear ground to put the spike onto
                for (int h = 3; h > -3; h--)
                {
                    if (!groundFound && (!Main.tile[x + d, y + h].HasTile || !Main.tile[x + d, y + h].IsTileSolid()))
                    {
                        //If there's a wall on the floor, don't put a spike there
                        if (Main.tile[x + d, y + h].WallType != 0)
                            break;

                        groundFound = true;
                        spikeBaseY = y + h + 1;
                        spikeHeight = WorldGen.genRand.Next(4, 8);
                        break;
                    }
                }

                if (groundFound)
                {
                    availableSpikes--;

                    int startWidth = -WorldGen.genRand.Next(0, 2);
                    int endWidth = WorldGen.genRand.Next(0, 2);

                    for (int i = 0; i < spikeHeight; i++)
                    {

                        //Only the base of the spike is thicque
                        if (i > 1)
                        {
                            startWidth = 0;
                            endWidth = 0;
                        }

                        for (int j = startWidth; j <= endWidth; j++)
                        {
                            WorldGen.PlaceWall(x + d + j, spikeBaseY - i, WallID.WroughtIronFence, true);
                            WorldGen.paintWall(x + d + j, spikeBaseY - i, WorldGen.genRand.NextBool() ? PaintID.BrownPaint : PaintID.DeepOrangePaint);
                        }
                    }

                    //Prevents 2 spikes from generating next to one another
                    d += WorldGen.genRand.Next(1, 2);
                }
            }
        }

        public static void PlaceGraveDebris(int x, int y)
        {
            DebrisZone(x + 1, y - 1, WorldGen.genRand.Next(6, 19), WorldGen.genRand.Next(8, 14), 0);
            DebrisZone(x + 1, y + 1, WorldGen.genRand.Next(6, 9), WorldGen.genRand.Next(6, 10), 1);
            DebrisZone(x + 1, y + 1, WorldGen.genRand.Next(3, 5), WorldGen.genRand.Next(4, 5), 2);

            DebrisCleanupService(x + 1, y - 1, 19, 18);
        }

        public static void DebrisZone(int x, int y, int width, int height, int tier)
        {
            int[] tileOrder = new int[4] { TileID.Sand, TileID.HardenedSand, TileID.Sandstone, TileID.DesertFossil };

            int placedTile = tileOrder[tier + 1];
            int replacedTile = tileOrder[tier];

            for (int i = x - width / 2; i <= x + width / 2; i++)
            {
                for (int j = y - height / 2; j <= y + height / 2; j++)
                {
                    if (Main.tile[i, j].HasTile && ((replacedTile == TileID.Sand && TileID.Sets.Conversion.Sand[Main.tile[i, j].TileType]) || Main.tile[i, j].TileType == replacedTile))
                    {
                        double distanceToCenter = Math.Pow((i) - x, 2) / Math.Pow(height / 2, 2) + Math.Pow((j) - y, 2) / Math.Pow(height / 2, 2);
                        if (distanceToCenter <= 0.8f || distanceToCenter <= 1f && WorldGen.genRand.NextBool(2))
                        {
                            Main.tile[i, j].TileType = (ushort)placedTile;
                        }
                    }
                }
            }
        }

        //Prevents ugly fucked up tiles not connecting nicely
        public static void DebrisCleanupService(int x, int y, int width, int height)
        {
            //Make sure there is sandstone on the side of fossils
            for (int i = x - width / 2; i <= x + width / 2; i++)
            {
                for (int j = y - height / 2; j <= y + height / 2; j++)
                {
                    Tile myTile = Main.tile[i, j];

                    if (!myTile.HasTile || myTile.TileType != TileID.DesertFossil)
                        continue;

                    if (Main.tile[i - 1, j].HasTile && (Main.tile[i - 1, j].TileType == TileID.Sand || Main.tile[i - 1, j].TileType == TileID.HardenedSand))
                        Main.tile[i - 1, j].TileType = TileID.Sandstone;

                    if (Main.tile[i + 1, j].HasTile && (Main.tile[i + 1, j].TileType == TileID.Sand || Main.tile[i + 1, j].TileType == TileID.HardenedSand))
                        Main.tile[i + 1, j].TileType = TileID.Sandstone;
                }
            }

            for (int i = x - width / 2; i <= x + width / 2; i++)
            {
                for (int j = y - height / 2; j <= y + height / 2 + 1; j++)
                {
                    int aboveTileType = -1;
                    if (Main.tile[i, j - 1].HasTile)
                        aboveTileType = Main.tile[i, j - 1].TileType;

                    int belowTileType = -1;
                    if (Main.tile[i, j + 1].HasTile)
                        belowTileType = Main.tile[i, j + 1].TileType;


                    Tile myTile = Main.tile[i, j];

                    if (!myTile.HasTile)
                        continue;

                    //Fossil and (hardened) sand do not tile together therefore it is important to avoid them touching. We simply swap them out for sandstone as it tiles with b oth
                    if ((myTile.TileType == TileID.HardenedSand || myTile.TileType == TileID.Sand) && aboveTileType == TileID.DesertFossil)
                        myTile.TileType = TileID.Sandstone;

                    //1-tile portrusions of hardened sand and sandstone do not mesh well with sand
                    if ((aboveTileType == -1 || (aboveTileType == TileID.DesertFossil && !Main.tile[i, j - 2].HasTile)) && ((myTile.TileType == TileID.HardenedSand && belowTileType == TileID.Sand) || (myTile.TileType == TileID.Sandstone && (belowTileType == TileID.Sand || belowTileType == TileID.HardenedSand))))
                    {
                        Tile belowTile = Main.tile[i, j + 1];
                        belowTile.TileType = myTile.TileType;
                    }
                }
            }


        }
        #endregion

        #region Background drawing
        public static Asset<Texture2D> ParallaxTexture1;
        public static Asset<Texture2D> ParallaxTexture2;
        public static Asset<Texture2D> ParallaxTexture3;

        public static Asset<Texture2D> ParallaxTextureHallow1;
        public static Asset<Texture2D> ParallaxTextureHallow2;
        public static Asset<Texture2D> ParallaxTextureHallow3;
        public static Asset<Texture2D> ParallaxTextureCorruption1;
        public static Asset<Texture2D> ParallaxTextureCorruption2;
        public static Asset<Texture2D> ParallaxTextureCorruption3;
        public static Asset<Texture2D> ParallaxTextureCrimson1;
        public static Asset<Texture2D> ParallaxTextureCrimson2;
        public static Asset<Texture2D> ParallaxTextureCrimson3;

        public static Asset<Texture2D> ParallaxTexture1_CampfireRed;
        public static Asset<Texture2D> ParallaxTexture2_CampfireRed;
        public static Asset<Texture2D> ParallaxTexture3_CampfireRed;
        public static Asset<Texture2D> ParallaxTexture1_CampfireBlue;
        public static Asset<Texture2D> ParallaxTexture2_CampfireBlue;
        public static Asset<Texture2D> ParallaxTexture3_CampfireBlue;

        private void LoadSealedBackgroundTextures()
        {
            ParallaxTexture1 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax1");
            ParallaxTexture2 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax2");
            ParallaxTexture3 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax3");

            ParallaxTextureHallow1 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax1Hallow");
            ParallaxTextureHallow2 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax2Hallow");
            ParallaxTextureHallow3 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax3Hallow");
            ParallaxTextureCorruption1 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax1Corruption");
            ParallaxTextureCorruption2 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax2Corruption");
            ParallaxTextureCorruption3 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax3Corruption");
            ParallaxTextureCrimson1 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax1Crimson");
            ParallaxTextureCrimson2 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax2Crimson");
            ParallaxTextureCrimson3 ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax3Crimson");

            ParallaxTexture1_CampfireRed ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax1_CampfireRed");
            ParallaxTexture2_CampfireRed ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax2_CampfireRed");
            ParallaxTexture3_CampfireRed ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax3_CampfireRed");

            ParallaxTexture1_CampfireBlue ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax1_CampfireBlue");
            ParallaxTexture2_CampfireBlue ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax2_CampfireBlue");
            ParallaxTexture3_CampfireBlue ??= ModContent.Request<Texture2D>(AssetDirectory.BurntDesert + "SealedChamberParallax3_CampfireBlue");
        }


        public static Asset<Texture2D> UsedParallax1;
        public static Asset<Texture2D> UsedParallax2;
        public static Asset<Texture2D> UsedParallax3;
        public static Asset<Texture2D> OldParallax1;
        public static Asset<Texture2D> OldParallax2;
        public static Asset<Texture2D> OldParallax3;
        public static float backgroundBiomeTransition = 0;


        private void DrawSealedChamberBackground()
        {
            //No chamber, no background
            if (PointOfInterestMarkerSystem.NautilusChamberPos == Vector2.Zero)
                return;

            Vector2 chamberCenter = PointOfInterestMarkerSystem.NautilusChamberPos * 16f + new Vector2(8f, 8f);
            Vector2 viewCenter = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
            Vector2 difference = (chamberCenter - viewCenter) ;

            if (Math.Abs(difference.X) > Main.screenWidth * 1.5f || Math.Abs(difference.Y) > Main.screenHeight * 1.5f)
                return;

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
            Matrix view = Main.GameViewMatrix.TransformationMatrix;
            Matrix renderMatrix = Matrix.CreateTranslation(-Main.screenPosition.Vec3()) * view * projection;
            Effect effect = Scene["SealedChamberParallax"].GetShader().Shader;

            LoadSealedBackgroundTextures();

            Asset<Texture2D> parallax1 = ParallaxTexture1;
            Asset<Texture2D> parallax2 = ParallaxTexture2;
            Asset<Texture2D> parallax3 = ParallaxTexture3;

            if (Main.LocalPlayer.ZoneCorrupt)
            {
                parallax1 = ParallaxTextureCorruption1;
                parallax2 = ParallaxTextureCorruption2;
                parallax3 = ParallaxTextureCorruption3;
            }
            else if (Main.LocalPlayer.ZoneCrimson)
            {
                parallax1 = ParallaxTextureCrimson1;
                parallax2 = ParallaxTextureCrimson2;
                parallax3 = ParallaxTextureCrimson3;
            }
            else if (Main.LocalPlayer.ZoneHallow)
            {
                parallax1 = ParallaxTextureHallow1;
                parallax2 = ParallaxTextureHallow2;
                parallax3 = ParallaxTextureHallow3;
            }

            if (UsedParallax1 == null)
            {

                UsedParallax1 = parallax1;
                UsedParallax2 = parallax2;
                UsedParallax3 = parallax3;
            }

            //Keep track of the older parallax
            if (parallax1.Name != UsedParallax1.Name)
            {
                OldParallax1 = UsedParallax1;
                OldParallax2 = UsedParallax2;
                OldParallax3 = UsedParallax3;

                UsedParallax1 = parallax1;
                UsedParallax2 = parallax2;
                UsedParallax3 = parallax3;
                backgroundBiomeTransition = 1f;
            }

            if (backgroundBiomeTransition > 0f)
                backgroundBiomeTransition -= 0.05f;
            if (backgroundBiomeTransition < 0f)
                backgroundBiomeTransition = 0f;

            effect.Parameters["layer1Texture"].SetValue(UsedParallax1.Value);
            effect.Parameters["layer2Texture"].SetValue(UsedParallax2.Value);
            effect.Parameters["layer3Texture"].SetValue(UsedParallax3.Value);
            effect.Parameters["layer1GlowRed"].SetValue(ParallaxTexture1_CampfireRed.Value);
            effect.Parameters["layer2GlowRed"].SetValue(ParallaxTexture2_CampfireRed.Value);
            effect.Parameters["layer3GlowRed"].SetValue(ParallaxTexture3_CampfireRed.Value);
            effect.Parameters["layer1GlowBlue"].SetValue(ParallaxTexture1_CampfireBlue.Value);
            effect.Parameters["layer2GlowBlue"].SetValue(ParallaxTexture2_CampfireBlue.Value);
            effect.Parameters["layer3GlowBlu"].SetValue(ParallaxTexture3_CampfireBlue.Value);

            float flameFlicker = (0.05f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.05f) + Main.rand.NextFloat(0.85f, 1f);
            Tile t = Main.tile[PointOfInterestMarkerSystem.NautilusChamberPos.ToPoint() + new Point(0, 3)];
            if (!t.HasTile || !Main.tileLighted[t.TileType])
                flameFlicker *= 0f;

            effect.Parameters["parallaxStrenght"].SetValue(new Vector3(0f, 0.005f, 0.016f));
            effect.Parameters["layerTints"].SetValue(new Vector3(1f, 0.98f, 0.96f));
            effect.Parameters["blueness"].SetValue(1 - SirNautilus.SignathionVisualInfluence); 
            effect.Parameters["flameFlicker"].SetValue(flameFlicker);
            effect.Parameters["opacity"].SetValue(1f);

            difference /= 160f;
            difference.Y *= 0.15f;

            effect.Parameters["parallaxDisplace"].SetValue(difference);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin();

            LightedMeshRendering.Render(effect, renderMatrix, new Rectangle((int)PointOfInterestMarkerSystem.NautilusChamberPos.X - 20, (int)PointOfInterestMarkerSystem.NautilusChamberPos.Y - 16, 41, 21));
        
            if (backgroundBiomeTransition > 0)
            {
                effect.Parameters["layer1Texture"].SetValue(OldParallax1.Value);
                effect.Parameters["layer2Texture"].SetValue(OldParallax2.Value);
                effect.Parameters["layer3Texture"].SetValue(OldParallax3.Value);
                effect.Parameters["opacity"].SetValue(backgroundBiomeTransition);

                LightedMeshRendering.RenderAgain(effect, renderMatrix);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        #endregion
    }
}
