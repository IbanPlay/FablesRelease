using CalamityFables.Helpers;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System.IO.Pipelines;
using System.Reflection;
using Terraria.GameContent.Drawing;

namespace CalamityFables.Core
{
    /// <summary>
    /// I am having, a navier stroke
    /// </summary>
    public class NavierStrokeCanvas
    {
        private int gridResX = 90;
        private int gridResY = 90;

        public float CellSizeMultiplier = 1f;

        /// <summary>
        /// The thickness of the fade gradient at the left and right edges of the canvas
        /// </summary>
        public int fadeBorderX = 10;
        /// <summary>
        /// The thickness of the fade gradient at the top and bottom edges of the canvas
        /// </summary>
        public int fadeBorderY = 10;

        /// <summary>
        /// How much does the density bleed into neighboring pixels
        /// </summary>
        public float densityDiffusion = 0.1f;
        /// <summary>
        /// How much does the velocity bleed into neighboring pixels
        /// </summary>
        public float velocityDiffusion = 0.1f;
        /// <summary>
        /// How much of the density is retained after each step
        /// </summary>
        public float densityDissipation = 0.99f;
        /// <summary>
        /// How much of the velocity is retained after each step
        /// </summary>
        public float velocityDissipation = 0.99f;

        /// <summary>
        /// How strongly does the density and velocity move along the canvas
        /// </summary>
        public float advectionStrength = 1f;

        /// <summary>
        /// How many times are the projection values computed. Higher number= more swirly fluid
        /// </summary>
        public int projectionIterations = 5;

        private static Effect fluidShader;
        private static Asset<Texture2D> DataDrawTexture;

        //Targets
        private RenderTarget2D MainTarget;
        private RenderTarget2D BufferTarget;
        private RenderTarget2D DivergenceGrid;
        private RenderTarget2D PressureGrid;
        private RenderTarget2D PressureBuffer;

        /// <summary>
        /// If the canvas has been disposed due to not being used for enough time. All rendertargets have been safely disposed by that point, so you're free to recreate a new one
        /// </summary>
        public bool Disposed = false;

        private int framesSinceLastInUse = 0;
        private bool needsUpdating = false;

        private Vector2 lastUpdatePosition;
        /// <summary>
        /// The position of the canvas. Use this so the canvas's contents get shifted around in the world when moving
        /// </summary>
        public Vector2 position;
        /// <summary>
        /// Multiplier for the displacement recieved by the canvas when moving through the world. Use this when you draw the canvas upscaled
        /// </summary>
        public float displacementMultiplier = 0.5f;

        /// <summary>
        /// Velocity that'll be uniformly added to the fluid during the simulation
        /// </summary>
        public Vector2 addedVelocity;

        /// <summary>
        /// The canvas on which the final fluid sim result is drawn
        /// </summary>
        public Texture2D Canvas
        {
            get
            {
                framesSinceLastInUse = 0;
                return MainTarget;
            }
        }

        public void KeepCanvasActive() => framesSinceLastInUse = 0;


        public Vector2 Size => new Vector2(gridResX, gridResY);

        public static void LoadAssets(Mod mod)
        {
            fluidShader = mod.Assets.Request<Effect>("Effects/FluidSim", AssetRequestMode.ImmediateLoad).Value;
            DataDrawTexture = mod.Assets.Request<Texture2D>("Assets/Misc/FluidSimShapes", AssetRequestMode.AsyncLoad);

            drawSourceVertices = new Vertex4DColor[500 * 4];
            drawSourceIndices = new short[500 * 6];
        }

        public NavierStrokeCanvas(Point resolution, Point? fadeBorderSize = null)
        {
            gridResX = resolution.X;
            gridResY = resolution.Y;

            fadeBorderX = fadeBorderSize.HasValue ? fadeBorderSize.Value.X : 10;
            fadeBorderY = fadeBorderSize.HasValue ? fadeBorderSize.Value.Y : 10;

            Main.QueueMainThreadAction(() =>
            {
                MainTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, gridResX, gridResY, false, SurfaceFormat.Vector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                BufferTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, gridResX, gridResY, false, SurfaceFormat.Vector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

                //Single surface format cuz they only need to hold 1 info
                DivergenceGrid = new RenderTarget2D(Main.graphics.GraphicsDevice, gridResX, gridResY, false, SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                PressureGrid = new RenderTarget2D(Main.graphics.GraphicsDevice, gridResX, gridResY, false, SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                PressureBuffer = new RenderTarget2D(Main.graphics.GraphicsDevice, gridResX, gridResY, false, SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

                NavierStrokeCanvasManager.activeCanvases.Add(this);
            });
        }

        /// <summary>
        /// Updates the canvas in the game logic loop
        /// </summary>
        public void UpdateLogic()
        {
            framesSinceLastInUse++;
            needsUpdating = true;

            if (framesSinceLastInUse > 10 * 60)
            {
                Disposed = true;
                MainTarget.Dispose();
                BufferTarget.Dispose();
                DivergenceGrid.Dispose();
                PressureGrid.Dispose();
                PressureBuffer.Dispose();
            }
        }

        /// <summary>
        /// Updates the canvas in the draw loop, when rendertargets are drawn
        /// </summary>
        public void Update()
        {
            if (!needsUpdating)
                return;
            needsUpdating = false;

            //Shift the texture based on if the canvas was moved
            Vector2 displacement = position - lastUpdatePosition;
            lastUpdatePosition = position;
            if (displacement != Vector2.Zero)
                Recenter(displacement);

            //Set these values to remove some boilerplate in the shader passes
            fluidShader.Parameters["step"].SetValue(new Vector2(1f / gridResX, 1f / gridResY));
            fluidShader.Parameters["stepX"].SetValue(new Vector2(1f / gridResX, 0f));
            fluidShader.Parameters["stepY"].SetValue(new Vector2(0f, 1f / gridResY));
            fluidShader.Parameters["cellSize"].SetValue(CellSizeMultiplier);

            if (fadeBorderX != 0 || fadeBorderY != 0)
                DrawSinks();
            DrawSources();

            Project();
            DiffuseAndDissipate();
            //Project(); //Disabled this projection to save on resources and it doesnt seem to look too meaningfully worse without it?
            Advect();

            Main.instance.GraphicsDevice.SetRenderTarget(null);
        }

        private static Vertex4DColor[] drawSourceVertices;
        private static short[] drawSourceIndices;
        private static int drawSourceBatchIndex;

        #region Recenter
        private void Recenter(Vector2 displacement)
        {
            Main.instance.GraphicsDevice.SetRenderTarget(BufferTarget);
            Main.instance.GraphicsDevice.Clear(Color.Black);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

            Main.spriteBatch.Draw(MainTarget, -displacement * displacementMultiplier, Color.White);
            Main.spriteBatch.End();

            //Swap out the data from the drawn-on target with the results target
            RenderTarget2D temp = MainTarget;
            MainTarget = BufferTarget;
            BufferTarget = temp;
        }

        #endregion

        #region Sources and boundaries
        private struct Vertex4DColor : IVertexType
        {
            public Vector2 Position;
            public Vector4 Color;
            public Vector2 TextureCoordinates;
            public VertexDeclaration VertexDeclaration => _vertexDeclaration;

            private static readonly VertexDeclaration _vertexDeclaration = new(new VertexElement[]
            {
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            });
            public Vertex4DColor(Vector2 position, Vector4 color, Vector2 textureCoordinates)
            {
                Position = position;
                Color = color;
                TextureCoordinates = textureCoordinates;
            }
        }

        /// <summary>
        /// Subscribe to this event to draw sprites as "sources" for the fluid on the canvas with <see cref="DrawOnCanvas(float, Vector2, Vector2, Rectangle, float, Vector2, Vector2)"/>
        /// </summary>
        public event Action<NavierStrokeCanvas> DrawDirectionalSourcesEvent;

        /// <summary>
        /// Subscribe to this event to draw sprites as "sources" for the fluid on the canvas with <see cref="DrawOnCanvas(float, float, Vector2, Rectangle, float)"/>
        /// </summary>
        public event Action<NavierStrokeCanvas> DrawOmnidirectionalSourcesEvent;

        private static bool drawingOmnidirectionalSources = false;

        public event Action DrawBoundariesEvent;

        private void DrawSources()
        {
            //LoadAssets(CalamityFables.Instance);

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, MainTarget.Width, MainTarget.Height, 0, -1, 1);
            fluidShader.Parameters["drawDataMatrix"].SetValue(projection);

            Main.instance.GraphicsDevice.SetRenderTarget(MainTarget);
            Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            Main.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Main.graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            drawingOmnidirectionalSources = false;

            if (DrawDirectionalSourcesEvent != null)
            {
                fluidShader.Parameters["drawDataTexture"].SetValue(DataDrawTexture.Value);
                fluidShader.CurrentTechnique.Passes["DrawTexturePass"].Apply();

                drawSourceBatchIndex = 0;
                DrawDirectionalSourcesEvent?.Invoke(this);

                //Dont draw if theres nothing to draw
                if (drawSourceBatchIndex > 0)
                    Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, drawSourceVertices, 0, drawSourceBatchIndex * 4, drawSourceIndices, 0, drawSourceBatchIndex * 2);
            }
            if (DrawOmnidirectionalSourcesEvent != null)
            {
                fluidShader.Parameters["drawDataTexture"].SetValue(DataDrawTexture.Value);
                fluidShader.CurrentTechnique.Passes["DrawOmnidirectionalTexturePass"].Apply();

                drawSourceBatchIndex = 0;
                drawingOmnidirectionalSources = true;
                DrawOmnidirectionalSourcesEvent?.Invoke(this);

                //Dont draw if theres nothing to draw
                if (drawSourceBatchIndex > 0)
                    Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, drawSourceVertices, 0, drawSourceBatchIndex * 4, drawSourceIndices, 0, drawSourceBatchIndex * 2);
            }

        }

        private void DrawBoundaries()
        {
            return;

            //if (DrawBoundariesEvent == null)
            //    return;

            Main.instance.GraphicsDevice.SetRenderTarget(DivergenceGrid);
            Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            Main.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Main.graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, MainTarget.Width, MainTarget.Height, 0, -1, 1);

            Texture2D maskTexture = DrawOverTileMaskLoader.solidTilesTarget;

            fluidShader.Parameters["drawDataMatrix"].SetValue(projection);
            fluidShader.Parameters["drawDataTexture"].SetValue(maskTexture);
            fluidShader.CurrentTechnique.Passes["DrawTexturePass"].Apply();

            drawSourceBatchIndex = 0;
            Vector4 color = new Vector4(-1f, 0f, 0f, 0f);
            Vector2 topLeft = Vector2.Zero;
            Vector2 unitRight = Vector2.UnitX * gridResX;
            Vector2 unitDown = Vector2.UnitY * gridResY;

            Vector2 cropSize = Size * 1 / displacementMultiplier;
            Vector2 cropOrigin = position - cropSize * 0.5f - Main.screenPosition;
            float uvX = (cropOrigin.X) / (float)maskTexture.Width;
            float uvY = (cropOrigin.Y) / (float)maskTexture.Height;
            float uvW = cropSize.X / (float)maskTexture.Width;
            float uvH = cropSize.Y / (float)maskTexture.Height;

            drawSourceVertices[0] = new Vertex4DColor(Vector2.Zero, color, new Vector2(uvX, uvY));
            drawSourceVertices[1] = new Vertex4DColor(unitRight, color, new Vector2(uvX + uvW, uvY));
            drawSourceVertices[2] = new Vertex4DColor(unitRight + unitDown, color, new Vector2(uvX + uvW, uvY + uvH));
            drawSourceVertices[3] = new Vertex4DColor(unitDown, color, new Vector2(uvX, uvY + uvH));

            drawSourceIndices[0] = (short)(0);
            drawSourceIndices[1] = (short)(1);
            drawSourceIndices[2] = (short)(2);
            drawSourceIndices[3] = (short)(0);
            drawSourceIndices[4] = (short)(2);
            drawSourceIndices[5] = (short)(3);
            drawSourceBatchIndex = 1;

            DrawBoundariesEvent?.Invoke();

            // Flush the vertices and indices and draw them to the render target.
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, drawSourceVertices, 0, drawSourceBatchIndex * 4, drawSourceIndices, 0, drawSourceBatchIndex * 2);
        }

        /// <summary>
        /// Works similar to <see cref="SpriteBatch.Draw"/>, except for drawing density and velocity information to the fluid canvas <br/>
        /// The texture used by this method is Assets/Misc/FluidSimShapes. Feel free to add onto the texture if you need another "brush"
        /// </summary>
        /// <param name="density"></param>
        /// <param name="velocity"></param>
        /// <param name="position"></param>
        /// <param name="frame"></param>
        /// <param name="rotation"></param>
        /// <param name="origin"></param>
        /// <param name="scale"></param>
        public void DrawOnCanvas(float density, Vector2 velocity, Vector2 position, Rectangle frame, float rotation, Vector2 origin, Vector2 scale)
        {
            scale /= CellSizeMultiplier; 

            int i = drawSourceBatchIndex;
            Vector4 color = new Vector4(velocity, density, 0f);
            Vector2 topLeft = position - (origin * scale).RotatedBy(rotation);
            Vector2 unitRight = Vector2.UnitX.RotatedBy(rotation) * frame.Width * scale.X;
            Vector2 unitDown = Vector2.UnitY.RotatedBy(rotation) * frame.Height * scale.Y;

            float uvX = frame.X / (float)DataDrawTexture.Width();
            float uvY = frame.Y / (float)DataDrawTexture.Height();
            float uvW = frame.Width / (float)DataDrawTexture.Width();
            float uvH = frame.Height / (float)DataDrawTexture.Height();

            drawSourceVertices[i * 4] = new Vertex4DColor(topLeft, color, new Vector2(uvX, uvY));
            drawSourceVertices[i * 4 + 1] = new Vertex4DColor(topLeft + unitRight, color, new Vector2(uvX + uvW, uvY));
            drawSourceVertices[i * 4 + 2] = new Vertex4DColor(topLeft + unitRight + unitDown, color, new Vector2(uvX + uvW, uvY + uvH));
            drawSourceVertices[i * 4 + 3] = new Vertex4DColor(topLeft + unitDown, color, new Vector2(uvX, uvY + uvH));

            drawSourceIndices[i * 6] = (short)(i * 4);
            drawSourceIndices[i * 6 + 1] = (short)(i * 4 + 1);
            drawSourceIndices[i * 6 + 2] = (short)(i * 4 + 2);
            drawSourceIndices[i * 6 + 3] = (short)(i * 4);
            drawSourceIndices[i * 6 + 4] = (short)(i * 4 + 2);
            drawSourceIndices[i * 6 + 5] = (short)(i * 4 + 3);

            drawSourceBatchIndex++;
        }


        /// <summary>
        /// Works similar to <see cref="SpriteBatch.Draw"/>, except for drawing density and velocity information to the fluid canvas <br/>
        /// This overload draws a circle shape for the source
        /// </summary>
        /// <param name="density"></param>
        /// <param name="velocity"></param>
        /// <param name="position"></param>
        /// <param name="size"></param>
        /// <param name="hollow"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        public void DrawOnCanvas(float density, Vector2 velocity, Vector2 position, int size, bool hollow, float rotation, Vector2 scale)
        {
            size = Math.Min(size, 14);
            int frameY = 0;
            switch (size)
            {
                case 2:
                    frameY = 2;
                    break;
                case 3:
                    frameY = 5;
                    break;
                case 4:
                    frameY = 9;
                    break;
                case 5:
                    frameY = 14;
                    break;
                case 6:
                    frameY = 20;
                    break;
                case 7:
                    frameY = 27;
                    break;
                case 8:
                    frameY = 35;
                    break;
                case 9:
                    frameY = 44;
                    break;
                case 10:
                    frameY = 54;
                    break;
                case 11:
                    frameY = 65;
                    break;
                case 12:
                    frameY = 77;
                    break;
                case 13:
                    frameY = 90;
                    break;
                case 14:
                    frameY = 104;
                    break;
            }
            Rectangle frame = new Rectangle(hollow ? 15 : 0, frameY, size, size);
            DrawOnCanvas(density, velocity, position, frame, rotation, new Vector2(size / 2f, size/ 2f), scale);
        }

        /// <summary>
        /// Works similar to <see cref="SpriteBatch.Draw"/>, except for drawing density and velocity information to the fluid canvas <br/>
        /// The texture used by this method is Assets/Misc/FluidSimShapes. Feel free to add onto the texture if you need another "brush" <br/>
        /// This override creates an omnidirectional source that has velocity sprouting out in all directions. Must be called during <see cref="DrawOmnidirectionalSourcesEvent"/>
        /// </summary>
        /// <param name="density"></param>
        /// <param name="outpouringVelocity">The speed at which fluid is flowing out of this source</param>
        /// <param name="falloffExponent">The exponent for the fluid values as they go further from the center</param>
        public void DrawOnCanvas(float density, float outpouringVelocity, float falloffExponent, Vector2 position, float scale)
        {
            if (!drawingOmnidirectionalSources)
                throw new InvalidOperationException("DrawOnCanvas with an outpouring velocity can only be called during DrawOmnidirectionalSourcesEvent");
            DrawOnCanvas(density, new Vector2(outpouringVelocity, falloffExponent), position, DataDrawTexture.Value.Bounds, 0f, DataDrawTexture.Size() / 2f, Vector2.One * scale / DataDrawTexture.Size());
        }
        #endregion

        #region Sinks at edges
        private Vertex4DColor[] sinkStripeVertices;
        private short[] sinkStripeIndices;
        private void InitializeSinkEdgeMesh()
        {
            sinkStripeVertices = new Vertex4DColor[4 * 4];
            sinkStripeIndices = new short[6 * 4];
            Vector4 fullFadeColor = new Vector4(1f, 1f, 0f, 1f);
            Vector4 noFadeColor = new Vector4(1f, 1f, 1f, 1f);

            Vector2 canvasXUnit = Vector2.UnitX * gridResX;
            Vector2 canvasYUnit = Vector2.UnitY * gridResY;

            Vector2 fadeXUnit = Vector2.UnitX * fadeBorderX;
            Vector2 fadeYUnit = Vector2.UnitY * fadeBorderY;

            //Top
            sinkStripeVertices[0] = new Vertex4DColor(Vector2.Zero, fullFadeColor, Vector2.Zero);
            sinkStripeVertices[1] = new Vertex4DColor(canvasXUnit, fullFadeColor, Vector2.Zero);
            sinkStripeVertices[2] = new Vertex4DColor(canvasXUnit + fadeYUnit, noFadeColor, Vector2.Zero);
            sinkStripeVertices[3] = new Vertex4DColor(fadeYUnit, noFadeColor, Vector2.Zero);

            //Down
            sinkStripeVertices[4] = new Vertex4DColor(canvasYUnit, fullFadeColor, Vector2.Zero);
            sinkStripeVertices[5] = new Vertex4DColor(canvasYUnit + canvasXUnit, fullFadeColor, Vector2.Zero);
            sinkStripeVertices[6] = new Vertex4DColor(canvasYUnit + canvasXUnit - fadeYUnit, noFadeColor, Vector2.Zero);
            sinkStripeVertices[7] = new Vertex4DColor(canvasYUnit - fadeYUnit, noFadeColor, Vector2.Zero);

            //Left
            sinkStripeVertices[8] = new Vertex4DColor(Vector2.Zero, fullFadeColor, Vector2.Zero);
            sinkStripeVertices[9] = new Vertex4DColor(fadeXUnit, noFadeColor, Vector2.Zero);
            sinkStripeVertices[10] = new Vertex4DColor(canvasYUnit + fadeXUnit, noFadeColor, Vector2.Zero);
            sinkStripeVertices[11] = new Vertex4DColor(canvasYUnit, fullFadeColor, Vector2.Zero);

            //Right
            sinkStripeVertices[12] = new Vertex4DColor(canvasXUnit - fadeXUnit, noFadeColor, Vector2.Zero);
            sinkStripeVertices[13] = new Vertex4DColor(canvasXUnit, fullFadeColor, Vector2.Zero);
            sinkStripeVertices[14] = new Vertex4DColor(canvasXUnit + canvasYUnit, fullFadeColor, Vector2.Zero);
            sinkStripeVertices[15] = new Vertex4DColor(canvasXUnit + canvasYUnit - fadeXUnit, noFadeColor, Vector2.Zero);

            for (int i = 0; i < 4; i++)
            {
                sinkStripeIndices[i * 6] = (short)(i * 4);
                sinkStripeIndices[i * 6 + 1] = (short)(i * 4 + 1);
                sinkStripeIndices[i * 6 + 2] = (short)(i * 4 + 2);
                sinkStripeIndices[i * 6 + 3] = (short)(i * 4);
                sinkStripeIndices[i * 6 + 4] = (short)(i * 4 + 2);
                sinkStripeIndices[i * 6 + 5] = (short)(i * 4 + 3);
            }
        }

        private void DrawSinks()
        {
            //Create a mesh that had fading borders at the edges
            if (sinkStripeIndices == null)
                InitializeSinkEdgeMesh();

            Main.instance.GraphicsDevice.SetRenderTarget(MainTarget);
            Main.graphics.GraphicsDevice.BlendState = CustomBlendStates.Darken;
            Main.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Main.graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, MainTarget.Width, MainTarget.Height, 0, -1, 1);
            fluidShader.Parameters["drawDataMatrix"].SetValue(projection);
            fluidShader.Parameters["drawDataTexture"].SetValue(DataDrawTexture.Value);
            fluidShader.CurrentTechnique.Passes["DrawTexturePass"].Apply();

            // Flush the vertices and indices and draw them to the render target.
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, sinkStripeVertices, 0, 16, sinkStripeIndices, 0, 8);
        }

        #endregion

        #region Simulation stuff
        /// <summary>
        /// Draws the data target onto the result buffer with the desired shader pass <br/>
        /// By default, moves the data in the result buffer back into the data target, but this can be disabled
        /// </summary>
        /// <param name="dataTarget">The rendertarget that will be drawn with the shader</param>
        /// <param name="resultBuffer">The rendertarget that gets drawn to</param>
        /// <param name="pass">The shader pass to use</param>
        /// <param name="blendState"></param>
        /// <param name="copyResultsToDataTarget"></param>
        /// <param name="clearResultBuffer"></param>
        private void DoTheCalculation(ref RenderTarget2D dataTarget, ref RenderTarget2D resultBuffer, EffectPass pass, BlendState blendState = null, bool copyResultsToDataTarget = true, bool clearResultBuffer = true)
        {
            Main.instance.GraphicsDevice.SetRenderTarget(resultBuffer);
            if (clearResultBuffer)
                Main.instance.GraphicsDevice.Clear(Color.Transparent);

            blendState ??= BlendState.AlphaBlend;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
            pass.Apply();
            Main.spriteBatch.Draw(dataTarget, dataTarget.Bounds, Color.White);
            Main.spriteBatch.End();

            if (!copyResultsToDataTarget)
                return;

            //Swap out the data from the drawn-on target with the results target
            RenderTarget2D temp = dataTarget;
            dataTarget = resultBuffer;
            resultBuffer = temp;
        }

        /// <summary>
        /// Spreads out values around neighboring cells to even it out, and fade out the values as well
        /// </summary>
        private void DiffuseAndDissipate()
        {
            //2 different dissipation values for vel and density
            fluidShader.Parameters["diffuse_dissipation"].SetValue(new Vector4(velocityDissipation, velocityDissipation, densityDissipation, 1f));
            fluidShader.Parameters["diffuse_strength"].SetValue(new Vector4(velocityDiffusion, velocityDiffusion, densityDiffusion, 1f));
            DoTheCalculation(ref MainTarget, ref BufferTarget, fluidShader.CurrentTechnique.Passes["DiffusionPass"]);
        }

        /// <summary>
        /// Moves density (and velocity) along the velocity paths
        /// </summary>
        private void Advect()
        {
            fluidShader.Parameters["advect_strenght"].SetValue(advectionStrength * 1 / (float)gridResX);
            fluidShader.Parameters["advect_additionalVelocity"].SetValue(addedVelocity / Size);
            fluidShader.Parameters["simGridButLinear"].SetValue(MainTarget);

            DoTheCalculation(ref MainTarget, ref BufferTarget, fluidShader.CurrentTechnique.Passes["AdvectionPass"]);
        }

        /// <summary>
        /// Removes any sources or sinks of fluid
        /// </summary>
        private void Project()
        {
            //We don't copy the results back to mainTarget for this one
            DoTheCalculation(ref MainTarget, ref DivergenceGrid, fluidShader.CurrentTechnique.Passes["DivergenceInitializationPass"], copyResultsToDataTarget: false);

            DrawBoundaries();

            fluidShader.Parameters["divergenceGrid"].SetValue(DivergenceGrid);

            //Clear pressure grid
            Main.instance.GraphicsDevice.SetRenderTarget(PressureGrid);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);

            for (int i = 0; i < projectionIterations; i++)
            {
                DoTheCalculation(ref PressureGrid, ref PressureBuffer, fluidShader.CurrentTechnique.Passes["IterateProjectionPass"]);
            }

            //We draw the result of our shader in the substract blend mode
            DoTheCalculation(ref PressureGrid, ref MainTarget, fluidShader.CurrentTechnique.Passes["ClearDivergencePass"], CustomBlendStates.Substract, false, false);
        }

        #endregion
    }

    public class NavierStrokeCanvasManager : ILoadable
    {
        public static readonly List<NavierStrokeCanvas> activeCanvases = new();
        public static NavierStrokeCanvas debugCanvas;

        public void Load(Mod mod)
        {
            RenderTargetsManager.DrawToRenderTargetsEvent += UpdateCanvases;
            FablesGeneralSystemHooks.PostUpdateEverythingEvent += UpdateCanvasesLogicLoop;

            //debugCanvas = new(new Point(100, 100));
            //debugCanvas.DrawSourcesEvent += DrawDebugCanvasSources;
            // FablesDrawLayers.DrawThingsAbovePlayersEvent += DrawDebugCanvas; ;
            NavierStrokeCanvas.LoadAssets(mod);
        }

        private void DrawDebugCanvasSources()
        {
            //Vector2 position = new Vector2(50 + MathF.Sin(Main.GlobalTimeWrappedHourly * 2f) * 20f, 50 + MathF.Sin(Main.GlobalTimeWrappedHourly * 4f) * 10f);
            //NavierStrokeCanvas.DrawOnCanvas(11f, Vector2.UnitY.RotatedBy(Main.GlobalTimeWrappedHourly * 4f) * -12f, position, new Rectangle(0, 0, 1, 1), 0f, Vector2.Zero, Vector2.One * 1f);
        }

        private void DrawDebugCanvas(bool afterProjectiles)
        {
            Texture2D tex = TextureAssets.MagicPixel.Value;
            Rectangle pixelFrame = new Rectangle(0, 0, 1, 1);
            Vector2 position = Vector2.One * 400f;

            debugCanvas.position = position;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            Main.spriteBatch.Draw(tex, position, pixelFrame, new Color(0, 0f, 0f), 0f, Vector2.Zero, debugCanvas.Canvas.Size() * 4f, 0, 0);
            Main.spriteBatch.Draw(debugCanvas.Canvas, position, null, Color.White, 0f, Vector2.Zero, 4f, 0, 0);

            Main.spriteBatch.End();

            //Hide the Red/Green velocity channels
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, CustomBlendStates.Substract, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            Main.spriteBatch.Draw(tex, position, pixelFrame, new Color(1f, 1f, 0f), 0f, Vector2.Zero, new Vector2(400, 400), 0, 0);
            Main.spriteBatch.End();
        }

        public void Unload() { }

        private static void UpdateCanvases()
        {
            foreach (NavierStrokeCanvas canvas in activeCanvases)
            {
                canvas.Update();
            }
        }

        private static void UpdateCanvasesLogicLoop()
        {
            foreach (NavierStrokeCanvas canvas in activeCanvases)
            {
                canvas.UpdateLogic();
            }
            activeCanvases.RemoveAll(c => c.Disposed);
        }
    }
}
