using CalamityFables.Content.NPCs.GeodeGrawlers;
using ReLogic.Utilities;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;

namespace CalamityFables.Content.NPCs.Underground
{
    public class SpelunkerScarab : ModNPC
    {
        public override string Texture => AssetDirectory.UndergroundNPCs + Name;

        public static int SCARED_RANGE = 10 * 16;

        public static int HORIZ_JUMP_RANGE = 50 * 16;
        public static int ABOVE_JUMP_RANGE = 15 * 16;
        public static int BELOW_JUMP_RANGE = 30 * 16;

        public static int BOULDER_CHUCK_START_LENGTH = 72;
        public static int GROUND_POUND_START_LENGTH = 32;
        public static int STUCK_LENGTH = 300;
        public static int STUCK_RECOVERY_LENGTH = 25;

        public static Point BOULDER_DURABILITY_RANGE = new(16, 24);

        public const int FRAME_WIDTH = 50;
        public const int FRAME_HEIGHT = 78;

        public static int SpiritRLapisTileID = -1;
        public static int SpiritRLapisItemID = -1;
        public static int SpiritRZigguratTile1ID = -1;
        public static int SpiritRZigguratTile2ID = -1;
        public static int SpiritRZigguratTile3ID = -1;

        #region AI State Enums
        public enum AIState
        {
            Idle,
            WalkAround,
            RunAwayFromPlayer,

            JumpingStart,
            Jumping,
            GroundPoundStart,
            GroundPound,
            Stuck,
            StuckRecovery
        }

        public enum ScarabAnim
        {
            Idle,
            ChuckBoulder,
            Teleport,
            FallDown,
            BoulderPopOutRecoil,
            RollBoulder,
            RunAway,
            BoulderSuplex,
            BoulderKickAnticipation,
            BoulderKick,
            Stomp,
            AngryStomp
        }
        #endregion

        #region Sounds
        public static readonly SoundStyle KickBoulderSound = new("CalamityFables/Sounds/SpelunkerScarab/SpelunkerScarabKick");
        public static readonly SoundStyle BoulderSlamSound = new("CalamityFables/Sounds/SpelunkerScarab/SpelunkerScarabBoulderSlam", 2);
        public static readonly SoundStyle AmbientSound = new("CalamityFables/Sounds/SpelunkerScarab/SpelunkerShellAmbient", 2) { PitchVariance = 0.05f };
        public static readonly SoundStyle DeathSound = new("CalamityFables/Sounds/SpelunkerScarab/SpelunkerScarabDeath", 3);
        public static readonly SoundStyle HitSound = new("CalamityFables/Sounds/SpelunkerScarab/SpelunkerShellHit");
        public static readonly SoundStyle RollingLoop = new("CalamityFables/Sounds/SpelunkerScarab/SpelunkerScarabBoulderRollLoop") { IsLooped = true, MaxInstances = 0 };
        public static readonly SoundStyle JumpSound = new("CalamityFables/Sounds/SpelunkerScarab/SpelunkerScarabJump");
        public static readonly SoundStyle WarningSound = new("CalamityFables/Sounds/SpelunkerScarab/SpelunkerScarabSpooked", 3);
        public static readonly SoundStyle BoulderTrampleSound = new("CalamityFables/Sounds/SpelunkerScarab/SpelunkerScarabPound", 2) {  PitchVariance = 0.6f};
        public static readonly SoundStyle BoulderDislodgeSound = new("CalamityFables/Sounds/SpelunkerScarab/SpelunkerScarabDislodgeBoulder", 3);
        public static readonly SoundStyle BreakBoulderSound = new("CalamityFables/Sounds/SpelunkerScarab/SpelunkerScarabBoulderBreak");

        #endregion

        #region Fields
        public Player Target => Main.player[NPC.target];

        /// <summary>
        /// A timer that tracks the frames since changing AI state.
        /// </summary>
        public ref float AICounter => ref NPC.ai[0];

        /// <summary>
        /// The current AI state of this Spelunker Shell. Possible states are in <see cref="AIState"/>.
        /// </summary>
        public AIState CurrentAIState
        {
            get => (AIState)NPC.ai[1];
            set {
                if (NPC.ai[1] == (int)value)
                    return;
                // Reset counter whenever AI state is changed 
                AICounter = 0;
                ResetScarabFrame();
                NPC.netUpdate = true;

                NPC.ai[1] = (int)value;
            }
        }

        /// <summary>
        /// The current armor layer of this Spelunker Scarab
        /// </summary>
        public int BoulderOre
        {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = (int)value;
        }
        public bool BoulderBroken => BoulderOre == 0;

        /// <summary>
        /// Number of ore drops before the boulder gets broken. Mined drops deduct 1 point, spawning a rock projectile deducts 2.
        /// </summary>
        public float BoulderDurability;

        /// <summary>
        /// The current animation state of this Spelunker Shell, which controls which set of animations will be used. Possible states are in <see cref="ScarabAnim"/>.
        /// </summary>
        public ScarabAnim ScarabAnimation
        {
            get => (ScarabAnim)NPC.localAI[0];
            set => NPC.localAI[0] = (int)value;
        }

        public bool IsGlimmering
        {
            get => NPC.ai[3] == 1;
            set => NPC.ai[3] = value ? 1 : 0;
        }

        public Vector2 lastScarabDrawPosition;
        public Vector2 lastScarabWorldPosition;
        public float scarabFadeInTimer = 0f;
        public float detachedScarabYVelocity;
        public float boulderVisualShake = 0f;
        public SlotId RollingSoundSlot;
        public float RollingVolume = 0f;

        /// <summary>
        /// X coords of the current destination to walk to.
        /// </summary>
        public float WalkDestinationX;

        public bool ChangedAIStateLastFrame = false;

        /// <summary>
        /// Timer that allows the Spelunker to walk off ledges. If this value is greater than zero, it is allowed.
        /// </summary>
        public int UnafraidOfHeightsCounter = 0;

        /// <summary>
        /// Delay after landing from a jump that prevents another jump from starting.
        /// </summary>
        public int JumpCooldown = 0;

        /// <summary>
        /// Timer that allows the Spelunker to run the opposite way when encountering an obstacle.
        /// </summary>
        public int IgnoreWrongDirectionCounter = 0;

        /// <summary>
        /// Tracks the target's last position that had line of sight with the Spelunker. This is used when starting a jump.
        /// </summary>
        public Vector2 LastTargetPosition;

        public Vector2 JumpAttackTargetPosition;

        public int TimeSinceLastHadLineOfSight = 2000;

        /// <summary>
        /// Whether or not the Spelunker shell can fall through platforms. False by default.
        /// </summary>
        public bool FallThroughPlatforms = false;

        /// <summary>
        /// Returns if the Spelunker is standing on any tiles. If it is currently jumping, this will exclude platforms.
        /// </summary>
        public bool OnTopOfTiles
        {
            get
            {
                Vector2 collisionPosition = NPC.position;
                int collisionWidth = NPC.width;
                int collisionHeight = NPC.height;
                ShrinkHitbox(NPC, ref collisionPosition, ref collisionWidth, ref collisionHeight);
                return FablesUtils.SolidCollisionFix(collisionPosition, collisionWidth, collisionHeight + 8, !FallThroughPlatforms);
            }
        }

        public bool InsideTiles
        {
            get
            {
                Vector2 collisionPosition = NPC.position;
                int collisionWidth = NPC.width;
                int collisionHeight = NPC.height;
                ShrinkHitbox(NPC, ref collisionPosition, ref collisionWidth, ref collisionHeight);
                return FablesUtils.SolidCollisionFix(collisionPosition, collisionWidth, collisionHeight, FallThroughPlatforms);
            }
        }

        public static int BannerType
        {
            get;
            private set;
        }

        public static AutoloadedBanner BannerTile
        {
            get;
            private set;
        }
        #endregion

        #region Setup
        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Spelunker Scarab", AssetDirectory.Banners, out AutoloadedBanner banner);
            BannerTile = banner;
            FablesNPC.ModifyCollisionParametersEvent += ShrinkHitbox;
            FablesGeneralSystemHooks.CustomShimmerEffects += SparkleOn;
        }

        private void ShrinkHitbox(NPC npc, ref Vector2 collisionPosition, ref int collisionWidth, ref int collisionHeight)
        {
            //Shave off the top
            if (npc.type == Type)
            {
                collisionHeight -= 24;
                collisionPosition.Y += 24;

                int shrinkX = 24;
                if (npc.ai[3] == 0)
                    shrinkX += 10;

                collisionWidth -= shrinkX;
                collisionPosition.X += shrinkX / 2;
            }
        }


        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new(0)
            {
                SpriteDirection = 1
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;

            NPCID.Sets.TrailCacheLength[Type] = 6;
            NPCID.Sets.TrailingMode[Type] = 0;
            Main.npcFrameCount[Type] = 10;
            BannerTile.NPCType = Type;



            if (CalamityFables.SpiritEnabled)
            {
                if (CalamityFables.SpiritReforged.TryFind("CarvedLapisItem", out ModItem lapisItem))
                    SpiritRLapisItemID = lapisItem.Type;
                if (CalamityFables.SpiritReforged.TryFind("CarvedLapis", out ModTile lapisTile))
                    SpiritRLapisTileID = lapisTile.Type;
                if (CalamityFables.SpiritReforged.TryFind("RedSandstoneBrick", out ModTile redSandstone))
                    SpiritRZigguratTile1ID = redSandstone.Type;
                if (CalamityFables.SpiritReforged.TryFind("RedSandstoneBrickCracked", out ModTile redSandstoneCracked))
                    SpiritRZigguratTile2ID = redSandstoneCracked.Type;
                if (CalamityFables.SpiritReforged.TryFind("RedSandstoneSlab", out ModTile redSandstoneSlab))
                    SpiritRZigguratTile3ID = redSandstoneSlab.Type;
            }
        }

        public override void SetDefaults()
        {
            AIType = -1;
            NPC.aiStyle = -1;
            NPC.damage = 30;
            NPC.width = 50;
            NPC.height = 40;
            NPC.defense = 4;
            NPC.lifeMax = 175;
            NPC.knockBackResist = 0.05f;
            NPC.value = Item.buyPrice(silver: 3, copper: 50);

            NPC.HitSound = HitSound;
            NPC.lavaImmune = true;

            // Make the scarabs noticeable by the lifeform analyzer.
            NPC.rarity = 3;

            Banner = NPC.type;
            BannerItem = BannerType;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Set the variant
            NPC.netUpdate = true;

            // 0-1 value representing depth between the top of the caverns and top of the underworld
            float depthValue = Utils.GetLerpValue((float)Main.rockLayer, Main.UnderworldLayer, NPC.position.ToTileCoordinates().Y, true);

            BoulderOre = WorldGen.SavedOreTiers.Iron;

            // Choose armor layer at random
            BoulderOre = !Main.rand.NextBool(3) ? WorldGen.SavedOreTiers.Gold : Main.rand.NextBool() ? WorldGen.SavedOreTiers.Silver : WorldGen.SavedOreTiers.Iron;

            int evilOreSpawnChance = (int)MathHelper.Lerp(16, 4, depthValue);
            if (Main.rand.NextBool(evilOreSpawnChance))
                BoulderOre = WorldGen.crimson ? TileID.Crimtane : TileID.Demonite;

            //Lapis beetle with spiritR
            if (CalamityFables.SpiritEnabled && SpiritRLapisTileID != -1)
            {
                Tile bottomTile = Framing.GetTileSafely((NPC.Bottom + Vector2.UnitY * 7f).ToTileCoordinates());
                if (bottomTile.HasTile && (bottomTile.TileType == SpiritRZigguratTile1ID || bottomTile.TileType == SpiritRZigguratTile2ID || bottomTile.TileType == SpiritRZigguratTile3ID))
                    BoulderOre = SpiritRLapisTileID;
            }

            BoulderDurability = Main.rand.Next(BOULDER_DURABILITY_RANGE.X, BOULDER_DURABILITY_RANGE.Y);

            // Set last target position to the default
            LastTargetPosition = NPC.Center;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,

                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.SpelunkerScarab")
            });
        }
        #endregion

        #region Syncing
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(BoulderDurability);
            writer.Write(WalkDestinationX);
            writer.Write(ChangedAIStateLastFrame);
            writer.Write(UnafraidOfHeightsCounter);
            writer.Write(JumpCooldown);
            writer.Write(IgnoreWrongDirectionCounter);
            writer.WriteVector2(LastTargetPosition);
            writer.WriteVector2(JumpAttackTargetPosition);
            writer.Write(TimeSinceLastHadLineOfSight);
            writer.Write((byte)FallThroughPlatforms.ToInt());
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            BoulderDurability = reader.ReadSingle();
            WalkDestinationX = reader.ReadSingle();
            ChangedAIStateLastFrame = reader.ReadBoolean();
            UnafraidOfHeightsCounter = reader.ReadInt32();
            JumpCooldown = reader.ReadInt32();
            IgnoreWrongDirectionCounter = reader.ReadInt32();
            LastTargetPosition = reader.ReadVector2();
            JumpAttackTargetPosition = reader.ReadVector2();
            TimeSinceLastHadLineOfSight = reader.ReadInt32();
            FallThroughPlatforms = reader.ReadByte() != 0;
        }
        #endregion

        public override void AI()
        {
            // keep the same target while preparing to jump in MP
            if ((CurrentAIState != AIState.JumpingStart && CurrentAIState != AIState.Jumping && CurrentAIState != AIState.GroundPoundStart && CurrentAIState != AIState.GroundPound) || Main.netMode == NetmodeID.SinglePlayer)
                NPC.TargetClosest();

            //Cant shimmer if already shimmered
            if (IsGlimmering)
            {
                NPC.shimmering = false;
                Color glowColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.4f) % 1, 0.7f, 0.75f);
                Lighting.AddLight(NPC.Center, glowColor.ToVector3());
            }

            if (CurrentAIState != AIState.Jumping && CurrentAIState != AIState.GroundPound)
                NPC.noTileCollide = false;
            NPC.noGravity = false;
            NPC.behindTiles = false;
            FallThroughPlatforms = false;
            NPC.knockBackResist = 0.04f;

            // Reset liquid movement speed since they change when jumping
            NPC.waterMovementSpeed = 0.5f;
            NPC.lavaMovementSpeed = 0.5f;
            NPC.honeyMovementSpeed = 0.25f;
            NPC.shimmerMovementSpeed = 0.375f;
            NPC.GravityIgnoresLiquid = false;

            AIState frameStartAIState = CurrentAIState;

            // Redirect to AI state specfiic behavior
            switch (CurrentAIState)
            {
                case AIState.Idle:
                    AI_Idle();
                    break;
                case AIState.WalkAround:
                    AI_WalkAround();
                    break;
                case AIState.RunAwayFromPlayer:
                    AI_RunAwayFromPlayer();
                    break;
                case AIState.JumpingStart:
                    AI_JumpingStart();
                    break;
                case AIState.Jumping:
                    AI_Jumping();
                    break;
                case AIState.GroundPoundStart:
                    AI_GroundPoundStart();
                    break;
                case AIState.GroundPound:
                    AI_GroundPound();
                    break;
                case AIState.Stuck:
                    AI_Stuck();
                    break;
                case AIState.StuckRecovery:
                    AI_StuckRecovery();
                    break;
            }

            if (!BoulderBroken)
            {
                bool jumpAttack = CurrentAIState == AIState.Jumping || CurrentAIState == AIState.GroundPoundStart || CurrentAIState == AIState.GroundPound;
                float jumpAttackProgress = 0.4f + (CurrentAIState == AIState.GroundPoundStart ? AICounter / (float)GROUND_POUND_START_LENGTH : 0) * 0.6f;
                if (!jumpAttack)
                    jumpAttackProgress = 0;
                if (CurrentAIState == AIState.GroundPound)
                    jumpAttackProgress = 1f;

                float baseGlowMult = (BoulderOre == TileID.Crimtane || BoulderOre == TileID.Demonite) ? 1f : 0.3f;
                baseGlowMult = Math.Max(baseGlowMult, jumpAttackProgress);

                if (Main.rand.NextBool(130 - (int)(jumpAttackProgress * 90)))
                {
                    Vector2 dustPosition = NPC.Center + Main.rand.NextVector2Circular(26, 26);
                    SpelunkerScarabOreChunk.CreateOreSparkles(dustPosition, BoulderOre);
                }

                Color glowColor = BoulderOre switch
                {
                    TileID.Iron => new Color(189, 159, 139),
                    TileID.Silver => new Color(171, 182, 183),
                    TileID.Gold => new Color(231, 213, 65),
                    TileID.Lead => new Color(104, 140, 150),
                    TileID.Tungsten => new Color(154, 190, 155),
                    TileID.Platinum => new Color(181, 194, 217),
                    TileID.Crimtane => Color.Red,
                    TileID.Demonite => CommonColors.DemoniteBlue,
                    _ => Color.Black
                };

                glowColor *= baseGlowMult;
                Lighting.AddLight(NPC.Center, glowColor.ToVector3());

                // Handle rolling loop
                if (!SoundEngine.TryGetActiveSound(RollingSoundSlot, out var sound))
                    RollingSoundSlot = SoundEngine.PlaySound(RollingLoop, NPC.Center);
                if (SoundEngine.TryGetActiveSound(RollingSoundSlot, out sound))
                {
                    if (NPC.velocity.Y == 0)
                        RollingVolume = Utils.GetLerpValue(0, 5f, Math.Abs(NPC.velocity.X), true);
                    else if (RollingVolume > 0)
                        RollingVolume = Math.Max(RollingVolume - 0.06f, 0f);

                    // Increases volume with speed
                    sound.Position = NPC.Center;
                    sound.Volume = RollingVolume;
                    sound.Pitch = MathHelper.Lerp(-0.25f, 0f, RollingVolume);

                    sound.Update();
                }

                SoundHandler.TrackSound(RollingSoundSlot);
            }

            if (UnafraidOfHeightsCounter > 0)
                UnafraidOfHeightsCounter--;

            if (JumpCooldown > 0)
                JumpCooldown--;

            if (IgnoreWrongDirectionCounter > 0)
                IgnoreWrongDirectionCounter--;

            AICounter++;

            if (NPC.target != -1 && TimeSinceLastHadLineOfSight > 60 && Collision.CanHitLine(NPC.position + new Vector2(NPC.width / 2, 0), 1, 1, Target.Center, 1, 1))
                TimeSinceLastHadLineOfSight = 0;
            TimeSinceLastHadLineOfSight++;

            ChangedAIStateLastFrame = CurrentAIState != frameStartAIState;
            if (ChangedAIStateLastFrame && frameStartAIState == AIState.Jumping && CurrentAIState != AIState.GroundPoundStart)
                scarabFadeInTimer = 1f;
            if (scarabFadeInTimer > 0)
            {
                scarabFadeInTimer -= 1 / 20f;
                if (scarabFadeInTimer < 0)
                    scarabFadeInTimer = 0;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            int damage = hit.SourceDamage;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int spawnChance = (int)MathHelper.Lerp(5, 1, damage / (NPC.lifeMax * 0.06f));
                if (spawnChance <= 1 || Main.rand.NextBool(spawnChance))
                    SpawnOreChunk(6, CurrentAIState != AIState.Jumping && CurrentAIState != AIState.GroundPound && CurrentAIState != AIState.GroundPoundStart);
            }

            if (NPC.life <= 0 && !Main.dedServ)
            {
                Vector2 gorePos = NPC.Center;
                if (!BoulderBroken)
                {
                    gorePos.Y -= 20;
                    if (ScarabAnimation == ScarabAnim.ChuckBoulder)
                        gorePos = lastScarabDrawPosition + Main.screenPosition - Vector2.UnitY * 16f;
                    if (ScarabAnimation == ScarabAnim.BoulderKickAnticipation || ScarabAnimation == ScarabAnim.BoulderKick || ScarabAnimation == ScarabAnim.FallDown)
                        gorePos = lastScarabWorldPosition - Vector2.UnitY * 16f;
                }

                ParticleHandler.SpawnParticle(new SpelunkerScarabGore(gorePos, new Vector2(hit.HitDirection * Main.rand.NextFloat(2f, 4f), -10f)));
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.PlayerSafe)
                return 0f;

            Player player = spawnInfo.Player;

            //Same spawn chance as graverobbers
            if (CalamityFables.SpiritEnabled)
            {
                int aboveWallType = Framing.GetTileSafely(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1).WallType;
                if ((player.ZoneDesert || player.ZoneUndergroundDesert) && (spawnInfo.SpawnTileType == SpiritRZigguratTile1ID || spawnInfo.SpawnTileType == SpiritRZigguratTile2ID || spawnInfo.SpawnTileType == SpiritRZigguratTile3ID)
                    && aboveWallType != WallID.None && !Main.wallHouse[aboveWallType])
                    return 0.18f;
            }

            if (player.ZoneSnow || player.ZoneJungle)
                return 0f;

            return SpawnCondition.Cavern.Chance * 0.065f;
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            // Fucking dies if it has no armor
            if (BoulderBroken)
                modifiers.SetInstantKill();
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => CurrentAIState == AIState.Jumping || CurrentAIState == AIState.GroundPound || (CurrentAIState == AIState.Stuck && AICounter <= 1);

        public override bool? CanFallThroughPlatforms() => FallThroughPlatforms;

        #region AI
        #region AI code
        private void AI_Idle()
        {
            // Sit in place
            NPC.velocity.X *= 0.91f;

            // Wait a bit before moving around
            if (BoulderBroken || (AICounter >= 60f && Main.rand.NextBool(20)))
            {
                CurrentAIState = AIState.WalkAround;
                return;
            }

            RunFromNearbyPlayer();
            CheckForJumpTarget();

            if (NPC.wet && NPC.velocity.Y > -2f)
                NPC.velocity.Y -= 0.3f;
            NPC.rotation += NPC.velocity.X * 0.03f;
            ScarabAnimation = ScarabAnim.Idle;
        }

        private void AI_WalkAround()
        {
            // Immediately find a new destination if the wander state just started
            if (ChangedAIStateLastFrame)
                FindDestination(0);

            // Update horizontal velocity to move across the ground
            float speed = BoulderBroken ? 5.5f : 3f;
            float acceleration = speed * 0.02f;
            GroundMotion(new Vector2(WalkDestinationX, NPC.Center.Y), speed, acceleration, out bool obstactleAhead);

            // Determine if the destination has been reached.
            bool reachedDestination = 10f > MathHelper.Distance(NPC.Center.X, WalkDestinationX);

            // Change destination if the NPC was stopped by an obstacle, the destination has been reached, or took to long to reach
            if (obstactleAhead || reachedDestination || AICounter >= 300)
            {  
                int direction = 0;

                // If an obstacle's ahead, the direction of the next target will be opposite of the current direction
                if (obstactleAhead)
                    direction = Math.Sign(NPC.position.X - WalkDestinationX);

                // Otherwise, there's a random chance to enter the idle state
                else if (Main.rand.NextBool())
                    CurrentAIState = AIState.Idle;

                // Find a new target
                FindDestination(direction);
            }

            RunFromNearbyPlayer();
            CheckForJumpTarget();

            // Ambient Sounds
            if (NPC.soundDelay <= 0 && Main.rand.NextBool(60))
            {
                SoundEngine.PlaySound(AmbientSound, NPC.Center);
                NPC.soundDelay = 270;
            }
        }

        private void AI_RunAwayFromPlayer()
        {
            // Find direction to run from player
            int direction = (NPC.Center.X - Target.Center.X).NonZeroSign();

            // Immediately find a new destination if the run away state just started
            if (ChangedAIStateLastFrame)
                FindDestination(direction);

            // Update horizontal velocity to move across the ground 
            float speed = BoulderBroken ? 8.5f : 5f;
            float acceleration = speed * 0.04f;
            GroundMotion(new Vector2(WalkDestinationX, NPC.Center.Y), speed, acceleration, out bool obstactleAhead);

            // Determine if the destination has been reached.
            bool reachedDestination = 10f > MathHelper.Distance(NPC.Center.X, WalkDestinationX);

            // Check if target is in range
            bool targetWithinRange = NPC.WithinRange(Target.Center, SCARED_RANGE);

            // Check if the direction being traveled is away from the target
            // Can ignore if it's facing the wrong way
            bool wrongDirection = IgnoreWrongDirectionCounter <= 0 ? direction != (WalkDestinationX - NPC.Center.X).NonZeroSign() : false;

            if (wrongDirection || reachedDestination || AICounter >= 300)
            {
                // Change destination if target is still near
                if (targetWithinRange)
                    FindDestination(direction);
                // Or change to a normal AI state
                else
                    CurrentAIState = Main.rand.NextBool() ? AIState.WalkAround : AIState.Idle;             
            }

            if (obstactleAhead)
            {
                // Change to a normal AI state if the target isn't near
                if (!targetWithinRange)
                    CurrentAIState = Main.rand.NextBool() ? AIState.WalkAround : AIState.Idle;
                // Or try to jump. If it cannot, run the other way
                else if (!CheckForJumpTarget(true))
                {
                    IgnoreWrongDirectionCounter = 120;
                    FindDestination(-direction);
                }
            }

            // Ambient Sounds
            if (NPC.soundDelay <= 0 && Main.rand.NextBool(60))
            {
                SoundEngine.PlaySound(AmbientSound, NPC.Center);
                NPC.soundDelay = 270;
            }
        }

        private void AI_JumpingStart()
        {
            if (AICounter == 0)
                ResetScarabFrame();

            // Sit in place
            NPC.velocity.X *= 0.8f;

            // Find current jump distance and velocity
            GetJumpDistanceAndVelocity(Target.Center, out Vector2 distance, out Vector2 velocity, out float maxHeight);

            // Change NPC sprite direction to face target
            int direction = Math.Sign(distance.X);
            if (direction != 0)
                NPC.spriteDirection = direction;

            // Check if the spelunker still has line of sight and update last target position
            if (WithinJumpTargettingRange(Target.Center, distance, maxHeight))
            {
                LastTargetPosition = Target.Center;
                NPC.netUpdate = true;
            }

            // Wait a bit before jumping
            if (AICounter >= BOULDER_CHUCK_START_LENGTH)
            {
                // Recalculate jump velocity based on last target position
                // If there is no target position, denoted by it being the same as the NPC center, use the target center regardless of line of sight
                GetJumpDistanceAndVelocity(LastTargetPosition != NPC.Center ? LastTargetPosition : Target.Center, out _, out Vector2 finalVelocity, out _);

                // Do the jump
                NPC.velocity = finalVelocity;
                NPC.noTileCollide = true;
                JumpAttackTargetPosition = LastTargetPosition != NPC.Center ? LastTargetPosition : Target.Center;
                SoundEngine.PlaySound(JumpSound, NPC.Center);
                CurrentAIState = AIState.Jumping;
                ScarabAnimation = ScarabAnim.ChuckBoulder;
                return;
            }

            // Use idle animation state
            ScarabAnimation = ScarabAnim.BoulderSuplex;
        }

        private void AI_Jumping()
        {
            // Lower knockback resist
            NPC.knockBackResist = 0f;

            // Unimpeded liquid movement
            NPC.waterMovementSpeed = 1f;
            NPC.lavaMovementSpeed = 1f;
            NPC.honeyMovementSpeed = 1f;
            NPC.shimmerMovementSpeed = 1f;
            NPC.GravityIgnoresLiquid = true;

            // Fall through platforms if the target is below
            FallThroughPlatforms = Target.Center.Y > NPC.Center.Y;

            // Rotate
            int direction = NPC.velocity.X.NonZeroSign();
            NPC.spriteDirection = direction;
            NPC.rotation += NPC.velocity.X * 0.05f;

            // Ignores collision
            NPC.noTileCollide = true;

            // Exits state when landing on tiles
            if (OnTopOfTiles && NPC.Center.Y >= JumpAttackTargetPosition.Y - 16 && NPC.velocity.Y >= 0)
            {
                JumpCooldown = 300;
                LastTargetPosition = NPC.Center;
                UnstuckBoulder();
                ShakeBoulder(10, false);
                SoundEngine.PlaySound(BoulderSlamSound, NPC.Center);
                CurrentAIState = AIState.StuckRecovery;
                ScarabAnimation = ScarabAnim.BoulderPopOutRecoil;
                return;
            }

            Vector2 distanceToTarget = NPC.Center - Target.Center;
            bool normalGroundPoundWithLineOfSight = distanceToTarget.Y < 0 && Math.Abs(distanceToTarget.X + NPC.velocity.X) < 16 && Collision.CanHit(NPC, Target) && FablesUtils.DepthFromPoint(NPC.Center, 10, true) > 5;

            bool forcedGroundPoundXCoordReached = Math.Max((NPC.Center.X - JumpAttackTargetPosition.X) * NPC.spriteDirection, (NPC.Center.X - Target.Center.X) * NPC.spriteDirection) >= 0 && InsideTiles;


            // Transition to ground pound if the target is directly below
            if (normalGroundPoundWithLineOfSight || forcedGroundPoundXCoordReached)
            {
                JumpCooldown = 500;
                LastTargetPosition = NPC.Center;
                CurrentAIState = AIState.GroundPoundStart;
                ScarabAnimation = ScarabAnim.BoulderKickAnticipation;
                return;
            }

            // Bouncin
            if (NPC.collideX)
            {
                // Always bounce up
                if (NPC.velocity.Y > 0)
                    NPC.velocity.Y *= -1f;
                NPC.velocity.X *= -1;
                NPC.netUpdate = true;
            }

            // Use jumping animation state
            ScarabAnimation = ScarabAnim.ChuckBoulder;
        }

        private void AI_GroundPoundStart()
        {
            // Cannot move in state
            NPC.velocity.X *= 0.7f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;

            // Fall through platforms if the target is below
            FallThroughPlatforms = Target.Center.Y > NPC.Center.Y;

            // Go up a little on the first frame
            if (AICounter <= 1)
            {
                NPC.velocity.Y -= 4f;
                NPC.netUpdate = true;
            }

            NPC.GravityMultiplier *= 0f;

            // Rotate
            float animProgress = Utils.GetLerpValue(0, GROUND_POUND_START_LENGTH, AICounter);
            NPC.rotation += -NPC.direction * MathF.Pow(1 - animProgress, 3f) * 0.3f;

            NPC.velocity.Y = MathF.Pow(1 - animProgress, 5f) * -4f;
            lastScarabWorldPosition = NPC.Center;
            detachedScarabYVelocity = 0f;

            // Wait a bit before body slam
            if (AICounter >= GROUND_POUND_START_LENGTH)
            {
                CurrentAIState = AIState.GroundPound;
                ScarabAnimation = ScarabAnim.BoulderKick;
                SoundEngine.PlaySound(KickBoulderSound, NPC.Center);
                return;
            }

            // Use jumping animation state
            ScarabAnimation = ScarabAnim.BoulderKickAnticipation;
        }

        private void AI_GroundPound()
        {
            if (AICounter <= 1 && NPC.velocity.Y < 6)
                NPC.velocity.Y = 6;

            // Cannot move in state
            NPC.velocity.X *= 0.7f;
            NPC.knockBackResist = 0f;

            // Unimpeded liquid movement
            NPC.waterMovementSpeed = 1f;
            NPC.lavaMovementSpeed = 1f;
            NPC.honeyMovementSpeed = 1f;
            NPC.shimmerMovementSpeed = 1f;
            NPC.GravityIgnoresLiquid = true;
            NPC.noTileCollide = true;

            // Fall through platforms if the target is below
            FallThroughPlatforms = Target.Center.Y > NPC.Center.Y;

            // Fall much faster than normal
            NPC.noGravity = true;
            if (NPC.velocity.Y < 30f)
                NPC.velocity.Y += 1.2f;

            //Used for velocity of falling scarab when transitionning to the ground
            detachedScarabYVelocity = 0.2f;

            // Switch to stuck AI state upon hitting tiles
            if (OnTopOfTiles && NPC.Center.Y >= JumpAttackTargetPosition.Y - 16)
            {
                NPC.velocity = Vector2.Zero;
                GroundPoundEffects();
                CurrentAIState = AIState.Stuck;
                SoundEngine.PlaySound(BoulderSlamSound, NPC.Center);
                UnstuckBoulder();
                return;
            }
        }

        private void AI_Stuck()
        {
            NPC.noGravity = true;
            // Cannot move in state
            NPC.velocity *= 0f;
            NPC.knockBackResist = 0f;
            NPC.behindTiles = true;

            //If inside a block, rise up to 1 block up
            if (AICounter <= 1)
            {
                UnstuckBoulder();
            }

            // Get unstuck after a while
            if (AICounter >= STUCK_LENGTH)
            {
                CurrentAIState = BoulderDurability > 0 ? AIState.StuckRecovery : AIState.RunAwayFromPlayer;

                //Shatter the boulder
                if (BoulderDurability <= 0)
                {
                    // Break sound
                    SoundEngine.PlaySound(BreakBoulderSound, NPC.Center);

                    NPC.position += Vector2.UnitY * -20;
                    NPC.velocity.Y = -8;
                    BoulderOre = 0;
                    BreakIntoRocks();
                }
                // Normal sound when not broken
                else
                    SoundEngine.PlaySound(BoulderDislodgeSound, NPC.Center);

                return;
            }

            detachedScarabYVelocity += 2.6f;
            lastScarabWorldPosition.Y += detachedScarabYVelocity;
            if (lastScarabWorldPosition.Y < NPC.Center.Y)
            {
                ScarabAnimation = ScarabAnim.FallDown;
            }
            else if (ScarabAnimation != ScarabAnim.AngryStomp && ScarabAnimation != ScarabAnim.Stomp)
            {
                ShakeBoulder(10);
                boulderVisualShake += 1.6f;
                ScarabAnimation = Main.rand.NextBool() ? ScarabAnim.Stomp : ScarabAnim.AngryStomp;
            }
        }

        public void UnstuckBoulder()
        {
            bool shifted = false;
            Vector2 collisionPosition = NPC.position;
            int collisionWidth = NPC.width;
            int collisionHeight = NPC.height;
            ShrinkHitbox(NPC, ref collisionPosition, ref collisionWidth, ref collisionHeight);

            for (int i = 0; i < 18; i++)
            {
                if (Collision.SolidCollision(collisionPosition, collisionWidth, collisionHeight, true))
                {
                    shifted = true;
                    NPC.position.Y -= 1f;
                    collisionPosition.Y--;
                }
            }
            if (shifted)
            {
                NPC.netUpdate = true;
                NPC.velocity.Y = 0;
                NPC.noTileCollide = false;
            }
        }

        public void ShakeBoulder(int dustCount, bool doSound = true)
        {
            boulderVisualShake = 1f;
            if (doSound)
                 SoundEngine.PlaySound(BoulderTrampleSound with { MaxInstances = -1 , Volume = 0.5f}, NPC.Center);

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustPosition = NPC.Bottom;
                Point tilePosition = dustPosition.ToSafeTileCoordinates();
                int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

                Dust dust = Main.dust[dustIndex];
                dust.position = dustPosition + (Vector2.UnitX * (Main.rand.NextFloat(NPC.width) - NPC.width / 2));
                dust.velocity.Y -= Main.rand.NextFloat(1.5f, 3f);
                dust.velocity.X *= 0.5f;
                dust.noLightEmittence = true;
            }
        }

        private void AI_StuckRecovery()
        {
            // Jump
            if (AICounter <= 1)
            {
                // Reset velocity, then increase it
                NPC.velocity =  Vector2.UnitY * -6f;
                NPC.noTileCollide = true;
                NPC.netUpdate = true;
            }

            // Wait a bit before going back to normal
            if (AICounter >= STUCK_RECOVERY_LENGTH)
            {
                CurrentAIState = AIState.Idle;
                ScarabAnimation = ScarabAnim.Idle;
                return;
            }

            // Use falling animation state
            ScarabAnimation = ScarabAnim.BoulderPopOutRecoil;
        }

        public void RunFromNearbyPlayer()
        {
            // Check if target has line of sight and is within range
            bool lineOfSight = !Target.invis && Collision.CanHit(NPC, Target) && NPC.WithinRange(Target.Center, SCARED_RANGE);

            // Switch to running state if hit or line of sight
            if (NPC.justHit || lineOfSight)
            {
                // Run away sound
                SoundEngine.PlaySound(WarningSound with { Volume = 0.4f }, NPC.Center);
                CurrentAIState = AIState.RunAwayFromPlayer;
            }
        }

        public bool CheckForJumpTarget(bool forceJump = false)
        {
            // Cant jump if not on tiles or the spelunker has no boulder
            if ((!OnTopOfTiles && !NPC.wet) || BoulderBroken)
                return false;

            // Unless the jump is forced, check the jump cooldown, AI state, and make sure the target's in range
            if (!forceJump)
                if (JumpCooldown > 0 || CurrentAIState == AIState.RunAwayFromPlayer || Target.invis || NPC.WithinRange(Target.Center, SCARED_RANGE))
                    return false;

            // Get jump distance and maximum height
            GetJumpDistanceAndVelocity(Target.Center, out Vector2 distance, out _, out float maxHeight);



            // Jump path must be clear and there must be vertical space above the target
            if (!WithinJumpTargettingRange(Target.Center, distance, maxHeight))
                return false;

            if (TimeSinceLastHadLineOfSight > 500)
                return false;

            // Switch to start jump state
            CurrentAIState = AIState.JumpingStart;
            LastTargetPosition = Target.Center;
            return true;
        }
        #endregion

        #region Ground pound effects
        /// <summary>
        /// Returns if the current armor layer was broken.
        /// </summary>
        /// <returns></returns>
        public void GroundPoundEffects()
        {
            int projectileAmount = Main.rand.Next(4, 8);
            for (int i = 0; i < projectileAmount; i++)
                if (SpawnOreChunk(7.5f))
                    return;
        }

        public void BreakIntoRocks()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 6; i++)
                    SpawnOreChunk(7.5f, false, TileID.Stone);
            }
        }

        public bool SpawnOreChunk(float speed = 6f, bool canBreakBoulder = false, int typeOverride = -1)
        {
            if (BoulderBroken && typeOverride == -1)
                return true;

            Vector2 position = NPC.Center + Vector2.UnitY * -8 + Main.rand.NextVector2Circular(20, 20);

            if (CurrentAIState == AIState.Stuck)
                position.Y += 12;

            Vector2 velocity = (Vector2.UnitY * -Main.rand.NextFloat(0.8f, 1.2f) * speed).RotatedByRandom(0.9f);

            // Create ore projectile
            int projectileDamage = NPC.damage / 2; //projectiles always deal double damage
            if (Main.masterMode)
                projectileDamage /= 3; //MM projectiles deal 3x damage but were already scaling it off the NPC's damage so its unnecessary
            else if (Main.expertMode)
                projectileDamage /= 2; //expert projectiles deal 2x damage but were already scaling it off the NPC's damage so its unnecessary

            if (typeOverride == TileID.Stone)
                projectileDamage /= 5;

            Projectile.NewProjectile(NPC.GetSource_FromThis(), position, velocity, ModContent.ProjectileType<SpelunkerScarabOreChunk>(), (int)(projectileDamage * 0.75f), 0, Main.myPlayer, typeOverride== -1 ? BoulderOre : typeOverride);

            // Reduce armor durability
            BoulderDurability -= 1.5f;
            if (canBreakBoulder && BoulderDurability <= 0)
            {
                CurrentAIState = AIState.RunAwayFromPlayer;
                BoulderOre = 0;
                BreakIntoRocks();
            }
            return BoulderDurability <= 0;
        }
        #endregion

        #region Movement Helpers
        public void GroundMotion(Vector2 goal, float speed, float acceleration, out bool obstacleAhead)
        {
            // Check for an obstacle. Gaps will be ignored if an obstacle stopped the NPC recently
            obstacleAhead = CheckForWall(BoulderBroken ? 5 : 3, 6, out bool shortObstacle) || (UnafraidOfHeightsCounter <= 0 && CheckForGap(4, 2));

            // Move if an obstacle has not been detected
            if (!obstacleAhead)
            {
                if (AICounter > 1 && (OnTopOfTiles || NPC.wet) && shortObstacle)
                {
                    // Sets velocity to max speed
                    NPC.velocity.X = speed * NPC.spriteDirection;
                    NPC.velocity.Y = BoulderBroken  ? -7 : - 6f;
                    //NPC.noTileCollide = true;
                    NPC.netUpdate = true;
                }

                // Approach the destination
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.SafeDirectionTo(goal).X * speed, acceleration);

                // Look in the direction of movement
                if (NPC.velocity.X != 0f)
                    NPC.spriteDirection = NPC.velocity.X.NonZeroSign();
            }
            else
            {
                UnafraidOfHeightsCounter = Main.rand.Next(60, 120);
                NPC.netUpdate = true;
            }

            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

            if (NPC.wet && NPC.velocity.Y > -2f)
                NPC.velocity.Y -= 0.3f;

            NPC.rotation += NPC.velocity.X * 0.03f;
            ScarabAnimation = BoulderBroken ? ScarabAnim.RunAway : ScarabAnim.RollBoulder;
        }

        public void FindDestination(int direction)
        {
            // If direction is zero, randomize it
            if (direction == 0)
                direction = Main.rand.NextFromList(-1, 1);

            // Pick a random nearby point as the destination
            WalkDestinationX = NPC.Center.X + Main.rand.NextFloat(160f, 400f) * direction;

            // Reset AI timer upon changing target
            AICounter = 0;
            NPC.netUpdate = true;
        }

        public bool CheckForWall(int shortObstacleHeight, int tallObstacleHeight, out bool shortObstacle, int checkAhead = 1)
        {
            shortObstacle = false;

            // Dont even bother if not standing on tiles
            if (!OnTopOfTiles && !NPC.wet)
                return false;

            // Direction of the walking destination from the NPC center, rather than the sprite direction
            int direction = (WalkDestinationX - NPC.Center.X).NonZeroSign();

            // Starting point for collision checks
            Vector2 npcPosition = direction == 1 ? NPC.BottomRight : NPC.BottomLeft;
            Point startPosition = npcPosition.ToSafeTileCoordinates();

            // Find height to check for walls. Checking from the NPC's height will sometimes result in it getting stuck where it shouldn't
            // First checks if theres an actual gap ahead. If there isn't one, it can also result in it getting stuck
            int startHeight = 0;
            if (FablesUtils.CanHitLine(startPosition, startPosition with { X = startPosition.X + 2 * direction }))
                startHeight = FablesUtils.DepthFromPoint(startPosition, 2);

            // Streamlines code
            // Performs the collision check at the specified height
            bool CollisionCheck(int height)
            {
                Point lineStartPosition = startPosition with { Y = startPosition.Y - height + startHeight };
                Point lineEndPosition = lineStartPosition with { X = lineStartPosition.X + checkAhead * direction };

                return !FablesUtils.CanHitLine(lineStartPosition, lineEndPosition);
            }

            // First checks up to the short obstactle height. If anything is detected, shortObstacle is set to true
            // Starts checking at a lower height if X velocity is zero.
            for (int i = NPC.velocity.X == 0 ? 1 : 2; i <= shortObstacleHeight; i++)
                shortObstacle |= CollisionCheck(i);

            // If there's a short obstacle, check for a tall obstacle. If anything is detected, shortObstacle is set to false and the method returns true
            if (shortObstacle)
                for (int j = shortObstacleHeight + 1; j <= tallObstacleHeight; j++)
                    if (CollisionCheck(j))
                    {
                        shortObstacle = false;
                        return true;
                    }

            return false;
        }

        public bool CheckForGap(int maxDepth, int checkAhead)
        {
            // Dont even bother if not standing on tiles
            if (!OnTopOfTiles)
                return false;

            // Direction of the walking destination from the NPC center, rather than the sprite direction
            int direction = (WalkDestinationX - NPC.Center.X).NonZeroSign();

            // Starting point for collision checks
            Vector2 npcPosition = direction == 1 ? NPC.BottomRight : NPC.BottomLeft;
            Point startPosition = npcPosition.ToSafeTileCoordinates();

            // Find height to check for gaps. Checking from the NPC's height will sometimes result in it getting stuck where it shouldn't
            int startDepth = FablesUtils.DepthFromPoint(startPosition with { X = startPosition.X - direction }, 3);

            // Iterate over several tiles ahead to ignore small gaps
            for (int i = 0; i <= checkAhead; i++)
            {
                // Return false if solid tiles were detected. Otherwise, iterate again
                if (!FablesUtils.CanHitLine(startPosition, startPosition with { Y = startPosition.Y + maxDepth + startDepth - 1 }))
                    return false;

                startPosition.X += direction;
            }

            return true;
        }
        #endregion

        #region Jump Helpers
        public bool WithinJumpTargettingRange(Vector2 targetPosition, Vector2 distance, float arcHeight)
        {
            // Check if the target is within range first
            if (Math.Abs(distance.X) > HORIZ_JUMP_RANGE || distance.Y < -ABOVE_JUMP_RANGE || distance.Y > BELOW_JUMP_RANGE)
                return false;
            return true;
        }

        public void GetJumpDistanceAndVelocity(Vector2 targetPosition, out Vector2 distance, out Vector2 velocity, out float maxHeight)
        {
            // Find distance between the two points
            distance = targetPosition - NPC.Center;

            // Calculate velocity of the jump and it's maximum height above the target
            Vector2 endPosition = new Vector2(targetPosition.X + distance.X, Math.Min(targetPosition.Y, NPC.Center.Y));
            velocity = FablesUtils.GetArcVel(NPC.Center, endPosition, 0.3f, out maxHeight, 32, null, 12f, Math.Clamp(-distance.Y + 208, 0, 208));
        }
        #endregion

        #endregion

        #region Shimmer
        private void SparkleOn(NPC npc)
        {
            if (npc.type != Type || npc.ModNPC is not SpelunkerScarab crawler)
                return;

            //Become the glimmerald
            crawler.IsGlimmering = true;
            npc.shimmering = false;
            npc.buffImmune[BuffID.Shimmer] = true;
            npc.GravityIgnoresLiquid = true;
            npc.GivenName = Main.rand.Next(["Superbris", "Iralphon", "Luxuriel", "Gulan", "Invideul", "Acedion", "Avarifel"]);
            crawler.BoulderOre = TileID.ShimmerBlock;

            if (Main.netMode == NetmodeID.SinglePlayer)
                Item.ShimmerEffect(npc.Center);
            else
            {
                NetMessage.SendData(MessageID.ShimmerActions, -1, -1, null, 0, (int)npc.Center.X, (int)npc.Center.Y);
                new SpelunkerScarabShimmerPacket(npc).Send();
            }
            NPC.netUpdate = true;
        }

        [Serializable]
        public class SpelunkerScarabShimmerPacket : RenameNPCPacket
        {
            public SpelunkerScarabShimmerPacket(NPC npc) : base(npc) { }

            protected override bool PreSend(int toClient = -1, int ignoreClient = -1)
            {
                if (Main.npc[npc].type != ModContent.NPCType<SpelunkerScarab>())
                    return false;
                return base.PreSend(toClient, ignoreClient);
            }

            protected override void Receive()
            {
                if (Main.netMode == NetmodeID.MultiplayerClient && !CanApply)
                {
                    PacketWaitingList.AddToWaitingList(this);
                    return;
                }

                NPC scarab = Main.npc[npc];
                scarab.shimmering = false;
                scarab.GravityIgnoresLiquid = true;
                scarab.buffImmune[BuffID.Shimmer] = true; 
                scarab.ai[3] = 1;

                base.Receive();
            }
        }
        #endregion

        #region Drawing
        public void ResetScarabFrame()
        {
            NPC.frame.Y = 0;
            NPC.frameCounter = 0;
        }

        private float BestiaryBoulderRotation = 0f;
        public override void FindFrame(int frameHeight)
        {
            int xFrame;

            //Suplex is unique because it goes over 2 rows, therefore when transitionning from another animation into suplex we have to start with the xframe as 2
            if (ScarabAnimation == ScarabAnim.BoulderSuplex)
            {
                xFrame = NPC.frame.X / FRAME_WIDTH;
                if (xFrame != 2 && xFrame != 7)
                    xFrame = 2;                    
            }
            else
                xFrame = (int)ScarabAnimation;


            int yFrame = NPC.frame.Y / FRAME_HEIGHT;
            int lastYFrame = yFrame;

            if (NPC.IsABestiaryIconDummy)
            {
                ScarabAnimation = ScarabAnim.RollBoulder;
                if (BoulderOre == 0)
                    BoulderOre = WorldGen.SavedOreTiers.Gold;

                AICounter++;
                BestiaryBoulderRotation += 0.07f;
            }

            bool loopAnimation = true;
            int animMaxYFrame = 0;
            bool animSpeedScaledByVelocity = false;
            int animSpeed = 6;

            // Specfic frames for each action
            switch (ScarabAnimation)
            {
                case ScarabAnim.BoulderKickAnticipation:
                    loopAnimation = false;
                    animMaxYFrame = 1;
                    animSpeed = 16;
                    break;
                case ScarabAnim.BoulderKick:
                case ScarabAnim.BoulderPopOutRecoil:
                    loopAnimation = false;
                    animMaxYFrame = 1;
                    break;
                case ScarabAnim.RollBoulder:
                    animMaxYFrame = 3;
                    animSpeedScaledByVelocity = true;
                    break;
                case ScarabAnim.AngryStomp:
                    animMaxYFrame = 3;
                    break;
                case ScarabAnim.Stomp:
                    animMaxYFrame = 4;
                    break;
                case ScarabAnim.RunAway:
                    animMaxYFrame = 5;
                    animSpeedScaledByVelocity = true;
                    break;
                case ScarabAnim.BoulderSuplex:
                    animMaxYFrame = 5;
                    animSpeed = 8;
                    loopAnimation = false;
                    break;
            }

            if (yFrame > animMaxYFrame)
            {
                yFrame = 0;
                NPC.frameCounter = 0;
            }

            if (animSpeedScaledByVelocity)
            {
                float NPCspeed = Utils.GetLerpValue(Math.Abs(NPC.velocity.X), 1, 3.95f);
                if (NPCspeed > 0.5f)
                    animSpeed--;
                if (NPCspeed >= 1)
                    animSpeed--;
            }    

            if (animMaxYFrame == 0)
                NPC.frameCounter = 0;
            else
            {
                NPC.frameCounter++;
                if (NPC.frameCounter >= animSpeed)
                {
                    NPC.frameCounter = 0;
                    yFrame++;
                }
                if (yFrame > animMaxYFrame)
                {
                    if (loopAnimation)
                        yFrame = 0;
                    else
                        yFrame = animMaxYFrame;
                }
            }


            if (ScarabAnimation == ScarabAnim.BoulderSuplex)
            {
                //Switch animation columns after the start of the suplex anim
                if (xFrame == 2 && yFrame >= 3)
                {
                    yFrame = 0;
                    xFrame = 7;
                }

                //Last frame of the boulder chuck anim only happens right before the throw
                if (AICounter >= BOULDER_CHUCK_START_LENGTH - 6)
                    yFrame = 6;
            }

            if (ScarabAnimation == ScarabAnim.BoulderPopOutRecoil)
            {
                NPC.frameCounter = 0;
                yFrame = AICounter > STUCK_RECOVERY_LENGTH / 2 ? 1 : 0;
            }

            if (ScarabAnimation == ScarabAnim.AngryStomp && lastYFrame != yFrame && yFrame % 2 == 0)
                ShakeBoulder(3);
            else if (ScarabAnimation == ScarabAnim.Stomp && lastYFrame != yFrame && yFrame == 4)
                ShakeBoulder(6);

            if (boulderVisualShake > 0f)
                boulderVisualShake -= 0.13f;

            NPC.frame = new Rectangle(xFrame * FRAME_WIDTH, yFrame * FRAME_HEIGHT, 48, 76);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Simple draw method since we arent doing much
            Texture2D texture = TextureAssets.Npc[Type].Value;
            Color tintedColor = NPC.IsABestiaryIconDummy ? Color.White : NPC.TintFromBuffAesthetic(drawColor);

            if (CurrentAIState == AIState.Stuck)
                screenPos -= Vector2.UnitY * 16f;

            if (ScarabAnimation == ScarabAnim.BoulderSuplex)
                DrawScarab(tintedColor, false, -screenPos + NPC.GfxOffY());

            if (!BoulderBroken)
            {
                int boulderFrameY = BoulderOre switch
                {
                    TileID.Iron => 234,
                    TileID.Silver => 390,
                    TileID.Gold => 494,
                    TileID.Lead => 286,
                    TileID.Tungsten => 338,
                    TileID.Platinum => 442,
                    TileID.Demonite => 130,
                    TileID.Crimtane => 182,
                    TileID.ShimmerBlock => 78,
                    _ => 234
                };

                if (BoulderOre == SpiritRLapisTileID)
                    boulderFrameY = 624;

                Rectangle boulderFrame = new Rectangle(0, boulderFrameY, 52, 50);
                Vector2 boulderOrigin = boulderFrame.Size() / 2f;
                Vector2 boulderDrawPosition = NPC.Center - screenPos + NPC.GfxOffY();
                float boulderRotation = NPC.IsABestiaryIconDummy ? BestiaryBoulderRotation : NPC.rotation;

                if (ScarabAnimation == ScarabAnim.BoulderSuplex && NPC.frame.X / FRAME_WIDTH > 2)
                {
                    int frameY = NPC.frame.Y / FRAME_HEIGHT;
                    Vector2 boulderOffset = frameY switch
                    {
                        1 => new Vector2(2, -2),
                        2 => new Vector2(20, -2),
                        3 => new Vector2(24, -6),
                        4 => new Vector2(22, -6),
                        5 => new Vector2(20, -6),
                        6 => new Vector2(14, -8),
                        _ => Vector2.Zero
                    };

                    float boulderRotationOffset = frameY switch
                    {
                        1 => 0.04f,
                        2 => 0.5f,
                        3 => 0.5f,
                        4 => 0.45f,
                        5 => 0.4f,
                        6 => 0.3f,
                        _ => 0
                    };

                    boulderRotationOffset *= NPC.spriteDirection;
                    boulderOffset.X *= -NPC.spriteDirection;
                    boulderOffset.Y -= 4;
                    boulderDrawPosition += boulderOffset;
                    boulderRotation += boulderRotationOffset;
                }

                if (boulderVisualShake > 0)
                {
                    float shakeMult = ScarabAnimation == ScarabAnim.Stomp ? 1f : 0.5f;

                    if (CurrentAIState == AIState.Stuck && AICounter > 3)
                        shakeMult *= AICounter / (float)STUCK_LENGTH * 1.4f;

                    boulderDrawPosition += Main.rand.NextVector2Circular(boulderVisualShake, boulderVisualShake) * 7f * shakeMult;
                    boulderRotation += Main.rand.NextFloat(-0.2f, 0.2f) * boulderVisualShake * shakeMult;
                }

                if (IsGlimmering)
                {
                    Rectangle shimmerBoulderFrame = boulderFrame;
                    shimmerBoulderFrame.Y += 1092;

                    for (int i = 0; i < 3; i++)
                    {
                        Color disco = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.4f + NPC.whoAmI * 0.3f + i * 0.2f) % 1, 0.6f, 0.35f);
                        Vector2 offset = Vector2.UnitY.RotatedBy(i / 3f * MathHelper.TwoPi + Main.GlobalTimeWrappedHourly) * 3f;
                        Main.spriteBatch.Draw(texture, boulderDrawPosition + offset, shimmerBoulderFrame, NPC.GetAlpha(disco) with { A = 0}, boulderRotation, boulderOrigin, NPC.scale, 0, 0f);
                    }
                }


                if (Main.LocalPlayer.findTreasure)
                {
                    if (drawColor.R < 200)
                        drawColor.R = 200;
                    if (drawColor.G < 170)
                        drawColor.G = 170;
                }

                Main.spriteBatch.Draw(texture, boulderDrawPosition, boulderFrame, NPC.GetAlpha(drawColor), boulderRotation, boulderOrigin, NPC.scale, 0, 0f);
            }

            DrawScarab(tintedColor, ScarabAnimation == ScarabAnim.BoulderSuplex, -screenPos + NPC.GfxOffY());
            return false;
        }

        public void DrawScarab(Color drawColor, bool front, Vector2 drawOffset)
        {
            Texture2D texture = TextureAssets.Npc[Type].Value;
            Vector2 scarabOrigin = new Vector2(21, 44);
            float rotation = 0f;
            //Wobble when rotating the boulder
            if (ScarabAnimation == ScarabAnim.RollBoulder)
                rotation = MathF.Sin(AICounter * 0.2f) * 0.1f;
            bool useLastDrawPosition = false;
            float scarabOpacity = 1f;

            Vector2 scarabDrawPosition = NPC.Center - Vector2.UnitY.RotatedBy(rotation) * 25 + drawOffset;
            Rectangle scarabFrame = NPC.frame;

            int frameY = NPC.frame.Y / FRAME_HEIGHT;
            bool suplexStart = ScarabAnimation == ScarabAnim.BoulderSuplex && NPC.frame.X / FRAME_WIDTH == 2 && frameY <= 1;
            bool suplexEnd = ScarabAnimation == ScarabAnim.BoulderSuplex && !suplexStart;

            if (scarabFadeInTimer > 0 && (ScarabAnimation == ScarabAnim.Idle || ScarabAnimation == ScarabAnim.RollBoulder))
            {
                scarabOpacity = 1 - scarabFadeInTimer;
                scarabDrawPosition += MathF.Pow(scarabFadeInTimer, 2f) * new Vector2(NPC.direction * 8f, -9f);
            }

            if (ScarabAnimation == ScarabAnim.RunAway || suplexEnd || ScarabAnimation == ScarabAnim.ChuckBoulder)
                scarabOrigin = new Vector2(19, 58);

            if (suplexStart && frameY == 1)
            {
                scarabDrawPosition.Y += 10;
                scarabDrawPosition.X -= 10 * NPC.spriteDirection;
            }

            if (suplexEnd)
            {
                scarabDrawPosition = NPC.Bottom + drawOffset;
                scarabDrawPosition.X -= 28 * NPC.spriteDirection;
            }

            if (ScarabAnimation == ScarabAnim.BoulderPopOutRecoil)
            {
                float progress = Utils.GetLerpValue(0, STUCK_RECOVERY_LENGTH, AICounter, true);
                float sineBump = 1 - MathF.Sin(progress * MathHelper.Pi);

                scarabDrawPosition.Y -= 7f;

                if (progress < 0.5f)
                    scarabDrawPosition.Y += sineBump * 28f;
                else
                    scarabDrawPosition.Y += sineBump * 7;
            }

            if (ScarabAnimation == ScarabAnim.ChuckBoulder)
            {
                //Invisible scarab after teleportation
                if (AICounter > 20)
                    return;
                scarabOpacity = 1 - MathF.Pow(AICounter / 20f, 3.8f);

                scarabDrawPosition = lastScarabDrawPosition;
                scarabDrawPosition += MathF.Pow(AICounter / 20f, 0.25f) * new Vector2(NPC.direction * 16f, -14f);
                useLastDrawPosition = true;
            }

            if (ScarabAnimation == ScarabAnim.BoulderKickAnticipation)
            {
                if (AICounter < 8)
                {
                    scarabOpacity = MathF.Pow(AICounter / 8f, 3.8f);
                    scarabDrawPosition += (1 - scarabOpacity) * new Vector2(NPC.direction * 10, 18f);
                }

                scarabOrigin.Y += 14;
            }

            if (ScarabAnimation == ScarabAnim.BoulderKick)
            {
                scarabDrawPosition = lastScarabWorldPosition + drawOffset;
                float pushDown = CurrentAIState == AIState.GroundPoundStart ? MathF.Pow(AICounter / 16f, 0.5f) : 1f;
                scarabDrawPosition.Y += pushDown* 10f;
                scarabOrigin.Y += 12;
            }

            if (ScarabAnimation == ScarabAnim.FallDown)
            {
                scarabDrawPosition = lastScarabWorldPosition + drawOffset;
                scarabOrigin.Y += 12;
            }


            if (BoulderBroken)
            {
                scarabDrawPosition = NPC.Bottom + drawOffset;
                rotation += NPC.velocity.X * 0.03f;
            }
            if (front)
                scarabFrame.X += texture.Width / 2;

            if (!useLastDrawPosition)
                lastScarabDrawPosition = scarabDrawPosition;

            SpriteEffects direction = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (NPC.spriteDirection == 1)
                scarabOrigin.X = scarabFrame.Width - scarabOrigin.X;

            Main.spriteBatch.Draw(texture, scarabDrawPosition, scarabFrame, NPC.GetAlpha(drawColor) * scarabOpacity, rotation, scarabOrigin, NPC.scale, direction, 0f);

            scarabFrame.Y += texture.Height / 3;
            Main.spriteBatch.Draw(texture, scarabDrawPosition, scarabFrame, NPC.GetAlpha(Color.White * 0.6f) * scarabOpacity, rotation, scarabOrigin, NPC.scale, direction, 0f);
        
            if (IsGlimmering)
            {
                scarabFrame.Y += texture.Height / 3;
                Color disco = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.4f + NPC.whoAmI * 0.3f) % 1, 0.7f, 0.75f);
                Main.spriteBatch.Draw(texture, scarabDrawPosition, scarabFrame, NPC.GetAlpha(disco) * scarabOpacity, rotation, scarabOrigin, NPC.scale, direction, 0f);
            }


            else if (BoulderOre == SpiritRLapisTileID)
            {
                scarabFrame.Y += texture.Height / 3;
                Color disco = Main.hslToRgb(0.1f, 0.6f, 0.5f);
                Main.spriteBatch.Draw(texture, scarabDrawPosition, scarabFrame, NPC.GetAlpha(disco) * scarabOpacity, rotation, scarabOrigin, NPC.scale, direction, 0f);
            }
        }
        #endregion
    }

    public class SpelunkerScarabOreChunk : ModProjectile
    {
        public override string Texture => AssetDirectory.UndergroundNPCs + Name;

        public ref float OreType => ref Projectile.ai[0];

        public int SubVariant;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;

            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            SubVariant = Main.rand.Next(3);
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.3f;
            Projectile.velocity.X *= 0.98f;
            Projectile.rotation += 0.05f * Projectile.velocity.X.NonZeroSign();

            // Create sparkle dust
            if (OreType != 0 && Main.rand.NextBool(60))
            {
                Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(12, 12);
                CreateOreSparkles(dustPosition, (int)OreType);
            }
        }

        public override bool CanHitPlayer(Player target) => true;

        public override void OnKill(int timeLeft)
        {
            // Drop an ore item corresponding to the variant
            if (!Projectile.noDropItem)
            {
                GetOreDrops(out int type, out int stack);
                Item.NewItem(Projectile.GetSource_Loot(), Projectile.Center, Projectile.Size, type, stack);
            }

            // Create several ore dusts
            int dustAmount = Main.rand.Next(3, 6);
            for (int i = 0; i < dustAmount; i++)
            {
                Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(12, 12);
                Vector2 dustVelocity = (Projectile.velocity * -Main.rand.NextFloat(0.2f, 0.4f)).RotatedByRandom(0.25f);
                CreateOreDust(dustPosition, dustVelocity, (int)OreType);
            }

            // Tile dust and hit sound
            Collision.HitTiles(Projectile.Center, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
        }

        #region Ore Drops
        public void GetOreDrops(out int type, out int stack)
        {
            stack = Main.rand.Next(2, 6);

            type = OreType switch
            {
                TileID.Iron => ItemID.IronOre,
                TileID.Silver => ItemID.SilverOre,
                TileID.Gold => ItemID.GoldOre,
                TileID.Lead => ItemID.LeadOre,
                TileID.Tungsten => ItemID.TungstenOre,
                TileID.Platinum => ItemID.PlatinumOre,
                TileID.Demonite => ItemID.DemoniteOre,
                TileID.Crimtane => ItemID.CrimtaneOre,
                TileID.ShimmerBlock => ItemID.ShimmerBlock,
                _ => ItemID.StoneBlock
            };

            if (OreType == SpelunkerScarab.SpiritRLapisTileID)
                type = SpelunkerScarab.SpiritRLapisItemID;
        }
        #endregion

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the right frame
            GetOreDrops(out int itemType, out _);
            Main.instance.LoadItem(itemType);
            Texture2D texture = TextureAssets.Item[itemType].Value;

            Rectangle frame = texture.Frame(1, 1, 0, 0);

            Main.EntitySpriteDraw(texture, Projectile.position - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() / 2, Projectile.scale, SpriteEffects.None);
            return false;
        }


        /// <summary>
        /// Creates sparkly ore dust based on specified type. <br/>
        /// The type matches the Ore Chunk projectile variant, so 0 would be stone (nothing), 1 would be iron, ect. <br/>
        /// Note that variant 4 randomizes between demonite and crimtane colors.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="position"></param>
        public static void CreateOreSparkles(Vector2 position, int oreType)
        {
            Color oreColor = oreType switch
            {
                TileID.Iron => new Color(189, 159, 139),
                TileID.Silver => new Color(171, 182, 183),
                TileID.Gold => new Color(231, 213, 65),
                TileID.Lead => new Color(104, 140, 150),
                TileID.Tungsten => new Color(154, 190, 155),
                TileID.Platinum => new Color(181, 194, 217),
                TileID.Demonite => new Color(98, 95, 167),
                TileID.Crimtane => new Color(216, 59, 63),
                TileID.ShimmerBlock => Main.DiscoColor with { A = 0 },
                _ => Color.Transparent
            };

            if (oreType == SpelunkerScarab.SpiritRLapisTileID)
                oreColor = new Color(20, 40, 255);

            Dust dust = Dust.NewDustPerfect(position, DustID.TintableDustLighted, Vector2.Zero, 100, oreColor);
            dust.noGravity = true;
        }

        /// <summary>
        /// Creates ore dust based on specified type. <br/>
        /// The type matches the Ore Chunk projectile variant, so 0 would be stone, 1 would be iron, ect. <br/>
        /// Note that variant 4 randomizes between demonite and crimtane dust.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <param name="type"></param>
        public static void CreateOreDust(Vector2 position, Vector2 velocity, int oreType)
        {
            int oreDust = oreType switch
            {
                TileID.Iron => DustID.Iron,
                TileID.Silver => DustID.Silver,
                TileID.Gold => DustID.Gold,
                TileID.Lead => DustID.Lead,
                TileID.Tungsten => DustID.Tungsten,
                TileID.Platinum => DustID.Platinum,
                TileID.Demonite => DustID.Demonite,
                TileID.Crimtane => DustID.CrimtaneWeapons,
                TileID.ShimmerBlock =>  DustID.ShimmerSpark,
                _ => DustID.Stone
            };

            if (oreType == SpelunkerScarab.SpiritRLapisTileID)
                oreDust = DustID.GemSapphire;

            Dust.NewDustPerfect(position, oreDust, velocity);
        }
    }

    public class SpelunkerScarabGore : Particle
    {
        public override string Texture => AssetDirectory.UndergroundNPCs + "SpelunkerScarabGore";
        public override bool SetLifetime => true;
        public override bool UseCustomDraw => true;

        public SlotId screamSoundSlot;

        public SpelunkerScarabGore(Vector2 position, Vector2 speed)
        {
            Position = position;
            Lifetime = 380;
            Scale = 1f;
            Color = Color.White;
            Velocity = speed;
            Rotation = 0f;
            screamSoundSlot = SoundEngine.PlaySound(SpelunkerScarab.DeathSound, Position);
            SoundHandler.TrackSoundWithFade(screamSoundSlot);
        }

        public override void Update()
        {
            Time++;
            Rotation += Velocity.X.NonZeroSign() * 0.44f;
            Velocity.Y += 0.3f;

            if (screamSoundSlot != SlotId.Invalid && SoundEngine.TryGetActiveSound(screamSoundSlot, out var screamSound))
            {
                screamSound.Position = Position;
                SoundHandler.TrackSoundWithFade(screamSoundSlot);
            }
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            float opacity = Utils.GetLerpValue(0f, 60, TimeLeft, true);
            Color lightColor = Lighting.GetColor(Position.ToTileCoordinates());
            Texture2D texture = ParticleTexture;
            Rectangle frame = new Rectangle(0, 0, texture.Width / 2 - 2, texture.Height);
            spriteBatch.Draw(texture, Position - basePosition, frame, Color.MultiplyRGBA(lightColor) * opacity, Rotation, frame.Size() * 0.5f, Scale, 0, 0);
            frame.X += texture.Width / 2;
            spriteBatch.Draw(texture, Position - basePosition, frame, Color.White * opacity * 0.6f, Rotation, frame.Size() * 0.5f, Scale, 0, 0);
        }
    }   
}