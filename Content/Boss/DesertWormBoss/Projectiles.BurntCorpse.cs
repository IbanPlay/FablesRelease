using Terraria.DataStructures;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public class BurntCorpse : ModProjectile
    {
        public override string Texture => AssetDirectory.DesertScourge + Name + "Eyes";
        public static readonly SoundStyle SpawnSound = new SoundStyle(SoundDirectory.DesertScourge + "BurntCorpseElectrification") { MaxInstances = 0 };
        public static readonly SoundStyle CrumbleSound = new SoundStyle(SoundDirectory.DesertScourge + "BurntCorpseCrumble") { MaxInstances = 0 };

        public override void Load()
        {
            FablesPlayer.PreKillEvent += BurnCorpseToACrisp;
        }

        private bool BurnCorpseToACrisp(Player player, double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (damageSource.TryGetCausingEntity(out Entity entity) && entity != null && entity is Projectile projectile && projectile.type == ModContent.ProjectileType<DesertScourgeElectroblast>())
            {
                playSound = false;
                genGore = false;

                if (Main.myPlayer == player.whoAmI)
                {
                    Vector2 velocity = player.velocity * 1.3f - Vector2.UnitY * 4f;
                    velocity = velocity.ClampMagnitude(10f);

                    Projectile.NewProjectile(player.GetSource_Misc("PlayerDeath_TombStone"), player.Center, velocity, Type, 0, 0, Main.myPlayer, Main.myPlayer);
                }
            }

            return true;
        }

        public int CorpsePlayer => (int)Projectile.ai[0];
        public Texture2D corpseTexture;
        public bool initializationEffects = false;

        public bool ShouldDeathCheck => Projectile.ai[2] == 0;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 500;
        }

        public int skeletonFlashTime = 40; //How many frames after spawning does the skeleton flash
        public int skeletonFlashDuration = 6; //For how long does an individual flash last

        public int eyeBlinkStart = 30; //When the eye blink starts
        public int eyeBlinkDuration = 60; //How long the blink anim lasts

        public int crumbleStart = 80; //When the crumble effects start appearing

        public int eyeFallStart = 170;
        public int eyeFallDuration = 20;


        public int EyeBlinkStart => 500 - eyeBlinkStart;
        public int EyeBlinkEnd => 500 - eyeBlinkStart - eyeBlinkDuration;
        public int CrumbleStart => 500 - crumbleStart;

        public int EyeFallStart => 500 - eyeFallStart;
        public int EyeFallEnd => 500 - eyeFallStart - eyeFallDuration;



        public override void AI()
        {
            if (false && Projectile.timeLeft == 500 && Main.myPlayer == CorpsePlayer && (!Main.player[CorpsePlayer].active || !Main.player[CorpsePlayer].dead))
            {
                //deactivates it in mp for the syncing
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    new DeactivateProjectilePacket(Projectile).Send(-1, -1, false);
                Projectile.active = false;
                return;
            }

            if (!Main.dedServ)
            {
                if (corpseTexture == null)
                {
                    //If the player somehow isnt active while generating the texture
                    if (!Main.player[CorpsePlayer].active)
                    {
                        CalamityFables.Instance.Logger.Debug("Burnt corpse found inactive player before we could generate a player capture. Despawning projectile");
                        Projectile.active = false;
                        return;
                    }

                    Projectile.spriteDirection = Main.player[CorpsePlayer].direction;
                    PlayerCapture.CapturePlayer(Main.player[CorpsePlayer], StorePlayerTarget, 5, 5);
                }
                else
                    PlayerCapture.TrackCapture(corpseTexture);

                if (!initializationEffects)
                {
                    SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                    initializationEffects = true;

                    Rectangle dustRectangle = new Rectangle((int)Projectile.Center.X - 20, (int)Projectile.Center.Y - 25, 40, 50);
                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 position = Main.rand.NextVector2FromRectangle(dustRectangle);
                        Vector2 dustVelocity = Projectile.Center.SafeDirectionTo(position) * Main.rand.NextFloat(0.3f, 2f);
                        Dust d = Dust.NewDustPerfect(position, Main.rand.NextBool(4) ? DustID.Torch : DustID.Wraith, dustVelocity, Scale: Main.rand.NextFloat(1f, 2.6f));
                        d.noGravity = true;
                    }
                }
            }

            //Dissolve into dust
            if (Projectile.timeLeft < CrumbleStart && Projectile.timeLeft > CrumbleStart - 70)
            {
                if (Main.rand.NextFloat() < 0.5f + FablesUtils.SineBumpEasing(Utils.GetLerpValue(CrumbleStart, CrumbleStart - 70, Projectile.timeLeft, true)) * 0.3f)
                {
                    float crumbleProgress = Utils.GetLerpValue(CrumbleStart - 50, CrumbleStart - 70, Projectile.timeLeft, true);
                    float height = 50 - 20f * crumbleProgress;
                    float dustStartHeight = 25 + crumbleProgress * 10;

                    Rectangle dustRectangle = new Rectangle((int)Projectile.Center.X - 15, (int)(Projectile.Center.Y - dustStartHeight), 30, (int)height);
                    Vector2 dustVelocity = new Vector2(-Projectile.spriteDirection * Main.rand.NextFloat(0.1f, 0.3f), -Main.rand.NextFloat(0.6f, 2.4f));
                    Dust d = Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(dustRectangle), DustID.Wraith, dustVelocity, Scale: Main.rand.NextFloat(1f, 2.6f));
                    d.noGravity = true;
                }

                Projectile.velocity *= 0.9f;

                if (Projectile.timeLeft == CrumbleStart - 20)
                    SoundEngine.PlaySound(CrumbleSound, Projectile.Center);
            }

            else if (Projectile.timeLeft >= CrumbleStart)
            {
                Projectile.velocity *= 0.9f;
                Projectile.rotation = Main.rand.NextFloat(-0.2f, 0.2f) * MathF.Pow(Utils.GetLerpValue(440, 500, Projectile.timeLeft, true), 2f);
            }

            if (Projectile.timeLeft < EyeBlinkStart && Projectile.timeLeft > EyeBlinkEnd)
            {
                int currentFrame = (int)(Utils.GetLerpValue(EyeBlinkStart, EyeBlinkEnd, Projectile.timeLeft, true) * 10);
                int lastFrame = (int)(Utils.GetLerpValue(EyeBlinkStart, EyeBlinkEnd, Projectile.timeLeft + 1, true) * 10);

                if (currentFrame != lastFrame && (currentFrame == 5 || currentFrame == 7))
                    SoundEngine.PlaySound(currentFrame == 5 ? SoundDirectory.CommonSounds.LooneyBlink1 : SoundDirectory.CommonSounds.LooneyBlink2, Projectile.Center);
            }

            if (Projectile.timeLeft < EyeFallEnd)
                Projectile.Kill();
        }

        public void StorePlayerTarget(PlayerCapture.PlayerTargetHolder target) => corpseTexture = target.Target;

        public Asset<Texture2D> Skeleton;

        public override bool PreDraw(ref Color lightColor)
        {
            if (corpseTexture == null)
                return false;

            float progress = FablesUtils.PolyInEasing(Utils.GetLerpValue(CrumbleStart - 40, CrumbleStart - 90, Projectile.timeLeft, true), 2f);
            float fadeProgress = FablesUtils.PolyInOutEasing(Utils.GetLerpValue(CrumbleStart - 30, CrumbleStart - 90, Projectile.timeLeft, true), 2f);

            Effect effect = Scene["BurntCorpseFade"].GetShader().Shader;
            effect.Parameters["noiseStretch"].SetValue(new Vector2(5f, 1.4f));
            effect.Parameters["noiseOffset"].SetValue(new Vector2(0, 0.2f * progress));
            effect.Parameters["fadeProgress"].SetValue(fadeProgress * 2f);
            effect.Parameters["noiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "MilkyBlobNoise").Value);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

            Vector2 position = Projectile.Center;
            position.Y -= progress * 40f;
            position.X -= progress * 20f * Projectile.spriteDirection;

            Main.EntitySpriteDraw(corpseTexture, position - Main.screenPosition, null, Color.Black, Projectile.rotation, corpseTexture.Size() / 2f + Vector2.UnitY * 3f, 1f, 0, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            
            SpriteEffects flip = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            if (Projectile.timeLeft > 500 - skeletonFlashTime && Projectile.timeLeft % (skeletonFlashDuration * 2) < skeletonFlashDuration)
            {
                Skeleton ??= ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + Name + "Skeleton");
                Texture2D skelly = Skeleton.Value;
                Main.EntitySpriteDraw(skelly, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, skelly.Size() / 2f, 1f, flip, 0);
            }

            Texture2D eyes = TextureAssets.Projectile[Type].Value;
            float animation = Utils.GetLerpValue(EyeBlinkStart, EyeBlinkEnd, Projectile.timeLeft, true); //Blink anim
            Rectangle eyeFrame = eyes.Frame(1, 10, 0, Math.Min(9, (int)(animation * 10)), 0, -2);
            Vector2 eyesPosition = Projectile.Center - new Vector2(-4f * Projectile.spriteDirection, 12f).RotatedBy(Projectile.rotation);
            float eyeRotation = Projectile.rotation;

            float fallProgress = Utils.GetLerpValue(EyeFallEnd, EyeFallStart, Projectile.timeLeft, true);

            float opacity = FablesUtils.PolyInOutEasing(fallProgress, 0.5f);
            eyesPosition.Y += FablesUtils.PolyInEasing(1 - fallProgress, 2.2f) * 50;
            eyeRotation -= Projectile.spriteDirection * MathF.Pow(1 - fallProgress, 0.7f) * 0.3f;


            Main.EntitySpriteDraw(eyes, eyesPosition - Main.screenPosition, eyeFrame, lightColor * opacity, eyeRotation, eyeFrame.Size() / 2f, 1f, flip, 0);


            return false;
        }
    }
}
