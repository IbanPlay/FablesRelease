using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public partial class Primitives : IDisposable //Credit goes to Oli!!!!
    {
    }

    public class PrimitiveCube : IDisposable
    {
        private Primitives primitives;

        internal readonly int maxPointCount;

        internal readonly ITrailTip tip;

        internal readonly TrailWidthFunction trailWidthFunction;

        internal readonly TrailColorFunction trailColorFunction;

        private readonly BasicEffect baseEffect;

        /// <summary>
        /// Array of positions that define the trail. NOTE: Positions[Positions.Length - 1] is assumed to be the start (e.g. Projectile.Center) and Positions[0] is assumed to be the end.
        /// </summary>
        public Vector2[] Positions {
            get => positions;
            set {
                if (value.Length != maxPointCount)
                {
                    throw new ArgumentException("Array of positions was a different length than the expected result!");
                }

                positions = value;
            }
        }

        private Vector2[] positions;

        /// <summary>
        /// Used in order to calculate the normal from the frontmost position, because there isn't a point after it in the original list.
        /// </summary>
        public Vector2 NextPosition { get; set; }

        private const float defaultWidth = 16;

        public PrimitiveCube(int maxPointCount, TrailWidthFunction trailWidthFunction, TrailColorFunction trailColorFunction, ITrailTip tip = null)
        {
            this.tip = tip ?? new NoTip();

            this.maxPointCount = maxPointCount;

            this.trailWidthFunction = trailWidthFunction;

            this.trailColorFunction = trailColorFunction;

            /* A---B---C
             * |  /|  /|
             * D / E / F
             * |/  |/  |
             * G---H---I
             * 
             * Let D, E, F, etc. be the set of n points that define the trail.
             * Since each point generates 2 vertices, there are 2n vertices, plus the tip's count.
             * 
             * As for indices - in the region between 2 defining points there are 2 triangles.
             * The amount of regions in the whole trail are given by n - 1, so there are 2(n - 1) triangles for n points.
             * Finally, since each triangle is defined by 3 indices, there are 6(n - 1) indices, plus the tip's count.
             */

            primitives = new Primitives(Main.graphics.GraphicsDevice, (maxPointCount * 2) + this.tip.ExtraVertices, (6 * (maxPointCount - 1)) + this.tip.ExtraIndices);

            baseEffect = new BasicEffect(Main.graphics.GraphicsDevice)
            {
                VertexColorEnabled = true,
                TextureEnabled = false
            };
        }

        private void GenerateMesh(out VertexPositionColorTexture[] vertices, out short[] indices, out int nextAvailableIndex)
        {
            VertexPositionColorTexture[] verticesTemp = new VertexPositionColorTexture[maxPointCount * 2];

            short[] indicesTemp = new short[maxPointCount * 6 - 6];

            // k = 0 indicates starting at the end of the trail (furthest from the origin of it).
            for (int k = 0; k < Positions.Length; k++)
            {
                // 1 at k = Positions.Length - 1 (start) and 0 at k = 0 (end).
                float factorAlongTrail = (float)k / (Positions.Length - 1);

                // Uses the trail width function to decide the width of the trail at this point (if no function, use 
                float width = trailWidthFunction?.Invoke(factorAlongTrail) ?? defaultWidth;

                Vector2 current = Positions[k];
                Vector2 next = (k == Positions.Length - 1 ? Positions[Positions.Length - 1] + (Positions[Positions.Length - 1] - Positions[Positions.Length - 2]) : Positions[k + 1]);

                Vector2 normalToNext = (next - current).SafeNormalize(Vector2.Zero);
                Vector2 normalPerp = normalToNext.RotatedBy(MathHelper.PiOver2);

                /* A
                 * |
                 * B---D
                 * |
                 * C
                 * 
                 * Let B be the current point and D be the next one.
                 * A and C are calculated based on the perpendicular vector to the normal from B to D, scaled by the desired width calculated earlier.
                 */

                Vector2 a = current + (normalPerp * width);
                Vector2 c = current - (normalPerp * width);

                /* Texture coordinates are calculated such that the top-left is (0, 0) and the bottom-right is (1, 1).
                 * To achieve this, we consider the Y-coordinate of A to be 0 and that of C to be 1, while the X-coordinate is just the factor along the trail.
                 * This results in the point last in the trail having an X-coordinate of 0, and the first one having a Y-coordinate of 1.
                 */
                Vector2 texCoordA = new Vector2(factorAlongTrail, 0);
                Vector2 texCoordC = new Vector2(factorAlongTrail, 1);

                // Calculates the color for each vertex based on its texture coordinates. This acts like a very simple shader (for more complex effects you can use the actual shader).
                Color colorA = trailColorFunction?.Invoke(factorAlongTrail) ?? Color.White;
                Color colorC = trailColorFunction?.Invoke(factorAlongTrail) ?? Color.White;

                /* 0---1---2
                 * |  /|  /|
                 * A / B / C
                 * |/  |/  |
                 * 3---4---5
                 * 
                 * Assuming we want vertices to be indexed in this format, where A, B, C, etc. are defining points and numbers are indices of mesh points:
                 * For a given point that is k positions along the chain, we want to find its indices.
                 * These indices are given by k for the above point and k + n for the below point.
                 */

                verticesTemp[k] = new VertexPositionColorTexture(a.Vec3(), colorA, texCoordA);
                verticesTemp[k + maxPointCount] = new VertexPositionColorTexture(c.Vec3(), colorC, texCoordC);
            }

            /* Now, we have to loop through the indices to generate triangles.
             * Looping to maxPointCount - 1 brings us halfway to the end; it covers the top row (excluding the last point on the top row).
             */
            for (short k = 0; k < maxPointCount - 1; k++)
            {
                /* 0---1
                 * |  /|
                 * A / B
                 * |/  |
                 * 2---3
                 * 
                 * This illustration is the most basic set of points (where n = 2).
                 * In this, we want to make triangles (2, 3, 1) and (1, 0, 2).
                 * Generalising this, if we consider A to be k = 0 and B to be k = 1, then the indices we want are going to be (k + n, k + n + 1, k + 1) and (k + 1, k, k + n)
                 */

                indicesTemp[k * 6] = (short)(k + maxPointCount);
                indicesTemp[k * 6 + 1] = (short)(k + maxPointCount + 1);
                indicesTemp[k * 6 + 2] = (short)(k + 1);
                indicesTemp[k * 6 + 3] = (short)(k + 1);
                indicesTemp[k * 6 + 4] = k;
                indicesTemp[k * 6 + 5] = (short)(k + maxPointCount);
            }

            // The next available index will be the next value after the count of points (starting at 0).
            nextAvailableIndex = verticesTemp.Length;

            vertices = verticesTemp;

            // Maybe we could use an array instead of a list for the indices, if someone figures out how to add indices to an array properly.
            indices = indicesTemp;
        }

        private void SetupMeshes()
        {
            GenerateMesh(out VertexPositionColorTexture[] mainVertices, out short[] mainIndices, out int nextAvailableIndex);

            Vector2 toNext = (NextPosition - Positions[Positions.Length - 1]).SafeNormalize(Vector2.Zero);

            tip.GenerateMesh(Positions[Positions.Length - 1], toNext, nextAvailableIndex, out VertexPositionColorTexture[] tipVertices, out short[] tipIndices, trailWidthFunction, trailColorFunction);

            primitives.SetVertices(mainVertices.FastUnion(tipVertices));
            primitives.SetIndices(mainIndices.FastUnion(tipIndices));
        }

        public void Render(Effect effect = null, Vector2? offset = null)
        {
            Vector2 offset_ = offset.GetValueOrDefault();
            Render(effect, Matrix.CreateTranslation(offset_.Vec3()));
        }

        public void Render(Effect effect = null, Matrix? translation = null)
        {
            if (Positions == null || primitives == null)
                return;
            if (primitives.IsDisposed)
                primitives = new Primitives(Main.graphics.GraphicsDevice, (maxPointCount * 2) + tip.ExtraVertices, (6 * (maxPointCount - 1)) + tip.ExtraIndices);
            GhostTrailsHandler.LogDisposable(this);

            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            SetupMeshes();
            if (!translation.HasValue)
                translation = Matrix.CreateTranslation(-Main.screenPosition.Vec3());

            if (effect == null)
            {
                effect = baseEffect;
            }

            primitives.Render(effect, translation.Value);
        }

        public void Dispose()
        {
            primitives?.Dispose();
        }

        /// <summary>
        /// Sets the current positions of the trail to a given array of points
        /// Automatically generates more points if the provided array doesn't contain enough positions, using the provided retrieval function
        /// </summary>
        /// <param name="points">List of points the primitive trail moves across</param>
        /// <param name="retrievalFunction">Retrieval function used to generate more points to fill in the gaps</param>
        public void SetPositions(IEnumerable<Vector2> points, TrailPointRetrievalFunction retrievalFunction = null)
        {
            if (retrievalFunction is null)
                retrievalFunction = RigidPointRetreivalFunction;

            List<Vector2> trailPoints = retrievalFunction(points, maxPointCount);
            if (trailPoints.Count != maxPointCount)
                return;

            Positions = trailPoints.ToArray();
        }

        /// <summary>
        /// Just like SetPositions() but if the list of points has any zeros, it will instead make them use the earliest position thats valid in the list
        /// </summary>
        public void SetPositionsSmart(IEnumerable<Vector2> points, Vector2 fallBack, TrailPointRetrievalFunction retrievalFunction = null)
        {
            if (!points.Contains(Vector2.Zero))
            {
                SetPositions(points, retrievalFunction);
                return;
            }

            Vector2 lastValidPoint = fallBack;
            List<Vector2> pointList = new List<Vector2>();
            for (int i = points.Count() - 1; i >= 0; i--)
            {
                if (points.ElementAt(i) != Vector2.Zero)
                    lastValidPoint = points.ElementAt(i);

                pointList.Add(lastValidPoint);
            }
            pointList.Reverse();

            SetPositions(pointList, retrievalFunction);
        }
    }
}