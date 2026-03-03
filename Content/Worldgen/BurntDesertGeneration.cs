using CalamityFables.Content.Tiles.BurntDesert;
using Terraria;
using Terraria.DataStructures;
using Terraria.WorldBuilding;

namespace CalamityFables.Core
{
    public partial class FablesWorld : ModSystem
    {
        public static int[] saltTilesMask;

        #region Scourge skeletons
        public static void FillDesertWithScourgeSkeletons()
        {
            int desertWidth = GenVars.UndergroundDesertLocation.Width;
            List<Point> desertTopography = new List<Point>(desertWidth);

            bool[] validDesertSurfaceTiles = TileID.Sets.Factory.CreateBoolSet(TileID.Sand, TileID.HardenedSand, TileID.Sandstone );

            int i = -1;

            int minSkeletonX = GenVars.UndergroundDesertLocation.X;
            int maxSkeletonX = GenVars.UndergroundDesertLocation.X + GenVars.UndergroundDesertLocation.Width;


            int headCount = 0;
            if (!WorldGen.genRand.NextBool(4))
                headCount++;
            if (WorldGen.genRand.NextBool(6))
                headCount++;
            int skeletonCount = headCount + WorldGen.genRand.Next(1, 3) * headCount;


            if (CalamityFables.SpiritEnabled)
            {
                if (ModContent.TryFind("SpiritReforged/SavannaGrass", out ModTile savannaGrass))
                {
                    Rectangle savannaArea = (Rectangle)CalamityFables.SpiritReforged.Call("GetSavannaArea");
                    if (!savannaArea.IsEmpty)
                    {
                        //We can place scourge skeletons ontop of savanna grass
                        validDesertSurfaceTiles[savannaGrass.Type] = true;

                        //Extend the area towards the savanna
                        if (savannaArea.X < minSkeletonX)
                            minSkeletonX = Math.Max(minSkeletonX - 200, savannaArea.X);

                        if (savannaArea.X + savannaArea.Width > maxSkeletonX)
                            maxSkeletonX = Math.Min(maxSkeletonX + 200, savannaArea.X + savannaArea.Width);

                        skeletonCount += 1;
                        if (WorldGen.genRand.NextBool())
                            headCount++;
                    }
                }

                if (ModContent.TryFind("SpiritReforged/SaltBlockReflective", out ModTile saltReflective) && ModContent.TryFind("SpiritReforged/SaltBlockDull", out ModTile saltDull))
                {
                    Rectangle saltArea = (Rectangle)CalamityFables.SpiritReforged.Call("GetSaltFlatsArea");
                    if (!saltArea.IsEmpty)
                    {
                        //We can place scourge skeletons ontop of salt
                        validDesertSurfaceTiles[saltReflective.Type] = true;
                        validDesertSurfaceTiles[saltDull.Type] = true;

                        //Extend the area towards the salt flats
                        if (saltArea.X < minSkeletonX)
                            minSkeletonX = Math.Max(minSkeletonX - 200, saltArea.X);

                        if (saltArea.X + saltArea.Width > maxSkeletonX)
                            maxSkeletonX = Math.Min(maxSkeletonX + 200, saltArea.X + saltArea.Width);

                        skeletonCount ++;
                        if (WorldGen.genRand.NextBool())
                            headCount++;
                    }
                }
            }


            for (int x = minSkeletonX; x < maxSkeletonX; x++)
            {
                i++;
                for (int y = (int)(Main.worldSurface * 0.35f); y < Main.worldSurface + 10; y++)
                {
                    if (Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType])
                    {
                        if (validDesertSurfaceTiles[Main.tile[x, y].TileType])
                            desertTopography.Add(new Point(x, y));
                        break;
                    }
                }
            }

            desertTopography.RemoveAll(p => p == new Point(0, 0));


            for (int j = 0; j < skeletonCount; j++)
            {
                if (desertTopography.Count < 30)
                    break;

                //Find a random tile in the desert to place down a fossil
                int potentialStartTileIndex = WorldGen.genRand.Next(desertTopography.Count - 30) + 15;
                Point potentialStartTile = desertTopography[potentialStartTileIndex];

                //Rarely, spines can go "big mode"
                bool hasHead = headCount > 0;
                bool bigMode = WorldGen.genRand.NextBool(5) && !hasHead;

                if (GenerateScourgeSkeleton(potentialStartTile, bigMode, hasHead, desertTopography) && hasHead)
                {
                    headCount--;

                    List<Point> tilesNearTheHead = desertTopography.FindAll(p =>
                    {
                        int distanceToTile = Math.Abs(p.X - potentialStartTile.X);
                        return distanceToTile <= 25;
                    });

                    if (tilesNearTheHead.Count > 0)
                    {
                        potentialStartTile = tilesNearTheHead[WorldGen.genRand.Next(tilesNearTheHead.Count)];
                        bigMode = WorldGen.genRand.NextBool(5);
                        if (GenerateScourgeSkeleton(potentialStartTile, bigMode, false, desertTopography, potentialStartTile))
                            j++;
                    }
                }
            }
        }

        public static bool GenerateScourgeSkeleton(Point originTile, bool largeSize, bool hasHead, List<Point> topography, Point? fromHead = null)
        {
            int maxTileDistance = largeSize ? 20 : 15;
            int minTileDistance = largeSize ? 15 : 8;

            //Get all nearby tiles
            List<Point> tilesNearby = topography.FindAll(p =>
            {
                int distanceToTile = Math.Abs(p.X - originTile.X);
                return (distanceToTile > minTileDistance && distanceToTile <= maxTileDistance);
            });

            //If no tile matched the search, keep going
            if (tilesNearby.Count <= 0)
                return false;
            
            //Select a random tile nearby to attach the other end of the fossil
            Point endTile = tilesNearby[WorldGen.genRand.Next(tilesNearby.Count)];
            int distanceToEnd = Math.Abs(endTile.X - originTile.X);

            topography.RemoveAll(p => Math.Abs(p.X - originTile.X) < (distanceToEnd + 5)); //Clear the tiles nearby to avoid overlapping segments

            //Place the tile entity
            if (hasHead)
                endTile.Y -= (int)(distanceToEnd * WorldGen.genRand.NextFloat(0.7f, 1.4f));

            //Following from a head, we need to go in the same direction
            if (fromHead.HasValue && Math.Abs(fromHead.Value.X - endTile.X) > Math.Abs(fromHead.Value.X - originTile.X))
            {
                Point cache = originTile;
                originTile = endTile;
                endTile = cache;
            }

            PlaceScourgeSkeleton(originTile, endTile, hasHead, WorldGen.genRand.Next(100));

            return true;
        }

        public static bool PlaceScourgeSkeleton(Point originTile, Point endTile, bool hasHead, int curvatureOffset, int? randomSeed = null)
        {
            //Place the tile entity
            int rand = randomSeed ?? WorldGen.genRand.Next(int.MaxValue - 24);

            int spineID = ModContent.GetInstance<ScourgeSpineDecor>().Place(originTile.X, originTile.Y);
            ScourgeSpineDecor placedSpine = TileEntity.ByID[spineID] as ScourgeSpineDecor;

            placedSpine.hasHead = hasHead;
            placedSpine.hasTail = !Main.tile[endTile].HasTile && !hasHead;
            placedSpine.randomSeed = rand;
            placedSpine.PlayerPlaced = !WorldGen.generatingWorld;

            if (Main.tile[endTile].HasTile)
                placedSpine.salty = (byte)saltTilesMask[Main.tile[endTile].TileType];
            if (Main.tile[originTile].HasTile)
                placedSpine.salty = Math.Max(placedSpine.salty, (byte)saltTilesMask[Main.tile[originTile].TileType]);

            placedSpine.SetControlPoints(endTile, curvatureOffset);
            return true;
        }       
        #endregion

        #region Scourgekiller painting
        public static void GenerateScourgekillerPainting()
        {
            List<Point> largePaintings = new List<Point>();

            for (int x = GenVars.UndergroundDesertLocation.X; x < GenVars.UndergroundDesertLocation.X + GenVars.UndergroundDesertLocation.Width; x++)
            {
                for (int y = GenVars.UndergroundDesertLocation.Y; y < GenVars.UndergroundDesertLocation.Y + GenVars.UndergroundDesertLocation.Height; y++)
                {
                    if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.Painting6X4)
                    {
                        int yVariant = Main.tile[x, y].TileFrameY / (18 * 4);

                        if (Main.tile[x, y].TileFrameX == 6 * 18 && yVariant >= 10 && yVariant < 16)
                        {
                            largePaintings.Add(new Point(x, y));
                        }
                    }
                }
            }

            if (largePaintings.Count == 0)
                return;

            //Replace the 6x4 painting that is the closest to the center of the desert
            Vector2 topCenterOfDesert = new Vector2(GenVars.UndergroundDesertLocation.X + GenVars.UndergroundDesertLocation.Width / 2, GenVars.UndergroundDesertLocation.Y);

            Point closestPoint = largePaintings[0];
            float distanceToTopCenter = closestPoint.ToVector2().Distance(topCenterOfDesert);

            for (int i = 1; i < largePaintings.Count; i++)
            {
                float newDistanceToTopCenter = largePaintings[i].ToVector2().Distance(topCenterOfDesert);
                if (distanceToTopCenter > newDistanceToTopCenter)
                {
                    distanceToTopCenter = newDistanceToTopCenter;
                    closestPoint = largePaintings[i];
                }
            }

            WorldGen.KillTile(closestPoint.X, closestPoint.Y, false, false, true);
            FablesUtils.PlaceMultitile(closestPoint.ToPoint16(), ModContent.TileType<ScourgekillerPaintingTile>());
        }
        #endregion
    }
}