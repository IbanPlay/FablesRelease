using CalamityFables.Content.NPCs.Cursed;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics;

namespace CalamityFables.Core
{
    public static class LightedMeshRendering
    {
        private static short[] _indices;
        private static VertexPositionColorTexture[] _vertices;

        public static void Render(Effect effect, Matrix worldViewProjection, Rectangle tileArea)
        {
            //Sample corner colors for every other tile, since itd be redundant to do it for every tile
            int horizontalSamples = tileArea.Width / 2 + 1;
            int verticalSamples = tileArea.Height / 2 + 1;

            int meshWidth = tileArea.Width + 1;
            int meshHeight = tileArea.Height + 1;

            int vertexCount = meshWidth * meshHeight;
            int indexCount = tileArea.Width * tileArea.Height * 6;

            _indices = new short[indexCount];
            _vertices = new VertexPositionColorTexture[vertexCount];

            //Make the vertices
            for (int j = 0; j < verticalSamples; j++)
            {
                for (int i = 0; i < horizontalSamples; i++)
                {
                    Lighting.GetCornerColors(tileArea.X + i * 2, tileArea.Y + j * 2, out var vertexColors);
                    bool rightEdge = i * 2 == tileArea.Width;
                    bool bottomEdge = j * 2 == tileArea.Height;

                    Vector2 topLeftUv = new Vector2((i * 2f) / (float)(meshWidth - 1), (j * 2f) / (float)(meshHeight - 1));
                    Vector2 botRightUv = new Vector2((i * 2f + 1) / (float)(meshWidth - 1), (j * 2f + 1) / (float)(meshHeight - 1));

                    _vertices[i * 2 + j * 2 * meshWidth] = new VertexPositionColorTexture(              new Vector3(tileArea.X + i * 2    , tileArea.Y + j * 2    , 0f) * 16f, vertexColors.TopLeftColor, topLeftUv);
                    if (!rightEdge)
                        _vertices[i * 2 + 1 + j * 2 * meshWidth] = new VertexPositionColorTexture(      new Vector3(tileArea.X + i * 2 + 1, tileArea.Y + j * 2    , 0f) * 16f, vertexColors.TopRightColor, new Vector2(botRightUv.X, topLeftUv.Y));
                    if (!bottomEdge)
                        _vertices[i * 2 + (j * 2 + 1) * meshWidth] = new VertexPositionColorTexture(    new Vector3(tileArea.X + i * 2    , tileArea.Y + j * 2 + 1, 0f) * 16f, vertexColors.BottomLeftColor, new Vector2(topLeftUv.X, botRightUv.Y));
                    if (!bottomEdge && !rightEdge)
                        _vertices[i * 2 + 1 + (j * 2 + 1) * meshWidth] = new VertexPositionColorTexture(new Vector3(tileArea.X + i * 2 + 1, tileArea.Y + j * 2 + 1, 0f) * 16f, vertexColors.BottomRightColor, botRightUv);
                }
            }

            //Make the indices
            int primCount = 0;

            for (int j = 0; j < meshHeight - 1; j++)
            {
                for (int i = 0; i < meshWidth - 1; i++)
                {
                    _indices[primCount] = (short)(i + j * meshWidth);
                    _indices[primCount + 1] = (short)(i + 1 + j * meshWidth);
                    _indices[primCount + 2] = (short)(i + (j + 1) * meshWidth);
                    _indices[primCount + 3] = (short)(i + 1 + j * meshWidth);
                    _indices[primCount + 4] = (short)(i + 1 + (j + 1) * meshWidth);
                    _indices[primCount + 5] = (short)(i + (j + 1) * meshWidth);

                    primCount+=6;
                }
            }

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                effect.Parameters["uWorldViewProjection"].SetValue(worldViewProjection);
                pass.Apply();
                Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, vertexCount, _indices, 0, indexCount / 3);
            }
        }

        /// <summary>
        /// Renders the same area without recalculating vertices and incies. Use this if you're gonna draw the mesh with multiple layers 
        /// </summary>
        public static void RenderAgain(Effect effect, Matrix worldViewProjection)
        {
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                effect.Parameters["uWorldViewProjection"].SetValue(worldViewProjection);
                pass.Apply();
                Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, _vertices.Length, _indices, 0, _indices.Length / 3);
            }
        }
    }
}
