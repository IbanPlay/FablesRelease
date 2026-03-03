using CalamityFables.Particles;
using Terraria.UI;

namespace CalamityFables.Content.UI
{
    public class WulfrumFidgetMortar : FidgetToyUI
    {
        public struct FidgetMortarShell
        {
            public Vector2 position;
            public Vector2 velocity;
            public bool exploded;
            public int timeLeft;

            public FidgetMortarShell(Vector2 position, Vector2 velocity)
            {
                exploded = false;
                timeLeft = 600;
                this.position = position;
                this.velocity = velocity;
            }
        }

        public Vector2 MortarPosition {
            get {

                return new Vector2(Main.screenWidth * 0.05f, Main.screenHeight * (0.85f - height * 0.3f + 0.01f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f)));
            }
        }

        public float fireMult;
        public int fireTimer;
        public float velocity;
        public float height;
        public float squish;
        public List<FidgetMortarShell> shells;
        public List<Particle> streaks;

        public override InterfaceScaleType Scale => InterfaceScaleType.None;

        public WulfrumFidgetMortar()
        {
            shells = new List<FidgetMortarShell>();
            streaks = new List<Particle>();
            fireMult = 1;
            fireTimer = 100;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            velocity -= 0.4f;
            height += velocity * 0.2f;

            height = MathHelper.Clamp(height, 0f, 1f);

            velocity = MathHelper.Clamp(velocity, -1f, -1f);

            SimulateMortarShots();
            ShootMortar();
        }

        public void SimulateMortarShots()
        {
            for (int i = 0; i < shells.Count; i++)
            {
                FidgetMortarShell shell = shells[i];
                shell.position += shell.velocity;
                shell.velocity.Y += 0.15f;
                shell.timeLeft--;

                if (shell.position.Y > Main.screenHeight + 100)
                {
                    shell.timeLeft = 0;
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { PlayOnlyIfFocused = true, Volume = 0.2f, MaxInstances = 0 });
                }

                if (shell.exploded)
                    shell.velocity = Vector2.Zero;

                shells[i] = shell;
            }

            shells.RemoveAll(s => s.timeLeft <= 0);

            foreach (Particle streak in streaks)
            {
                streak.Position += streak.Velocity;
                streak.Time++;
                streak.Update();
            }

            streaks.RemoveAll(p => p.Time > p.Lifetime);
        }

        public void ShootMortar()
        {
            if (fireTimer <= 0)
            {
                SoundEngine.PlaySound(NPCs.Wulfrum.WulfrumMortar.FireMortar with { PlayOnlyIfFocused = true });

                int shellCount = 1 + (int)(fireMult / 5);
                for (int i = 0; i < shellCount; i++)
                {
                    Vector2 shellTarget = new Vector2(Main.rand.NextFloat(0.25f, 0.9f) * Main.screenWidth, Main.rand.NextFloat(0.3f, 0.5f) * Main.screenHeight);
                    Vector2 shellVelocity = FablesUtils.GetArcVel(MortarPosition, shellTarget, 0.15f, 200f, null, null, 200f);
                    FidgetMortarShell shell = new FidgetMortarShell(MortarPosition, shellVelocity);
                    shells.Add(shell);
                }
                fireTimer = (int)(Main.rand.Next(80, 130));
                fireMult++;
                fireMult = Math.Min(fireMult, 10);
                velocity = -1f;
            }

            if (fireTimer == 20)
                SoundEngine.PlaySound(NPCs.Wulfrum.WulfrumMortar.ReadyMortar with { PlayOnlyIfFocused = true });

            fireTimer--;
        }


        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Texture2D bodyTex = ModContent.Request<Texture2D>(AssetDirectory.WulfrumNPC + "WulfrumMortar").Value;
            Texture2D cannonTex = ModContent.Request<Texture2D>(AssetDirectory.WulfrumNPC + "WulfrumMortarCannon").Value;
            Texture2D antiBloom = AssetDirectory.CommonTextures.BigBloomCircle.Value;

            Rectangle bodyFrame = bodyTex.Frame(1, 4, 0, (int)((Main.GlobalTimeWrappedHourly * 10f) % 4), 0, -2);

            //Draw anti bloom
            spriteBatch.Draw(antiBloom, MortarPosition, null, Color.Black * 0.2f, 0, antiBloom.Size() / 2f, 0.5f, SpriteEffects.None, 0);
            spriteBatch.Draw(antiBloom, MortarPosition, null, Color.Black * 0.1f, 0, antiBloom.Size() / 2f, 0.7f, SpriteEffects.None, 0);


            spriteBatch.Draw(bodyTex, MortarPosition, bodyFrame, Color.White, 0, bodyFrame.Size() / 2f, 1f, SpriteEffects.FlipHorizontally, 0);
            Vector2 cannonRotationPoint = MortarPosition + new Vector2(2, -12);
            Vector2 origin = new Vector2(0f, cannonTex.Height);
            float gunRotation = -0.8f + (MathHelper.PiOver4);

            Main.spriteBatch.Draw(cannonTex, cannonRotationPoint, null, Color.White, gunRotation, origin, 1f, SpriteEffects.FlipHorizontally, 0f);

            DrawMortarShells(spriteBatch);
            DrawMortarStreaks(spriteBatch);
        }

        public void DrawMortarStreaks(SpriteBatch spriteBatch)
        {
            foreach (Particle streak in streaks)
            {
                streak.CustomDraw(spriteBatch, Vector2.Zero);
            }
        }

        public void DrawMortarShells(SpriteBatch spriteBatch)
        {
            Texture2D shellTex = ModContent.Request<Texture2D>(AssetDirectory.DeathFidgets + "WulfrumMortarShell").Value;
            Texture2D shellOutlineTex = ModContent.Request<Texture2D>(AssetDirectory.DeathFidgets + "WulfrumMortarShellOutline").Value;

            for (int i = 0; i < shells.Count; i++)
            {
                FidgetMortarShell shell = shells[i];

                if (shell.exploded)
                    continue;

                float scale = (0.4f + 0.6f * (float)Math.Pow(Utils.GetLerpValue(600, 575, shell.timeLeft, true), 0.7f));
                float inflate = 3f;

                Rectangle hitbox = new Rectangle((int)(shell.position.X - shellTex.Width * inflate / 2f), (int)(shell.position.Y - shellTex.Height * inflate / 2f), (int)(shellTex.Width * inflate), (int)(shellTex.Height * inflate));

                if (shell.timeLeft < 550 && hitbox.Contains(Main.MouseScreen.ToPoint()))
                {
                    spriteBatch.Draw(shellOutlineTex, shell.position, null, Main.OurFavoriteColor, shell.velocity.ToRotation() + MathHelper.PiOver2, shellOutlineTex.Size() / 2f, scale, SpriteEffects.None, 0);

                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        if (CameraManager.Quake < 30)
                            CameraManager.Quake += 15;

                        SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode);

                        for (int j = 0; j < 6; j++)
                        {
                            Vector2 direction = Main.rand.NextVector2Circular(140f * 0.42f, 140f * 0.42f);

                            if (direction.Length() < 140f * 0.3f)
                                direction = direction.SafeNormalize(Vector2.UnitY) * 140f * 0.42f;

                            Particle streak = new BlastStreak(shell.position, direction, 140f * 0.42f, Color.Gold with { A = 0 }, Color.OrangeRed with { A = 0 } * 0.5f, Color.Gold with { A = 0 } * 0.1f, 0.4f, 12, 3f);
                            streaks.Add(streak);
                        }

                        shell.timeLeft = 22;
                        shell.velocity = Vector2.Zero;
                        shell.exploded = true;
                        shells[i] = shell;
                    }
                }

                spriteBatch.Draw(shellTex, shell.position, null, Color.White, shell.velocity.ToRotation() + MathHelper.PiOver2, shellTex.Size() / 2f, scale, SpriteEffects.None, 0);
            }

            Effect blastEffect = Scene["GyroMortarBlast"].GetShader().Shader;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, blastEffect);

            for (int i = 0; i < shells.Count; i++)
            {
                if (shells[i].exploded)
                    DrawMortarExplosion(spriteBatch, shells[i].position, shells[i].timeLeft, blastEffect);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

        }

        public void DrawMortarExplosion(SpriteBatch spriteBatch, Vector2 position, int timeLeft, Effect blastEffect)
        {

            float dissipateProgress = timeLeft / 30f;
            Texture2D noiseTex = ModContent.Request<Texture2D>(AssetDirectory.Noise + "PerlinNoise").Value;
            Vector2 scale = Vector2.One * (140f / (float)noiseTex.Height) * (float)Math.Pow(1 - dissipateProgress, 0.07f);

            Color innerBlastColor = Color.Lerp(CommonColors.WulfrumBlue, CommonColors.WulfrumGreen, 1 - dissipateProgress) with { A = 222 };
            Color outerBlastColor = Color.Lerp(Color.Gold, Color.OrangeRed, 1 - dissipateProgress);

            float innerBlastTreshold = 1 - (timeLeft - 5f) / 25f;
            float outerBlastTreshold = (float)Math.Pow(1 - dissipateProgress, 2);

            Vector2 innerOffset = new Vector2((float)Math.Pow(dissipateProgress, 2) * 0.3f, 0f);
            Vector2 outerOffset = new Vector2(-(float)Math.Pow(dissipateProgress, 2) * 0.3f, 0f);

            float innerResolution = 140f / 2;
            float outerResolution = 140f / 2 * 1.1f;


            if (timeLeft < 20)
                outerBlastColor *= timeLeft / 20f;

            blastEffect.Parameters["noiseScale"].SetValue(0.5f + dissipateProgress * 0.3f);

            blastEffect.Parameters["edgeFadeDistance"].SetValue(0.05f);
            blastEffect.Parameters["edgeFadePower"].SetValue(3f);
            blastEffect.Parameters["shapeFadeTreshold"].SetValue(0.1f * (1 - outerBlastTreshold));
            blastEffect.Parameters["shapeFadePower"].SetValue(1f);

            blastEffect.Parameters["fresnelDistance"].SetValue(0.3f);
            blastEffect.Parameters["fresnelStrenght"].SetValue(1.6f);
            blastEffect.Parameters["fresnelOpacity"].SetValue(0.25f);

            blastEffect.Parameters["offset"].SetValue(innerOffset);
            blastEffect.Parameters["resolution"].SetValue(innerResolution);
            blastEffect.Parameters["treshold"].SetValue(innerBlastTreshold);
            blastEffect.Parameters["blastColor"].SetValue(innerBlastColor.ToVector4());



            //Middle green
            Main.EntitySpriteDraw(noiseTex, position, null, Color.White, 0, noiseTex.Size() / 2f, scale, SpriteEffects.None, 0);

            blastEffect.Parameters["offset"].SetValue(outerOffset);
            blastEffect.Parameters["resolution"].SetValue(outerResolution);
            blastEffect.Parameters["treshold"].SetValue(outerBlastTreshold);
            blastEffect.Parameters["blastColor"].SetValue(outerBlastColor.ToVector4() with { W = outerBlastColor.A / 255f });

            Main.EntitySpriteDraw(noiseTex, position, null, Color.White, 0, noiseTex.Size() / 2f, scale * 1.05f, SpriteEffects.None, 0);
        }
    }
}