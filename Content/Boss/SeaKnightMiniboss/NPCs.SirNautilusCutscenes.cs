using Terraria.DataStructures;
using static CalamityFables.Helpers.FablesUtils;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Effects;
using CalamityFables.Particles;

namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public partial class SirNautilus : ModNPC, IIntroCardBoss
    {
        public Vector2 SignathionDummyPosition;

        public CurveSegment JumpOnSignathionUp = new CurveSegment(SineOutEasing, 0f, 0f, 200f);
        public CurveSegment JumpOnSignathionDown = new CurveSegment(SineInEasing, 0.5f, 200f, -120f);
        internal float JumpOnSignathionCurve => PiecewiseAnimation(1 - AttackTimer, new CurveSegment[] { JumpOnSignathionUp, JumpOnSignathionDown });

        public static Vector2 RestPointTileEntityPos;

        #region Ragdoll stuff
        public struct NautilusBone
        {
            public Vector2 position;
            public Vector2 velocity;
            public int spriteVariant;
            public float rotation;
            public float rebuildDelay;

            public NautilusBone(Vector2 pos, Vector2 vel, int var)
            {
                position = pos;
                velocity = vel;
                spriteVariant = var;
                rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                rebuildDelay = (1 - (var - 1) / 6f) * 0.3f;
            }
        }
        public List<NautilusBone> RagdollingBones = new List<NautilusBone>();

        public void ProduceRagdollBones(Vector2 deathDirection)
        {
            RagdollingBones = new List<NautilusBone>();

            for (int i = 1; i < 8; i++)
            {
                NautilusBone ragdollBone = new NautilusBone(NPC.Center + Main.rand.NextVector2Circular(18f, 18f), deathDirection.RotatedByRandom(MathHelper.PiOver4 * 0.5f) * Main.rand.NextFloat(1f, 3f) - Vector2.UnitY * 4f, i);
                RagdollingBones.Add(ragdollBone);
            }
        }

        public void SimulateRagdoll()
        {
            for (int i = 0; i < RagdollingBones.Count; i++)
            {
                NautilusBone bone = RagdollingBones[i];

                bone.velocity.Y += 0.3f;
                if (bone.velocity.Y > 10)
                    bone.velocity.Y = 10;

                bone.velocity = Collision.TileCollision(bone.position - Vector2.One * 4f, bone.velocity, 8, 8, true, true);


                bone.velocity.X *= 0.96f;
                bone.rotation += bone.velocity.X * 0.1f;
                bone.position += bone.velocity;

                RagdollingBones[i] = bone;
            }
        }

        public Vector2 RagdollPartOffset(int index)
        {
            Vector2 offset = Vector2.Zero;

            switch (index)
            {
                case 1: //Head
                    offset += new Vector2(-3, -8);
                    break;
                case 2: //Front arm
                    offset += new Vector2(-12, 17);
                    break;
                case 3: //Body
                    offset += new Vector2(0, 13);
                    break;
                case 4: //Front leg
                    offset += new Vector2(-4, 26);
                    break;
                case 5: //Back leg
                    offset += new Vector2(6, 25);
                    break;
                case 6: //Tail
                    offset += new Vector2(-14, 25);
                    break;
                case 7: //Trident
                    offset += new Vector2(16, 0);
                    break;
            }

            return offset;
        }
        #endregion

        public void SpawnAnimation()
        {
            Vector2 magnetAnchor = NPC.Center;

            if (SubState == ActionState.CutsceneFightStart)
            {
                NPC.dontTakeDamage = true;
                SubState = ActionState.CutsceneFightStart_InitialPose;
                CuteSpeechSystem.Speak(NPC.Center, 3, RegularSpeech);
                AttackTimer = 1f;

                SignathionDummyPosition = NPC.Bottom;
                SignathionFadeOpacity = 0f;

                NPC.TargetClosest(true);

                if (Main.LocalPlayer.Distance(NPC.Center) < 1300)
                    CameraManager.HideUI(4 * 60);

                //Normally the passive nautie should get despawned safely by the tile entity, but if for whatever reason the TE isnt present, despwn it forcefully
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<SirNautilusPassive>())
                        Main.npc[i].active = false;
                }
            }

            if (SubState == ActionState.CutsceneFightStart_InitialPose)
            {
                AttackTimer -= 1 / (60f * 0.9f);
                SignathionDummyPosition = NPC.Bottom;

                float percentAtWhichFadeInStarts = 0.7f;

                if (AttackTimer <= percentAtWhichFadeInStarts)
                {
                    if (AttackTimer + 1 / (60f * 0.9f) > percentAtWhichFadeInStarts)
                    {
                        SoundEngine.PlaySound(SignathionAppearSizzle, NPC.Center);
                    }

                    float fadeCompletion = 1 - AttackTimer / percentAtWhichFadeInStarts;
                    SignathionFadeOpacity = fadeCompletion * 1.5f;
                }

                if (SignathionFadeOpacity > 0.3f && SignathionFadeOpacity < 1f)
                {
                    Vector2 dustPosition = SignathionDummyPosition + Vector2.UnitX * (P1Size.X * 0.9f * Main.rand.NextFloat() - P1Size.X * 0.5f);
                    if (NPC.spriteDirection == -1)
                        dustPosition += Vector2.UnitX * P1Size.X * 0.1f;

                    dustPosition -= Vector2.UnitY * P1Size.Y * (0.5f + 0.4f * Main.rand.NextFloat());

                    Dust cust = Dust.NewDustPerfect(dustPosition, ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, 3f);

                    cust.noGravity = true;
                    cust.velocity *= 0.5f;
                    cust.velocity -= Vector2.UnitY * 4f;
                    cust.velocity += Main.rand.NextVector2Circular(2f, 2f);

                    cust.rotation = Main.rand.NextFloat(0.5f, 1f);
                }



                Point[] frameProgression = new Point[] { new(0, 0), new(0, 0), new(0, 0), new(5, 0), new(5, 2), new(5, 3), new(5, 2), new(5, 2), new(5, 1) };
                Point currentFrame = frameProgression[Math.Min((int)((1 - AttackTimer) * frameProgression.Length), frameProgression.Length - 1)];

                xFrame = currentFrame.X;
                yFrame = currentFrame.Y;

                if (AttackTimer <= 0)
                {
                    SignathionFadeOpacity = 1.5f;
                    AttackTimer = 1f;
                    SubState = ActionState.CutsceneFightStart_JumpOnSignathion;
                    SignathionDummyPosition = NPC.Bottom;

                    NPC.noGravity = true;
                }
            }

            else if (SubState == ActionState.CutsceneFightStart_JumpOnSignathion)
            {

                AttackTimer -= 1 / (60f * 0.75f);

                NPC.position.Y = SignathionDummyPosition.Y - JumpOnSignathionCurve - 130;
                NPC.rotation += 0.137f;

                xFrame = 2;
                yFrame = 0;

                //Get the highest height between the center and the jump position
                magnetAnchor.Y = Math.Min(SignathionDummyPosition.Y - P1Size.Y * 0.5f, NPC.Center.Y);

                //Get the inbetween point
                float startingHeight = SignathionDummyPosition.Y - P1Size.Y * 0.5f;
                float finalHeight = SignathionDummyPosition.Y - 80 - 130 + P1Size.Y * 0.5f;

                magnetAnchor.Y = MathHelper.Lerp(magnetAnchor.Y, MathHelper.Lerp(startingHeight, finalHeight, 1 - AttackTimer), 0.55f);

                //Camera lags a bit
                magnetAnchor.Y = MathHelper.Lerp(magnetAnchor.Y, CameraManager.PanMagnet.magnetPosition.Y, 0.7f);

                if (AttackTimer <= 0)
                {
                    SubState = ActionState.CutsceneFightStart_SignathionScream;
                    NPC.noGravity = false;
                    AttackTimer = 1f;
                    NPC.Bottom = SignathionDummyPosition;

                    CuteSpeechSystem.Speak(NPC.Center, 2, RegularSpeech, 1f);
                    SoundEngine.PlaySound(SignathionSpawnRoar, NPC.Center);

                    if (Main.LocalPlayer.Distance(NPC.Center) < 600)
                        CameraManager.Shake += 14;
                }
            }

            else if (SubState == ActionState.CutsceneFightStart_SignathionScream)
            {
                AttackTimer -= 1 / (60f * 1.35f);

                if (AttackTimer > 0.3f)
                    CameraManager.Shake = 20f * ((AttackTimer - 0.3f) / 0.7f);

                if (AttackTimer <= 0)
                {
                    SubState = ActionState.SlowWalk;

                    NPC.dontTakeDamage = false;
                    AttackTimer = 1f;
                    Stamina = 0f;

                    if (Main.LocalPlayer.Distance(NPC.Center) < 1300 || CameraManager.UIHidden)
                        CameraManager.UnHideUI();
                }

                //NPC.Bottom.Y - 130 is the height of the camera before, with half the height because we were using the NPC's center, and 80 because its the final value of the jump offset
                magnetAnchor.Y = NPC.Bottom.Y - 80 - 130 + NPC.height / 2f;
                //magnetAnchor.Y = SignathionDummyPosition.Y - P1Size.Y * 0.5f;
            }

            if (CameraManager.PanMagnet.SetMagnetPositionAndImmunityForEveryone(magnetAnchor, NPC.Center, 1300))
            {
                CameraManager.PanMagnet.PanProgress += 0.04f;
            }
        }

        #region Phase transition
        public void DismountSignathion()
        {
            Vector2 magnetAnchor = NPC.Center;

            if (SubState == ActionState.CutsceneDismountSig)
            {
                NPC.dontTakeDamage = true;
                CleanseOfEffects();
                SubState = ActionState.CutsceneDismountSig_InitialWait;
                NPC.velocity.Y = 0;

                if (Main.LocalPlayer.Distance(NPC.Center) < 1300)
                    CameraManager.HideUI();

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active)
                    {
                        //Clear lingering spectral pools
                        if (proj.type == ModContent.ProjectileType<SignathionSpectralBolt>() && proj.ai[1] > 1.5f)
                        {
                            proj.timeLeft = 1;
                        }


                        else if (proj.type == ModContent.ProjectileType<SandstoneBoulder>())
                        {
                            //Clear rocks that are waiting to fall
                            if (proj.ai[0] > 0)
                                proj.timeLeft = 1;

                            //Make falling rocks harmless
                            else
                                proj.damage = 0;
                        }
                    }
                }
            }

            if (SubState == ActionState.CutsceneDismountSig_InitialWait)
            {
                NPC.noTileCollide = false;
                AttackTimer -= 1 / (60f * 1f);
                NPC.velocity.X *= 0.7f;
                if (Math.Abs(NPC.velocity.X) <= 0.1)
                    NPC.velocity.X = 0;

                //If not on the floor, wait twice as long before transitioning states
                if ((AttackTimer <= 0 && NPC.velocity.Y == 0) || AttackTimer <= -1f)
                {
                    AttackTimer = 1;
                    SubState = ActionState.CutsceneDismountSig_NautilusJumpingOff;

                    //Save the old position of signathion (for the dissapearing dummy)
                    SignathionDummyPosition = NPC.Bottom;

                    //Change from default signathion size to default nautilus size
                    NPC.position = NPC.Center;
                    NPC.Size = P2Size;
                    NPC.Center = NPC.position - Vector2.UnitY * 30f;

                    //Decide to jump towards the player
                    Vector2 targetPosition = new Vector2(NPC.Center.X + Math.Clamp(Target.Center.X - NPC.Center.X, -200, 200), NPC.Center.Y);
                    NPC.velocity = GetArcVel(NPC.Center, targetPosition, 0.3f, 140, 300);
                    NPC.noTileCollide = true;

                    SoundEngine.PlaySound(SignathionDisappearSizzle, NPC.Center);

                    magnetAnchor = SignathionDummyPosition - Vector2.UnitY * P1Size * 0.5f;
                }
            }

            else if (SubState == ActionState.CutsceneDismountSig_NautilusJumpingOff)
            {
                //Fade sig into nothingness
                SignathionFadeOpacity -= 0.02f;
                if (SignathionFadeOpacity > 1f)
                    SignathionFadeOpacity -= 0.02f;
                if (SignathionFadeOpacity < 0f)
                    SignathionFadeOpacity = 0f;

                if (SignathionFadeOpacity > 0.3f && SignathionFadeOpacity < 1f)
                {
                    Vector2 dustPosition = SignathionDummyPosition + Vector2.UnitX * (P1Size.X * 0.9f * Main.rand.NextFloat() - P1Size.X * 0.5f);
                    if (NPC.spriteDirection == -1)
                        dustPosition += Vector2.UnitX * P1Size.X * 0.1f;

                    dustPosition -= Vector2.UnitY * P1Size.Y * (0.5f + 0.4f * Main.rand.NextFloat());

                    Dust cust = Dust.NewDustPerfect(dustPosition, ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, 3f);

                    cust.noGravity = true;
                    cust.velocity *= 0.5f;
                    cust.velocity -= Vector2.UnitY * 4f;
                    cust.velocity += Main.rand.NextVector2Circular(2f, 2f);

                    cust.rotation = Main.rand.NextFloat(0.5f, 1f);
                }

                //Do a spinny
                NPC.rotation += Math.Sign(NPC.velocity.X) * 0.17f;
                xFrame = 2;
                yFrame = 0;

                //Hit tiles and stomp the anim 
                NPC.noGravity = false;
                if (NPC.velocity.Y > 0 && Collision.CanHitLine(NPC.Center, 1, 1, Target.position, Target.width, Target.height))
                    NPC.noTileCollide = false;

                if (!NPC.noTileCollide && Collision.SolidCollision(NPC.position + NPC.velocity, NPC.width, NPC.height, !CanFallThroughPlatforms().Value))
                {
                    AttackTimer = 0;
                    SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, NPC.Center);
                    if (Main.LocalPlayer.Distance(NPC.Center) < 1000)
                        CameraManager.Shake += 10;

                    Collision.HitTiles(NPC.position, NPC.velocity, NPC.width, NPC.height);
                }


                AttackTimer -= 1 / (60f * 2.2f);
                if (AttackTimer <= 0)
                {
                    NPC.rotation = 0;
                    AttackTimer = 1;
                    Stamina = 0;
                    AIState = ActionState.SlowWalk;

                    if (Main.LocalPlayer.Distance(NPC.Center) < 1300 || CameraManager.UIHidden)
                        CameraManager.UnHideUI();
                    NPC.noTileCollide = false;
                    NPC.dontTakeDamage = false;
                }

                magnetAnchor = Vector2.Lerp(SignathionDummyPosition - Vector2.UnitY * P1Size * 0.5f, NPC.Center, 0.5f + 0.5f * (1 - AttackTimer));
                magnetAnchor = Vector2.Lerp(magnetAnchor, CameraManager.PanMagnet.magnetPosition, 0.7f);
            }
            if (CameraManager.PanMagnet.SetMagnetPositionAndImmunityForEveryone(magnetAnchor, NPC.Center, 1300))
            {
                CameraManager.PanMagnet.PanProgress += 0.02f;
            }
        }
        #endregion

        #region Death
        public void DeathBehavior()
        {
            if (SubState == ActionState.CutsceneDeath)
            {
                if (Main.LocalPlayer.Distance(NPC.Center) < 1000)
                {
                    CameraManager.Shake += 25;
                }

                for (int i = 0; i < 53; i++)
                {
                    Dust cust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(10f, 10f), ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, 1.2f);
                    cust.noGravity = false;

                    Vector2 dustVelocity = NPC.velocity.SafeNormalize(NPC.DirectionFrom(Main.LocalPlayer.Center)) * MathHelper.Clamp(NPC.velocity.Length(), 0.5f, 2f);
                    dustVelocity = dustVelocity.RotatedByRandom(MathHelper.PiOver4 * 0.5f) * Main.rand.NextFloat(2.4f, 3.9f);


                    cust.velocity = dustVelocity + Main.rand.NextVector2Circular(1.3f, 1.3f) + NPC.DirectionFrom(Main.LocalPlayer.Center) * 1f;
                    cust.rotation = 1f;

                    cust.customData = new Vector3(255, 0, 0);
                }

                ProduceRagdollBones(NPC.velocity * 2f + NPC.DirectionFrom(Main.LocalPlayer.Center));

                NPC.life = 1;
                NPC.dontTakeDamage = true;
                NPC.noGravity = true;
                NPC.noTileCollide = true;
                NPC.velocity = Vector2.Zero;
                AttackTimer = 1f;
                CleanseOfEffects();
                SoundEngine.PlaySound(DeathSound, NPC.Center);

                SubState = ActionState.CutsceneDeath_WaitingForReformation;


                movementTarget = Vector2.Zero;
                oldPosition = NPC.Center;
                foreach (var item in TileEntity.ByID)
                {
                    if (item.Value.type == ModContent.TileEntityType<TESirNautilusSpawner>())
                    {
                        movementTarget = item.Value.Position.ToWorldCoordinates();
                        break;
                    }
                }
            }

            if (SubState == ActionState.CutsceneDeath_WaitingForReformation)
            {
                AttackTimer -= 1 / (60f * 2.5f);
                SimulateRagdoll();

                if (movementTarget != Vector2.Zero)
                {
                    NPC.Center = Vector2.Lerp(oldPosition, movementTarget, PolyInOutEasing(Math.Clamp(1 - AttackTimer, 0f, 1f), 4));
                }

                if (AttackTimer <= 0f)
                {
                    SoundEngine.PlaySound(RattlingBonesSound, NPC.Center);
                    SubState = ActionState.CutsceneDeath_ShakingBones;
                    AttackTimer = 1f;
                }
            }

            if (SubState == ActionState.CutsceneDeath_ShakingBones)
            {
                AttackTimer -= 1 / (60f * 0.5f);
                SimulateRagdoll();

                if (AttackTimer <= 0f)
                {
                    SoundEngine.PlaySound(BonesReformSound, NPC.Center);
                    SubState = ActionState.CutsceneDeath_ReformBones;
                    AttackTimer = 1f;
                }
            }

            if (SubState == ActionState.CutsceneDeath_ReformBones)
            {
                AttackTimer -= 1 / (60f * 1.9f);
                NPC.position.Y -= (float)Math.Pow(1 - AttackTimer, 0.3f) * 2f;

                for (int i = 0; i < RagdollingBones.Count; i++)
                {
                    NautilusBone bone = RagdollingBones[i];

                    //Keep simulating bones that arent being rebuilt yet
                    if (1 - AttackTimer < bone.rebuildDelay)
                    {
                        bone.velocity.Y += 0.3f;
                        if (bone.velocity.Y > 10)
                            bone.velocity.Y = 10;

                        bone.velocity = Collision.TileCollision(bone.position - Vector2.One * 4f, bone.velocity, 8, 8, true, true);


                        bone.velocity.X *= 0.96f;
                        bone.rotation += bone.velocity.X * 0.1f;
                        bone.position += bone.velocity;
                    }

                    else
                    {
                        Vector2 oldBonePosition = bone.position;
                        Vector2 idealBonePosition = NPC.Center + RagdollPartOffset(bone.spriteVariant);

                        bone.position = Vector2.Lerp(oldBonePosition, idealBonePosition, 0.1f + 0.3f * (1 - AttackTimer));

                        bone.rotation = MathHelper.Lerp(bone.rotation, 0f, 0.3f);
                    }

                    RagdollingBones[i] = bone;
                }

                if (Main.rand.NextFloat(2f) < 1 - AttackTimer + 0.5f)
                {
                    Color dustColor = Main.rand.NextBool(3) ? Color.DeepSkyBlue : Color.Turquoise;

                    Dust glow = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(30f, 50f), 43, -Vector2.UnitY * Main.rand.NextFloat(1f, 2f) + NPC.velocity * 0.4f, 200, dustColor, Main.rand.NextFloat(0.57f, 1f));
                    glow.noGravity = true;
                }

                if (AttackTimer <= 0)
                {
                    AttackTimer = 1f;
                    //SubState = ActionState.SlowWalk;
                    for (int i = 0; i < 24; i++)
                    {
                        Color dustColor = Main.rand.NextBool(3) ? Color.DeepSkyBlue : Color.Turquoise;
                        Vector2 dustPosition = NPC.Center + Main.rand.NextVector2Circular(50f, 50f);

                        Dust glow = Dust.NewDustPerfect(dustPosition, 43, dustPosition.DirectionFrom(NPC.Center) * Main.rand.NextFloat(1f, 2f) + NPC.velocity * 0.4f, 200, dustColor, Main.rand.NextFloat(0.57f, 1f));
                        glow.noGravity = true;


                        Dust cust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(10f, 10f), ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, 1.2f);
                        cust.noGravity = false;

                        cust.velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 3f);
                        cust.rotation = 1f;
                    }

                    SoundEngine.PlaySound(BonesSutureSound, NPC.Center);


                    NPC.life = 0;
                    NPC.dontTakeDamage = false;
                    NPC.checkDead();

                    //NPC.active = false;
                    /*
                    Main.BestiaryTracker.Kills.RegisterKill(NPC);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        DropAttemptInfo dropAttemptInfo = default(DropAttemptInfo);
                        dropAttemptInfo.player = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
                        dropAttemptInfo.npc = NPC;
                        dropAttemptInfo.IsExpertMode = Main.expertMode;
                        dropAttemptInfo.IsMasterMode = Main.masterMode;
                        dropAttemptInfo.IsInSimulation = false;
                        dropAttemptInfo.rng = Main.rand;
                        DropAttemptInfo info = dropAttemptInfo;
                        Main.ItemDropSolver.TryDropping(info);
                    }
                    */
                }
            }
        }
        #endregion

        public void CleanseOfEffects()
        {
            for (int i = 0; i < NPC.maxBuffs; i++)
            {
                NPC.buffTime[i] = 0;
                NPC.buffType[i] = 0;
            }
        }

        #region Drawing

        public void MountSignathionCinematicsDrawing(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float sizeMult)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && SubState == ActionState.CutsceneFightStart)
                return;

            //Use a dummy signathion and a regular jumping nautie
            if (SubState == ActionState.CutsceneFightStart_InitialPose || SubState == ActionState.CutsceneFightStart_JumpOnSignathion)
            {
                Color sigColor = Lighting.GetColor((SignathionDummyPosition - Vector2.UnitY * P1Size.X * 0.5f).ToTileCoordinates());
                DrawSignathion(spriteBatch, screenPos, sigColor, SignathionDummyPosition, sizeMult);
                DrawNautilus(spriteBatch, screenPos, drawColor, sizeMult);
            }

            //Regularly draw nautilus and signathion riding
            if (SubState == ActionState.CutsceneFightStart_SignathionScream)
            {
                NautilusRidingSheet = NautilusRidingSheet ?? ModContent.Request<Texture2D>(Texture + "_Riding");
                NautilusRidingOverSheet = NautilusRidingOverSheet ?? ModContent.Request<Texture2D>(Texture + "_RidingOver");

                Rectangle ridingNautilusFrame = new Rectangle(xFrame * (nautieRiderFrameWidth + 2), yFrame * (nautieRiderFrameHeight + 2), nautieRiderFrameWidth, nautieRiderFrameHeight);
                Vector2 offset = GetRidingNautilusOffset();

                DrawRidingNautilus(screenPos, drawColor, NautilusRidingSheet.Value, ridingNautilusFrame, offset, sizeMult);
                DrawSignathion(spriteBatch, screenPos, drawColor, NPC.Bottom, sizeMult);
                DrawRidingNautilus(screenPos, drawColor, NautilusRidingOverSheet.Value, ridingNautilusFrame, offset, sizeMult);
            }
        }

        public void DismountSignathionCinematicsDrawing(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float sizeMult)
        {
            //Regular drawing
            if (SubState == ActionState.CutsceneDismountSig_InitialWait)
            {
                NautilusRidingSheet = NautilusRidingSheet ?? ModContent.Request<Texture2D>(Texture + "_Riding");
                NautilusRidingOverSheet = NautilusRidingOverSheet ?? ModContent.Request<Texture2D>(Texture + "_RidingOver");

                Rectangle ridingNautilusFrame = new Rectangle(xFrame * (nautieRiderFrameWidth + 2), yFrame * (nautieRiderFrameHeight + 2), nautieRiderFrameWidth, nautieRiderFrameHeight);
                Vector2 offset = GetRidingNautilusOffset();

                DrawRidingNautilus(screenPos, drawColor, NautilusRidingSheet.Value, ridingNautilusFrame, offset, sizeMult);
                DrawSignathion(spriteBatch, screenPos, drawColor, NPC.Bottom, sizeMult);
                DrawRidingNautilus(screenPos, drawColor, NautilusRidingOverSheet.Value, ridingNautilusFrame, offset, sizeMult);
            }

            //Draw nautilus as normal, draw a "fake" signathion stuck in a dummy position.
            if (SubState == ActionState.CutsceneDismountSig_NautilusJumpingOff)
            {
                Color sigColor = Lighting.GetColor((SignathionDummyPosition - Vector2.UnitY * P1Size.X * 0.5f).ToTileCoordinates());
                DrawSignathion(spriteBatch, screenPos, sigColor, SignathionDummyPosition, sizeMult);
                DrawNautilus(spriteBatch, screenPos, drawColor, sizeMult);
            }
        }

        public void DrawNautilusesRagdollBones(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float sizeMult)
        {
            for (int i = RagdollingBones.Count - 1; i >= 0; i--)
            {
                NautilusBone bone = RagdollingBones[i];
                Texture2D tex = ModContent.Request<Texture2D>(AssetDirectory.SirNautilus + "SirNautilus_Bone" + bone.spriteVariant.ToString()).Value;

                Vector2 offset = Vector2.Zero;
                if (SubState == ActionState.CutsceneDeath_ShakingBones ||
                    (SubState == ActionState.CutsceneDeath_ReformBones && bone.rebuildDelay > (1 - AttackTimer)))
                {
                    float shakeStrenght = SubState == ActionState.CutsceneDeath_ShakingBones ? (1 - AttackTimer) : 1f;
                    offset += Main.rand.NextVector2Circular(1f, 1f) * 4f * shakeStrenght;
                }

                Color boneColor = Lighting.GetColor(bone.position.ToTileCoordinates());

                Main.EntitySpriteDraw(tex, (bone.position + offset - screenPos) * sizeMult, null, boneColor, bone.rotation, tex.Size() / 2f, 1f * sizeMult, 0, 0);
            }
        }

        public void DrawNautiluseDeathShine(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float sizeMult)
        {
            Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;
            Texture2D bloommmmm = AssetDirectory.CommonTextures.BloomCircle.Value;
            Texture2D overlay = ModContent.Request<Texture2D>(AssetDirectory.SirNautilus + "NautilusSilouette").Value;

            float shineOpacity = Math.Clamp((0.7f - AttackTimer) / 0.7f, 0f, 1f);
            Vector2 shienCenter = NPC.Center + Vector2.UnitY * 10f;

            Main.EntitySpriteDraw(bloommmmm, (shienCenter - screenPos) * sizeMult, null, Color.Goldenrod with { A = 0 } * shineOpacity * 0.3f, MathHelper.PiOver2, bloommmmm.Size() / 2, NPC.scale * 0.3f * sizeMult, 0, 0);
            Main.EntitySpriteDraw(bloommmmm, (shienCenter - screenPos) * sizeMult, null, Color.White with { A = 0 } * shineOpacity * 0.1f, MathHelper.PiOver2, bloommmmm.Size() / 2, NPC.scale * 0.8f * sizeMult, 0, 0);

            Vector2 lensFlareScale = new Vector2(0.8f, 2f);

            Main.EntitySpriteDraw(lensFlare, (shienCenter - screenPos) * sizeMult, null, Color.Gold with { A = 0 } * shineOpacity * 0.4f, MathHelper.PiOver2, lensFlare.Size() / 2, lensFlareScale * 1.3f * sizeMult, 0, 0);
            Main.EntitySpriteDraw(lensFlare, (shienCenter - screenPos) * sizeMult, null, Color.White with { A = 0 } * shineOpacity, MathHelper.PiOver2, lensFlare.Size() / 2, lensFlareScale * sizeMult, 0, 0);

            Color silouetteColor = Color.Lerp(Color.Gold, Color.White, (float)Math.Pow(shineOpacity, 4f));
            shineOpacity = (float)Math.Pow(shineOpacity, 5f);
            Main.EntitySpriteDraw(overlay, (NPC.Center + Vector2.UnitY * 3f - screenPos) * sizeMult, null, silouetteColor * shineOpacity, 0f, overlay.Size() / 2, 1.01f * NPC.scale * sizeMult, 0, 0);

        }
        #endregion


        #region Ambience mood
        public static float SignathionVisualInfluence = 1f;
        public static float TorchLightColorShift => (1 - SignathionVisualInfluence) * 0.6f;

        private void UpdateSignathionLightInfluence()
        {
            bool signathionPresent = true;

            if (!Main.gameMenu)
            {
                //Check for nautilus
                NPC npc = Main.npc.Where(n => n.active && n.type == Type && n.Distance(Main.LocalPlayer.Center) < 3000f).OrderBy(n => n.Distance(Main.LocalPlayer.Center)).FirstOrDefault();
                if (npc != null && npc != default(NPC))
                {
                    SirNautilus naut = npc.ModNPC as SirNautilus;
                    if (!naut.IsSignathionPresent)
                        signathionPresent = false;
                }
            }

            if (signathionPresent)
            {
                SignathionVisualInfluence += 0.08f;
                if (SignathionVisualInfluence > 1f)
                    SignathionVisualInfluence = 1f;
            }
            else
            {
                SignathionVisualInfluence -= 0.02f;
                if (SignathionVisualInfluence < 0f)
                    SignathionVisualInfluence = 0f;
            }
        }
        
        private void SpawnAmbienceParticles()
        {
            return;

            for (int i = 0; i < 1; i++)
            {
                Vector2 particleSpawnPos = new Vector2(Main.rand.NextFloat(0f, Main.screenWidth), Main.screenHeight + Main.rand.NextFloat(-30f, 200f)) + Main.screenPosition;

                float gasParallax = Main.rand.NextFloat(0f, 0.8f);
                float gasSize = Main.rand.NextFloat(0.8f, 1.2f) + (0.8f - gasParallax) * 1f;
                Vector2 particleVelocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 6f) + Vector2.UnitX * Main.rand.NextFloat(0f, 1f);

                Particle embers = new ScreenGhostlyEmbers(particleSpawnPos, particleVelocity, gasSize, 0.01f, gasParallax);
                ParticleHandler.SpawnParticle(embers);
            }
        }
        #endregion

        #region Music
        public static MusicLayerHandler secondPhaseMusicLayer;
        public static int signathionMusic;
        public static int nautilusMusic;
        public static float transitionFade;

        private void ManageMusicFade()
        {
            if (Main.gameMenu)
                return;

            NPC npc = Main.npc.Where(n => n.active && n.type == Type && n.Distance(Main.LocalPlayer.Center) < 5000f).OrderBy(n => n.Distance(Main.LocalPlayer.Center)).FirstOrDefault();
            if (npc == null || npc == default)
                return;

            SirNautilus nautilus = npc.ModNPC as SirNautilus;

            //Cut out the music when dying
            if (nautilus.AIState == ActionState.CutsceneDeath)
            {
                Main.musicFade[Main.curMusic] = 0;
                Main.curMusic = 0;
                Main.newMusic = 0;
                return;
            }

            //Quick fade in for the spawn music
            if (nautilus.AIState == ActionState.CutsceneFightStart && Main.newMusic == signathionMusic)
            {
                //Silence
                if ((int)nautilus.SubState < (int)ActionState.CutsceneFightStart_SignathionScream)
                {
                    //The initial pose lasts for 0.9 seconds, and the jump on signathion lasts for 0.75 seconds
                    float spawnAnimationDuration = 0.9f + 0.75f;

                    float spawnTime = 1 - nautilus.AttackTimer;
                    if (nautilus.SubState == ActionState.CutsceneFightStart_InitialPose)
                        spawnTime *= 0.9f / spawnAnimationDuration;
                    else if (nautilus.SubState == ActionState.CutsceneFightStart_JumpOnSignathion)
                    {
                        spawnTime *= 0.75f / spawnAnimationDuration;
                        spawnTime += 0.9f / spawnAnimationDuration;
                    }

                    Main.musicFade[Main.curMusic] = MathF.Pow(1 - spawnTime, 0.5f);
                    Main.newMusic = Main.curMusic;


                    //int sealedChamberIndex = MusicLoader.GetMusicSlot(Mod, "Sounds/Music/SealedChamber");
                    //Main.musicFade[sealedChamberIndex] = Math.Max(0, Main.musicFade[sealedChamberIndex] - 0.03f);
                }
                else
                    Main.musicFade[signathionMusic] = 1;
            }

            //Change music in phase 1
            if (!nautilus.IsSignathionPresent && nautilus.Music == signathionMusic)
            {
                nautilus.Music = nautilusMusic;
                transitionFade = 0f;
            }

            //Have both versions of the track fade in perfectly with one another
            if (Main.curMusic == nautilusMusic && transitionFade < 1f)
            {
                float fadingInVolume = (float)Math.Sqrt(transitionFade);
                float fadingOutVolume = (float)Math.Sqrt(1 - transitionFade);

                Main.musicFade[nautilusMusic] = fadingInVolume;
                Main.musicFade[signathionMusic] = fadingOutVolume;

                transitionFade += 1 / (60f * 0.5f);
            }
        }
        #endregion

        #region Spawn card
        private bool playedIntroCard;
        public bool PlayedIntroCard {
            get => playedIntroCard;
            set => playedIntroCard = value;
        }

        public BossIntroCard GetIntroCard {
            get {
                return new BossIntroCard("SirNautilus", (int)(1.5f * 60), NPC.Center.X < Main.LocalPlayer.Center.X,

                Color.DarkGoldenrod * 0.4f,
                Color.DarkOrange,
                Color.Cyan * 0.7f,
                Color.MediumSpringGreen * 0.7f
                )
                {
                    music = MusicUsedInfo
                };
            }
        }

        public MusicTrackInfo? MusicUsedInfo => SoundHandler.UseCalamityMusic ?
            null : SoundHandler.UseVanillaMusic ?
            null :
            new MusicTrackInfo("Beating A Dead Horseman", "Sbubby & Moonburn");

        public bool ShouldPlayIntroCard => SubState == ActionState.CutsceneFightStart_SignathionScream;
        #endregion
    }

    public class ScreenGhostlyEmbers : ScreenParticle
    {
        public override string Texture => AssetDirectory.SirNautilus + "SpectralWaterDust";

        public int counter;
        public float Spin;

        public override bool SetLifetime => false;
        public override int FrameVariants => 3;
        public override bool Important => true;


        public ScreenGhostlyEmbers(Vector2 position, Vector2 velocity, float scale, float rotationSpeed = 0f, float parallax = 0f)
        {
            Position = position;
            Velocity = velocity;
            Scale = scale;
            counter = Main.rand.Next(80);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Variant = Main.rand.Next(3);

            OriginalScreenPosition = Main.screenPosition;
            ParallaxRatio = parallax;
            WrapHorizontal = true;
        }

        public override void Update()
        {
            //Fly up
            if (Velocity.Y > -2 && counter > 140)
                Velocity.Y -= 0.05f * Math.Min(1f, (counter - 140) / 60f);

            //Fade out
            if (counter > 100)
            {
                Scale -= 0.01f;
                counter += 2;
            }
            else
            {
                Scale *= 0.985f;
                counter += 4;
            }
            //Dissapear
            if (counter >= 255)
                Kill();

            //Slowly fade in
            float opacity = 0.8f + 0.15f * Math.Min(1f, counter / 90f);
            opacity *= 0.4f + 0.6f * (1 - ParallaxRatio);

            Color = Color.White * opacity;
            Color.A = (byte)(Math.Max(Color.A * 0.8f, 250)); //Color is always at least a bit glowy

            //Fade off in terms of opacity
            if (counter > 150)
                Color *= (float)Math.Pow(1 - (counter - 150) / 105f, 1.2f);

            //Slightly turn into the bg color
            Color backgroundColor = Lighting.GetColor(Position.ToTileCoordinates());
            Color = Color.Lerp(Color, Color.MultiplyRGBA(backgroundColor), 0.7f);

            //Start bright and fade away
            if (counter < 150)
            {
                byte oldA = Color.A;
                Color *= 1 + 2f * (float)Math.Pow(1 - counter / 150f, 4);
                Color.A = oldA;
            }
        }
    }

    //public interface IFiniteStateAttack
    //{
    //Initialization behavior
    //Stamina
    //States it can switch to
    //Selection percentage based on a delegate
    //Contact damage
    //Behavior
    //Gravity & tilecollide
    //Framing
    //Drawing

    //}

    //(from 2025) what is buddy talking abt
}
