namespace CalamityFables.Core
{
    public static partial class AStarPathfinding
    {
        #region Old chugly large nodes pathfinding
        /*
        private static bool ValidNode(Point origin, int size, out float fullness)
        {
            int nonSolidTileCount = 0;
            int halfSolidTileCount = 0;
            int solidTileCount = 0;

            //Since we count the edges as well
            size += 2;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    Tile t = Main.tile[origin.X + i, origin.Y + j];
                    int type = t.TileType;

                    if (t.HasUnactuatedTile && Main.tileSolid[type] && !Main.tileSolidTop[type] && !TileID.Sets.Platforms[type])
                        solidTileCount++;

                    else if (TileID.Sets.Platforms[type] || Main.tileSolidTop[type] && t.TileFrameY == 0)
                        halfSolidTileCount++;

                    else
                        nonSolidTileCount++;
                }
            }

            int walkableTileCount = halfSolidTileCount + solidTileCount;
            int clearSpaceCount = halfSolidTileCount + nonSolidTileCount;
            fullness = solidTileCount / (float)(size * size);

            return walkableTileCount > 0 && clearSpaceCount > 0;
        }

        private static MeshNode ToNode(this Vector2 worldPosition, int nodeSize, Point offsetFromOrigin)
        {
            Point tilePosition = worldPosition.ToTileCoordinates();
            tilePosition.X -= (tilePosition.X - offsetFromOrigin.X) % (nodeSize + 1);
            tilePosition.Y -= (tilePosition.Y - offsetFromOrigin.Y) % (nodeSize + 1);
            return new MeshNode(tilePosition, 0, 0);
        }

        public static List<Point> PathfindRough(Vector2 start, Vector2 end, int nodeSize)
        {
            Point halfNode = new Point(nodeSize / 2 + 1, nodeSize / 2 + 1);
            Point startAlignment = start.ToTileCoordinates() - halfNode;
            Point offsetFromOrigin = new Point(startAlignment.X % (nodeSize + 1), startAlignment.Y % (nodeSize + 1)); //Not exactly right because of the overlap size

            MeshNode startNode = start.ToNode(nodeSize, Point.Zero);

            if (!ValidNode(startAlignment, nodeSize, out float _))
                return new List<Point>() { startAlignment + halfNode };

            Heap<MeshNode> openNodes = new Heap<MeshNode>();
            List<Point> closedNodes = new List<Point>();

            //Use this method to get a end node that aligns with the grid size and offset
            MeshNode endNode = end.ToNode(nodeSize, Point.Zero);

            if (!ValidNode(endNode.origin, nodeSize, out float _))
                return new List<Point>() { startAlignment + halfNode };

            openNodes.Add(startNode);
            int iterations = 0;

            while (openNodes.Count > 0)
            {
                MeshNode current = openNodes.PopFirst();
                closedNodes.Add(current.origin);

                if (current.origin == endNode.origin)
                {
                    endNode = current;
                    break;
                }

                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                    {
                        if (j == 0 && i == 0)
                            continue;

                        Point newOrigin = current.origin + new Point(i * (nodeSize + 1), j * (nodeSize + 1));
                        if (closedNodes.Contains(newOrigin))
                            continue;
                        if (!ValidNode(newOrigin, nodeSize, out float _))
                        {
                            //Cache the invalid node into closednodes so that we don't have to recalculate if its valid or not again
                            closedNodes.Add(newOrigin);
                            continue;
                        }

                        //Diagonals are 1.4 in cost, straight lines are 1
                        float newPathlenght = current.gCost + (i * j != 0 ? 1.4f : 1);
                        float distanceToEndPoint = GetDistance(newOrigin, endNode.origin, nodeSize);

                        bool alreadyInOpen = openNodes.TryFind(n => n != null && n.origin == newOrigin, out MeshNode adjNode);
                        if (!alreadyInOpen)
                        {
                            adjNode = new MeshNode(newOrigin, newPathlenght, distanceToEndPoint);
                            adjNode.parent = current;
                            openNodes.Add(adjNode);
                        }

                        else if (adjNode.gCost > newPathlenght)
                        {
                            adjNode.gCost = newPathlenght;
                            adjNode.hCost = distanceToEndPoint;
                            adjNode.parent = current;
                            openNodes.UpdateItem(adjNode);
                        }
                    }

                iterations++;
                if (iterations > 400)
                {
                    endNode = openNodes.PopFirst();
                    break;
                }    
            }

            return RetraceSteps(endNode, halfNode);
        }
        

        private static float GetDistance(Point start, Point end, int nodeSize)
        {
            int xDistance = Math.Abs(start.X - end.X) / (nodeSize + 1);
            int yDistance = Math.Abs(start.Y - end.Y) / (nodeSize + 1);

            int min = Math.Min(xDistance, yDistance);
            int max = Math.Min(xDistance, yDistance);
            return min * 1.4f + (max - min) * 1;
        }

        private static List<Point> RetraceSteps(MeshNode endPoint, Point offsetFromOrigin)
        {
            List<Point> path = new List<Point>();
            MeshNode currentNode = endPoint;
            while (currentNode.parent != null)
            {
                path.Add(currentNode.origin + offsetFromOrigin);
                currentNode = currentNode.parent;
            }
            path.Add(currentNode.origin + offsetFromOrigin);
            return path;
        }
        */
        #endregion

        public static bool AirNavigable(Point p, Point? origin, out bool universallyUnnavigable)
        {
            universallyUnnavigable = true;
            Tile t = Main.tile[p];
            return !t.HasUnactuatedTile || !Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType];
        }

        public static bool FloorNavigable(Point p, Point? origin, out bool universallyUnnavigable)
        {
            universallyUnnavigable = true;
            Tile t = Main.tile[p];
            return t.HasUnactuatedTile && (Main.tileSolid[t.TileType] || (Main.tileSolidTop[t.TileType] && t.TileFrameY == 0));
        }

        public static bool EdgeRunner(Point p, Point? origin, out bool universallyUnnavigable)
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

            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    //Only cardinal directions here
                    if (j * i != 0 || (j == 0 && i == 0))
                        continue;

                    //IF a neighboring tile is solid we can go on it
                    Tile adjacentTile = Main.tile[p.X + i, p.Y + j];
                    if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                        return true;
                }

            return false;
        }



        public static Point OffsetUntilNavigable(Point point, Point offset, TileNavigableDelegate navigable)
        {
            int iterations = 50;
            return OffsetUntilNavigable(point, offset, navigable, ref iterations);
        }

        public static Point OffsetUntilNavigable(Point point, Point offset, TileNavigableDelegate navigable, ref int iterations)
        {
            if (navigable(point, null, out _))
                return point;
            while (!navigable(point, null, out _))
            {
                point += offset;
                iterations--;
                if (iterations < 0)
                    return Point.Zero;
            }
            return point;
        }

        public static List<Point> Pathfind(Vector2 start, Vector2 end, List<AStarNeighbour> neighborConfiguration, TileNavigableDelegate tileNavigable, int maxIterations = 1000)
        {
            return Pathfind(start.ToTileCoordinates(), end.ToTileCoordinates(), neighborConfiguration, tileNavigable, maxIterations);
        }

        public static List<Point> Pathfind(Point start, Point end, List<AStarNeighbour> neighborConfiguration, TileNavigableDelegate tileNavigable, int maxIterations = 1000)
        {
            _maxNodeIndex = 0;
            MeshNode startNode = GetNode(start, 0, 0);
            MeshNode endNode = GetNode(end, 0, 0);

            if (!tileNavigable(start, null, out _) || !tileNavigable(endNode.origin, null, out _))
                return new List<Point>() { start };

            Heap<MeshNode> openNodes = new Heap<MeshNode>();
            List<Point> closedNodes = new List<Point>();
            openNodes.Add(startNode);

            int iterations = 0;
            while (openNodes.Count > 0)
            {
                MeshNode current = openNodes.PopFirst();
                closedNodes.Add(current.origin);

                if (current.origin == endNode.origin)
                {
                    endNode = current;
                    break;
                }

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
                        adjNode = GetNode(newOrigin, newPathlenght, distanceToEndPoint);
                        adjNode.parent = current;
                        openNodes.Add(adjNode);
                    }
                    //Update the node if it already exists and we found a shoter path
                    else if (adjNode.gCost > newPathlenght)
                    {
                        adjNode.gCost = newPathlenght;
                        adjNode.parent = current;
                        openNodes.UpdateItem(adjNode);
                    }
                }

                iterations++;
                if (iterations > maxIterations)
                {
                    endNode = openNodes.PopFirst();
                    break;
                }
            }

            return RetraceSteps(endNode);
        }

        private static float GetShortestDistance(Point start, Point end)
        {
            int xDistance = Math.Abs(start.X - end.X);
            int yDistance = Math.Abs(start.Y - end.Y);

            int min = Math.Min(xDistance, yDistance);
            int max = Math.Max(xDistance, yDistance);
            return min * AStarNeighbour.SquareRootOfTwo + (max - min);
        }

        private static List<Point> RetraceSteps(MeshNode endPoint)
        {
            List<Point> path = new List<Point>();
            MeshNode currentNode = endPoint;
            while (currentNode.parent != null)
            {
                path.Add(currentNode.origin);
                currentNode = currentNode.parent;
            }
            path.Add(currentNode.origin);
            return path;
        }

        public static bool IsThereAPath(Vector2 start, Vector2 end, List<AStarNeighbour> neighborConfiguration, TileNavigableDelegate tileNavigable, float distanceTreshold)
        {
            return IsThereAPath(start.ToTileCoordinates(), end.ToTileCoordinates(), neighborConfiguration, tileNavigable, distanceTreshold);
        }

        public delegate bool TileNavigableDelegate(Point tileCandidate, Point? fromTile, out bool universallyUnnavigable);

        public static bool IsThereAPath(Point start, Point end, List<AStarNeighbour> neighborConfiguration, TileNavigableDelegate tileNavigable, float distanceTreshold)
        {
            bool debug = false;

            //Reset the max pooled node
            _maxNodeIndex = 0;

            MeshNode startNode = GetNode(start, 0, 0);
            MeshNode endNode = GetNode(end, 0, 0);
            if (!tileNavigable(start, null, out _) || !tileNavigable(endNode.origin, null, out _))
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
                        adjNode = GetNode(newOrigin, newPathlenght, distanceToEndPoint);
                        openNodes.Add(adjNode);
                    }
                    //Update the node if it already exists and we found a shoter path
                    else if (adjNode.gCost > newPathlenght)
                    {
                        adjNode.gCost = newPathlenght;
                        openNodes.UpdateItem(adjNode);
                    }
                }

                iterations++;
                if (iterations > 2000)
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
    }

    /// <summary>
    /// Represents a combination of offset and cost data for a given position
    /// </summary>
    public class AStarNeighbour
    {
        public readonly Point offset;
        public readonly float travelCost;

        public AStarNeighbour(Point offset, float travelCost)
        {
            this.offset = offset;
            this.travelCost = travelCost;
        }
        public AStarNeighbour(int x, int y, float travelCost)
        {
            this.offset = new Point(x, y);
            this.travelCost = travelCost;
        }

        internal const float SquareRootOfTwo = 1.41421f;
        public static readonly List<AStarNeighbour> BasicCardinalOrdinal = new List<AStarNeighbour>()
        {
            new AStarNeighbour(-1, -1, SquareRootOfTwo),
            new AStarNeighbour(0, -1, 1),
            new AStarNeighbour(1, -1, SquareRootOfTwo),

            new AStarNeighbour(-1, 0, 1),
            new AStarNeighbour(1, 0, 1),

            new AStarNeighbour(-1, 1, SquareRootOfTwo),
            new AStarNeighbour(0, 1, 1),
            new AStarNeighbour(1, 1, SquareRootOfTwo),
        };

        public static readonly List<AStarNeighbour> DoubleStride = new List<AStarNeighbour>()
            {
                new AStarNeighbour(-1, -1, SquareRootOfTwo),
                new AStarNeighbour(0, -1, 1),
                new AStarNeighbour(1, -1, SquareRootOfTwo),

                new AStarNeighbour(-1, 0, 1),
                new AStarNeighbour(1, 0, 1),
                new AStarNeighbour(-2, 0, 2),
                new AStarNeighbour(2, 0, 2),

                new AStarNeighbour(-1, 1, SquareRootOfTwo),
                new AStarNeighbour(0, 1, 1),
                new AStarNeighbour(1, 1, SquareRootOfTwo)
            };

        public static List<AStarNeighbour> BigStride(int stride)
        {
            List<AStarNeighbour> movementPattern = new List<AStarNeighbour>(6 + 2 * stride);
            movementPattern.Add(new AStarNeighbour(-1, -1, SquareRootOfTwo));
            movementPattern.Add(new AStarNeighbour(0, -1, 1));
            movementPattern.Add(new AStarNeighbour(1, -1, SquareRootOfTwo));

            for (int i = 1; i <= stride; i++)
            {
                movementPattern.Add(new AStarNeighbour(-i, 0, i));
                movementPattern.Add(new AStarNeighbour(i, 0, i));
            }

            movementPattern.Add(new AStarNeighbour(-1, 1, SquareRootOfTwo));
            movementPattern.Add(new AStarNeighbour(0, 1, 1));
            movementPattern.Add(new AStarNeighbour(1, 1, SquareRootOfTwo));

            return movementPattern;
        }

        public static List<AStarNeighbour> BigOmniStride(int strideX, int strideY)
        {
            List<AStarNeighbour> movementPattern = new List<AStarNeighbour>(6 + 2 * strideX + 2 * strideY);
            movementPattern.Add(new AStarNeighbour(-1, -1, SquareRootOfTwo));
            movementPattern.Add(new AStarNeighbour(0, -1, 1));
            movementPattern.Add(new AStarNeighbour(1, -1, SquareRootOfTwo));

            for (int i = 1; i <= strideX; i++)
            {
                movementPattern.Add(new AStarNeighbour(-i, 0, i));
                movementPattern.Add(new AStarNeighbour(i, 0, i));
            }

            for (int i = 1; i <= strideY; i++)
            {
                movementPattern.Add(new AStarNeighbour(0, -i, i));
                movementPattern.Add(new AStarNeighbour(0, i, i));
            }

            movementPattern.Add(new AStarNeighbour(-1, 1, SquareRootOfTwo));
            movementPattern.Add(new AStarNeighbour(0, 1, 1));
            movementPattern.Add(new AStarNeighbour(1, 1, SquareRootOfTwo));

            return movementPattern;
        }

        //Not done fully . L
        public static List<AStarNeighbour> UStride(int strideWidth, int strideHeight)
        {
            List<AStarNeighbour> movementPattern = new List<AStarNeighbour>(3 + 2 * strideHeight * strideWidth);

            for (int i = 1; i <= strideWidth; i++)
            {
                movementPattern.Add(new AStarNeighbour(-i, 0, i));
                movementPattern.Add(new AStarNeighbour(i, 0, i));

                for (int j = 1; j <= strideHeight; j++)
                {
                    movementPattern.Add(new AStarNeighbour(-i, -j, i));
                    movementPattern.Add(new AStarNeighbour(i, -j, i));
                }
            }

            movementPattern.Add(new AStarNeighbour(-1, 1, SquareRootOfTwo));
            movementPattern.Add(new AStarNeighbour(0, 1, 1));
            movementPattern.Add(new AStarNeighbour(1, 1, SquareRootOfTwo));

            return movementPattern;
        }

    }

    public class MeshNode : IHeapItem<MeshNode>
    {
        /// <summary>
        /// The lenght of the path we took to reach this node, starting from the original node
        /// May get updated as we find shorter paths to reach this node
        /// </summary>
        public float gCost;
        /// <summary>
        /// The heuristic guess of how much distance is between the current node and the target node
        /// Remains constant, as neither the node itself or the target moves
        /// </summary>
        public float hCost;
        /// <summary>
        /// The combined cost of both the lenght of the path from the starting node, and the estimated lenght of the path to the target node
        /// </summary>
        public float FCost => gCost + hCost;

        public Point origin;
        public MeshNode parent;

        public int HeapIndex { get; set; }
        public int CompareTo(MeshNode other)
        {
            int compare = FCost.CompareTo(other.FCost);
            //If the 2 nodes have the same F cost, pick the one with the smaller H cost
            if (compare == 0)
                compare = hCost.CompareTo(other.hCost);
            return -compare;
        }

        public MeshNode(Point origin, float gCost, float hCost)
        {
            this.origin = origin;
            this.gCost = gCost;
            this.hCost = hCost;
        }

        public MeshNode()
        {
        }
    }
}
