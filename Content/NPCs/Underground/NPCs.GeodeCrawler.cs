using CalamityFables.Content.Items.Food;
using CalamityFables.Particles;
using System.IO;
using System.Xml.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;
using static CalamityFables.Helpers.DropHelper;

namespace CalamityFables.Content.NPCs.GeodeGrawlers
{
    // TODO -- Should this include amber and hallowed crystals variants?
    [ReplacingCalamity("CrawlerAmethyst", "CrawlerDiamond", "CrawlerEmerald", "CrawlerRuby", "CrawlerSapphire", "CrawlerTopaz")]
    public class GeodeCrawler : ModNPC
    {
        #region Setup and variables
        public enum FrameState
        {
            // Frame 0.
            Idle,

            // Frame 1.
            Falling,

            // Frame 2.
            Resting,

            // Frames 3-5.
            Surprised,

            // Frames 6-10.
            Walking,

            // Frames 11-15.
            Digging
        }

        public enum AIState
        {
            Rest,
            WalkAround,
            EatTreeRoots,

            SurpriseAnimation,
            RunAwayFromPlayer,
            DigAwayFromPlayer,
            BeStuck
        }

        public enum CrystalType
        {
            Amethyst = ItemID.Amethyst,
            Topaz = ItemID.Topaz,

            Sapphire = ItemID.Sapphire,
            Emerald = ItemID.Emerald,

            Ruby = ItemID.Ruby,
            Diamond = ItemID.Diamond
        }

        public List<KnockoutStarParticle> KnockoutStars {
            get;
            set;
        } = new();

        public int AITimer {
            get;
            set;
        }

        public int maxCrystalDrops;
        public int variantID;

        /// <summary>
        /// Actually also used as a minimum Y depth for caverns when it digs :nerd:
        /// </summary>
        public float walkDestinationX;
        public float feedingTreeX = -1;
        public float feedingTreeY = -1;

        public int maxTileDepthWhenDigging;
        public bool hasHitGroundWhileStuck;
        public bool walkingTowardsTree;
        public bool hasPlayedDigSound;

        //Keeps track of how long weve been stuck
        public float unafraidOfHeightsTimer;

        public AIState CurrentState {
            get => (AIState)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public FrameState CurrentFrame {
            get => (FrameState)NPC.localAI[0];
            set => NPC.localAI[0] = (int)value;
        }

        public CrystalType[] CrystalTypes {
            get => new CrystalType[3] { (CrystalType)NPC.ai[1], (CrystalType)NPC.ai[2], (CrystalType)NPC.ai[3] };
            set {
                NPC.ai[1] = (int)value[0];
                NPC.ai[2] = (int)value[1];
                NPC.ai[3] = (int)value[2];
            }
        }

        public bool OnTopOfTiles => FablesUtils.SolidCollisionFix(NPC.TopLeft, NPC.width, NPC.height + 8, true);

        public Color AverageColor {
            get {
                Vector3 colorVector = Vector3.Zero;
                for (int i = 0; i < CrystalTypes.Length; i++)
                {
                    if (CrystalColorTable.TryGetValue(CrystalTypes[i], out Color c))
                        colorVector += c.ToVector3() * (i == 0 ? 6f : 1f);
                }
                colorVector = Vector3.Clamp(colorVector / (CrystalTypes.Length + 5f), Vector3.Zero, Vector3.One);
                return new(colorVector);
            }
        }

        public ref float FrameIndex => ref NPC.localAI[1];

        public ref float ShockedEyesOpacity => ref NPC.localAI[2];

        public Player Target => Main.player[NPC.target];

        public string VariantName => (int)variantID switch
        {
            0 => "Rhino",
            1 => "Weevil",
            _ => "Hercules",
        };

        public static int BannerType {
            get;
            private set;
        }

        public static AutoloadedBanner BannerTile {
            get;
            private set;
        }

        // This aesthetic is slightly controversial, so I'll leave the code around in case anyone wants to enable it again.
        public static bool LoseCrystalsWhenStuck => false;

        #region Sounds
        public static readonly SoundStyle AmbientSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerAmbient", 2) { Volume = 0.8f };

        // HAHAHA!! That Geode Crawler got hit in the head with a coconut!
        public static readonly SoundStyle BonkSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerBonk") { Volume = 0.8f };

        public static readonly SoundStyle DeathSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerDeath") { Volume = 0.8f };

        public static readonly SoundStyle DigSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerDig") { Volume = 0.8f };

        public static readonly SoundStyle GnawingSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerGnaw", 3) { Volume = 0.67f };

        public static readonly SoundStyle HitSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerHit") { Volume = 0.8f };

        public static readonly SoundStyle JumpSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerJump") { Volume = 0.6f };

        public static readonly SoundStyle StartledSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerStartled") { Volume = 0.9f };
        #endregion

        public static readonly Dictionary<CrystalType, Color> CrystalColorTable = new()
        {
            [CrystalType.Amethyst] = new(255, 93, 255),
            [CrystalType.Topaz] = new(239, 191, 0),

            [CrystalType.Sapphire] = new(0, 154, 255),
            [CrystalType.Emerald] = new(115, 255, 86),

            [CrystalType.Ruby] = new(255, 0, 45),
            [CrystalType.Diamond] = new(255, 235, 250),
        };

        public static readonly Dictionary<CrystalType, Color> CrystalHighlightColorTable = new()
        {
            [CrystalType.Amethyst] = new(238, 169, 255),
            [CrystalType.Topaz] = new(255, 239, 174),

            [CrystalType.Sapphire] = new(180, 226, 255),
            [CrystalType.Emerald] = new(170, 255, 93),

            [CrystalType.Ruby] = new(255, 132, 120),
            [CrystalType.Diamond] = new(255, 255, 255),
        };

        public float CrystalRatio(CrystalType crystal) => (float)CrystalTypes.Average(c => (c == crystal).ToInt());

        public bool IsGlimmering {
            get => NPC.ai[1] == ItemID.RainbowMoss;
            set => NPC.ai[1] = value ? ItemID.RainbowMoss : (int)Main.rand.NextFromList(CrystalType.Amethyst, CrystalType.Topaz, CrystalType.Sapphire, CrystalType.Emerald, CrystalType.Ruby, CrystalType.Diamond);
        }

        public const int MinUnscaledCrystalQuantity = 6;

        public const int MaxUnscaledCrystalQuantity = 9;

        public override string Texture => AssetDirectory.UndergroundNPCs + Name + VariantName;

        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Geode Crawler", AssetDirectory.Banners, out AutoloadedBanner banner);
            BannerTile = banner;
            FablesGeneralSystemHooks.CustomShimmerEffects += SparkleOn;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Geode Crawler");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new(0)
            {
                SpriteDirection = 1
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;

            NPCID.Sets.TrailCacheLength[Type] = 6;
            NPCID.Sets.TrailingMode[Type] = 0;
            Main.npcFrameCount[Type] = 16;
            BannerTile.NPCType = Type;
        }

        public override void SetDefaults()
        {
            AIType = -1;
            NPC.aiStyle = -1;
            NPC.damage = 14;
            NPC.width = 54;
            NPC.height = 36;
            NPC.defense = 15;
            NPC.lifeMax = 42;
            NPC.knockBackResist = 0.04f;
            NPC.value = Item.buyPrice(0, 0, 4, 20);

            NPC.HitSound = HitSound with
            {
                SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
                Volume = 0.65f
            };
            NPC.DeathSound = DeathSound with
            {
                Volume = 0.5f
            };

            NPC.behindTiles = true;
            NPC.lavaImmune = true;

            // Make the crawlers noticeable by the lifeform analyzer.
            NPC.rarity = 3;

            Banner = NPC.type;
            BannerItem = BannerType;
        }

        public override void OnSpawn(IEntitySource source)
        {
            maxCrystalDrops = Main.rand.Next(5, 11);

            // Use a random assortment of gems at first.
            for (int i = 0; i < 3; i++)
                NPC.ai[i + 1] = (int)Main.rand.NextFromList(CrystalType.Amethyst, CrystalType.Topaz, CrystalType.Sapphire, CrystalType.Emerald, CrystalType.Ruby, CrystalType.Diamond);

            // Initialize crystal variants based on the nearest gem tree type.
            // If no such tree exists, the crawler gets a random assortment of crystals.
            for (int tries = 0; tries <= 5000; tries++)
            {
                int dx = Main.rand.Next(-40, 40);
                int dy = Main.rand.Next(-40, 40);
                Tile t = Framing.GetTileSafely((int)(NPC.Center.X / 16f + dx), (int)(NPC.Center.Y / 16f + dy));
                if (!TileID.Sets.CountsAsGemTree[t.TileType] || !t.HasTile)
                    continue;

                CrystalType crystalType = t.TileType switch
                {
                    TileID.TreeTopaz => CrystalType.Topaz,
                    TileID.TreeSapphire => CrystalType.Sapphire,
                    TileID.TreeEmerald => CrystalType.Emerald,
                    TileID.TreeRuby => CrystalType.Ruby,
                    TileID.TreeDiamond => CrystalType.Diamond,
                    _ => CrystalType.Amethyst,
                };

                for (int i = 0; i < CrystalTypes.Length; i++)
                    NPC.ai[i + 1] = (int)crystalType;

                // Look at the tree.
                NPC.spriteDirection = (dx >= 0f).ToDirectionInt();

                // Sometimes replace one of the crystals with something else, for a bit of visual variety.
                if (Main.rand.NextBool(5))
                    NPC.ai[Main.rand.Next(1, 4)] = (int)Main.rand.NextFromList(CrystalType.Amethyst, CrystalType.Topaz, CrystalType.Sapphire, CrystalType.Emerald, CrystalType.Ruby, CrystalType.Diamond);
                break;
            }

            // Use a random variant.
            variantID = Main.rand.Next(3);
            NPC.netUpdate = true;

            // Create knockout star particles. These will be drawn if it gets stuck.
            Color primColor = Color.Lerp(Color.Gold, Color.White, 0.2f);
            Color primOutlineColor = Color.DarkGoldenrod;
            primOutlineColor.A = 255;

            for (int i = 0; i < 2; i++)
                KnockoutStars.Add(new(NPC.Center, Color.White, primColor, primOutlineColor, 1f, MathHelper.Pi / 13f, 0.3f));
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,

                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.GeodeCrawler")
            });
        }
        #endregion

        #region Syncing
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(AITimer);
            writer.Write(variantID);
            writer.Write(maxTileDepthWhenDigging);
            writer.Write(walkDestinationX);
            writer.Write((byte)hasHitGroundWhileStuck.ToInt());
            writer.Write((byte)walkingTowardsTree.ToInt());
            writer.Write(feedingTreeX);
            writer.Write(feedingTreeY);
            writer.Write(unafraidOfHeightsTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            AITimer = reader.ReadInt32();
            variantID = reader.ReadInt32();
            maxTileDepthWhenDigging = reader.ReadInt32();
            walkDestinationX = reader.ReadSingle();
            hasHitGroundWhileStuck = reader.ReadByte() != 0;
            walkingTowardsTree = reader.ReadByte() != 0;
            feedingTreeX = reader.ReadSingle();
            feedingTreeY = reader.ReadSingle();
            unafraidOfHeightsTimer = reader.ReadSingle();
        }
        #endregion

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            // NO MORE DEFENSE FOR YOU!
            // YOU ARE DEFENSELESS, I TELL YOU! DEFENSELESS!!!

            if (CurrentState == AIState.BeStuck)
                modifiers.Defense *= 0;
        }

        private void SparkleOn(NPC npc)
        {
            if (npc.type != Type || npc.ModNPC is not GeodeCrawler crawler)
                return;

            //Become the glimmerald
            crawler.IsGlimmering = true;

            npc.shimmering = false;
            npc.buffImmune[BuffID.Shimmer] = true;
            npc.GravityIgnoresLiquid = true;
            npc.GivenName = Main.rand.Next(new string[] { "Gemerald", "Glimmerald", "Sparklerald", "Glisterald", "Gleamond", "Radianterald", "Glintbowl", "Lusterald", });

            if (Main.netMode == NetmodeID.SinglePlayer)
                Item.ShimmerEffect(npc.Center);
            else
            {
                NetMessage.SendData(MessageID.ShimmerActions, -1, -1, null, 0, (int)npc.Center.X, (int)npc.Center.Y);
                //This syncs the npc's new given name and all that
                new GeodeCrawlerShimmerPacket(npc).Send();
            }
            NPC.netUpdate = true;
        }

        [Serializable]
        public class GeodeCrawlerShimmerPacket : RenameNPCPacket
        {
            public GeodeCrawlerShimmerPacket(NPC npc) : base(npc) { }

            protected override bool PreSend(int toClient = -1, int ignoreClient = -1)
            {
                if (Main.npc[npc].type != ModContent.NPCType<GeodeCrawler>())
                    return false;
                return base.PreSend(toClient, ignoreClient);
            }

            protected override void Receive()
            {
                NPC crawler = Main.npc[npc];
                crawler.shimmering = false;
                crawler.GravityIgnoresLiquid = true;
                crawler.buffImmune[BuffID.Shimmer] = true;
                if (crawler.type == ModContent.NPCType<GeodeCrawler>())
                    (crawler.ModNPC as GeodeCrawler).IsGlimmering = true;
                base.Receive();
            }
        }

        public int previousTarget;

        public override void AI()
        {
            //In multiplayer, don't retarget while digging down. 
            if (CurrentState != AIState.DigAwayFromPlayer || Main.netMode == NetmodeID.SinglePlayer)
                NPC.TargetClosest();

            // Reset things every frame. They may be altered in the behavior methods below.
            //Don't reset donttakedamage when digging away though
            if (CurrentState != AIState.DigAwayFromPlayer)
                NPC.dontTakeDamage = false;

            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.knockBackResist = 0.04f;
            ShockedEyesOpacity = MathHelper.Clamp(ShockedEyesOpacity - 0.038f, 0f, 1f);

            //Shimmer version can't be shimmering
            if (IsGlimmering)
                NPC.shimmering = false;

            switch (CurrentState)
            {
                case AIState.Rest:
                    DoBehavior_Rest();
                    break;
                case AIState.WalkAround:
                    DoBehavior_WalkAround();
                    break;
                case AIState.EatTreeRoots:
                    DoBehavior_EatTreeRoots();
                    break;
                case AIState.SurpriseAnimation:
                    DoBehavior_SurpriseAnimation();
                    break;
                case AIState.RunAwayFromPlayer:
                    DoBehavior_RunAwayFromPlayer();
                    break;
                case AIState.DigAwayFromPlayer:
                    DoBehavior_DigAwayFromPlayer();
                    break;
                case AIState.BeStuck:
                    DoBehavior_BeStuck();
                    break;
            }

            if (NPC.justHit && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int crystalCount = Main.rand.Next(1, 4);
                while (maxCrystalDrops > 0 && crystalCount > 0)
                {
                    DropRandomCrystal(NPC.GetSource_FromAI());
                    maxCrystalDrops--;
                    crystalCount--;
                }
            }

            // Randomly emit sparkles off the crystals.
            if (Main.rand.NextBool(12) && !Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height) && NPC.WithinRange(Target.Center, 400f) && AverageColor.ToVector3().Length() >= 0.1f)
            {
                Color dustColor = Color.LightYellow;
                Vector2 sparkleSpawnPosition = NPC.Center + new Vector2(-NPC.spriteDirection * 12f, -5f).RotatedBy(NPC.rotation) + Main.rand.NextVector2Circular(7f, 7f);
                Dust sparkle = Dust.NewDustPerfect(sparkleSpawnPosition, 267, -Vector2.UnitY.RotatedByRandom(0.51f) * Main.rand.NextFloat(2f), 0, dustColor);
                sparkle.scale = 0.3f;
                sparkle.fadeIn = Main.rand.NextFloat(1.2f);
                sparkle.noLightEmittence = true;
                sparkle.noGravity = true;
            }

            //Glow
            if ((CurrentState != AIState.DigAwayFromPlayer || !Collision.SolidCollision(NPC.Center, 1, 1)))
            {
                Color glowColor = IsGlimmering ? Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.4f) % 1, 0.7f, 0.75f) : CrystalColorTable.GetValueOrDefault(CrystalTypes[0], Color.Gold) * 0.2f;
                Lighting.AddLight(NPC.Center, glowColor.ToVector3());
            }

            previousTarget = NPC.target;
            AITimer++;
        }

        #region Behaviors
        public void PerformSurpriseCheck()
        {
            // Get scared if hit or if the player is in the line of sight of the crawler.
            bool playerInLineOfSight = !Target.invis && Collision.CanHit(NPC, Target) && NPC.WithinRange(Target.Center, 192f) && NPC.spriteDirection == Math.Sign(Target.Center.X - NPC.Center.X);
            if (NPC.justHit || playerInLineOfSight)
            {
                // SCREM.
                SoundEngine.PlaySound(StartledSound, NPC.Center);

                NPC.gfxOffY = 0;
                NPC.rotation = 0;
                CurrentState = AIState.SurpriseAnimation;

                if (OnTopOfTiles)
                    NPC.velocity = Vector2.UnitY * -6f;
                NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
                PrepareForDifferentState();

                // Create a surprise exclaimation points.
                for (int i = 0; i < 2; i++)
                {
                    float surpriseRotation = NPC.spriteDirection * (0.1f + i * 0.15f);
                    Vector2 particleDirection = -Vector2.UnitY.RotatedBy(surpriseRotation);
                    Vector2 surpriseSpawnPosition = NPC.Bottom + Vector2.UnitX * (NPC.spriteDirection * 25f) + particleDirection * 25f;
                    surpriseSpawnPosition.Y += 10f + i * 3f;

                    ExclamationMarkParticle surprise = new(surpriseSpawnPosition, particleDirection * (7f - 1f * i), 1.2f - 0.2f * i, surpriseRotation);
                    ParticleHandler.SpawnParticle(surprise);
                }
            }
        }

        public void DoBehavior_Rest()
        {
            // Disable natural knockback resistance. Hit knockback effects will be applied manually.
            NPC.knockBackResist = 0f;

            // Use resting frames.
            CurrentFrame = FrameState.Resting;

            // Sit in place.
            NPC.velocity.X *= 0.91f;

            // Wait a bit before moving around.
            if (AITimer >= 60f && Main.rand.NextBool(20))
            {
                CurrentState = AIState.WalkAround;
                PrepareForDifferentState();
                return;
            }

            PerformSurpriseCheck();
        }

        public void DoBehavior_WalkAround()
        {
            // Approach the destination.
            GroundMotion(new Vector2(walkDestinationX, NPC.Center.Y), 3.5f, 0.06f, 4, AITimer >= 10, NPC.width / 2f + 4f, out bool tallObstacleAhead);

            bool reachedGoalX = MathHelper.Distance(NPC.Center.X, walkDestinationX) <= 10f;

            float maxPatience = walkingTowardsTree ? 600f : 300f;

            // Decide a new walk destination if there's a tall obstacle ahead or if on the first frame.
            if ((tallObstacleAhead && OnTopOfTiles) || AITimer <= 1f || AITimer >= maxPatience || reachedGoalX)
            {
                bool reachedTree = walkingTowardsTree && reachedGoalX && (NPC.Center.Y < feedingTreeY + 16 && NPC.Center.Y > feedingTreeY - 96);

                //If we couldnt find a place to go , be less afraid again
                if (AITimer == 300f)
                    unafraidOfHeightsTimer++;

                if (reachedGoalX)
                    unafraidOfHeightsTimer--;

                if (AITimer > 1)
                {
                    //if we reached the tree, or at a random chance if we came at a stop (or spent too long walking)
                    if (reachedTree || Main.rand.NextBool())
                    {
                        CurrentState = reachedTree ? AIState.EatTreeRoots : AIState.Rest;

                        if (!reachedTree)
                            unafraidOfHeightsTimer++;

                        if (reachedTree)
                        {
                            NPC.spriteDirection = (feedingTreeX - NPC.Center.X).NonZeroSign();
                            //Flip around if we were backpedalling away from tree
                            if (Math.Sign(NPC.velocity.X) != NPC.spriteDirection)
                                NPC.velocity *= -0.6f;

                            NPC.velocity.X = Math.Clamp(NPC.velocity.X, -3f, 3f);
                        }

                        PrepareForDifferentState();
                    }
                }

                //Find a new target
                FindMovementTarget();
                NPC.netUpdate = true;
            }

            //Dont fall off, unless you're falling towards the tree
            else if (!FablesUtils.SolidCollisionFix(NPC.BottomLeft + Vector2.UnitX * NPC.width * NPC.spriteDirection, NPC.width, 70, true))
            {
                //If afraid of falls
                if (unafraidOfHeightsTimer < 4)
                {
                    //flip around if not trying to reach a tree
                    if (!walkingTowardsTree)
                        walkDestinationX = NPC.Center.X - NPC.spriteDirection * 50;
                }

                //Jump ahead, unafraid
                else if (OnTopOfTiles)
                {
                    if (NPC.velocity.Y >= 0f)
                        SoundEngine.PlaySound(JumpSound, NPC.Center);

                    NPC.velocity.X = NPC.spriteDirection * Main.rand.NextFloat(3f, 5f);
                    NPC.velocity.Y = -8f;
                    NPC.noTileCollide = true;
                    NPC.netUpdate = true;
                }

                NPC.netUpdate = true;
            }


            // Randomly make ambient sounds.
            if (NPC.soundDelay <= 0 && Main.rand.NextBool(60))
            {
                SoundEngine.PlaySound(AmbientSound, NPC.Center);
                NPC.soundDelay = 270;
            }

            PerformSurpriseCheck();
        }

        public void FindMovementTarget()
        {
            walkingTowardsTree = false;
            FindFeedingTree(1f, 0.7f, 42f, 1.2f);

            //If we found a tree
            if (walkingTowardsTree)
            {
                //Dust.QuickDust(new Point((int)(feedingTreeX / 16), (int)(feedingTreeY / 16)), Color.Red);
            }
            else
            {
                // IF we didn't find a tree, pick a random spot
                walkDestinationX = NPC.Center.X + Main.rand.NextFloat(240f, 580f) * Main.rand.NextFromList(-1f, 1f);
                //Dust.QuickDust(new Point((int)(walkDestinationX / 16), (int)(NPC.Center.Y / 16)), Color.Blue);
            }
        }

        public void FindFeedingTree(float heightDifferenceImportance = 1f, float horizontalDistanceImportance = 1f, float tooCloseDistance = 42f, float instantPickScore = 1.9f)
        {
            float bestTreeScore = float.NegativeInfinity;
            bool alreadyFoundTreeBefore = feedingTreeX != -1;

            bool foundATreeNearby = false;
            bool pickedATree = false;


            for (int tries = 0; tries <= 5000; tries++)
            {
                int dx = Main.rand.Next(-40, 40);
                int dy = Main.rand.Next(-40, 40);
                if (Math.Abs(dx) < 2)
                    dx = 2;

                Tile t = Framing.GetTileSafely((int)(NPC.Center.X / 16f + dx), (int)(NPC.Center.Y / 16f + dy));
                if (!TileID.Sets.CountsAsGemTree[t.TileType] || !t.HasTile)
                    continue;

                //Branches. Avoid them
                if (!t.IsTreeTrunk(true))
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        t = Framing.GetTileSafely((int)(NPC.Center.X / 16f + dx) + i, (int)(NPC.Center.Y / 16f + dy));
                        if (t.IsTreeTrunk(true) && t.HasTile && TileID.Sets.CountsAsGemTree[t.TileType])
                        {
                            dx += i;
                            break;
                        }
                    }
                }

                foundATreeNearby = true;


                //Check if the tree is valid
                Point ground = NPC.Center.ToTileCoordinates();
                int maxIterations = 6;
                ground = AStarPathfinding.OffsetUntilNavigable(ground, new Point(0, 1), GeodeCrawlerPathfind, ref maxIterations);
                if (maxIterations < 0)
                    continue;

                //Try to find navigable ground below the tree
                maxIterations = 34;
                Point pathfindingEnd = AStarPathfinding.OffsetUntilNavigable(new Point((int)(NPC.Center.X / 16f + dx), (int)(NPC.Center.Y / 16f + dy)), new Point(0, 1), GeodeCrawlerPathfind, ref maxIterations);

                //If theres no floor under the tree (((SOMEHOW))) then skip
                if (maxIterations < 0)
                    continue;


                //If we managed to find a good starting point and a good ending point, we then proceed to simulate pathfinding between the two.
                //If we can find a path to the target whose lenght is shorter than a straight line from start to end + some varying leeway
                //Then we know there exists an "easy straightforward path", but if it differs too much, we simply just wallcrawl
                if (!AStarPathfinding.IsThereAPath(ground, pathfindingEnd, GeodeCrawlerStride, GeodeCrawlerPathfind, 200f))
                    continue;

                //Rate the tree
                float treeScore = (40 - Math.Abs(dy)) / 40f * heightDifferenceImportance + (40 - Math.Abs(dx)) / 40f * horizontalDistanceImportance;
                if (alreadyFoundTreeBefore)
                    treeScore *= 0.1f + 0.9f * Utils.GetLerpValue(0f, tooCloseDistance, Math.Abs((NPC.Center.X + dx * 16f) - feedingTreeX), true);

                if (treeScore > bestTreeScore)
                    bestTreeScore = treeScore;
                else
                    continue;

                pickedATree = true;

                walkDestinationX = NPC.Center.X + dx * 16f;
                feedingTreeX = walkDestinationX;
                feedingTreeY = NPC.Center.Y + dy * 16f;

                //Scroll down the tree
                while (feedingTreeY < Main.maxTilesY * 16f - 16)
                {
                    t = Framing.GetTileSafely((int)(feedingTreeX / 16), (int)(feedingTreeY / 16));
                    if (!TileID.Sets.CountsAsGemTree[t.TileType])
                        break;
                    feedingTreeY += 16;
                }

                //OFfset the target so we aim in front of the tree (or, rarely ish, behind the tree...
                walkDestinationX -= 44f * FablesUtils.NonZeroSign(dx) * (Main.rand.NextBool(3) ? -1 : 1);

                //Dust.QuickDustLine(new Vector2(walkDestinationX, NPC.Center.Y - 50), new Vector2(walkDestinationX, NPC.Center.Y + 50), 30, Color.Red);
                walkingTowardsTree = true;
                if (treeScore > instantPickScore)
                    return;
            }

            //if theres a tree nearby but you cant reach it, grow a little less afraid of jumping
            if (foundATreeNearby && !pickedATree)
                unafraidOfHeightsTimer++;
        }


        public static List<AStarNeighbour> GeodeCrawlerStride = AStarNeighbour.BigStride(3);

        public static bool GeodeCrawlerPathfind(Point p, Point? from, out bool universallyUnnavigable)
        {
            universallyUnnavigable = true;
            Tile t = Main.tile[p];
            bool solidTile = Main.tileSolid[t.TileType];
            bool platform = TileID.Sets.Platforms[t.TileType];

            //Can't navigate inside solid tiles
            if (t.HasUnactuatedTile && !t.IsHalfBlock && !platform && solidTile)
                return false;

            //Can navigate on half tiles and platforms just fine
            if (t.HasUnactuatedTile && (t.IsHalfBlock || platform) && solidTile)
                return true;

            for (int i = -1; i <= 1; i++)
                for (int j = 0; j <= 1; j++)
                {
                    //Only cardinal directions here
                    if (j * i != 0 || (j == 0 && i == 0))
                        continue;

                    //IF a neighboring tile is solid we can go on it
                    Tile adjacentTile = Main.tile[p.X + i, p.Y + j];
                    if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                        return true;
                }

            //Can fall straight down just fine
            if (from != null && p.X == from.Value.X && p.Y == from.Value.Y + 1)
                return true;

            return false;
        }

        public void GroundMotion(Vector2 goal, float speed, float acceleration, int tallObstacleCheckHeight, bool canJump, float tallObstacleHorizontalDistance, out bool tallObstacleAhead)
        {
            bool shortObstacleAhead = !Collision.CanHitLine(NPC.Center, 1, 1, NPC.Center + Vector2.UnitX * NPC.spriteDirection * 60f, 1, 1) || NPC.velocity.X == 0f;
            tallObstacleAhead = shortObstacleAhead;
            for (int dy = 0; dy < tallObstacleCheckHeight; dy++)
            {
                tallObstacleAhead &= !Collision.CanHitLine(NPC.Center - Vector2.UnitY * dy * 16f, 1, 1, NPC.Center + new Vector2(NPC.spriteDirection * tallObstacleHorizontalDistance, -dy * 16f), 1, 1);
                //Dust.QuickDustLine(NPC.Center - Vector2.UnitY * dy * 16f, NPC.Center + new Vector2(NPC.spriteDirection * tallObstacleHorizontalDistance, -dy * 16f), 20, Color.Red);
            }

            // Approach the destination.
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.SafeDirectionTo(goal).X * speed, acceleration);

            // Make a cute hop if there's a short obstacle ahead.
            if (shortObstacleAhead && !tallObstacleAhead && OnTopOfTiles && canJump)
            {
                if (NPC.velocity.Y >= 0f)
                    SoundEngine.PlaySound(JumpSound, NPC.Center);

                NPC.velocity.X = NPC.spriteDirection * 3f;
                NPC.velocity.Y = -6f;
                NPC.noTileCollide = true;
                NPC.netUpdate = true;
            }

            // Look in the direction of movement.
            if (NPC.velocity.X != 0f)
                NPC.spriteDirection = NPC.velocity.X.NonZeroSign();

            // Use walking frames if walking. Otherwise, use falling frames.
            CurrentFrame = FrameState.Walking;
            if (!OnTopOfTiles)
                CurrentFrame = FrameState.Falling;
        }

        public int treeEatTimer = 0;
        public void DoBehavior_EatTreeRoots()
        {
            // Slow down.
            float smoothSlowdown = 1 - Utils.GetLerpValue(0, 15, AITimer, true) * 0.25f;
            NPC.velocity.X *= smoothSlowdown;

            //Don't overshoot the tree!
            if (Math.Abs(NPC.Center.X - feedingTreeX) < 30f)
                NPC.velocity.X *= 0.95f;

            if (AITimer <= 1f)
                treeEatTimer = 0;

            //Go back to preffering to not jump off of ledges if you have  a tree
            unafraidOfHeightsTimer = 0;

            if (OnTopOfTiles)
            {
                // Use digging frames.
                CurrentFrame = FrameState.Digging;

                float effectStrenght = Utils.GetLerpValue(0f, 15f, treeEatTimer, true);

                //Schnoze around in the ground
                NPC.rotation = -(NPC.spriteDirection * 0.3f + 0.1f * (float)Math.Pow(Math.Sin(AITimer * 0.1f), 2f)) * effectStrenght;

                // Play eating and digging sounds on the first frame it touches the ground
                if (treeEatTimer == 0)
                {
                    SoundEngine.PlaySound(GnawingSound, NPC.Center);
                    SoundEngine.PlaySound(DigSound with
                    {
                        Pitch = 0.2f,
                        Volume = 0.3f
                    }, NPC.Center);
                }

                if (Main.rand.NextBool())
                {
                    Point bottom = new Point((int)(NPC.Bottom.X / 16f) + NPC.spriteDirection, (int)(NPC.Bottom.Y / 16f) - 1);
                    Tile t = Main.tile[bottom];
                    int i = 0;
                    while (i < 3 && (!t.HasTile || !TileID.Sets.CountsAsGemTree[t.TileType]))
                    {
                        bottom.X += NPC.spriteDirection;
                        t = Main.tile[bottom];
                        i++;
                    }

                    if (i < 3)
                    {
                        int dustIndex = WorldGen.KillTile_MakeTileDust(bottom.X, bottom.Y, Framing.GetTileSafely(bottom));

                        Dust dust = Main.dust[dustIndex];
                        dust.position.X += Main.rand.NextFloat(-8f, 8f);
                        dust.velocity.X *= 0.5f;
                        dust.velocity.Y = -Main.rand.NextFloat(1f, 3f);
                        if (Main.rand.NextBool(3))
                            dust.velocity = dust.velocity.RotatedBy(MathHelper.PiOver2);

                        dust.noGravity = true;
                        dust.noLightEmittence = true;
                    }
                }

                //Timer to keep track of how long its been ACTUALLY eating the roots
                treeEatTimer++;
            }
            else
                CurrentFrame = FrameState.Falling;


            //Only transition off if the player is close enough
            if (AITimer >= 280f && NPC.WithinRange(Target.Center, 550))
            {
                NPC.gfxOffY = 0;
                NPC.rotation = 0;
                CurrentState = AIState.Rest;
                PrepareForDifferentState();
            }

            PerformSurpriseCheck();
        }

        public void DoBehavior_SurpriseAnimation()
        {
            // Use running frames, as a cute mid-air "get away!" animation.
            CurrentFrame = FrameState.Walking;

            // Use shocked eyes.
            ShockedEyesOpacity = Utils.GetLerpValue(0f, 8f, AITimer, true);

            // Begin running if on ground again.
            if (OnTopOfTiles && AITimer >= 10f)
            {
                CurrentState = AIState.RunAwayFromPlayer;
                PrepareForDifferentState();
            }
        }

        public void DoBehavior_RunAwayFromPlayer()
        {
            //Flee!
            GroundMotion(Target.Center, -5f, 0.05f, 5, true, 50f, out bool tallObstacleAhead);

            // Prepare to dig away if there's a tall obstacle ahead.
            //In multiplayer, also dig if cornered between players
            if (tallObstacleAhead ||
                (Main.netMode != NetmodeID.SinglePlayer && 
                (NPC.target != previousTarget && (NPC.Center.X - Target.Center.X).NonZeroSign() != (NPC.Center.X - Main.player[previousTarget].Center.X).NonZeroSign())))
            {
                NPC.velocity.X *= 0.7f;
                CurrentState = AIState.DigAwayFromPlayer;
                PrepareForDifferentState();
            }

            //Stop scurrying away if far away enough
            if (AITimer > 60 && !NPC.WithinRange(Target.Center, 900f))
            {
                NPC.velocity.X *= 0.9f;
                CurrentState = AIState.WalkAround;
                PrepareForDifferentState();
            }
        }

        public void DoBehavior_DigAwayFromPlayer()
        {
            // Horizontally decelerate.
            NPC.velocity.X *= 0.96f;

            // Disable damage. This also serves as a means of removing the awkward health bar as the crawler digs.
            if (!NPC.dontTakeDamage)
                NPC.dontTakeDamage = FablesUtils.FullSolidCollision(NPC.position, NPC.width, NPC.height);

            int digDelay = 90;
            // Randomly emit ground particles if in tiles.
            if (AITimer >= digDelay - 15 && Collision.SolidCollision(NPC.Center, 1, 1))
            {
                if (!hasPlayedDigSound)
                {
                    SoundEngine.PlaySound(DigSound with
                    {
                        SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                    }, NPC.Center);
                    hasPlayedDigSound = true;

                    for (int i = 0; i < 9; i++)
                        CreateGroundHitDust();
                }
            }

            // Wait a moment before digging away.
            if (AITimer <= digDelay)
            {
                // Make a nervous sound at first.
                if (AITimer == 16f)
                    SoundEngine.PlaySound(AmbientSound with
                    {
                        Pitch = -0.15f
                    }, NPC.Center);

                // Look at the player at first and jitter nervously, backing away slowly.
                float backAwaySpeedInterpolant = Utils.GetLerpValue(0f, digDelay - 20f, AITimer, true);
                float backAwaySpeed = MathHelper.Lerp(0.02f, 1.1f, MathF.Pow(backAwaySpeedInterpolant, 2.81f));
                if (AITimer <= digDelay - 18f)
                {
                    NPC.spriteDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();
                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, -NPC.SafeDirectionTo(Target.Center).X * backAwaySpeed, 0.13f);

                    if (OnTopOfTiles)
                        NPC.position.X += Main.rand.NextFloat(-0.27f, 0.27f);
                }

                // Jump away from the player and begin digging.
                if (AITimer == digDelay - 15f)
                {
                    walkDestinationX = NPC.position.Y;
                    SoundEngine.PlaySound(JumpSound, NPC.Center);
                    if (OnTopOfTiles)
                    {
                        NPC.velocity.X = NPC.SafeDirectionTo(Target.Center).X * -6f;
                        NPC.velocity.Y = -5f;
                    }
                    NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
                    NPC.netUpdate = true;
                }

                if (AITimer >= digDelay - 15f)
                    NPC.noTileCollide = true;

                // Use walking frames if moving fast enough. Otherwise use idle frames.
                CurrentFrame = NPC.velocity.Length() >= 0.64f ? FrameState.Walking : FrameState.Resting;
                return;
            }

            // Dig into the ground.
            NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y + 0.06f, -7f, 4.2f);

            // Disable gravity and tile collision, to allow the crawler to enter the ground and disappear.
            NPC.noGravity = OnTopOfTiles;
            NPC.noTileCollide = true;

            // Use ground digging frames.
            CurrentFrame = FrameState.Digging;

            // Look down slightly.
            float idealRotation = MathHelper.Clamp(NPC.spriteDirection * NPC.velocity.Y * 0.32f, -MathHelper.PiOver2, MathHelper.PiOver2);
            NPC.rotation = NPC.rotation.AngleLerp(idealRotation, 0.036f);

            // Disappear if sufficiently deep.
            int tileDepth = 12;
            for (int i = 0; i < 12; i++)
            {
                Point checkPoint = NPC.Center.ToTileCoordinates();
                checkPoint.Y -= i;
                Tile t = Framing.GetTileSafely(checkPoint);
                if (!t.HasUnactuatedTile || !Main.tileSolid[t.TileType])
                {
                    tileDepth = i;
                    break;
                }
            }

            // Store the greatest depth the crawler has reached.
            maxTileDepthWhenDigging = Math.Max(maxTileDepthWhenDigging, tileDepth);

            // Silently disappear if sufficiently deep inside of tiles.
            if (FablesUtils.FullSolidCollision(NPC.TopLeft, NPC.width, NPC.height) && tileDepth >= 11)
                NPC.active = false;


            if (!Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height) && NPC.velocity.Y > 0 && NPC.position.Y > walkDestinationX + 30)
            {
                // Get stuck if for some reason the crawler is no longer as deep as it was before.
                // This accounts for the edge-case of it digging through some shallow rock and ending up in a separate cavern.
                if (tileDepth <= maxTileDepthWhenDigging - 2)
                {
                    CurrentState = AIState.BeStuck;
                    PrepareForDifferentState();
                }
                else if (NPC.position.Y < walkDestinationX + 80)
                {
                    NPC.rotation = 0;
                    CurrentState = AIState.WalkAround;
                    PrepareForDifferentState();
                }

            }
        }

        public void DoBehavior_BeStuck()
        {
            // Use falling frames if falling.
            // Otherwise use idle frames.
            CurrentFrame = OnTopOfTiles ? FrameState.Idle : FrameState.Falling;

            // Disable horizontal movement.
            NPC.velocity.X *= 0.9f;

            // Make the graphical offset go downward.
            NPC.gfxOffY = (int)Utils.Remap(AITimer, 0f, 10f, 0f, 18f);

            if (!hasHitGroundWhileStuck && OnTopOfTiles && AITimer >= 10)
            {
                // Play a bonk sound.
                SoundEngine.PlaySound(BonkSound, NPC.Center);

                // Create particles.
                for (int i = 0; i < 5; i++)
                    CreateGroundHitDust(16f);

                // Shed crystals if that feature is enabled.
                if (LoseCrystalsWhenStuck)
                    ShedCrystals(-Vector2.UnitY);

                //Drop items
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int gemCount = Main.rand.Next(2, 5);
                    for (int i = 0; i < gemCount; i++)
                        DropRandomCrystal(NPC.GetSource_FromAI());
                }


                // Make all stars properly spawn. This allows them to draw in the natural particle loop and thusly not require a
                // sprite batch restart to draw it as part of the crawler.
                for (int i = 0; i < KnockoutStars.Count; i++)
                    ParticleHandler.SpawnParticle(KnockoutStars[i]);

                NPC.velocity.X = 0f;
                hasHitGroundWhileStuck = true;
                NPC.netUpdate = true;
            }

            // Update the cartoon knockout stars while stuck on the ground.
            if (hasHitGroundWhileStuck)
            {
                for (int i = 0; i < KnockoutStars.Count; i++)
                {
                    float orbitOffsetAngle = MathHelper.TwoPi * i / KnockoutStars.Count;
                    Vector2 orbitOffset = KnockoutStars[i].OffsetForAngle(orbitOffsetAngle + AITimer / 9f);

                    KnockoutStars[i].Position = NPC.Top - Vector2.UnitY * MathHelper.Lerp(15f, 25f, MathF.Sin(AITimer / 20f) * 0.5f + 0.5f) + orbitOffset * 40f;
                    KnockoutStars[i].Rotation = orbitOffset.X * 0.2f;
                    KnockoutStars[i].Time = KnockoutStars[i].Lifetime - 2;
                    KnockoutStars[i].SpinTilt = MathF.Sin(AITimer / 23f) * MathHelper.Pi / 14f;
                }
            }

            if (AITimer > 60 * 15)
            {
                NPC.velocity.Y = -2;
                CurrentState = AIState.WalkAround;
                PrepareForDifferentState();
                NPC.rotation = 0;

                //Take the particles out
                for (int i = 0; i < KnockoutStars.Count; i++)
                {
                    KnockoutStars[i].Kill();
                }
            }
        }
        #endregion

        public void CreateGroundHitDust(float verticalOffset = 0f, float scaleFactor = 1f)
        {
            for (int i = 0; i < 4; i++)
            {
                Point bottom = new((int)(NPC.Bottom.X / 16f) + Main.rand.Next(-2, 2), (int)(NPC.Bottom.Y / 16f));
                int dustIndex = WorldGen.KillTile_MakeTileDust(bottom.X, bottom.Y, Framing.GetTileSafely(bottom));
                float dustScaleInterpolant = Main.rand.NextFloat();

                Dust dust = Main.dust[dustIndex];
                dust.position.Y = NPC.Bottom.Y + verticalOffset - 16f;
                dust.velocity = -NPC.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(6f, 1f, dustScaleInterpolant) * scaleFactor;
                if (Main.rand.NextBool(3))
                    dust.velocity = dust.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFromList(-0.2f, 0.2f) + Main.rand.NextVector2Circular(0.4f, 0.4f);

                dust.scale += dustScaleInterpolant * 1.26f;
                dust.scale *= scaleFactor;
                dust.noGravity = true;
            }
        }

        public void PrepareForDifferentState()
        {
            AITimer = 0;
            NPC.netUpdate = true;
            hasPlayedDigSound = false;
        }

        public void ShedCrystals(Vector2 direction, bool clearArray = true)
        {
            for (int i = 1; i <= 4; i++)
            {
                // Create the crystal gores.
                Color crystalColor = Color.Transparent;
                CrystalType crystalType = i < CrystalTypes.Length ? CrystalTypes[i] : CrystalTypes.Last();
                if (CrystalColorTable.TryGetValue(crystalType, out Color c))
                    crystalColor = c;

                GeodeCrawlerCrystalGore gore = new(NPC.Center, direction.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.5f, 5f), crystalColor, NPC.scale);
                ParticleHandler.SpawnParticle(gore);

                // Clear the crystal type.
                if (clearArray)
                {
                    NPC.ai[Math.Min(i, 3)] = 0f;
                    NPC.netUpdate = true;
                }
            }

            // Create some sparkle particles.
            for (int i = 0; i < 20; i++)
            {
                Color dustColor = Color.Lerp(AverageColor, Color.Pink, Main.rand.NextFloat(0.16f)) * 1.6f;
                Vector2 sparkleSpawnPosition = NPC.Center + new Vector2(-NPC.spriteDirection * 12f, -5f).RotatedBy(NPC.rotation) + Main.rand.NextVector2Circular(30f, 7f);
                Dust sparkle = Dust.NewDustPerfect(sparkleSpawnPosition, 267, -Vector2.UnitY.RotatedByRandom(0.91f) * Main.rand.NextFloat(4f), 0, dustColor);
                sparkle.scale = 0.9f;
                sparkle.fadeIn = Main.rand.NextFloat(1.2f);
                sparkle.noLightEmittence = true;
                sparkle.noGravity = true;
            }
        }

        public virtual void DropRandomCrystal(IEntitySource dropSource)
        {
            if (IsGlimmering)
            {
                Item.NewItem(dropSource, NPC.Hitbox, (int)Main.rand.NextFromList(CrystalType.Amethyst, CrystalType.Topaz, CrystalType.Sapphire, CrystalType.Emerald, CrystalType.Ruby, CrystalType.Diamond));
                return;
            }

            int crystalIndex = Main.rand.Next(CrystalTypes.Length);
            if (CrystalTypes[crystalIndex] != 0)
                Item.NewItem(dropSource, NPC.Hitbox, (int)CrystalTypes[crystalIndex]);
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                CurrentFrame = FrameState.Walking;
                CrystalTypes = new CrystalType[]
                {
                    CrystalType.Ruby,
                    CrystalType.Emerald,
                    CrystalType.Sapphire
                };
            }

            switch (CurrentFrame)
            {
                case FrameState.Idle:
                    NPC.frameCounter = 0;
                    FrameIndex = 0f;
                    break;
                case FrameState.Falling:
                    NPC.frameCounter = 0;
                    FrameIndex = 1f;
                    break;
                case FrameState.Resting:
                    NPC.frameCounter = 0;
                    FrameIndex = 2f;
                    break;
                case FrameState.Surprised:
                    if (FrameIndex <= 2f)
                        FrameIndex = 3f;
                    NPC.frameCounter++;
                    if (NPC.frameCounter >= 6)
                    {
                        NPC.frameCounter = 0;
                        FrameIndex++;
                    }

                    if (FrameIndex >= 5f)
                    {
                        CurrentFrame = FrameState.Walking;
                        FrameIndex = 6f;
                    }
                    break;
                case FrameState.Walking:
                    NPC.frameCounter++;

                    int frameUpdateRate = CurrentState == AIState.DigAwayFromPlayer ? 11 : 6;
                    if (NPC.frameCounter >= frameUpdateRate)
                    {
                        NPC.frameCounter = 0;
                        FrameIndex++;
                    }
                    if (FrameIndex <= 4f || FrameIndex >= 11f)
                        FrameIndex = 6f;
                    break;
                case FrameState.Digging:
                    if (FrameIndex <= 10f)
                        FrameIndex = 11f;

                    if (Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height + 12))
                        NPC.frameCounter++;
                    if (NPC.frameCounter >= 8)
                    {
                        NPC.frameCounter = 0;
                        FrameIndex++;
                    }
                    if (FrameIndex >= 15f)
                        FrameIndex = 12f;
                    break;
            }

            NPC.frame = new Rectangle(0, (int)FrameIndex * 54, 64, 54);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
                drawColor = Color.White;
            Color tintedColor = NPC.TintFromBuffAesthetic(drawColor);

            // Draw the body.
            Texture2D texture = TextureAssets.Npc[Type].Value;
            Rectangle frame = NPC.frame;
            Vector2 drawPosition = NPC.Center - screenPos + NPC.GfxOffY();
            SpriteEffects direction = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, drawPosition, frame, NPC.GetAlpha(tintedColor), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0f);

            DrawCrystals(texture, drawPosition, drawColor);

            // Draw the shocked eyes.
            frame.X = frame.Width;
            Main.spriteBatch.Draw(texture, drawPosition, frame, tintedColor * NPC.Opacity * ShockedEyesOpacity * (1 - NPC.shimmerTransparency), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0f);

            return false;
        }

        public virtual void DrawCrystals(Texture2D texture, Vector2 drawPosition, Color drawColor)
        {
            SpriteEffects direction = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            for (int i = 0; i < CrystalTypes.Length; i++)
            {
                bool canDrawCrystal = CrystalColorTable.TryGetValue(CrystalTypes[i], out Color c);
                Color disco = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.4f + i * 0.1f + NPC.whoAmI * 0.3f) % 1, 0.7f, 0.75f);

                Color crystalColor = Color.Transparent;
                if (IsGlimmering)
                    crystalColor = disco;
                else if (canDrawCrystal)
                    crystalColor = c;
                else
                    continue;

                //Shell glows with spelunker
                if (Main.LocalPlayer.findTreasure)
                {
                    if (drawColor.R < 200)
                        drawColor.R = 200;
                    if (drawColor.G < 170)
                        drawColor.G = 170;
                }

                crystalColor = crystalColor.MultiplyRGB(drawColor);

                Rectangle frame = NPC.frame;
                frame.X = frame.Width * (2 + i);

                Main.spriteBatch.Draw(texture, drawPosition, frame, crystalColor * NPC.Opacity * (1 - NPC.shimmerTransparency), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0f);

                //SPecial overlay for glimmer
                if (IsGlimmering)
                {
                    crystalColor.A = 0;
                    crystalColor *= 0.6f + 0.4f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.6f + i * 0.3f);
                    Main.spriteBatch.Draw(texture, drawPosition, frame, crystalColor * NPC.Opacity * (1 - NPC.shimmerTransparency), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale * 1f, direction, 0f);
                }

                // Draw crystal highlights.
                if (CrystalHighlightColorTable.TryGetValue(CrystalTypes[i], out c) || IsGlimmering)
                {
                    frame.X += frame.Width * 3;

                    if (!IsGlimmering)
                    {
                        crystalColor = Color.Lerp(c, c.MultiplyRGB(drawColor), 0.9f);
                        if (CrystalTypes[i] == CrystalType.Sapphire)
                            crystalColor *= 0.5f;
                    }
                    else
                    {
                        crystalColor = Color.Lerp(Color.White, disco, 0.2f);
                        crystalColor = Color.Lerp(crystalColor, crystalColor.MultiplyRGB(drawColor), 0.9f);
                    }

                    crystalColor.A /= 2;
                    Main.spriteBatch.Draw(texture, drawPosition, frame, crystalColor * NPC.Opacity * (1 - NPC.shimmerTransparency), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0f);
                }
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.PlayerSafe)
                return 0f;

            // Check if there are any gem trees nearby.
            // If there are, bias the spawn rate of the crawler immensely.
            if (!NPC.AnyNPCs(Type))
            {
                Point spawnPos = new Point(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1);
                for (int i = -25; i < 25; i++)
                {
                    for (int j = -2; j < 12; j++)
                    {
                        Point checkPoint = new(spawnInfo.SpawnTileX + i, spawnInfo.SpawnTileY + j);
                        Tile t = Framing.GetTileSafely(checkPoint);
                        if (TileID.Sets.CountsAsGemTree[t.TileType] && t.HasTile)
                        {
                            //only if the gem tree is connected
                            Point treeTarget = spawnPos + new Point(i, j);
                            float maxDistance = spawnPos.DistanceTo(treeTarget);

                            if (AStarPathfinding.IsThereAPath(spawnPos, treeTarget, AStarNeighbour.BasicCardinalOrdinal, AStarPathfinding.AirNavigable, maxDistance * 2f))
                                return SpawnCondition.Cavern.Chance * 2.2f;
                        }
                    }
                }
            }

            return SpawnCondition.Cavern.Chance * 0.02f;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ)
                return;

            // Create death gores.
            if (NPC.life <= 0)
            {
                // Create crystal gores.
                ShedCrystals(Vector2.UnitX * hit.HitDirection, false);

                // Create body part gores.
                for (int i = 1; i <= 3; i++)
                    Gore.NewGorePerfect(NPC.GetSource_Death(), NPC.Center, Vector2.UnitX.RotatedByRandom(0.5f) * hit.HitDirection * Main.rand.NextFloat(0.5f, 5f), Mod.Find<ModGore>($"GeodeCrawler{VariantName}_Gore{i}").Type, NPC.scale);
            }
        }

        // This function exists for the purpose of making crystals drops less likely if the crawler has less of those crystals on its back.
        public static void ChangeCrystalYields(NPC npc, int itemID, ref int minQuantity, ref int maxQuantity)
        {
            if (npc.ModNPC is not GeodeCrawler crawler)
                return;
            float crystalRatio = MathF.Pow(crawler.CrystalRatio((CrystalType)itemID), 0.72f);

            //Equal chance for each crystal when glimmering
            if (crawler.IsGlimmering)
                crystalRatio = 1 / 6f;

            minQuantity = (int)(minQuantity * crystalRatio);
            maxQuantity = (int)(maxQuantity * crystalRatio);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(new VariedStackProportionDropRule(new(1, 1), ChangeCrystalYields, true, new Helpers.WeightedItemStack[]
            {
                new(ItemID.Amethyst, 1f, MinUnscaledCrystalQuantity, MaxUnscaledCrystalQuantity),
                new(ItemID.Topaz, 1f, MinUnscaledCrystalQuantity, MaxUnscaledCrystalQuantity),
                new(ItemID.Sapphire, 1f, MinUnscaledCrystalQuantity, MaxUnscaledCrystalQuantity),
                new(ItemID.Emerald, 1f, MinUnscaledCrystalQuantity, MaxUnscaledCrystalQuantity),
                new(ItemID.Ruby, 1f, MinUnscaledCrystalQuantity, MaxUnscaledCrystalQuantity),
                new(ItemID.Diamond, 1f, MinUnscaledCrystalQuantity, MaxUnscaledCrystalQuantity),
            }));

            npcLoot.Add(ModContent.ItemType<GeodeCandy>(), 25);
        }
    }

    // Gores are frustratingly rigid, and do not allow me to change colors in the same way as dusts or other entities, hence the usage of this particle.
    public class GeodeCrawlerCrystalGore : Particle
    {
        public override string Texture => $"{AssetDirectory.Assets}Gores/GeodeCrawlerCrystal_Gore{Variant + 1}";

        public override bool UseCustomDraw => true;

        public GeodeCrawlerCrystalGore(Vector2 position, Vector2 speed, Color color, float scale = 1f)
        {
            Position = position;
            Scale = scale;
            Color = color;
            Velocity = speed;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Lifetime = 180;
            Variant = Main.rand.Next(4);
        }

        public override void Update()
        {
            Lifetime = 180;
            Velocity = Collision.TileCollision(Position, Velocity + Vector2.UnitY * 0.3f, 6, 6);
            if (Velocity.Y == 0f)
                Velocity.X = 0f;
            if (Time >= Lifetime)
                Kill();

            // SPIN 2 WIN.
            Rotation += Velocity.X * 0.02f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            float opacity = Utils.GetLerpValue(0f, -180f, Time - Lifetime, true);
            Color lightColor = Lighting.GetColor(Position.ToTileCoordinates());
            Texture2D texture = ParticleTexture;
            Texture2D highlight = ModContent.Request<Texture2D>($"{AssetDirectory.Assets}Gores/GeodeCrawlerCrystalHighlight_Gore{Variant + 1}").Value;
            spriteBatch.Draw(texture, Position - basePosition, null, Color.MultiplyRGBA(lightColor) * opacity, Rotation, texture.Size() * 0.5f, Scale, 0, 0);
            spriteBatch.Draw(texture, Position - basePosition, null, Color.MultiplyRGBA(lightColor) with { A = 25 } * opacity, Rotation, texture.Size() * 0.5f, Scale, 0, 0);
            spriteBatch.Draw(highlight, Position - basePosition, null, lightColor * opacity * 0.6f, Rotation, texture.Size() * 0.5f, Scale, 0, 0);
        }
    }

    public class ExclamationMarkParticle : Particle
    {
        public float Opacity;
        public override string Texture => AssetDirectory.UndergroundNPCs + "GeodeExclamationMark";

        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;


        public ExclamationMarkParticle(Vector2 position, Vector2 velocity, float scale, float rotation)
        {
            Position = position;
            Velocity = velocity;
            Scale = scale;
            Opacity = 1f;
            Rotation = rotation;
            Lifetime = 25;
        }

        public override void Update()
        {
            float punch = (float)Math.Pow(1 - LifetimeCompletion, 2f);

            Velocity *= 0.91f;
            Opacity = 1 - (float)Math.Pow(LifetimeCompletion, 3f);
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D sprite = ParticleTexture;
            Vector2 size = Scale * (1 + (float)Math.Pow(1 - LifetimeCompletion, 3f) * 1.3f) * Vector2.One;
            size.Y *= 1f + 0.06f * -Velocity.Y;

            Vector2 origin = new Vector2(sprite.Width / 2, sprite.Height - 2);

            spriteBatch.Draw(sprite, Position - basePosition, null, Color.White * Opacity, Rotation, origin, size, 0, 0);
        }
    }
}
