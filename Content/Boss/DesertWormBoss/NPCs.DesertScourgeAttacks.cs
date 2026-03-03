using CalamityFables.Content.Buffs;
using CalamityFables.Content.Dusts;
using Terraria.Utilities;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public partial class DesertScourge : ModNPC
    {
        public bool ActBasedOnSubstate(bool insideTiles, bool onlyInsideTopSurfaces, ref float rotationAmount, ref float rotationFade, ref float velocityWiggle, ref float velocityWiggleFrequency)
        {
            bool swapToNextAttack = false;

            if (AIState == ActionState.IdleMovement)
            {
                NPC.TargetClosest(false);

                if (NPC.target < 0 || 
                    NPC.target == Main.maxPlayers || 
                    Main.player[NPC.target].dead || 
                    !Main.player[NPC.target].active ||
                    distanceDespawnTimer <= 0)
                {
                    AIState = ActionState.Despawning;
                    NPC.netUpdate = true;
                }

                if (!target.ZoneDesert)
                    GetClosestDesertPlayer(16 * 160);

                if (biomeDespawnTimer <= 0)
                {
                    AIState = ActionState.Despawning;
                    NPC.netUpdate = true;
                }

                IdleBehavior(insideTiles, onlyInsideTopSurfaces);
                AttackTimer--;
                if (AttackTimer <= 0)
                    swapToNextAttack = true;
            }

            if (AIState == ActionState.Despawning)
                BurrowDownAndDespawn();

            if (AIState == ActionState.CutsceneDeath)
                DeathAnimation(insideTiles, onlyInsideTopSurfaces, ref rotationAmount, ref rotationFade, ref velocityWiggle, ref velocityWiggleFrequency);

            if (AIState == ActionState.CutsceneFightStart)
                swapToNextAttack = SpawnAnimation(insideTiles, onlyInsideTopSurfaces, ref rotationAmount, ref rotationFade, ref velocityWiggle, ref velocityWiggleFrequency);

            if (AIState == ActionState.UnnagroedMovement)
                swapToNextAttack = UnnagroedMovement(insideTiles, onlyInsideTopSurfaces, ref rotationAmount, ref rotationFade, ref velocityWiggle, ref velocityWiggleFrequency);

            if (AIState == ActionState.FastLunge)
                swapToNextAttack = FastLungeAttack(insideTiles, onlyInsideTopSurfaces, ref rotationAmount, ref rotationFade, ref velocityWiggle, ref velocityWiggleFrequency);

            if (AIState == ActionState.LeviathanLunge)
                swapToNextAttack = LeviathanLungeAttack(insideTiles, onlyInsideTopSurfaces, ref rotationAmount, ref rotationFade, ref velocityWiggle, ref velocityWiggleFrequency);

            if (AIState == ActionState.PreyBelch)
                swapToNextAttack = PreyBelchAttack(insideTiles, onlyInsideTopSurfaces, ref rotationAmount, ref rotationFade, ref velocityWiggle, ref velocityWiggleFrequency);

            if (AIState == ActionState.ElectroLunge)
                swapToNextAttack = ElectroLungeAttack(insideTiles, onlyInsideTopSurfaces, ref rotationAmount, ref rotationFade, ref velocityWiggle, ref velocityWiggleFrequency);

            if (AIState == ActionState.ChungryLunge)
                swapToNextAttack = ChungryLungeAttack(insideTiles, onlyInsideTopSurfaces, ref rotationAmount, ref rotationFade, ref velocityWiggle, ref velocityWiggleFrequency);

            return swapToNextAttack;
        }

        #region Attack selection
        public void SelectNextAttack()
        {
            //Reset extra memory
            ExtraMemory = -1;

            //Transition from attack to idle
            if (AIState != ActionState.IdleMovement)
            {
                //AIState = ActionState.IdleMovement;
                movementTarget = Vector2.Zero;

                //Set the longest time possible while waiting for sync
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    AttackTimer = (int)(60 * 4.5f);
                    SubState = ActionState.IdleMovement_MeleeArcs;
                    return;
                }

                //Random substate selection
                ActionState[] idleVariants = new ActionState[] { ActionState.IdleMovement_MeleeArcs, ActionState.IdleMovement_SmallArcs, ActionState.IdleMovement_SlowArcs };
                SubState = Main.rand.Next(idleVariants);

                float timeUntilNextAttack = Main.rand.Next(60 * 2, (int)(60 * 4.5f));
                if (SecondPhase)
                    timeUntilNextAttack -= 200;

                timeUntilNextAttack = Math.Max(timeUntilNextAttack, 90);

                //Fast as fuck boi
                if (Main.getGoodWorld)
                    timeUntilNextAttack = 30;

                AttackTimer = timeUntilNextAttack;
                NPC.netUpdate = true;
            }

            //Pick a new attack
            else
            {
                WeightedRandom<ActionState> attackPool = new WeightedRandom<ActionState>(Main.rand);
                float distanceToTarget = NPC.Distance(target.Center);

                int[] chungryMeatTypes = new int[] { ModContent.NPCType<GroundBeef>(), ModContent.NPCType<ScourgeFlesh>() };
                NPC chungriestMeat = Main.npc.Where(n => n.active && n.ai[0] != 0 && n.ai[2] > ChungryLunge_BufferTime * 60f && chungryMeatTypes.Contains(n.type) && n.Distance(NPC.Center) < 3000f)
                    .OrderBy(n => n.Distance(NPC.Center))
                    .FirstOrDefault();

                attackPool.Add(ActionState.FastLunge, 1f);
                attackPool.Add(ActionState.LeviathanLunge, 1f);
                attackPool.Add(ActionState.PreyBelch, 1f);
                attackPool.Add(ActionState.ElectroLunge, 0.85f);

                if (chungriestMeat != null)
                    attackPool.Add(ActionState.ChungryLunge, 8f);

                ActionState potentialNewState = ActionState.IdleMovement;
                //Reduce the probabily for it to repeat an attack
                for (int i = 0; i < attackPool.elements.Count; i++)
                {
                    if (attackPool.elements[i].Item1 == PreviousState)
                        attackPool.elements[i] = new Tuple<ActionState, double>(PreviousState, attackPool.elements[i].Item2 * 0.1f);
                }

                if (attackPool.elements.Count > 0)
                    potentialNewState = attackPool.Get();

                SubState = potentialNewState;
                PreviousState = potentialNewState;
                AttackTimer = 0;

                if (AIState == ActionState.ChungryLunge)
                {
                    //Can repeat chungry lunges
                    PreviousState = ActionState.IdleMovement;
                    ExtraMemory = chungriestMeat.whoAmI;
                    movementTarget = chungriestMeat.Center;
                }

                NPC.netUpdate = true;
            }
        }
        #endregion

        #region Manual control
        public void ManualControlBehavior(ref float rotationAmount, ref float rotationFade, ref float velocityWiggle, ref float velocityWiggleFrequency, ref bool updateSegments)
        {
            NPC.TargetClosest();
            if (Main.mouseLeft && Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
            {
                NPC.velocity = NPC.DirectionTo(Main.MouseWorld) * 7.5f;
                NPC.rotation = NPC.velocity.ToRotation();

                if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
                    NPC.velocity *= 2f;
            }
            else
            {
                velocityWiggle = 0f;
                NPC.velocity = Vector2.Zero;
            }
        }
        #endregion

        #region Base movement tools
        public void CirclePlayer(float radius = 1200f, float speed = 30f, bool inGround = false, bool onlyInPlatforms = false, float gravityDepletionMultiplier = 1f, Vector2? offsetFromPlayer = null)
        {
            Vector2 targetPosition = target.Center;
            if (offsetFromPlayer.HasValue)
                targetPosition += offsetFromPlayer.Value;

            int side = Vector2.Dot(NPC.Center - targetPosition, new Vector2(0, -1).RotatedBy(NPC.rotation)).NonZeroSign();

            //Ideal position is at the side of the player, making it coil around them
            Vector2 goalPosition = targetPosition + NPC.SafeDirectionFrom(targetPosition).RotatedBy(side) * radius;
            Vector2 goalVelocity = (goalPosition - NPC.Center) / speed;
            NPC.velocity += ((goalVelocity - NPC.velocity) / speed) * (0.5f + 0.5f * Utils.GetLerpValue(0f, 1f, AntiGravityCharge));

            float speedLimit = 15f;
            if (!inGround)
            {
                if (AntiGravityCharge == 1 && DifficultyScale >= 2)
                {
                    NPC.velocity = Vector2.Lerp(NPC.velocity, goalVelocity * 2f, 0.1f);
                    NPC.velocity *= 1.2f;
                    AntiGravityCharge -= 0.001f;
                }

                float antiGravityDepletion = 0.02f * Utils.GetLerpValue(500, 0f, NPC.Center.Y - targetPosition.Y, true);
                AntiGravityCharge -= antiGravityDepletion * gravityDepletionMultiplier;
                if (AntiGravityCharge < 0)
                    AntiGravityCharge = 0;

                NPC.velocity.Y += 1f * (1 - AntiGravityCharge);
            }

            else
            {
                if (!onlyInPlatforms)
                {
                    int maxSurfaceDistance = 70;
                    float dustProbability = 0.08f;
                    if (AIState == ActionState.IdleMovement)
                    {
                        maxSurfaceDistance = 300;
                        dustProbability = 0.03f;
                    }
                    TelegraphSand(150f, 1f, dustProbability, maxSurfaceDistance, noFalloff: false);
                }

                if (AntiGravityCharge < 0.1f)
                    NPC.velocity.Y += 1f * (1 - AntiGravityCharge);

                if (NPC.velocity.Length() < speedLimit)
                    NPC.velocity *= 1.05f;
            }
        }

        public void BasicSimulateMovement(float maxSpeed)
        {
            NPC.velocity.Y += 0.5f;
            if (NPC.velocity.Y > maxSpeed)
                NPC.velocity.Y = maxSpeed;
        }

        //Ty turing
        public void GoTowardsRadial(Vector2 goalPosition, Vector2 orbitPoint, float timeLeft, float maxSpeed = float.PositiveInfinity)
        {
            if (timeLeft <= 1)
            {
                //force precise movement if no time is left
                NPC.velocity = goalPosition - NPC.Center;
            }
            else
            {
                float dRadial = (goalPosition - orbitPoint).Length() - (NPC.Center - orbitPoint).Length();
                float dAngle = (goalPosition - orbitPoint).ToRotation() - (NPC.Center - orbitPoint).ToRotation();
                while (dAngle > MathHelper.Pi)
                {
                    dAngle -= MathHelper.TwoPi;
                }
                while (dAngle < -MathHelper.Pi)
                {
                    dAngle += MathHelper.TwoPi;
                }

                NPC.velocity = (new Vector2(dRadial, dAngle * (NPC.Center - orbitPoint).Length()).RotatedBy((NPC.Center - orbitPoint).ToRotation()) + (goalPosition - NPC.Center)) / 2 / timeLeft;
            }

            if (NPC.velocity.Length() > maxSpeed)
            {
                NPC.velocity.Normalize();
                NPC.velocity *= maxSpeed;
            }
        }
        #endregion

        #region Idle
        public void IdleBehavior(bool inGround, bool onlyInsidePlatforms)
        {
            if (SubState == ActionState.IdleMovement)
            {
                //Substate selection happens in attack selection. This is just here as a failsafe if somehow we fall here
                SubState = ActionState.IdleMovement_LargeArcs;
            }

            if (SubState == ActionState.IdleMovement_LargeArcs)
                CirclePlayer(200, 30, inGround, onlyInsidePlatforms);
            else if (SubState == ActionState.IdleMovement_SmallArcs)
                CirclePlayer(200, 30, inGround, onlyInsidePlatforms, gravityDepletionMultiplier: 4f, Vector2.UnitY * -300);

            else if (SubState == ActionState.IdleMovement_MeleeArcs)
                CirclePlayer(100, 30, inGround, onlyInsidePlatforms, gravityDepletionMultiplier: 1.3f, Vector2.UnitY * -100);
            else if (SubState == ActionState.IdleMovement_SlowArcs)
            {
                CirclePlayer(40, 30, inGround, onlyInsidePlatforms, gravityDepletionMultiplier: 0.55f, -Vector2.UnitY * 100);
                if (NPC.velocity.Length() > 10)
                    NPC.velocity = NPC.velocity.SafeNormalize(Vector2.One) * 10f;
            }
            //else if (SubState == ActionState.IdleMovement_SnakeGraze) 
            //    CirclePlayer(100, 20, inGround, gravityDepletionMultiplier: 2f, Vector2.UnitY * -200 + target.velocity.X.NonZeroSign() * Vector2.UnitX * 400f);
        }
        #endregion

        #region Straight lunge
        public bool FastLungeAttack(bool inGround, bool onlyInsidePlatforms, ref float rotationAmount, ref float rotationFade, ref float velocityWiggle, ref float velocityWiggleFrequency)
        {
            if (SubState == ActionState.FastLunge)
            {
                if (!GetRandomTarget(160f * 16))
                    return true;
                SubState = ActionState.FastLunge_Burrow; //Shouldn't this be transitionintoattack? Welp, ingame it's fine so i wont change it haha
                if (!inGround || onlyInsidePlatforms)
                    SubState = ActionState.FastLunge_Burrow;

                SoundEngine.PlaySound(FastLungeStartSound, target.Center);
            }

            if (SubState == ActionState.FastLunge_TransitionIntoAttack)
            {
                BasicSimulateMovement(20f);
                if ((inGround && !onlyInsidePlatforms) || Math.Abs(NPC.velocity.X) < 3f)
                    SubState = ActionState.FastLunge_Burrow;
            }

            else if (SubState == ActionState.FastLunge_Burrow)
            {
                rotationFade = 3f;
                GoTowardsRadial(target.Center + Vector2.UnitY * 1300f, target.Center, 40 - AttackTimer / 2f);

                if (CanPlayBurrowSound(inGround, onlyInsidePlatforms))
                {
                    CameraManager.Shake += 20f;
                }

                if (inGround && AttackTimer > 40)
                    TelegraphSand(150f, 2f, 0.02f);

                AttackTimer++;
                if (inGround)
                    AttackTimer++;

                if (AttackTimer >= 80)
                {
                    AttackTimer = 0;
                    SubState = ActionState.FastLunge_Anticipation;
                }
            }

            else if (SubState == ActionState.FastLunge_Anticipation)
            {
                rotationFade = 35f;
                rotationAmount = 0.02f;
                ForceRotation = true;

                float attackSpeed = 1 / (60f * 1.4f - DifficultyScale * 0.2f);
                AttackTimer += attackSpeed;

                NPC.rotation = NPC.rotation.AngleTowards(-MathHelper.PiOver2, 0.02f + AttackTimer * 0.07f);


                if (AttackTimer >= 0.2f && AttackTimer < 0.2f + attackSpeed)
                    SoundEngine.PlaySound(FastLungeTelegraphSound, NPC.Center);

                NPC.Center = new Vector2(target.Center.X, MathHelper.Lerp(NPC.Center.Y, target.Center.Y + 1300f - 100 * AttackTimer, 0.2f));

                if (NPC.soundDelay > 26 - 20 * AttackTimer)
                    NPC.soundDelay = (int)(26 - 20 * AttackTimer);

                CameraManager.Shake = Math.Max(CameraManager.Shake, AttackTimer * 4f);
                TelegraphSand(150f, 16f * (float)Math.Pow(AttackTimer, 2f), 0.1f * AttackTimer);

                if (AttackTimer >= 1)
                {
                    AttackTimer = 0;
                    SubState = ActionState.FastLunge_Lunge;
                    movementTarget = NPC.SafeDirectionTo(target.Center) * 400f + target.Center;
                    NPC.velocity = NPC.SafeDirectionTo(movementTarget) * 55;

                    CameraManager.Shake += 15;
                    AttackTimer = 55;
                }
            }

            else if (SubState == ActionState.FastLunge_Lunge)
            {
                velocityWiggle = 0f;
                rotationAmount = 0.04f;
                rotationFade = 5f;
                NPC.velocity *= 0.99f;

                if (NPC.soundDelay > 6f)
                    NPC.soundDelay = 6;

                if (NPC.Center.Y > target.Center.Y + 600)
                    NPC.velocity = Vector2.Lerp(NPC.velocity.SafeNormalize(Vector2.Zero), NPC.DirectionTo(target.Center), 0.05f).SafeNormalize(Vector2.Zero) * NPC.velocity.Length();

                AttackTimer--;
                if (AttackTimer > 0)
                    CameraManager.Shake = AttackTimer;

                if (inGround)
                    TelegraphSand(150f, 16f, 0.1f);

                if (CanPlayEmergeSound(inGround, onlyInsidePlatforms))
                {
                    SoundEngine.PlaySound(FastLungeEmergeSound with { Volume = 0.5f });

                    //If we had the music silenced for its lunge (Happens when attacked while its passive, play an awesome vignette effect to jumpscare the player)
                    if (quietMusic && Main.LocalPlayer.Distance(NPC.Center) < 1000f)
                        VignetteFadeEffects.AddVignetteEffect(new VignettePunchModifier(20, 0.3f));

                    quietMusic = false;
                }


                if (NPC.Center.Y < movementTarget.Y + 60)
                {
                    AttackTimer = 0;
                    SubState = ActionState.FastLunge_FallDown;
                    movementTarget = Vector2.Zero;
                    NPC.velocity.Y += 4;
                    quietMusic = false;
                }
            }

            else if (SubState == ActionState.FastLunge_FallDown)
            {
                velocityWiggle = 0f;
                if (AttackTimer < 20f)
                    NPC.velocity.X = Math.Clamp(NPC.velocity.X + (target.Center.X - NPC.Center.X).NonZeroSign() * 0.70f, -8f, 8f);
                else if (AttackTimer > 60f)
                    NPC.velocity.X *= 0.97f;

                NPC.velocity.Y += 1.2f + 0.3f * AttackTimer / 60f;
                NPC.velocity.Y = Math.Min(NPC.velocity.Y, 20 + DifficultyScale * 4);

                AttackTimer++;

                if (AttackTimer > 120 || target.Center.Y < NPC.Center.Y - 500)
                {
                    return true;
                }
            }

            if (target.Distance(NPC.Center) > 200 * 16)
                return true;
            return false;
        }
        #endregion

        #region Leviathan lunge
        public float LeviathanLunge_ChargeRadiusExtra {
            get {
                if (movementTarget.X < 0)
                    return 35;
                else
                    return 120;
            }
        }
        public float LeviathanLunge_BelowPlayerDistance => 1200f;
        public Vector2 LeviathanLunge_IdealRotationAxis => target.Center + Vector2.UnitY * LeviathanLunge_BelowPlayerDistance;

        public ref float SideLungeTimePower => ref ExtraMemory;

        public bool LeviathanLungeAttack(bool inGround, bool onlyInsidePlatforms, ref float rotationAmount, ref float rotationFade, ref float velocityWiggle, ref float velocityWiggleFrequency)
        {
            //https://media.discordapp.net/attachments/802291445360623686/1041107448573988984/image.png
            if (SubState == ActionState.LeviathanLunge)
            {
                if (!GetRandomTarget(160f * 16))
                    return true;
                rotationAxis = Vector2.Zero;
                movementTarget = Vector2.Zero;

                ExtraMemory = Main.rand.NextBool() ? 1 : -1;
                SubState = ActionState.LeviathanLunge_Position;

                if (!inGround || onlyInsidePlatforms)
                    SubState = ActionState.LeviathanLunge_TransitionIntoAttack;
                else
                {
                    float soundVolume = (float)Math.Pow(Utils.GetLerpValue(3000f, 1200f, NPC.Distance(Main.LocalPlayer.Center), true), 0.5f);
                    SoundEngine.PlaySound((ExtraMemory > 0 ? SideLungeRightTelegraph : SideLungeLeftTelegraph) with { Volume = soundVolume }, Main.LocalPlayer.Center);
                }
            }

            //Continue movement until floor
            if (SubState == ActionState.LeviathanLunge_TransitionIntoAttack)
            {
                BasicSimulateMovement(30f);
                if ((inGround && !onlyInsidePlatforms))
                {
                    SubState = ActionState.LeviathanLunge_Position;
                    float soundVolume = (float)Math.Pow(Utils.GetLerpValue(3000f, 1200f, NPC.Distance(Main.LocalPlayer.Center), true), 0.5f);
                    SoundEngine.PlaySound((ExtraMemory > 0 ? SideLungeRightTelegraph : SideLungeLeftTelegraph) with { Volume = soundVolume }, Main.LocalPlayer.Center);
                }
            }

            if (SubState == ActionState.LeviathanLunge_Position)
            {
                //Sink down below
                if (movementTarget == Vector2.Zero)
                {
                    velocityWiggle = 0.2f;
                    rotationFade = 45;
                    NPC.velocity.X *= 0.8f;
                    NPC.velocity.Y += 0.6f;
                    if (NPC.velocity.Y > 40)
                        NPC.velocity.Y = 40;

                    if (NPC.Center.Y > target.Center.Y + LeviathanLunge_BelowPlayerDistance + 500)
                    {
                        rotationAxis = LeviathanLunge_IdealRotationAxis;
                        movementTarget = Vector2.UnitX * ExtraMemory;
                        AttackTimer = 20;
                        NPC.velocity *= 0.6f;
                    }
                }

                //Go towards position around rotation axis
                else
                {
                    AttackTimer--;
                    rotationAxis = LeviathanLunge_IdealRotationAxis;
                    GoTowardsRadial(rotationAxis + movementTarget.RotatedBy(MathHelper.PiOver4 * 0.2f * -movementTarget.X.NonZeroSign()) * (LeviathanLunge_BelowPlayerDistance + LeviathanLunge_ChargeRadiusExtra), rotationAxis, AttackTimer);

                    if (AttackTimer <= 0)
                    {
                        AttackTimer = 0;
                        SubState = ActionState.LeviathanLunge_Anticipation;
                        NPC.velocity = Vector2.Zero;
                    }
                }
            }

            if (SubState == ActionState.LeviathanLunge_Anticipation)
            {
                rotationFade = 15f;
                rotationAmount = 0.03f;
                ForceRotation = true;

                float rotAxisSnapTimeX = 0.95f; //time during which the rotation axis can snap to theideal target
                float rotAxisSnapTimeY = 0.5f;

                if (AttackTimer < rotAxisSnapTimeY)
                    rotationAxis.Y = LeviathanLunge_IdealRotationAxis.Y;
                else
                    rotationAxis.Y = MathHelper.Lerp(rotationAxis.Y, LeviathanLunge_IdealRotationAxis.Y, 1 - ((AttackTimer - rotAxisSnapTimeY) / (1 - rotAxisSnapTimeY)));

                if (AttackTimer < rotAxisSnapTimeX)
                    rotationAxis.X = LeviathanLunge_IdealRotationAxis.X;
                else
                    rotationAxis.X = MathHelper.Lerp(rotationAxis.X, LeviathanLunge_IdealRotationAxis.X, 1 - ((AttackTimer - rotAxisSnapTimeX) / (1 - rotAxisSnapTimeX)));



                NPC.rotation = NPC.rotation.AngleTowards(NPC.AngleTo(rotationAxis + movementTarget.RotatedBy((MathHelper.PiOver4 * 0.3f) * -movementTarget.X.NonZeroSign()) * (LeviathanLunge_BelowPlayerDistance + LeviathanLunge_ChargeRadiusExtra)), 0.06f);

                float telegraphTime = 0.9f - DifficultyScale * 0.16f;
                if (movementTarget.X >= 0)
                    telegraphTime -= 0.4f;
                telegraphTime = Math.Max(telegraphTime, 0.3f);

                telegraphTime = Math.Max(0.2f, telegraphTime);
                AttackTimer += 1 / (60f * telegraphTime);
                NPC.Center = rotationAxis + movementTarget.RotatedBy(MathHelper.PiOver4 * 0.2f * -movementTarget.X.NonZeroSign()) * (LeviathanLunge_BelowPlayerDistance + LeviathanLunge_ChargeRadiusExtra);

                CameraManager.Shake = Math.Max(CameraManager.Shake, AttackTimer * 9f);
                if (NPC.soundDelay > 26 - 20 * AttackTimer)
                    NPC.soundDelay = (int)(26 - 20 * AttackTimer);

                float dustRotation = movementTarget.X < 0 ? 0.7f : 0.3f; //Less random rotation when doing the high lunge
                float dustBias = movementTarget.X < 0 ? -0.2f : 0f;
                float dustSpeed = 16f * (float)Math.Pow(AttackTimer, 2f);
                if (movementTarget.X >= 0)
                    dustSpeed += 20f;

                TelegraphSandCirleTrajectory(rotationAxis, LeviathanLunge_BelowPlayerDistance + LeviathanLunge_ChargeRadiusExtra, 150f, dustSpeed, 0.1f * AttackTimer, 100, dustBias, dustRandomRotation: dustRotation);

                if (AttackTimer >= 1)
                {
                    AttackTimer = 0;
                    SubState = ActionState.LeviathanLunge_Lunge;
                    NPC.velocity = Vector2.Zero;
                    CameraManager.Shake += 15;
                    AttackTimer = 0f;
                    playedSound = false;

                    sideLungeRoarSlot = SoundEngine.PlaySound(SideLungeDashRoar, NPC.Center);

                    SideLungeTimePower = SecondPhase ? 1f : 1.3f;
                }
            }

            if (SubState == ActionState.LeviathanLunge_Lunge)
            {
                rotationFade = 1000f;
                bool halfwayDone = (NPC.Center.X - rotationAxis.X).NonZeroSign() != movementTarget.X.NonZeroSign();

                if (inGround && !halfwayDone)
                {
                    TelegraphSandCirleTrajectory(rotationAxis, LeviathanLunge_BelowPlayerDistance + LeviathanLunge_ChargeRadiusExtra, 150f, 16f, 0.1f, 100, dustRandomRotation: 0.3f);
                    CameraManager.Shake = Math.Max(CameraManager.Shake, 5f);
                }

                if ((CanPlayEmergeSound(inGround, onlyInsidePlatforms) || AttackTimer > 0.25f) && !halfwayDone && !playedSound)
                {
                    for (int i = 0; i < 22; i++)
                    {
                        Vector2 normalizedVelocity = NPC.velocity.SafeNormalize(Vector2.Zero);
                        float displace = Main.rand.NextFloat(-50f, 50f);
                        Vector2 dustPosition = NPC.Center + normalizedVelocity.RotatedBy(MathHelper.PiOver2) * displace;

                        Dust dus = Dust.NewDustPerfect(dustPosition, DustID.Sand, normalizedVelocity * 10f * Main.rand.NextFloat(0.5f, 1.2f), 0);
                        dus.noGravity = false;
                        dus.velocity = dus.velocity.RotatedByRandom(0.2f);
                        dus.scale = Main.rand.NextFloat(0.6f, 2f) * (1 - (Math.Abs(displace) / 50f));
                    }

                    ManageSideLungeRoarSound(inGround, onlyInsidePlatforms);
                    playedSound = true;
                }

                float jumpProgress = (float)Math.Pow(AttackTimer, SideLungeTimePower);

                float attacktime = 2f;
                if (SecondPhase)
                    attacktime -= 0.75f;
                attacktime = Math.Max(attacktime, 1.6f);

                Vector2 goalPosition = rotationAxis + movementTarget.RotatedBy((MathHelper.PiOver4 * 0.2f + MathHelper.Pi * jumpProgress) * -movementTarget.X.NonZeroSign()) * (LeviathanLunge_BelowPlayerDistance + LeviathanLunge_ChargeRadiusExtra);
                NPC.velocity = goalPosition - NPC.Center;

                AttackTimer += 1 / (60f * attacktime);
                if (AttackTimer >= 1)
                    return true;
            }

            if (target.Distance(NPC.Center) > 200 * 16)
                return true;

            return false;
        }

        public void TelegraphSandCirleTrajectory(Vector2 circleCenter, float circleRadius, float telegraphWidth, float dustSpeed, float dustProbability, int steps, float? dustAngleBias = null, float dustRandomRotation = MathHelper.PiOver4)
        {
            int x = (int)(circleCenter.X / 16);
            int y = (int)(circleCenter.Y / 16);
            int halfWidth = (int)(telegraphWidth / 32);

            int side = Vector2.Dot(Vector2.UnitX, circleCenter - NPC.Center).NonZeroSign();
            float ogRotation = (NPC.Center - circleCenter).ToRotation();
            float angleChange = -(-MathHelper.PiOver2).AngleBetween(ogRotation);
            Vector2 ogDirection = (circleCenter - NPC.Center).SafeNormalize(Vector2.Zero);


            for (int i = -halfWidth; i < halfWidth; i++)
            {
                int lastTileType = -1;
                int lastTileX = -1;
                int lastTileY = -1;

                for (int j = 0; j < steps; j++)
                {
                    float rotationAtPoint = ogRotation + angleChange * j / (float)steps;
                    Vector2 tilePos = circleCenter + (rotationAtPoint.ToRotationVector2() * circleRadius);

                    Point tilePoint = (tilePos + new Vector2(i * 16f, 0).RotatedBy(rotationAtPoint)).ToSafeTileCoordinates();
                    Tile tile = Framing.GetTileSafely(tilePoint);

                    if ((!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || TileID.Sets.Platforms[tile.TileType]) && (tile.WallType == 0 || Main.wallHouse[tile.WallType]))
                    {
                        if (lastTileType == -1)
                            continue;

                        float sideness = (1 - Math.Abs(i) / (float)halfWidth);
                        float probability = dustProbability;

                        for (int d = 0; d < 10; d++)
                        {
                            if (Main.rand.NextFloat() < probability * (float)Math.Pow(sideness, 0.5f))
                            {
                                Vector2 dustPos = tilePoint.ToVector2() * 16f;
                                dustPos += Vector2.UnitX * Main.rand.NextFloat(16f) + Vector2.UnitY * 16f;

                                Dust floorDust = Main.dust[WorldGen.KillTile_MakeTileDust(lastTileX, lastTileY, Main.tile[lastTileX, lastTileY])];
                                floorDust.position = dustPos;
                                floorDust.velocity = -Vector2.UnitY * dustSpeed * Main.rand.NextFloat(0.5f, 1.2f);
                                floorDust.noGravity = false;
                                float dustVelocityRotation = rotationAtPoint - MathHelper.PiOver2 * -side;
                                if (dustAngleBias.HasValue)
                                    dustVelocityRotation = dustVelocityRotation.AngleTowards(dustAngleBias.Value, 0.3f);
                                floorDust.velocity = dustVelocityRotation.ToRotationVector2() * dustSpeed * Main.rand.NextFloat(0.5f, 1.2f);
                                floorDust.velocity = floorDust.velocity.RotatedByRandom(dustRandomRotation);
                                floorDust.scale = Main.rand.NextFloat(0.6f, 2f) * sideness;

                                if (Main.rand.NextBool(3))
                                    floorDust.type = DustID.SpelunkerGlowstickSparkle;

                                /*
                                Dust dus = Dust.NewDustPerfect(dustPos, DustID.Sand, -Vector2.UnitY * dustSpeed * Main.rand.NextFloat(0.5f, 1.2f), 0);
                                dus.noGravity = false;
                                float dustVelocityRotation = rotationAtPoint - MathHelper.PiOver2 * -side;
                                if (dustAngleBias.HasValue)
                                    dustVelocityRotation = dustVelocityRotation.AngleTowards(dustAngleBias.Value, 0.3f);

                                dus.velocity = dustVelocityRotation.ToRotationVector2() * dustSpeed * Main.rand.NextFloat(0.5f, 1.2f);
                                dus.velocity = dus.velocity.RotatedByRandom(dustRandomRotation);
                                dus.scale = Main.rand.NextFloat(0.6f, 2f) * sideness;
                                */
                                //dus.velocity *= 0.2f + 0.8f * (1 - j / (float)steps);
                                //dus.scale *= 0.2f + 0.8f * (1 - j / (float)steps);
                            }
                        }
                        break;
                    }

                    if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                    {
                        lastTileType = tile.TileType;
                        lastTileX = tilePoint.X;
                        lastTileY = tilePoint.Y;
                    }
                }

            }
        }

        #endregion

        #region Prey Belch
        public bool PreyBelchAttack(bool inGround, bool onlyInsidePlatforms, ref float rotationAmount, ref float rotationFade, ref float velocityWiggle, ref float velocityWiggleFrequency)
        {
            if (inGround && !onlyInsidePlatforms)
                TelegraphSand(150f, 1f, 0.08f, maxSurfaceDistance: 70, noFalloff: false);

            if (SubState == ActionState.PreyBelch)
            {
                AttackTimer = 0;
                SubState = ActionState.PreyBelch_Position;
                if (!inGround || onlyInsidePlatforms)
                    SubState = ActionState.PreyBelch_TransitionIntoAttack;

                //Commented out, probably don't telegraph the fact you just chose to do the attack?
                //SoundEngine.PlaySound(WooshSound, target.Center);
                if (Math.Abs(NPC.velocity.X) < 3f)
                    NPC.velocity.X = NPC.velocity.X.NonZeroSign() * 3f;
            }

            if (SubState == ActionState.PreyBelch_TransitionIntoAttack)
            {
                NPC.TargetClosest(false);
                BasicSimulateMovement(50f);
                if ((inGround && !onlyInsidePlatforms))
                    SubState = ActionState.PreyBelch_Position;
            }

            else if (SubState == ActionState.PreyBelch_Position)
            {
                AttackTimer += 1 / (60f * 6f);
                velocityWiggle = 0;
                rotationAmount = 0;
                CirclePlayer(500, 40, inGround, onlyInsidePlatforms, 0f);

                mandibleJerkiness = MathHelper.Lerp(mandibleJerkiness, -0.12f, 0.2f);
                bool startTheAttack = Vector2.Dot(-Vector2.UnitY, NPC.velocity) > 0.7f;

                if ((!inGround && NPC.Center.Y < target.Center.Y + 200 && startTheAttack) || AttackTimer > 1)
                {
                    AttackTimer = 0;
                    SubState = ActionState.PreyBelch_Telegraph;
                    NPC.velocity *= 0.7f;
                    NPC.velocity = NPC.velocity.ToRotation().AngleTowards(NPC.AngleTo(target.Center), 0.02f).ToRotationVector2() * NPC.velocity.Length();
                    movementTarget = target.Center;

                    gurgleSoudnSlot = SoundEngine.PlaySound(PreyBelchBurpSound, NPC.Center);
                }
            }

            else if (SubState == ActionState.PreyBelch_Telegraph)
            {
                if (SoundEngine.TryGetActiveSound(gurgleSoudnSlot, out var glurglurg))
                {
                    glurglurg.Position = NPC.Center;
                    if (AttackTimer > 0.8f)
                        glurglurg.Volume = 1f - (AttackTimer - 0.8f) / 0.2f;
                }

                velocityWiggle = 0;
                rotationAmount = 0;
                NPC.velocity *= 0.98f;
                NPC.velocity.Y *= 0.98f;
                NPC.velocity = NPC.velocity.ToRotation().AngleLerp(NPC.AngleTo(target.Center - Vector2.UnitY * 400f), 0.02f).ToRotationVector2() * NPC.velocity.Length();
                float telegraphTime = 0.8f;
                AttackTimer += 1 / (60f * telegraphTime);

                mandibleJerkiness = -0.1f + 0.5f * (float)Math.Pow(AttackTimer, 0.7f);

                if (AttackTimer < 0.5f)
                    movementTarget = target.Center;
                else
                    movementTarget = Vector2.Lerp(movementTarget, target.Center, 0.4f * (1f - (AttackTimer - 0.5f) / 0.5f));

                if (AttackTimer >= 1f)
                {
                    mandibleJerkiness = 0.9f;
                    NPC.velocity = -NPC.velocity.SafeNormalize(NPC.DirectionTo(target.Center)) * 8f;
                    NPC.velocity += Vector2.UnitY * -3f;
                    AntiGravityCharge = 0f;
                    SoundEngine.PlaySound(PreyBelchVomitSound, NPC.Center);
                    AttackTimer = 0f;
                    SubState = ActionState.PreyBelch_BackInTheGround;

                    if (glurglurg != null)
                        glurglurg.Stop();

                    gurgleSoudnSlot = ReLogic.Utilities.SlotId.Invalid;


                    int projectileCount = 7 + (int)DifficultyScale;
                    int stormlionCount = 1 + (int)DifficultyScale;
                    bool needsToFireMeatChunk = SecondPhase;

                    //Stormlion chunks are expert only
                    if (DifficultyScale < 2)
                        stormlionCount = 0;

                    //https://media.discordapp.net/attachments/802291445360623686/1042881965877116928/image.png
                    float areaWidth = 1000f;
                    Vector2 areaCenter = movementTarget + (Vector2.UnitX * 50f * (movementTarget.X - NPC.Center.X).NonZeroSign());

                    int segmentDivisons = (int)Math.Ceiling(projectileCount / 2f); //x
                    float segmentWidth = areaWidth / (float)segmentDivisons; //Segment
                    float halfWidth = (segmentDivisons / 2f - 0.5f) / (float)segmentDivisons * areaWidth; //Lerp

                    for (int i = 0; i < projectileCount; i++)
                    {
                        Vector2 fireTarget = areaCenter + Vector2.UnitX * MathHelper.Lerp(-halfWidth, halfWidth, i / (float)(projectileCount - 1f)) + Vector2.UnitY * Main.rand.NextFloat(-50f, 20f);
                        fireTarget += Vector2.UnitX * Main.rand.NextFloat(-segmentWidth / 2f, segmentWidth / 2f);

                        if (i == projectileCount / 2)
                            fireTarget = movementTarget;

                        Vector2 burbVel = FablesUtils.GetArcVel(NPC.Center, fireTarget, 0.2f, 130f, 300f, 20f);
                        if (i != projectileCount / 2)
                            burbVel = burbVel.RotatedByRandom(MathHelper.PiOver4 * 0.5f) * Main.rand.NextFloat(1f, 1.2f);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (needsToFireMeatChunk && (Main.rand.NextBool(5) || i == projectileCount - 1))
                            {
                                NPC meatball = NPC.NewNPCDirect(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<GroundBeef>(), NPC.whoAmI);
                                meatball.velocity = burbVel;

                                needsToFireMeatChunk = false;
                                continue;
                            }

                            int projType = ModContent.ProjectileType<AntlionChunk>();
                            int variant = Main.rand.Next(8);
                            int damage = PreyBelch_AntlionChunkDamage / 2;

                            if (Main.rand.NextBool(5) && stormlionCount > 0)
                            {
                                projType = ModContent.ProjectileType<StormlionChunk>();
                                variant = Main.rand.Next(4);
                                damage = PreyBelch_StormlionChunkDamage / 2;
                                stormlionCount--;
                            }

                            Projectile proj = Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), NPC.Center, burbVel, projType, damage, 4, Main.myPlayer, variant);
                            proj.friendly = false;
                            proj.hostile = true;
                            proj.timeLeft *= 3;
                        }

                        int goreType = Main.rand.Next(1094, 1104);
                        Gore gore = Gore.NewGoreDirect(NPC.GetSource_FromAI(), NPC.Center, burbVel * Main.rand.NextFloat(0.1f, 0.4f), goreType);
                        gore.timeLeft = 6;
                    }
                }
            }

            else if (SubState == ActionState.PreyBelch_BackInTheGround)
            {
                rotationAmount = AttackTimer * 0.03f;
                AntiGravityCharge = 0f;
                CirclePlayer(400, 30f, inGround, onlyInsidePlatforms);
                if (NPC.velocity.Y > 6f + 6f * AttackTimer)
                    NPC.velocity.Y = 6f + 6f * AttackTimer;

                AttackTimer += 1 / (60f * 0.5f);

                if (AttackTimer >= 1f)
                    return true;
            }

            if (target.Distance(NPC.Center) > 200 * 16)
                return true;

            return false;
        }
        #endregion

        #region Electro Lunge
        public ref float PlatformElectrificationCooldown => ref ExtraMemory;

        public bool FindJumpEndPosition(float gravity, out Vector2 jumpEnd)
        {
            jumpEnd = Vector2.Zero;
            Vector2 simulatedPosition = NPC.Center;
            Vector2 arcVel = NPC.velocity;

            for (int i = 0; i < 70; i++)
            {
                if (simulatedPosition.Y <= target.Center.Y && simulatedPosition.Y + arcVel.Y > target.Center.Y)
                {
                    jumpEnd = new Vector2(simulatedPosition.X, target.Center.Y);
                    return true;
                }
                simulatedPosition += arcVel;
                arcVel += Vector2.UnitY * gravity;
            }

            return false;
        }

        public bool ElectroLungeAttack(bool inGround, bool onlyInsidePlatforms, ref float rotationAmount, ref float rotationFade, ref float velocityWiggle, ref float velocityWiggleFrequency)
        {
            if (inGround && !onlyInsidePlatforms)
                TelegraphSand(150f, 1f, 0.08f, maxSurfaceDistance: 70, noFalloff: false);

            if (SubState == ActionState.ElectroLunge)
            {
                electroLoopSlot = ReLogic.Utilities.SlotId.Invalid;

                if (!GetRandomTarget(160f * 16))
                    return true;
                SubState = ActionState.ElectroLunge_Position;
                if (!inGround || onlyInsidePlatforms)
                    SubState = ActionState.ElectroLunge_TransitionIntoAttack;

                SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap, target.Center);
                if (Math.Abs(NPC.velocity.X) < 3f)
                    NPC.velocity.X = NPC.velocity.X.NonZeroSign() * 3f;
            }

            //Continue movement until we are inside the floor
            if (SubState == ActionState.ElectroLunge_TransitionIntoAttack)
            {
                BasicSimulateMovement(50f);
                if ((inGround && !onlyInsidePlatforms))
                    SubState = ActionState.ElectroLunge_Position;
            }

            //Continue movement until peeking out
            else if (SubState == ActionState.ElectroLunge_Position)
            {
                velocityWiggle = 0;
                rotationAmount = 0;
                CirclePlayer(400, Math.Max(30, 40 - DifficultyScale * 5f), inGround, onlyInsidePlatforms, 0f);

                bool startTheAttack = NPC.Center.Y < target.Center.Y + 50 || Vector2.Dot(-Vector2.UnitY, NPC.velocity) > 0.7f;

                if (!inGround && NPC.Center.Y < target.Center.Y + 600 && startTheAttack)
                {
                    AttackTimer = 0;
                    SubState = ActionState.ElectroLunge_Peek;
                    NPC.velocity = NPC.velocity.SafeNormalize(-Vector2.UnitY) * 5f;
                    movementTarget = target.Center;
                    SoundEngine.PlaySound(ElectroChargeSound, NPC.Center);
                }
            }

            else if (SubState == ActionState.ElectroLunge_Peek)
            {
                velocityWiggle = 0;
                rotationAmount = 0;

                NPC.velocity *= 0.98f;
                NPC.velocity.Y *= 0.98f;

                float dotToUpright = Vector2.Dot(NPC.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitY);
                bool tiltedTowardsPlayer = 0 < Vector2.Dot(NPC.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitX * (target.Center.X - NPC.Center.X).NonZeroSign());
                float tiltUp = tiltedTowardsPlayer ? 0.1f : 0.6f;

                float maxAngleChange = 0.02f + tiltUp * Utils.GetLerpValue(-1f, -0.7f, dotToUpright, true);

                NPC.velocity = NPC.velocity.ToRotation().AngleLerp(-MathHelper.PiOver2, maxAngleChange).ToRotationVector2() * NPC.velocity.Length(); //Tilt towards facing up
                float telegraphTime = 1.2f;
                AttackTimer += 1 / (60f * telegraphTime);

                //OPEN the MANDIBLES!
                mandibleJerkiness = 0.6f * FablesUtils.SineInOutEasing(AttackTimer);

                //Retarget the player (at the start fully retargets but then the retarget gets less and less accurate
                float guaranteedRepositionPercent = Math.Min(1f, 0.8f);
                if (AttackTimer < guaranteedRepositionPercent)
                    movementTarget = target.Center;
                else
                    movementTarget = Vector2.Lerp(movementTarget, target.Center, 0.4f * (1f - (AttackTimer - guaranteedRepositionPercent) / (1 - guaranteedRepositionPercent)));


                if (AttackTimer >= 1f)
                {
                    AttackTimer = 0f;
                    SubState = ActionState.ElectroLunge_Backpedal;
                    NPC.velocity = NPC.velocity.SafeNormalize(-Vector2.UnitY) * -4f;
                    electroLoopSlot = SoundEngine.PlaySound(ElectroLoopSound, NPC.Center);
                }
            }

            else if (SubState == ActionState.ElectroLunge_Backpedal)
            {
                mandibleJerkiness = 0.6f;
                velocityWiggle = 0;
                AttackTimer += 1 / (60f * 0.15f);
                NPC.velocity *= 1f - AttackTimer * 0.1f;

                if (AttackTimer >= 1f)
                {
                    AttackTimer = 0f;
                    SubState = ActionState.ElectroLunge_Jump;
                    PlatformElectrificationCooldown = 0;
                    float maxForwardness = Math.Max(0f, 400f - 75f * DifficultyScale);
                    Vector2 jumpTarget = target.Center + Vector2.UnitX * (target.Center.X - NPC.Center.X).NonZeroSign() * Main.rand.NextFloat(0f, maxForwardness);

                    jumpTarget += Vector2.UnitX * Math.Clamp(target.velocity.X * 5f, -90f, 90f);

                    NPC.velocity = FablesUtils.GetArcVel(NPC.Center, jumpTarget, 0.6f, 400f, 400f, 30f, 200f);
                    SoundEngine.PlaySound(ElectroJumpSound, NPC.Center);
                }
            }

            else if (SubState == ActionState.ElectroLunge_Jump)
            {
                velocityWiggle = 0f;
                AttackTimer += 1 / (60f);

                mandibleJerkiness = 0.6f;
                NPC.velocity.Y += 0.6f;


                //REcalculate a bit. This calculates the arc of the jump and then adjusts the horizontal speed more or less to make DS land right on the player
                float recalculationTime = 0.7f + DifficultyScale * 0.25f;
                if (AttackTimer < recalculationTime && FindJumpEndPosition(0.6f, out Vector2 jumpEnd))
                {
                    float acceleratePower = 1 - Math.Clamp(AttackTimer / (recalculationTime - 0.2f), 0f, 1f);
                    float deceleratePower = 1 - AttackTimer / recalculationTime;

                    if (!FablesUtils.ArePointsInOrder(NPC.Center.X, target.Center.X, jumpEnd.X))
                        NPC.velocity.X += (target.Center.X - NPC.Center.X).NonZeroSign() * 0.2f * (float)Math.Pow(acceleratePower, 0.25f);
                    else
                        NPC.velocity.X += (NPC.Center.X - target.Center.X).NonZeroSign() * 0.6f * (float)Math.Pow(deceleratePower, 0.5f);
                }

                float maxSpeed = 12 + 2f * DifficultyScale;
                NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -maxSpeed, maxSpeed);


                PlatformElectrificationCooldown--;
                if (PlatformElectrificationCooldown <= 0 && DifficultyScale > 0)
                {
                    Point platformCollisionPoint = GetPlatformCollisionPoint();
                    if (platformCollisionPoint != Point.Zero)
                    {
                        SoundEngine.PlaySound(SoundID.AbigailAttack, NPC.Center);
                        SoundEngine.PlaySound(PlatformElectrificationSound with { Volume = 0.8f }, NPC.Center);
                        PlatformElectrificationCooldown = 20;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 projCenter = platformCollisionPoint.ToWorldCoordinates();
                            int projType = ModContent.ProjectileType<PlatformElectrification>();
                            for (int i = -1; i <= 1; i += 2)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), projCenter, Vector2.UnitX * i, projType, ElectroLunge_PlatformElectrificationDamage / 2, 0, Main.myPlayer);
                            }
                        }
                    }
                }

                bool canBlast = inGround && !onlyInsidePlatforms && NPC.Center.Y > target.Center.Y && NPC.velocity.Y >= 0;
                if (!canBlast && NPC.velocity.Y > 0)
                {
                    Rectangle nextHitbox = NPC.Hitbox;
                    nextHitbox.X += (int)NPC.velocity.X;
                    nextHitbox.Y += (int)NPC.velocity.Y;

                    for (int i = 0; i < 200; i++)
                    {
                        Player player = Main.player[i];
                        int worthless = 0;
                        if (player.active && !player.dead && nextHitbox.Intersects(player.Hitbox) && NPCLoader.CanHitPlayer(NPC, player, ref worthless) && PlayerLoader.CanBeHitByNPC(player, NPC, ref worthless))
                        {
                            canBlast = true;
                            break;
                        }
                    }
                }

                if (canBlast)
                {
                    ExplodeElectricCharge();
                    NPC.velocity.Y += 13f;
                    SubState = ActionState.ElectroLunge_BurrowBack;
                    AttackTimer = 0;
                }
            }

            if (SubState == ActionState.ElectroLunge_BurrowBack)
            {
                electroLoopSlot = ReLogic.Utilities.SlotId.Invalid;
                AttackTimer += 1 / (60f * 0.45f);
                BasicSimulateMovement(50f);
                if ((inGround && !onlyInsidePlatforms) && AttackTimer > 1f)
                    return true;
            }

            //DUST VISUALS
            if ((int)SubState >= (int)ActionState.ElectroLunge_Peek)
            {
                Vector2 ballCenter = NPC.Center + NPC.rotation.ToRotationVector2() * 80f;

                //Add light
                float lightMult = 1f;
                if (SubState == ActionState.ElectroLunge_Peek)
                    lightMult = AttackTimer;

                Lighting.AddLight(ballCenter, new Vector3(20, 20, 70) * 0.02f * lightMult);

                if (Main.rand.NextBool(8) && SubState != ActionState.ElectroLunge_Peek)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 dustPos = ballCenter + Main.rand.NextVector2Circular(50f, 50f);
                        int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();

                        Dust dus = Dust.NewDustPerfect(dustPos, dusType, (dustPos - ballCenter) * 0.1f, 30);
                        dus.noGravity = true;
                        dus.scale = Main.rand.NextFloat(0.8f, 1.2f);

                        dus.velocity = (dustPos - ballCenter).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.5f, 2f) + NPC.velocity;

                        dus.customData = Color.RoyalBlue;
                    }
                }

                if (SubState == ActionState.ElectroLunge_Jump && Main.rand.NextBool(3))
                {
                    float segmentForwardness = (1 - (float)Math.Pow(Main.rand.NextFloat(0f, 1f), 0.5f));
                    int segmentIndex = (int)(SegmentCount * (0.7f * segmentForwardness));
                    segmentIndex = Math.Clamp(segmentIndex, 0, SegmentCount - 2);
                    Vector2 segmentDirection = (SegmentPosition(segmentIndex + 1) - SegmentPosition(segmentIndex)).SafeNormalize(Vector2.Zero);
                    Vector2 dustPosition = SegmentPosition(segmentIndex) + segmentDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-25f, 25f);
                    Vector2 dustDirection = segmentDirection * Main.rand.NextFloat(1f, 5f);

                    for (int i = 0; i < 3; i++)
                    {
                        Dust d = Dust.NewDustPerfect(dustPosition + dustDirection * i * 3f, ModContent.DustType<ElectroDust>(), dustDirection);
                        d.noGravity = true;
                        d.alpha = 90;
                        d.scale = Main.rand.NextFloat(0.1f, 0.4f) + (1 - segmentForwardness) * 0.2f;
                    }
                }
            }

            if (electroLoopSlot != ReLogic.Utilities.SlotId.Invalid)
            {
                SoundHandler.TrackSound(electroLoopSlot);
                if (SoundEngine.TryGetActiveSound(electroLoopSlot, out var electroLoopSound))
                    electroLoopSound.Position = NPC.Center;
                else
                    electroLoopSlot = SoundEngine.PlaySound(ElectroLoopSound, NPC.Center);
            }

            if (target.Distance(NPC.Center) > 200 * 16)
                return true;

            return false;
        }

        public Point GetPlatformCollisionPoint()
        {
            Vector2 start = NPC.Center;
            Vector2 end = NPC.Center + NPC.rotation.ToRotationVector2() * 90f;

            float distance = (start - end).Length() / 8f;

            for (int i = 0; i < distance; i++)
            {
                Vector2 worldPos = Vector2.Lerp(start, end, i / distance);
                Point tilePos = worldPos.ToSafeTileCoordinates();

                Tile tile = Main.tile[tilePos];
                if (!tile.HasUnactuatedTile || (!TileID.Sets.Platforms[tile.TileType] && tile.TileType != TileID.PlanterBox))
                {
                    if (tile.LiquidAmount > 0)
                    {
                        Tile nextTile = Main.tile[tilePos + new Point(0, -1)];
                        if (nextTile.LiquidAmount <= 0)
                            return tilePos;
                    }

                    if (i >= 0)
                        i++;
                    continue;
                }

                return tilePos;
            }

            return Point.Zero;
        }

        public void ExplodeElectricCharge()
        {
            SoundEngine.PlaySound(ElectroBlastSound, NPC.Center);

            if (Main.LocalPlayer.Distance(NPC.Center) < 1200 && CameraManager.Shake < 40f)
                CameraManager.Shake += 25;

            if (Main.IsItStorming)
                Main.NewLightning();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int blastDiameter = 350 + (int)(40 * DifficultyScale);
                blastDiameter = Math.Min(450, blastDiameter);

                Projectile proj = Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DesertScourgeElectroblast>(), ElectroLunge_FinalBlastDamage / 2, 10, Main.myPlayer, ai2 : blastDiameter  / 2f);
            }

            if (SoundEngine.TryGetActiveSound(electroLoopSlot, out var sound))
                sound.Stop();

            AntiGravityCharge = 0;
        }
        #endregion

        #region Chungry Lunge
        public ref Vector2 LungeGravity => ref rotationAxis;

        public NPC FindTargetOfMyChunger(out bool targetFound)
        {
            NPC targetOfMyChunger = null;

            //If invalid npc slot, break
            if (ExtraMemory < 0 || ExtraMemory >= Main.maxNPCs)
                targetFound = false;

            else
            {
                targetFound = true;
                int[] chungryMeatTypes = new int[] { ModContent.NPCType<GroundBeef>(), ModContent.NPCType<ScourgeFlesh>() };
                targetOfMyChunger = Main.npc[(int)ExtraMemory];

                //if the chungry meat got killed
                if (!targetOfMyChunger.active || !chungryMeatTypes.Contains(targetOfMyChunger.type))
                    targetFound = false;
            }

            if (targetFound)
            {
                if (SubState != ActionState.ChungryLunge_Lunge)
                    movementTarget = targetOfMyChunger.Center;
            }
            else
                ExtraMemory = -1;

            return targetOfMyChunger;
        }

        public bool ChungryLungeAttack(bool inGround, bool onlyInsidePlatforms, ref float rotationAmount, ref float rotationFade, ref float velocityWiggle, ref float velocityWiggleFrequency)
        {
            //https://media.discordapp.net/attachments/802291445360623686/1042585411270230017/image.png
            if (SubState == ActionState.ChungryLunge)
            {
                AttackTimer = 0f;
                SubState = ActionState.ChungryLunge_Reposition;
                if (!inGround || onlyInsidePlatforms)
                    SubState = ActionState.ChungryLunge_TransitionIntoAttack;
                else if (!GetRandomTarget(160f * 16))
                    return true;
            }

            NPC targetOfMyChunger = FindTargetOfMyChunger(out bool targetFound);
            if (!targetFound && (int)SubState <= (int)ActionState.ChungryLunge_Anticipation)
                return true;

            float ballisticsMinimumHeight = 400f;
            float ballisticsFallSpeed = 1.2f;
            float ballisticsRecalculationDuration = 0.5f;
            Vector2 ballisticsTarget = target.Center + target.velocity * 25f;
            Vector2 ballisticsJumpTarget = target.Center + target.velocity * Main.rand.NextFloat(20f, 35f);

            if (SubState == ActionState.ChungryLunge_TransitionIntoAttack)
            {
                BasicSimulateMovement(40f);
                if ((inGround && !onlyInsidePlatforms))
                {
                    SubState = ActionState.ChungryLunge_Reposition;
                    if (!GetRandomTarget(160f * 16))
                        return true;
                }
            }

            else if (SubState == ActionState.ChungryLunge_Reposition)
            {
                rotationFade = 33f;

                GoTowardsRadial(movementTarget + Vector2.UnitY * 1300f, movementTarget, 40f * (1 - AttackTimer));

                if (CanPlayBurrowSound(inGround, onlyInsidePlatforms))
                    CameraManager.Shake += 20f;

                AttackTimer += 1 / (60f * 0.3f);

                if (AttackTimer >= 1)
                {
                    AttackTimer = 0;
                    SubState = ActionState.ChungryLunge_Anticipation;
                }
            }


            else if (SubState == ActionState.ChungryLunge_Anticipation)
            {
                rotationFade = 35f;
                rotationAmount = 0.03f;
                ForceRotation = true;
                float attackSpeed = 1 / (60f * Math.Max(0.35f, 0.7f - DifficultyScale * 0.1f));

                AttackTimer += attackSpeed;
                if (AttackTimer >= 0.2f && AttackTimer < 0.2f + attackSpeed)
                    SoundEngine.PlaySound(FastLungeTelegraphSound, NPC.Center);

                Vector2 whereIWannaGo;
                //If the player is in a 90° cone above the meat (ignore the sketch labeling it 90°),
                //DS can simply align itself with the player through the meat.
                //https://media.discordapp.net/attachments/802291445360623686/1042606347511943178/image.png
                if (Vector2.Dot(movementTarget.SafeDirectionTo(target.Center), -Vector2.UnitY) >= 0.5f)
                    whereIWannaGo = movementTarget + movementTarget.SafeDirectionFrom(target.Center + target.velocity * 1.5f) * 1000f;

                //If the player is outside of this cone its a bit more complicated to make it look correct.
                //The solution is to calculate the ballistics trajectory from the meat to the target, with a HUGE gravity so it falls down super fast
                //Then when we get that velocity, we simply just move DS further below ground to match the start of that trajectory
                //https://media.discordapp.net/attachments/802291445360623686/1042607608835604590/image.png
                else
                {
                    Vector2 ballisticLockOn = FablesUtils.GetArcVel(movementTarget, ballisticsTarget, ballisticsFallSpeed, ballisticsMinimumHeight);
                    whereIWannaGo = movementTarget - ballisticLockOn.SafeNormalize(-Vector2.UnitY) * 1000f;
                }

                GoTowardsRadial(whereIWannaGo, movementTarget, 20f - AttackTimer * 19f);
                NPC.rotation = NPC.AngleTo(movementTarget);

                if (NPC.soundDelay > 26 - 20 * AttackTimer)
                    NPC.soundDelay = (int)(26 - 20 * AttackTimer);

                CameraManager.Shake = Math.Max(CameraManager.Shake, AttackTimer * 4f);
                if (targetFound && targetOfMyChunger.ModNPC is GroundBeef beefStrogadnoff)
                    beefStrogadnoff.RumblingSandEffects(AttackTimer * 5f);

                if (AttackTimer >= 1)
                {
                    AttackTimer = 0;
                    LungeGravity = Vector2.Zero;

                    SubState = ActionState.ChungryLunge_Lunge;
                    NPC.velocity = NPC.SafeDirectionTo(movementTarget) * 55;

                    //Larger hitbox
                    NPC.Resize(NPC.width + 40, NPC.height + 40);

                    SoundEngine.PlaySound(ChungryLungeSound, NPC.Center);

                    CameraManager.Shake += 15;
                    AttackTimer = 0;
                }
            }

            else if (SubState == ActionState.ChungryLunge_Lunge)
            {
                velocityWiggle = 0f;
                rotationAmount = 0f;
                rotationFade = 35f;

                if (LungeGravity == Vector2.Zero)
                    NPC.velocity *= 0.99f;
                else
                {
                    NPC.oldVelocity = NPC.velocity;
                    NPC.velocity += LungeGravity;
                }

                if (NPC.soundDelay > 6f)
                    NPC.soundDelay = 6;

                AttackTimer += 1 / (60f * 0.6f);
                if (AttackTimer < 1)
                    CameraManager.Shake = 1;

                if (inGround && !onlyInsidePlatforms)
                    TelegraphSand(150f, 16f, 0.1f);

                if (CanPlayEmergeSound(inGround, onlyInsidePlatforms))
                    SoundEngine.PlaySound(FastLungeEmergeSound with { Volume = 0.5f });

                //Help the ballistic lunge be more accurate
                if (LungeGravity.Y == ballisticsFallSpeed)
                {
                    //REcalculate a bit
                    if (AttackTimer > 1f && AttackTimer < 1 + ballisticsRecalculationDuration)
                    {
                        Vector2 recalculatedBallistics = FablesUtils.GetArcVel(NPC.Center, ballisticsJumpTarget, ballisticsFallSpeed, ballisticsMinimumHeight);
                        float recalculationStrenght = (1 - (AttackTimer - 1f) / ballisticsRecalculationDuration);
                        recalculationStrenght = (float)Math.Pow(recalculationStrenght, 0.4f);

                        NPC.velocity = Vector2.Lerp(NPC.velocity, recalculatedBallistics, recalculationStrenght);
                    }

                    else
                    {
                        if ((target.Center.X - NPC.Center.X).NonZeroSign() == NPC.velocity.X.NonZeroSign())
                        {
                            NPC.velocity.X += NPC.velocity.X.NonZeroSign() * 0.1f;
                        }
                        else
                        {
                            NPC.velocity.X -= NPC.velocity.X.NonZeroSign() * 0.2f;
                        }
                    }
                }

                //Glurpy the slurpy
                if (targetFound && NPC.Distance(targetOfMyChunger.Center) < (60f + NPC.velocity.Length()))
                {
                    //Poison!!!!!!
                    if (targetOfMyChunger.ai[1] == 1)
                    {
                        //Poison cant kill scrouge
                        int poisonDamage = Math.Min(NPC.lifeMax / 12, NPC.life - 1);
                        NPC.life -= poisonDamage;
                        NPC.HitEffect(0, poisonDamage);

                        NPC.AddBuff(ModContent.BuffType<ScourgePoison>(), 10 * 60);
                    }

                    targetOfMyChunger.life = 0;
                    targetOfMyChunger.hide = true;
                    targetOfMyChunger.HitEffect();

                    //Extra efex!
                    if (targetOfMyChunger.ModNPC is GroundBeef beefStrogadnoff)
                        beefStrogadnoff.DevourEffects(NPC.velocity);


                    //If the player is outside a 90° cone above the meat, use ballistics<
                    if (Vector2.Dot(movementTarget.SafeDirectionTo(target.Center), -Vector2.UnitY) < 0.5f)
                    {
                        Vector2 ballisticLockOn = FablesUtils.GetArcVel(NPC.Center, ballisticsJumpTarget, ballisticsFallSpeed, ballisticsMinimumHeight);
                        NPC.velocity = ballisticLockOn;
                        LungeGravity = new Vector2(0f, ballisticsFallSpeed);
                        AttackTimer = 1f;
                    }

                    ExtraMemory = -1;
                }

                //Up high in the sky after the lunge, set the gravity to fall down
                if (NPC.Center.Y < movementTarget.Y - 460 && LungeGravity == Vector2.Zero)
                {
                    NPC.velocity *= 0.8f;
                    NPC.velocity.Y *= 0.7f;
                    LungeGravity = new Vector2(0f, 1f);
                    NPC.velocity.Y += 4;
                }

                if (NPC.velocity.Y > 0 && LungeGravity.Y == ballisticsFallSpeed)
                {
                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, (target.Center.X - NPC.Center.X) * 0.2f, 1 / 20f);
                }

                if (AttackTimer > 10f || (LungeGravity != Vector2.Zero && (target.Center.Y < NPC.Center.Y - 500)))
                {
                    NPC.velocity.Y = Math.Min(NPC.velocity.Y, 16f);

                    //Unlarge the hitbox
                    NPC.Resize(NPC.width - 40, NPC.height - 40);

                    return true;
                }
            }

            if (target.Distance(NPC.Center) > 200 * 16)
                return true;
            return false;
        }
        #endregion
    }
}
