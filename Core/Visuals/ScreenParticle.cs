namespace CalamityFables.Core
{
    //Inspired by spirit's own screenparticle, and this great video https://www.youtube.com/watch?v=c_3TLN2gHow
    public abstract class ScreenParticle : Particle
    {
        public sealed override bool UseCustomDraw => true;

        public bool WrapHorizontal { get; set; }
        public bool WrapVertical { get; set; }

        public Vector2 WrapPadding = Vector2.One * 30f;

        /// <summary>
        /// Parallax layer of 0 is the baseline. The particle will follow screen movements 1:1 <br/>
        /// Parallax in the negatives will be closer to the camera and therefore move faster <br/>
        /// Parallax up to 1 makes the particle scroll slower, up to 1 where it will stop scrolling entirely <br/>
        /// Parallax above 1 will make it scroll in reverse to the camera, creating the illusion of a camera rotation around a circular scene
        /// </summary>
        public float ParallaxRatio = 0;

        public Vector2 OriginalScreenPosition;

        public Vector2 GetDrawPosition()
        {
            Vector2 offset = -Main.screenPosition;
            Vector2 cameraChange = Main.screenPosition - OriginalScreenPosition;
            offset += cameraChange * ParallaxRatio;

            Vector2 drawPosition = Position + offset;

            if (!WrapHorizontal && !WrapVertical)
                return drawPosition;

            if (WrapHorizontal && (drawPosition.X < - WrapPadding.X || drawPosition.X >= Main.screenWidth + WrapPadding.X))
            {
                drawPosition.X += WrapPadding.X;
                drawPosition.X = drawPosition.X.Modulo(Main.screenWidth + WrapPadding.X * 2f) - WrapPadding.X;
            }

            if (WrapVertical && (drawPosition.Y < - WrapPadding.Y || drawPosition.Y >= Main.screenHeight + WrapPadding.Y))
            {
                drawPosition.Y += WrapPadding.Y;
                drawPosition.Y = drawPosition.Y.Modulo(Main.screenHeight + WrapPadding.Y * 2f) - WrapPadding.Y;
            }

            return drawPosition;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D tex = ParticleTexture;
            Rectangle frame = tex.Frame(1, FrameVariants, 0, Variant);
            spriteBatch.Draw(tex, GetDrawPosition(), frame, Color, Rotation, frame.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
        }

    }
}
