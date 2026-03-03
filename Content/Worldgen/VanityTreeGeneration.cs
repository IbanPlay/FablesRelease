using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Content.Tiles.VanityTrees;
using Terraria.GameContent.Biomes.CaveHouse;
using Terraria.WorldBuilding;
using static CalamityFables.Core.FablesGeneralSystemHooks;

namespace CalamityFables.Core
{
    public partial class FablesWorld : ModSystem
    {

        public static byte celebrationRainbowPaint = 0;

        public static void GenerateExtraVanityTrees()
        {
            int vanityTreeChanceCorruption = 60;
            int vanityTreeChanceCrimson = 62;
            int vanityTreeChanceSnow = 18;
            int minSuccessfulTreeSpacing = 12;
            celebrationRainbowPaint = 0;
            if (Main.tenthAnniversaryWorld)
                celebrationRainbowPaint = 1;

            if (WorldGen.drunkWorldGen)
            {
                vanityTreeChanceCorruption /= 3;
                vanityTreeChanceCrimson /= 3;
                vanityTreeChanceSnow /= 3;
            }
            if (WorldGen.remixWorldGen)
            {
                vanityTreeChanceCorruption /= 2;
                vanityTreeChanceCrimson /= 2;
                vanityTreeChanceSnow /= 2;
            }

            for (int i = 10; i < Main.maxTilesX - 10; i++)
            {
                bool successfullyGrownTree = false;
                int biome = 0;

                for (int j = 30; j < Main.worldSurface; j++)
                {

                    if (!Main.tile[i, j].HasTile)
                        continue;


                    if (Main.tile[i, j].TileType == TileID.CorruptGrass && (Main.tenthAnniversaryWorld || WorldGen.genRand.NextBool(vanityTreeChanceCorruption)))
                    {
                        if (WorldGen.genRand.NextBool())
                            successfullyGrownTree = PlopDownVanityTree(i, j, InkyTree.PreGrowChecks, InkyTree.GrowTree, 5, InkyTree.MIN_TREE_HEIGHT, InkyTree.MAX_TREE_HEIGHT, false);

                        else
                            successfullyGrownTree = PlopDownVanityTree(i, j, MallowTree.PreGrowChecks, MallowTree.GrowTree, 2, MallowTree.MIN_TREE_HEIGHT, MallowTree.MAX_TREE_HEIGHT, true);

                        biome = 1;
                        break;
                    }

                    if (Main.tile[i, j].TileType == TileID.CrimsonGrass && (Main.tenthAnniversaryWorld || WorldGen.genRand.NextBool(vanityTreeChanceCrimson)))
                    {
                        if (WorldGen.genRand.NextBool())
                            successfullyGrownTree = PlopDownVanityTree(i, j, MarrowTree.PreGrowChecks, MarrowTree.GrowTree, 3, MarrowTree.MIN_TREE_HEIGHT, MarrowTree.MAX_TREE_HEIGHT, false);
                        else
                            successfullyGrownTree = PlopDownVanityTree(i, j, SpiderTree.PreGrowChecks, SpiderTree.GrowTree, 6, SpiderTree.MIN_TREE_HEIGHT, SpiderTree.MAX_TREE_HEIGHT, true);

                        biome = 2;
                        break;
                    }

                    if (Main.tile[i, j].TileType == TileID.Crimsand && !Main.tile[i, j - 1].HasTile && WorldGen.genRand.NextBool(vanityTreeChanceCrimson * 2))
                    {
                        Tile tileLeft = Main.tile[i - 1, j];
                        Tile tileRight = Main.tile[i + 1, j];

                        //Helps it generate on the ups and downs of the desert
                        if (tileLeft.HasTile && SpiderTree.ValidTileToGrowOn(tileLeft.TileType) && tileRight.HasTile && SpiderTree.ValidTileToGrowOn(tileRight.TileType))
                        {
                            tileLeft.IsHalfBlock = false;
                            tileLeft.Slope = SlopeType.Solid;
                            tileRight.IsHalfBlock = false;
                            tileRight.Slope = SlopeType.Solid;
                        }

                        successfullyGrownTree = PlopDownVanityTree(i, j, SpiderTree.PreGrowChecks, SpiderTree.GrowTree, 6, SpiderTree.MIN_TREE_HEIGHT, SpiderTree.MAX_TREE_HEIGHT, true);
                        
                        biome = 3;
                        break;
                    }

                    if ((Main.tile[i, j].TileType == TileID.SnowBlock || 
                        Main.tile[i, j].TileType == TileID.IceBlock || 
                        Main.tile[i, j].TileType == TileID.CorruptIce || 
                        Main.tile[i, j].TileType == TileID.FleshIce)
                        && !Main.tile[i, j - 1].HasTile 
                        && (WorldGen.genRand.NextBool(vanityTreeChanceSnow) || Main.tenthAnniversaryWorld))
                    {
                        Tile tileLeft = Main.tile[i - 1, j];
                        Tile tileRight = Main.tile[i + 1, j];

                        if (WorldGen.genRand.NextBool())
                        {
                            //Helps it generate with its wide base
                            if (tileLeft.HasTile && WisteriaTree.ValidTileToGrowOn(tileLeft) && tileRight.HasTile && WisteriaTree.ValidTileToGrowOn(tileRight))
                            {
                                tileLeft.IsHalfBlock = false;
                                tileLeft.Slope = SlopeType.Solid;
                                tileRight.IsHalfBlock = false;
                                tileRight.Slope = SlopeType.Solid;
                            }

                            successfullyGrownTree = PlopDownVanityTree(i, j, WisteriaTree.PreGrowChecks, WisteriaTree.GrowTree, 3, WisteriaTree.MIN_TREE_HEIGHT, WisteriaTree.MAX_TREE_HEIGHT, false);
                        }
                        else
                            successfullyGrownTree = PlopDownVanityTree(i, j, FrostTree.PreGrowChecks, FrostTree.GrowTree, 2, FrostTree.MIN_TREE_HEIGHT + FrostTree.TREETOP_HEIGHT, FrostTree.MAX_TREE_HEIGHT + FrostTree.TREETOP_HEIGHT, false);

                        i += 1;
                        break;
                    }
                }

                if (successfullyGrownTree)
                {
                    if (!Main.tenthAnniversaryWorld)
                    {
                        //Corruption
                        if (biome == 1)
                            i += minSuccessfulTreeSpacing + 1;
                        //Crimson, more spaced out because of the thick trees
                        else if (biome == 2)
                            i += minSuccessfulTreeSpacing + 3;
                        //Lower spacing for spider trees in crimson desert
                        else if (biome == 3)
                            i += 3;
                        //Snow
                        else
                            i += minSuccessfulTreeSpacing + 4;
                    }
                    else
                    {
                        celebrationRainbowPaint = (byte)(celebrationRainbowPaint + 1);
                        if (celebrationRainbowPaint > 12)
                            celebrationRainbowPaint = 1;

                        i += 2;
                    }
                }

                if (WorldGen.genRand.NextBool(3))
                    i++;
                if (WorldGen.genRand.NextBool(4))
                    i++;
            }
        }

        public delegate bool PreGrowModdedSaplingDelegate(int i, int j, int height, int widthClearance = 2, bool ignoreTrees = false);

        /// <summary>
        /// Places down an evil vanity tree at the coordinates. Use width clearance for it to remove adjacent regular trees
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="widthClearance"></param>
        public static bool PlopDownVanityTree(int i, int j, PreGrowModdedSaplingDelegate preGrowCheck, GrowModdedSaplingDelegate growMethod, int widthClearance, int minHeight, int maxHeight, bool callRandomUpdates)
        {
            int height = WorldGen.genRand.Next(minHeight, maxHeight + 1);

            //FablesUtils.LogILEpicFail("celebrationmk10 vanity trees", $"Placing tree at coordinates {i} {j}, with height params of min {minHeight} and max {maxHeight}");
           // CalamityFables.Instance.Logger.Debug($"Generating vanity tree at {i},{j} , with height range of {minHeight}-{maxHeight}");

           // if (i == 1095 && j == 411)
           //     CalamityFables.Instance.Logger.Debug($"Vie de merde!");


            //Couldn't grow tree
            if (!preGrowCheck(i, j, height, ignoreTrees: true))
                return false;

            //CalamityFables.Instance.Logger.Debug($"Passed pre growth check for tree at {i},{j}");

            //Clear more regular trees around the special trees on anniversarymk10
            if (Main.tenthAnniversaryWorld)
                widthClearance += 4;

            //Remove nearby trees to avoid everything being too snug
            for (int x = i - widthClearance; x < i + widthClearance; x++)
            {
                for (int y = j - 3; y < j + 3; y++)
                {
                    Tile t = Main.tile[x, y];
                    if (t.HasTile && t.TileType == TileID.Trees)
                        WorldGen.KillTile(x, y);
                }
            }

            if (Main.tenthAnniversaryWorld && WorldGen.genRand.NextBool(4))
                return false;


            bool success = growMethod(i, j, false, false, height);
            if (success && callRandomUpdates)
            {
                //Force growth of the tiny blossoms
                for (int t = 0; t < 10; t++)
                    TileLoader.RandomUpdate(i, j - 1, Main.tile[i, j-1].TileType);
            }


           // CalamityFables.Instance.Logger.Debug($"Successfully grew tree at {i},{j}");

            return success;
        }


        public static bool TreeTileClearanceCheck(int startX, int endX, int startY, int endY, bool ignoreTrees = false)
        {
            if (startX < 0 || startY < 0 || endX >= Main.maxTilesX || endY >= Main.maxTilesY)
                return false;

            for (int i = startX; i < endX + 1; i++)
            {
                for (int j = startY; j < endY + 1; j++)
                {
                    if (!Main.tile[i, j].HasTile)
                        continue;

                    int tileType = Main.tile[i, j].TileType;

                    if (TileID.Sets.BreakableWhenPlacing[tileType] && (!WorldGen.generatingWorld || !Main.tenthAnniversaryWorld))
                        continue;

                    if (TileID.Sets.CommonSapling[tileType] || TileID.Sets.IgnoredByGrowingSaplings[tileType])
                        continue;

                    if (ignoreTrees && tileType == TileID.Trees)
                        continue;

                    return false;

                }
            }

            return true;
        }
    }

}