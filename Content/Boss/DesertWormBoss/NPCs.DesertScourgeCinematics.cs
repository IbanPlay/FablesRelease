using CalamityFables.Content.Dusts;
using CalamityFables.Particles;
using ReLogic.Utilities;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.GameContent.Creative;
using Terraria.Localization;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public partial class DesertScourge : ModNPC, IIntroCardBoss
    {
        #region Music
        internal int previousMusic = 0;
        internal bool quietMusic = false;
        public static int DeathTrack => MusicLoader.GetMusicSlot(CalamityFables.Instance, "Sounds/Music/DesertScourgeFry");
        public static int ProwlerTrack => SoundHandler.UseVanillaMusic ? MusicID.OtherworldlyEerie : MusicLoader.GetMusicSlot(CalamityFables.Instance, "Sounds/Music/DesertScourgeProwl");

        private void SelectMusic()
        {
            if (Main.gameMenu)
                return;

            NPC npc = Main.npc.Where(n => n.active && n.type == Type && n.Distance(Main.LocalPlayer.Center) < 5000f).OrderBy(n => n.Distance(Main.LocalPlayer.Center)).FirstOrDefault();
            if (npc == null || npc == default)
                return;

            DesertScourge scrouge = npc.ModNPC as DesertScourge;

            //When it's idling around, play the tense, otherworldy eerie theme
            if (scrouge.AIState == ActionState.UnnagroedMovement)
            {
                Main.newMusic = ProwlerTrack;
                return;
            }

            //I don't remember what this is doing exactly :(
            if (scrouge.AIState == ActionState.Despawning)
            {
                Main.musicFade[previousMusic] = 1f;
                return;
            }

            //Stop silencing the music after spawning just in case
            if (scrouge.AIState == ActionState.IdleMovement && scrouge.quietMusic)
                scrouge.quietMusic = false;

            //Mute the music so it starts right as DS eats the wriggly little worm
            //Also mute the music if DS was hit while passive. Builds up tension for the jumpscare when it starts attacking
            if (scrouge.AIState == ActionState.CutsceneFightStart && (int)scrouge.SubState < (int)ActionState.CutsceneFightStart_FallBack || scrouge.quietMusic)
            {
                //Keep the music as whatever it was before the fight started
                if (Main.curMusic != Music && Main.curMusic != 0)
                    previousMusic = Main.curMusic;

                //Force the music to not change, and slowly quiet it
                Main.newMusic = previousMusic;
                Main.musicFade[previousMusic] -= 1 / (60f * CutsceneStartTime * 0.75f);

                //Flat out cut it out for the passive fight start / for the last 0.25% of the time
                if (scrouge.quietMusic || Main.musicFade[previousMusic] < 0)
                {
                    Main.newMusic = 0;
                    Main.musicFade[previousMusic] = 0;
                    scrouge.quietMusic = true;
                }
            }

            //If DS was spawned on its own "passively", use the passive track
            else if (scrouge.AIState == ActionState.CutsceneFightStart && scrouge.BecomePassiveAfterSpawnAnim)
            {
                Main.newMusic = ProwlerTrack;
                Main.musicFade[Main.newMusic] = 1;
                return;
            }

            //Regularly set the music
            else if (scrouge.Music != -1)
            {
                //When dying fade into the plonky music
                if (scrouge.AIState == ActionState.CutsceneDeath && (int)scrouge.SubState >= (int)ActionState.CutsceneDeath_Peek && SoundHandler.UsedMusicSource == SoundHandler.MusicSource.Fables)
                {
                    Main.newMusic = DeathTrack;

                    //Start pre-faded
                    float quickFadeTreshold = scrouge.SubState == ActionState.CutsceneDeath_Peek ? Utils.GetLerpValue(0f, 0.8f, scrouge.AttackTimer, true) : 1f;
                    quickFadeTreshold = MathF.Pow(quickFadeTreshold, 2f);

                    Main.musicFade[Main.newMusic] = Math.Max(quickFadeTreshold, Main.musicFade[Main.newMusic]);
                    Main.musicFade[scrouge.Music] = Math.Min(1 - quickFadeTreshold, Main.musicFade[scrouge.Music]);
                }

                //Otherwise play the default track
                else
                {
                    Main.newMusic = scrouge.Music;
                    Main.musicFade[Main.newMusic] = 1;
                }
            }
        }
        #endregion

        #region Spawn
        public static float cameraMagnetizeTime = 1.2f;
        public static float throwCreatureTime = 0.3f;
        public static float eatCreatureTime = 0.5f;

        public static float CutsceneStartTime => cameraMagnetizeTime + throwCreatureTime + eatCreatureTime;

        public NPC FindTargetedGrub(out bool targetFound)
        {
            NPC targetOfMyChunger = null;

            //If invalid npc slot, break
            if (ExtraMemory < 0 || ExtraMemory >= Main.maxNPCs)
                targetFound = false;

            else
            {
                targetFound = true;
                int[] larvaTypes = new int[] { ModContent.NPCType<DeadStormlionLarva>(), ModContent.NPCType<StormlionLarva>(), ModContent.NPCType<Fibsh>() };
                targetOfMyChunger = Main.npc[(int)ExtraMemory];

                //if the chungry meat got killed
                if (!targetOfMyChunger.active || !larvaTypes.Contains(targetOfMyChunger.type))
                {
                    targetFound = false;
                    targetOfMyChunger = null;
                }
            }

            if (!targetFound)
                ExtraMemory = -1;

            return targetOfMyChunger;
        }

        public bool BecomePassiveAfterSpawnAnim => AntiGravityCharge == 1;

        public bool SpawnAnimation(bool inGround, bool onlyInsidePlatforms, ref float rotationAmount, ref float rotationFade, ref float velocityWiggle, ref float velocityWiggleFrequency)
        {
            if (SubState == ActionState.CutsceneFightStart)
            {
                SubState = ActionState.CutsceneFightStart_CameraMagnetize;
                AttackTimer = 0;
            }

            velocityWiggle = 0f;
            NPC grubby = FindTargetedGrub(out bool foundGrubby);
            if (!foundGrubby && (int)SubState <= (int)ActionState.CutsceneFightStart_EatCreature) //Cut spawn anim short if fsr grubby died
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    return true;
                return false;
            }
                    

            Vector2 magnetAnchor = NPC.Center;
            if (foundGrubby)
                magnetAnchor = grubby.Center;

            if (SubState == ActionState.CutsceneFightStart_CameraMagnetize)
            {
                ForceRotation = true;
                NPC.rotation = -MathHelper.PiOver2;

                NPC.velocity = Vector2.Zero;
                AttackTimer += 1 / (60f * cameraMagnetizeTime);

                NPC.Center = grubby.Center + Vector2.UnitY * (1670f - 1000 * AttackTimer);

                TelegraphSand(100f, 3f + AttackTimer * 3f, 0.05f * AttackTimer);

                if (AttackTimer >= 1f)
                {
                    AttackTimer = 0f;
                    SubState = ActionState.CutsceneFightStart_ThrowCreatureAndWait;

                    grubby.noGravity = true;
                    grubby.noTileCollide = true;
                    grubby.velocity = Vector2.Zero;
                    grubby.netUpdate = true;

                    movementTarget = grubby.Center;
                    grubby.velocity = -Vector2.UnitY * 13f;
                    SoundEngine.PlaySound(SlapLarvaIntoTheAir, grubby.Center);
                    SoundEngine.PlaySound(BurrowSound, grubby.Center);

                    PropelGrubEffects(180f, 10);

                    if (Main.LocalPlayer.Distance(grubby.Center) < 1400)
                    {
                        CameraManager.Shake += 20f;
                        CameraManager.HideUI();
                    }
                }
            }

            else if (SubState == ActionState.CutsceneFightStart_ThrowCreatureAndWait)
            {
                ForceRotation = true;
                NPC.rotation = -MathHelper.PiOver2;

                NPC.velocity = Vector2.Zero;
                AttackTimer += 1 / (60f * throwCreatureTime);

                TelegraphSand(100f, 6f + AttackTimer * 1.3f, 0.05f + 0.1f * AttackTimer);

                grubby.velocity.Y += 0.2f;

                if (AttackTimer >= 1)
                {
                    AttackTimer = 0;
                    SubState = ActionState.CutsceneFightStart_EatCreature;
                    NPC.velocity = NPC.DirectionTo(grubby.Center) * 50f;
                    emergeSoundSlot = SlotId.Invalid;
                }
            }

            else if (SubState == ActionState.CutsceneFightStart_EatCreature)
            {
                float slowMo = (float)Math.Pow(AttackTimer, 2f);

                grubby.velocity.Y += 0.25f * slowMo; //Grubby falls
                mandibleJerkiness = 0.7f;
                NPC.velocity *= 0.985f - 0.12f * slowMo; //NPC decelerates
                if (NPC.velocity.Y > -2.7f)
                    NPC.velocity.Y = -2.7f;

                if (SoundEngine.TryGetActiveSound(emergeSoundSlot, out var roaar))
                {
                    emergeSoundTimer += 1 / (60f * 0.6f);
                    if (emergeSoundTimer > 1)
                        emergeSoundTimer = 1;

                    roaar.Position = NPC.Center;
                    roaar.Sound.Pitch = -(float)Math.Pow(emergeSoundTimer, 2f) * 0.2f;
                    roaar.Update();
                }

                AttackTimer += 1 / (60f * eatCreatureTime);
                if (AttackTimer > 1f)
                    AttackTimer = 1f;

                //Fade off the grub's "scary" visuals (screenshake, vignette and desat)
                grubby.ai[3] = 1 - slowMo;

                if ((!inGround || onlyInsidePlatforms) && Main.rand.NextFloat() > (float)Math.Pow(AttackTimer, 0.9f))
                {
                    for (int d = 0; d < 2; d++)
                    {
                        Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(NPC.width / 2, 50f);

                        Dust dus = Dust.NewDustPerfect(dustPos, DustID.Sand, Vector2.UnitY * 8f * Main.rand.NextFloat(0.5f, 1.2f), 0);
                        dus.noGravity = false;
                        dus.velocity = dus.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.2f);
                        dus.scale = Main.rand.NextFloat(0.6f, 2f);
                    }
                }

                if (NPC.Distance(grubby.Center) < (70f + NPC.velocity.Length()))
                {
                    grubby.life = 0;
                    grubby.HitEffect();
                    grubby.active = false;

                    SoundEngine.PlaySound(SoundID.NPCDeath37, grubby.Center);
                    SoundEngine.PlaySound(SpawnBiteSound, grubby.Center);
                    mandibleJerkiness = -0.2f;
                    AttackTimer = 0;
                    SubState = ActionState.CutsceneFightStart_FallBack;

                    NPC.velocity.X = (NPC.Center.X - target.Center.X).NonZeroSign() * 10f;
                    NPC.velocity.Y = -9f;

                    CameraManager.Shake += 35;
                    CameraManager.UnHideUI();

                    if (Main.netMode == NetmodeID.Server)
                        new DesertScourgeIntroAnimPacket(this, grubby).Send(-1, -1, false);
                }
            }

            else if (SubState == ActionState.CutsceneFightStart_FallBack)
            {
                velocityWiggle = 0f;

                NPC.velocity.X *= 0.98f;
                NPC.velocity.Y += 0.4f + 0.3f * AttackTimer / 60f;
                NPC.velocity.Y = Math.Min(NPC.velocity.Y, 20 + DifficultyScale * 4);

                AttackTimer++;

                if (AttackTimer > 120 || target.Center.Y < NPC.Center.Y - 500)
                {
                    if (BecomePassiveAfterSpawnAnim)
                    {
                        AttackTimer = 0;
                        AntiGravityCharge = 0f;
                        SubState = ActionState.UnnagroedMovement;
                        return false;
                    }

                    return true;
                }
            }

            if (foundGrubby && CameraManager.PanMagnet.SetMagnetPositionAndImmunityForEveryone(magnetAnchor, grubby.Center, 1800f, 1.5f, FablesUtils.PolyInOutEasing, 4f))
            {
                CameraManager.PanMagnet.PanProgress += 0.02f;
            }

            return false;
        }

        public void PropelGrubEffects(float width, int dustCounte)
        {
            int x = (int)(NPC.Center.X / 16);
            int y = (int)(NPC.Center.Y / 16);
            int halfWidth = (int)(width / 32);

            for (int i = x - halfWidth; i < x + halfWidth; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    Tile tile = Framing.GetTileSafely(i, y - j);
                    if ((!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || TileID.Sets.Platforms[tile.TileType]) && tile.WallType == 0)
                    {
                        float sideness = (float)Math.Pow((1 - Math.Abs(i - x) / (float)halfWidth), 0.4f);

                        for (int d = 0; d < dustCounte; d++)
                        {
                            Vector2 dustPos = new Vector2(i, y - j) * 16f;
                            dustPos += Vector2.UnitX * Main.rand.NextFloat(16f) + Vector2.UnitY * 16f;

                            Dust dus = Dust.NewDustPerfect(dustPos, DustID.Sand, -Vector2.UnitY * 16f * Main.rand.NextFloat(0.5f, 1.2f), 0);
                            dus.noGravity = false;
                            dus.velocity = dus.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.2f);
                            dus.scale = Main.rand.NextFloat(0.6f, 2f) * sideness;

                        }
                        break;
                    }
                }
            }

        }
        #endregion

        #region Unnagroed Movement
        public void PickUnnagroedMovementRotationAxises(float sinkIn, float furthestDistance = 1500f, float closestDistance = 300f)
        {
            NPC.TargetClosest();
            int direction = (Main.rand.NextBool() ? 1 : -1);

            if (direction == (movementTarget.X - rotationAxis.X).NonZeroSign())
                direction *= -1;

            if (Math.Abs(target.Center.X - NPC.Center.X) > furthestDistance)
                direction = (target.Center.X - NPC.Center.X).NonZeroSign();
            else if (Math.Abs(target.Center.X - NPC.Center.X) < closestDistance && Main.rand.NextBool())
                direction = -(target.Center.X - NPC.Center.X).NonZeroSign();



            if (SubState != ActionState.UnnagroedMovement)
                SubState = SubState == ActionState.UnnagroedMovement_SinkDown ? ActionState.UnnagroedMovement_Idle : ActionState.UnnagroedMovement_SinkDown;

            rotationAxis = NPC.Center + Vector2.UnitX * Main.rand.NextFloat(200f, 300f) * direction + Vector2.UnitY * sinkIn;
            movementTarget = NPC.Center + Vector2.UnitY * sinkIn;
        }

        public bool UnnagroedMovement(bool inGround, bool onlyInsidePlatforms, ref float rotationAmount, ref float rotationFade, ref float velocityWiggle, ref float velocityWiggleFrequency)
        {
            inGround = inGround && !onlyInsidePlatforms;

            if (SubState == ActionState.UnnagroedMovement)
            {
                AttackTimer = 0;
                PickUnnagroedMovementRotationAxises(200f);
                SubState = inGround ? ActionState.UnnagroedMovement_SinkDown : ActionState.UnnagroedMovement_Idle;
            }

            //Despawns after a minute
            velocityWiggle = 0;
            rotationAmount = 0;
            AttackTimer += 1 / (60f * 40f);
            if (AttackTimer >= 1f)
            {
                SubState = ActionState.Despawning;
                return false;
            }

            //Out in the air
            if (SubState == ActionState.UnnagroedMovement_Idle)
            {
                bool pointingDown = Vector2.Dot(Vector2.UnitY, NPC.velocity.SafeNormalize(Vector2.UnitY)) > 0.7f;
                float pointingTowardsAxis = Vector2.Dot(Vector2.UnitX * Math.Sign(movementTarget.X - rotationAxis.X), NPC.velocity.SafeNormalize(-Vector2.UnitY));

                if (inGround && (pointingDown || pointingTowardsAxis < 0f))
                {
                    PickUnnagroedMovementRotationAxises(100f, 1000f, 60f);
                }
            }

            //Inside tiles
            else if (SubState == ActionState.UnnagroedMovement_SinkDown)
            {
                float dot = Vector2.Dot(-Vector2.UnitY, NPC.velocity.SafeNormalize(-Vector2.UnitY));
                float pointingTowardsAxis = Vector2.Dot(Vector2.UnitX * Math.Sign(movementTarget.X - rotationAxis.X), NPC.velocity.SafeNormalize(-Vector2.UnitY));

                bool pointingUp = dot > 0.7f;
                if (!inGround && (pointingUp || pointingTowardsAxis < 0f))
                {
                    PickUnnagroedMovementRotationAxises(200f, 1000f, 60f);
                }
                else if (dot > 0.7f && Collision.SolidCollision(NPC.position - Vector2.UnitY * NPC.height * 1.3f, NPC.width, NPC.height, false))
                    NPC.velocity.Y -= 1f;
                else if (dot < -0.4f)
                    NPC.velocity.Y = Math.Min(NPC.velocity.Y, 7f);
            }

            //Move arounde
            int side = Vector2.Dot(NPC.Center - rotationAxis, new Vector2(0, -1).RotatedBy(NPC.rotation)).NonZeroSign();

            float speed = SubState == ActionState.UnnagroedMovement_Idle ? 40f : 30f;

            //Ideal position is at the side of the player, making it coil around them
            Vector2 goalPosition = rotationAxis + NPC.SafeDirectionFrom(rotationAxis).RotatedBy(side) * (rotationAxis - movementTarget).Length();
            Vector2 goalVelocity = (goalPosition - NPC.Center) / speed;
            NPC.velocity += (goalVelocity - NPC.velocity) / speed;

            //If it was hit, mute the music and go do a fast lunge to start the fight
            if (NPC.life < NPC.lifeMax)
            {
                SubState = ActionState.FastLunge;
                quietMusic = true;
            }

            return false;
        }
        #endregion

        #region Despawn
        public void BurrowDownAndDespawn()
        {
            AntiGravityCharge = 0f;
            NPC.velocity.Y += 1f; //Fall down
            if (NPC.position.Y > Main.worldSurface * 16) //Fall down faster if below the ground
                NPC.velocity.Y += 1f;

            if (NPC.position.Y > Main.rockLayer * 16) //Despawn if below ground
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (Main.netMode == NetmodeID.Server)
                        new DesertScourgeDespawnTailPacket(this).Send(-1, -1, false);
                    RecursivelyDespawnSegments(nextHitbox);
                }

                NPC.active = false;
            }
        }

        public void RecursivelyDespawnSegments(int segmentIndex)
        {
            if (ValidSegment(segmentIndex, NPC))
            {
                Main.npc[segmentIndex].active = false;
                RecursivelyDespawnSegments((int)Main.npc[segmentIndex].ai[1]);
            }
        }
        #endregion

        #region Death
        public float DeathAnimationElectroOrbProgress => (float)Math.Pow(1 - Utils.GetLerpValue(0.1f, 0.9f, AttackTimer, true), 0.65f);

        public void DeathAnimation(bool inGround, bool onlyInsidePlatforms, ref float rotationAmount, ref float rotationFade, ref float velocityWiggle, ref float velocityWiggleFrequency)
        {
            if (SubState == ActionState.CutsceneDeath)
            {
                //Despawn segments
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (Main.netMode == NetmodeID.Server)
                        new DesertScourgeDespawnTailPacket(this).Send(-1, -1, false);

                    RecursivelyDespawnSegments(nextHitbox);
                }

                AttackTimer = 0f;
                if (CameraManager.Shake < 40)
                    CameraManager.Shake += 30;

                SubState = ActionState.CutsceneDeath_Position;
                if (!inGround || onlyInsidePlatforms)
                    SubState = ActionState.CutsceneDeath_TransitionIntoPose;

            }

            //Continue movement until we are inside the floor
            if (SubState == ActionState.CutsceneDeath_TransitionIntoPose)
            {
                BasicSimulateMovement(50f);
                if ((inGround && !onlyInsidePlatforms))
                    SubState = ActionState.CutsceneDeath_Position;
            }

            //Continue movement until peeking out
            else if (SubState == ActionState.CutsceneDeath_Position)
            {
                velocityWiggle = 0;
                rotationAmount = 0;
                CirclePlayer(700, 30, inGround, onlyInsidePlatforms, 0f);

                bool startTheAttack = NPC.Center.Y < target.Center.Y + 50 || Vector2.Dot(-Vector2.UnitY, NPC.velocity) > 0.7f;

                if (!inGround && NPC.Center.Y < target.Center.Y + 600 && startTheAttack)
                {
                    AttackTimer = 0;
                    SubState = ActionState.CutsceneDeath_Peek;
                    NPC.velocity = NPC.velocity.SafeNormalize(-Vector2.UnitY) * 5f;
                    movementTarget = target.Center;
                    SoundEngine.PlaySound(BurnoutChargeSound, NPC.Center);
                }
            }

            else if (SubState == ActionState.CutsceneDeath_Peek)
            {
                velocityWiggle = 0;
                rotationAmount = 0;
                float telegraphTime = 1.8f;

                NPC.velocity *= 0.98f;
                NPC.velocity.Y *= 0.98f;

                float dotToUpright = Vector2.Dot(NPC.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitY);
                bool tiltedTowardsPlayer = 0 < Vector2.Dot(NPC.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitX * (target.Center.X - NPC.Center.X).NonZeroSign());
                float tiltUp = tiltedTowardsPlayer ? 0.1f : 0.6f;
                float maxAngleChange = 0.02f + tiltUp * Utils.GetLerpValue(-1f, -0.7f, dotToUpright, true);
                NPC.velocity = NPC.velocity.ToRotation().AngleLerp(-MathHelper.PiOver2, maxAngleChange).ToRotationVector2() * NPC.velocity.Length(); //Tilt towards facing up


                AttackTimer += 1 / (60f * telegraphTime);

                //OPEN the MANDIBLES!
                mandibleJerkiness = 0.7f * FablesUtils.SineInOutEasing(AttackTimer);

                //Retarget the player (at the start fully retargets but then the retarget gets less and less accurate
                float guaranteedRepositionPercent = Math.Min(1f, 0.8f);
                if (AttackTimer < guaranteedRepositionPercent)
                    movementTarget = target.Center;
                else
                    movementTarget = Vector2.Lerp(movementTarget, target.Center, 0.4f * (1f - (AttackTimer - guaranteedRepositionPercent) / (1 - guaranteedRepositionPercent)));

                if (Main.LocalPlayer.Distance(NPC.Center) < 1000f)
                    CameraManager.Shake = Math.Max(CameraManager.Shake, AttackTimer * 28f);


                if (Main.rand.NextBool(4) && AttackTimer > 0.5f)
                    SpawnElectricArcsAllAcross(0.2f);

                if (AttackTimer >= 1f)
                {
                    AttackTimer = 0f;
                    SubState = ActionState.CutsceneDeath_Backpedal;
                    NPC.velocity = NPC.velocity.SafeNormalize(-Vector2.UnitY) * -4f;
                    electroLoopSlot = SoundEngine.PlaySound(ElectroLoopSound, NPC.Center);
                }
            }

            else if (SubState == ActionState.CutsceneDeath_Backpedal)
            {
                mandibleJerkiness = 0.7f;
                velocityWiggle = 0;
                AttackTimer += 1 / (60f * 0.15f);
                NPC.velocity *= 1f - AttackTimer * 0.1f;

                if (Main.LocalPlayer.Distance(NPC.Center) < 1000f)
                    CameraManager.Shake = Math.Max(CameraManager.Shake, 28f);

                if (Main.rand.NextBool(4))
                    SpawnElectricArcsAllAcross(0.2f);

                if (AttackTimer >= 1f)
                {
                    AttackTimer = 0f;
                    SubState = ActionState.CutsceneDeath_Jump;
                    Vector2 jumpTarget = target.Center;

                    NPC.velocity = FablesUtils.GetArcVel(NPC.Center, jumpTarget, 0.1f, 140f, 340f, 10f, 100f);
                    SoundEngine.PlaySound(ElectroJumpSound, NPC.Center);

                    if (Main.LocalPlayer.Distance(NPC.Center) < 1000f)
                        CameraManager.Quake = Math.Max(CameraManager.Quake, 100f);
                }
            }

            else if (SubState == ActionState.CutsceneDeath_Jump)
            {
                NPC.TargetClosest(false);
                CameraManager.Quake *= 0.95f;

                rotationAmount = 0f;
                velocityWiggle = 0f;

                float jumpTime = 0.6f;

                AttackTimer += 1 / (60f * jumpTime);
                mandibleJerkiness = 0.7f;
                NPC.velocity.Y += 0.1f;

                //Slow down if pointing downwards
                if (Vector2.Dot(NPC.velocity.SafeNormalize(Vector2.UnitY), Vector2.UnitY) > 0.5f)
                    NPC.velocity *= 0.9f;
                //Slow down if too close horizontally
                if (Math.Abs(NPC.Center.X - target.Center.X) < 80f)
                    NPC.velocity.X *= 0.9f;

                else if (Math.Abs(NPC.Center.X - target.Center.X) > 700f)
                    NPC.velocity.X *= 1.015f;

                //Spawn electricity coursing through its body
                if (Main.rand.NextBool(4))
                    SpawnElectricArcsAllAcross(0.3f + 0.3f * AttackTimer);
                if (AttackTimer >= 1f)
                {
                    AttackTimer = 0;
                    SubState = ActionState.CutsceneDeath_SegmentsBurn;
                    burnoutLoopSlot = SoundEngine.PlaySound(BurnoutLoopSound, NPC.Center);
                }
            }

            if (SubState == ActionState.CutsceneDeath_SegmentsBurn)
            {
                float attackTime = 1 / (60f * 1.8f);
                rotationAmount = 0f;
                velocityWiggle = 0f;
                mandibleJerkiness = 0.7f * (DeathAnimationElectroOrbProgress);

                #region Visual spam
                //Spawn fire dust from DS
                if (AttackTimer < 0.7f)
                {
                    for (int i = 0; i < 3 + AttackTimer * 2f; i++)
                    {
                        int burntSegment = Main.rand.Next(segmentPositions.Length);
                        Vector2 dustPos = segmentPositions[burntSegment] + Main.rand.NextVector2Circular(40f, 40f);
                        Dust d = Dust.NewDustPerfect(dustPos, 6, SegmentRotation(burntSegment).ToRotationVector2() * 2f - Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * 1f);
                        d.noGravity = true;
                        d.scale = 1f + (float)Math.Pow(AttackTimer, 2f) * 1f;
                    }
                }

                //Spawn electric dust from DS
                if (AttackTimer < 0.5f && Main.rand.NextBool())
                {
                    for (int i = 0; i < 2; i++)
                    {
                        int electrifiedSegment = (int)(Math.Pow(Main.rand.NextFloat(), 3f) * segmentPositions.Length);
                        Vector2 dustPos = segmentPositions[electrifiedSegment] + Main.rand.NextVector2Circular(40f, 40f);
                        int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();

                        Dust d = Dust.NewDustPerfect(dustPos, dusType, SegmentRotation(electrifiedSegment).ToRotationVector2() * 2f - Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * 5f);
                        d.noGravity = true;
                        d.scale = 0.3f + (float)Math.Pow(AttackTimer, 2f) * 0.3f;
                    }
                }

                //Spawn bone dust crumbling off from DS
                if (Main.rand.NextFloat() < AttackTimer && AttackTimer > 0.4f)
                {
                    for (int i = 0; i < 3 + AttackTimer * 2f; i++)
                    {
                        int dustSegment = Main.rand.Next(segmentPositions.Length);
                        Vector2 dustPos = segmentPositions[dustSegment] + Main.rand.NextVector2Circular(30f, 30f);
                        Dust d = Dust.NewDustPerfect(dustPos, 284, Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * 1f, 0, Color.Tan);
                    }
                }

                //Spawn electricity coursing through its body
                if (Main.rand.NextBool(3) && AttackTimer < 0.7f)
                    SpawnElectricArcsAllAcross(0.6f);

                //Sparks from the orb
                if (Main.rand.NextBool(5) && Main.rand.NextFloat() < DeathAnimationElectroOrbProgress)
                {
                    Vector2 arcStart = NPC.Center + NPC.rotation.ToRotationVector2() * (88f + NPC.velocity.Length() * 8f);
                    Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                    arcStart += direction * DeathAnimationElectroOrbProgress * Main.rand.NextFloat(5f, 50f);

                    Particle sparkZap = new ElectroFireEtincelle(arcStart, direction * 7f * (float)Math.Pow(Main.rand.NextFloat(), 2f), 12);
                    ParticleHandler.SpawnParticle(sparkZap);
                }

                //The electric orb blasts
                if (AttackTimer <= 0.85f && AttackTimer + attackTime > 0.85f)
                {
                    SoundEngine.PlaySound(ElectricFizzleSound, NPC.Center);

                    if (Main.LocalPlayer.Distance(NPC.Center) < 1000f)
                        CameraManager.Quake = Math.Max(CameraManager.Quake, 25f);

                    if (Main.IsItStorming)
                        Main.NewLightning();

                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 sparkPosition = NPC.Center + NPC.rotation.ToRotationVector2() * (88f + NPC.velocity.Length() * 8f);
                        Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                        sparkPosition += direction * DeathAnimationElectroOrbProgress * Main.rand.NextFloat(5f, 50f);

                        Particle spark = new ElectroFireEtincelle(sparkPosition, direction * 18f * (float)Math.Pow(Main.rand.NextFloat(), 2f), 12);
                        ParticleHandler.SpawnParticle(spark);
                    }

                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 smokePosition = NPC.Center + NPC.rotation.ToRotationVector2() * (88f + NPC.velocity.Length() * 8f) + Main.rand.NextVector2Circular(60f, 60f);
                        Vector2 smokeVelocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 2f) + Main.rand.NextVector2Circular(2.6f, 2.6f);
                        Color smokeFireColor = Main.rand.NextBool(3) ? Color.PaleGreen : Color.OrangeRed;
                        Particle sparkZap = new ExplosionSmoke(smokePosition, smokeVelocity, smokeFireColor, Color.DarkGray * 0.2f, Color.Black * 0.4f, Main.rand.NextFloat(1.4f, 3f), 0.03f);
                        ParticleHandler.SpawnParticle(sparkZap);
                    }
                }
                #endregion

                NPC.velocity.Y += 0.1f;
                AttackTimer += attackTime;
                NPC.velocity.X *= 0.985f;

                if (Vector2.Dot(NPC.velocity.SafeNormalize(Vector2.UnitY), Vector2.UnitY) > 0.2f)
                    NPC.velocity.Y *= 0.9f;
                //Slow down if too close horizontally
                if (Math.Abs(NPC.Center.X - target.Center.X) < 80f)
                    NPC.velocity.X *= 0.9f;

                SoundHandler.TrackSoundWithFade(burnoutLoopSlot);

                if (AttackTimer >= 1f)
                {
                    if (SoundEngine.TryGetActiveSound(electroLoopSlot, out var electroLoopSounde))
                        electroLoopSounde.Stop();

                    AttackTimer = 0f;
                    SubState = ActionState.CutsceneDeath_ComedicPause;
                }
            }

            if (SubState == ActionState.CutsceneDeath_ComedicPause)
            {
                rotationAmount = 0f;
                velocityWiggle = 0f;
                NPC.velocity.Y += 0.04f;
                NPC.velocity *= 0.9f;
                float animationTime = 1 / (60f * 0.5f);
                AttackTimer += animationTime;

                for (int i = 0; i < 4 + AttackTimer * 6f; i++)
                {
                    int dustSegment = Main.rand.Next(segmentPositions.Length);
                    Vector2 dustPos = segmentPositions[dustSegment] + Main.rand.NextVector2Circular(30f, 30f);
                    Dust d = Dust.NewDustPerfect(dustPos, 284, Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * 1f, 0, Color.Tan);
                }


                for (int j = 0; j < 4; j++)
                {
                    int dustSegment = Main.rand.Next(segmentPositions.Length);
                    Vector2 dustPos = segmentPositions[dustSegment] + Main.rand.NextVector2Circular(30f, 30f);
                    Vector2 dustVelocity = Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4 * 0.3f);
                    Dust.NewDustPerfect(dustPos, 26, dustVelocity, 0, Color.White, Main.rand.NextFloat(0.2f, 1f));
                }

                //We have to spawn gores one frame early or else they wont display for one frame
                if (AttackTimer < 1f && AttackTimer >= 1f - animationTime)
                    SpawnGores();

                if (AttackTimer >= 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectileDirect(NPC.GetSource_Death(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DesertScourgeFallingSkull>(), 50, 10, Main.myPlayer, (int)segmentDamage[0] + 1, ai2: MathHelper.WrapAngle(NPC.rotation + MathHelper.PiOver2));

                    //Quiet the base music
                    Main.musicFade[Music] = 0;

                    NPC.life = 0;
                    SetDontTakeDamage(false);
                    NPC.checkDead();
                }
            }

            //Manage sounds
            if (electroLoopSlot != SlotId.Invalid)
                SoundHandler.TrackSoundWithFade(electroLoopSlot);

            if (SoundEngine.TryGetActiveSound(electroLoopSlot, out var electroLoopSound))
            {
                electroLoopSound.Position = NPC.Center;
                if (SubState == ActionState.CutsceneDeath_SegmentsBurn)
                {
                    electroLoopSound.Volume = DeathAnimationElectroOrbProgress;
                    electroLoopSound.Update();
                }
            }

            if (SoundEngine.TryGetActiveSound(burnoutLoopSlot, out var burnoutLoopSound))
            {
                burnoutLoopSound.Position = NPC.Center;
                if (SubState == ActionState.CutsceneDeath_SegmentsBurn)
                {
                    burnoutLoopSound.Volume = 1f - 0.5f * (float)Math.Pow(DeathAnimationElectroOrbProgress, 1.3f);
                    burnoutLoopSound.Update();
                }
            }

            //Particles from orb
            if ((int)SubState >= (int)ActionState.CutsceneDeath_Peek && (int)SubState < (int)ActionState.CutsceneDeath_ComedicPause)
            {
                Vector2 ballCenter = NPC.Center + NPC.rotation.ToRotationVector2() * 80f;

                //Add light
                float lightMult = 1f;
                if (SubState == ActionState.CutsceneDeath_Peek)
                    lightMult = AttackTimer;

                Lighting.AddLight(ballCenter, new Vector3(20, 20, 70) * 0.02f * lightMult);

                if (Main.rand.NextBool(8) && SubState != ActionState.CutsceneDeath_Peek)
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

                if (SubState == ActionState.CutsceneDeath_Jump && Main.rand.NextBool(3))
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
        }

        public void SpawnElectricArcsAllAcross(float furthestArcStart = 0.6f)
        {
            int startPositionIndex = Main.rand.Next((int)(segmentPositions.Length * furthestArcStart));
            Vector2 startPosition = segmentPositions[startPositionIndex];

            int startDirection = Main.rand.NextBool() ? 1 : -1;
            int endDirection = Main.rand.NextBool() ? 1 : -1;

            //33% chance to have the arc come from the electro orb
            if (Main.rand.NextBool(3) && startPositionIndex < segmentPositions.Length * 0.2f)
            {
                startPositionIndex = 0;
                startPosition = NPC.Center + NPC.rotation.ToRotationVector2() * 88f;
                endDirection = startDirection * -1;
            }

            Vector2 startSegmentDirection = SegmentRotation(startPositionIndex).ToRotationVector2().RotatedBy(startDirection * MathHelper.PiOver2);
            startPosition += startSegmentDirection.RotatedByRandom(MathHelper.PiOver2 * 0.8f) * Main.rand.NextFloat(10f, 30f);

            //Get the end position
            int endPositionIndex = startPositionIndex + Main.rand.Next(2, Math.Max(3, (int)(segmentPositions.Length * 0.14f)));
            if (endPositionIndex >= segmentPositions.Length)
                endPositionIndex = segmentPositions.Length - 1;

            Vector2 endPosition = segmentPositions[endPositionIndex];

            Vector2 endSegmentDirection = SegmentRotation(endPositionIndex).ToRotationVector2().RotatedBy(endDirection * MathHelper.PiOver2);
            endPosition += endSegmentDirection.RotatedByRandom(MathHelper.PiOver2 * 0.8f) * Main.rand.NextFloat(10f, 30f);

            Particle arc;

            //If the 2 segments are on the same side of the scourge, the arc goes outwards
            if (endDirection == startDirection)
                arc = new ElectricArcPrim(startPosition, endPosition, startPosition + startSegmentDirection * Main.rand.NextFloat(10f, 60f), endPosition + endSegmentDirection * Main.rand.NextFloat(10f, 60f), 2f);

            else
            {
                Vector2 controlA = startPosition + startSegmentDirection * Main.rand.NextFloat(30f, 80f);
                Vector2 controlB = endPosition + endSegmentDirection * Main.rand.NextFloat(30f, 80f);
                arc = new ElectricArcPrim(startPosition, endPosition, controlA, controlB, 2f);
            }

            ParticleHandler.SpawnParticle(arc);
        }

        public void DrawDyingScourge(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            DrawSkeletalScourge(spriteBatch, screenPos, drawColor);

            if (SubState == ActionState.CutsceneDeath_SegmentsBurn)
            {
                float dissapearStart = (1 - (float)Math.Pow(1 - AttackTimer, 0.6f)) * segmentPositions.Length;
                float dissapearEnd = (1 - (float)Math.Pow(1 - AttackTimer, 1.3f)) * segmentPositions.Length;

                Effect effect = Scene["DesertScourgeBurnout"].GetShader().Shader;
                effect.Parameters["gradientStart"].SetValue(dissapearEnd);
                effect.Parameters["gradientEnd"].SetValue(dissapearStart);
                effect.Parameters["noiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise2").Value);

                effect.Parameters["multiplyNoiseScale"].SetValue(0.4f);
                effect.Parameters["lightNoiseScale"].SetValue(0.2f);
                effect.Parameters["lightOpacity"].SetValue(0.8f);
                effect.Parameters["generalProgress"].SetValue(AttackTimer);

                effect.Parameters["segmentCount"].SetValue(segmentPositions.Length);
                effect.Parameters["timeLeftSkeleton"].SetValue(0.3f);
                effect.Parameters["noiseRange"].SetValue(2.8f);
                effect.Parameters["enabled"].SetValue(1f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

                for (int i = segmentPositions.Length - 1; i > 0; i--)
                {
                    GetSegmentTextureAndFrame(i, out Texture2D tex, out Rectangle frame, "Skeleton/DScourgeSkin");
                    Color segmentColor = Lighting.GetColor(SegmentPosition(i).ToTileCoordinates() - new Point(0, 1));

                    effect.Parameters["topIndex"].SetValue(i);
                    effect.Parameters["bottomIndex"].SetValue(i + 1);

                    effect.Parameters["texSize"].SetValue(tex.Size());
                    effect.Parameters["sourceFrame"].SetValue(new Vector4(frame.X, frame.Y, frame.Width, frame.Height));
                    effect.Parameters["lightColor"].SetValue(segmentColor);

                    DrawSegment(spriteBatch, screenPos, segmentColor, i, tex, frame);
                }

                effect.Parameters["topIndex"].SetValue(0);
                effect.Parameters["bottomIndex"].SetValue(1);

                //Skeleton doesnt burn
                effect.Parameters["enabled"].SetValue(0f);

                GetSegmentTextureAndFrame(0, out Texture2D headTex, out Rectangle headFrame, "Skeleton/DScourgeSkeleton");
                DrawHeadWithNoMandibles(spriteBatch, screenPos, drawColor, headTex, headFrame);

                effect.Parameters["enabled"].SetValue(1f);

                GetSegmentTextureAndFrame(0, out headTex, out headFrame, "Skeleton/DScourgeSkin");
                effect.Parameters["texSize"].SetValue(headTex.Size());
                effect.Parameters["sourceFrame"].SetValue(new Vector4(headFrame.X, headFrame.Y, headFrame.Width, headFrame.Height));
                effect.Parameters["lightColor"].SetValue(drawColor);

                DrawHeadWithNoMandibles(spriteBatch, screenPos, drawColor, headTex, headFrame);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                DrawMandiblesWithNoHead(spriteBatch, screenPos, drawColor, headTex, headFrame);
            }

            else
            {
                GetSegmentTextureAndFrame(0, out Texture2D headTex, out Rectangle headFrame, "Skeleton/DScourgeSkeleton");
                DrawHeadWithNoMandibles(spriteBatch, screenPos, drawColor, headTex, headFrame);
                DrawMandiblesWithNoHead(spriteBatch, screenPos, drawColor, headTex, headFrame);
            }

        }

        public void DrawSkeletalScourge(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            //Draw skeleton
            for (int i = segmentPositions.Length - 1; i > 0; i--)
            {
                GetSegmentTextureAndFrame(i, out Texture2D tex, out Rectangle frame, "Skeleton/DScourgeSkeleton");
                Color segmentColor = Lighting.GetColor(SegmentPosition(i).ToTileCoordinates() - new Point(0, 1));
                DrawSegment(spriteBatch, screenPos, segmentColor, i, tex, frame);
            }
        }

        public void SpawnGores()
        {
            if (Main.dedServ)
                return;

            for (int i = segmentPositions.Length - 1; i > 0; i--)
            {
                string goreName = "DScourgeGore_";
                if (i == 1)
                    goreName += "Body1";
                else if (i == segmentPositions.Length - 1)
                    goreName += "Tail";
                else
                    goreName += "Body" + (2 + (i % 3)).ToString();

                goreName += "_" + (1 + (int)segmentDamage[i]).ToString();

                int goreType = Mod.Find<ModGore>(goreName).Type;

                Gore segment = Gore.NewGoreDirect(NPC.GetSource_Death(), segmentPositions[i] * NPC.scale, Vector2.Zero, goreType, NPC.scale);
                segment.rotation = SegmentRotation(i) + MathHelper.PiOver2;
                segment.velocity = Vector2.Zero;

                segment.timeLeft = 140 + (int)(i * 2) + Main.rand.Next(0, 50);

                for (int j = 0; j < 4; j++)
                {
                    Vector2 dustPos = segmentPositions[i] + Main.rand.NextVector2Circular(30f, 30f);
                    Vector2 dustVelocity = Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4 * 0.3f);
                    Dust.NewDustPerfect(dustPos, 284, dustVelocity, 0, Color.Tan, Main.rand.NextFloat(1f, 2f));
                }
            }
        }

        public void LoadGores()
        {
            string[] goreTypes = new string[] { "_Body1", "_Body2", "_Body3", "_Body4", "_Tail" };

            foreach (string goreType in goreTypes)
            {
                for (int i = 1; i <= 4; i++)
                {
                    AutoloadedScourgeSpineGore gore = new AutoloadedScourgeSpineGore("DScourgeGore" + goreType + "_" + i.ToString());
                    Mod.AddContent(gore);
                }
            }
        }
        #endregion

        public void ManageSandstormCoolEffects()
        {
            foreach (Player Player in Main.player.Where(p => p.active && !p.dead))
                Player.buffImmune[BuffID.WindPushed] = true;

            if (!SecondPhase || CreativePowerManager.Instance.GetPower<CreativePowers.FreezeWindDirectionAndStrength>().Enabled || CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().Enabled)
                return;

            Sandstorm.Happening = true;
            Sandstorm.TimeLeft = 60;
            if (Sandstorm.TimeLeft < 2)
                Sandstorm.TimeLeft = 2;

            //Sandstorm ramps up as the fight progresses
            float intendedSandstormPower = 0.2f + 0.8f * Utils.GetLerpValue(1f, 0.3f, (NPC.life / (float)NPC.lifeMax) / (1f - LifePercentForFasterAttacks), true);
            float sandstormPower = Math.Max(MathHelper.Lerp(Sandstorm.Severity, intendedSandstormPower, 0.2f), 0.2f);

            Sandstorm.Severity = Math.Max(Sandstorm.Severity, sandstormPower);
            Sandstorm.IntendedSeverity = Math.Max(Sandstorm.IntendedSeverity, sandstormPower);
            Main.windSpeedTarget = 0.8f;
        }

        #region Spawn card
        private bool playedIntroCard;
        public bool PlayedIntroCard {
            get => playedIntroCard;
            set => playedIntroCard = value;
        }

        public BossIntroCard GetIntroCard {
            get {
                return new BossIntroCard("DesertScourge", (int)(1.9f * 60), NPC.Center.X < Main.LocalPlayer.Center.X,

                Color.GreenYellow * 0.4f,
                Color.Lerp(Color.Goldenrod, Color.LightGray, 0.4f) * 0.8f,
                Color.DarkRed,
                Color.Coral * 0.7f
                )
                { 
                    music = MusicUsedInfo
                };
            }
        }

        public bool ShouldPlayIntroCard => SubState == ActionState.CutsceneFightStart_EatCreature && AttackTimer > 0.6f && !BecomePassiveAfterSpawnAnim;

        public MusicTrackInfo MusicUsedInfo => SoundHandler.UseCalamityMusic ?
            new MusicTrackInfo("Guardian Of The Former Seas", "DM DOKURO") : SoundHandler.UseVanillaMusic ?
            new MusicTrackInfo("Behold the Octoeye", "Jonathan Van Den Wijngaarden") :
            new MusicTrackInfo("The Dessicated Husk", "Sbubby");
        #endregion
    }

    #region Head pop
    public class DesertScourgeFallingSkull : ModProjectile, ICustomDeathMessages
    {
        public override string Texture => AssetDirectory.DesertScourge + "Gores/DScourgeGore_Head1";
        public int Damage => (int)Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Desert Scourge's Skull");
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 2;
            Projectile.timeLeft = 750;
            Projectile.friendly = true;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.hide = true;
            Projectile.penetrate = -1;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (oldVelocity.Y > 0)
            {
                //SCREENSHAKE
                if (Main.LocalPlayer.Distance(Projectile.Center) < 1000)
                    CameraManager.Quake = 20 + 16 * Utils.GetLerpValue(oldVelocity.Y, 0, 3, true);

                //Make the player jump from the shockwave
                if (Main.LocalPlayer.Distance(Projectile.Center) < 300 && Main.LocalPlayer.velocity.Y == 0 && !Main.LocalPlayer.mount.Active)
                    Main.LocalPlayer.velocity.Y = -10;

                SoundEngine.PlaySound(DesertScourge.SkullStompSound, Projectile.Center);

                if (SoundHandler.UsedMusicSource == SoundHandler.MusicSource.Fables && Main.LocalPlayer.WithinRange(Projectile.Center, 2300))
                {
                    SoundEngine.PlaySound(DesertScourge.SongFinalNote);
                    Main.musicFade[DesertScourge.DeathTrack] = 0;
                }

                //Bone dust
                for (int i = 0; i < 30; i++)
                {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(80f, 20f);
                    Vector2 dustVelocity = (Projectile.Center + Vector2.UnitY * 40f).DirectionTo(dustPos) * Main.rand.NextFloat(4f, 8f);
                    Dust d = Dust.NewDustPerfect(dustPos, 284, dustVelocity, 0, Color.Tan, Main.rand.NextFloat(1f, 2f));
                }

                //Sand
                for (int i = 0; i < 20; i++)
                {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(80f, 20f);
                    Vector2 dustVelocity = (Projectile.Center + Vector2.UnitY * 40f).DirectionTo(dustPos) * Main.rand.NextFloat(1f, 3f);
                    Dust d = Dust.NewDustPerfect(dustPos, 32, dustVelocity, 0, Color.Tan, Main.rand.NextFloat(1f, 2f));
                }

                for (int i = 0; i < 16; i++)
                {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(120f, 20f);
                    Vector2 dustVelocity = -Vector2.UnitY * Main.rand.NextFloat(3f, 6f);
                    dustVelocity += Projectile.Center.SafeDirectionTo(dustPos) * 5f;
                    dustPos += Vector2.UnitY * Projectile.height * 1.5f;

                    Color dustColor = new Color(133, 122, 94);

                    SmokeParticle sandsmoke = new SmokeParticle(dustPos, dustVelocity, dustColor, dustColor * 0.4f, Main.rand.NextFloat(1f, 2f), 0.8f, 40, 0.03f);
                    ParticleHandler.SpawnParticle(sandsmoke);
                }

                Collision.HitTiles(Projectile.TopLeft, Vector2.Zero, Projectile.width, (int)(Projectile.height * 2.2f));
            }

            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.damage = 0;

            return false;
        }

        public bool setRotation = false;

        public override void AI()
        {
            if (!setRotation)
            {
                setRotation = true;
                Projectile.rotation = Projectile.ai[2];
            }
                

            if (Projectile.tileCollide)
            {
                Projectile.velocity.Y += 0.65f;
                Projectile.rotation += 0.017f * Math.Sign(Projectile.rotation);

                for (int i = 0; i < 3; i++)
                {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(80f, 40f);
                    Dust d = Dust.NewDustPerfect(dustPos, 284, -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4 * 0.3f) * 1f, 0, Color.Tan);
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= Utils.GetLerpValue(Projectile.velocity.Y, 0, 5, true);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (hit.Damage > 50)
                CombatText.NewText(Projectile.Hitbox, Color.OrangeRed, "+SCOURGED UP", true, true);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = Projectile.timeLeft > 60 ? 1 : Projectile.timeLeft / 60f;

            Texture2D tex = ModContent.Request<Texture2D>(AssetDirectory.DesertScourge + "Gores/DScourgeGore_Head" + Damage.ToString()).Value;
            Vector2 origin = tex.Size() / 2f;

            lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0); ;
            return false;
        }

        public bool CustomDeathMessage(Player player, ref PlayerDeathReason customDeath)
        {
            customDeath.CustomReason = Language.GetText("Mods.CalamityFables.Extras.DeathMessages.DesertScourgeSkullCrush." + Main.rand.Next(1, 6).ToString()).ToNetworkText(player.name, ItemID.Skull.ToString());
            return true;
        }
    }
    #endregion
}
