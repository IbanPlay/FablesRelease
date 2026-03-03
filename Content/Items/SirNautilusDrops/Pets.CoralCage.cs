using CalamityFables.Particles;
using Terraria.WorldBuilding;
using CalamityFables.Content.Boss.DesertWormBoss;

namespace CalamityFables.Content.Items.SirNautilusDrops
{
    public class CoralCage : ModItem
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Coral Cage");
            Tooltip.SetDefault("Summons a little dragonfish\n" +
                "'A Coral Corral, if you will'");
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ZephyrFish);

            Item.shoot = ModContent.ProjectileType<LittleDragonfish>();
            Item.buffType = ModContent.BuffType<LittleDragonfishBuff>();

            Item.rare = ItemRarityID.Master;
            Item.master = true;

            // All master mode pets sell for 5 gold.
            // TODO -- Should this be made into some static/const value?
            Item.value = Item.sellPrice(0, 5, 0, 0);
        }

        //I guess setting bufftype doesnt work automatically for pet items..?
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
                player.AddBuff(Item.buffType, 3600);
            return true;
        }
    }

    public class LittleDragonfishBuff : ModBuff
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
            DisplayName.SetDefault("Little Dragonfish");
            Description.SetDefault("Not associated with Great Seadragons");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            bool _ = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref _, ModContent.ProjectileType<LittleDragonfish>());
        }
    }

    public class LittleDragonfish : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        public List<Vector2> positionCache;
        public List<float> rotationCache;


        public bool Flying {
            get => Projectile.ai[0] == 1;
            set {
                Projectile.ai[0] = value.ToInt();
                FlightTimer = 0;
            }
        }

        public ref float FlightTimer => ref Projectile.ai[1];


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Little Dragonfish");
            Main.projFrames[Type] = 6;
            Main.projPet[Type] = true;

            ProjectileID.Sets.CharacterPreviewAnimations[Projectile.type] =
                ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Projectile.type], 5);
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.EyeOfCthulhuPet);
            Projectile.aiStyle = -1;
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.ignoreWater = true;
        }

        public ref float PirouetteTimer => ref Projectile.localAI[0];
        public bool Pirouetting {
            get => Projectile.localAI[1] == 1;
            set => Projectile.localAI[1] = value.ToInt();
        }
        public ref float PirouetteCooldown => ref Projectile.localAI[2];

        public float afterimageOpacity = 0f;

        public override void AI()
        {
            if (!Owner.dead && Owner.HasBuff<LittleDragonfishBuff>())
                Projectile.timeLeft = 2;

            float distanceToPlayer = Projectile.Distance(Owner.Center);
            if (distanceToPlayer > 2000)
                Projectile.Center = Owner.Center;

            float horizontalDistanceToPlayer = Math.Abs(Owner.Center.X - Projectile.Center.X);
            float verticalDistanceToPlayer = Math.Abs(Owner.Center.Y - Projectile.Center.Y);

            if (!Flying)
            {
                Projectile.tileCollide = true;

                Projectile.frameCounter++;
                if (Projectile.frameCounter > 5)
                {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                }
                //Loop
                if (Projectile.frame > 5)
                    Projectile.frame = 0;

                //Bounce
                bool canWaterBounce = Projectile.velocity.Y > 0 && Owner.Bottom.Y - 6 < Projectile.Bottom.Y && Collision.WetCollision(Projectile.position, Projectile.width, Projectile.height + 16);

                if (Projectile.velocity.Y == 0 || canWaterBounce)
                {
                    PirouetteTimer = 0;
                    Pirouetting = false;
                    PirouetteCooldown++;

                    float bounceStrenght = 4;
                    bounceStrenght += Utils.GetLerpValue(140, 300, distanceToPlayer, true) * 4f;

                    //Bounces harder when close
                    if (horizontalDistanceToPlayer < 100)
                    {
                        bounceStrenght += 4;
                        Projectile.velocity.X *= 1.4f;

                        //Does a pirouette every 7 boucnes
                        if (PirouetteCooldown > 7)
                        {
                            bounceStrenght += 1.5f;
                            Projectile.velocity.X *= 1.2f;
                            Pirouetting = true;
                            PirouetteCooldown = 0;
                        }
                    }

                    if (bounceStrenght < 11)
                    {
                        //Jump over obstacles
                        Point tilePosition = Projectile.Center.ToTileCoordinates();
                        tilePosition.X += Projectile.spriteDirection * 2;
                        tilePosition.X += (int)Projectile.velocity.X;

                        tilePosition.Y -= (int)(Utils.GetLerpValue(4, 7, bounceStrenght, true) * 2.9f);

                        for (int i = 0; i < 4; i++)
                        {
                            if (!WorldGen.InWorld(tilePosition.X, tilePosition.Y) ||bounceStrenght > 11)
                                break;

                            Tile t = Main.tile[tilePosition];
                            if (WorldGen.SolidTile(t))
                                bounceStrenght += 2.5f;

                            tilePosition.Y--;
                        }
                    }

                    Projectile.velocity.Y = -bounceStrenght;
                    DoBounceSplatVFX();
                }

                if (Projectile.velocity.Y < 10)
                    Projectile.velocity.Y += 0.52f;

                //Catch up to the player whle flying if too far
                if (distanceToPlayer > 500 || verticalDistanceToPlayer > 300 || Owner.rocketDelay2 > 0)
                    Flying = true;

                //Go towards the player
                else if (horizontalDistanceToPlayer > 100)
                {
                    int direction = (Owner.Center.X - Projectile.Center.X).NonZeroSign();
                    float acceleration = 0.04f;

                    if (Projectile.velocity.X.NonZeroSign() != direction)
                        acceleration *= 2f;

                    Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, 6 * direction, acceleration);
                }

                //Keep on bouncing
                else
                {
                    if (Projectile.velocity.X == 0)
                        Projectile.velocity.X = -Projectile.spriteDirection * 0.3f;
                }

                Projectile.rotation = Math.Clamp(Projectile.velocity.X * 0.04f * Utils.GetLerpValue(0, 5f, Projectile.velocity.Y), -0.3f, 0.3f);
                Projectile.spriteDirection = Projectile.velocity.X.NonZeroSign();

                if (Pirouetting)
                {
                    PirouetteTimer += 1 / (60f * 0.3f);

                    if (!Main.rand.NextBool(3))
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f);
                        Dust zust = Dust.NewDustPerfect(Projectile.Center + offset * Main.rand.NextFloat(9f, 26f), 43, offset.RotatedBy(MathHelper.PiOver2 * Projectile.spriteDirection) * Main.rand.NextFloat(1f, 2f), 100, Color.DeepSkyBlue, Main.rand.NextFloat(0.7f, 1f));
                        zust.noGravity = true;
                    }
                }
            }

            else
            {
                Projectile.tileCollide = false;
                PirouetteTimer = 0;
                PirouetteCooldown = 0;
                Projectile.spriteDirection = Projectile.velocity.X.NonZeroSign();

                Vector2 idealPosition = Owner.MountedCenter - Vector2.UnitY * 30f + Vector2.UnitX * Owner.direction * 30;
                Vector2 goalVelocity = (idealPosition - Projectile.Center) * 0.03f;

                FlightTimer += 0.2f + 0.8f * Utils.GetLerpValue(290f, 0f, distanceToPlayer, true);

                float wobbleStrenght = 0.1f + 0.4f * Utils.GetLerpValue(0, 300, distanceToPlayer, true);
                goalVelocity = goalVelocity.RotatedBy(MathF.Sin(FlightTimer * 0.16f) * wobbleStrenght);

                float approachAcceleration = 0.1f + MathF.Pow(Utils.GetLerpValue(70, 0, distanceToPlayer, true), 2f) * 0.3f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity, approachAcceleration);
                Projectile.velocity *= 0.98f;

                Projectile.rotation = Projectile.velocity.ToRotation();



                //Stick when close enough
                if (Projectile.WithinRange(idealPosition, 1f))
                {
                    Projectile.Center = idealPosition;
                    Projectile.spriteDirection = Owner.direction;

                    Projectile.rotation = MathHelper.PiOver2 - MathHelper.PiOver2 * Owner.direction + 0.1f * MathF.Sin(FlightTimer * 0.1f);
                }

                else if (Projectile.velocity.Length() > 2)
                {

                    if (Main.rand.NextBool(6))
                    {
                        Dust zust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), 43, Projectile.velocity * 0.25f, 100, Color.DeepSkyBlue, Main.rand.NextFloat(1.1f, 1.6f));
                        zust.noGravity = true;
                    }


                    if (Main.rand.NextBool(18))
                    {
                        Dust zust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25f, 25f), 43, -Projectile.velocity * 0.25f, 200, Color.Turquoise, Main.rand.NextFloat(0.3f, 0.5f));
                        zust.noGravity = true;
                    }
                }


                //Stop flying
                if (distanceToPlayer < (float)200 && Owner.velocity.Y == 0f && Projectile.Bottom.Y <= Owner.Bottom.Y && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    Flying = false;
                }

                //Animation
                Projectile.frameCounter++;
                if (Projectile.frameCounter > 4)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                }
                if (Projectile.frame < 6 || Projectile.frame > 16)
                    Projectile.frame = 6;
            }

            if (!Main.dedServ)
            {
                ManageCaches();
                ManageTrail();

                if (Flying || Pirouetting)
                {
                    float idealOpacity = 0.2f + 0.8f * Utils.GetLerpValue(2f, 6f, Projectile.velocity.Length(), true);
                    afterimageOpacity = MathHelper.Lerp(afterimageOpacity, idealOpacity, 0.04f);
                }
                else
                {
                    afterimageOpacity = MathHelper.Lerp(afterimageOpacity, 0f, 0.1f);
                    if (afterimageOpacity < 0.01f)
                        afterimageOpacity = 0;
                }    
            }
        }

        public void DoBounceSplatVFX()
        {
            ParticleHandler.SpawnParticle(new SplatRippleParticle(Projectile.Bottom, Color.MediumSpringGreen * 0.5f, Color.Turquoise * 0.5f) { Scale = 0.7f });

            int spectralDust = ModContent.DustType<SpectralWaterDustNoisy>();
            for (int i = 0; i < 20; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                direction.Y *= 0.2f;

                Dust d = Dust.NewDustPerfect(Projectile.Bottom + direction * 35f * 0.7f, spectralDust, direction * 2.2f * 0.7f, 100, Color.Blue, Main.rand.NextFloat(1f, 1.4f) * 0.7f);

                d.velocity.Y -= 0.4f;
                d.rotation = Main.rand.NextFloat(0f, 3f);
                d.customData = Color.Teal.ToVector3();
            }

            spectralDust = ModContent.DustType<SpectralWaterDustEmbers>();
            for (int i = 0; i < 10; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                direction.Y *= 0.2f;

                Dust d = Dust.NewDustPerfect(Projectile.Bottom + direction * 35f * 0.7f, spectralDust, direction * 1.2f * 0.7f - Vector2.UnitY * 0.3f, 15, Color.White, Main.rand.NextFloat(0.7f, 1.4f));
                d.noGravity = true;
            }
        }

        public PrimitiveTrail TrailDrawer;

        private void ManageCaches()
        {
            if (positionCache == null)
            {
                positionCache = new List<Vector2>();

                for (int i = 0; i < 30; i++)
                    positionCache.Add(Projectile.Center + Projectile.velocity);
            }
            positionCache.Add(Projectile.Center + Projectile.velocity);
            while (positionCache.Count > 30)
                positionCache.RemoveAt(0);
        }

        private void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, factor => factor * 13, factor => Color.DodgerBlue * 0.5f * afterimageOpacity * (float)Math.Pow(factor, 2f));
            TrailDrawer.SetPositions(positionCache);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D projectileTex = TextureAssets.Projectile[Type].Value;
            int frameCount = Projectile.frame;
            if (frameCount > 5)
                frameCount += 5;
            Rectangle frame = projectileTex.Frame(2, 11, frameCount / 11, frameCount.ModulusPositive(11), -2, -2);
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (Flying && effects == SpriteEffects.None)
                effects = SpriteEffects.FlipVertically | SpriteEffects.FlipHorizontally;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float extraRotation = PirouetteTimer * MathHelper.TwoPi * Projectile.spriteDirection;
            float drawRotation = Projectile.rotation + extraRotation;

            Vector2 squish = Vector2.One * Projectile.scale;
            if (!Flying)
            {
                drawPosition.Y += 10;

                squish.Y += Utils.GetLerpValue(0, 6, Projectile.velocity.Y) * 0.2f;
                squish.X = 2 - squish.Y;
            }

            if (afterimageOpacity > 0)
            {
                Effect effect = AssetDirectory.PrimShaders.StreakyTrail;
                effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
                effect.Parameters["verticalStretch"].SetValue(0.5f);
                effect.Parameters["repeats"].SetValue(4f);

                effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
                effect.Parameters["overlayOpacity"].SetValue(0.5f);

                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
                effect.Parameters["streakNoiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise").Value);
                effect.Parameters["streakScale"].SetValue(1f);
                TrailDrawer?.Render(effect, -Main.screenPosition);
            }

            // Draw the head.
            Main.EntitySpriteDraw(projectileTex, drawPosition, frame, lightColor, drawRotation, frame.Size() * 0.5f, squish, effects, 0);

            return false;
        }
    }
}
