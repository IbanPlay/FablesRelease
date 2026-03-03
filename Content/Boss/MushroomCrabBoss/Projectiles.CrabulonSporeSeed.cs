using CalamityFables.Particles;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public class CrabulonSporeSeed : ModProjectile
    {
        public override string Texture => AssetDirectory.Crabulon + Name;
        public Asset<Texture2D> StrandTexture;
        public Asset<Texture2D> KnotTexture;
        public VerletNet strands;

        public float targetX => Projectile.ai[0];
        public Player target => Main.player[(int)Projectile.ai[1]];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spore Seed");
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 600;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            DoDust();

            if (!Main.dedServ)
            {
                if (strands == null)
                    InitializeStrands();
                UpdateStrandAttachPoints();
                strands.Update(7, -Projectile.velocity.SafeNormalize(Vector2.UnitY) * 1.2f);
            }

            Lighting.AddLight(Projectile.Center, CommonColors.MushroomDeepBlue.ToVector3());

            //Projectile.tileCollide = (Projectile.Center.X - targetX) * Projectile.velocity.X.NonZeroSign() > -30;
            Projectile.tileCollide = Projectile.velocity.Y > 0 || (Projectile.Center.X - target.Center.X) * Projectile.velocity.X.NonZeroSign() > 0;

            Projectile.rotation += Projectile.velocity.X * 0.01f;
            Projectile.velocity.Y += 0.4f;
            if (Projectile.velocity.Y > 26f)
                Projectile.velocity.Y = 26;
        }

        #region Visuals
        public void InitializeStrands()
        {
            strands = new VerletNet();

            VerletPoint[] mainAttachPoints = new VerletPoint[3];
            for (int i = 0; i < 3; i++)
            {
                //Do three attach points
                mainAttachPoints[i] = new VerletPoint(Projectile.Center + (i / 3f * MathHelper.TwoPi).ToRotationVector2() * 6f, true);
            }

            VerletPoint[] loops = new VerletPoint[10];
            for (int i = 0; i < 3; i++)
            {
                float loopRotation = (i / 3f * MathHelper.TwoPi + MathHelper.TwoPi / 6f);
                Vector2 loopCenter = Projectile.Center + loopRotation.ToRotationVector2() * 14f;
                loops[0] = mainAttachPoints[i];
                loops[9] = mainAttachPoints[(i + 1) % 3];

                for (int j = 1; j < 9; j++)
                {
                    loops[j] = new VerletPoint(loopCenter + ((j / 10f) * MathHelper.TwoPi * 0.8f - MathHelper.TwoPi * 0.4f + loopRotation).ToRotationVector2() * 18f);
                }

                strands.AddChain(WidthFunction, ColorFunction, loops);
            }

            Vector2 away = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 2; i++)
            {
                //long strands
                float distance = i == 0 ? 60f : 24;
                strands.AddChain(mainAttachPoints[i], new VerletPoint(mainAttachPoints[i].position + away * distance), 10, WidthFunction, ColorFunction);
            }
        }

        public void UpdateStrandAttachPoints()
        {
            for (int i = 0; i < 3; i++)
            {
                //Do three attach points
                strands.extremities[i].position = Projectile.Center + (i / 3f * MathHelper.TwoPi + Projectile.rotation).ToRotationVector2() * 6f;
            }
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    float direction = (i / 3f + 1 / 6f) * MathHelper.TwoPi + Projectile.rotation;
                    direction += (j / 10f) * MathHelper.TwoPi * 0.8f - MathHelper.TwoPi * 0.4f;

                    strands.chains[i][j].customGravity = Vector2.Lerp(-Projectile.velocity.SafeNormalize(Vector2.UnitY) * 2f, direction.ToRotationVector2() * 2f, 0.6f);
                }
            }
        }

        public virtual void DoDust()
        {
            if (Main.rand.NextBool(2))
            {
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 dustPosition = Projectile.Center + gushDirection * Main.rand.NextFloat(0f, 4.6f);
                Vector2 velocity = gushDirection.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3f, 6f);
                Dust d = Dust.NewDustPerfect(dustPosition, DustID.GlowingMushroom, velocity);
                d.noLightEmittence = true;
            }

            if (Main.rand.NextBool(2))
            {
                float smokeSize = Main.rand.NextFloat(1.5f, 2f);
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 smokeCenter = Projectile.Center + gushDirection * Main.rand.NextFloat(0.2f, 5f);
                Vector2 velocity = gushDirection * Main.rand.NextFloat(0.1f, 0.3f);

                Vector2 origin = Projectile.Center;

                Particle smoke = new SporeGas(smokeCenter, velocity, origin, 1000f, smokeSize, 0.01f);
                ParticleHandler.SpawnParticle(smoke);
            }
        }
        #endregion

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = Projectile.Center.Y < target.Top.Y;
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            #region Doing stuff because terraria's collision usually only applies to one axis at a time
            Vector2 difference = oldVelocity - Projectile.velocity; //Get the velocity difference after tile collision has been applied

            bool collideX = false;
            bool collideY = false;
            Vector2 newFallVelocity = oldVelocity - difference; //Apply the difference

            //Check for impacts, and zero out the velocity for the direction that already had an imapct, since we already know it's collided 
            if (difference.X != 0)
            {
                newFallVelocity.X = 0;
                newFallVelocity.Y += 16f * newFallVelocity.Y.NonZeroSign();
                collideX = true;
            }
            if (difference.Y != 0)
            {
                newFallVelocity.Y = 0;
                newFallVelocity.X += 16f * newFallVelocity.X.NonZeroSign();
                collideY = true;
            }

            if (!collideX || !collideY)
            {
                if (Collision.SolidCollision(Projectile.position + newFallVelocity, Projectile.width, Projectile.height, false))
                {
                    collideX = true;
                    collideY = true;
                }
            }
            #endregion


            Vector2 normalizedDirection = oldVelocity.SafeNormalize(Vector2.UnitY);

            //If we hit sloped terrain, the bulb can be axed any way we want
            if (collideX && collideY)
                Projectile.velocity = normalizedDirection;
            //Hit a wall
            else if (collideX)
                Projectile.velocity = Vector2.Lerp(normalizedDirection, Vector2.UnitX * (oldVelocity.X - Projectile.velocity.X).NonZeroSign(), 0.95f);
            //Hit the floor / ceiling
            else
                Projectile.velocity = Vector2.Lerp(normalizedDirection, Vector2.UnitY * (oldVelocity.Y - Projectile.velocity.Y).NonZeroSign(), 0.95f);

            //Slightly make the projectile go into the ground
            Projectile.position += normalizedDirection * 4f;
            if (oldVelocity.Length() > 9f)
            {
                for (int i = 0; i < oldVelocity.Length() / 9; i++)
                {
                    if (FablesUtils.SolidCollisionFix(Projectile.position, Projectile.width, Projectile.height, true))
                        break;
                    Projectile.position += normalizedDirection * 9f;
                }
            }

            //Velocity is automatically added to the position, so we undo that, since we use the velocity to get the direction of the bulb
            Projectile.position -= Projectile.velocity;
            Projectile.netUpdate = true;
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 dustPosition = Projectile.Center + gushDirection * Main.rand.NextFloat(0f, 4.6f);
                Vector2 velocity = gushDirection.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(4f, 6f);
                Dust d = Dust.NewDustPerfect(dustPosition, DustID.GlowingMushroom, velocity);
                d.noLightEmittence = true;
            }

            for (int i = 0; i < 5; i++)
            {
                float smokeSize = Main.rand.NextFloat(2.5f, 3f);
                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 smokeCenter = Projectile.Center + gushDirection * Main.rand.NextFloat(0.2f, 5f);
                Vector2 velocity = gushDirection * Main.rand.NextFloat(0.6f, 1f);

                Vector2 origin = Projectile.Center;

                Particle smoke = new SporeGas(smokeCenter, velocity, origin, 1000f, smokeSize, 0.01f);
                ParticleHandler.SpawnParticle(smoke);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 position = Projectile.Center + Vector2.UnitY * 20f;
                float rotation = (-Projectile.velocity).ToRotation();

                NPC bomb = NPC.NewNPCDirect(Projectile.GetSource_Death(), (int)position.X, (int)position.Y, ModContent.NPCType<CrabulonThrownSporeBomb>(), 0, 0, rotation + MathHelper.TwoPi);
                bomb.lifeMax = Crabulon.SporeBomb_BudLifeMax;
            }

            //Showlrch
            SoundEngine.PlaySound(Crabulon.SporeMortarLandSound, Projectile.Center);
        }

        public virtual float WidthFunction(float completion) => 6f;

        public static Color PrimsLightColor;
        public virtual Color ColorFunction(float completion) => PrimsLightColor;


        public override bool PreDraw(ref Color lightColor)
        {
            if (StrandTexture == null)
                StrandTexture = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MyceliumStrand");
            PrimsLightColor = lightColor;
            strands?.Render(StrandTexture.Value, -Main.screenPosition);

            if (KnotTexture == null)
                KnotTexture = ModContent.Request<Texture2D>(Texture + "Bud");
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = strands.extremities[^(i + 1)].position;
                Main.EntitySpriteDraw(KnotTexture.Value, position - Main.screenPosition, null, lightColor, Projectile.rotation, KnotTexture.Value.Size() / 2f, Projectile.scale, 0, 0);
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() / 2f, Projectile.scale, 0, 0);
            return false;
        }

    }
}
