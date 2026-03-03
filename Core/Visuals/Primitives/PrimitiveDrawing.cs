using Microsoft.Xna.Framework.Graphics;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public partial class Primitives : IDisposable //Credit goes to Oli!!!!
    {
        public bool IsDisposed {
            get;
            private set;
        }

        private DynamicVertexBuffer vertexBuffer;
        private DynamicIndexBuffer indexBuffer;

        private readonly GraphicsDevice device;
        public static BasicEffect BaseEffect;



        public Primitives(GraphicsDevice device, int maxVertices, int maxIndices)
        {
            this.device = device;

            if (device != null)
            {
                Main.QueueMainThreadAction(() =>
                {
                    vertexBuffer = new DynamicVertexBuffer(device, typeof(VertexPositionColorTexture), maxVertices, BufferUsage.None);
                    indexBuffer = new DynamicIndexBuffer(device, IndexElementSize.SixteenBits, maxIndices, BufferUsage.None);
                });
            }
        }

        public static BasicEffect GetBaseEffect()
        {
            BaseEffect = BaseEffect ?? new BasicEffect(Main.graphics.GraphicsDevice) { VertexColorEnabled = true, TextureEnabled = false };
            return BaseEffect;
        }

        public void Render(Effect effect, Matrix translation, Matrix view)
        {
            if (vertexBuffer is null || indexBuffer is null)
                return;

            VertexBufferBinding[] prefVBuffer = device.GetVertexBuffers();
            IndexBuffer prevIndices = device.Indices;
            BlendState prevBlendState = device.BlendState;
            DepthStencilState prevDepthStencil = device.DepthStencilState;
            RasterizerState prevRasterizer = device.RasterizerState;

            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

            if (RenderTargetsManager.NoViewMatrixPrims)
                view = Matrix.Identity;

            if (effect is null)
            {
                effect = GetBaseEffect();
                BaseEffect.View = view;
                BaseEffect.Projection = projection;
                BaseEffect.World = translation;
            }
            else
            {
                effect.Parameters["uWorldViewProjection"].SetValue(translation * view * projection);
            }

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexBuffer.VertexCount, 0, indexBuffer.IndexCount / 3);
            }

            device.SetVertexBuffers(prefVBuffer);
            device.Indices = prevIndices;
            device.BlendState = prevBlendState;
            device.DepthStencilState = prevDepthStencil;
            device.RasterizerState = prevRasterizer;

            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            if (SpriteBatchHelper.HasBegun) //&& SpriteBatchHelper.CurrentSortMode == SpriteSortMode.Immediate)
                SpriteBatchHelper.ApplyDefaultEffectPass();
        }


        public void Render(Effect effect, Matrix translation, bool useUiMatrix = false)
        {
            if (vertexBuffer is null || indexBuffer is null)
                return;

            Render(effect, translation, useUiMatrix ? Main.UIScaleMatrix : Main.GameViewMatrix.TransformationMatrix);
        }

        public void SetVertices(VertexPositionColorTexture[] vertices)
        {
            vertexBuffer?.SetData(0, vertices, 0, vertices.Length, VertexPositionColorTexture.VertexDeclaration.VertexStride, SetDataOptions.Discard);
        }

        public void SetIndices(short[] indices)
        {
            indexBuffer?.SetData(0, indices, 0, indices.Length, SetDataOptions.Discard);
        }

        public void Dispose()
        {
            IsDisposed = true;

            GC.SuppressFinalize(this);
            Main.QueueMainThreadAction(() =>
            {
                vertexBuffer?.Dispose();
                indexBuffer?.Dispose();
            });
        }
    }

    public abstract class PrimitiveShape : IDisposable
    {
        protected Primitives primitives;

        public virtual Vector2 DefaultOffset => Main.screenPosition;


        public abstract int VertexCount { get; }
        public abstract int IndexCount { get; }
        public virtual bool InvalidForDrawing => false;

        public void InitializePrimitives() => primitives = new Primitives(Main.graphics.GraphicsDevice, VertexCount, IndexCount);


        public abstract void GenerateMesh(out VertexPositionColorTexture[] mainVertices, out short[] mainIndices);
        public virtual void SetupMesh()
        {
            GenerateMesh(out VertexPositionColorTexture[] mainVertices, out short[] mainIndices);
            primitives.SetVertices(mainVertices);
            primitives.SetIndices(mainIndices);
        }


        public bool SetupRender()
        {
            if (InvalidForDrawing || primitives == null)
                return false;
            if (primitives.IsDisposed)
                InitializePrimitives();

            GhostTrailsHandler.LogDisposable(this);

            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            SetupMesh();
            return true;
        }

        public void Render(Effect effect = null, Vector2? offset = null, bool useUiMatrix = false)
        {
            Vector2 offset_ = offset.GetValueOrDefault(-DefaultOffset);
            Render(effect, Matrix.CreateTranslation(offset_.Vec3()), useUiMatrix);
        }

        public void Render(Effect effect, Matrix translation, bool useUiMatrix = false)
        {
            if (!SetupRender())
                return;

            primitives.Render(effect, translation, useUiMatrix);
        }

        public void RenderWithView(Matrix view, Effect effect, Matrix? translation)
        {
            if (!SetupRender())
                return;

            if (!translation.HasValue)
                Matrix.CreateTranslation(-DefaultOffset.Vec3());
            primitives.Render(effect, translation.Value, view);
        }

        public void RenderWithView(Matrix view, Effect effect, Vector2? offset = null)
        {
            Vector2 offset_ = offset.GetValueOrDefault(-DefaultOffset);
            RenderWithView(view, effect, Matrix.CreateTranslation(offset_.Vec3()));
        }


        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
            primitives?.Dispose();
        }
    }
}