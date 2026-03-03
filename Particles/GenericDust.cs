using rail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CalamityFables.Particles
{
    public class GenericDust : Particle
    {
        public static int DUST_DRAW_WIDTH = 10;
        public static int DUST_DRAW_HEIGHT = 10;
        public static int VARIANT_COUNT = 3;
        public static int RAND_VARIANT_COUNT = 3;

        public override string Texture => AssetDirectory.Particles + nameof(GenericDust);
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public Color outlineColor;

        public int variant;

        public int randVariant;

        public float opacityScale = 1f;
        public float alphaScale = 1f;
        public float scaleDecay = 0.95f;
        public bool useLighting;
        public bool useGravity;

        public GenericDust(Vector2 position,
            Vector2 velocity,
            Color fillColor,
            Color outlineColor,
            int lifetime = 60,
            int variant = 0,
            float scale = 1f,
            float opacity = 1f,
            float alphaScale = 1f,
            float scaleDecay = 0.95f,
            bool usesLighting = false,
            bool usesGravity = false)
        {
            Position = position;
            Velocity = velocity;

            Color = fillColor;
            this.outlineColor = outlineColor;

            Lifetime = lifetime;
            this.variant = variant;

            randVariant = Main.rand.Next(0, VARIANT_COUNT + 1);

            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Scale = scale;

            opacityScale = opacity;
            this.alphaScale = alphaScale;
            this.scaleDecay = scaleDecay;

            useLighting = usesLighting;
            useGravity = usesGravity;
        }

        public override void Update()
        {
            Velocity *= 0.94f;
            Rotation += Velocity.X / 30;
            Scale *= scaleDecay;

            if (useGravity)
            {
                Velocity.Y += 0.2f;
            }

            if (Scale <= 0.2f)
            {
                Kill();
            }
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Rectangle fillSourceRect = new(DUST_DRAW_WIDTH * 2 * variant, DUST_DRAW_HEIGHT * randVariant, DUST_DRAW_WIDTH, DUST_DRAW_HEIGHT);
            Rectangle outlineSourceRect = new(DUST_DRAW_WIDTH * (2 * variant + 1), DUST_DRAW_HEIGHT * randVariant, DUST_DRAW_WIDTH, DUST_DRAW_HEIGHT);

            float opacity = Math.Clamp((1f - LifetimeCompletion) * 5, 0f, 1f) * opacityScale;

            Color oc = outlineColor * opacity;
            Color fc = Color * opacity;

            oc.A = (byte)(oc.A * alphaScale);
            fc.A = (byte)(fc.A * alphaScale);

            if (useLighting)
            {
                Color lightColor = Lighting.GetColor(Position.ToTileCoordinates());

                oc = oc.MultiplyRGB(lightColor);
                fc = fc.MultiplyRGB(lightColor);
            }

            Texture2D tex = ParticleHandler.GetTexture(Type);
            Main.EntitySpriteDraw(tex, Position - Main.screenPosition, outlineSourceRect, oc, Rotation, outlineSourceRect.Size() / 2, Scale, SpriteEffects.None);
            Main.EntitySpriteDraw(tex, Position - Main.screenPosition, fillSourceRect, fc, Rotation, fillSourceRect.Size() / 2, Scale, SpriteEffects.None);
        }
    }
}
