using Microsoft.Xna.Framework.Graphics;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public partial class Crabulon : ModNPC, ICustomDeathMessages, IDrawOverTileMask
    {
        public class CrabulonArm
        {
            public Crabulon crabulon;
            public readonly Vector2 offsetFromBase;
            public readonly Vector2 tipRestingOffset;

            public Vector2 OffsetFromBase => crabulon.DrawFlip ? new Vector2(-offsetFromBase.X, offsetFromBase.Y) : offsetFromBase;
            public Vector2 TipRestingOffset => crabulon.DrawFlip ? new Vector2(-tipRestingOffset.X, tipRestingOffset.Y) : tipRestingOffset;

            public readonly float forearmLength;
            public readonly float armLenght;
            public float MaxLenght => forearmLength + armLenght;

            public readonly float forearmRotationOffset;
            public readonly float armRotationOffset;

            public bool VisuallyFlipped => !crabulon.lookingSideways ? false : !largeClaw ? NPC.direction == 1 : NPC.direction == -1;
            public float ForearmRotationOffset => VisuallyFlipped ? -forearmRotationOffset + MathHelper.Pi : forearmRotationOffset;
            public float ArmRotationOffset => VisuallyFlipped ? -armRotationOffset + MathHelper.Pi : armRotationOffset;



            public readonly Asset<Texture2D> ForearmAsset;
            public readonly Asset<Texture2D> ArmAsset;
            public readonly Asset<Texture2D> ClawAsset;


            public readonly Asset<Texture2D> ForearmBackAsset;
            public readonly Asset<Texture2D> ArmBackAsset;

            public readonly Vector2 forearmOrigin;
            public readonly Vector2 armOrigin;

            public readonly Vector2 clawOffset;
            public readonly Vector2 clawOrigin;

            public float acceleration;
            public float springiness;
            public Vector2 velocity = Vector2.Zero;
            public Vector2 armTip;
            public Vector2 elbow;
            public bool bendFlip;
            public bool largeClaw;
            public float spriteSizeMultiplier;

            public float fallTime = 0f;
            public float IdealContraction => (0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly));

            public Vector2? goalOverride = null;
            public float? contractionOverride = null;
            public float? springinessOverride = null;
            public float? accelerationOverride = null;

            public float clawClackTimer;
            public float? clawOpennessOverride = null;
            public float ClawOpenness => (clawOpennessOverride ??  (0.1f - 0.2f * (float)Math.Pow(clawClackTimer, 2f))) * (crabulon.DrawFlip ? 1 : -1);


            public float Acceleration => accelerationOverride ?? acceleration;
            public float Springiness => springinessOverride ?? springiness;

            public Vector2 Anchor {
                get {
                    if (crabulon.SubState == ActionState.Dead_InternalGoreSimulation)
                        return LimpingArm.points[0].position;

                    return crabulon.VisualCenter + OffsetFromBase.RotatedBy(crabulon.visualRotation) * NPC.scale;
                }
            }

            public Vector2 Goal => goalOverride.HasValue ? goalOverride.Value : crabulon.VisualCenter + TipRestingOffset.RotatedBy(crabulon.visualRotation) * NPC.scale * (contractionOverride ?? IdealContraction);

            public NPC NPC => crabulon.NPC;

            public CrabulonArm(Crabulon crabulon, Vector2 offsetFromBase, Vector2 tipRestingOffset, float forearmLength, float armLenght,
                float acceleration, float springiness,
                CrabulonArmSkin skin)
            {
                this.crabulon = crabulon;
                this.offsetFromBase = offsetFromBase;
                this.tipRestingOffset = tipRestingOffset;
                this.forearmLength = forearmLength;
                this.armLenght = armLenght;

                this.acceleration = acceleration;
                this.springiness = springiness;

                ForearmAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + skin.texturePath + "Forearm");
                ArmAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + skin.texturePath + "Arm");
                forearmRotationOffset = skin.forearmRotationOffset;
                armRotationOffset = skin.armRotationOffset;
                forearmOrigin = skin.forearmOrigin;
                armOrigin = skin.armOrigin;
                spriteSizeMultiplier = skin.spriteSizeMultiplier;

                clawOffset = skin.clawOffset;
                clawOrigin = skin.clawOrigin;

                armTip = Goal;
                elbow = FablesUtils.InverseKinematic(Anchor, armTip, forearmLength * NPC.scale, armLenght * NPC.scale, bendFlip);
                largeClaw = crabulon.Arms.Count == 1;

                if (largeClaw)
                {
                    ClawAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + skin.texturePath + "ArmClaw");
                    ForearmBackAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + skin.texturePath + "ForearmBack");
                    ArmBackAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + skin.texturePath + "ArmBack");
                }

                LimpingArm = new VerletNet();
                LimpingArm.points.Add(new VerletPoint(Anchor, true));
                LimpingArm.points.Add(new VerletPoint(elbow));
                LimpingArm.points.Add(new VerletPoint(armTip));
                LimpingArm.segments.Add(new VerletStick(LimpingArm.points[0], LimpingArm.points[1]));
                LimpingArm.segments.Add(new VerletStick(LimpingArm.points[1], LimpingArm.points[2]));
            }

            public void Update()
            {
                if (clawClackTimer > 0)
                    clawClackTimer -= 1 / (60f * 0.4f);

                if (limping)
                {
                    LimpUpdate();
                    return;
                }

                Vector2 anchor = Anchor;
                if (NPC.velocity.Y > 2f)
                    fallTime++;
                else
                {
                    if (fallTime > 20f)
                        velocity *= 0.05f;

                    fallTime = 0;
                }

                float xAccelerationMultiplier = 1f;
                //Accelerates more slowly when falling
                float yAccelerationMultiplier = 1f * (0.4f + 0.6f * Utils.GetLerpValue(5f, 2f, NPC.velocity.Y, true));

                Vector2 goal = Goal;
                Vector2 accelerationTarget = (goal - armTip);
                accelerationTarget.X *= Springiness;
                accelerationTarget.Y *= 1f + ((1 - Springiness) * (Utils.GetLerpValue(1f, 5f, NPC.velocity.Y, true)));

                velocity.X = MathHelper.Lerp(velocity.X, accelerationTarget.X, MathHelper.Min(Acceleration * xAccelerationMultiplier, 1f));
                velocity.Y = MathHelper.Lerp(velocity.Y, accelerationTarget.Y, MathHelper.Min(Acceleration * yAccelerationMultiplier, 1f));

                armTip += velocity;
                if (armTip.Distance(anchor) > MaxLenght)
                    armTip = anchor + anchor.DirectionTo(armTip) * MaxLenght;

                elbow = FablesUtils.InverseKinematic(anchor, armTip, forearmLength * NPC.scale, armLenght * NPC.scale, crabulon.DrawFlip ? !bendFlip : bendFlip);

                contractionOverride = null;
                goalOverride = null;
                springinessOverride = null;
                accelerationOverride = null;
            }

            public VerletNet LimpingArm;
            public bool limping;
            public bool updateLimpElbow;

            public bool GoLimp()
            {
                if (limping)
                    return false;
                limping = true;

                LimpingArm.points[0].position = Anchor;
                LimpingArm.points[1].position = elbow;
                LimpingArm.points[2].position = armTip;

                LimpingArm.points[0].oldPosition = Anchor;
                LimpingArm.points[1].oldPosition = elbow;
                LimpingArm.points[2].oldPosition = armTip;

                updateLimpElbow = true;
                return true;
            }

            public void LimpUpdate()
            {
                if (LimpingArm.points[0].locked)
                    LimpingArm.points[0].position = Anchor;

                float gravity = 1.6f;
                float momentum = 0.8f;

                //When moving around, less gravity, and only get higher momentum when moving slow
                if (crabulon.Limp_DanglingSimulationMode)
                {
                    gravity = 0.5f;
                    momentum = 0.8f + (1 - Math.Min(NPC.velocity.Length() / 30f, 1)) * 0.2f;
                }

                //Give custom outwards gravity to the elbows
                LimpingArm.points[1].customGravity = tipRestingOffset.SafeNormalize(Vector2.UnitY) * gravity;

                //When falling, the leg tips also try to spread out to mimic the air pushing them aside
                if (crabulon.Limp_FallingSimulationMode)
                    LimpingArm.points[2].customGravity = LimpingArm.points[1].customGravity + Vector2.UnitY * 2f;
                else
                    LimpingArm.points[2].customGravity = null;

                //Gravity increases for the leg tip when stunned, but decreases for the knees, so that the knees mostly stay up
                if (crabulon.Limp_RagdollSimulationMode)
                {
                    LimpingArm.points[1].customGravity *= 0f;
                    gravity *= 2.5f;
                }


                int armSegment = 0;
                foreach (VerletPoint point in LimpingArm.points)
                {
                    if (!crabulon.Limp_CollidingWithTiles)
                        point.tileCollideStyle = 0;
                    else
                    {
                        point.tileCollideStyle = 2;
                        if (armSegment == 2 && point.position.Y < LimpingArm.points[0].position.Y && LimpingArm.points[0].position.Distance(LimpingArm.points[2].position) > MaxLenght)
                            point.tileCollideStyle = 1;
                    }

                    armSegment++;
                }


                if (crabulon.SubState == ActionState.Dead_InternalGoreSimulation)
                {
                    LimpingArm.points[1].customGravity = null;
                    momentum = 1f - 0.05f * crabulon.AttackTimer;
                    gravity = 0.25f + crabulon.AttackTimer * 0.5f;
                    updateLimpElbow = true;
                }

                LimpingArm.Simulate(4, gravity, momentum);
                if (updateLimpElbow)
                    elbow = LimpingArm.points[1].position;
                armTip = LimpingArm.points[2].position;
            }

            public void UnLimp()
            {
                if (!limping)
                    return;
                limping = false;
                fallTime = 0;
                velocity = Vector2.Zero;
            }

            public void UnloadSprings()
            {
                velocity = Vector2.Zero;
                armTip = Goal;
            }

            public void ClackClaw()
            {
                if (clawClackTimer <= 0)
                {
                    clawClackTimer = 1f;
                    SoundEngine.PlaySound(ClawClickSound with { Volume = 0.1f }, NPC.Center);
                }
            }

            public void Draw(SpriteBatch spriteBatch, Vector2 screenPos, Color color, ArmDrawLayer layer)
            {
                //When seen from top down, the arms are drawn below everything
                if (crabulon.topDownView && layer != ArmDrawLayer.BehindBody)
                    return;

                //Don't draw anything on the back layer if not looking sideways
                else if (!crabulon.lookingSideways && !crabulon.topDownView && layer == ArmDrawLayer.BehindBody)
                    return;

                #region Sideway large claw drawing
                else if (largeClaw && crabulon.lookingSideways)
                {
                    //large claw draws behind the body
                    if (layer != ArmDrawLayer.BehindBody)
                        return;


                    Vector2 elbowPos = elbow;
                    float backArmRotation = 0f;
                    Vector2 ahead = Vector2.UnitX * NPC.direction * forearmLength;
                    bool onlyDrawArm = false;

                    if (crabulon.AIState == ActionState.Slam)
                    {
                        float angle = MathHelper.PiOver2 * 0.6f * MathF.Pow(1 - crabulon.AttackTimer, 1.6f) * -NPC.direction;

                        if (crabulon.SubState == ActionState.Slam_SlamDown)
                        {
                            float animProgress = Utils.GetLerpValue(0.6f, 1f, crabulon.AttackTimer, true);

                            angle = ((float)Math.Pow(animProgress, 7.6f)) * 0.6f * NPC.direction * -MathHelper.PiOver2 * 1.0f;

                        }

                        backArmRotation = angle * 2.6f;
                        elbowPos = Anchor + ahead.RotatedBy(angle);

                        if (crabulon.SubState == ActionState.Slam_SlamDown)
                        {
                            elbowPos.Y += crabulon.CollisionBoxYOffset * (1 - (float)Math.Pow(crabulon.AttackTimer, 7.6f)) * 0.5f;

                            //Draw afetrimages
                            if (crabulon.AttackTimer > 0.3f)
                            {
                                float afterimageOpacity = MathF.Pow(Utils.GetLerpValue(0.3f, 1f, crabulon.AttackTimer, true), 1.6f);

                                float angleSquish = MathF.Pow(Utils.GetLerpValue(0.3f, 1f, crabulon.AttackTimer, true), 0.8f);
                                float beforeAngle = MathHelper.PiOver2 * 0.6f * -NPC.direction * (0.4f + angleSquish * 0.6f);

                                for (int i = 0; i < 6; i++)
                                {
                                    float afterimageProgress = i / 6f;
                                    float afterimageAttackTimer = MathHelper.Lerp(1f, crabulon.AttackTimer, afterimageProgress);

                                    float afterImageAngle = Utils.AngleLerp(beforeAngle, angle, afterimageProgress);
                                    Vector2 afterImageElbow = Anchor + ahead.RotatedBy(afterImageAngle);
                                    afterImageElbow.Y += crabulon.CollisionBoxYOffset * (1 - (float)Math.Pow(afterimageAttackTimer, 7.6f)) * 0.5f;

                                    Color afterImageColor = Color.Lerp(Color.Blue, Color.RoyalBlue * 1.6f, afterimageProgress);


                                    DrawArm_Inner(spriteBatch, screenPos, afterImageColor with { A = 0} * afterimageProgress * afterimageOpacity, afterImageElbow, afterImageAngle * 2.6f, false, true, NPC.direction == -1);
                                    DrawArm_Inner(spriteBatch, screenPos, afterImageColor with { A = 0 } * afterimageProgress * afterimageOpacity * 0.4f, afterImageElbow, afterImageAngle * 2.6f, false, true, NPC.direction == -1);
                                }
                            }
                        }
                    }

                    else if (crabulon.AIState == ActionState.Snip)
                    {
                        if (crabulon.SubState == ActionState.Snip_ThrustForwards)
                        {
                            float rotateDown = Utils.GetLerpValue(0.7f, 0f, crabulon.AttackTimer, true) * NPC.direction * 0.9f;
                            elbowPos = Anchor + ahead.RotatedBy(rotateDown) * ((float)Math.Pow(crabulon.AttackTimer, 2f) * 0.3f + 1f);
                        }
                        else
                        {
                            float retraction = 0.3f - 0.4f * MathF.Sin(crabulon.AttackTimer * MathHelper.Pi);
                            elbowPos = Anchor + ahead * retraction;

                            onlyDrawArm = true;
                            elbowPos.Y += 10f * (float)Math.Sin(crabulon.AttackTimer * MathHelper.PiOver2);
                            backArmRotation = NPC.direction * MathF.Pow(1 - crabulon.AttackTimer, 0.5f) * -0.2f;
                        }
                    }

                    //Default (only for slingshot atm), retracted back
                    else
                    {
                        elbowPos = Anchor;
                        onlyDrawArm = true;
                    }

                    DrawArm_Inner(spriteBatch, screenPos, color, elbowPos, backArmRotation, !onlyDrawArm, true, NPC.direction == -1);
                    return;
                }
                #endregion

                #region Regular drawing
                bool drawForearm = crabulon.topDownView ? true : layer == ArmDrawLayer.BehindFaceProps;
                bool drawArm = crabulon.topDownView ? true : layer == ArmDrawLayer.AboveBody;
                Vector2 offset = Vector2.Zero;
                bool flipped = false;

                if (crabulon.topDownView)
                    offset = (NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * 30f * NPC.scale;
                else if (crabulon.lookingSideways)
                {
                    offset = Vector2.UnitX * -20 * NPC.scale;
                    if (NPC.direction == 1)
                    {
                        offset = Vector2.UnitX * 20f * NPC.scale;
                        flipped = true;
                    }
                }

                float armRotation = elbow.AngleTo(armTip);
                if (!largeClaw)
                    armRotation -= ArmRotationOffset;
                else
                    armRotation += ArmRotationOffset;

                DrawArm_Inner(spriteBatch, screenPos - offset, color, elbow, armRotation, drawForearm, drawArm, flipped);
                #endregion
            }

            public void DrawArm_Inner(SpriteBatch spriteBatch, Vector2 screenPos, Color color, Vector2 elbowPosition, float armRotation, bool drawForearm, bool drawArm, bool flipped)
            {
                //Forearm
                if (drawForearm)
                {
                    Texture2D tex = ForearmAsset.Value;
                    if (crabulon.lookingSideways && largeClaw)
                        tex = ForearmBackAsset.Value;

                    SpriteEffects effects = flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    Vector2 origin = flipped ? new Vector2(tex.Width - forearmOrigin.X, forearmOrigin.Y) : forearmOrigin;
                    float rotation = Anchor.AngleTo(elbowPosition) + ForearmRotationOffset;
                    spriteBatch.Draw(tex, Anchor - screenPos, null, color, rotation, origin, NPC.scale * spriteSizeMultiplier, effects, 0);
                }

                //Arm
                if (drawArm)
                {
                    Texture2D tex = ArmAsset.Value;
                    if (crabulon.lookingSideways && largeClaw)
                        tex = ArmBackAsset.Value;

                    SpriteEffects effects = flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    Vector2 origin = flipped ? new Vector2(tex.Width - armOrigin.X, armOrigin.Y) : armOrigin;

                    //Draw the claw's pincer
                    if (largeClaw)
                    {
                        Vector2 clawPosition = clawOffset;
                        if (flipped)
                            clawPosition.X = -clawPosition.X;
                        Vector2 clawOrig = flipped ? new Vector2(ClawAsset.Value.Width - clawOrigin.X, clawOrigin.Y) : clawOrigin;

                        spriteBatch.Draw(ClawAsset.Value, elbowPosition + clawPosition.RotatedBy(armRotation) * NPC.scale - screenPos, null, color, armRotation + ClawOpenness, clawOrig, NPC.scale * spriteSizeMultiplier, effects, 0);
                    }

                    spriteBatch.Draw(tex, elbowPosition - screenPos, null, color, armRotation, origin, NPC.scale * spriteSizeMultiplier, effects, 0);
                }
            }
        }

        #region Strings    
        public class CrabulonPupeteeringRack
        {
            public static Asset<Texture2D> StringTexture;

            public Crabulon crabulon;
            public NPC NPC => crabulon.NPC;
            public readonly int originalStringCount;
            public int StringCount => strings == null ? 0 : strings.chains.Count;
            public readonly List<Vector2> rackOffsets;
            public readonly List<float> stringOpacities;


            public readonly VerletNet strings;
            public readonly VerletNet backLegStrings;
            public readonly VerletNet frontLegStrings;

            //The strings that snapped
            public readonly VerletNet snappingStrings;
            public List<float> brokenStringOpacities;
            public List<Vector2> brokenStringOffsets;

            public int StringCountLimit {
                get {
                    if (DifficultyScale == 0)
                        return originalStringCount - (int)((1 - (NPC.life / (float)NPC.lifeMax)) * originalStringCount * 0.5f);
                    else
                        return originalStringCount - (int)(Utils.GetLerpValue(1f, DesperationPhaseTreshold + 0.1f, NPC.life / (float)NPC.lifeMax, true) * (originalStringCount - 1) * 0.5f);
                }
            }
            public float BrokenStringDivisionPoint => 0.22f;


            public float tension = 0f;
            public float gravTension = 0f;

            public float EasedTension => (float)Math.Pow(tension, 2f);
            public float EasedGravTension => (float)Math.Pow(gravTension, 2f);

            public Vector2 baseOffsetFromCrab;
            public Vector2 rackOffset;

            public Vector2 IdealRackPosition => crabulon.VisualCenter + rackOffset * (1 + tension);
            public Vector2 rackPosition;
            public Vector2 IdealLegsRackPosition => crabulon.VisualCenter + rackOffset * 2f;
            public Vector2 legsRackPosition;


            public int STRING_LIGHT_SAMPLES => (int)(24 * (1 + tension));
            public float StringWidth(float progress) => Math.Min(4f * NPC.scale, 4f);
            public Color StringColor(float progress)
            {
                int currentColorIndex = (int)(progress * (STRING_LIGHT_SAMPLES - 1));
                Color currentColor = stringLightColors[currentColorIndex];
                Color nextColor = stringLightColors[(currentColorIndex + 1) % (STRING_LIGHT_SAMPLES - 1)];
                Color lightColor = Color.Lerp(currentColor, nextColor, (progress * (STRING_LIGHT_SAMPLES - 1)) % 1f);

                return lightColor * OpacityMultiplier * (float)Math.Pow(1 - progress, 2f);
            }

            public static readonly List<Color> stringLightColors = new List<Color>(50);

            public static float OpacityMultiplier;

            public CrabulonPupeteeringRack(Crabulon crab)
            {
                crabulon = crab;
                originalStringCount = Main.rand.Next(7, 10);
                baseOffsetFromCrab = Vector2.UnitY * -1200f;
                rackOffset = baseOffsetFromCrab;

                rackOffsets = new List<Vector2>();
                stringOpacities = new List<float>();
                strings = new VerletNet();

                rackPosition = IdealRackPosition;
                legsRackPosition = IdealLegsRackPosition;

                //TODO make the strings better spread
                for (int i = 0; i < originalStringCount; i++)
                {
                    rackOffsets.Add(Main.rand.NextVector2Circular(100f, 20f) * NPC.scale);
                    stringOpacities.Add(Main.rand.NextFloat(0.5f, 1f) * (float)Math.Sin((1 - Math.Abs(rackOffsets[i].X) / 100f) * MathHelper.PiOver2));

                    VerletPoint start = new VerletPoint(crabulon.VisualCenter + rackOffsets[i].RotatedBy(crabulon.visualRotation), true);
                    VerletPoint end = new VerletPoint(rackPosition + rackOffsets[i].RotatedBy(crabulon.visualRotation), true);
                    strings.AddChain(start, end, 10, StringWidth, StringColor, primResolution: 3f);
                }

                frontLegStrings = new VerletNet();
                backLegStrings = new VerletNet();

                //Leg strings
                for (int i = 0; i < 4; i++)
                {
                    CrabulonLeg leg = crab.Legs[i];

                    //We actually set the end point as the rack position instead of the leg rack position because it makes the strings have a lot of tension afterwards
                    VerletPoint start = new VerletPoint(leg.legTipGraphic, true);
                    VerletPoint end = new VerletPoint(rackPosition + (leg.baseRotation + crabulon.visualRotation).ToRotationVector2() * 60f, true);

                    VerletNet matchingNet = leg.frontPair ? frontLegStrings : backLegStrings;
                    matchingNet.AddChain(start, end, 20, StringWidth, StringColor);
                }

                snappingStrings = new VerletNet();
                brokenStringOpacities = new List<float>();
                brokenStringOffsets = new List<Vector2>();
            }

            public void Update()
            {
                Vector2 gravVelocity = NPC.velocity;
                Vector2 velocityOffset = NPC.velocity * 30f;

                if (crabulon.AIState == ActionState.Raving)
                {
                    velocityOffset = crabulon.visualOffsetVelocity * 10f;
                    gravVelocity = crabulon.visualOffsetVelocity * 4f;
                }

                if (velocityOffset.Y > 0)
                    velocityOffset.Y = 0;
                velocityOffset.X *= 2f;

                UpdateTensions(velocityOffset, gravVelocity);

                //The X offset is lowered when falling
                if (gravTension < 0)
                    velocityOffset.X *= 1 + gravTension * 0.8f;

                rackOffset = baseOffsetFromCrab + velocityOffset;

                //When falling, make the rack go lower
                if (gravTension < 0)
                    rackOffset.Y *= 1f - EasedGravTension * 0.2f;

                float bodyScale = 1f;
                if (gravTension > 0f)
                    bodyScale -= 0.7f * EasedGravTension;
                float rackScale = 1 - tension * 0.7f;
                if (gravTension < 0)
                    rackScale += -gravTension * 1.5f;

                rackPosition = Vector2.Lerp(rackPosition, IdealRackPosition, 0.05f);
                legsRackPosition = Vector2.Lerp(legsRackPosition, IdealLegsRackPosition, 0.05f);

                for (int i = 0; i < StringCount; i++)
                {
                    Vector2 offset = rackOffsets[i].RotatedBy(crabulon.visualRotation);
                    strings.extremities[i * 2].position = crabulon.VisualCenter + offset * bodyScale;
                    strings.extremities[i * 2 + 1].position = rackPosition + offset * rackScale;
                }

                for (int i = 0; i < 2; i++)
                {
                    CrabulonLeg leg = crabulon.Legs[i * 2 + i];
                    backLegStrings.extremities[i * 2].position = leg.legTipGraphic;
                    backLegStrings.extremities[i * 2 + 1].position = legsRackPosition + (leg.baseRotation + crabulon.visualRotation).ToRotationVector2() * 60f;
                }
                for (int i = 0; i < 2; i++)
                {
                    CrabulonLeg leg = crabulon.Legs[1 + i];
                    frontLegStrings.extremities[i * 2].position = leg.legTipGraphic;
                    frontLegStrings.extremities[i * 2 + 1].position = legsRackPosition + (leg.baseRotation + crabulon.visualRotation).ToRotationVector2() * 60f;
                }

                int stringIterationsExtra = 0;
                if (crabulon.SubState == ActionState.Snip_ThrustForwards)
                    stringIterationsExtra = (int)(15 * crabulon.AttackTimer);
                else if (crabulon.SubState == ActionState.Snip_ReadyClaw)
                    stringIterationsExtra = (int)(10 * (1 - crabulon.AttackTimer));

                strings.Update(2 + stringIterationsExtra, 0.3f);
                frontLegStrings.Update(7 + stringIterationsExtra, 0.3f);
                backLegStrings.Update(7 + stringIterationsExtra, 0.3f);

                //Snap strings
                while (StringCount > StringCountLimit)
                    SnapString();

                for (int i = 0; i < snappingStrings.chains.Count; i++)
                {

                    if (i % 2 == 1)
                        snappingStrings.extremities[i * 2].position.Y -= 1f;
                    else
                    {
                        snappingStrings.extremities[i * 2].position = crabulon.VisualCenter + brokenStringOffsets[i / 2].RotatedBy(crabulon.visualRotation) * bodyScale;

                        //Fade out
                        brokenStringOpacities[i / 2] *= 0.99f;
                        brokenStringOpacities[i / 2] -= 0.01f;

                        if (brokenStringOpacities[i / 2] <= 0)
                        {
                            snappingStrings.RemoveFirstChain();
                            snappingStrings.RemoveFirstChain();
                            brokenStringOpacities.RemoveAt(0);
                            brokenStringOffsets.RemoveAt(0);
                        }
                    }
                }

                snappingStrings.Update(2, 0.5f);
            }

            public void UpdateTensions(Vector2 velocityOffset, Vector2 gravityVelocity)
            {
                float idealGravTension = Utils.GetLerpValue(0f, -5f, gravityVelocity.Y, true);
                if (NPC.velocity.Y > 0)
                    idealGravTension = -Utils.GetLerpValue(0f, 4f, gravityVelocity.Y, true);
                gravTension = MathHelper.Lerp(gravTension, idealGravTension, 0.06f);


                float idealTension = Utils.GetLerpValue(3f, 6f, velocityOffset.Length(), true);
                if (crabulon.SubState == ActionState.Snip_ReadyClaw)
                    idealTension += 1f * (1 - crabulon.AttackTimer);
                if (crabulon.SubState == ActionState.Snip_ThrustForwards)
                    idealTension += 1.5f;


                tension = MathHelper.Lerp(tension, idealTension, 0.04f);
            }

            public void SnapString(bool silent = false)
            {
                if (!silent)
                {
                    SoundEngine.PlaySound(Items.DesertScourgeDrops.StormlionWhip.YankSound, NPC.Center);
                    if (Main.LocalPlayer.Distance(NPC.Center) < 1000)
                        CameraManager.Quake += 10;
                }

                int midwayPointIndex = (int)(BrokenStringDivisionPoint * strings.chains[^1].Count);
                VerletPoint midwayPoint = strings.chains[^1][midwayPointIndex];

                VerletPoint snappedMidwayPoint = new VerletPoint(midwayPoint.position);
                VerletPoint snappedMidwayPoint2 = new VerletPoint(midwayPoint.position);

                //2 chains
                snappingStrings.AddChain(strings.chains[^1][0], snappedMidwayPoint, 10, StringWidth, SnappedStringColorBottomString, primResolution: 1.5f);
                snappingStrings.AddChain(strings.chains[^1][^1], snappedMidwayPoint2, 10, StringWidth, SnappedStringColorTopString, primResolution: 1.5f);

                Vector2 snapDirection = Vector2.UnitX * Main.rand.NextFloat(0.6f, 1f) * (Main.rand.NextBool() ? -1 : 1) * 7f;

                for (int i = 0; i < snappingStrings.chains[^2].Count; i++)
                    snappingStrings.chains[^2][i].oldPosition -= snapDirection * (float)Math.Pow(i / 10f, 2f);

                for (int i = 0; i < snappingStrings.chains[^1].Count; i++)
                    snappingStrings.chains[^1][i].oldPosition += snapDirection * (float)Math.Pow(i / 10f, 2f);

                strings.RemoveLastChain();

                int snappedStringIndex = StringCount;
                brokenStringOpacities.Add(stringOpacities[snappedStringIndex]);
                brokenStringOffsets.Add(rackOffsets[snappedStringIndex]);
            }

            public void AssignOpacityMultiplier(int i, ref float scroll)
            {
                OpacityMultiplier = stringOpacities[i];

                if (crabulon.AIState == ActionState.HuskDrop)
                {
                    if (crabulon.SubState == ActionState.HuskDrop_VineAttach)
                        OpacityMultiplier *= 0.1f + 0.9f * crabulon.AttackTimer;
                    else if (crabulon.SubState == ActionState.HuskDrop_Stunned)
                        OpacityMultiplier *= 0.1f + 0.9f * (1 - crabulon.AttackTimer);
                    else
                        OpacityMultiplier *= 0.1f;
                }

                OpacityMultiplier *= crabulon.GlobalStringOpacityMutliplier;
            }

            public void AssignBackLegMultipliers(int i, ref float scroll)
            {
                OpacityMultiplier = 0.3f;
                if (crabulon.TopDownView)
                    OpacityMultiplier += 0.2f;
                OpacityMultiplier *= crabulon.GlobalStringOpacityMutliplier;

                GetColorMultipliers(backLegStrings.extremities[i * 2].position, backLegStrings.extremities[i * 2 + 1].position);
            }

            public void AssignFrontLegMultipliers(int i, ref float scroll)
            {
                OpacityMultiplier = 0.4f;
                if (crabulon.TopDownView)
                    OpacityMultiplier += 0.2f;
                OpacityMultiplier *= crabulon.GlobalStringOpacityMutliplier;

                GetColorMultipliers(frontLegStrings.extremities[i * 2].position, frontLegStrings.extremities[i * 2 + 1].position);
            }

            public void GetColorMultipliers(Vector2 start, Vector2 end)
            {
                while (stringLightColors.Count < STRING_LIGHT_SAMPLES)
                    stringLightColors.Add(Color.Black);

                for (int i = 0; i < STRING_LIGHT_SAMPLES; i++)
                {
                    float progress = i / (float)(STRING_LIGHT_SAMPLES - 1);
                    Vector2 position = Vector2.Lerp(start, end, progress);
                    //Dust d = Dust.QuickDust(position + Vector2.UnitX * 40f, Color.Red);
                    //d.noLightEmittence = true;
                    stringLightColors[i] = Lighting.GetColor(position.ToTileCoordinates());
                }
            }

            public void AssignSnappedStringFade(int i, ref float scroll)
            {
                OpacityMultiplier = brokenStringOpacities[i / 2];
                scroll = (i / 2) * 0.24f;
            }

            public Color SnappedStringColorTopString(float progress)
            {
                float remappedProgress = 1 - progress * (1 - BrokenStringDivisionPoint);

                int currentColorIndex = (int)(remappedProgress * (STRING_LIGHT_SAMPLES - 1));
                Color currentColor = stringLightColors[currentColorIndex];
                Color nextColor = stringLightColors[(currentColorIndex + 1) % (STRING_LIGHT_SAMPLES - 1)];
                Color lightColor = Color.Lerp(currentColor, nextColor, (remappedProgress * (STRING_LIGHT_SAMPLES - 1)) % 1f);

                return lightColor * OpacityMultiplier * (float)Math.Pow(progress, 2f);
            }
            public Color SnappedStringColorBottomString(float progress)
            {
                float remappedProgress = progress * BrokenStringDivisionPoint;

                int currentColorIndex = (int)(remappedProgress * (STRING_LIGHT_SAMPLES - 1));
                Color currentColor = stringLightColors[currentColorIndex];
                Color nextColor = stringLightColors[(currentColorIndex + 1) % (STRING_LIGHT_SAMPLES - 1)];
                Color lightColor = Color.Lerp(currentColor, nextColor, (remappedProgress * (STRING_LIGHT_SAMPLES - 1)) % 1f);

                return lightColor * OpacityMultiplier;
            }

            public void Draw(Vector2 screenPos, Color color)
            {
                StringTexture = StringTexture ?? ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MyceliumString");

                if (crabulon.AIState != ActionState.Dead && crabulon.AIState != ActionState.Desperation)
                    backLegStrings?.Render(StringTexture.Value, -screenPos, 0.24f, AssignBackLegMultipliers, 1 + tension * 0.98f);

                GetColorMultipliers(crabulon.VisualCenter, rackPosition);
                strings?.Render(StringTexture.Value, -screenPos, 0.24f, AssignOpacityMultiplier, 2f);

                snappingStrings?.Render(StringTexture.Value, -screenPos, 0f, AssignSnappedStringFade);
            }

            public void DrawFrontStrings(Vector2 screenPos, Color color)
            {
                StringTexture = StringTexture ?? ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MyceliumString");
                frontLegStrings?.Render(StringTexture.Value, -screenPos, 0.24f, AssignFrontLegMultipliers, 2f);
            }
        }

        public class CrabulonCables
        {
            public static Asset<Texture2D> Vines;
            public static Asset<Texture2D> VineHook;

            public Crabulon crabulon;
            public NPC NPC => crabulon.NPC;
            public readonly VerletNet hangingCables;
            public int stabTimer;
            public int stabDirection;
            public float Stab => stabTimer / 15f;
            public int cableCount;

            public List<Vector2> attachPointOffsets;
            public Vector2 position;
            public Vector2 positionExtra;

            public float cableBalance = 0f;

            public bool Attached => (int)crabulon.SubState < (int)ActionState.HuskDrop_Drop || crabulon.AIState == ActionState.Desperation || crabulon.SubState == ActionState.DespawningDropDownDesperation;
            public bool ManualSet => crabulon.SubState == ActionState.HuskDrop_Chase || crabulon.SubState == ActionState.Desperation_Chase;

            public CrabulonCables(Crabulon crab)
            {
                crabulon = crab;
                attachPointOffsets = new List<Vector2>();
                hangingCables = new VerletNet();
                cableCount = 0;
                stabTimer = 0;
                position = crabulon.VisualCenter - Vector2.UnitY * 1400f;
            }

            public void AddSkewer()
            {
                SoundEngine.PlaySound(SporedCorpse.VineHookSound with { Volume = SporedCorpse.VineHookSound.Volume * 0.4f }, NPC.Center);

                stabTimer = 15;
                Vector2 attach = Main.rand.NextVector2Circular(85f, 35f);
                if (cableCount > 0)
                {
                    float nextBalance = cableBalance + attach.X / 120f;
                    if (Math.Abs(nextBalance) > 1.2f)
                        attach.X *= -1f;
                }

                attach *= NPC.scale;
                attachPointOffsets.Add(attach);
                cableCount++;
                cableBalance += attach.X / 120f;
                stabDirection = attach.X.NonZeroSign();

                VerletPoint start = new VerletPoint(crabulon.VisualCenter + attach.RotatedBy(crabulon.visualRotation), true);
                VerletPoint end = new VerletPoint(position + attach.RotatedBy(crabulon.visualRotation) * 1.4f, true);

                hangingCables.AddChain(start, end, 20, VineWidth, VineColor, 0f, 2f);
            }

            public void Update()
            {
                if (crabulon.AIState != ActionState.HuskDrop && crabulon.AIState != ActionState.Desperation && crabulon.AIState != ActionState.Dead && crabulon.SubState != ActionState.DespawningDropDownDesperation)
                    return;

                float stretch = crabulon.SubState != ActionState.Desperation_Stunned ? 1f : 1 + crabulon.desperationTug * 0.2f;

                //MOVE PROPERLY
                if (Attached && !ManualSet)
                    position = crabulon.VisualCenter - Vector2.UnitY * 1400f * stretch + positionExtra;
                else if (!Attached)
                    position.Y -= 4f;

                positionExtra = Vector2.Lerp(positionExtra, new Vector2(NPC.velocity.X * 75f, 0f), 0.1f);
                stabTimer = Math.Max(0, stabTimer - 1);

                UpdatePositions();

                float gravity = crabulon.Limp_DanglingSimulationMode ? 0.17f : 0.57f;
                int iterations = crabulon.AIState == ActionState.Desperation ? 30 : 10;
                float momentum = 1f;
                if (crabulon.SubState == ActionState.Desperation_Chase)
                {
                    momentum = 1f;
                    iterations = 30;
                }
                else if (crabulon.SubState == ActionState.Desperation_ReelUp)
                    momentum = 0.8f;
                else if (crabulon.SubState == ActionState.Desperation_Drop)
                    momentum = 0.5f;
                else if (crabulon.SubState == ActionState.Desperation_Stunned && crabulon.desperationTug > 0f)
                {
                    momentum = 1f;
                }

                hangingCables.Update(iterations, gravity, momentum);
            }

            public void UpdatePositions()
            {
                float squeeze = crabulon.SubState != ActionState.Desperation_Stunned ? 1f : 1 - crabulon.desperationTug * 0.2f;

                for (int i = 0; i < cableCount; i++)
                {
                    if (Attached)
                        hangingCables.extremities[i * 2].position = crabulon.VisualCenter + attachPointOffsets[i].RotatedBy(crabulon.visualRotation);
                    hangingCables.extremities[i * 2 + 1].position = position + attachPointOffsets[i].RotatedBy(crabulon.visualRotation) * 1.4f * squeeze;
                }
            }

            public void CutSkewers()
            {
                //Detach the cables
                for (int i = 0; i < cableCount; i++)
                {
                    hangingCables.extremities[i * 2].locked = false;
                    hangingCables.extremities[i * 2].oldPosition = hangingCables.extremities[i * 2].position;
                }
            }

            public void SmoothenCurvesAgain(float heightReduction)
            {

                for (int i = 0; i < cableCount; i++)
                {
                    float baseHeight = hangingCables.extremities[i * 2].position.Y;
                    float chainHeight = baseHeight - hangingCables.extremities[i * 2 + 1].position.Y;

                    for (int j = 0; j < hangingCables.chains[i].Count; j++)
                    {
                        hangingCables.chains[i][j].position.Y = baseHeight + (hangingCables.chains[i][j].position.Y - baseHeight) * heightReduction;
                        hangingCables.chains[i][j].oldPosition.Y = baseHeight + (hangingCables.chains[i][j].oldPosition.Y - baseHeight) * heightReduction;
                    }
                }
            }

            public static Color VineLightColor;
            public float VineWidth(float progress) => 9f;
            public Color VineColor(float progress) => VineLightColor * (1 - progress);

            public void Render(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
            {
                Vines = Vines ?? ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MyceliumVine");
                VineHook = VineHook ?? ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MyceliumVineHook");

                float opacityMultiplier = 1f;
                if (crabulon.SubState == ActionState.HuskDrop_Stunned)
                    opacityMultiplier *= crabulon.AttackTimer;
                else if (crabulon.SubState == ActionState.Dead_Limping)
                    opacityMultiplier *= Math.Max(0f, (0.5f - crabulon.AttackTimer) / 0.5f);
                else if (crabulon.SubState == ActionState.DespawningDropDownDesperation)
                    opacityMultiplier *= crabulon.AttackTimer;

                VineLightColor = lightColor * opacityMultiplier;
                hangingCables.Render(Vines.Value, -screenPos, 0.33f, PerCableCallback);

                if (!Attached)
                {
                    for (int i = 0; i < cableCount; i++)
                    {
                        float rotation = hangingCables.trailRenderers[i].Positions[0].AngleTo(hangingCables.trailRenderers[i].Positions[1]);
                        Vector2 origin = new Vector2(VineHook.Value.Width / 2f, 0f);
                        spriteBatch.Draw(VineHook.Value, hangingCables.extremities[i * 2].position - screenPos, null, lightColor * opacityMultiplier, rotation + MathHelper.PiOver2, origin, 1f, 0, 0);
                    }
                }
            }

            public void PerCableCallback(int chain, ref float scroll)
            {
                if (chain == cableCount - 1)
                    scroll += 0.3f * (float)Math.Pow(Math.Max(0, Stab), 2f);
            }

            public void Reset()
            {
                attachPointOffsets.Clear();
                hangingCables.Clear();
                stabTimer = 0;
                cableCount = 0;
                cableBalance = 0f;
            }
        }
        #endregion

        public class CrabulonProp
        {
            public Crabulon crabulon;
            public readonly Vector2 offsetFromBase;
            private Vector2 gravityDirection;
            public Vector2 GravityDirection {
                get {
                    if (crabulon.topDownView)
                        return gravityDirection.RotatedBy(NPC.rotation);
                    if (crabulon.DrawFlip)
                        return new Vector2(gravityDirection.X * -1, gravityDirection.Y);
                    return gravityDirection;
                }
                set => gravityDirection = value;
            }

            public float gravityAngle;
            public readonly float damping;
            public readonly float velocityCarryover;
            public readonly float maxAngleDeviation;

            #region Textures
            public string textureName;
            public string FlickerTextureName => (useFlicker && crabulon.EffectFlicker > 0) ? "Flicker" + crabulon.EffectFlicker.ToString() : "";

            public Asset<Texture2D> TextureAsset;

            public Asset<Texture2D> GlowmaskAsset;

            public Asset<Texture2D> SideTextureAsset;

            public Asset<Texture2D> SideGlowmaskAsset;

            public Asset<Texture2D> TopDownTextureAsset;

            public Asset<Texture2D> TopDownGlowmaskAsset;
            #endregion

            public bool faceProp;
            public bool useFlicker;

            public readonly bool flipped;
            public readonly SpriteEffects spriteEffects;
            public SpriteEffects Flip {
                get {
                    if (crabulon.DrawFlip)
                        return spriteEffects == SpriteEffects.FlipHorizontally ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                    return spriteEffects;
                }
            }


            public readonly Vector2 spriteOrigin;
            public Vector2 position;
            public Vector2 velocity;
            public Vector2 lastAnchor;
            public Color glowmaskColor = Color.White;

            public NPC NPC => crabulon.NPC;
            public Vector2 Anchor {
                get {

                    Vector2 usedOffset = offsetFromBase;

                    if (crabulon.topDownView)
                        usedOffset = topDownOffsetFromBase;

                    else if (crabulon.lookingSideways)
                    {
                        usedOffset = sidewayOffsetFromBase;
                        if (NPC.direction == 1)
                            usedOffset.X *= -1;
                    }

                    return crabulon.VisualCenter + usedOffset.RotatedBy(crabulon.visualRotation) * crabulon.NPC.scale;
                }
            }
            public Vector2 IdealPosition => Anchor + GravityDirection * 16f;
            public float GravityAngle {
                get {
                    if (crabulon.DrawFlip)
                        return new Vector2(gravityDirection.X * -1, gravityDirection.Y).ToRotation() + crabulon.visualRotation;
                    return gravityAngle + crabulon.visualRotation;
                }
            }


            public CrabulonProp(Crabulon crab, Vector2 offsetFromCenter, Vector2 gravityDir, float damping, float velocityCarry, float maxAngleDeviation, string textureName, Vector2 spriteOrigin, bool flipped = false, bool glowmask = false, bool useEyeFlicker = false)
            {
                crabulon = crab;
                offsetFromBase = offsetFromCenter;
                GravityDirection = gravityDir;
                gravityAngle = gravityDir.ToRotation();
                this.damping = damping;
                this.maxAngleDeviation = maxAngleDeviation;
                this.velocityCarryover = velocityCarry;
                this.flipped = flipped;
                this.spriteOrigin = spriteOrigin;
                this.textureName = textureName;
                this.useFlicker = useEyeFlicker;

                spriteEffects = SpriteEffects.None;

                TextureAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + textureName, AssetRequestMode.ImmediateLoad);

                if (glowmask)
                    GlowmaskAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + textureName + "Glow", AssetRequestMode.ImmediateLoad);


                if (flipped)
                {
                    if (!useFlicker)
                    {
                        this.spriteOrigin.X = TextureAsset.Value.Width - this.spriteOrigin.X;
                    }
                    else
                    {
                        int frameWidth = TextureAsset.Value.Width / 6 - 2;
                        this.spriteOrigin.X = frameWidth - this.spriteOrigin.X;
                    }

                    gravityDirection.X *= -1;
                    gravityAngle = GravityDirection.ToRotation();
                    spriteEffects = SpriteEffects.FlipHorizontally;
                }

                position = Anchor + GravityDirection * 16f;
                velocity = crabulon.NPC.velocity;
            }

            #region sideway and top down stuff
            public Vector2 sidewayOffsetFromBase;
            public Vector2 sidewaySpriteOrigin = Vector2.Zero;
            public float sidewayHorizontalSquish = 1f;

            public CrabulonProp SetSidewaysData(Vector2 sidewayOffsetFromBase, float horizontalSquish = 1f, bool customTexture = false, Vector2? customTextureOrigin = null)
            {
                this.sidewayOffsetFromBase = sidewayOffsetFromBase;
                sidewayHorizontalSquish = horizontalSquish;

                if (customTexture)
                {
                    SideTextureAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + textureName + "Side", AssetRequestMode.ImmediateLoad);
                    if (GlowmaskAsset != null)
                        SideGlowmaskAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + textureName + "SideGlow", AssetRequestMode.ImmediateLoad);

                    if (customTextureOrigin.HasValue)
                        sidewaySpriteOrigin = customTextureOrigin.Value;
                    else
                        sidewaySpriteOrigin = spriteOrigin;
                }

                return this;
            }

            public Vector2 topDownOffsetFromBase;
            public Vector2 topDownSpriteOrigin = Vector2.Zero;
            public bool hiddenWhenTopDown = false;
            public bool topDownOnly = false;

            public CrabulonProp SetTopDownData(Vector2 topDownOffsetFromBase, bool customTexture = false, Vector2? customTextureOrigin = null)
            {
                this.topDownOffsetFromBase = topDownOffsetFromBase;

                if (customTexture)
                {
                    TopDownTextureAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + textureName + "TopDown", AssetRequestMode.ImmediateLoad);
                    if (GlowmaskAsset != null)
                        TopDownGlowmaskAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + textureName + "TopDownGlow", AssetRequestMode.ImmediateLoad);

                    if (customTextureOrigin.HasValue)
                    {
                        topDownSpriteOrigin = customTextureOrigin.Value;
                        if (flipped)
                            topDownSpriteOrigin.X = TopDownTextureAsset.Width() - topDownSpriteOrigin.X;
                    }
                    else
                        topDownSpriteOrigin = spriteOrigin;
                }

                return this;
            }

            public CrabulonProp HideTopDown()
            {
                topDownOffsetFromBase = offsetFromBase;
                hiddenWhenTopDown = true;
                return this;
            }

            public CrabulonProp TopDownOnly()
            {
                topDownOnly = true;
                topDownOffsetFromBase = offsetFromBase;
                sidewayOffsetFromBase = offsetFromBase;
                return this;
            }

            #endregion

            public void Update()
            {
                velocity += NPC.velocity * velocityCarryover;
                velocity = Vector2.Lerp(velocity, (IdealPosition - position) * 0.5f, 1 - damping);
                if (velocity.Length() < 0.03f)
                    velocity = Vector2.Zero;

                position += velocity;
                Vector2 anchor = Anchor;

                float distanceFromAnchor = anchor.Distance(position);
                if (distanceFromAnchor > 26f)
                {
                    distanceFromAnchor = 26f;
                    position = anchor + anchor.DirectionTo(position) * distanceFromAnchor;
                }
            }

            public void Draw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
            {
                //Don't draw if crabby is top down and the prop hides when top down
                if (hiddenWhenTopDown && crabulon.TopDownView)
                    return;
                //and don't draw if we're a top down only thing
                if (topDownOnly && !crabulon.TopDownView)
                    return;

                //Get the squish and the origin for the sprite, accounting for top down & sideways anchors
                Vector2 squish = Vector2.One;
                Vector2 origin = spriteOrigin;

                if (crabulon.TopDownView && TopDownTextureAsset != null)
                    origin = topDownSpriteOrigin;
                else if (crabulon.lookingSideways)
                {
                    if (SideTextureAsset != null)
                        origin = sidewaySpriteOrigin;
                    squish = new Vector2(sidewayHorizontalSquish, 1f);
                }

                Texture2D propTexture = TextureAsset.Value;
                Texture2D glowTexture = null;

                    //Get the proper textures based on the angles 
                if (crabulon.TopDownView && TopDownTextureAsset != null)
                    propTexture = TopDownTextureAsset.Value;
                else if (crabulon.lookingSideways && SideTextureAsset != null)
                    propTexture = SideTextureAsset.Value;

                //Get the proper glowmasks based on the angles
                if (GlowmaskAsset != null)
                {
                    glowTexture = GlowmaskAsset.Value;
                    if (crabulon.TopDownView && TopDownGlowmaskAsset != null)
                        glowTexture = TopDownGlowmaskAsset.Value;
                    else if (crabulon.lookingSideways && SideGlowmaskAsset != null)
                        glowTexture = SideGlowmaskAsset.Value;
                }

                //Set the value for the flicker frame (eyes)
                //This is the background the flicker thing flickers from, so we use the flicker baground index here
                int flickerValue = useFlicker ? crabulon.flickerBackground : -1;

                Draw_Inner(spriteBatch, screenPos, drawColor, 1f, origin, squish, propTexture, glowTexture, flickerValue);
                

                //In the case of a flickering prop we draw it once more but this time with a different flicker frame
                if (useFlicker && crabulon.flickerOpacity > 0 && crabulon.flickerFrom != crabulon.flickerBackground)
                {
                    flickerValue = crabulon.flickerFrom;
                    Draw_Inner(spriteBatch, screenPos, drawColor, crabulon.FlickerOpacity, origin, squish, propTexture, glowTexture, flickerValue);
                }
            }

            public void Draw_Inner(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float opacity, Vector2 origin, Vector2 squish, Texture2D texture, Texture2D glowmask, int flickerFrame = -1)
            {
                Vector2 anchor = Anchor;
                float rotation = anchor.AngleTo(position);
                rotation = Math.Clamp(GravityAngle.AngleBetween(rotation), -maxAngleDeviation, maxAngleDeviation);
                if (crabulon.topDownView)
                    rotation += NPC.rotation;

                if (crabulon.lookingSideways && NPC.direction == 1)
                    origin.X = texture.Width - origin.X;

                //Eyes use sheets for the flicker effect
                Rectangle? frame = null;
                if (flickerFrame >= 0)
                {
                    int frameWidth = texture.Width / 6;
                    frame = new Rectangle(frameWidth * flickerFrame, 0, frameWidth - 2, texture.Height);

                    //Correct the origin change to use the frame width instead of the texture width
                    if (crabulon.lookingSideways && NPC.direction == 1)
                        origin.X = frame.Value.Width - (-origin.X + texture.Width);
                }

                spriteBatch.Draw(texture, anchor - screenPos, frame, drawColor * opacity, rotation, origin, NPC.scale * squish * crabulon.spriteSizeMultiplier, Flip, 0);
                if (glowmask != null)
                { 
                    spriteBatch.Draw(glowmask, anchor - screenPos, frame, glowmaskColor * opacity, rotation, origin, NPC.scale * squish * crabulon.spriteSizeMultiplier, Flip, 0);
                }
            }
        }
    }
}
