namespace CalamityFables.Particles
{
    public class VortexSwirlParticle : RTParticle
    {
        public override string Texture => AssetDirectory.Invisible;

        private readonly Vector2 Trajectory;
        private readonly Color BackColor;
        private readonly float StartRadius;
        private readonly float RadiusChange;
        private readonly float SwirlWidth;
        private readonly float WidthVariance;

        #region Fine tuning
        /// <summary>
        /// The number of vertex pairs that make up this swirl particle.
        /// </summary>
        public float StripDefinition { get; set; } = 10;

        /// <summary>
        /// An exponent applied to progress while calculating the sine bump.
        /// </summary>
        public float PositionProgressPower { get; set; } = 1f;

        /// <summary>
        /// An exponent applied to progress while calculating the radius.
        /// </summary>
        public float RadiusProgressPower { get; set; } = 1f;

        /// <summary>
        /// Offset applied as a sine bump. Higher values makes the strip curve more.
        /// </summary>
        public Vector2 FlatSineBump { get; set; } = Vector2.UnitY;

        /// <summary>
        /// Offset applied to the radius as a sine bump. Higher values make the cylinder appear more elliptical.
        /// </summary>
        public float RadiusSineBump { get; set; } = 1f;

        #endregion

        public VortexSwirlParticle(Vector2 position, Vector2 trajectory, Color frontColor, Color backColor, float startRadius, float radiusChange, int lifetime = 60, float swirlWidth = 1f, float widthVariance = 1f)
        {
            Position = position;
            Trajectory = trajectory;
            Velocity = Vector2.Zero;
            Color = frontColor;
            BackColor = backColor;
            StartRadius = startRadius;
            RadiusChange = radiusChange;
            Lifetime = lifetime;

            SwirlWidth = swirlWidth;
            WidthVariance = widthVariance;
        }

        /// <summary>
        /// Construcs a primitive strip for this vortex particle. Appends the vertices and indices to the provided arrays.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="indices"></param>
        /// <param name="frameHeight"></param>
        public void GenerateMesh(ref VertexPositionColorTexture[] vertices, ref short[] indices, float frameHeight)
        {
            /*
             *  0---2---4
             *  |  /|  /|
             *  | / | / |
             *  |/  |/  |
             *  1---3---5
             *  
             *  Kinda like any normal prim trail, but drawn twice for different layers
             */

            List<VertexPositionColorTexture> vertexList = [];
            List<short> indexList = [];

            float heightMult = 1f - MathF.Pow(Progress, 4f);

            // Basically drawing the same thing twice, once for the front layer and once for the back
            for (int i = 0; i < 2; i++)
            {
                // Create vertices and index them
                for (int k = 0; k < StripDefinition; k++)
                {
                    // Height between vertices, tapers at the edges and gets thinner with age
                    float stripProgress = Utils.GetLerpValue(0, StripDefinition - 1, k);
                    float swirlHeight = SwirlWidth + WidthVariance * MathF.Sin(stripProgress * MathHelper.Pi);
                    swirlHeight *= heightMult;

                    // Get the 2D position along the cylinder and then transform it into a pseudo 3D position with sines and such
                    Vector2 flatPosition = FlatPositionAlongPath(stripProgress, out float radius);

                    // Find first vertex position, then find the second one by offsetting it by the height
                    Vector2 vertexPos = TorsePosition(flatPosition, radius, out float depth);
                    Vector2 vertexPosTop = vertexPos + Vector2.UnitY * swirlHeight;

                    // Create vertices
                    // Normal for the front layer
                    if (i == 0)
                    {
                        vertexList.Add(new VertexPositionColorTexture(vertexPos.Vec3(), Color, new Vector2(1, depth)));
                        vertexList.Add(new VertexPositionColorTexture(vertexPosTop.Vec3(), Color, new Vector2(1, depth)));
                    }
                    // Add X offset, use BackColor, and change the texture coordinates for the back layer
                    else
                    {
                        vertexList.Add(new VertexPositionColorTexture((vertexPos + Vector2.UnitX * frameHeight).Vec3(), BackColor, new Vector2(-1, depth)));
                        vertexList.Add(new VertexPositionColorTexture((vertexPosTop + Vector2.UnitX * frameHeight).Vec3(), BackColor, new Vector2(-1, depth)));
                    }

                    // index the vertices for each step except the last one
                    if (k >= StripDefinition - 1)
                        continue;

                    // Get current number of vertices for indexing
                    int vertexIndex = (short)(vertices.Length + (k * 2) + (i * StripDefinition * 2));

                    indexList.Add((short)(vertexIndex + 1));    // Vertex 1
                    indexList.Add((short)vertexIndex);          // Vertex 0
                    indexList.Add((short)(vertexIndex + 2));    // Vertex 2

                    indexList.Add((short)(vertexIndex + 1));    // Vertex 1
                    indexList.Add((short)(vertexIndex + 3));    // Vertex 3
                    indexList.Add((short)(vertexIndex + 2));    // Vertex 2
                }
            }

            vertices = [.. vertices, .. vertexList];
            indices = [.. indices, .. indexList];
        }

        public Vector2 FlatPositionAlongPath(float progress, out float radius)
        {
            // Get the progress point of the start and end and then figure out what our progress is along the line
            float startPoint = MathF.Pow(Progress, 1.2f);
            float endPoint = MathF.Pow(Progress, 0.6f);
            float currentProgressPoint = MathHelper.Lerp(startPoint, endPoint, MathF.Pow(progress, PositionProgressPower));

            // Sine bump function based on progress
            float sineBump = MathF.Sin(currentProgressPoint * MathHelper.Pi);

            Vector2 start = Position + Trajectory * startPoint;
            Vector2 end = Position + Trajectory * endPoint;

            radius = MathHelper.Lerp(StartRadius + RadiusChange * startPoint, StartRadius + RadiusChange * endPoint, MathF.Pow(progress, RadiusProgressPower));
            radius += RadiusSineBump * sineBump;

            // Lerp between the two positions and then add the sine displacement
            return Vector2.Lerp(start, end, progress) + sineBump * FlatSineBump;
        }

        public static Vector2 TorsePosition(Vector2 flatPosition, float radius, out float depth)
        {
            float x = MathF.Sin(flatPosition.X * MathHelper.TwoPi) * radius;
            depth = MathF.Cos(flatPosition.X * MathHelper.TwoPi) * radius;

            return new Vector2(x, flatPosition.Y);
        }
    }

    public class VortexSwirlRenderTarget : ParticleRenderTarget
    {
        DrawActionTextureContent RenderTarget;

        public override bool RegisterOnLoad => false;

        public VortexSwirlRenderTarget() { } // Required parameterless constructor

        public VortexSwirlRenderTarget(Point size, Vector2 origin, float scale = 2f)
        {
            Size = size;
            Origin = origin;
            Scale = scale;
        }

        public override void Initialize()
        {
            RenderTarget = new DrawActionTextureContent(DrawSwirls, Size.X, Size.Y, startSpritebatch: false);
            Main.ContentThatNeedsRenderTargets.Add(RenderTarget);
        }

        public override void Dispose()
        {
            RenderTarget?.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(RenderTarget);
            RenderTarget.GetTarget()?.Dispose();
        }

        private void DrawSwirls(SpriteBatch spriteBatch)
        {
            // Check if there are any particles to draw before doing anything
            int particleCount = AssignedParticles.Count;
            if (AssignedParticles is null || particleCount <= 0)
                return;

            Effect swirlEffect = Scene["DustDevilPrimitive"].GetShader().Shader;

            // Translation so its got the proper origin
            Matrix translation = Matrix.CreateTranslation(Origin.Vec3());   // Translation based on origin
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1);    // Translation based on RT size
            swirlEffect.Parameters["uWorldViewProjection"].SetValue(translation * projection);

            VertexPositionColorTexture[] vertices = [];
            short[] indices = [];

            // Construct full mesh from each particle on this target
            for (int i = 0; i < particleCount; i++)
                if (AssignedParticles[i] is VortexSwirlParticle vortexParticle)
                    vortexParticle.GenerateMesh(ref vertices, ref indices, Size.X / 2);

            // Apply shader pass
            swirlEffect.CurrentTechnique.Passes[0].Apply();

            // Draw primitives
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3);
        }

        public override void DrawRenderTarget(SpriteBatch spritebatch, DrawhookLayer layer = DrawhookLayer.AbovePlayer, Rectangle? source = null)
        {
            if (RenderTarget is null)
                return;

            RenderTarget.Request();
            if (!RenderTarget.IsReady)
                return;

            Main.spriteBatch.Draw(RenderTarget.GetTarget(), Position - Main.screenPosition, source, Color.White * Opacity, 0, Origin, Scale, 0, 0);
        }
    }
}
