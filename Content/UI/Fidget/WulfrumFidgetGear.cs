namespace CalamityFables.Content.UI
{
    public class WulfrumFidgetGear : FidgetToyUI
    {
        public static readonly SoundStyle ClickSound = new SoundStyle("CalamityFables/Sounds/Fidget/WulfrumGearClick") { MaxInstances = 0 };

        public float squish;
        public float leverProgress;
        public float shakeProgress;
        public float consecutiveShakes;
        public float gearRotation;
        public float gearRotationVelocity;
        public Vector2 fidgetPosition = new Vector2(0.35f, 0.5f);

        public Vector2 oldPosition;

        public override void Update(GameTime gameTime)
        {
            Top.Set(0, fidgetPosition.X);
            Left.Set(0, fidgetPosition.Y);

            Height.Set(56, 0f);
            Width.Set(56, 0f);

            Recalculate();

            base.Update(gameTime);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle dimensions = GetDimensions().ToRectangle();
            Texture2D bodyTex = ModContent.Request<Texture2D>(AssetDirectory.WulfrumNPC + "WulfrumRoller").Value;
            Texture2D gearTex = ModContent.Request<Texture2D>(AssetDirectory.WulfrumNPC + "WulfrumRoller_Gear").Value;
            Texture2D gearOutlineTex = ModContent.Request<Texture2D>(AssetDirectory.WulfrumNPC + "WulfrumRoller_GearOutline").Value;
            Texture2D antiBloom = AssetDirectory.CommonTextures.BigBloomCircle.Value;


            Vector2 position = dimensions.Center();

            //Draw anti bloom
            spriteBatch.Draw(antiBloom, position, null, Color.Black * 0.3f, 0, antiBloom.Size() / 2f, 0.5f, SpriteEffects.None, 0);
            spriteBatch.Draw(antiBloom, position, null, Color.Black * 0.2f, 0, antiBloom.Size() / 2f, 0.7f, SpriteEffects.None, 0);

            position += Main.rand.NextVector2Circular(6f, 6f) * shakeProgress;

            float wobbleSquish = (float)(Math.Sin(squish * MathHelper.Pi * 3f)) * 0.1f * (float)Math.Pow(squish, 0.7f);
            Vector2 scale = new Vector2(1 - wobbleSquish * 0.7f, 1 + (float)Math.Pow(squish, 1.3f) * 1f);

            if (squish > 0f)
            {
                Texture2D glow = ModContent.Request<Texture2D>(AssetDirectory.Assets + "Glow").Value;
                Rectangle glowFrame = new Rectangle(0, 0, glow.Width, glow.Height / 2);
                float trailRotation = position.SafeDirectionTo(oldPosition).ToRotation() + MathHelper.PiOver2;
                Vector2 trailSize = new Vector2(squish * 0.6f, oldPosition.Distance(position) / (float)glowFrame.Height);
                spriteBatch.Draw(glow, position, glowFrame, CommonColors.WulfrumBlue with { A = 0 } * squish * 0.3f, trailRotation, glow.Size() / 2f, trailSize, SpriteEffects.None, 0);
            }

            //Draw gear base
            spriteBatch.Draw(gearTex, position, null, Color.White, gearRotation, gearTex.Size() / 2f, 1f, SpriteEffects.None, 0);

            //Draw the outline
            if (dimensions.Contains(Main.MouseScreen.ToPoint()))
            {
                spriteBatch.Draw(gearOutlineTex, position, null, Main.OurFavoriteColor, gearRotation, gearOutlineTex.Size() / 2f, 1f, SpriteEffects.None, 0);

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    SoundEngine.PlaySound(ClickSound with { Pitch = consecutiveShakes / 16f * 0.7f });
                    SoundEngine.PlaySound(NPCs.Wulfrum.WulfrumRoller.JumpUp);

                    squish = 1f;
                    leverProgress = 1f;
                    shakeProgress = 1f;
                    consecutiveShakes = (float)Math.Ceiling(consecutiveShakes) + 1;
                    gearRotationVelocity = 1;

                    oldPosition = position;

                    Vector2 oldfidgetPos = fidgetPosition;
                    fidgetPosition = new Vector2(0.5f, 0.5f) + Main.rand.NextVector2Circular(0.3f, 0.3f);
                    if (fidgetPosition.Distance(oldfidgetPos) > 0.22f)
                        fidgetPosition = oldfidgetPos + oldfidgetPos.DirectionTo(fidgetPosition) * 0.22f;
                }
            }

            Rectangle bodyFrame = bodyTex.Frame(1, 2, 0, (int)Math.Ceiling(squish), 0, -2);
            spriteBatch.Draw(bodyTex, position, bodyFrame, Color.White, 0, bodyFrame.Size() / 2f, scale, SpriteEffects.None, 0);

            //Lower the variables
            squish -= 1 / (60f * 0.2f);
            if (squish < 0)
                squish = 0;

            shakeProgress -= 1 / (60f * 0.1f);
            if (shakeProgress < 0)
                shakeProgress = 0;

            gearRotationVelocity -= 1 / (60f * 0.9f);
            if (gearRotationVelocity < 0)
                gearRotationVelocity = 0;
            else if (gearRotationVelocity > 0.2f)
                SoundEngine.PlaySound(NPCs.Wulfrum.WulfrumRoller.GearClick with { MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew });

            gearRotation += (float)Math.Pow(gearRotationVelocity, 1.4f) * 0.3f;
            consecutiveShakes -= 1 / (60f * 0.64f);
            consecutiveShakes = MathHelper.Clamp(consecutiveShakes, 0, 16);
        }
    }
}