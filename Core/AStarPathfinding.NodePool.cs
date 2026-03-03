namespace CalamityFables.Core
{
    public static partial class AStarPathfinding
    {
        private static readonly List<MeshNode> _nodePool = new List<MeshNode>(200);
        private static int _maxNodeIndex;
        private static MeshNode GetNode(Point origin, float gCost, float hCost, int createMore = 10)
        {
            if (_maxNodeIndex < _nodePool.Count)
            {
                MeshNode poolNode = _nodePool[_maxNodeIndex++];
                poolNode.origin = origin;
                poolNode.gCost = gCost;
                poolNode.hCost = hCost;
                return poolNode;
            }

            //Create a bunch of new nodes for the future
            for (int i = 0; i < createMore; i++)
            {
                _nodePool.Add(new MeshNode());
            }

            MeshNode node = _nodePool[_maxNodeIndex++];
            node.origin = origin;
            node.gCost = gCost;
            node.hCost = hCost;
            return node;
        }

    }
}
