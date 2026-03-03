using System.Transactions;
using Terraria.Graphics.Effects;

namespace CalamityFables.Particles
{
    public class IceShardParticle : Particle, IDrawPixelated
    {
        public virtual DrawhookLayer layer => DrawhookLayer.AboveProjectiles;

        public override string Texture => AssetDirectory.Invisible;
        public override bool SetLifetime => true;
        public float AngularMomentum;

        public Color borderColor;

        public VertexPositionTexture[] vertices;
        public bool triangleShard;
        public float randomIndex;

        public IceShardParticle(Vector2 position, Vector2 velocity, float scale, int lifetime, Color? centerColor = null, Color? borderColor = null)
        {
            if (Main.dedServ)
                return;

            triangleShard = Main.rand.NextBool();

            InitializeMesh();
            randomIndex = Main.rand.NextFloat() * 128f;

            Position = position;
            Velocity = velocity;
            Scale = scale;
            Color = centerColor ?? Main.hslToRgb(Main.rand.NextFloat(0.48f, 0.56f), Main.rand.NextFloat(0.6f, 0.94f), 0.8f);
            this.borderColor = borderColor ?? Color.White;

            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            AngularMomentum = Main.rand.NextFloat(-0.1f, 0.1f);
            Lifetime = lifetime;
        }

        public void InitializeMesh()
        {
            vertices = new VertexPositionTexture[triangleShard ? 3 : 4];

            if (triangleShard)
            {
                for (int i = 0; i < 3; i++)
                    vertices[i] = new VertexPositionTexture(Vector2.UnitX.RotatedBy(i / 3f * MathHelper.TwoPi).Vec3() * 10, new Vector2(Main.rand.NextFloat(), i));
            }
            else
            {
                vertices[0] = new VertexPositionTexture(new Vector3(-1, -1, 0) * 10, new Vector2(Main.rand.NextFloat(), 0));
                vertices[1] = new VertexPositionTexture(new Vector3(1, -1, 0) * 10, new Vector2(Main.rand.NextFloat(), 1));
                vertices[2] = new VertexPositionTexture(new Vector3(-1, 1, 0) * 10, new Vector2(Main.rand.NextFloat(), 2));
                vertices[3] = new VertexPositionTexture(new Vector3(1, 1, 0) * 10, new Vector2(Main.rand.NextFloat(), 3));
            }
        }

        public override void Update()
        {
            Velocity *= 0.95f;
            Velocity.Y += 0.1f;

            Scale *= 0.99f;
            if (LifetimeCompletion > 0.7f)
                Scale *= 0.98f - LifetimeCompletion * 0.1f;

            Rotation += AngularMomentum;
            AngularMomentum *= 0.99f;
        }


        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            float scale = Scale * (1f + 0.1f * (float)Math.Sin(Time * 0.5f));

            Effect shardEffect = Scene["IceShardPrimitive"].GetShader().Shader;
            shardEffect.Parameters["rand"].SetValue(randomIndex);
            shardEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
            Matrix scaleRotation = Matrix.CreateRotationZ(Rotation) * Matrix.CreateScale(scale); 

            for (int i = 0; i < 4; i++)
                DrawPrimShape(shardEffect, borderColor, (i / 4f * MathHelper.TwoPi).ToRotationVector2() * 2f, scaleRotation, projection);
            DrawPrimShape(shardEffect, Color, Vector2.Zero, scaleRotation, projection, -1);

            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }

        public void DrawPrimShape(Effect effect, Color color, Vector2 drawOffset, Matrix scaleRotation, Matrix projection, float zOffset = 0f)
        {
            Vector3 offset = (drawOffset - Main.screenPosition + Position).Vec3();
            offset.Z = zOffset;

            effect.Parameters["colorTint"].SetValue(color.ToVector4());
            effect.Parameters["uWorldViewProjection"].SetValue(scaleRotation * Matrix.CreateTranslation(offset) * projection);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                pass.Apply();

            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, triangleShard ? 1 : 2);
        }
    }
}
