using CalamityFables.Particles;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public partial class SirNautilus : ModNPC
    {
        public int CollisionBoxWidth => 40;
        public int CollisionBoxHeight => 20;
        public Vector2 CollisionBoxOrigin => NPC.Bottom - Vector2.UnitX * (float)(CollisionBoxWidth / 2) - Vector2.UnitY * CollisionBoxHeight;

        #region Movement
        public void StepUp() //Does Collision.StepUp but with the custom collision box
        {
            Vector2 displacedCollisionBoxOrigin = CollisionBoxOrigin;
            Collision.StepUp(ref displacedCollisionBoxOrigin, ref NPC.velocity, CollisionBoxWidth, CollisionBoxHeight, ref NPC.stepSpeed, ref NPC.gfxOffY);
            NPC.position += displacedCollisionBoxOrigin - CollisionBoxOrigin;

        }

        public void GoUpOneBlock()
        {
            bool canFallThroughPlatforms = CanFallThroughPlatforms().Value;
            for (int i = 0; i < 8; i++)
            {
                if (Collision.SolidCollision(CollisionBoxOrigin, CollisionBoxWidth, CollisionBoxHeight, canFallThroughPlatforms) && !Collision.SolidCollision(CollisionBoxOrigin + Vector2.UnitY * (CollisionBoxHeight - 8), CollisionBoxWidth, 1, canFallThroughPlatforms))
                {
                    NPC.position.Y -= 1f;
                }
            }
        }

        public void SignathionMovement()
        {
            NPC.TargetClosest(true);
            NPC.rotation = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;

            float attackRechargeMultiplier = 1f;
            float riseSpeed = -0.4f;
            float min = -8f;
            float gravity = 0.4f;
            Rectangle targetHitbox = NPC.GetTargetData().Hitbox;

            // Look at player
            float playerLocation = NPC.Center.X - Target.Center.X;
            NPC.direction = playerLocation < 0 ? 1 : -1;
            float distanceToPlayer = Math.Abs(NPC.Center.X - Target.Center.X);

            bool verticallyAlignedWithTarget = CollisionBoxOrigin.X < (float)targetHitbox.X && CollisionBoxOrigin.X + (float)NPC.width > (float)(targetHitbox.X + targetHitbox.Width);
            bool abovePlayer = CollisionBoxOrigin.Y + (float)CollisionBoxHeight < (float)(targetHitbox.Y + targetHitbox.Height - 16);
            bool acceptTopSurfaces = NPC.Bottom.Y >= (float)targetHitbox.Top; //Accept platforms if you aren't above the player's 
            bool insideSolids = Collision.SolidCollision(CollisionBoxOrigin, CollisionBoxWidth, CollisionBoxHeight, acceptTopSurfaces);
            bool upperBodyInSolids = Collision.SolidCollision(CollisionBoxOrigin, CollisionBoxWidth, CollisionBoxHeight - 4, acceptTopSurfaces);
            bool noForwardCollision = !Collision.SolidCollision(CollisionBoxOrigin + new Vector2(CollisionBoxWidth * NPC.direction, 0f), 16, 80, acceptTopSurfaces);
            float jumpVelocity = 8f;


            //Initialization (Unlike Nautilus, he starts off unmoving unless the player is really far )
            if (SubState == ActionState.SlowWalk)
            {
                float cooldownRechargeSpeed = 1 / (60f * Main.rand.NextFloat(1.2f, 1.65f) - 0.2f * DifficultyScale);
                ExtraMemory = cooldownRechargeSpeed;

                SubState = ActionState.SlowWalkStayPut;
                if (distanceToPlayer > 500f)
                    SubState = ActionState.SlowWalkForward;
            }

            //If the player is too close to signathion, run backwards
            if (distanceToPlayer < 60f)
                SubState = ActionState.FastWalkAway;

            //If you don't have line of sight to player, go towards them, while getting very annoyed
            if (!Collision.CanHitLine(NPC.Bottom - Vector2.UnitY * CollisionBoxHeight, 1, 1, Target.Top, 1, 1))
            {
                SubState = ActionState.SlowWalkForward;
                if (UnboundedPatience_PhaseTimer <= 1)
                    Patience += 0.15f;
            }

            if (SubState == ActionState.SlowWalkStayPut)
            {
                //Slow down
                NPC.velocity.X *= 0.9f;
                //Come to a half
                if (Math.Abs(NPC.velocity.X) < 0.1)
                    NPC.velocity.X = 0f;

                //After waiting a while, decide to move
                if (AttackTimer < 0.8f && Main.rand.NextBool(20))
                {
                    float chanceToBackpedal = Utils.GetLerpValue(400f, 130f, distanceToPlayer, true);

                    SubState = ActionState.SlowWalkForward;
                    if (Main.rand.NextFloat() < chanceToBackpedal)
                        SubState = ActionState.SlowWalkAway;
                }
            }

            else if (SubState == ActionState.SlowWalkAway || SubState == ActionState.SlowWalkForward || SubState == ActionState.FastWalkAway)
            {
                float walkSpeed = 2.5f + 1.5f * Math.Clamp(Patience, 0f, 1f);

                if (SubState == ActionState.SlowWalkAway)
                    walkSpeed *= -1f;

                if (SubState == ActionState.FastWalkAway)
                {
                    walkSpeed *= -2.5f;
                    attackRechargeMultiplier *= 1.6f; //Cooldown goes down faster while moving fast
                }

                NPC.velocity.X = (NPC.velocity.X * 20f + walkSpeed * NPC.direction) / 21f;


                //Don't back up into a wall
                if (SubState == ActionState.SlowWalkAway || SubState == ActionState.FastWalkAway)
                {
                    bool intoWall = Collision.SolidCollision(CollisionBoxOrigin + Vector2.UnitX * NPC.velocity.X, 40, CollisionBoxHeight - 4, false);
                    if (intoWall)
                    {
                        StepUp();
                        GoUpOneBlock();

                        bool wouldBeInsideSolids = Collision.SolidCollision(CollisionBoxOrigin + NPC.velocity.X * Vector2.UnitX, CollisionBoxWidth, CollisionBoxHeight, acceptTopSurfaces);
                        bool wouldUpperBodyBeInsideSolids = Collision.SolidCollision(CollisionBoxOrigin + NPC.velocity.X * Vector2.UnitX, CollisionBoxWidth, CollisionBoxHeight - 17, acceptTopSurfaces);

                        //Climb up tiles if theres a clear space above
                        if (wouldBeInsideSolids && !wouldUpperBodyBeInsideSolids)
                            NPC.velocity.Y = -1;

                        //Bonk nautilus into walls
                        else
                            NPC.velocity = Collision.TileCollision(CollisionBoxOrigin, NPC.velocity, CollisionBoxWidth, CollisionBoxHeight, CanFallThroughPlatforms().Value, CanFallThroughPlatforms().Value);

                    }
                }
            }
            if (SubState == ActionState.FastWalkAway && distanceToPlayer > 100f)
            {
                SubState = ActionState.SlowWalkAway;
            }

            AttackTimer -= ExtraMemory * attackRechargeMultiplier;

            if ((verticallyAlignedWithTarget || distanceToPlayer < 80f) && abovePlayer)
            {
                //Fall down fast if above player
                NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y + gravity * 2f, 0.001f, 16f);
            }
            else if (insideSolids && !upperBodyInSolids) // Don't fall into the ground if upper body is fine but not the below
            {
                NPC.velocity.Y = 0f;
            }
            else if (insideSolids) //Rise up if the whole body is in solid and below the player
            {
                if (NPC.Bottom.Y > Target.Bottom.Y - 3)
                {
                    NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y + riseSpeed, min, 0f);
                    AttackTimer += ExtraMemory * attackRechargeMultiplier * 0.65f; //Attack less often if inside solids
                }
                else
                {
                    NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, 0f, 0.05f);
                }
            }
            else if (NPC.velocity.Y == 0f && noForwardCollision)
            {
                NPC.velocity.Y = -jumpVelocity;
            }
            else
            { //Fall down
                NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y + gravity, -jumpVelocity, 16f);
            }
        }

        public void SignathionCollision()
        {
            int collisionWidth = 40;
            int collisionHeight = 20;
            Vector2 collisionPosition = NPC.Bottom - Vector2.UnitY * collisionHeight - Vector2.UnitX * collisionWidth * 0.5f;

            bool canFallThroughPlatforms = CanFallThroughPlatforms().Value;

            //Equivalent to the NPC.WalkDownSlope method.
            Vector4 slopeWalkVector = Collision.WalkDownSlope(collisionPosition, NPC.velocity, collisionWidth, collisionHeight);
            collisionPosition.X = slopeWalkVector.X;
            collisionPosition.Y = slopeWalkVector.Y;
            NPC.velocity.X = slopeWalkVector.Z;
            NPC.velocity.Y = slopeWalkVector.W;

            //If inside a block, rise up to 1 block up
            for (int i = 0; i < 8; i++)
            {
                if (Collision.SolidCollision(collisionPosition, collisionWidth, collisionHeight, canFallThroughPlatforms) && !Collision.SolidCollision(collisionPosition + Vector2.UnitY * (collisionHeight - 8), collisionWidth, 1, canFallThroughPlatforms))
                {
                    NPC.position.Y -= 1f;
                    collisionPosition = NPC.Bottom - Vector2.UnitY * collisionHeight - Vector2.UnitX * collisionWidth * 0.5f;
                }
            }

            NPC.oldVelocity = NPC.velocity;
            NPC.collideX = false;
            NPC.collideY = false;

            //Vanilla uses NPC.GetTileCOllisionParameters. Great method. Lets you modify the size of the collision hitbox. id have detoured it but i need to change more stuff
            //GetTileCollisionParameters(out Vector2 cPosition, out int cWidth, out int cHeight);
            NPC.velocity = Collision.TileCollision(collisionPosition, NPC.velocity, collisionWidth, collisionHeight, canFallThroughPlatforms, canFallThroughPlatforms);
            //That's NPC.Movewhiledry
            if (Collision.up)
                NPC.velocity.Y = 0.01f;
            if (NPC.oldVelocity.X != NPC.velocity.X)
                NPC.collideX = true;
            if (NPC.oldVelocity.Y != NPC.velocity.Y)
                NPC.collideY = true;

            NPC.oldPosition = NPC.position;
            NPC.oldDirection = NPC.direction;
            NPC.position += NPC.velocity;
        }
        #endregion

        #region Tail swipe
        public void SignathionTailSwipe()
        {
            //Initialization 
            if (SubState == ActionState.TailSwipe)
            {
                NPC.TargetClosest(true);

                if (!NPC.HasValidTarget)
                {
                    AIState = ActionState.SlowWalk;
                    return;
                }

                NPC.noTileCollide = false;
                NPC.noGravity = false;

                Stamina -= 0.5f;
                SubState = ActionState.TailSwipe_Telegraph;
                NPC.velocity.Y = 0;
                NPC.velocity *= 0.6f;
                movementTarget = NPC.Center + Vector2.UnitX * 10f * (Target.Center.X - NPC.Center.X).NonZeroSign();

                SoundEngine.PlaySound(SignathionScream, NPC.Center);
            }

            if (SubState == ActionState.TailSwipe_Telegraph)
            {
                NPC.velocity *= 0.95f;
                AttackTimer -= 1 / (60f * 0.8f);

                if (AttackTimer <= 0)
                {
                    AttackTimer = 1;
                    SubState = ActionState.TailSwipe_Swipe;
                    NPC.position = NPC.Center;
                    NPC.Size = new Vector2(NPC.Size.X * 1.9f, NPC.height);
                    NPC.Center = NPC.position;

                    NPC.velocity = NPC.DirectionTo(movementTarget) * (12f + Utils.GetLerpValue(150f, 300f, NPC.Distance(Target.Center), true) * 10f);

                    SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, NPC.Center);
                }
            }

            if (SubState == ActionState.TailSwipe_Swipe)
            {
                Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
                NPC.velocity *= 0.3f;
                AttackTimer -= 1 / (60f * 0.33f);

                if (AttackTimer <= 0)
                {
                    NPC.position = NPC.Center;
                    NPC.Size = P1Size;
                    NPC.Center = NPC.position;

                    AttackTimer = 1;
                    SubState = ActionState.TailSwipe_Recovery;
                }
            }

            if (SubState == ActionState.TailSwipe_Recovery)
            {
                NPC.velocity.X = 0;
                AttackTimer -= 1 / (60f * 0.5f);
            }
        }
        #endregion

        #region Specter bolts
        public void SignathionSpecterBolts()
        {

            if (SubState == ActionState.SpecterBolts)
            {
                NPC.TargetClosest(true);
                NPC.noTileCollide = false;
                NPC.noGravity = false;
                NPC.velocity.Y = 0;
                Stamina -= 0.34f;

                ExtraMemory = Main.rand.NextBool() ? 1 : -1; //-1 = regular directed blasts //1 = singular shotgun like blast

                if (ExtraMemory == PreviousAttackVariant && Main.rand.NextBool()) //50% to pick a different choice if the previous variant was this one (Because main.rand seems awfully unrandom at times
                    ExtraMemory *= -1;

                if (ExtraMemory == 1 && Math.Abs(NPC.Center.X - Target.Center.X) < 130 && !Main.rand.NextBool(3)) //66% chance to pick the straight bolts (aka the one you have to run away from) if too close
                    ExtraMemory *= -1;

                if (ExtraMemory == 1 && Target.Bottom.Y < NPC.Top.Y - 70 && !Main.rand.NextBool(5)) //80% chance to pick the straight bolts (aka the one you have to run away from) if the player is too high
                    ExtraMemory *= -1;

                SoundEngine.PlaySound(ExtraMemory == -1 ? SignathionWaterRifleCharge : SignathionWaterShotgunCharge);
                PreviousAttackVariant = ExtraMemory;
                SubState = ActionState.SpecterBolts_Chargeup;
            }

            Vector2 mouthPosition = NPC.Center - Vector2.UnitY * 50 + Vector2.UnitX * 100f * NPC.direction;

            if (SubState == ActionState.SpecterBolts_Chargeup)
            {
                float attackSpeed = ExtraMemory == -1 ? 0.75f : 0.95f;
                AttackTimer -= 1 / (60f * attackSpeed);
                NPC.velocity.X *= 0.6f;

                if (ExtraMemory < 0) //Rifle variant
                {
                    if (AttackTimer == 1 - 1 / (60f * attackSpeed))
                    {
                        Particle bloom = new StrongBloom(mouthPosition, Vector2.Zero, Color.DodgerBlue * 0.5f, 0.7f, (int)(0.8f * 60));
                        ParticleHandler.SpawnParticle(bloom);
                    }

                    if (Main.rand.NextBool(2))
                    {
                        int dustType = 187;
                        Vector2 dustDirection = Main.rand.NextVector2Circular(1f, 1f);

                        Dust charge = Dust.NewDustPerfect(mouthPosition + dustDirection * Main.rand.NextFloat(50f, 85f), dustType, -dustDirection * Main.rand.NextFloat(4.6f, 7f), 200, default, Main.rand.NextFloat(0.8f, 1f));
                        charge.noGravity = true;
                        charge.fadeIn = 1f;
                    }

                    if (Main.rand.NextBool(4))
                    {
                        Vector2 streakDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
                        Vector2 streakOriginDisplace = streakDirection * Main.rand.NextFloat(60f, 80f);

                        Particle streak = new SignathionRifleChargeStreak(mouthPosition + streakOriginDisplace, mouthPosition + streakOriginDisplace * 0.1f, 0.5f, Color.Blue, Color.DeepSkyBlue, 20);
                        ParticleHandler.SpawnParticle(streak);
                    }

                    if (Main.rand.NextBool(5))
                    {
                        Vector2 dustDirection = Main.rand.NextVector2Circular(1f, 1f);

                        Dust charge = Dust.NewDustPerfect(mouthPosition + dustDirection * Main.rand.NextFloat(70f, 85f), 43, -dustDirection * Main.rand.NextFloat(8f, 12f), 200, Color.Blue, Main.rand.NextFloat(1.2f, 1.8f));
                        charge.noGravity = true;
                    }
                }

                else //Shotgun variant
                {

                    if (AttackTimer == 1 - 1 / (60f * attackSpeed))
                    {
                        Particle bloom = new SignathionSpitTelegraphRing(mouthPosition, Vector2.Zero, Color.MediumTurquoise, 0.25f, 0.04f, (int)(0.9f * 60));
                        ParticleHandler.SpawnParticle(bloom);
                    }

                    if (Main.rand.NextBool(5))
                    {
                        int dustType = 220;
                        Vector2 dustDirection = Main.rand.NextVector2CircularEdge(1f, 1f);

                        Dust charge = Dust.NewDustPerfect(mouthPosition + dustDirection * (Main.rand.NextFloat(60f, 70f)) * (float)Math.Pow(AttackTimer, 0.2f), dustType, -dustDirection * Main.rand.NextFloat(1.6f, 3f) * AttackTimer, 200, default, Main.rand.NextFloat(0.5f, 0.9f));
                        charge.noGravity = true;
                        charge.fadeIn = 1f;
                    }

                    if (Main.rand.NextBool(3))
                    {
                        Vector2 dustDirection = Main.rand.NextVector2CircularEdge(1f, 1f);

                        Particle blobbu = new SignathionShotgunChargeDust(mouthPosition + dustDirection * (36 + Main.rand.NextFloat(-4f, 4f)) * (0.3f + 0.7f * (float)Math.Pow(AttackTimer, 0.4f)), dustDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(3.5f, 5.5f), mouthPosition, Main.rand.NextFloat(1f, 1.5f), Color.SpringGreen, Color.Turquoise, 20, 0, 0.2f, -0.01f);
                        ParticleHandler.SpawnParticle(blobbu);
                    }

                    if (Main.rand.NextBool(3) && AttackTimer > 0.15f)
                    {
                        Vector2 dustDirection = Main.rand.NextVector2CircularEdge(1f, 1f);

                        Vector2 dustOrigin = mouthPosition + dustDirection * (Main.rand.NextFloat(70f, 80f)) * (0.3f + 0.7f * (float)Math.Pow(AttackTimer, 0.2f));

                        Particle blobbu = new SignathionShotgunChargeDust(dustOrigin, dustDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(0.5f, 2f), mouthPosition + dustDirection * 1f, Main.rand.NextFloat(0.5f, 1f), Color.SpringGreen, Color.Turquoise, 30, 0, 0.1f, 0.1f);
                        ParticleHandler.SpawnParticle(blobbu);
                    }
                }

                if (AttackTimer <= 0)
                {
                    if (ExtraMemory < 0)
                    {
                        SubState = ActionState.SpecterBolts_FireBlasts;
                        ExtraMemory = Main.rand.Next(3, 7); //Amount of bolts in the barrage
                    }
                    else
                    {
                        SubState = ActionState.SpecterBolts_FireShotgun;
                        ExtraMemory = Main.rand.Next(4, 9); //Amount of bolts in the blast
                    }


                    AttackTimer = 1;
                }
            }

            else if (SubState == ActionState.SpecterBolts_FireBlasts)
            {
                mouthPosition += new Vector2(16 * NPC.direction, 6);

                float attackSpeed = 1 / (60f * 0.7f);
                AttackTimer -= attackSpeed;

                if (AttackTimer.Modulo(1 / ExtraMemory) < attackSpeed)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float puddlesUp = DifficultyScale > 0 ? 1 : 0;
                        Vector2 velocity = GetArcVel(mouthPosition, Target.Center, 0.22f, 0, 50, 20);
                        if (Math.Abs(Target.Center.X - mouthPosition.X) < 150)
                            velocity = Target.DirectionFrom(mouthPosition) * 10;

                        if (Target.Bottom.Y < mouthPosition.Y - 70)
                            velocity = Target.DirectionFrom(mouthPosition) * 14;


                        Projectile.NewProjectile(NPC.GetSource_FromAI(), mouthPosition, velocity, ModContent.ProjectileType<SignathionSpectralBolt>(), SpecterBolts_DirectDamage / 2, 1, Main.myPlayer, 0f, puddlesUp);
                    }

                    SoundEngine.PlaySound(SignathionWaterRifle, NPC.position);
                }

                if (AttackTimer <= 0)
                {
                    SubState = ActionState.SpecterBolts_Recovery;
                    AttackTimer = 1;
                }
            }

            else if (SubState == ActionState.SpecterBolts_FireShotgun)
            {
                mouthPosition += new Vector2(20 * NPC.direction, 6);

                if (AttackTimer == 1)
                {
                    for (int i = 0; i < ExtraMemory; i++)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float multiplier = 1 + Utils.GetLerpValue(200, 400, Math.Abs(Target.Center.X - mouthPosition.X), true);

                            Vector2 velocity = new Vector2(NPC.DirectionTo(Target.Center).X.NonZeroSign() * Main.rand.NextFloat(2f, 8f) * multiplier, -Main.rand.NextFloat(4f, 7f));
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), mouthPosition, velocity, ModContent.ProjectileType<SignathionSpectralBolt>(), SpecterBolts_DirectDamage / 2, 1, Main.myPlayer, 1f, 1.5f);
                        }

                        SoundEngine.PlaySound(SignathionWaterShotgun, NPC.position);
                    }
                }

                //Thats more like, recovery time lmao
                float attackSpeed = 1 / (60f * 0.3f);
                AttackTimer -= attackSpeed;

                if (AttackTimer <= 0)
                {
                    SubState = ActionState.SpecterBolts_Recovery;
                    AttackTimer = 1;
                }
            }

            else if (SubState == ActionState.SpecterBolts_Recovery)
            {
                AttackTimer -= 0.05f;
            }
        }

        public static Vector2 GetArcVel(Vector2 startingPos, Vector2 targetPos, float gravity, float? minArcHeight = null, float? maxArcHeight = null, float? maxXvel = null, float? heightabovetarget = null)
        {
            Vector2 DistanceToTravel = targetPos - startingPos;
            float MaxHeight = DistanceToTravel.Y - (heightabovetarget ?? 0);

            if (minArcHeight != null)
                MaxHeight = Math.Min(MaxHeight, -(float)minArcHeight);

            if (maxArcHeight != null)
                MaxHeight = Math.Max(MaxHeight, -(float)maxArcHeight);

            float TravelTime;
            float neededYvel;

            if (MaxHeight <= 0)
            {
                neededYvel = -(float)Math.Sqrt(-2 * gravity * MaxHeight);
                TravelTime = (float)Math.Sqrt(-2 * MaxHeight / gravity) + (float)Math.Sqrt(2 * Math.Max(DistanceToTravel.Y - MaxHeight, 0) / gravity); //time up, then time down
            }
            else
            {
                neededYvel = 0;
                TravelTime = (-neededYvel + (float)Math.Sqrt(Math.Pow(neededYvel, 2) - (4 * -DistanceToTravel.Y * gravity / 2))) / (gravity); //time down
            }

            if (maxXvel != null)
                return new Vector2(MathHelper.Clamp(DistanceToTravel.X / TravelTime, -(float)maxXvel, (float)maxXvel), neededYvel);

            return new Vector2(DistanceToTravel.X / TravelTime, neededYvel);
        }
        #endregion

        #region Rockfall
        const float Rockfall_PercentOfAnimationDuringWhichStompHappens = 7 / 12f;
        const int RockfallRepeats = 3;

        public void SignathionRockfallAttack()
        {
            if (SubState == ActionState.Rockfall)
            {
                Stamina -= 1f;

                NPC.TargetClosest(true);
                NPC.noTileCollide = false;
                NPC.noGravity = false;

                SubState = ActionState.Rockfall_RepeatedStomps;
                NPC.velocity = Vector2.Zero;

                //Do a preliminary search
                Point centerPoint = NPC.Center.ToTileCoordinates();
                if (Math.Abs(centerPoint.X - PointOfInterestMarkerSystem.NautilusChamberPos.X) <= 76 / 2f)
                    centerPoint = PointOfInterestMarkerSystem.NautilusChamberPos.ToPoint();
                List<Point> ceilingTopography = GetCeilingTopography_Rockfall(centerPoint, 76, 40);

                //If there is absolutely no ceiling, just use another attack
                if (ceilingTopography.Count == 0)
                {
                    AttackTimer = 0;
                    Stamina += 1f;
                    AIState = ActionState.SlowWalk;
                }
            }

            if (SubState == ActionState.Rockfall_RepeatedStomps)
            {
                float timeBetweenRocks = 1.55f - 0.05f * DifficultyScale;

                float attackSpeed = 1 / (60f * RockfallRepeats * timeBetweenRocks);
                AttackTimer -= attackSpeed;

                float completionOfCurrentStomp = (1 - AttackTimer).Modulo(1 / (float)RockfallRepeats);
                float singleStompDuration = 1 / (float)RockfallRepeats;

                if (completionOfCurrentStomp >= singleStompDuration * Rockfall_PercentOfAnimationDuringWhichStompHappens && completionOfCurrentStomp < attackSpeed + singleStompDuration * Rockfall_PercentOfAnimationDuringWhichStompHappens)
                {
                    SoundEngine.PlaySound(SignathionHeavyStomp, NPC.Center);

                    if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.Distance(NPC.Center) < 1500)
                        CameraManager.Shake += 3;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        //Get the topography of tiles above the target
                        Point centerPoint = Target.Top.ToTileCoordinates();
                        List<Point> ceilingTopography = GetCeilingTopography_Rockfall(centerPoint, 30, 40, 40, true);

                        //Get the topography of tiles above the npc or chamber center
                        centerPoint = NPC.Center.ToTileCoordinates();
                        if (Math.Abs(centerPoint.X - PointOfInterestMarkerSystem.NautilusChamberPos.X) <= 76 / 2f && Math.Abs(centerPoint.Y - PointOfInterestMarkerSystem.NautilusChamberPos.Y) < 80)
                        {
                            centerPoint = PointOfInterestMarkerSystem.NautilusChamberPos.ToPoint();
                        }
                        //Add that topography to our current map
                        List<Point> chamberCeilingTopography = GetCeilingTopography_Rockfall(centerPoint, 76, 40, 40);
                        chamberCeilingTopography.ForEach(p => { if (!ceilingTopography.Any(p2 => p2.X == p.X)) ceilingTopography.Add(p); });

                        //Space out the boulders
                        int spacing = 4;
                        int randomOffset = DifficultyScale < 2 ? Main.rand.Next(spacing) : (Target.Center.ToTileCoordinates().X % spacing); //The spacing is always perfetly aligned with the player to make it a bit more fair with the quick boulders
                        ceilingTopography.RemoveAll(p => (p.X - randomOffset) % spacing != 0);

                        //Remember the boulder RIGHT above the player
                        Point rightAbovePlayer = ceilingTopography.Find(p => Math.Abs(p.X - Target.Center.ToTileCoordinates().X) <= spacing / 2);

                        ceilingTopography = EnsureEscapeOptions(ceilingTopography, rightAbovePlayer, 4);
                        int boulderTelegraphTime = 55 - (int)(5 * DifficultyScale);

                        if (DifficultyScale > 0)
                        {
                            ceilingTopography.Remove(rightAbovePlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), rightAbovePlayer.ToWorldCoordinates(), Vector2.UnitY * 4f, ModContent.ProjectileType<SandstoneBoulder>(), Rockfall_RockDamage / 2, 2, Main.myPlayer, boulderTelegraphTime);
                        }

                        int bouldersFalling = Math.Min(Main.rand.Next(7 + (int)DifficultyScale, 12), ceilingTopography.Count);

                        for (int i = 0; i < bouldersFalling; i++)
                        {
                            Point tileTarget = ceilingTopography[Main.rand.Next(ceilingTopography.Count)];
                            ceilingTopography.Remove(tileTarget);
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), tileTarget.ToWorldCoordinates(), Vector2.UnitY * 4f, ModContent.ProjectileType<SandstoneBoulder>(), Rockfall_RockDamage / 2, 2, Main.myPlayer, boulderTelegraphTime);
                        }

                    }
                }
            }
        }

        public static List<Point> GetCeilingTopography_Rockfall(Point searchOrigin, int width, int maxHeight, int? heightAtWhichBouldersAppearIfNoCeilingIsFound = null, bool breakAtWalls = false, int? initialDisplacement = null)
        {
            List<Point> ceilingTopography = new List<Point>();
            bool clearTileUnder;
            int forcedDirection = 0;

            for (int i = 0; i < width / 2 && i > (-width / 2 - 1); i *= -1)
            {
                if (forcedDirection != 0)
                    i = (int)Math.CopySign(i, forcedDirection);

                clearTileUnder = false;
                bool foundCeiling = false;

                for (int j = 0; j < maxHeight; j++)
                {
                    Point pos = searchOrigin + new Point(i, -j);
                    Tile tile = Framing.GetTileSafely(pos);

                    if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !TileID.Sets.Platforms[tile.TileType])
                    {
                        //In the case of the first position being in a wall, we don't want nautilus to just throw his trident at the wall and spawn a boulder there
                        if (j == 0)
                        {
                            Tile tileBelow = Framing.GetTileSafely(pos + new Point(0, 1));
                            if (!tileBelow.HasUnactuatedTile || !Main.tileSolid[tileBelow.TileType])
                                clearTileUnder = true;
                        }

                        if (clearTileUnder)
                        {
                            ceilingTopography.Add(pos);
                            foundCeiling = true;
                            break;
                        }

                        //If the tile is solid, and doesnt have a clear tile below itself, mark the next tile up as lacking a clear tile under, since this one was solid.
                        //Avoids us to check twice
                        clearTileUnder = false;
                    }

                    else
                        clearTileUnder = true;
                }

                if ((!clearTileUnder || !foundCeiling) && breakAtWalls)
                {
                    if (forcedDirection != 0 || i == 0)
                        break;
                    else
                        forcedDirection = -Math.Sign(i);
                }

                if (clearTileUnder && heightAtWhichBouldersAppearIfNoCeilingIsFound != null && !foundCeiling)
                {
                    ceilingTopography.Add(searchOrigin + new Point(i, -heightAtWhichBouldersAppearIfNoCeilingIsFound.Value));
                }

                if (i >= 0 || forcedDirection != 0)
                    i += 1 * (forcedDirection != 0 ? forcedDirection : 1);
            }

            return ceilingTopography;
        }

        //Makes sure that there is at least one emtpy spot around the center point
        public static List<Point> EnsureEscapeOptions(List<Point> topography, Point centerPoint, int zoneHalfWidth)
        {
            int playerPositionIndex = topography.FindIndex(p => p.X == centerPoint.X);

            int startIndex = Math.Max(playerPositionIndex - zoneHalfWidth, 0);
            int endIndex = Math.Min(playerPositionIndex + zoneHalfWidth, topography.Count);

            int escapeOptionIndex = Main.rand.Next(startIndex, endIndex);
            if (escapeOptionIndex == playerPositionIndex)
            {
                escapeOptionIndex += Main.rand.Next(1, zoneHalfWidth) * (Main.rand.NextBool() ? -1 : 1);
                escapeOptionIndex = Math.Clamp(escapeOptionIndex, startIndex, endIndex);
            }

            int escapeOptionIndex2 = Main.rand.Next(startIndex, endIndex);
            if (escapeOptionIndex2 == playerPositionIndex)
            {
                escapeOptionIndex2 += Main.rand.Next(1, zoneHalfWidth) * (Main.rand.NextBool() ? -1 : 1);
                escapeOptionIndex2 = Math.Clamp(escapeOptionIndex, startIndex, endIndex);
            }

            topography.RemoveAt(escapeOptionIndex);


            startIndex = Math.Max(playerPositionIndex - zoneHalfWidth, 0);
            endIndex = Math.Min(playerPositionIndex + zoneHalfWidth, topography.Count);
            escapeOptionIndex = Main.rand.Next(startIndex, endIndex);
            if (escapeOptionIndex == playerPositionIndex)
            {
                escapeOptionIndex += Main.rand.Next(1, zoneHalfWidth) * (Main.rand.NextBool() ? -1 : 1);
                escapeOptionIndex = Math.Clamp(escapeOptionIndex, startIndex, endIndex);
            }
            topography.RemoveAt(escapeOptionIndex);


            return topography;
        }
        #endregion

        #region Charge
        public void SignathionChargeAttack()
        {
            if (SubState == ActionState.Charge)
            {
                yFrameSig = 0;
                frameCounterSig = 0f;
                NPC.TargetClosest(true);
                SubState = ActionState.Charge_GetReady;
                SoundEngine.PlaySound(SignathionScream, NPC.Center);
                NPC.noGravity = false;
                NPC.noTileCollide = true;
                NPC.velocity *= 0.1f;
                Stamina -= 0.5f;

                movementTarget = Vector2.Zero;
                //If starting a charge TOO Close to the player, go backwards a bit
                if (Math.Abs(Target.Center.X - NPC.Center.X) < 210)
                    movementTarget = NPC.Center + Vector2.UnitX * (NPC.Center.X - Target.Center.X).NonZeroSign() * (210 - Math.Abs(Target.Center.X - NPC.Center.X));
            }

            if (SubState == ActionState.Charge_GetReady)
            {
                float telegraphTime = 0.7f;

                bool insideTiles = Collision.SolidCollision(CollisionBoxOrigin, CollisionBoxWidth, CollisionBoxHeight);

                //If nto colliding with any tile, stop phasing through tiles
                if (!insideTiles && Collision.CanHitLine(NPC.Bottom - Vector2.UnitY * CollisionBoxHeight, 1, 1, Target.Top, 1, 1))
                    NPC.noTileCollide = false;
                //If below the player, rise up through tiles
                else if (insideTiles && NPC.Bottom.Y > Target.Bottom.Y)
                    NPC.velocity.Y = -0.8f;
                //Stop falling if inside of tiles
                else
                    NPC.velocity.Y = 0;

                //Slow down if nowhere to go
                if (movementTarget == Vector2.Zero)
                {
                    NPC.velocity.X *= 0.8f;
                    if (Math.Abs(NPC.velocity.X) < 1)
                        NPC.velocity.X = 0;
                }

                //If intending to walk backwards, go there
                else
                {
                    telegraphTime *= 4f;
                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, (movementTarget.X - NPC.Center.X).NonZeroSign() * 6f, 0.1f);

                    StepUp();
                    GoUpOneBlock();

                    if (Math.Abs(NPC.Center.X - movementTarget.X) < 10 + NPC.velocity.Length() || NPC.collideX || AttackTimer < 0.1f)
                    {
                        NPC.velocity *= 0.6f;
                        movementTarget = Vector2.Zero;
                        AttackTimer = 1; //Reset the attack timer so he can properly do it this time

                        frameCounterSig = 0f;
                        yFrameSig = 0;
                    }
                }

                AttackTimer -= 1 / (60f * telegraphTime);

                if (AttackTimer <= 0)
                {
                    if (!Collision.CanHitLine(NPC.Bottom - Vector2.UnitY * CollisionBoxHeight, 1, 1, Target.Top, 1, 1))
                    {
                        NPC.noGravity = true;
                        NPC.noTileCollide = true;
                    }
                    NPC.collideX = false;
                    NPC.collideY = false;

                    NPC.direction = (Target.Center.X - NPC.Center.X).NonZeroSign(); //Retarget towards te player just in case
                    oldPosition = NPC.Center;
                    ExtraMemory = -1;

                    SubState = ActionState.Charge_Run;
                    AttackTimer = 1;

                    frameCounterSig = 0f;
                    if (xFrameSig != 2)
                        yFrameSig = 0;
                    else
                        yFrameSig = 8;
                }
            }

            else if (SubState == ActionState.Charge_Run)
            {
                float chargeTime = 1.7f - DifficultyScale * 0.12f; //Since difficulty makes him run faster, we also need to shorten the dash (lol)

                //If charging past the player, shorten the remaining time
                if ((NPC.Center.X - Target.Center.X).NonZeroSign() == NPC.direction)
                {
                    //Save hte position of the player when they dodged
                    if (ExtraMemory == -1)
                        ExtraMemory = Target.Center.X;

                    float instantSkipChargeTimePercent = 0.7f;
                    if (AttackTimer > instantSkipChargeTimePercent)
                    {
                        AttackTimer = 1 - instantSkipChargeTimePercent;
                        ExtraMemory = -2; //Remembers there was a skip
                    }

                    chargeTime *= 0.36f;
                    //shorten the remaining time even further if going way past the player
                    if (Math.Abs(NPC.Center.X - Target.Center.X) > 400)
                        chargeTime *= 0.7f;
                }

                AttackTimer -= 1 / (60f * chargeTime);

                float runSpeed = 9.8f + DifficultyScale * 2f;
                float accelerationFactor = 20 - DifficultyScale * 3;
                bool acceptTopSurfaces = CanFallThroughPlatforms().Value;

                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * runSpeed, 1 / accelerationFactor);

                if (!NPC.noTileCollide)
                {
                    NPC.noGravity = false;

                    StepUp();
                    GoUpOneBlock();
                    if (NPC.collideX && !Collision.SolidCollision(CollisionBoxOrigin + NPC.velocity, CollisionBoxWidth, CollisionBoxHeight, acceptTopSurfaces))
                        NPC.collideX = false;

                    //If moving up, and said upwards movement is making signathion go higher up than its target, slow down that jump
                    if (NPC.velocity.Y < 0 && Math.Abs(NPC.Center.Y + NPC.velocity.Y - Target.Center.Y) > Math.Abs(NPC.Center.Y - Target.Center.Y))
                        NPC.velocity.Y *= 0.93f;

                    if (NPC.collideX) //Bonk agaisntthe wall, ending the attack
                    {
                        AttackTimer = 0;
                        SoundEngine.PlaySound(SignathionHeavyStomp, NPC.Center);
                        NPC.velocity.X *= -0.6f;
                    }
                }

                //notilecollide movement
                else
                {
                    bool insideSolids = Collision.SolidCollision(CollisionBoxOrigin, CollisionBoxWidth, CollisionBoxHeight, acceptTopSurfaces);
                    bool upperBodyInSolids = Collision.SolidCollision(CollisionBoxOrigin, CollisionBoxWidth, CollisionBoxHeight - 4, acceptTopSurfaces);

                    //If the feet are touching the ground, but not the rest of the body, stop falling or raising up
                    if (insideSolids && !upperBodyInSolids)
                        NPC.velocity.Y = 0;

                    //If the charge went past the player, stop not-colliding
                    if (Math.Abs(NPC.Center.X + NPC.velocity.X - Target.Center.X) > Math.Abs(NPC.Center.X - Target.Center.X))
                        NPC.noTileCollide = false;

                    //If signathion has line of sight with the target, stop phasing through tiles
                    else if (Collision.CanHitLine(NPC.Bottom - Vector2.UnitY * CollisionBoxHeight, 1, 1, Target.Top, 1, 1))
                    {
                        //If signathion is currently moving up, and said upwards movement is making him go ABOVE the player target, slow down the jump to avoid overshooting
                        //This happens when he climbs slopes for example
                        if (NPC.velocity.Y < 0 && Math.Abs(NPC.Center.Y + NPC.velocity.Y - Target.Center.Y) > Math.Abs(NPC.Center.Y - Target.Center.Y))
                            NPC.velocity.Y *= 0.8f;

                        NPC.noTileCollide = false;
                    }

                    //If inside tiles and below the player, raise up as fast as youre running
                    else if (NPC.Bottom.Y > Target.Bottom.Y && insideSolids)
                    {
                        if (upperBodyInSolids)
                            NPC.velocity.Y = -runSpeed;
                    }

                    //if above the player and not touching any ground, fall down
                    else if (!insideSolids || (CollisionBoxOrigin.X < Target.Hitbox.X && CollisionBoxOrigin.X + (float)NPC.width > (float)(Target.Hitbox.X + Target.Hitbox.Width)))
                    {
                        NPC.velocity.Y += 0.5f;
                        if (NPC.velocity.Y > 8)
                            NPC.velocity.Y = 8;
                    }

                    //Else if touching ground, stop falling
                    else
                        NPC.velocity.Y = 0;
                }

                if (AttackTimer <= 0)
                {
                    NPC.velocity.X *= 0.6f; //Slow down

                    bool doATridentThrow = DifficultyScale > 2 || ExtraMemory == -2; //Always does a trident throw in death, or if the player hopped over signa very soon
                    if (!doATridentThrow && DifficultyScale > 0)
                        doATridentThrow = Main.rand.NextBool(4 - (int)DifficultyScale); //50% chance to follow it up in rev, 30% in expert

                    //If the player went above cnid at 46% of the way through the dash or before
                    if (!doATridentThrow && (ExtraMemory > -1 && oldPosition.X != NPC.Center.X))
                    {
                        float percentageOfTheDashAtWhichThePlayerDodged = (Math.Abs(oldPosition.X - ExtraMemory) / Math.Abs(oldPosition.X - NPC.Center.X));
                        float maxPercentageOfDashAtWhichTridentIsThrown = 0.6f;

                        doATridentThrow = percentageOfTheDashAtWhichThePlayerDodged <= maxPercentageOfDashAtWhichTridentIsThrown;
                    }

                    if (doATridentThrow)
                    {
                        SoundEngine.PlaySound(SoundID.AbigailSummon, NPC.Center);
                        movementTarget = Target.Center;
                        AttackTimer = 1;
                        SubState = ActionState.Charge_TridentThrow;
                        ExtraMemory = 0;
                    }

                    else
                    {
                        AttackTimer = 1;
                        SubState = ActionState.Charge_Recovery;
                        ExtraMemory = 0;
                    }
                }
            }

            else if (SubState == ActionState.Charge_TridentThrow)
            {
                //Retarget
                float retargetingStrenght = DifficultyScale * 0.08f + 0.70f;
                float retargetingStrenghtMultiplier = ExtraMemory == 0 ? (float)Math.Pow(AttackTimer, 0.1f) : 1f;
                Vector2 retargetingPosition = Target.Center;
                if (ExtraMemory != 0)
                {
                    retargetingStrenght = 0.8f;
                    retargetingPosition += Target.velocity * 3f;
                }

                movementTarget = Vector2.Lerp(movementTarget, retargetingPosition, retargetingStrenght * retargetingStrenghtMultiplier);

                float attackSpeed = 1 / (60f * (0.42f + ExtraMemory)); //Extramemory is usually 0 unless accessed through an upwards slice attack
                AttackTimer -= attackSpeed;

                NPC.direction = NPC.DirectionTo(movementTarget).X.NonZeroSign();
                NPC.velocity.X *= 0.96f;

                if (AttackTimer <= 0)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float ai0 = DifficultyScale >= 3 ? Target.whoAmI : -1;
                        int damage = ExtraMemory == 0 ? Charge_TridentThrowDamage : Charge_ComboThrowDamage;

                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center - Vector2.UnitY * 68f, NPC.DirectionTo(movementTarget) * 10f, ModContent.ProjectileType<NautilusTrident>(), damage / 2, 1, Main.myPlayer, ai0, 0f);
                    }

                    SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, NPC.Center);
                    AttackTimer = 1;
                    SubState = ActionState.Charge_Recovery;
                    ExtraMemory = 1; //This is solely for the purposes of having the animation play out
                }
            }

            else if (SubState == ActionState.Charge_Recovery || SubState == ActionState.Charge_UpSlice)
            {
                float rechargeSpeed = 0.2f;

                if (SubState == ActionState.Charge_UpSlice)
                {
                    //Normally here he plays an anim but not rn
                    if (AttackTimer == 1f)
                        SoundEngine.PlaySound(SignathionScreamShort, NPC.Center);

                    if (DifficultyScale >= 2)
                        SubState = ActionState.Charge_TridentThrow;

                    rechargeSpeed = 0.4f;
                }

                NPC.noTileCollide = false;
                NPC.noGravity = false;

                AttackTimer -= 1 / (60f * rechargeSpeed);
                NPC.velocity.X *= 0.9f;
                NPC.direction = (Target.Center.X - NPC.Center.X).NonZeroSign();
            }
        }
        #endregion
    }

    #region particles

    public class SignathionRifleChargeStreak : Particle
    {
        public override string Texture => AssetDirectory.Particles + "DrainLineBloom";
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public Vector2 StartDestination;
        public Vector2 FinalDestination;
        public Color StartColor;
        public Color EndColor;

        public SignathionRifleChargeStreak(Vector2 startPosition, Vector2 endPosition, float thickness, Color colorStart, Color colorEnd, int lifetime)
        {
            StartDestination = startPosition;
            FinalDestination = endPosition;
            Scale = thickness;
            Velocity = Vector2.Zero;

            Vector2 fullVector = (endPosition - startPosition);
            Rotation = fullVector.ToRotation();
            StartColor = colorStart;
            EndColor = colorEnd;
            Color = colorStart;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Position = Vector2.Lerp(StartDestination, FinalDestination, (float)Math.Pow(LifetimeCompletion, 0.5f));

            Color = Color.Lerp(StartColor, EndColor, LifetimeCompletion);
            Lighting.AddLight(Position, Color.ToVector3() * 0.2f);
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D tex = ParticleTexture;

            float lenght = (Position - Vector2.Lerp(StartDestination, FinalDestination, (float)Math.Pow(LifetimeCompletion, 1.7f))).Length() / tex.Height;
            Vector2 scale = new Vector2(Scale, lenght);

            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height);

            spriteBatch.Draw(tex, Position - basePosition, null, Color, Rotation - MathHelper.PiOver2, origin, scale, SpriteEffects.None, 0);

        }
    }


    public class SignathionShotgunChargeDust : Particle
    {
        public override string Texture => AssetDirectory.SirNautilus + "SignathionTelegraphDust";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;
        public override int FrameVariants => 3;

        public Vector2 Magnet;
        public Color StartColor;
        public Color EndColor;
        public byte Alpha;
        public float MagnetStrentgh;
        public float Acceleration;

        public SignathionShotgunChargeDust(Vector2 position, Vector2 velocity, Vector2 homePoint, float scale, Color colorStart, Color colorEnd, int lifetime, byte alpha = 255, float magnetStrentgh = 0.03f, float acceleration = 0f)
        {
            Position = position;
            Velocity = velocity;
            Scale = scale;
            Magnet = homePoint;

            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            StartColor = colorStart;
            EndColor = colorEnd;
            Color = colorStart;
            Lifetime = lifetime;
            Alpha = alpha;
            MagnetStrentgh = magnetStrentgh;
            Acceleration = acceleration;

            Variant = Main.rand.Next(3);
        }

        public override void Update()
        {
            Velocity = Vector2.Lerp(Velocity, Position.DirectionTo(Magnet) * Velocity.Length(), MagnetStrentgh);
            Velocity *= 1 + Acceleration;

            Color = Color.Lerp(StartColor, EndColor, LifetimeCompletion) * (float)Math.Pow(LifetimeCompletion, 0.3f);
            Lighting.AddLight(Position, Color.ToVector3() * 0.2f);

            Scale *= 0.955f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D tex = ParticleTexture;
            Texture2D bloomTexture = AssetDirectory.CommonTextures.BloomCircle.Value;

            Color color = Color;
            color.A = Alpha;

            Rectangle frame = new Rectangle(0, 6 * Variant, 6, 6);

            spriteBatch.Draw(tex, Position - basePosition, frame, color, Rotation, frame.Size() / 2, Scale, SpriteEffects.None, 0);
            spriteBatch.Draw(bloomTexture, Position - basePosition, null, color * 0.5f, Rotation, bloomTexture.Size() / 2, Scale * 0.1f, SpriteEffects.None, 0);
        }
    }
    public class SignathionSpitTelegraphRing : Particle
    {
        public override string Texture => AssetDirectory.Particles + "PulseInwards";
        public override bool UseAdditiveBlend => true;
        public override bool SetLifetime => true;

        private float OriginalScale;
        private float FinalScale;
        private float opacity;
        private Color BaseColor;

        public SignathionSpitTelegraphRing(Vector2 position, Vector2 velocity, Color color, float originalScale, float finalScale, int lifeTime)
        {
            Position = position;
            Velocity = velocity;
            BaseColor = color;
            OriginalScale = originalScale;
            FinalScale = finalScale;
            Scale = originalScale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void Update()
        {
            float pulseProgress = PiecewiseAnimation(LifetimeCompletion, new CurveSegment[] { new CurveSegment(PolyOutEasing, 0f, 0f, 1f, 4) });
            Scale = MathHelper.Lerp(OriginalScale, FinalScale, pulseProgress);

            opacity = (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2);

            Color = BaseColor * opacity;
            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
            Velocity *= 0.95f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D tex =ParticleTexture;
            spriteBatch.Draw(tex, Position - basePosition, null, Color * opacity, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
        }
    }
    #endregion
}
