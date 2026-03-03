using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    /// <summary>
    /// A loop of primitives that makes a circle (or an ellipse)
    /// </summary>
    public class PrimitiveClosedLoop : PrimitiveShape
    {
        protected readonly int maxPointCount;

        protected readonly TrailWidthFunction trailWidthFunction;

        protected readonly TrailColorFunction trailColorFunction;

        public override bool InvalidForDrawing => Positions == null;
        public override int IndexCount => 6 * maxPointCount;
        public override int VertexCount => maxPointCount * 2 + 2;

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

        protected Vector2[] positions;

        protected const float defaultWidth = 16;

        public PrimitiveClosedLoop(int maxPointCount, TrailWidthFunction trailWidthFunction, TrailColorFunction trailColorFunction)
        {
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
             * The amount of regions in the whole trail are given by n, so there are 2n triangles for n points.
             * Finally, since each triangle is defined by 3 indices, there are 6n  indices, plus the tip's count.
             */

            InitializePrimitives();
        }

        public override void GenerateMesh(out VertexPositionColorTexture[] vertices, out short[] indices)
        {
            VertexPositionColorTexture[] verticesTemp = new VertexPositionColorTexture[maxPointCount * 2 + 2];

            short[] indicesTemp = new short[maxPointCount * 6];

            //Generating vertices is the same as for prim trails
            // k = 0 indicates starting at the end of the trail (furthest from the origin of it).
            for (int k = 0; k <= Positions.Length; k++)
            {
                // 1 at k = Positions.Length - 1 (start) and 0 at k = 0 (end).
                float factorAlongTrail = (float)k / (Positions.Length);

                // Uses the trail width function to decide the width of the trail at this point (if no function, use 
                float width = trailWidthFunction?.Invoke(factorAlongTrail) ?? defaultWidth;

                Vector2 current = Positions[k % positions.Length];
                Vector2 next = Positions[(k + 1) % positions.Length];

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
                 * These indices are given by k for the above point and k + n + 1 for the below point. (+1 cuz we have the doubled final points
                 */

                verticesTemp[k] = new VertexPositionColorTexture(a.Vec3(), colorA, texCoordA);
                verticesTemp[k + maxPointCount + 1] = new VertexPositionColorTexture(c.Vec3(), colorC, texCoordC);
            }

            /* Now, we have to loop through the indices to generate triangles.
             * Looping to maxPointCount brings us halfway to the end; it covers the top row (excluding the last point on the top row).
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

                indicesTemp[k * 6] = (short)(k + maxPointCount + 1);
                indicesTemp[k * 6 + 1] = (short)(k + maxPointCount + 2);
                indicesTemp[k * 6 + 2] = (short)(k + 1);
                indicesTemp[k * 6 + 3] = (short)(k + 1);
                indicesTemp[k * 6 + 4] = k;
                indicesTemp[k * 6 + 5] = (short)(k + maxPointCount + 1);
            }

            //Set the final triangles to loop the strip
            indicesTemp[(maxPointCount - 1) * 6] = (short)(maxPointCount * 2); //maxPointCount - 1 + maxPointCount + 1
            indicesTemp[(maxPointCount - 1) * 6 + 1] = (short)(maxPointCount * 2 + 1); //maxPointCount - 1 + maxPointCount + 2
            indicesTemp[(maxPointCount - 1) * 6 + 2] = (short)maxPointCount;
            indicesTemp[(maxPointCount - 1) * 6 + 3] = (short)maxPointCount;
            indicesTemp[(maxPointCount - 1) * 6 + 4] = (short)(maxPointCount - 1);
            indicesTemp[(maxPointCount - 1) * 6 + 5] = (short)(maxPointCount * 2);//maxPointCount - 1 + maxPointCount + 1

            // The next available index will be the next value after the count of points (starting at 0).
            vertices = verticesTemp;
            // Maybe we could use an array instead of a list for the indices, if someone figures out how to add indices to an array properly.
            indices = indicesTemp;
        }

        /// <summary>
        /// Generates and sets the positions to a circle
        /// </summary>
        /// <param name="Center">The center of the circle</param>
        /// <param name="radius">The radius of the circle</param>
        /// <param name="rotationOffset">A constant offset applied to the trail points, making them progress through the loop</param>
        public void SetPositionsCircle(Vector2 Center, float radius, float rotationOffset = 0f)
        {
            Vector2[] positionsTemp = new Vector2[maxPointCount];

            for (int i = 0; i < maxPointCount; i++)
            {
                float rotation = i / (float)maxPointCount * MathHelper.TwoPi + rotationOffset;
                positionsTemp[i] = Center + rotation.ToRotationVector2() * radius;
            }

            Positions = positionsTemp;
        }


        /// <summary>
        /// Generates positions as an ellipse
        /// </summary>
        /// <param name="Center">The center of the ellipse</param>
        /// <param name="radius">The base radius</param>
        /// <param name="rotationOffset">A constant offset applied to the trail points, making them progress through the loop</param>
        /// <param name="squish">How vertically squished the circle is</param>
        /// <param name="pointingRotation">Where does the ellipse point</param>
        /// <param name="axisRotation">Interpreted as a "spin" around the center of the ellipse, around the pointing rotation's axis</param>
        public void SetPositionsEllipse(Vector2 Center, float radius, float rotationOffset = 0f, float squish = 1f, float pointingRotation = 0f, float axisRotation = 0f)
        {
            Vector2[] positionsTemp = new Vector2[maxPointCount];

            Vector2 unitVectorX = new Vector2(1f, 0).RotatedBy(pointingRotation);
            Vector2 unitVectorY = new Vector2(0f, squish).RotatedBy(pointingRotation);

            for (int i = 0; i < maxPointCount; i++)
            {
                float ringProgress = (i / (float)maxPointCount + rotationOffset) * MathHelper.TwoPi;
                Vector2 unit = unitVectorX * (float)Math.Cos(ringProgress) + unitVectorY * (float)Math.Sin(ringProgress + axisRotation);
                positionsTemp[i] = Center + unit * radius;
            }

            Positions = positionsTemp;
        }
    }

    /// <summary>
    /// A loop of primitive that makes an ellipse
    /// Different from <see cref="PrimitiveClosedLoop"/> by having all the inner edges of the circle be parallel
    /// Results in the effect of seeing a band of paper from the side
    /// 
    /// Can draw the front and back section separately
    /// </summary>
    public class PrimitivePerspectiveRing : PrimitiveShape
    {
        protected readonly int maxPointCount;
        protected readonly TrailColorFunction trailColorFunction;

        public float Width = 16;
        public Vector2 Perpendicular;
        public bool DrawingFront = false;

        public override bool InvalidForDrawing => Positions == null;


        //This is the amount of vertices and indices per half the ring (front and back individually)
        public override int IndexCount => 6 * (maxPointCount - 1);
        public override int VertexCount => maxPointCount * 2;

        public Vector2[] Positions {
            get => positions;
            set {
                if (value.Length != maxPointCount * 2)
                {
                    throw new ArgumentException("Array of positions was a different length than the expected result!");
                }

                positions = value;
            }
        }
        protected Vector2[] positions;

        public PrimitivePerspectiveRing(int maxPointCount, float width, TrailColorFunction trailColorFunction)
        {
            this.maxPointCount = maxPointCount;
            this.Width = width;
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
             * The amount of regions in the whole trail are given by n, so there are 2n triangles for n points.
             * Finally, since each triangle is defined by 3 indices, there are 6n  indices, plus the tip's count.
             */

            InitializePrimitives();
        }

        public override void GenerateMesh(out VertexPositionColorTexture[] vertices, out short[] indices)
        {
            VertexPositionColorTexture[] verticesTemp = new VertexPositionColorTexture[VertexCount];
            short[] indicesTemp = new short[IndexCount];
            int positionOffset = DrawingFront ? 0 : maxPointCount;

            //Generating vertices is the same as for prim trails
            // k = 0 indicates starting at the end of the trail (furthest from the origin of it).
            for (int i = 0; i < maxPointCount; i++)
            {
                int k = i + positionOffset;
                // 1 at k = Positions.Length - 1 (start) and 0 at k = 0 (end).
                float factorAlongTrail = (float)k / (Positions.Length);

                Vector2 current = Positions[k];
                Vector2 a = current + (Perpendicular * Width);
                Vector2 c = current - (Perpendicular * Width);

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
                 * These indices are given by k for the above point and k + n + 1 for the below point. (+1 cuz we have the doubled final points
                 */

                verticesTemp[i] = new VertexPositionColorTexture(a.Vec3(), colorA, texCoordA);
                verticesTemp[i + maxPointCount] = new VertexPositionColorTexture(c.Vec3(), colorC, texCoordC);
            }

            /* Now, we have to loop through the indices to generate triangles.
             * Looping to maxPointCount brings us halfway to the end; it covers the top row (excluding the last point on the top row).
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
            vertices = verticesTemp;
            // Maybe we could use an array instead of a list for the indices, if someone figures out how to add indices to an array properly.
            indices = indicesTemp;
        }

        /// <summary>
        /// Generates and sets the positions to a circle
        /// </summary>
        /// <param name="Center">The center of the circle</param>
        /// <param name="radius">The radius of the circle</param>
        public void SetPositions(Vector2 Center, float radius, float pointing, float squish)
        {
            Perpendicular = new Vector2(1f, 0).RotatedBy(pointing);

            Vector2 unitVectorX = Perpendicular * squish;
            Vector2 unitVectorY = new Vector2(0f, 1f).RotatedBy(pointing);

            Vector2[] positionsTemp = new Vector2[maxPointCount];

            for (int side = 0; side < 2; side++)
            {
                for (int i = 0; i < maxPointCount; i++)
                {
                    float rotation = (i / (float)(maxPointCount - 1)) * MathHelper.Pi + side * MathHelper.Pi;
                    Vector2 unit = unitVectorX * (float)Math.Cos(rotation) + unitVectorY * (float)Math.Sin(rotation);
                    positionsTemp[i + maxPointCount * side] = Center + unit  * radius;
                }
            }

            Positions = positionsTemp;
        }
    }
}
