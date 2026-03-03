using CalamityFables.Content.Tiles.BurntDesert;
using Microsoft.VisualBasic;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Core
{
    public class MergingWallLoader
    {
        public static Mod Mod => CalamityFables.Instance;

        /// <summary>
        /// Creates the modwalls necessary to achieve a wall with custom blend <br/>
        /// Walls with merge are actually 4 separate sub-walls put together to circumvent vanilla's 16x8 limitation on wall frames
        /// </summary>
        /// <param name="texturePath">Texturepath for the wall's texture</param>
        /// <param name="baseName">Internal name used for the walls, automatically appended with the index of the specific sub-wall</param>
        /// <param name="itemType">Item drop for the wall</param>
        /// <param name="wallTypes">List of all the loaded wall types, to be filled when autoloaded. Used to retrieve the walltype to place</param>
        /// <param name="mergeTypes">List of all wall types with which the wall is supposed to have custom merge frames with</param>
        /// <param name="mapColor">Color of the wall on the map</param>
        /// <param name="dustType">Dust produced by the wall</param>
        public static void LoadMergingWall(string texturePath, string baseName, int itemType, ref ushort[] wallTypes, ushort[] mergeTypes, Color mapColor, int dustType)
        {
            int wallCount = 0;
            int wallBlend = Main.wallBlend[mergeTypes[0]]; //We can deduce the wallblend by looking at the type of wall were merging with

            for (int i = 0; i < 4; i++)
            {
                AutoloadedMergingWall wall = new AutoloadedMergingWall(texturePath, baseName, wallCount, itemType, wallTypes, mergeTypes, wallBlend, mapColor, dustType);
                Mod.AddContent(wall);
                wallTypes[wallCount] = wall.Type;
                wallCount++;
            }
        }
    }

    [Autoload(false)]
    public class AutoloadedMergingWall : ModWall
    {
        public override string Name => InternalName != "" ? InternalName : base.Name;
        public string InternalName;
        public string TexturePath;
        public override string Texture => TexturePath + InternalName;

        /// <summary>
        /// All wall types making up the wall
        /// </summary>
        public ushort[] wallTypes;
        /// <summary>
        /// All walls with which the wall should use its custom merge for
        /// </summary>
        public ushort[] mergeTypes;
        public Color mapColor;
        public int itemType;
        public int wallBlend;

        public AutoloadedMergingWall(string texturePath, string baseName, int wallCount, int itemType, ushort[] wallTypes, ushort[] mergeTypes, int wallBlend, Color mapColor, int dustType)
        {
            TexturePath = texturePath;
            this.itemType = itemType;
            this.wallTypes = wallTypes;
            this.mergeTypes = mergeTypes;   
            this.mapColor = mapColor;
            this.wallBlend = wallBlend;
            DustType = dustType;
            InternalName = baseName + wallCount.ToString();
        }

        public override void SetStaticDefaults()
        {
            Main.wallBlend[Type] = wallBlend;
            AddMapEntry(mapColor);
            RegisterItemDrop(itemType);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override bool WallFrame(int i, int j, bool randomizeFrame, ref int style, ref int frameNumber)
        {
            bool result = FrameLogic(i, j, ref style, ref frameNumber, out int wallFrameX, out int wallFrameY);

            Tile t = Main.tile[i, j];
            if (result || (wallFrameY <= 36 * 7))
            {
                t.WallType = wallTypes[0];

                if (!result)
                {
                    t.WallFrameX = wallFrameX;
                    t.WallFrameY = wallFrameY;
                }
            }
            else if (!result)
            {
                if (wallFrameX <= 36 * 15)
                {
                    if (wallFrameY <= 36 * 15)
                    {
                        t.WallType = wallTypes[1];
                        wallFrameY -= 36 * 8;
                    }
                    else if (wallFrameY <= 36 * 23)
                    {
                        t.WallType = wallTypes[2];
                        wallFrameY -= 36 * 16;
                    }
                    else
                    {
                        t.WallType = wallTypes[3];
                        wallFrameY -= 36 * 24;

                        if (wallFrameX <= 10 * 36)
                        {
                            wallFrameX += 5 * 36;
                        }
                        else
                        {
                            wallFrameY += 5 * 36;
                            wallFrameX -= 36;
                        }
                    }
                }
                else
                {
                    t.WallType = wallTypes[3];
                    if (wallFrameY <= 36 * 15)
                    {
                        wallFrameY -= 36 * 8;
                        wallFrameX -= 36 * 11;
                    }
                    else
                    {
                        wallFrameY -= 36 * 19;
                        wallFrameX -= 36 * 16;
                    }
                }

                t.WallFrameX = wallFrameX;
                t.WallFrameY = wallFrameY;
            }

            return result;
        }

        public bool FrameLogic(int i, int j, ref int style, ref int frameNumber, out int wallFrameX, out int wallFrameY)
        {
            bool showInvisible = Main.ShouldShowInvisibleWalls();
            Tile t = Main.tile[i, j];
            t.WallFrameNumber = frameNumber;

            wallFrameX = 0;
            wallFrameY = 0;

            //first bit of style is top, second is left, third is right, fourth is down
            //No connections on the sides, no issue
            if (style == 0)
                return true;

            //Vanilla applies an offset for fully surrounded tiles
            if (style > 15)
                style = 15;

            #region Initialize cardinal merge
            bool mergeLeft = false;
            bool mergeRight = false;
            bool mergeTop = false;
            bool mergeBottom = false;
            if (j - 1 >= 0)
            {
                Tile tile2 = Main.tile[i, j - 1];
                mergeTop = mergeTypes.Contains(tile2.WallType) && (showInvisible || !tile2.IsWallInvisible);
            }
            if (i - 1 >= 0)
            {
                Tile tile2 = Main.tile[i - 1, j];
                mergeLeft = mergeTypes.Contains(tile2.WallType) && (showInvisible || !tile2.IsWallInvisible);
            }
            if (i + 1 <= Main.maxTilesX - 1)
            {
                Tile tile2 = Main.tile[i + 1, j];
                mergeRight = mergeTypes.Contains(tile2.WallType) && (showInvisible || !tile2.IsWallInvisible);
            }
            if (j + 1 <= Main.maxTilesY - 1)
            {
                Tile tile2 = Main.tile[i, j + 1];
                mergeBottom = mergeTypes.Contains(tile2.WallType) && (showInvisible || !tile2.IsWallInvisible);
            }
            #endregion

            #region Simple pieces (single width, no diagonals in play)
            //Only 1 bit is on (aka only 1 connection)
            if (style == (style & -style))
            {
                wallFrameX = 36 * frameNumber;
                if (style == 1 && mergeTop)
                {
                    wallFrameY = 5 * 36;
                    return false;
                }
                else if (style == 2 && mergeLeft)
                {
                    wallFrameY = 6 * 36;
                    return false;
                }
                else if (style == 4 && mergeRight)
                {
                    wallFrameY = 7 * 36;
                    return false;
                }
                else if (style == 8 && mergeBottom)
                {
                    wallFrameY = 8 * 36;
                    return false;
                }
                return true;
            }

            //1 thin connection
            if (style == 9 || style == 6)
            {
                wallFrameX = 36 * (3 + frameNumber);
                if (style == 9 && (mergeTop || mergeBottom))
                {
                    wallFrameY = 4 * 36;
                    if (mergeTop)
                        wallFrameY += 36;
                    if (mergeBottom)
                        wallFrameY += 36 * 2;
                    return false;
                }
                else if (style == 6 && (mergeLeft || mergeRight))
                {
                    wallFrameY = 7 * 36;
                    if (mergeLeft)
                        wallFrameY += 36;
                    if (mergeRight)
                        wallFrameY += 36 * 2;
                    return false;
                }

                return true;
            }
            #endregion

            #region Initialize diagonal merge
            bool mergeTopLeft = false;
            bool mergeTopRight = false;
            bool mergeBottomLeft = false;
            bool mergeBottomRight = false;
            if (j - 1 >= 0 && i - 1 >= 0)
            {
                Tile tile2 = Main.tile[i - 1, j - 1];
                mergeTopLeft = mergeTypes.Contains(tile2.WallType) && (showInvisible || !tile2.IsWallInvisible);
            }
            if (j - 1 >= 0 && i + 1 <= Main.maxTilesX - 1)
            {
                Tile tile2 = Main.tile[i + 1, j - 1];
                mergeTopRight = mergeTypes.Contains(tile2.WallType) && (showInvisible || !tile2.IsWallInvisible);
            }
            if (j + 1 <= Main.maxTilesY - 1 && i - 1 >= 0)
            {
                Tile tile2 = Main.tile[i - 1, j + 1];
                mergeBottomLeft = mergeTypes.Contains(tile2.WallType) && (showInvisible || !tile2.IsWallInvisible);
            }
            if (j + 1 <= Main.maxTilesY - 1 && i + 1 <= Main.maxTilesX - 1)
            {
                Tile tile2 = Main.tile[i + 1, j + 1];
                mergeBottomRight = mergeTypes.Contains(tile2.WallType) && (showInvisible || !tile2.IsWallInvisible);
            }
            #endregion

            #region Corner pieces
            //Corner pieces
            if (style == 3 || style == 5 || style == 10 || style == 12)
            {
                if (style == 3)
                {
                    //Directly connected
                    if (mergeTop || mergeLeft)
                    {
                        wallFrameY = 8 * 36;
                        wallFrameX = (3 + frameNumber) * 36;
                        if (mergeLeft)
                            wallFrameX += 36 * 3;
                        if (mergeTop)
                            wallFrameX += 36 * 6;
                        return false;
                    }
                    //Corner
                    else if (mergeTopLeft)
                    {
                        wallFrameX = frameNumber * 36;
                        wallFrameY = 36 * 26;
                        return false;
                    }
                }
                else if (style == 5)
                {
                    //Directly connected
                    if (mergeTop || mergeRight)
                    {
                        wallFrameY = 7 * 36;
                        wallFrameX = (3 + frameNumber) * 36;
                        if (mergeRight)
                            wallFrameX += 36 * 3;
                        if (mergeTop)
                            wallFrameX += 36 * 6;
                        return false;
                    }
                    //Corner
                    else if (mergeTopRight)
                    {
                        wallFrameX = frameNumber * 36;
                        wallFrameY = 36 * 25;
                    }
                }
                else if (style == 10)
                {
                    //Directly connected
                    if (mergeBottom || mergeLeft)
                    {
                        wallFrameY = 5 * 36;
                        wallFrameX = (3 + frameNumber) * 36;
                        if (mergeLeft)
                            wallFrameX += 36 * 3;
                        if (mergeBottom)
                            wallFrameX += 36 * 6;
                        return false;
                    }
                    //Corner
                    else if (mergeBottomLeft)
                    {
                        wallFrameX = frameNumber * 36;
                        wallFrameY = 36 * 23;
                    }
                }
                else if (style == 12)
                {
                    //Directly connected
                    if (mergeBottom || mergeRight)
                    {
                        wallFrameY = 6 * 36;
                        wallFrameX = (3 + frameNumber) * 36;
                        if (mergeRight)
                            wallFrameX += 36 * 3;
                        if (mergeBottom)
                            wallFrameX += 36 * 6;
                        return false;
                    }
                    //Corner
                    else if (mergeBottomRight)
                    {
                        wallFrameX = frameNumber * 36;
                        wallFrameY = 36 * 24;
                    }
                }

                return true;
            }
            #endregion

            #region Three connected pieces
            //3-connection pieces
            if (style != 15)
            {
                int wallX = (-3 + frameNumber) * 36;
                bool modifiedFrame = false;

                //Surrounded on all sides but the top
                if (style == 14)
                {
                    wallFrameY = 11 * 36;
                    if (mergeLeft || mergeRight || mergeBottom)
                    {
                        if (mergeBottom)
                            wallX += 36 * 3;
                        if (mergeLeft)
                            wallX += 36 * 6;
                        if (mergeRight)
                            wallX += 36 * 12;

                        wallFrameX = wallX;
                        modifiedFrame = true;
                    }
                    //Corner check with no connection
                    else if (mergeBottomLeft || mergeBottomRight)
                    {
                        wallFrameX = (9 + frameNumber) * 36;
                        wallFrameY = 19 * 36;

                        if (mergeBottomLeft)
                            wallFrameX += 36 * 3;
                        if (mergeBottomRight)
                            wallFrameX += 36 * 6;

                        modifiedFrame = true;
                    }

                    //Corner check with 1 connection
                    if (!mergeBottom && (mergeLeft ^ mergeRight))
                    {
                        if (mergeLeft && mergeBottomRight)
                        {
                            wallFrameX = 36 * (3 + frameNumber);
                            wallFrameY = 36 * 23;
                            modifiedFrame = true;
                        }
                        else if (mergeRight && mergeBottomLeft)
                        {
                            wallFrameX = 36 * (6 + frameNumber);
                            wallFrameY = 36 * 23;
                            modifiedFrame = true;
                        }
                    }
                }
                //Surrounded on all sides but the bottom
                else if (style == 7)
                {
                    wallFrameY = 12 * 36;
                    if (mergeLeft || mergeRight || mergeTop)
                    {
                        if (mergeTop)
                            wallX += 36 * 3;
                        if (mergeLeft)
                            wallX += 36 * 6;
                        if (mergeRight)
                            wallX += 36 * 12;

                        wallFrameX = wallX;
                        modifiedFrame = true;
                    }
                    //Corner check with no connection
                    else if (mergeTopLeft || mergeTopRight)
                    {
                        wallFrameX = (9 + frameNumber) * 36;
                        wallFrameY = 20 * 36;

                        if (mergeTopLeft)
                            wallFrameX += 36 * 3;
                        if (mergeTopRight)
                            wallFrameX += 36 * 6;

                        modifiedFrame = true;
                    }

                    //Corner check with 1 connection
                    if (!mergeTop && (mergeLeft ^ mergeRight))
                    {
                        if (mergeLeft && mergeTopRight)
                        {
                            wallFrameX = 36 * (3 + frameNumber);
                            wallFrameY = 36 * 24;
                            modifiedFrame = true;
                        }
                        else if (mergeRight && mergeTopLeft)
                        {
                            wallFrameX = 36 * (6 + frameNumber);
                            wallFrameY = 36 * 24;
                            modifiedFrame = true;
                        }
                    }
                }
                //Surrounded on all sides but the left
                else if (style == 13)
                {
                    wallFrameY = 13 * 36;
                    if (mergeBottom || mergeRight || mergeTop)
                    {
                        if (mergeRight)
                            wallX += 36 * 3;
                        if (mergeBottom)
                            wallX += 36 * 6;
                        if (mergeTop)
                            wallX += 36 * 12;

                        wallFrameX = wallX;
                        modifiedFrame = true;
                    }
                    //Corner check with no connection
                    else if (mergeTopRight || mergeBottomRight)
                    {
                        wallFrameX = (9 + frameNumber) * 36;
                        wallFrameY = 21 * 36;

                        if (mergeTopRight)
                            wallFrameX += 36 * 3;
                        if (mergeBottomRight)
                            wallFrameX += 36 * 6;

                        modifiedFrame = true;
                    }

                    //Corner check with 1 connection
                    if (!mergeRight && (mergeTop ^ mergeBottom))
                    {
                        if (mergeTop && mergeBottomRight)
                        {
                            wallFrameX = 36 * (3 + frameNumber);
                            wallFrameY = 36 * 25;
                            modifiedFrame = true;
                        }
                        else if (mergeBottom && mergeTopRight)
                        {
                            wallFrameX = 36 * (6 + frameNumber);
                            wallFrameY = 36 * 25;
                            modifiedFrame = true;
                        }
                    }
                }
                //Surrounded on all sides but the right
                else if (style == 11)
                {
                    wallFrameY = 14 * 36;
                    if (mergeBottom || mergeLeft || mergeTop)
                    {
                        if (mergeLeft)
                            wallX += 36 * 3;
                        if (mergeBottom)
                            wallX += 36 * 6;
                        if (mergeTop)
                            wallX += 36 * 12;

                        wallFrameX = wallX;
                        modifiedFrame = true;
                    }
                    //Corner check with no connection
                    else if (mergeTopLeft || mergeBottomLeft)
                    {
                        wallFrameX = (9 + frameNumber) * 36;
                        wallFrameY = 22 * 36;

                        if (mergeTopLeft)
                            wallFrameX += 36 * 3;
                        if (mergeBottomLeft)
                            wallFrameX += 36 * 6;

                        modifiedFrame = true;
                    }

                    //Corner check with 1 connection
                    if (!mergeLeft && (mergeTop ^ mergeBottom))
                    {
                        if (mergeTop && mergeBottomLeft)
                        {
                            wallFrameX = 36 * (3 + frameNumber);
                            wallFrameY = 36 * 26;
                            modifiedFrame = true;
                        }
                        else if (mergeBottom && mergeTopLeft)
                        {
                            wallFrameX = 36 * (6 + frameNumber);
                            wallFrameY = 36 * 26;
                            modifiedFrame = true;
                        }
                    }
                }

                return !modifiedFrame;
            }
            #endregion

            #region All connected
            //All sides
            #region Surrounded and without possibility of corners

            //Fully covered
            if (mergeLeft && mergeRight && mergeTop && mergeBottom)
            {
                wallFrameX = (3 + frameNumber) * 36;
                wallFrameY = 17 * 36;
                return false;
            }

            //Covered on 2 opposite sides
            if (!mergeLeft && !mergeRight && mergeTop && mergeBottom)
            {
                wallFrameX = (3 + frameNumber) * 36;
                wallFrameY = 15 * 36;
                return false;
            }
            if (mergeLeft && mergeRight && !mergeTop && !mergeBottom)
            {
                wallFrameX = (3 + frameNumber) * 36;
                wallFrameY = 16 * 36;
                return false;
            }

            //Covered on 3 sides
            if (mergeLeft && mergeRight && mergeTop && !mergeBottom)
            {
                wallFrameX = (9 + frameNumber) * 36;
                wallFrameY = 15 * 36;
                return false;
            }
            if (mergeLeft && !mergeRight && mergeTop && mergeBottom)
            {
                wallFrameX = (9 + frameNumber) * 36;
                wallFrameY = 16 * 36;
                return false;
            }
            if (!mergeLeft && mergeRight && mergeTop && mergeBottom)
            {
                wallFrameX = (9 + frameNumber) * 36;
                wallFrameY = 17 * 36;
                return false;
            }
            if (mergeLeft && mergeRight && !mergeTop && mergeBottom)
            {
                wallFrameX = (9 + frameNumber) * 36;
                wallFrameY = 18 * 36;
                return false;
            }
            #endregion

            #region not covered besides corners
            if (!mergeLeft && !mergeTop && !mergeRight && !mergeBottom)
            {
                //Entirely unsurrounded
                if (!mergeBottomLeft && !mergeTopLeft && !mergeBottomRight && !mergeTopRight)
                {
                    //Random vairant
                    if (WorldGen.genRand.NextBool(9))
                    {
                        wallFrameY = 4 * 36;
                        wallFrameX = (9 + frameNumber) * 36;
                        return false;
                    }
                    return true;
                }

                if (mergeTopLeft)
                {
                    if (!mergeTopRight && !mergeBottomLeft && !mergeBottomRight)
                    {
                        wallFrameY = 19 * 36;
                        wallFrameX = frameNumber * 36;
                        return false;
                    }
                    else if (mergeTopRight && !mergeBottomLeft && !mergeBottomRight)
                    {
                        wallFrameY = 19 * 36;
                        wallFrameX = (3 + frameNumber) * 36;
                        return false;
                    }
                    else if (mergeTopRight && !mergeBottomLeft && mergeBottomRight)
                    {
                        wallFrameY = 19 * 36;
                        wallFrameX = (6 + frameNumber) * 36;
                        return false;
                    }
                    else if (mergeTopRight && mergeBottomLeft && mergeBottomRight)
                    {
                        wallFrameY = 19 * 36;
                        wallFrameX = (9 + frameNumber) * 36;
                        return false;
                    }
                    else if (!mergeTopRight && !mergeBottomLeft && mergeBottomRight)
                    {
                        wallFrameY = 20 * 36;
                        wallFrameX = (9 + frameNumber) * 36;
                        return false;
                    }
                    else if (!mergeTopRight && mergeBottomLeft && mergeBottomRight)
                    {
                        wallFrameY = 21 * 36;
                        wallFrameX = (6 + frameNumber) * 36;
                        return false;
                    }
                    else if (mergeTopRight && mergeBottomLeft && !mergeBottomRight)
                    {
                        wallFrameY = 22 * 36;
                        wallFrameX = (6 + frameNumber) * 36;
                        return false;
                    }
                    else if (!mergeTopRight && mergeBottomLeft && !mergeBottomRight)
                    {
                        wallFrameY = 22 * 36;
                        wallFrameX = (3 + frameNumber) * 36;
                        return false;
                    }
                }
                else if (mergeTopRight)
                {
                    if (!mergeBottomLeft && !mergeBottomRight)
                    {
                        wallFrameY = 20 * 36;
                        wallFrameX = frameNumber * 36;
                        return false;
                    }
                    else if (!mergeBottomLeft && mergeBottomRight)
                    {
                        wallFrameY = 20 * 36;
                        wallFrameX = (3 + frameNumber) * 36;
                        return false;
                    }
                    else if (mergeBottomLeft && mergeBottomRight)
                    {
                        wallFrameY = 20 * 36;
                        wallFrameX = (6 + frameNumber) * 36;
                        return false;
                    }
                    else if (mergeBottomLeft && !mergeBottomRight)
                    {
                        wallFrameY = 21 * 36;
                        wallFrameX = (9 + frameNumber) * 36;
                        return false;
                    }

                }
                else if (mergeBottomRight)
                {
                    if (!mergeBottomLeft)
                    {
                        wallFrameY = 21 * 36;
                        wallFrameX = frameNumber * 36;
                        return false;
                    }
                    else
                    {
                        wallFrameY = 21 * 36;
                        wallFrameX = (3 + frameNumber) * 36;
                        return false;
                    }
                }
                else
                {
                    wallFrameY = 22 * 36;
                    wallFrameX = frameNumber * 36;
                    return false;
                }
            }

            #endregion

            #region L shapes
            //Corn
            else if (mergeLeft && mergeTop)
            {
                if (!mergeBottomRight)
                {
                    wallFrameY = 15 * 36;
                    wallFrameX = (6 + frameNumber) * 36;
                }
                else
                {
                    wallFrameY = 24 * 36;
                    wallFrameX = (18 + frameNumber) * 36;
                }
                return false;
            }
            else if (mergeLeft && mergeBottom)
            {
                if (!mergeTopRight)
                {
                    wallFrameY = 16 * 36;
                    wallFrameX = (6 + frameNumber) * 36;
                }
                else
                {
                    wallFrameY = 23 * 36;
                    wallFrameX = (18 + frameNumber) * 36;
                }
                return false;
            }
            else if (mergeTop && mergeRight)
            {
                if (!mergeBottomLeft)
                {
                    wallFrameY = 17 * 36;
                    wallFrameX = (6 + frameNumber) * 36;
                }
                else
                {
                    wallFrameY = 25 * 36;
                    wallFrameX = (18 + frameNumber) * 36;
                }
                return false;
            }
            else if (mergeRight && mergeBottom)
            {
                if (!mergeTopLeft)
                {
                    wallFrameY = 18 * 36;
                    wallFrameX = (6 + frameNumber) * 36;
                }
                else
                {
                    wallFrameY = 26 * 36;
                    wallFrameX = (18 + frameNumber) * 36;
                }
                return false;
            }
            #endregion

            #region only 1 adjacent side
            else if (mergeTop)
            {
                if (!mergeBottomLeft && !mergeBottomRight)
                {
                    wallFrameY = 15 * 36;
                    wallFrameX = (0 + frameNumber) * 36;
                }
                else
                {
                    wallFrameY = 25 * 36;
                    wallFrameX = (6 + frameNumber) * 36;
                    if (mergeBottomRight)
                        wallFrameX += 3 * 36;
                    if (mergeBottomLeft)
                        wallFrameX += 6 * 36;
                }
                return false;
            }

            else if (mergeLeft)
            {
                if (!mergeTopRight && !mergeBottomRight)
                {
                    wallFrameY = 16 * 36;
                    wallFrameX = (0 + frameNumber) * 36;
                }
                else
                {
                    wallFrameY = 24 * 36;
                    wallFrameX = (6 + frameNumber) * 36;
                    if (mergeTopRight)
                        wallFrameX += 3 * 36;
                    if (mergeBottomRight)
                        wallFrameX += 6 * 36;
                }
                return false;
            }

            else if (mergeRight)
            {
                if (!mergeTopLeft && !mergeBottomLeft)
                {
                    wallFrameY = 17 * 36;
                    wallFrameX = (0 + frameNumber) * 36;
                }
                else
                {
                    wallFrameY = 26 * 36;
                    wallFrameX = (6 + frameNumber) * 36;
                    if (mergeBottomLeft)
                        wallFrameX += 3 * 36;
                    if (mergeTopLeft)
                        wallFrameX += 6 * 36;
                }
                return false;
            }

            else if (mergeBottom)
            {
                if (!mergeTopLeft && !mergeTopRight)
                {
                    wallFrameY = 18 * 36;
                    wallFrameX = (0 + frameNumber) * 36;
                }
                else
                {
                    wallFrameY = 23 * 36;
                    wallFrameX = (6 + frameNumber) * 36;
                    if (mergeTopLeft)
                        wallFrameX += 3 * 36;
                    if (mergeTopRight)
                        wallFrameX += 6 * 36;
                }
                return false;
            }


            #endregion
            #endregion
            return true;
        }

        public override bool CanPlace(int i, int j)
        {
            return !wallTypes.Contains(Main.tile[i, j].WallType);
        }
    }
}
