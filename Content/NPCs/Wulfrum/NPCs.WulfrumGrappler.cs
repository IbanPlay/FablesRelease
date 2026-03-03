using CalamityFables.Content.Items.Food;
using CalamityFables.Content.Items.Wulfrum;
using System.IO;
using Terraria.GameContent.Bestiary;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.NPCs.Wulfrum
{
    [ReplacingCalamity("WulfrumDrone")]
    public class WulfrumGrappler : ModNPC, ISuperchargable
    {
        public static readonly SoundStyle LandingSound = new SoundStyle(SoundDirectory.Wulfrum + "WulfrumGrapplerLand") { Volume = 0.7f, MaxInstances = 0 };
        public static readonly SoundStyle HopSound = new SoundStyle(SoundDirectory.Wulfrum + "WulfrumGrapplerHop") { MaxInstances = 1, Identifier = "Grappler", SoundLimitBehavior = SoundLimitBehavior.IgnoreNew};
        public static readonly SoundStyle HandGrabSound = new SoundStyle(SoundDirectory.Wulfrum + "WulfrumGrapplerGrab") { Volume = 0.7f };
        public static readonly SoundStyle LaunchSound = new SoundStyle(SoundDirectory.Wulfrum + "WulfrumGrapplerLaunch");
        public static readonly SoundStyle SlamSound = new SoundStyle(SoundDirectory.Wulfrum + "WulfrumGrapplerSlam");
        public static readonly SoundStyle AngryOuch = new SoundStyle(SoundDirectory.Wulfrum + "WulfrumGrapplerAngeredNoise");


        public override string Texture => AssetDirectory.WulfrumNPC + Name;
        public Player target => Main.player[NPC.target];

        public ref float AttackCharge => ref NPC.ai[0];
        public AttackStates AttackState {
            get {
                return (AttackStates)NPC.ai[1];
            }

            set {
                NPC.ai[1] = (float)value;
            }
        }
        public ref float HandState => ref NPC.ai[2];
        public ref float ArmStaticLenght => ref NPC.ai[3];

        public enum AttackStates
        {
            Chasing,
            ShootingArm,
            SwingingAroundHand,
            ZoomingAround,
            GroundPound,
            TryingToGrabMagnetizer,
            SwingingAroundMagnetizer
        }

        public Vector2 handPositionOverride;
        public Vector2 handPositionTarget;

        public int yFrame;
        public float handFrameCounter;
        public int handFrame;
        public float handRotation;

        public float squishyY;
        public bool hasYelped = false;
        public float MoveSpeedMultiplier => MathHelper.Lerp(0.95f, 1.05f, NPC.whoAmI * 13.72f % 1f);

        public NPC magnetToSwingAround = default;

        public float arcSpeed;
        public bool hasGrabbedOntoMagnet = false;

        public ref float ArmRotation => ref NPC.localAI[0];
        public ref float ArmGrabbyStyle => ref NPC.localAI[1];
        public ref float FlipNormal => ref NPC.localAI[2];

        public Vector2 GetLinkPosition(Vector2 rotationOrigin) => rotationOrigin - Vector2.UnitY.RotatedBy(NPC.rotation) * (12f + NPC.height / 2f) * squishyY;
        public Vector2 GetHandPosition(Vector2 rotationOrigin) => GetLinkPosition(rotationOrigin) + (ArmRotation + NPC.rotation + MathHelper.PiOver2).ToRotationVector2() * -17f;

        private bool _supercharged = false;
        public bool IsSupercharged {
            get => _supercharged;
            set {
                if (_supercharged != value)
                {
                    _supercharged = value;
                    NPC.netUpdate = true;
                }
            }
        }
        public bool Aggroed => IsSupercharged || NPC.life < NPC.lifeMax;


        public static int BannerType;
        public static AutoloadedBanner bannerTile;
        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Wulfrum Grappler", AssetDirectory.WulfrumBanners, out bannerTile);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Grappler");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new(0)
            {
                SpriteDirection = 1
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            Main.npcFrameCount[Type] = 6;

            NPCID.Sets.TrailCacheLength[Type] = 6;
            NPCID.Sets.TrailingMode[Type] = 0;
            FablesSets.WulrumNPCs[Type] = true;
            bannerTile.NPCType = Type;


            if (Main.dedServ)
                return;
            for (int i = 0; i < 4; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("WulfrumGrapplerGore" + i.ToString()).Type] = true;
        }

        public override void SetDefaults()
        {
            ArmRotation = 0f;
            AIType = -1;
            NPC.aiStyle = -1;
            NPC.damage = 13;
            NPC.width = 42;
            NPC.height = 42;
            NPC.defense = 2;
            NPC.lifeMax = 44;
            NPC.knockBackResist = 0.35f;
            NPC.value = Item.buyPrice(0, 0, 1, 15);
            NPC.HitSound = SoundDirectory.CommonSounds.WulfrumNPCHitSound;
            NPC.DeathSound = SoundDirectory.CommonSounds.WulfrumNPCDeathSound;
            Banner = NPC.type;
            BannerItem = BannerType;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            if (Main.GameModeInfo.EnemyDamageMultiplier >= 3f)
                NPC.damage = (int)(NPC.damage * 0.75f);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.DayTime,

                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.WulfrumGrappler")
            });
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(magnetToSwingAround == default ? -1 : magnetToSwingAround.whoAmI);

            writer.Write(arcSpeed);
            writer.Write(ArmRotation);
            writer.Write(ArmGrabbyStyle);
            writer.Write(FlipNormal);
            writer.Write((byte)hasGrabbedOntoMagnet.ToInt());
            writer.Write((byte)_supercharged.ToInt());

            writer.WriteVector2(handPositionOverride);
            writer.WriteVector2(handPositionTarget);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int magnetToSwingAroundIndex = reader.ReadInt32();
            magnetToSwingAround = magnetToSwingAroundIndex >= 0 ? Main.npc[magnetToSwingAroundIndex] : default;

            arcSpeed = reader.ReadSingle();
            ArmRotation = reader.ReadSingle();
            ArmGrabbyStyle = reader.ReadSingle();
            FlipNormal = reader.ReadSingle();
            hasGrabbedOntoMagnet = reader.ReadByte() != 0;
            _supercharged = reader.ReadByte() != 0;

            handPositionOverride = reader.ReadVector2();
            handPositionTarget = reader.ReadVector2();
        }

        public override void AI()
        {
            //Prevent the on-spawn "stuck" issue by forcing it to have an initial direction
            if (NPC.direction == 0)
            {
                NPC.TargetClosest(false);
                NPC.direction = (target.Center.X - NPC.Center.X).NonZeroSign();
            }

            //Reset nogravity (it gets it set when doing some funky actions)
            NPC.noGravity = false;
            NPC.TargetClosest(false);
            float distanceToTargetX = Math.Abs(NPC.Center.X - target.Center.X);

            //If hurt and hasnt screamed out, scream OUT!
            if (NPC.life < NPC.lifeMax && !hasYelped)
            {
                hasYelped = true;
                SoundEngine.PlaySound(AngryOuch, NPC.Center);
            }

            ComboLogic();

            //Regular rolely polley movement
            if (AttackState == AttackStates.Chasing)
            {
                Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
                NPC.noTileCollide = false;
                NPC.noGravity = false;

                //Tries to get its arm back into regular position (If after an attack or something)
                ResetArmPosition();

                float flipTime = (!Aggroed && target.GetPlayerFlag("WulfrumAmbassador")) ? 6f : 3f;

                //Charge up towards doing its flippy
                AttackCharge += 1 / (60f * flipTime);
                if (AttackCharge >= 1 && NPC.velocity.Y == 0)
                {
                    //Do a flippy (Well, throw its hand out to try to grab onto something)
                    AttackCharge = 0;

                    if (!TryToFindMagnetizer())
                    {
                        AttackState = AttackStates.ShootingArm;

                        if (Aggroed || distanceToTargetX < 300 || Collision.CanHitLine(target.position, target.width, target.height, NPC.position, NPC.width, NPC.height))
                            NPC.direction = (target.Center.X - NPC.Center.X).NonZeroSign(); //Face the player if got less than max hp
                    }

                    else
                    {
                        hasGrabbedOntoMagnet = false;
                        AttackState = AttackStates.TryingToGrabMagnetizer;
                        NPC.direction = (magnetToSwingAround.Center.X - NPC.Center.X).NonZeroSign();
                        NPC.velocity.Y = -9f;
                    }


                    //Set the hand position override to be the default hand position so the transition looks normal
                    handPositionOverride = GetHandPosition(NPC.Bottom);
                    NPC.netUpdate = true;
                }

                //Vroom vroom
                ChaseBehavior(target.Center);
            }

            //if either shooting its arm out or swinging around it
            else if (AttackState == AttackStates.ShootingArm || AttackState == AttackStates.SwingingAroundHand)
            {
                //Reset a bunch of stats
                squishyY = 0;
                ArmRotation = 0f;
                handRotation = 0f;

                //Point towards the hand position like a goofball
                NPC.rotation = Utils.AngleLerp(NPC.rotation, NPC.Bottom.DirectionTo(handPositionOverride).ToRotation() + MathHelper.PiOver2, 0.4f);

                bool didASlam = false;

                //If shooting the arm out to find a tile to grab onto
                if (AttackState == AttackStates.ShootingArm)
                {
                    HandState = 1f; //Handstate = 1 means that it uses the position override instead of the default position

                    //Slow down FAST
                    NPC.velocity.X *= 0.85f;
                    if (Math.Abs(NPC.velocity.X) < 0.05f)
                        NPC.velocity.X = 0f;

                    //Basically adjust the attack charge because we want it to hang on the "final" state for a while longer.
                    float adjustedAttackCharge = MathHelper.Clamp(AttackCharge * 1.5f, 0f, 1f);

                    //Gravity on the hand goes up a bit and then down a lot
                    Vector2 gravity = Vector2.UnitY * (float)Math.Sin(adjustedAttackCharge * MathHelper.Pi + MathHelper.PiOver2) * -1f;
                    if (gravity.Y > 0)
                        gravity.Y *= 4.5f;

                    //Move the hand position override forward and apply the gravity to it
                    handPositionOverride.X += (float)Math.Pow(1 - adjustedAttackCharge, 1.2f) * NPC.direction * 4f;
                    handPositionOverride += gravity;

                    Tile tile = Framing.GetTileSafely(handPositionOverride.ToSafeTileCoordinates());
                    Tile tileAbove = Framing.GetTileSafely(handPositionOverride.ToSafeTileCoordinates() - new Point(0, 1));
                    bool clearTileAbove = !tileAbove.HasUnactuatedTile || !Main.tileSolid[tile.TileType];

                    //it can only grapple to tiles way above itself if its a "surface" tile. Prevents it from grappling onto ceilings
                    bool veryHighAboveRobot = NPC.Top.Y - handPositionOverride.Y > 20;
                    bool canGrappleToHighTile = !veryHighAboveRobot || clearTileAbove;

                    //If the hand found a tile to grapple on
                    if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && AttackCharge > 0.02f && canGrappleToHighTile)
                    {
                        //Do the swingy!
                        NPC.Center = NPC.Bottom + (NPC.rotation - MathHelper.PiOver2).ToRotationVector2() * NPC.height / 2; //This is done because while its flinging itself around, it rotates around its center, not its bottom. If we dont do it, itll look like it jittered a bit
                        SoundEngine.PlaySound(HandGrabSound, handPositionOverride);
                        AttackState = AttackStates.SwingingAroundHand;
                        AttackCharge = 0;
                        NPC.netUpdate = true;

                        //Flat palm on the ground if flat ground
                        if (clearTileAbove && tile.Slope == SlopeType.Solid)
                        {
                            ArmGrabbyStyle = 0f; //Grabby style 0 means that its got a flat palm
                            handPositionOverride = handPositionOverride.ToSafeTileCoordinates().ToWorldCoordinates() - Vector2.UnitY * 22f; //Set the palm to lay specifically flush with the floor

                            //Sink it down if its a half tile
                            if (tile.IsHalfBlock)
                                handPositionOverride += Vector2.UnitY * 8f;
                        }
                        else
                            ArmGrabbyStyle = 1f; //Grabby style 0 means that its got a clenched fist

                        //remember the length of the arm so that the flip can keep a static arm lenght, and remember the "normal" of the flip so it knows at which angle to do the flip
                        ArmStaticLenght = NPC.Bottom.Distance(handPositionOverride);
                        FlipNormal = handPositionOverride.DirectionTo(NPC.Center).RotatedBy(MathHelper.PiOver2 * NPC.direction).ToRotation();


                        NPC.direction = (handPositionOverride.X - NPC.Center.X).NonZeroSign();
                    }

                    AttackCharge += 1 / (60f * 1.5f);
                }

                //If its doing a flip
                else
                {
                    //Flip around the hand in a curve.
                    float flipProgress = (float)Math.Pow(AttackCharge, 2f);
                    Vector2 targetAngle = (FlipNormal + NPC.direction * (-MathHelper.PiOver2 + MathHelper.Pi * flipProgress)).ToRotationVector2();
                    Vector2 moveTarget = handPositionOverride + targetAngle * ArmStaticLenght;

                    NPC.velocity = moveTarget - NPC.Center;

                    //Dot product explanation
                    //targetAngle is the direction from the hand in which the grappler is pointing
                    //finalFlipAngle is the direction the grappler would be pointing once its fully done with its flip.

                    //If targetAngle is the same as finalFlipAngle it means the dot product would be 1.
                    //If its pointing in the exact opposite direction (aka at the start of the flip), the dot product would be -1
                    //If its perpendicular, the dot product would be 0

                    //If the dot product is between 1 and 0, it means that the current angle of the grappler is at an acute angle to the final flip angle = the flip of 180° is halfway done

                    Vector2 finalFlipAngle = (FlipNormal + NPC.direction * MathHelper.PiOver2).ToRotationVector2();
                    bool lessThanHalfwayThrough = Vector2.Dot(targetAngle, finalFlipAngle) < 0;
                    float distanceToTarget = NPC.Distance(target.Center);

                    //Only collides when the flip is more than halfway done
                    //Edit: WRONG ignore collision at the very start so it doesnt insta-slam itself
                    NPC.noTileCollide = AttackCharge <= 0.4f; 
                    NPC.noGravity = true;
                    NPC.gfxOffY = 0;

                    AttackCharge += 1 / (60f * 0.55f);

                    //Can only launch if the target is too far. If the target is close enough, itll just do its regular flip.
                    //It also cant launch if is flipping away from the player
                    bool mayLaunchAtAll = handPositionOverride.Distance(target.Center) > ArmStaticLenght * 1.4f && (target.Center.X - NPC.Center.X).NonZeroSign() == NPC.direction && Math.Abs(target.Center.X - handPositionOverride.X) > ArmStaticLenght * 1.4f;

                    if (mayLaunchAtAll)
                    {
                        Vector2 launchVel = Vector2.Zero;

                        //Launch attack to fling itself above the player in order to ground pound. CAn only do this if close enough and not inside of tiles
                        bool mayLaunchToGroundPound = distanceToTarget < ArmStaticLenght * 5f && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height, false) && AttackCharge > 0.05f;
                        if (mayLaunchToGroundPound)
                        {
                            //if its able to launch itself, calculate the velocity it would need to go in an arc that goes above the player
                            launchVel = GetArcVel(NPC.Center, target.Center + target.velocity * 2f + Vector2.UnitX * (target.Center.X - NPC.Center.X), 0.3f, 200, 300);

                            //Calculate the dot product between it and the current velocity direction of the NPC. (if its 1 it means theyre perfectly the same. We will allow for some minor deviation as it wont be caught visually)
                            Vector2 normalizedLaunchVel = launchVel.SafeNormalize(Vector2.Zero);
                            mayLaunchToGroundPound &= Vector2.Dot(normalizedLaunchVel, (moveTarget - NPC.Center).SafeNormalize(Vector2.Zero)) > 0.9f;
                        }


                        //Launch attack in case the player is too far away. It only does it if more than halfway through the dash, and if its angered
                        bool mayLaunchToCatchThePlayer = distanceToTarget < 1300 && !lessThanHalfwayThrough && NPC.life < NPC.lifeMax;


                        //Throw yourself if able to
                        if (mayLaunchToCatchThePlayer || mayLaunchToGroundPound)
                        {
                            SoundEngine.PlaySound(LaunchSound, NPC.Center);

                            AttackCharge = 0f;
                            AttackState = AttackStates.ZoomingAround; //Zoom!
                            NPC.collideX = false;
                            NPC.collideY = false;
                            NPC.netUpdate = true;
                            NPC.noTileCollide = false;

                            //If doing a ground pound, we use the launch velocity we calculated above
                            if (mayLaunchToGroundPound)
                            {
                                NPC.velocity = launchVel;
                            }

                            //If not, we just give it a boosti n Y velocity as it gets flung
                            else
                            {
                                float jumpSpeed = -4.5f - 4f * Utils.GetLerpValue(ArmStaticLenght * 2f, 1300f, Math.Abs(NPC.Center.X - target.Center.X), true);
                                NPC.velocity.Y = jumpSpeed;
                            }
                        }
                    }

                    //If its colliding into tiles, stop the flip instantly (
                    //AttackState check necessary to avoid doing it as the little guy flings itself up
                    if (AttackState != AttackStates.ZoomingAround && !NPC.noTileCollide && Collision.SolidCollision(NPC.position + NPC.velocity, NPC.width, NPC.height, true))
                    {
                        AttackCharge = 1;
                        NPC.netUpdate = true;
                        NPC.noTileCollide = false;
                        didASlam = true;
                    }
                }

                //If the timer runs out (either it took too long and its hand didnt find a solid tile to grapple on, or its done with its flip
                if (AttackCharge >= 1)
                {
                    //If it was swinging around its hand and collides with tiles, play a sound and squish
                    if (AttackState == AttackStates.SwingingAroundHand && didASlam)
                    {
                        SoundEngine.PlaySound(SlamSound with
                        {
                            Volume = SlamSound.Volume * 0.7f
                        }, NPC.Center);
                        squishyY = 0.6f;
                    }
                    AttackState = 0f;
                    AttackCharge = 0f;
                    NPC.netUpdate = true;
                }
            }

            //If being flung around
            if (AttackState == AttackStates.ZoomingAround)
            {
                //Try to get its arm to catch up
                ResetArmPosition(0.2f, 4f);
                if (HandState == 0)
                    ArmRotation = Utils.AngleLerp(ArmRotation, 0f, 0.2f);

                NPC.noGravity = false;
                //NPC.noTileCollide = !(!NPC.noTileCollide || AttackCharge > 10); //Don't collide with tiles for a few frames after the launch
                AttackCharge += 1;

                //A bunch of extra checks it only performs after a few frames of dashing.
                bool extraChecks = AttackCharge > 10 && (NPC.oldPos[NPC.oldPos.Length - 1] == NPC.position || Collision.SolidCollision(NPC.position, NPC.width, NPC.height, !CanFallThroughPlatforms().Value));

                //If it collided, stop the fling
                if ((NPC.collideX || NPC.collideY) || extraChecks)
                {
                    NPC.velocity.Y = -2;
                    squishyY = 0.7f;
                    AttackCharge = 0f;
                    AttackState = AttackStates.Chasing;
                    NPC.netUpdate = true;

                    SoundEngine.PlaySound(LandingSound, NPC.Center);
                    return;
                }

                //Roll around
                NPC.rotation += Math.Max(0.6f, Math.Abs(NPC.velocity.X)) * NPC.velocity.X.NonZeroSign() * 0.02f;

                //Can ground pound from further away in expert = better accuracy and less chance to overshoot
                float distanceToGroundPound = Main.expertMode ? 75 : 30;

                //Ground pounds if above the target
                if (distanceToTargetX < distanceToGroundPound && (target.Center.Y - NPC.Center.Y) > 140)
                {
                    AttackState = AttackStates.GroundPound;
                    AttackCharge = 0f;
                    NPC.noTileCollide = false;
                    NPC.netUpdate = true;
                }
            }

            //If groundpounding
            if (AttackState == AttackStates.GroundPound)
            {
                //TLDR make it reset to its regular rotation. Just complicated cuz of radians n all
                if ((NPC.rotation.Modulo(MathHelper.TwoPi) > MathHelper.PiOver2) && (NPC.rotation.Modulo(MathHelper.TwoPi) <= MathHelper.TwoPi - MathHelper.PiOver2))
                    NPC.rotation = AngleLerpDirectional(NPC.rotation, 0f, 0.14f, NPC.velocity.X.NonZeroSign() == -1);
                else
                    NPC.rotation = Utils.AngleLerp(NPC.rotation, 0f, 0.14f);

                //Telegraph
                if (AttackCharge < 1)
                {
                    //Reset its arm position EXTRA FAST so it is almost guaranteed to be right above the grappler before falling
                    ResetArmPosition(0.3f, 4f);

                    //Rotation bullshit
                    if (NPC.rotation >= MathHelper.TwoPi || NPC.rotation < 0)
                    {
                        float clampedRotation = NPC.rotation.Modulo(MathHelper.TwoPi);
                        if (NPC.velocity.X < 0)
                            clampedRotation = MathHelper.TwoPi - clampedRotation;

                        NPC.rotation = clampedRotation;
                    }

                    //Slow down horizontally and make it not fall fast at all
                    NPC.velocity.X *= 0.94f;
                    NPC.velocity.Y = Math.Min(NPC.velocity.Y, 1f);
                    AttackCharge += 1 / (60f * 0.25f);
                }

                else
                {
                    ArmRotation = 0f;
                    NPC.noGravity = true;

                    //Make its hand be at the very precise position it needs to so its straight
                    if (AttackCharge == 1)
                    {
                        HandState = 1;
                        handPositionOverride = GetHandPosition(NPC.Bottom);
                    }

                    else if (AttackCharge >= 1.06f)
                    {
                        ResetArmPosition((AttackCharge - 1.05f), 1f); //reset its hand position. At the start of the attack it barely does it, but as the attack progresses it gets reeled in faster
                    }

                    //Squish as you fall
                    squishyY = MathHelper.Lerp(squishyY, 1.4f, 0.1f);

                    AttackCharge += 0.01f;
                    NPC.velocity.X *= 0.1f;
                    NPC.velocity.Y += 0.1f + (AttackCharge - 1) * 10f * 0.7f; //Fall down
                    NPC.velocity.Y = Math.Min(NPC.velocity.Y, 16);

                    //If crashed into the ground
                    if (NPC.collideX || NPC.collideY || NPC.oldPos[NPC.oldPos.Length - 1] == NPC.position || Collision.SolidCollision(NPC.position, NPC.width, NPC.height + (int)NPC.velocity.Y, !CanFallThroughPlatforms().Value))
                    {
                        AttackCharge = 0f;
                        AttackState = AttackStates.Chasing; //Go back to chasing
                        NPC.netUpdate = true;
                        squishyY = 0.52f;

                        SoundEngine.PlaySound(SlamSound, NPC.Center);

                        if (Main.LocalPlayer.Distance(NPC.Center) < 400)
                            CameraManager.Shake += 6 * Utils.GetLerpValue(400, 100, Main.LocalPlayer.Distance(NPC.Center), true);
                    }
                }
            }
        }

        public void ResetArmPosition(float lerpStrenght = 0.14f, float moveSpeed = 2f)
        {
            //If the hand state is in "use the overriden hand position" state, make it move towards the regular ideal hand position
            if (HandState == 1)
            {
                Vector2 idealHandPos = GetHandPosition(NPC.Bottom);

                //If we got close enough to the ideal hand state, make it stop using the override
                if (idealHandPos.Distance(handPositionOverride) < 3f)
                    HandState = 0;

                //Move the hand position override towards the ideal pos
                handPositionOverride = Vector2.Lerp(handPositionOverride, idealHandPos, lerpStrenght);
                handPositionOverride = handPositionOverride.MoveTowards(idealHandPos, moveSpeed);
            }
        }

        #region regular chase
        public void ChaseBehavior(Vector2 movementTarget, bool forcedRetarget = false, bool doHandThings = true)
        {
            bool lineOfSight = Collision.CanHitLine(movementTarget - new Vector2(target.width, target.height) / 2, target.width, target.height, NPC.position, NPC.width, NPC.height);

            //Retargets if close but not too close and if line of sight
            float distanceToPlayerX = Math.Abs(movementTarget.X - NPC.Center.X);

            if (forcedRetarget || (distanceToPlayerX < 600 && (Aggroed || (distanceToPlayerX > 80 && lineOfSight))))
            {
                int direction = (movementTarget.X - NPC.Center.X).NonZeroSign();
                if (NPC.direction != direction)
                {
                    NPC.direction = direction;
                    NPC.netSpam = 0;
                    NPC.netUpdate = true;
                }
            }

            //Accelerates forward
            float maxXSpeed = (NPC.life < NPC.lifeMax ? 5f : 3f) * MoveSpeedMultiplier;
            NPC.velocity.X += 0.02f * NPC.direction;
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * maxXSpeed, 0.01f);
            NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -maxXSpeed, maxXSpeed);

            float jumpHeight = (forcedRetarget || lineOfSight) ? 4f : 8f;

            //Jump if encountering a wall
            if (NPC.collideX)
            {
                if (NPC.life == NPC.lifeMax && !lineOfSight)
                {
                    NPC.direction *= -1;
                    NPC.velocity.X *= -1;
                }

                else if (NPC.velocity.Y == 0)
                {
                    SoundEngine.PlaySound(HopSound, NPC.Center);
                    NPC.velocity.Y = -jumpHeight;

                    //Swap directions if the player is on the other side
                    if ((movementTarget.X - NPC.Center.X).NonZeroSign() != NPC.direction)
                        NPC.direction *= -1;
                }
            }

            bool stuckInTiles = Collision.SolidCollision(NPC.position, NPC.width, NPC.height - 16, false);

            // Jump if there's an gap ahead or if stuck inside of tiles (can happen after a stomp)
            if (NPC.collideY && NPC.velocity.Y == 0f)
            {
                bool gapAhead = (movementTarget - Vector2.UnitY * target.height / 2).Y < NPC.Bottom.Y && HoleAtPosition(NPC.Center.X + NPC.velocity.X * 4f);

                if (gapAhead || stuckInTiles)
                {
                    NPC.velocity.Y = stuckInTiles ? -5 : -jumpHeight;
                    SoundEngine.PlaySound(HopSound, NPC.Center);
                }
            }

            if (stuckInTiles)
                NPC.position.Y -= 6f;



            //KIck up some dust as you go
            if (NPC.collideY && NPC.velocity.Y == 0f && Math.Abs(NPC.velocity.X) > 1f && Main.rand.NextBool(6))
            {
                Vector2 dustPos = NPC.Bottom - Vector2.UnitX * Main.rand.NextFloat() * NPC.width / 2 * NPC.direction;
                Dust.NewDustPerfect(dustPos, DustID.Smoke, new Vector2(-NPC.direction * Main.rand.NextFloat(0.5f, 1f), Main.rand.NextFloat(-1f, -0.3f)), 130, default, 1.4f);
            }

            //Rotate towards where youre going
            NPC.rotation = MathHelper.Clamp(NPC.velocity.X, -3.5f, 3.5f) * 0.05f;

            //Squish if youre falling down n all that jazz
            if (NPC.velocity.Y > 0)
            {
                if (squishyY < 1)
                    squishyY = 1;

                squishyY += 0.0034f;
            }
            else if (squishyY > 1)
            {
                if (squishyY >= 1.03f)
                {
                    SoundEngine.PlaySound(LandingSound with
                    {
                        Volume = LandingSound.Volume * Utils.GetLerpValue(1f, 1.05f, squishyY, true)
                    }, NPC.Center);
                }

                squishyY = 0.8f;
            }
            else if (squishyY < 1)
                squishyY = MathHelper.Lerp(squishyY, 1f, 0.1f);
            squishyY = MathHelper.Clamp(squishyY, 0.8f, 1.22f);

            if (doHandThings)
            {
                //Set its arm rotation based on its HP. Backwards if unharmed, forwards if harmed
                float idealArmRotation = NPC.life < NPC.lifeMax ? NPC.rotation * 5.4f : -NPC.rotation * 3f;
                ArmRotation = MathHelper.Lerp(ArmRotation, idealArmRotation, 0.06f);

                if (NPC.life < NPC.lifeMax)
                {
                    handRotation = MathHelper.Lerp(handRotation, 0.4f * NPC.direction, 0.04f);

                    if (handRotation.NonZeroSign() != NPC.direction)
                        handRotation = MathHelper.Lerp(handRotation, 0.4f * NPC.direction, 0.05f);
                }
            }
        }

        private bool HoleAtPosition(float xPosition)
        {
            int tileWidth = NPC.width / 16;
            xPosition = (int)(xPosition / 16f) - tileWidth;
            if (NPC.velocity.X > 0)
                xPosition += tileWidth;

            int tileY = (int)((NPC.position.Y + NPC.height) / 16f);
            for (int y = tileY; y < tileY + 2; y++)
            {
                for (int x = (int)xPosition; x < xPosition + tileWidth; x++)
                {
                    if (Main.tile[x, y].HasTile)
                        return false;
                }
            }

            return true;
        }
        #endregion

        public void ComboLogic()
        {
            if (AttackState != AttackStates.SwingingAroundMagnetizer && AttackState != AttackStates.TryingToGrabMagnetizer)
                return;

            if (magnetToSwingAround == default || magnetToSwingAround == null || !IsValidMagnetizerForCombo(magnetToSwingAround))
            {
                magnetToSwingAround = default;
                if (AttackState == AttackStates.SwingingAroundMagnetizer || AttackState == AttackStates.TryingToGrabMagnetizer)
                {
                    AttackState = AttackStates.Chasing;
                    AttackCharge = 0;
                    NPC.netUpdate = true;
                }
                return;
            }


            HandState = 1;

            //Reach out for the magnetizer
            if (AttackState == AttackStates.TryingToGrabMagnetizer)
            {
                AttackCharge += 1 / (60f * 0.5f);

                float minDistanceForHandToGrabGrappler = 20;
                float maxArmDistance = 130;


                if (!hasGrabbedOntoMagnet)
                {
                    ChaseBehavior(magnetToSwingAround.Center, true, false); //Chase after the magnet a bit

                    handPositionOverride = Vector2.Lerp(handPositionOverride, magnetToSwingAround.Center, 0.1f + 0.3f * (float)Math.Pow(AttackCharge, 2f));
                    handPositionOverride.MoveTowards(magnetToSwingAround.Center, 3f);

                    //Grab
                    if (handPositionOverride.Distance(magnetToSwingAround.Center) <= minDistanceForHandToGrabGrappler)
                    {
                        hasGrabbedOntoMagnet = true;
                        handPositionOverride = magnetToSwingAround.Center;

                        NPC.collideX = false;
                        NPC.collideY = false;
                        NPC.noTileCollide = true;

                        SoundEngine.PlaySound(HandGrabSound, handPositionOverride);
                        ArmStaticLenght = NPC.Distance(magnetToSwingAround.Center);
                        AttackCharge = 0f;
                        FlipNormal = NPC.DirectionFrom(magnetToSwingAround.Center).ToRotation();
                        magnetToSwingAround.velocity.Y = 2;
                        NPC.netUpdate = true;
                    }
                }

                else
                {

                    handPositionOverride = magnetToSwingAround.Center;
                    AttackCharge = 0f;

                    if (NPC.Distance(magnetToSwingAround.Center) > maxArmDistance)
                    {
                        ArmStaticLenght *= 0.98f;
                        ArmStaticLenght = MathHelper.Lerp(ArmStaticLenght, maxArmDistance, 0.02f);
                        NPC.Center = magnetToSwingAround.Center + NPC.DirectionFrom(magnetToSwingAround.Center) * ArmStaticLenght;
                    }

                    else
                    {
                        NPC.Center = magnetToSwingAround.Center + NPC.DirectionFrom(magnetToSwingAround.Center) * ArmStaticLenght;
                        arcSpeed = Math.Max(ArcAngle(NPC.velocity.Length(), ArmStaticLenght), 0.05f) + Utils.GetLerpValue(maxArmDistance * 0.3f, 0f, ArmStaticLenght, true) * 0.04f;
                        AttackState = AttackStates.SwingingAroundMagnetizer;
                        FlipNormal = NPC.DirectionFrom(magnetToSwingAround.Center).ToRotation();
                        NPC.netUpdate = true;
                    }
                }
            }

            else
            {
                handPositionOverride = magnetToSwingAround.Center;
                ArmGrabbyStyle = 1;

                NPC.noTileCollide = true;
                squishyY = 0;
                ArmRotation = 0f;
                handRotation = 0f;

                //Point towards the hand position like a goofball
                NPC.rotation = Utils.AngleLerp(NPC.rotation, NPC.Bottom.DirectionTo(handPositionOverride).ToRotation() + MathHelper.PiOver2, 0.4f);

                //Flip around the hand in a curve.
                float flipProgress = (float)Math.Pow(AttackCharge, 2f);

                float finalAngle = (Vector2.UnitX * NPC.direction).ToRotation();
                float currentAngle = NPC.AngleFrom(handPositionOverride);
                float targetAngle = currentAngle.AngleTowardsDirectional(finalAngle, arcSpeed, NPC.direction == 1);

                //FlipNormal = targetAngle;
                arcSpeed *= 1.03f;
                //arcSpeed = MathHelper.Lerp(arcSpeed, 0.14f, 0.1f);

                Vector2 moveTarget = handPositionOverride + targetAngle.ToRotationVector2() * ArmStaticLenght;
                NPC.velocity = moveTarget - NPC.Center;

                float distanceToTarget = NPC.Distance(target.Center);

                NPC.noTileCollide = true; //Only collides when the flip is more than halfway done
                NPC.noGravity = true;
                NPC.gfxOffY = 0;

                AttackCharge += 1 / (60f * 1f);

                if (AttackCharge > 0.05f && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height, false))
                {
                    Vector2 launchVel = GetArcVel(NPC.Bottom, target.Center + target.velocity * 2f + Vector2.UnitX * (target.Center.X - NPC.Center.X).NonZeroSign() * 260, 0.3f, 150, 250, heightAboveTarget: 160);
                    Vector2 normalizedLaunchVel = launchVel.SafeNormalize(Vector2.Zero);

                    //SimulateMortar(launchVel, 0.3f, NPC.Bottom, 100);

                    //Throw yourself if able to
                    if (Vector2.Dot(normalizedLaunchVel, (moveTarget - NPC.Center).SafeNormalize(Vector2.Zero)) > 0.9f)
                    {
                        SoundEngine.PlaySound(LaunchSound, NPC.Center);

                        AttackCharge = 0f;
                        AttackState = AttackStates.ZoomingAround; //Zoom!
                        NPC.Center = NPC.Bottom; //This is done because while its flinging itself around, it rotates around its center, not its bottom. If we dont do it, itll look like it jittered a bit
                        NPC.collideX = false;
                        NPC.collideY = false;
                        NPC.netUpdate = true;

                        NPC.velocity = launchVel;
                        magnetToSwingAround.velocity.Y = 3;
                    }
                }

                //If its colliding into tiles, stop the flip instantly (
                //AttackState check necessary to avoid doing it as the little guy flings itself up

                float angleLeftOnArc = Math.Abs(currentAngle.AngleBetween(finalAngle));
                bool doneSwinging = angleLeftOnArc < 0.05f;

                if (AttackState != AttackStates.ZoomingAround && (doneSwinging || (!NPC.noTileCollide && Collision.SolidCollision(NPC.position, NPC.width, NPC.height + 26, true))))
                {
                    if (doneSwinging)
                    {
                        Vector2 launchVel = GetArcVel(NPC.Center, target.Center + Vector2.UnitX * (target.Center.X - NPC.Center.X), 0.3f, 150, 250, heightAboveTarget: 100);

                        if (Vector2.Dot(launchVel.SafeNormalize(Vector2.Zero), (moveTarget - NPC.Center).SafeNormalize(Vector2.Zero)) > 0.6f)
                            NPC.velocity = launchVel;
                    }

                    if (NPC.velocity.Y > -2)
                        NPC.velocity.Y = -2;

                    AttackCharge = 1;
                }
            }

            //If the timer runs out (either it took too long and its hand didnt find a solid tile to grapple on, or its done with its flip
            if (AttackCharge >= 1)
            {
                if (AttackState == AttackStates.TryingToGrabMagnetizer)
                    AttackState = AttackStates.Chasing;

                else
                    AttackState = AttackStates.ZoomingAround;

                AttackCharge = 0f;
                NPC.netUpdate = true;
            }
        }

        public bool TryToFindMagnetizer()
        {
            magnetToSwingAround = Main.npc.Where(n => n.active && IsValidMagnetizerForCombo(n))
                .OrderBy(n => NPC.Distance(n.Center))
                .FirstOrDefault();

            bool magnetValidToSwingAround = magnetToSwingAround != default &&
                                                    magnetToSwingAround != null &&
                                                    (NPC.Center.Y - magnetToSwingAround.Center.Y) > 50 &&
                                                    (ArePointsInOrder(NPC.Center.X, magnetToSwingAround.Center.X, target.Center.X) ||
                                                    ArePointsInOrder(NPC.Center.X, target.Center.X, magnetToSwingAround.Center.X)) &&
                                                    Collision.CanHitLine(NPC.position, NPC.width, NPC.height, magnetToSwingAround.position, magnetToSwingAround.width, magnetToSwingAround.height);

            return magnetValidToSwingAround;
        }

        public bool IsValidMagnetizerForCombo(NPC magnet)
        {
            bool isAMagnetizer = magnet.active && magnet.type == ModContent.NPCType<WulfrumMagnetizer>();

            return isAMagnetizer &&
                   //Can only swing if the magnetizerr is between the player and the grappler
                   NPC.Distance(magnet.Center) < 430;
        }


        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return (AttackState != AttackStates.ShootingArm && AttackState != AttackStates.TryingToGrabMagnetizer);
        }

        //Does half as much damage when regularly chasing
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            if (AttackState == AttackStates.Chasing)
                modifiers.SourceDamage *= 0.5f;
        }

        public override bool? CanFallThroughPlatforms()
        {
            return target.Top.Y > NPC.Bottom.Y;
        }

        public override void FindFrame(int frameHeight)
        {
            if (squishyY == 0)
                squishyY = 1;

            float velocity = NPC.IsABestiaryIconDummy ? 2 : Math.Abs(NPC.velocity.X);
            NPC.frameCounter += 0.1 * velocity;

            if (NPC.frameCounter > 1)
            {
                NPC.frameCounter = 0;
                yFrame += 1;
                if (yFrame >= Main.npcFrameCount[Type])
                    yFrame = 0;
            }

            if (AttackState != AttackStates.SwingingAroundHand && AttackState != AttackStates.SwingingAroundMagnetizer)
            {
                handFrameCounter += (NPC.life < NPC.lifeMax ? 0.14f : 0.1f) / 7f;


                if (handFrameCounter >= 1)
                {
                    handFrameCounter = 0;
                }

                float handFrameFloat = (float)Math.Sin(handFrameCounter * MathHelper.Pi) * 4;

                handFrame = (int)MathHelper.Clamp((int)handFrameFloat, 0, 3);
            }

            else if (AttackState == AttackStates.SwingingAroundMagnetizer)
            {
                if (handPositionOverride.Distance(magnetToSwingAround.Center) > 10)
                    handFrame = 3;
                else
                    handFrame = 5;
            }

            else
            {
                handFrame = 4 + (int)ArmGrabbyStyle;
            }


            if (NPC.IsABestiaryIconDummy)
            {
                squishyY = 1f + 0.05f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.9f);
                ArmRotation = 0.1f * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.7f) * 0.5f + 0.5f);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = NPC.TintFromBuffAesthetic(drawColor);
            //Fsr the icon dummy uses a fullblack color by default?
            if (NPC.IsABestiaryIconDummy)
            {
                drawColor = Color.White;
                if (squishyY == 0)
                    squishyY = 1;
            }

            Texture2D bodyTex = TextureAssets.Npc[Type].Value;
            Texture2D linkTex = ModContent.Request<Texture2D>(Texture + "_Segment").Value;
            Texture2D handTex = ModContent.Request<Texture2D>(Texture + "_Hand").Value;

            Vector2 rotationCenter = NPC.Bottom;
            bool rotatesAroundCenter = AttackState == AttackStates.ZoomingAround || AttackState == AttackStates.SwingingAroundHand;
            if (rotatesAroundCenter) //Some states rotate around the center
            {
                rotationCenter = NPC.Center + Vector2.UnitY.RotatedBy(NPC.rotation) * NPC.height / 2f;
            }

            Vector2 gfxOffY = NPC.GfxOffY() + Vector2.UnitY;
            SpriteEffects flip = NPC.direction == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Vector2 linkBasePosition = GetLinkPosition(rotationCenter);
            Vector2 handPosition = GetHandPosition(rotationCenter);

            if (HandState != 0)
            {
                handPosition = handPositionOverride;

                if (AttackState == AttackStates.Chasing || AttackState == AttackStates.ZoomingAround)
                {
                    ArmRotation = (linkBasePosition - handPosition).ToRotation() - MathHelper.PiOver2;
                    handRotation = 0;
                }
            }

            float linkRotation = (handPosition - linkBasePosition).ToRotation() + MathHelper.PiOver2;
            Vector2 linkOrigin = new Vector2(linkTex.Width / 2, linkTex.Height);
            Vector2 linkScale = new Vector2(1.5f, (linkBasePosition - handPosition).Length() / linkTex.Height) * NPC.scale;

            Main.spriteBatch.Draw(linkTex, linkBasePosition + gfxOffY - screenPos, null, drawColor, linkRotation, linkOrigin, linkScale, flip, 0f);

            Vector2 position = NPC.Bottom;
            Rectangle frame = new Rectangle(0, yFrame * bodyTex.Height / 6, bodyTex.Width, bodyTex.Height / 6 - 2);
            Vector2 origin = new Vector2(frame.Width / 2, frame.Height);
            Vector2 scale = new Vector2(2 - squishyY, squishyY) * NPC.scale;

            //Rolling around
            if (rotatesAroundCenter)
            {
                position = NPC.Center;
                origin = frame.Size() / 2;

            }

            Main.spriteBatch.Draw(bodyTex, position + gfxOffY - screenPos, frame, drawColor, NPC.rotation, origin, scale, flip, 0f);


            float _handRotation = linkRotation - handRotation;
            if (AttackState == AttackStates.SwingingAroundHand || AttackState == AttackStates.SwingingAroundMagnetizer)
                _handRotation = MathHelper.Pi;

            Rectangle handRect = new Rectangle(0, handFrame * handTex.Height / 6, handTex.Width, handTex.Height / 6 - 2);
            Vector2 handOrigin = new Vector2(handRect.Width / 2, handRect.Height);

            if (ArmGrabbyStyle == 1f && (AttackState == AttackStates.SwingingAroundHand || AttackState == AttackStates.SwingingAroundMagnetizer))
            {
                _handRotation = linkRotation;
                handPosition += (linkRotation + MathHelper.PiOver2).ToRotationVector2() * 12f;
            }

            Main.spriteBatch.Draw(handTex, handPosition + gfxOffY - screenPos, handRect, drawColor, _handRotation, handOrigin, NPC.scale, flip, 0f);


            return false;
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (AttackState == AttackStates.Chasing && item.type == ItemID.SlapHand && player.whoAmI == Main.myPlayer)
            {
                CameraManager.Shake += 5;
                SoundEngine.PlaySound(SoundDirectory.CommonSounds.Comedy);
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => WulfrumCollaborationHelper.WulfrumGoonSpawnChance(spawnInfo);

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (!Main.dedServ)
            {
                for (int k = 0; k < 5; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 3, hit.HitDirection, -1f, 0, default, 1f);
                }
                if (NPC.life <= 0)
                {
                    for (int k = 0; k < 20; k++)
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, 3, hit.HitDirection, -1f, 0, default, 1f);
                    }


                    for (int i = 0; i < 3; i++)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumGrapplerGore" + i.ToString()).Type, 1f);
                    }

                    Vector2 handGorePos = GetHandPosition(NPC.Bottom);
                    if (HandState == 1)
                        handGorePos = handPositionOverride;

                    Gore.NewGore(NPC.GetSource_Death(), handGorePos, NPC.velocity, Mod.Find<ModGore>("WulfrumGrapplerGore3").Type, 1f);


                    int randomGoreCount = Main.rand.Next(0, 2);
                    for (int i = 0; i < randomGoreCount; i++)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumEnemyGore" + Main.rand.Next(1, 11).ToString()).Type, 1f);
                    }

                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ModContent.ItemType<WulfrumMetalScrap>(), 1, 1, 2);
            npcLoot.Add(ModContent.ItemType<WulfrumAcrobaticsPack>(), new Fraction(5, 100));
            npcLoot.AddIf(info => (info.npc.ModNPC as ISuperchargable).IsSupercharged, ModContent.ItemType<EnergyCore>());

            npcLoot.Add(ModContent.ItemType<WulfrumBrandCereal>(), WulfrumBrandCereal.DroprateInt);
        }
    }
}
