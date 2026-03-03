namespace CalamityFables.Content.UI
{
    public class WulfrumFidgetSwitch : FidgetToyUI
    {
        public static readonly SoundStyle FlickSound = new SoundStyle("CalamityFables/Sounds/Fidget/WulfrumSwitchFlick") { MaxInstances = 0 };

        public bool flipped;
        public float squish;
        public float leverProgress;
        public float shakeProgress;
        public float consecutiveShakes;

        public override void Update(GameTime gameTime)
        {
            Top.Set(0, 0.35f);
            Left.Set(0, 0.5f);

            Height.Set(56, 0f);
            Width.Set(56, 0f);

            Recalculate();

            base.Update(gameTime);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle dimensions = GetDimensions().ToRectangle();
            Texture2D buttonBaseTex = ModContent.Request<Texture2D>(AssetDirectory.DeathFidgets + "WulfrumSwitchPlate").Value;
            Texture2D buttonBaseOutlineTex = ModContent.Request<Texture2D>(AssetDirectory.DeathFidgets + "WulfrumSwitchPlateOutline").Value;
            Texture2D leverTex = ModContent.Request<Texture2D>(AssetDirectory.DeathFidgets + "WulfrumSwitchLever").Value;
            Texture2D antiBloom = AssetDirectory.CommonTextures.BigBloomCircle.Value;

            Vector2 position = dimensions.Center() - buttonBaseTex.Size() / 2f;
            dimensions.X -= buttonBaseTex.Width / 2;
            dimensions.Y -= buttonBaseTex.Height / 2;

            //Draw anti bloom
            spriteBatch.Draw(antiBloom, position, null, Color.Black * 0.3f, 0, antiBloom.Size() / 2f, 0.5f, SpriteEffects.None, 0);
            spriteBatch.Draw(antiBloom, position, null, Color.Black * 0.2f, 0, antiBloom.Size() / 2f, 0.7f, SpriteEffects.None, 0);

            position += Main.rand.NextVector2Circular(6f, 6f) * shakeProgress;

            float wobbleSquish = (float)(Math.Sin(squish * MathHelper.Pi * 3f)) * 0.3f * (float)Math.Pow(squish, 0.7f);
            Vector2 scale = new Vector2(1 - wobbleSquish * 0.7f, 1 + wobbleSquish);
            //Draw level base
            spriteBatch.Draw(buttonBaseTex, position, null, Color.White, 0, buttonBaseTex.Size() / 2f, scale, SpriteEffects.None, 0);

            //Draw the outline
            if (dimensions.Contains(Main.MouseScreen.ToPoint()))
            {
                spriteBatch.Draw(buttonBaseOutlineTex, position, null, Main.OurFavoriteColor, 0, buttonBaseOutlineTex.Size() / 2f, scale, SpriteEffects.None, 0);

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    SoundEngine.PlaySound(FlickSound with { Pitch = consecutiveShakes / 16f * 0.7f });

                    squish = 1f;
                    leverProgress = 1f;
                    shakeProgress = 1f;
                    consecutiveShakes = (float)Math.Ceiling(consecutiveShakes) + 1;
                    flipped = !flipped;
                }
            }

            //Draw the lever
            Vector2 leverPosition = position + Vector2.UnitY * -8 * (flipped ? 1 : -1) * (float)Math.Pow(1 - leverProgress, 0.1f);
            spriteBatch.Draw(leverTex, leverPosition, null, Color.White, 0, leverTex.Size() / 2f, scale, SpriteEffects.None, 0);

            squish -= 1 / (60f * 0.2f);
            if (squish < 0)
                squish = 0;

            leverProgress -= 1 / (60f * 0.1f);
            if (leverProgress < 0)
                leverProgress = 0;

            shakeProgress -= 1 / (60f * 0.1f);
            if (shakeProgress < 0)
                shakeProgress = 0;

            consecutiveShakes -= 1 / (60f * 0.14f);
            consecutiveShakes = MathHelper.Clamp(consecutiveShakes, 0, 16);
        }
    }
}