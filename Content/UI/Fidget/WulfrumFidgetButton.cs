namespace CalamityFables.Content.UI
{
    public class WulfrumFidgetButton : FidgetToyUI
    {
        public static readonly SoundStyle ClickSound = new SoundStyle("CalamityFables/Sounds/Fidget/WulfrumButtonClick") { MaxInstances = 0 };

        public float squish;
        public float leverProgress;
        public float shakeProgress;
        public float consecutiveShakes;
        public ref int Clicks => ref Main.LocalPlayer.Fables().deathFidgetData.wulfrumButtonClickCount;

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
            Texture2D buttonBaseTex = ModContent.Request<Texture2D>(AssetDirectory.DeathFidgets + "WulfrumButtonPlate").Value;
            Texture2D buttonBaseOutlineTex = ModContent.Request<Texture2D>(AssetDirectory.DeathFidgets + "WulfrumSwitchPlateOutline").Value;
            Texture2D buttonTex = ModContent.Request<Texture2D>(AssetDirectory.DeathFidgets + "WulfrumButton").Value;
            Texture2D antiBloom = AssetDirectory.CommonTextures.BigBloomCircle.Value;


            Vector2 position = dimensions.Center() - buttonBaseTex.Size() / 2f;
            dimensions.X -= buttonBaseTex.Width / 2;
            dimensions.Y -= buttonBaseTex.Height / 2;

            //Draw anti bloom
            spriteBatch.Draw(antiBloom, position, null, Color.Black * 0.3f, 0, antiBloom.Size() / 2f, 0.5f, SpriteEffects.None, 0);
            spriteBatch.Draw(antiBloom, position, null, Color.Black * 0.2f, 0, antiBloom.Size() / 2f, 0.7f, SpriteEffects.None, 0);

            position += Main.rand.NextVector2Circular(6f, 6f) * shakeProgress;

            float wobbleSquish = (float)(Math.Sin(squish * MathHelper.Pi * 3f)) * 0.1f * (float)Math.Pow(squish, 0.7f);
            Vector2 scale = new Vector2(1 - wobbleSquish * 0.7f, 1 + wobbleSquish);
            //Draw level base
            spriteBatch.Draw(buttonBaseTex, position, null, Color.White, 0, buttonBaseTex.Size() / 2f, scale, SpriteEffects.None, 0);

            //Draw the outline
            if (dimensions.Contains(Main.MouseScreen.ToPoint()))
            {
                spriteBatch.Draw(buttonBaseOutlineTex, position, null, Main.OurFavoriteColor, 0, buttonBaseOutlineTex.Size() / 2f, scale, SpriteEffects.None, 0);

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    SoundEngine.PlaySound(ClickSound with { Pitch = consecutiveShakes / 16f * 0.7f });

                    squish = 1f;
                    leverProgress = 1f;
                    shakeProgress = 1f;
                    Clicks++;
                    consecutiveShakes = (float)Math.Ceiling(consecutiveShakes) + 1;

                    if (Clicks > 100 && Math.Log10(Clicks) % 1 == 0)
                        SoundEngine.PlaySound(SoundDirectory.CommonSounds.Comedy);
                }
            }

            //Draw the button
            Rectangle buttonFrame = buttonTex.Frame(1, 2, 0, (int)Math.Ceiling(squish - 0.1f));
            Vector2 buttonScale = scale * (1f + 0.2f * (float)Math.Pow(squish, 2f));


            buttonScale.Y *= (1f + 0.7f * (float)Math.Pow(squish, 3f));
            buttonScale.X *= (1f + 0.4f * (float)Math.Pow(Math.Sin(squish * MathHelper.Pi), 2f)) * (float)Math.Pow(Math.Clamp(1 - (squish - 0.5f / 0.5f), 0f, 1f), 0.7f);


            Vector2 buttonOrigin = new Vector2(buttonFrame.Width / 2f, 20);
            Vector2 buttonPosition = position + Vector2.UnitY * (buttonOrigin.Y * scale.Y * 0.5f - 1);
            spriteBatch.Draw(buttonTex, buttonPosition, buttonFrame, Color.White, 0, buttonOrigin, buttonScale, SpriteEffects.None, 0);

            DrawScoreboard(spriteBatch, position, scale);

            //Lower the variables
            squish -= 1 / (60f * 0.2f);
            if (squish < 0)
                squish = 0;

            shakeProgress -= 1 / (60f * 0.1f);
            if (shakeProgress < 0)
                shakeProgress = 0;

            consecutiveShakes -= 1 / (60f * 0.14f);
            consecutiveShakes = MathHelper.Clamp(consecutiveShakes, 0, 16);
        }

        public void DrawScoreboard(SpriteBatch spriteBatch, Vector2 position, Vector2 scale)
        {
            Texture2D scoreboardTex = ModContent.Request<Texture2D>(AssetDirectory.DeathFidgets + "WulfrumButtonScoreboard").Value;
            Texture2D numbersTex = ModContent.Request<Texture2D>(AssetDirectory.DeathFidgets + "WulfrumNumbers").Value;

            //Get the click count as a list of digits
            List<int> digits = new List<int>();
            int countedScore = Clicks;
            while (countedScore > 0)
            {
                digits.Add(countedScore % 10);
                countedScore = countedScore / 10;
            }
            digits.Reverse();

            //Add the 2 empty 0s at the start
            while (digits.Count < 3)
                digits.Insert(0, 0);

            //Draw the panel
            Vector2 panelHingePosition = position + Vector2.UnitX * 24f * scale.X;
            Vector2 panelOrigin = new Vector2(7, 18);

            //Simple draw if digits under 4
            if (digits.Count <= 3)
                spriteBatch.Draw(scoreboardTex, panelHingePosition, null, Color.White, 0, panelOrigin, scale, SpriteEffects.None, 0);
            //Extend the UI otherwise
            else
            {
                Rectangle screenstart = new Rectangle(0, 0, 18, 34);
                spriteBatch.Draw(scoreboardTex, panelHingePosition, screenstart, Color.White, 0, panelOrigin, scale, SpriteEffects.None, 0);

                float screenWidth = digits.Count * (numbersTex.Width + 2f) - 2;
                Rectangle screenStretchSection = new Rectangle(18, 0, 1, 34);
                Vector2 drawPosition = panelHingePosition + Vector2.UnitX * 11f * scale.X;

                Vector2 screenStretchScale = new Vector2(screenWidth, 1f) * scale;
                Vector2 screenStretchOrigin = new Vector2(0, scoreboardTex.Height / 2 + 1);
                spriteBatch.Draw(scoreboardTex, drawPosition, screenStretchSection, Color.White, 0, screenStretchOrigin, screenStretchScale, SpriteEffects.None, 0);

                Rectangle screenEndSection = new Rectangle(40, 0, 10, 34);
                drawPosition += Vector2.UnitX * screenWidth * scale.X;
                spriteBatch.Draw(scoreboardTex, drawPosition, screenEndSection, Color.White, 0, screenStretchOrigin, scale, SpriteEffects.None, 0);

            }

            //Draw the numbers
            int i = digits.Count - 1;
            bool rippleCarryHiddenZero = true;
            Vector2 digitPosition = panelHingePosition + new Vector2(11f, -1f) * scale;

            foreach (int digit in digits)
            {
                if (digit != 0 || i == 0)
                    rippleCarryHiddenZero = false;

                if (!rippleCarryHiddenZero)
                {
                    Rectangle digitFrame = numbersTex.Frame(1, 10, 0, digit, 0, -2);
                    Vector2 digitOrigin = new Vector2(0, digitFrame.Height / 2f);
                    spriteBatch.Draw(numbersTex, digitPosition, digitFrame, Color.White, 0, digitOrigin, scale, SpriteEffects.None, 0);
                }

                digitPosition += Vector2.UnitX * (numbersTex.Width + 2f) * scale.X;
                i--;
            }
        }
    }
}