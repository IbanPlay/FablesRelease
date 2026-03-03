namespace CalamityFables.Core
{
    public static partial class AStarPathfinding
    {
        public static bool OffsetPositionsToValidNavigation(TileNavigableDelegate navigation, Vector2 start, Vector2 end, int startIterations, int endIterations, out Point startPoint, out Point endPoint)
        {
            startPoint = start.ToSafeTileCoordinates();
            endPoint = end.ToSafeTileCoordinates();

            return OffsetPositionsToValidNavigation(navigation, ref startPoint, ref endPoint, startIterations, endIterations);
        }

        public static bool OffsetPositionsToValidNavigation(TileNavigableDelegate navigation, ref Point start, ref Point end, int startIterations, int endIterations)
        {
            int maxIterations = startIterations;
            start = OffsetUntilNavigable(start, new Point(0, 1), navigation, ref maxIterations);
            if (maxIterations < 0)
                return false;

            maxIterations = endIterations;
            end = OffsetUntilNavigable(end, new Point(0, 1), navigation, ref maxIterations);
            if (maxIterations < 0)
                return false;

            return true;
        }

        #region Premade navigation checks
        public static int SolidCreatureNavigHeight = 1;
        public static int SolidCreatureNavigWallClimbCheckDown = -1;

        /// <summary>
        /// Simulates the movement of a hypothetical 1x1 creature as being able to walk around on tiles, climb walls, and fall down (including through platforms)  <br/>
        /// Does not check for any space larger than 1x1, nor does it check for the reachability of the goal from the origin point <br/>
        /// Can climb walls infinitely high
        /// </summary>
        public static bool SolidCreatureNavigationSimple(Point p, Point? from, out bool universallyUnnavigable)
        {
            universallyUnnavigable = true;
            Tile t = Main.tile[p];
            bool solidTile = Main.tileSolid[t.TileType];
            bool platform = TileID.Sets.Platforms[t.TileType];

            //Can't navigate inside solid tiles
            if (t.HasUnactuatedTile && !t.IsHalfBlock && !platform && solidTile)
                return false;

            //Can navigate on half tiles and platforms just fine
            if (t.HasUnactuatedTile && (t.IsHalfBlock || platform) && solidTile)
                return true;

            universallyUnnavigable = false;
            for (int i = -1; i <= 1; i++)
                for (int j = 0; j <= 1; j++)
                {
                    //Only cardinal directions here
                    if (j * i != 0 || (j == 0 && i == 0))
                        continue;

                    //IF a neighboring tile is solid we can go on it
                    Tile adjacentTile = Main.tile[p.X + i, p.Y + j];
                    if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                        return true;
                }

            //Can fall straight down just fine
            if (from != null && p.X == from.Value.X && p.Y > from.Value.Y)
                return true;

            return false;
        }

        /// <summary>
        /// Simulates the movement of a hypothetical 1xX creature as being able to walk around on tiles, climb walls, and fall down (including through platforms) <br/>
        /// Checks for clearance of a certain height using the value of <see cref="SolidCreatureNavigHeight"/> for the height <br/>
        /// Can crawl up walls as long as there is floor Y tiles below the point it's at. <see cref="SolidCreatureNavigWallClimbCheckDown"/> is used as Y<br/>
        /// Set that value to 1 to prevent wall crawling altogether, set it to 0 to let it crawl up walls indefinitely <br/>
        /// Does not check for the reachability of the goal from the origin point
        /// </summary>
        public static bool SolidCreatureNavigation(Point p, Point? from, out bool universallyUnnavigable)
        {
            universallyUnnavigable = true;
            Tile t = Main.tile[p];
            bool solidTile = Main.tileSolid[t.TileType];
            bool platform = TileID.Sets.Platforms[t.TileType];

            //Can't navigate inside solid tiles
            if (t.HasUnactuatedTile && !t.IsHalfBlock && !platform && solidTile)
                return false;

            //Can't navigate if you don't have the height clearance
            for (int i = 1; i < SolidCreatureNavigHeight; i++)
            {
                Tile aboveTile = Main.tile[p + new Point(0, -i)];
                if (aboveTile.HasUnactuatedTile && !TileID.Sets.Platforms[aboveTile.TileType] && Main.tileSolid[aboveTile.TileType])
                    return false;
            }


            if (BasicNavigationChecks(p, from, ref universallyUnnavigable))
                return true;

            return false;
        }

        /// <summary>
        /// Simulates the movement of a hypothetical 1xX creature through raycasting to see if it can reach the point from where it is<br/>
        /// If no origin is provided (aka, when used to solely determine validty of the position), acts like <see cref="SolidCreatureNavigation(Point, Point?)"/>
        /// Can crawl up walls as long as there is floor Y tiles below the point it's at. <see cref="SolidCreatureNavigWallClimbCheckDown"/> is used as Y<br/>
        /// Set that value to 1 to prevent wall crawling altogether, set it to 0 to let it crawl up walls indefinitely <br/>
        /// </summary>
        public static bool SolidCreatureNavigationRaycast(Point p, Point? from, out bool universallyUnnavigable)
        {
            universallyUnnavigable = true;
            if (!from.HasValue)
                return SolidCreatureNavigation(p, from, out universallyUnnavigable);


            //Can't navigate if you don't have the height clearance
            for (int i = 0; i < SolidCreatureNavigHeight; i++)
            {
                if (!FablesUtils.RaytraceTo(from.Value.X, from.Value.Y - i, p.X, p.Y - i, i == 0))
                {
                    universallyUnnavigable = false;
                    return false;
                }
            }

            if (BasicNavigationChecks(p, from, ref universallyUnnavigable))
                return true;

            return false;
        }

        private static bool CheckFloorAndWalls(Point p, Point? from)
        {
            if (SolidCreatureNavigWallClimbCheckDown <= 0)
            {
                for (int i = -1; i <= 1; i++)
                    for (int j = 0; j <= 1; j++)
                    {
                        //Only cardinal directions here
                        if (j * i != 0 || (j == 0 && i == 0))
                            continue;

                        //IF a neighboring tile is solid we can go on it
                        Tile adjacentTile = Main.tile[p.X + i, p.Y + j];
                        if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                            return true;
                    }
            }
            else
            {
                //Check for floor directly below, if there is its fine
                Tile adjacentTile = Main.tile[p.X, p.Y + 1];
                if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                    return true;

                //if no wall to crawl on, return false
                bool anyWall = false;
                for (int j = -1; j <= 1; j += 2)
                {
                    adjacentTile = Main.tile[p.X + j, p.Y];
                    if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                    {
                        anyWall = true;
                        break;
                    }
                }

                if (!anyWall)
                    return false;

                //Can crawl DOWN walls easy
                if (from != null && from.Value.Y < p.Y)
                    return true;

                //if crawling on wall, check down for floor
                for (int i = 1; i < SolidCreatureNavigWallClimbCheckDown; i++)
                {
                    adjacentTile = Main.tile[p.X, p.Y + 1 + i];
                    if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks for a floor / wall to crawl on , or if we can fall straight down, etc.
        /// </summary>
        private static bool BasicNavigationChecks(Point p, Point? from, ref bool universallyUnnavigable)
        {
            Tile t = Main.tile[p];
            bool solidTile = Main.tileSolid[t.TileType];
            bool platform = TileID.Sets.Platforms[t.TileType];

            //Can navigate on half tiles and platforms just fine
            if (t.HasUnactuatedTile && (t.IsHalfBlock || platform) && solidTile)
                return true;

            universallyUnnavigable = false;

            //If there's a floor to stand on / Walls to crawl on, we're fine
            if (CheckFloorAndWalls(p, from))
                return true;

            //Can fall straight down just fine
            if (from != null && p.X == from.Value.X && p.Y > from.Value.Y)
                return true;

            return false;
        }
        #endregion

        public static bool AriadneThread(Point start, Point end, List<AStarNeighbour> neighborConfiguration, TileNavigableDelegate tileNavigable, float distanceTreshold)
        {
            bool debug = true;

            MeshNode startNode = new MeshNode(start, 0, 0);
            MeshNode endNode = new MeshNode(end, 0, 0);
            if (!tileNavigable(start, null, out _ ) || !tileNavigable(endNode.origin, null, out _))
                return false;

            distanceTreshold += start.ToWorldCoordinates().Distance(end.ToWorldCoordinates());

            Heap<MeshNode> openNodes = new Heap<MeshNode>();
            List<Point> closedNodes = new List<Point>();
            openNodes.Add(startNode);

            if (debug)
                Dust.QuickDust(endNode.origin, Color.Red);

            int iterations = 0;

            while (openNodes.Count > 0)
            {
                MeshNode current = openNodes.PopFirst();
                closedNodes.Add(current.origin);
                if (current.origin == endNode.origin)
                    return true;

                if (debug)
                    Dust.QuickDust(current.origin, Color.Orange);
                if (current.gCost * 16f > distanceTreshold)
                    return false;


                foreach (AStarNeighbour neighborNode in neighborConfiguration)
                {
                    Point newOrigin = current.origin + neighborNode.offset;
                    if (closedNodes.Contains(newOrigin))
                        continue;

                    //If we can't navigate to the new tile, add it to the list of closed tiles
                    if (!tileNavigable(newOrigin, current.origin, out bool universallyUnnavigable))
                    {
                        if (universallyUnnavigable)
                            closedNodes.Add(newOrigin);
                        continue;
                    }

                    float newPathlenght = current.gCost + neighborNode.travelCost;
                    float distanceToEndPoint = GetShortestDistance(newOrigin, endNode.origin);
                    bool alreadyInOpen = openNodes.TryFind(n => n != null && n.origin == newOrigin, out MeshNode adjNode);

                    //Create a new node if it doesnt exist already
                    if (!alreadyInOpen)
                    {
                        if (neighborNode.offset == new Point(1, 0))
                            current.gCost *= 1f;
                        adjNode = new MeshNode(newOrigin, newPathlenght, distanceToEndPoint);
                        adjNode.parent = current;
                        openNodes.Add(adjNode);
                    }
                    //Update the node if it already exists and we found a shoter path
                    else if (adjNode.gCost > newPathlenght)
                    {
                        adjNode.parent = current;
                        adjNode.gCost = newPathlenght;
                        openNodes.UpdateItem(adjNode);
                    }
                }

                iterations++;
                if (iterations > 3000)
                    return false;
            }

            if (debug)
            {
                Dust d = Dust.QuickDust(end, Color.Blue);
                d.position.Y -= 5;
            }

            //Main.NewText("hCost x 16 : " + (endNode.gCost * 16f).ToString() + " - Treshold : " + distanceTreshold.ToString() + " - Deviation: " + (endNode.gCost * 16f - start.ToWorldCoordinates().Distance(end.ToWorldCoordinates())).ToString() );

            //if (endNode.gCost == 0)
            //    Main.NewText("Couldn't find a path towards goal shorter than: " + distanceTreshold.ToString());
            //else
            //    Main.NewText("Deviation from straight line to goal: " + (endNode.gCost * 16f - start.ToWorldCoordinates().Distance(end.ToWorldCoordinates())).ToString());

            return endNode.gCost != 0;
        }
    

        public class PathfindingTraceback
        {
        }
    }
}
