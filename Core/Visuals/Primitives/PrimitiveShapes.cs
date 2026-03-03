using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public class PrimitiveQuadrilateral : PrimitiveShape
    {
        public override int VertexCount => _vertexCount;
        public override int IndexCount => _indexCount;

        internal const int _vertexCount = 4;
        internal const int _indexCount = 6;

        public override Vector2 DefaultOffset => Vector2.Zero;
        public override bool InvalidForDrawing => Vertices == null;

        public Color color;

        /// <summary>
        /// Array of positions that define the trail. NOTE: Positions[Positions.Length - 1] is assumed to be the start (e.g. Projectile.Center) and Positions[0] is assumed to be the end.
        /// </summary>
        public Vector2[] Vertices {
            get => vertices;
            set {
                if (value.Length != VertexCount)
                {
                    throw new ArgumentException("Array of positions was a different length than the expected result!");
                }

                vertices = value;
            }
        }
        private Vector2[] vertices;

        public PrimitiveQuadrilateral(Color? color = null)
        {
            /* A---B
             * |  /|
             * C / D
             * Bozo alert
             */
            this.color = color ?? Color.White;
            InitializePrimitives();
        }

        public override void GenerateMesh(out VertexPositionColorTexture[] vertices, out short[] indices)
        {
            //HARDCODING IS THE BEST! SAY IT WITH ME! HARDCODING! RULES!
            vertices = new VertexPositionColorTexture[_vertexCount]
            {   new VertexPositionColorTexture(Vertices[0].Vec3(), color, Vector2.Zero), //Top left
                new VertexPositionColorTexture(Vertices[1].Vec3(), color, Vector2.UnitX), //Top right
                new VertexPositionColorTexture(Vertices[2].Vec3(), color, Vector2.UnitY), //Bottom left
                new VertexPositionColorTexture(Vertices[3].Vec3(), color, Vector2.One) //Bottom right
            };

            /* 0---1
             * |  /|
             * |/  |
             * 2---3
             */

            indices = new short[_indexCount]
            {
                (short)1, (short)0,(short)2,
                (short)2, (short)3,(short)1,
            };
        }

        /// <summary>
        /// Sets the positions of this quad using the center position, width, height, and rotation.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="rotation"></param>
        public void SetPositions(Vector2 center, float width, float height, float rotation)
        {
            Vertices ??= new Vector2[4];

            for (int i = 0; i < _vertexCount; i++)
            {
                int x = i % 2 == 0 ? -1 : 1;    // Even i, left side
                int y = i < 2 ? 1 : -1;         // i below 2, top edge

                Vector2 cornerPos = center - new Vector2(x * width / 2, y * height / 2).RotatedBy(rotation);

                Vertices[i] = cornerPos;
            }
        }
    }

    public class PrimitivePolygonOutline : PrimitiveShape
    {
        private readonly int maxVertexCount;
        public override bool InvalidForDrawing => Vertices == null;
        public override int VertexCount => maxVertexCount * 2;
        public override int IndexCount => maxVertexCount * 6;

        public override Vector2 DefaultOffset => Vector2.Zero;

        public Color color;
        public float outlineThickness;

        public PolygonUpscalingAlgorithm outlineUpscaling;

        /// <summary>
        /// Array of positions that define the trail. NOTE: Positions[Positions.Length - 1] is assumed to be the start (e.g. Projectile.Center) and Positions[0] is assumed to be the end.
        /// </summary>
        public Vector2[] Vertices {
            get => vertices;
            set {
                if (value.Length != maxVertexCount)
                {
                    throw new ArgumentException("Array of positions was a different length than the expected result!");
                }

                vertices = value;


                Vector2 highestCoordinates = new Vector2(float.MinValue);
                Vector2 lowestCoordinates = new Vector2(float.MaxValue);
                foreach (Vector2 vertex in vertices)
                {
                    highestCoordinates.X = Math.Max(vertex.X, highestCoordinates.X);
                    highestCoordinates.Y = Math.Max(vertex.Y, highestCoordinates.Y);
                    lowestCoordinates.X = Math.Min(vertex.X, lowestCoordinates.X);
                    lowestCoordinates.Y = Math.Min(vertex.Y, lowestCoordinates.Y);
                }
            }
        }
        private Vector2[] vertices;

        /// <summary>
        /// THIS PROBABLY WONT WORK WITH CONVEX SHAPES
        /// DONT EVEN TRY I WILL NOT FIX THE UPSCALING
        /// </summary>
        /// <param name="pointCount"></param>
        /// <param name="color"></param>
        public PrimitivePolygonOutline(int pointCount, float outlineThickness, Color? color = null, PolygonUpscalingAlgorithm upscaler = null)
        {
            maxVertexCount = pointCount;
            this.color = color ?? Color.White;
            this.outlineThickness = outlineThickness;
            outlineUpscaling = upscaler ?? UpscalePolygonFromCenter;

            //https://media.discordapp.net/attachments/802291445360623686/1036075596888952883/unknown.png
            InitializePrimitives();
        }


        public override void GenerateMesh(out VertexPositionColorTexture[] vertices, out short[] indices)
        {
            vertices = new VertexPositionColorTexture[maxVertexCount * 2];

            Vector2[] outwardsVertices = outlineUpscaling(Vertices.ToList(), outlineThickness).ToArray();

            // k = 0 indicates starting at the end of the trail (furthest from the origin of it).
            for (int k = 0; k < Vertices.Length; k++)
            {
                // 1 at k = Positions.Length - 1 (start) and 0 at k = 0 (end).
                float factorAlongOutline = (float)k / (Vertices.Length - 1);

                // Uses the trail width function to decide the width of the trail at this point (if no function, use 

                Vector2 inner = Vertices[k];
                Vector2 outer = outwardsVertices[k];

                //No nerd shit. Tldr X coordinate = how far along the outline from the first vertex to the last.
                //Y coordinate = is it on the outside or the inside of the outline
                Vector2 innerTexCoords = new Vector2(factorAlongOutline, 0);
                Vector2 outerTexCoords = new Vector2(factorAlongOutline, 1);

                //https://media.discordapp.net/attachments/802291445360623686/1036075596888952883/unknown.png

                vertices[k] = new VertexPositionColorTexture(outer.Vec3(), color, outerTexCoords);
                vertices[k + maxVertexCount] = new VertexPositionColorTexture(inner.Vec3(), color, innerTexCoords);
            }

            indices = new short[maxVertexCount * 6];

            /* Now, we have to loop through the indices to generate triangles.
             */
            for (short k = 0; k < maxVertexCount; k++)
            {
                short loopPoint = (short)(maxVertexCount * 2);
                //https://media.discordapp.net/attachments/802291445360623686/1036076440514482176/unknown.png

                short nextOuterIndex = (short)(k + 1);
                if (nextOuterIndex >= maxVertexCount)
                    nextOuterIndex = 0;



                indices[k * 6] = (short)(k % loopPoint);
                indices[k * 6 + 1] = (short)((k + maxVertexCount)); //First triangle
                indices[k * 6 + 2] = (short)(nextOuterIndex + maxVertexCount);

                indices[k * 6 + 3] = (short)(k);
                indices[k * 6 + 4] = nextOuterIndex; //Second triangle
                indices[k * 6 + 5] = (short)(nextOuterIndex + maxVertexCount);
            }
        }
    }
}
