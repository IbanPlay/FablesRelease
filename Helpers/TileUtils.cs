using Microsoft.CodeAnalysis.Text;
using Terraria.DataStructures;
using Terraria.ObjectData;

namespace CalamityFables.Helpers
{
    public static partial class FablesUtils
    {

        /// <summary>
        /// Determines if a tile is solid ground based on whether it's active and not actuated or if the tile is solid in any way, including just the top.
        /// </summary>
        /// <param name="tile">The tile to check.</param>
        public static bool IsTileSolidGround(this Tile tile) => tile != null && tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);


        /// <summary>
        /// Determines if a tile is solid based on whether it's active and not actuated or if the tile is solid. This will not count platforms and other non-solid ground tiles
        /// </summary>
        /// <param name="tile">The tile to check.</param>
        public static bool IsTileSolid(this Tile tile) => tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];

        /// <summary>
        /// Determines if a tile is solid based on whether it's active and not actuated or if the tile is solid or a platform
        /// </summary>
        /// <param name="tile">The tile to check.</param>
        public static bool IsTileSolidOrPlatform(this Tile tile) => tile != null && tile.HasUnactuatedTile && Main.tileSolid[tile.TileType];

        /// <summary>
        /// Determines if a tile is "full" based on if the tile is solid. This will count platforms and actuated tiles but no other non-solid ground tiles.
        /// </summary>
        /// <param name="tile">The tile to check.</param>
        public static bool IsTileFull(this Tile tile) => tile != null && tile.HasTile && Main.tileSolid[tile.TileType];

        /// <summary>
        /// Returns a random number between 0 and 1 that always remains the same based on the tile's coordinates.
        /// </summary>
        /// <param name="tilePos">The tile position to grab the rng from</param>
        /// <param name="shift">An extra offset. Useful if you need multiple counts of rng for the same time</param>
        public static float GetSmoothTileRNG(this Point tilePos, int shift = 0) => (float)(Math.Sin(tilePos.X * 17.07947 + shift * 36) + Math.Sin(tilePos.Y * 25.13274)) * 0.25f + 0.5f;



        /// <summary>
        /// Grabs the nearest tile point to the origin, in the specified direction
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Point GetNearestPointInDirection(this Point origin, float direction)
        {
            return origin + new Point((int)Math.Round(Math.Cos(direction)), (int)Math.Round(Math.Sin(direction)));
        }

        /// <summary>
        /// Just like Vector2.ToTileCoordinates, but also clamps the position to the tile grid.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>The tile coordinates</returns>
        public static Point ToSafeTileCoordinates(this Vector2 vec)
        {
            return new Point((int)MathHelper.Clamp((int)vec.X >> 4, 0, Main.maxTilesX), (int)MathHelper.Clamp((int)vec.Y >> 4, 0, Main.maxTilesY));
        }

        /// <summary>
        /// Is a tile valid to be grappled onto
        /// A straight rip of the private method Projectile.AI_007_GrapplingHooks_CanTileBeLatchedOnTo()
        /// </summary>
        /// <param name="theTile"></param>
        /// <returns>Wether or not the tile may be grappled onto</returns>
        public static bool CanTileBeLatchedOnTo(this Tile theTile, bool grappleOnTrees = false) => Main.tileSolid[theTile.TileType] | (theTile.TileType == 314) | (grappleOnTrees && TileID.Sets.IsATreeTrunk[theTile.TileType]) | (grappleOnTrees && theTile.TileType == 323);

        /// <summary>
        /// Gets the required pickaxe power of a tile, accounting for both the ModTile and the vanilla tile pick requirements
        /// </summary>
        /// <param name="tile"></param>
        /// <returns>The pickaxe power required to break a tile</returns>
        public static int GetRequiredPickPower(this Tile tile, int i, int j)
        {
            int pickReq = 0;

            if (Main.tileNoFail[tile.TileType])
                return pickReq;

            ModTile moddedTile = TileLoader.GetTile(tile.TileType);

            //Getting the pickaxe requirement of a modded tile is shrimple.
            if (moddedTile != null)
                pickReq = moddedTile.MinPick;

            //Getting the pickaxe requirement of a vanilla tile is quite clamplicated
            //This was lifted from code in onyx excavator, which likely was lifted from vanilla. It might need 1.4 updating.
            else
            {
                switch (tile.TileType)
                {
                    case TileID.Chlorophyte:
                        pickReq = 200;
                        break;
                    case TileID.Ebonstone:
                    case TileID.Crimstone:
                    case TileID.Pearlstone:
                    case TileID.DesertFossil:
                    case TileID.Obsidian:
                    case TileID.Hellstone:
                        pickReq = 65;
                        break;
                    case TileID.Meteorite:
                        pickReq = 50;
                        break;
                    case TileID.Demonite:
                    case TileID.Crimtane:
                        if (j > Main.worldSurface)
                            pickReq = 55;
                        break;
                    case TileID.LihzahrdBrick:
                    case TileID.LihzahrdAltar:
                        pickReq = 210;
                        break;
                    case TileID.Cobalt:
                    case TileID.Palladium:
                        pickReq = 100;
                        break;
                    case TileID.Mythril:
                    case TileID.Orichalcum:
                        pickReq = 110;
                        break;
                    case TileID.Adamantite:
                    case TileID.Titanium:
                        pickReq = 150;
                        break;
                    default:
                        break;
                }
            }

            if (Main.tileDungeon[tile.TileType])
            {
                if (i < Main.maxTilesX * 0.35 || i > Main.maxTilesX * 0.65)
                    pickReq = 65;
            }

            return pickReq;
        }

        /// <summary>
        /// Returns if a tile is safe to be mined in terms of it being "important"
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="ignoreAbyss">If voidstone and abyss gravel should be considered unsafe to mine</param>
        /// <returns></returns>
        public static bool ShouldBeMined(this Tile tile, bool ignoreAbyss = true)
        {
            List<int> tileExcludeList = new List<int>()
            {
                TileID.DemonAltar, TileID.ElderCrystalStand, TileID.LihzahrdAltar, TileID.Dressers, TileID.Containers
            };

            if (ignoreAbyss)
            {
                // tileExcludeList.Add(ModContent.TileType<AbyssGravel>());
                //tileExcludeList.Add(ModContent.TileType<Voidstone>());
            }

            return !Main.tileContainer[tile.TileType] && !tileExcludeList.Contains(tile.TileType);
        }

        public static bool InsidePolygon(this Vector2 point, List<Vector2> polygonVertexes)
        {
            if (polygonVertexes.Count < 3)
                return false;

            List<Vector2> orderedByHeight = polygonVertexes.OrderBy(v => v.Y).ToList();
            List<Vector2> orderedByWidth = polygonVertexes.OrderBy(v => v.X).ToList();

            Vector2 bottomRightMost = new Vector2(orderedByWidth[0].X, orderedByHeight[0].Y) - Vector2.One;

            int intersections = 0;
            for (int i = 0; i < polygonVertexes.Count; i++)
            {
                int nextIndex = i < polygonVertexes.Count - 1 ? i + 1 : 0;
                if (LinesIntersect(point, bottomRightMost, polygonVertexes[i], polygonVertexes[nextIndex]))
                    intersections++;
            }

            return intersections % 2 == 1;
        }

        public static bool InsidePolygonFast(this Vector2 point, List<Vector2> polygonVertexes, Vector2 bottomRightMost)
        {
            if (polygonVertexes.Count < 3)
                return false;

            int intersections = 0;
            for (int i = 0; i < polygonVertexes.Count; i++)
            {
                int nextIndex = i < polygonVertexes.Count - 1 ? i + 1 : 0;
                if (LinesIntersect(point, bottomRightMost, polygonVertexes[i], polygonVertexes[nextIndex]))
                    intersections++;
            }

            return intersections % 2 == 1;
        }

        public static bool CheckAirRectangle(Point16 position, Point16 size)
        {
            if (position.X + size.X > Main.maxTilesX || position.X < 0) return false; //make sure we dont check outside of the world!
            if (position.Y + size.Y > Main.maxTilesY || position.Y < 0) return false;

            for (int x = position.X; x < position.X + size.X; x++)
            {
                for (int y = position.Y; y < position.Y + size.Y; y++)
                {
                    if (Main.tile[x, y].HasTile) return false; //if any tiles there are active, return false!
                }
            }
            return true;
        }

        public static bool CheckNonsolidRectangle(Point16 position, Point16 size)
        {
            if (position.X + size.X > Main.maxTilesX || position.X < 0) return false; //make sure we dont check outside of the world!
            if (position.Y + size.Y > Main.maxTilesY || position.Y < 0) return false;

            for (int x = position.X; x < position.X + size.X; x++)
            {
                for (int y = position.Y; y < position.Y + size.Y; y++)
                {
                    if (Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType]) return false; //if any tiles there are active, return false!
                }
            }
            return true;
        }

        public static bool CheckAirRectangleAndFloor(Point16 position, Point16 size)
        {
            if (!CheckAirRectangle(position, size))
                return false;

            return CheckTileFloor(position, size);
        }

        public static bool CheckNonsolidRectangleAndFloor(Point16 position, Point16 size)
        {
            if (!CheckNonsolidRectangle(position, size))
                return false;

            return CheckTileFloor(position, size);
        }

        public static bool CheckTileFloor(Point16 position, Point16 size, bool acceptTopSurfaces = true)
        {
            for (int x = position.X; x < position.X + size.X; x++)
            {
                int y = position.Y + size.Y;

                if (!Main.tile[x, y].HasTile || (!Main.tileSolid[Main.tile[x, y].TileType] && !Main.tileSolidTop[Main.tile[x, y].TileType])) return false; //if any tiles there are active, return false!

            }

            return true;
        }

        public static bool FlatFloorify(Point16 position, Point16 size)
        {
            for (int x = position.X; x < position.X + size.X; x++)
            {
                int y = position.Y + size.Y;

                Tile floorTile = Main.tile[x, y];
                floorTile.IsHalfBlock = false;
                floorTile.Slope = SlopeType.Solid;
            }

            return true;
        }


        public static void PlaceMultitile(Point16 position, int type, int style = 0)
        {
            TileObjectData data = TileObjectData.GetTileData(type, style); //magic numbers and uneccisary params begone!

            if (position.X + data.Width > Main.maxTilesX || position.X < 0) return; //make sure we dont spawn outside of the world!
            if (position.Y + data.Height > Main.maxTilesY || position.Y < 0) return;

            int xVariants = 0;
            int yVariants = 0;

            if (data.StyleHorizontal)
                xVariants = Main.rand.Next(data.RandomStyleRange);
            else
                yVariants = Main.rand.Next(data.RandomStyleRange);

            for (int x = 0; x < data.Width; x++) //generate each column
            {
                for (int y = 0; y < data.Height; y++) //generate each row
                {
                    Tile tile = Framing.GetTileSafely(position.X + x, position.Y + y); //get the targeted tile
                    tile.IsHalfBlock = false;
                    tile.Slope = SlopeType.Solid;
                    tile.TileType = (ushort)type; //set the type of the tile to our multitile

                    tile.TileFrameX = (short)((x + data.Width * xVariants) * (data.CoordinateWidth + data.CoordinatePadding)); //set the X frame appropriately
                    //tile.TileFrameY = (short)((y + data.Height * yVariants) * (data.CoordinateHeights[1] + data.CoordinatePadding)); <= Doesn't work lmao! does some ugly corruption shit
                    tile.TileFrameY = (short)((y + data.Height * yVariants) * (data.CoordinateHeights[0] + data.CoordinatePadding)); //set the Y frame appropriately
                    tile.HasTile = true; //activate the tile
                }
            }
        }



        /// <summary>
        /// Gets the average height of the terrain
        /// </summary>
        /// <param name="topography"></param>
        /// <returns></returns>
        public static float AverageTerrainHeight(List<Point> topography)
        {
            int totalAltitudes = 0;
            foreach (Point p in topography)
                totalAltitudes += p.Y;

            return totalAltitudes / (float)topography.Count;
        }

        /// <summary>
        /// Calculates the average difference in elevation across the terrain from the average height of the terrain
        /// </summary>
        /// <param name="topography"></param>
        /// <returns></returns>
        public static float TerrainUnevenness(List<Point> topography)
        {
            float averageHeight = AverageTerrainHeight(topography);

            float heightDifferences = 0;
            foreach (Point p in topography)
                heightDifferences += Math.Abs(p.Y - averageHeight);

            return heightDifferences / topography.Count();
        }

        public static float AverageTerrainSlope(List<Point> topography)
        {
            int totalSlopes = 0;

            for (int i = 1; i < topography.Count(); i++)
            {
                totalSlopes += Math.Abs(topography[i].Y - topography[i - 1].Y);
            }

            return totalSlopes / (float)topography.Count();
        }

        public static List<Point> GetSurroundingTopography(Point center, int halfWidth, int searchMaxHeight, int searchMinHeight, bool needsAir = false)
        {
            List<Point> topography = new List<Point>(halfWidth * 2);

            int leftMost = Math.Max(0, -halfWidth + center.X);
            int rightMost = Math.Min(Main.maxTilesX, halfWidth + center.X);

            int topMost = Math.Max(1, -searchMaxHeight + center.Y);
            int bottomMost = Math.Min(Main.maxTilesY, center.Y + searchMinHeight);


            for (int i = leftMost; i < rightMost; i++)
            {
                bool foundTile = false;
                bool foundAir = !needsAir;
                if (!foundAir && new Point(i, topMost - 1).IsTileNotSolid())
                {
                    foundAir = true;
                }

                for (int j = topMost; j < bottomMost; j++)
                {
                    if (!new Point(i, j).IsTileNotSolid())
                    {
                        if (foundAir)
                        {
                            Dust.QuickDust(new Point(i, j), Color.Red);

                            topography.Add(new Point(i, j));
                            foundTile = true;
                            break;
                        }
                    }

                    else
                        foundAir = true;
                }

                if (!foundTile)
                    topography.Add(new Point(0, 0));
            }

            return topography;
        }

        public static bool IsTileNotSolid(this Point point) => !Main.tile[point].HasTile || !Main.tileSolid[Main.tile[point].TileType];

        //Collision.TileCollision but returns if it collided with nonsolid or not
        public static bool TileCollision(Vector2 position, float width, float height, out bool onlyTopSurfaces)
        {
            onlyTopSurfaces = false;

            int leftX = (int)(position.X / 16f) - 1;
            int rightX = (int)((position.X + (float)width) / 16f) + 2;
            int topY = (int)(position.Y / 16f) - 1;
            int bottomY = (int)((position.Y + (float)height) / 16f) + 2;

            leftX = Utils.Clamp(leftX, 0, Main.maxTilesX - 1);
            rightX = Utils.Clamp(rightX, 0, Main.maxTilesX - 1);
            topY = Utils.Clamp(topY, 0, Main.maxTilesY - 1);
            bottomY = Utils.Clamp(bottomY, 0, Main.maxTilesY - 1);

            Vector2 tileWorldPosition = default(Vector2);
            for (int i = leftX; i < rightX; i++)
            {
                for (int j = topY; j < bottomY; j++)
                {
                    Tile tile = Main.tile[i, j];
                    if (!tile.HasUnactuatedTile)
                        continue;

                    bool topSurfaceCollision = (Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0) || TileID.Sets.Platforms[tile.TileType];
                    bool solidCollision = Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType] && !TileID.Sets.Platforms[tile.TileType];


                    if (solidCollision || topSurfaceCollision)
                    {
                        tileWorldPosition.X = i * 16;
                        tileWorldPosition.Y = j * 16;
                        int tileHeight = 16;
                        if (tile.IsHalfBlock)
                        {
                            tileWorldPosition.Y += 8f;
                            tileHeight -= 8;
                        }

                        if (position.X + (float)width > tileWorldPosition.X &&
                            position.X < tileWorldPosition.X + 16f &&
                            position.Y + (float)height > tileWorldPosition.Y &&
                            position.Y < tileWorldPosition.Y + (float)tileHeight)
                        {
                            if (solidCollision)
                            {
                                onlyTopSurfaces = false;
                                return true;
                            }

                            //We don't return if we found a top surface. We keep checking for non top surface tiles
                            else
                                onlyTopSurfaces = true;
                        }
                    }
                }
            }

            if (onlyTopSurfaces)
                return true;

            return false;
        }

        public static bool FullyWalled(Vector2 position, float width, float height, bool dontCountFences = false)
        {
            int leftX = (int)(position.X / 16f) - 1;
            int rightX = (int)((position.X + (float)width) / 16f) + 2;
            int topY = (int)(position.Y / 16f) - 1;
            int bottomY = (int)((position.Y + (float)height) / 16f) + 2;

            leftX = Utils.Clamp(leftX, 0, Main.maxTilesX - 1);
            rightX = Utils.Clamp(rightX, 0, Main.maxTilesX - 1);
            topY = Utils.Clamp(topY, 0, Main.maxTilesY - 1);
            bottomY = Utils.Clamp(bottomY, 0, Main.maxTilesY - 1);

            for (int i = leftX; i < rightX; i++)
            {
                for (int j = topY; j < bottomY; j++)
                {
                    Tile tile = Main.tile[i, j];
                    if (tile.WallType != 0)
                        continue;

                    return false;
                }
            }
            return true;
        }

        public static float TileFillPercent(Vector2 position, float width, float height, bool countPlatforms = false)
        {
            int leftX = (int)(position.X / 16f) - 1;
            int rightX = (int)((position.X + (float)width) / 16f) + 2;
            int topY = (int)(position.Y / 16f) - 1;
            int bottomY = (int)((position.Y + (float)height) / 16f) + 2;

            leftX = Utils.Clamp(leftX, 0, Main.maxTilesX - 1);
            rightX = Utils.Clamp(rightX, 0, Main.maxTilesX - 1);
            topY = Utils.Clamp(topY, 0, Main.maxTilesY - 1);
            bottomY = Utils.Clamp(bottomY, 0, Main.maxTilesY - 1);

            int tilesChecked = 0;
            int solidTiles = 0;

            for (int i = leftX; i < rightX; i++)
            {
                for (int j = topY; j < bottomY; j++)
                {
                    Tile tile = Main.tile[i, j];
                    tilesChecked++;

                    if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && (countPlatforms || !Main.tileSolidTop[tile.TileType]))
                        solidTiles++;
                }
            }

            return solidTiles / (float)tilesChecked;
        }

        //This is just copied from SwitchMB but tweaked to work with more than 2 different activation states
        /// <summary>
        /// Modified version of <see cref="WorldGen.SwitchMB(int, int)"/> that lets you change the state of a music box through more than 2 different states
        /// </summary>
        /// <param name="type">The music box's type</param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="stateCount">How many different states the music box has</param>
        /// <returns></returns>
        public static bool MultiWayMusicBoxSwitch(int type, int i, int j, int stateCount)
        {
            Tile t = Main.tile[i, j];

            if (t.TileType == type)
            {
                int maxFrameX = (stateCount - 1) * 36;

                //Align with the corner of the tile
                i -= (t.TileFrameX / 18) % 2;
                j -= t.TileFrameY / 18;

                for (int x = i; x < i + 2; x++)
                {
                    for (int y = j; y < j + 2; y++)
                    {
                        if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == type)
                        {
                            //Cycle through
                            if (Main.tile[x, y].TileFrameX < maxFrameX)
                                Main.tile[x, y].TileFrameX += 36;
                            else
                                Main.tile[x, y].TileFrameX -= (short)maxFrameX;
                        }
                    }
                }

                if (Wiring.running)
                {
                    Wiring.SkipWire(i, j);
                    Wiring.SkipWire(i + 1, j);
                    Wiring.SkipWire(i, j + 1);
                    Wiring.SkipWire(i + 1, j + 1);
                }

                NetMessage.SendTileSquare(-1, i, j, 2, 2);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the tile draw offset, which is <see cref="Main.screenPosition"/> with the added <see cref="Main.offScreenRange"/> if necessary
        /// </summary>
        /// <returns></returns>
        public static Vector2 TileDrawOffset()
        {
            return -(Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange)) + Main.screenPosition;
        }

        /// <summary>
        /// Gets the tile draw position based on its position and accounting for <see cref="Main.screenPosition"/> and <see cref="Main.offScreenRange"/>
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public static Vector2 TileDrawPosition(int i, int j)
        {
            return new Vector2(i, j) * 16 - TileDrawOffset();
        }

        public static bool IsTreeTrunk(this Tile t, bool includeRoots = false)
        {
            //This is the blank rectangle at the bottom right of the sheet. Used for everything that is drawn from a different sheet
            if (t.TileFrameY >= 198 && t.TileFrameX >= 22)
                return false;

            //Roots
            if (t.TileFrameY >= 132 && t.TileFrameX >= 22 && t.TileFrameX < 66)
                return includeRoots;

            //Small non swaying branches (left
            if (t.TileFrameY < 66 && t.TileFrameX >= 66 && t.TileFrameX < 88)
                return false;

            //Small non swaying branches (right
            if (t.TileFrameY >= 66 && t.TileFrameY < 132 && t.TileFrameX >= 88 && t.TileFrameX < 110)
                return false;

            return true;
        }

        #region Raytracing
        public static bool RaytraceTo(this Vector2 pos1, Vector2 pos2)
        {
            Point point1 = pos1.ToSafeTileCoordinates();
            Point point2 = pos2.ToSafeTileCoordinates();
            return RaytraceTo(point1.X, point1.Y, point2.X, point2.Y);
        }

        public static bool RaytraceTo(int x0, int y0, int x1, int y1, bool ignoreHalfTiles = false)
        {
            //Bresenham's algorithm
            int horizontalDistance = Math.Abs(x1 - x0); //Delta X
            int verticalDistance = Math.Abs(y1 - y0); //Delta Y
            int horizontalIncrement = (x1 > x0) ? 1 : -1; //S1
            int verticalIncrement = (y1 > y0) ? 1 : -1; //S2

            int x = x0;
            int y = y0;
            int E = horizontalDistance - verticalDistance;

            while (true)
            {
                if (Main.tile[x, y].IsTileSolid() && (!ignoreHalfTiles || !Main.tile[x, y].IsHalfBlock))
                    return false;

                if (x == x1 && y == y1)
                    return true;

                int E2 = E * 2;
                if (E2 >= -verticalDistance)
                {
                    if (x == x1)
                        return true;
                    E -= verticalDistance;
                    x += horizontalIncrement;
                }
                if (E2 <= horizontalDistance)
                {
                    if (y == y1)
                        return true;

                    E += horizontalDistance;
                    y += verticalIncrement;
                }
            }
        }

        public static Point? RaytraceToFirstSolid(this Vector2 pos1, Vector2 pos2, bool ignorePlatforms = false)
        {
            Point point1 = pos1.ToSafeTileCoordinates();
            Point point2 = pos2.ToSafeTileCoordinates();
            return RaytraceToFirstSolid(point1, point2, ignorePlatforms);
        }

        public static Point? RaytraceToFirstSolid(this Point pos1, Point pos2, bool ignorePlatforms = false)
        {
            return RaytraceToFirstSolid(pos1.X, pos1.Y, pos2.X, pos2.Y, ignorePlatforms);
        }

        public static Point? RaytraceToFirstSolid(int x0, int y0, int x1, int y1, bool ignorePlatforms = false)
        {
            //Bresenham's algorithm
            int horizontalDistance = Math.Abs(x1 - x0); //Delta X
            int verticalDistance = Math.Abs(y1 - y0); //Delta Y
            int horizontalIncrement = (x1 > x0) ? 1 : -1; //S1
            int verticalIncrement = (y1 > y0) ? 1 : -1; //S2

            int x = x0;
            int y = y0;
            int i = 1 + horizontalDistance + verticalDistance;
            int E = horizontalDistance - verticalDistance;
            horizontalDistance *= 2;
            verticalDistance *= 2;

            while (i > 0)
            {
                Tile tile = Main.tile[x, y];

                bool isSolid = ignorePlatforms ? tile.IsTileSolid() : tile.IsTileSolidOrPlatform();
                if (isSolid)
                    return new Point(x, y);

                if (E > 0)
                {
                    x += horizontalIncrement;
                    E -= verticalDistance;
                }
                else
                {
                    y += verticalIncrement;
                    E += horizontalDistance;
                }
                i--;
            }
            return null;
        }

        /// <summary>
        /// Vanilla's Collision.SolidCollision breaks with any other platform than wooden ones
        /// </summary>
        /// <param name="Position"></param>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <param name="acceptTopSurfaces"></param>
        /// <returns></returns>
        public static bool SolidCollisionFix(Vector2 Position, int Width, int Height, bool acceptTopSurfaces)
        {
            int value = (int)(Position.X / 16f) - 1;
            int value2 = (int)((Position.X + (float)Width) / 16f) + 2;
            int value3 = (int)(Position.Y / 16f) - 1;
            int value4 = (int)((Position.Y + (float)Height) / 16f) + 2;
            int num = Utils.Clamp(value, 0, Main.maxTilesX - 1);
            value2 = Utils.Clamp(value2, 0, Main.maxTilesX - 1);
            value3 = Utils.Clamp(value3, 0, Main.maxTilesY - 1);
            value4 = Utils.Clamp(value4, 0, Main.maxTilesY - 1);
            Vector2 vector = default(Vector2);
            for (int i = num; i < value2; i++)
            {
                for (int j = value3; j < value4; j++)
                {
                    Tile tile = Main.tile[i, j];
                    if (tile == null || !tile.HasUnactuatedTile)
                        continue;

                    bool flag = Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
                    if (acceptTopSurfaces)
                        flag |= (Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0) || TileID.Sets.Platforms[tile.TileType];

                    if (flag)
                    {
                        vector.X = i * 16;
                        vector.Y = j * 16;
                        int num2 = 16;
                        if (tile.IsHalfBlock)
                        {
                            vector.Y += 8f;
                            num2 -= 8;
                        }

                        if (Position.X + (float)Width > vector.X && Position.X < vector.X + 16f && Position.Y + (float)Height > vector.Y && Position.Y < vector.Y + (float)num2)
                            return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc cref="CanHitLine(Point, Point, bool)"/>
        public static bool CanHitLine(Vector2 startPosition, Vector2 endPosition, bool ignorePlatforms = false) => CanHitLine(startPosition.ToSafeTileCoordinates(), endPosition.ToSafeTileCoordinates(), ignorePlatforms);

        /// <summary>
        /// An improved version of <see cref="Collision.CanHitLine(Vector2, int, int, Vector2, int, int)"/> that utilizes raytracing and properly detects platforms.
        /// </summary>
        /// <param name="StartPosition"></param>
        /// <param name="StartSize"></param>
        /// <param name="EndPosition"></param>
        /// <param name="EndSize"></param>
        /// <param name="ignorePlatforms"></param>
        /// <returns></returns>
        public static bool CanHitLine(Point startPosition, Point endPosition, bool ignorePlatforms = false)
        {
            // Fills a list of points by raytracing between the start and end positions
            List<Point> points = [];
            RaytraceFillList(startPosition, endPosition, ref points);

            // Determine if platforms should be ignored if the points list is entirely horizontal, since top collision wont matter.
            ignorePlatforms &= (points.First().Y - points.Last().Y) == 0;

            // Iterate through each point and check if each is solid
            foreach(Point tilePosition in points)
            {
                Tile tile = Main.tile[tilePosition];

                // Checks if the tile is unactuated and solid, or it has a solid top
                bool isSolid = ignorePlatforms ? tile.IsTileSolid() : tile.IsTileSolidOrPlatform();
                if (isSolid)
                    return false;
            }

            return true;
        }

        /// <inheritdoc cref="DepthFromPoint(Point, int, bool)"/>
        public static int DepthFromPoint(Vector2 startPosition, int maxDepth = 10, bool ignorePlatforms = false) => DepthFromPoint(startPosition.ToSafeTileCoordinates(), maxDepth, ignorePlatforms);

        /// <summary>
        /// Finds the number of tiles below a specific point to the first solid tile.
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="maxDepth"></param>
        /// <param name="ignorePlatforms"></param>
        /// <returns></returns>
        public static int DepthFromPoint(Point startPosition, int maxDepth = 10, bool ignorePlatforms = false)
        {
            // Fills a list of points by raytracing between the start and end positions
            List<Point> points = [];
            RaytraceFillList(startPosition, startPosition with { Y = startPosition.Y + maxDepth - 1}, ref points);

            RaytraceToFirstSolid(startPosition, startPosition with { Y = startPosition.Y + maxDepth - 1 });

            for (int i = 0; i < maxDepth; i++)
            {
                Tile tile = Main.tile[points[i]];

                // Checks if the tile is unactuated and solid, or it has a solid top
                bool isSolid = ignorePlatforms ? tile.IsTileSolid() : tile.IsTileSolidOrPlatform();
                if (isSolid)
                    return i;
            }

            return maxDepth;
        }

        /// <inheritdoc cref="DepthFromPoint(Point, int, bool)"/>
        public static int HeightFromPoint(Vector2 startPosition, int maxHeight = 10, bool ignorePlatforms = false) => HeightFromPoint(startPosition.ToSafeTileCoordinates(), maxHeight, ignorePlatforms);

        /// <summary>
        /// Finds the number of tiles above a specific point to the first solid tile.
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="maxDepth"></param>
        /// <param name="ignorePlatforms"></param>
        /// <returns></returns>
        public static int HeightFromPoint(Point startPosition, int maxHeight = 10, bool ignorePlatforms = false)
        {
            // Fills a list of points by raytracing between the start and end positions
            List<Point> points = [];
            RaytraceFillList(startPosition, startPosition with { Y = startPosition.Y - maxHeight + 1 }, ref points);

            for (int i = 0; i < maxHeight; i++)
            {
                Tile tile = Main.tile[points[i]];

                // Checks if the tile is unactuated and solid, or it has a solid top
                bool isSolid = ignorePlatforms ? tile.IsTileSolid() : tile.IsTileSolidOrPlatform();
                if (isSolid)
                    return i;
            }

            return maxHeight;
        }

        /// <summary>
        /// Returns if the entire boundary is within solid tiles
        /// </summary>
        /// <param name="Position"></param>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <param name="tightFit"> should the check box be restricted to only tiles that the coordinates are fully into, or should they include the ones bordering the box</param>
        /// <returns></returns>
        public static bool FullSolidCollision(Vector2 Position, int Width, int Height, bool tightFit = true)
        {
            int minX = (int)(Position.X / 16f) - 1;
            int maxX = (int)((Position.X + Width) / 16f) + 2;
            int minY = (int)(Position.Y / 16f) - 1;
            int maxY = (int)((Position.Y + Height) / 16f) + 2;


            int num = Utils.Clamp(minX, 0, Main.maxTilesX - 1);
            maxX = Utils.Clamp(maxX, 0, Main.maxTilesX - 1);
            minY = Utils.Clamp(minY, 0, Main.maxTilesY - 1);
            maxY = Utils.Clamp(maxY, 0, Main.maxTilesY - 1);
            Vector2 tilePosition = default(Vector2);
            for (int i = num; i < maxX; i++)
            {
                for (int j = minY; j < maxY; j++)
                {
                    Tile tile = Main.tile[i, j];
                    tilePosition.X = i * 16;
                    tilePosition.Y = j * 16;
                    int tileHeight = 16;
                    if (tile.IsHalfBlock)
                    {
                        tilePosition.Y += 8f;
                        tileHeight -= 8;
                    }

                    //Check if the tile is within the hitbox
                    if (Position.X + Width < tilePosition.X ||
                        Position.X > tilePosition.X + 16f ||
                        Position.Y + Height < tilePosition.Y ||
                        Position.Y > tilePosition.Y + tileHeight)
                        continue;

                    if (!tile.HasUnactuatedTile || Main.tileSolidTop[tile.TileType] || !Main.tileSolid[tile.TileType])
                        return false;
                }
            }

            return true;
        }


        public static int RaytraceFillList(this Vector2 pos1, Vector2 pos2, ref List<Point> path)
        {
            Point point1 = pos1.ToSafeTileCoordinates();
            Point point2 = pos2.ToSafeTileCoordinates();
            return RaytraceFillList(point1, point2, ref path);
        }

        public static int RaytraceFillList(this Point pos1, Point pos2, ref List<Point> path)
        {
            return RaytraceFillList(pos1.X, pos1.Y, pos2.X, pos2.Y, ref path);
        }

        public static int RaytraceFillList(int x0, int y0, int x1, int y1, ref List<Point> path)
        {
            //Bresenham's algorithm
            int horizontalDistance = Math.Abs(x1 - x0); //Delta X
            int verticalDistance = Math.Abs(y1 - y0); //Delta Y
            int horizontalIncrement = (x1 > x0) ? 1 : -1; //S1
            int verticalIncrement = (y1 > y0) ? 1 : -1; //S2

            int x = x0;
            int y = y0;
            int i = 1 + horizontalDistance + verticalDistance;
            int E = horizontalDistance - verticalDistance;
            horizontalDistance *= 2;
            verticalDistance *= 2;
            int pathLenght = 0;

            if (path == null)
                path = new();

            while (i > 0)
            {
                if (path.Count <= pathLenght)
                    path.Add(new Point(x, y));
                else
                    path[pathLenght] = new Point(x, y);

                pathLenght++;
                if (E > 0)
                {
                    x += horizontalIncrement;
                    E -= verticalDistance;
                }
                else
                {
                    y += verticalIncrement;
                    E += horizontalDistance;
                }
                i--;
            }

            return pathLenght;
        }
        #endregion

        public static readonly Point[] AllAdjacentTileDirections = new Point[]
        {
            new Point(0, 0),
            new Point(-1, -1),
            new Point(0, -1),
            new Point(1, -1),
            new Point(-1, 0),
            new Point(1, 0),
            new Point(-1, 1),
            new Point(0, 1),
            new Point(1, 1)
        };

        public static readonly Point[] DirectAdjacentTileDirections = new Point[]
        {
            new Point(0, -1),
            new Point(1, 0),
            new Point(0, 1),
            new Point(-1, 0),
        };

        public static readonly Vector2[] AllAdjacentDirections = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(-1, -1),
            new Vector2(0, -1),
            new Vector2(1, -1),
            new Vector2(-1, 0),
            new Vector2(1, 0),
            new Vector2(-1, 1),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };


        //TYYYY lyra :3
        #region Tile framing

        /// <summary>
        /// Sets the mergeability state of two tiles. By default, enables tile merging.
        /// </summary>
        /// <param name="type1">The first tile type which should merge (or not).</param>
        /// <param name="type2">The second tile type which should merge (or not).</param>
        /// <param name="merge">The mergeability state of the tiles. Defaults to true if omitted.</param>
        public static void SetMerge(int type1, int type2, bool merge = true)
        {
            if (type1 != type2)
            {
                Main.tileMerge[type1][type2] = merge;
                Main.tileMerge[type2][type1] = merge;
            }
        }

        /// <summary>
        /// Makes the first tile type argument merge with all the other tile type arguments. Also accepts arrays.
        /// </summary>
        /// <param name="myType">The tile whose merging properties will be set.</param>
        /// <param name="otherTypes">Every tile that should be merged with.</param>
        public static void MergeWithSet(int myType, params int[] otherTypes)
        {
            for (int i = 0; i < otherTypes.Length; ++i)
                SetMerge(myType, otherTypes[i]);
        }

        public delegate bool MergeCheckDelegate(Tile myTile, Tile mergeTile);
        public static bool NoSpecialmergeCheck(Tile t, Tile t2) => false;
        public static MergeCheckDelegate SpecialMergeCheck = NoSpecialmergeCheck;

        private static bool GetMerge(Tile myTile, Tile mergeTile)
        {
            return mergeTile.HasTile && (mergeTile.TileType == myTile.TileType || Main.tileMerge[myTile.TileType][mergeTile.TileType] || SpecialMergeCheck.Invoke(myTile, mergeTile));
        }

        private static void GetAdjacentTiles(int x, int y, out bool up, out bool down, out bool left, out bool right, out bool upLeft, out bool upRight, out bool downLeft, out bool downRight)
        {
            // These all get null checked in the GetMerge function
            Tile tile = Main.tile[x, y];
            Tile north = Main.tile[x, y - 1];
            Tile south = Main.tile[x, y + 1];
            Tile west = Main.tile[x - 1, y];
            Tile east = Main.tile[x + 1, y];
            Tile southwest = Main.tile[x - 1, y + 1];
            Tile southeast = Main.tile[x + 1, y + 1];
            Tile northwest = Main.tile[x - 1, y - 1];
            Tile northeast = Main.tile[x + 1, y - 1];

            left = false;
            right = false;
            up = false;
            down = false;
            upLeft = false;
            upRight = false;
            downLeft = false;
            downRight = false;

            if (GetMerge(tile, north) && (north.Slope == 0 || north.Slope == SlopeType.SlopeDownLeft || north.Slope == SlopeType.SlopeDownRight))
                up = true;
            if (GetMerge(tile, south) && (south.Slope == 0 || south.Slope == SlopeType.SlopeUpLeft || south.Slope == SlopeType.SlopeUpRight))
                down = true;
            if (GetMerge(tile, west) && (west.Slope == 0 || west.Slope == SlopeType.SlopeDownRight || west.Slope == SlopeType.SlopeUpRight))
                left = true;
            if (GetMerge(tile, east) && (east.Slope == 0 || east.Slope == SlopeType.SlopeDownLeft || east.Slope == SlopeType.SlopeUpLeft))
                right = true;
            if (GetMerge(tile, north) && GetMerge(tile, west) && GetMerge(tile, northwest) && (northwest.Slope == 0 || northwest.Slope == SlopeType.SlopeDownRight) && (north.Slope == 0 || north.Slope == SlopeType.SlopeDownLeft || north.Slope == SlopeType.SlopeUpLeft) && (west.Slope == 0 || west.Slope == SlopeType.SlopeUpLeft || west.Slope == SlopeType.SlopeUpRight))
                upLeft = true;
            if (GetMerge(tile, north) && GetMerge(tile, east) && GetMerge(tile, northeast) && (northeast.Slope == 0 || northeast.Slope == SlopeType.SlopeDownLeft) && (north.Slope == 0 || north.Slope == SlopeType.SlopeDownRight || north.Slope == SlopeType.SlopeUpRight) && (east.Slope == 0 || east.Slope == SlopeType.SlopeUpLeft || east.Slope == SlopeType.SlopeUpRight))
                upRight = true;
            if (GetMerge(tile, south) && GetMerge(tile, west) && GetMerge(tile, southwest) && !southwest.IsHalfBlock && (southwest.Slope == 0 || southwest.Slope == SlopeType.SlopeUpRight) && (south.Slope == 0 || south.Slope == SlopeType.SlopeDownLeft || south.Slope == SlopeType.SlopeUpLeft) && (west.Slope == 0 || west.Slope == SlopeType.SlopeDownLeft || west.Slope == SlopeType.SlopeDownRight))
                downLeft = true;
            if (GetMerge(tile, south) && GetMerge(tile, east) && GetMerge(tile, southeast) && !southeast.IsHalfBlock && (southeast.Slope == 0 || southeast.Slope == SlopeType.SlopeUpLeft) && (south.Slope == 0 || south.Slope == SlopeType.SlopeDownRight || south.Slope == SlopeType.SlopeUpRight) && (east.Slope == 0 || east.Slope == SlopeType.SlopeDownLeft || east.Slope == SlopeType.SlopeDownRight))
                downRight = true;
        }

        internal static bool BetterGemsparkFraming(int x, int y, bool resetFrame, MergeCheckDelegate specialMergeCheck = null)
        {
            SpecialMergeCheck = NoSpecialmergeCheck;
            if (specialMergeCheck != null)
                SpecialMergeCheck = specialMergeCheck;

            if (x < 0 || x >= Main.maxTilesX)
                return false;
            if (y < 0 || y >= Main.maxTilesY)
                return false;

            Tile tile = Main.tile[x, y];
            if (tile.Slope > 0 && TileID.Sets.HasSlopeFrames[tile.TileType])
                return true;

            GetAdjacentTiles(x, y, out bool up, out bool down, out bool left, out bool right, out bool upLeft, out bool upRight, out bool downLeft, out bool downRight);

            // Reset the tile's random frame style if the frame is being reset.
            int randomFrame;
            if (resetFrame)
            {
                randomFrame = WorldGen.genRand.Next(3);
                Main.tile[x, y].Get<TileWallWireStateData>().TileFrameNumber = randomFrame;
            }
            else
                randomFrame = Main.tile[x, y].TileFrameNumber;

            /*
                8 2 9
                4 - 5
                6 3 7
            */

            #region L States
            if (!up && down && !left && right && !downRight)
            {
                tile.TileFrameX = 13 * 18;
                tile.TileFrameY = 0;
                return false;
            }
            if (!up && down && left && !right && !downLeft)
            {
                tile.TileFrameX = 15 * 18;
                tile.TileFrameY = 0;
                return false;
            }
            if (up && !down && !left && right && !upRight)
            {
                tile.TileFrameX = 13 * 18;
                tile.TileFrameY = 2 * 18;
                return false;
            }
            if (up && !down && left && !right && !upLeft)
            {
                tile.TileFrameX = 15 * 18;
                tile.TileFrameY = 2 * 18;
                return false;
            }
            #endregion

            #region T States
            if (!up && down && left && right && !downLeft && !downRight)
            {
                tile.TileFrameX = 14 * 18;
                tile.TileFrameY = 0;
                return false;
            }
            if (up && !down && left && right && !upLeft && !upRight)
            {
                tile.TileFrameX = 14 * 18;
                tile.TileFrameY = 2 * 18;
                return false;
            }
            if (up && down && !left && right && !downRight && !upRight)
            {
                tile.TileFrameX = 13 * 18;
                tile.TileFrameY = 18;
                return false;
            }
            if (up && down && left && !right && !downLeft && !upLeft)
            {
                tile.TileFrameX = 15 * 18;
                tile.TileFrameY = 18;
                return false;
            }
            #endregion

            #region X State
            if (up && down && left && right && !downLeft && !downRight && !upLeft && !upRight)
            {
                tile.TileFrameX = 14 * 18;
                tile.TileFrameY = 18;
                return false;
            }
            #endregion

            #region Inner Corner x1
            if (up && down && left && right && !downLeft && downRight && upLeft && upRight)
            {
                tile.TileFrameX = 15 * 18;
                tile.TileFrameY = 3 * 18;
                return false;
            }
            if (up && down && left && right && downLeft && !downRight && upLeft && upRight)
            {
                tile.TileFrameX = 14 * 18;
                tile.TileFrameY = 3 * 18;
                return false;
            }
            if (up && down && left && right && downLeft && downRight && !upLeft && upRight)
            {
                tile.TileFrameX = 15 * 18;
                tile.TileFrameY = 4 * 18;
                return false;
            }
            if (up && down && left && right && downLeft && downRight && upLeft && !upRight)
            {
                tile.TileFrameX = 14 * 18;
                tile.TileFrameY = 4 * 18;
                return false;
            }
            #endregion

            #region Inner Corner x2 (same side)
            if (up && down && left && right && !downLeft && !downRight && upLeft && upRight)
            {
                tile.TileFrameX = (short)((6 * 18) + (randomFrame * 18));
                tile.TileFrameY = 2 * 18;
                return false;
            }
            if (up && down && left && right && downLeft && downRight && !upLeft && !upRight)
            {
                tile.TileFrameX = (short)((6 * 18) + (randomFrame * 18));
                tile.TileFrameY = 1 * 18;
                return false;
            }
            if (up && down && left && right && !downLeft && downRight && !upLeft && upRight)
            {
                tile.TileFrameX = 10 * 18;
                tile.TileFrameY = (short)(randomFrame * 18);
                return false;
            }
            if (up && down && left && right && downLeft && !downRight && upLeft && !upRight)
            {
                tile.TileFrameX = 11 * 18;
                tile.TileFrameY = (short)(randomFrame * 18);
                return false;
            }
            #endregion

            #region Inner Corner x2 (opposite corners)
            if (up && down && left && right && !downLeft && downRight && upLeft && !upRight)
            {
                tile.TileFrameX = 16 * 18;
                tile.TileFrameY = 4 * 18;
                return false;
            }
            if (up && down && left && right && downLeft && !downRight && !upLeft && upRight)
            {
                tile.TileFrameX = 17 * 18;
                tile.TileFrameY = 4 * 18;
                return false;
            }
            #endregion

            #region Inner Corner x3
            if (up && down && left && right && !downLeft && !downRight && !upLeft && upRight)
            {
                tile.TileFrameX = 12 * 18;
                tile.TileFrameY = 4 * 18;
                return false;
            }
            if (up && down && left && right && !downLeft && downRight && !upLeft && !upRight)
            {
                tile.TileFrameX = 12 * 18;
                tile.TileFrameY = 3 * 18;
                return false;
            }
            if (up && down && left && right && !downLeft && !downRight && upLeft && !upRight)
            {
                tile.TileFrameX = 13 * 18;
                tile.TileFrameY = 4 * 18;
                return false;
            }
            if (up && down && left && right && downLeft && !downRight && !upLeft && !upRight)
            {
                tile.TileFrameX = 13 * 18;
                tile.TileFrameY = 3 * 18;
                return false;
            }
            #endregion

            #region Corner and Side
            if (!up && down && left && right && !downLeft && downRight && !upLeft && !upRight)
            {
                tile.TileFrameX = 17 * 18;
                tile.TileFrameY = 2 * 18;
                return false;
            }
            if (!up && down && left && right && downLeft && !downRight && !upLeft && !upRight)
            {
                tile.TileFrameX = 16 * 18;
                tile.TileFrameY = 2 * 18;
                return false;
            }
            if (up && !down && left && right && !downLeft && !downRight && !upLeft && upRight)
            {
                tile.TileFrameX = 17 * 18;
                tile.TileFrameY = 3 * 18;
                return false;
            }
            if (up && !down && left && right && !downLeft && !downRight && upLeft && !upRight)
            {
                tile.TileFrameX = 16 * 18;
                tile.TileFrameY = 3 * 18;
                return false;
            }
            if (up && down && !left && right && !downLeft && !downRight && !upLeft && upRight)
            {
                tile.TileFrameX = 16 * 18;
                tile.TileFrameY = 0;
                return false;
            }
            if (up && down && !left && right && !downLeft && downRight && !upLeft && !upRight)
            {
                tile.TileFrameX = 16 * 18;
                tile.TileFrameY = 18;
                return false;
            }
            if (up && down && left && !right && !downLeft && !downRight && upLeft && !upRight)
            {
                tile.TileFrameX = 17 * 18;
                tile.TileFrameY = 0;
                return false;
            }
            if (up && down && left && !right && downLeft && !downRight && !upLeft && !upRight)
            {
                tile.TileFrameX = 17 * 18;
                tile.TileFrameY = 18;
                return false;
            }
            #endregion

            return true;
        }

        #region Set merge defaults
        public static void MergeWithGeneral(int type) => MergeWithSet(type, new int[] {
            // Soils
            TileID.Dirt,
            TileID.Mud,
            TileID.ClayBlock,
            // Stones
            TileID.Stone,
            TileID.Ebonstone,
            TileID.Crimstone,
            TileID.Pearlstone,
            // Sands
            TileID.Sand,
            TileID.Ebonsand,
            TileID.Crimsand,
            TileID.Pearlsand,
            // Snows
            TileID.SnowBlock
        });

        /// <summary>
        /// Makes the specified tile merge with all ores, vanilla and Calamity. Particularly useful for stone blocks.
        /// </summary>
        /// <param name="type">The tile whose merging properties will be set.</param>
        public static void MergeWithOres(int type) => MergeWithSet(type, new int[] {
            // Vanilla Ores
            TileID.Copper,
            TileID.Tin,
            TileID.Iron,
            TileID.Lead,
            TileID.Silver,
            TileID.Tungsten,
            TileID.Gold,
            TileID.Platinum,
            TileID.Demonite,
            TileID.Crimtane,
            TileID.Cobalt,
            TileID.Palladium,
            TileID.Mythril,
            TileID.Orichalcum,
            TileID.Adamantite,
            TileID.Titanium,
            TileID.LunarOre
        });

        /// <summary>
        /// Makes the specified tile merge with all types of desert tiles, including the Calamity Sunken Sea.
        /// </summary>
        /// <param name="type">The tile whose merging properties will be set.</param>
        public static void MergeWithDesert(int type) => MergeWithSet(type, new int[] {
            // Sands
            TileID.Sand,
            TileID.Ebonsand,
            TileID.Crimsand,
            TileID.Pearlsand,
            // Hardened Sands
            TileID.HardenedSand,
            TileID.CorruptHardenedSand,
            TileID.CrimsonHardenedSand,
            TileID.HallowHardenedSand,
            // Sandstones
            TileID.Sandstone,
            TileID.CorruptSandstone,
            TileID.CrimsonSandstone,
            TileID.HallowSandstone,
            // Miscellaneous Desert Tiles
            TileID.FossilOre,
            TileID.DesertFossil
        });

        /// <summary>
        /// Makes the specified tile merge with all types of snow and ice tiles.
        /// </summary>
        /// <param name="type">The tile whose merging properties will be set.</param>
        public static void MergeWithSnow(int type) => MergeWithSet(type, new int[] {
            // Snows
            TileID.SnowBlock,
            // Ices
            TileID.IceBlock,
            TileID.CorruptIce,
            TileID.FleshIce,
            TileID.HallowedIce,
        });

        /// <summary>
        /// Makes the specified tile merge with all tiles which generate in hell. Does not include Charred Ore.
        /// </summary>
        /// <param name="type">The tile whose merging properties will be set.</param>
        public static void MergeWithHell(int type) => MergeWithSet(type, new int[] {
            TileID.Ash,
            TileID.Hellstone,
            TileID.ObsidianBrick,
            TileID.HellstoneBrick,
        });
        #endregion
        #endregion
    }
}
