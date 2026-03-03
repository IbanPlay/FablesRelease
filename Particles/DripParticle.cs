using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalamityFables.Particles
{
    public class DripParticle : Particle, IDrawPixelated
    {
        public override string Texture => AssetDirectory.Particles + "DripParticle";

        Color edgeColor;
        public float opacityScale;
        public bool ignoreTiles;
        public bool ignoreLighting;

        public static Asset<Texture2D> EdgeTex;
        public DrawhookLayer layer => DrawhookLayer.AbovePlayer;

        public override bool UseCustomDraw => true;

        public override bool SetLifetime => false;

        public DripParticle(Vector2 position, Color coreColor, Color edgeColor, float scale, bool ignoreLighting = false)
        {
            Position = position;
            Color = coreColor;
            this.edgeColor = edgeColor;
            Scale = scale;
            Lifetime = Main.rand.Next(60, 60);
            this.ignoreLighting = ignoreLighting;
        }

        public override void Update()
        {
            Velocity.Y += 0.15f;

            if ((float)Math.Pow(LifetimeCompletion, 4f) > 1f ||
                !ignoreTiles && Collision.IsWorldPointSolid(Position, true))
            {
                Kill();
            }
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {

        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Texture2D tex = ParticleTexture;
            EdgeTex ??= ModContent.Request<Texture2D>(AssetDirectory.Particles + "DripParticleEdge");

            float opacity = (1f - (float)Math.Pow(LifetimeCompletion, 4f)) * Math.Clamp(LifetimeCompletion * 4f, 0, 1);
            int heightStretch = (int)(Velocity.Y * 2f) + 4;
            float finalScale = 0.5f;

            Rectangle topFrame = new(0, 0, 6, 4);
            Rectangle midFrame = new(0, topFrame.Height, 6, 6);
            Rectangle bottomFrame = new(0, midFrame.Y + midFrame.Height, 6, 4);

            Vector2 drawPosition = Position - Main.screenPosition;

            Vector2 topPosition = (new Vector2(0, -heightStretch) + drawPosition) * finalScale;
            Vector2 midPosition = (new Vector2(0, -heightStretch * 0.5f) + drawPosition) * finalScale;
            Vector2 bottomPosition = (new Vector2(0, 0) + drawPosition) * finalScale;

            float heightStretchFactor = (float)heightStretch / tex.Height;

            float horizontalStretch = MathHelper.Lerp(4f, 1f, (float)Math.Pow(Math.Clamp(heightStretchFactor, 0f, 1f), 1.3f)) * (0.5f + opacity * 0.5f);
            Vector2 tipScale = new Vector2(horizontalStretch, 1f) * finalScale;
            Vector2 bodyScale = new Vector2(horizontalStretch, heightStretchFactor * 2) * finalScale;

            Color lightColor = Lighting.GetColor(Position.ToTileCoordinates());
            Color finalEdgeColor = edgeColor.MultiplyRGBA(lightColor);
            Color finalCoreColor = Color.MultiplyRGBA(lightColor);

            for (int i = 0; i < 4; i++)
            {
                Vector2 o = (i * MathHelper.PiOver2).ToRotationVector2() * 2f * finalScale;

                Main.EntitySpriteDraw(tex, topPosition + o, topFrame, finalEdgeColor * 0.6f * opacity, 0, topFrame.Size() / 2, tipScale, SpriteEffects.None);
                Main.EntitySpriteDraw(tex, midPosition + o, midFrame, finalEdgeColor * 0.6f * opacity, 0, midFrame.Size() / 2, bodyScale, SpriteEffects.None);
                Main.EntitySpriteDraw(tex, bottomPosition + o, bottomFrame, finalEdgeColor * 0.6f * opacity, 0, bottomFrame.Size() / 2, tipScale, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(tex, topPosition, topFrame, finalCoreColor * opacity, 0, topFrame.Size() / 2, tipScale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, midPosition, midFrame, finalCoreColor * opacity, 0, midFrame.Size() / 2, bodyScale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, bottomPosition, bottomFrame, finalCoreColor * opacity, 0, bottomFrame.Size() / 2, tipScale, SpriteEffects.None);
        }
    }
}
