using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.Utilities;
using ReLogic.Utilities;
using CalamityFables.Particles;
using System.IO;
using CalamityFables.Content.Items.CrabulonDrops;
using Terraria.GameContent.ItemDropRules;
using CalamityFables.Content.Items.SirNautilusDrops;
using CalamityFables.Content.Items.DesertScourgeDrops;
using CalamityFables.Content.Boss.DesertWormBoss;
using CalamityFables.Content.Tiles.MusicBox;
using CalamityFables.Content.Tiles.Graves;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    [AutoloadBossHead]
    [ReplacingCalamity("Crabulon")]
    public partial class Crabulon : ModNPC, IDrawOverTileMask
    {
        #region Variables & Setup
        private static float DifficultyScale => Main.getGoodWorld ? 8f : Main.masterMode ? 3 : Main.expertMode ? 2 : 0;

        #region Sound Effects
        public static readonly SoundStyle HitSound = new(SoundDirectory.Crabulon + "CrabulonHit") { MaxInstances = 6, PitchVariance = 0.12f };
        public static readonly SoundStyle FlimsyHitSound = new(SoundDirectory.Crabulon + "CrabulonFlimsyHit", 2) { MaxInstances = 6, PitchVariance = 0.12f };
        public static readonly SoundStyle DeathSound = new(SoundDirectory.Crabulon + "CrabulonDeath");

        public static readonly SoundStyle TwitchSound = new(SoundDirectory.Crabulon + "CrabulonTwitch", 3) { PitchVariance = 0.1f };
        public static readonly SoundStyle StepSound = new(SoundDirectory.Crabulon + "CrabulonStep", 4) { MaxInstances = 6, PitchVariance = 0.1f };
        public static readonly SoundStyle ClawClickSound = new(SoundDirectory.Crabulon + "CrabulonClawClick", 2) { Volume = 0.6f, PitchVariance = 0.15f };
        public static readonly SoundStyle ClawClackSound = new(SoundDirectory.Crabulon + "CrabulonClawClack");
        public static readonly SoundStyle LightSlamSound = new(SoundDirectory.Crabulon + "CrabulonLightSlam");


        public static readonly SoundStyle ThumpDigSound = new(SoundDirectory.Crabulon + "CrabulonThump") { PitchVariance = 0.1f };
        public static readonly SoundStyle EmergeSound = new(SoundDirectory.Crabulon + "CrabulonEmerge");
        public static readonly SoundStyle SpawnScreamSound = new(SoundDirectory.Crabulon + "CrabulonRoar");

        public static readonly SoundStyle SlamSound = new(SoundDirectory.Crabulon + "CrabulonGroundSlam");
        public static readonly SoundStyle GetupSound = new(SoundDirectory.Crabulon + "CrabulonRecover", 3);

        public static readonly SoundStyle SporeMinefieldShakeSound = new(SoundDirectory.Crabulon + "CrabulonSporeShake", 2);
        public static readonly SoundStyle SporeMinefieldDeploySound = new(SoundDirectory.Crabulon + "CrabulonSporeMinefield") { Volume = 0.8f };
        public static readonly SoundStyle SporeMortarChargeSound = new(SoundDirectory.Crabulon + "CrabulonSporeCharge");
        public static readonly SoundStyle SporeMortarFireSound = new(SoundDirectory.Crabulon + "CrabulonSporeShoot") { Volume = 0.5f };
        public static readonly SoundStyle SporeMortarLandSound = new(SoundDirectory.Crabulon + "CrabulonSporeLand") { Volume = 0.9f };
        public static readonly SoundStyle ClawSnipTelegraphSound = new(SoundDirectory.Crabulon + "CrabulonClawSnipTelegraph", 2);
        public static readonly SoundStyle ClawSnipSound = new(SoundDirectory.Crabulon + "CrabulonClawSnipDash");
        public static readonly SoundStyle ClawSlamTelegraphSound = new(SoundDirectory.Crabulon + "CrabulonClawSlamTelegraph", 2);
        public static readonly SoundStyle ClawSlamSound = new(SoundDirectory.Crabulon + "CrabulonClawSlam");
        public static readonly SoundStyle ShriekSound = new(SoundDirectory.Crabulon + "CrabulonShriek", 2) { Volume = 0.8f };
        public static readonly SoundStyle GrappleSound = new(SoundDirectory.Crabulon + "CrabulonGrapple");
        public static readonly SoundStyle GrappleLoopSound = new(SoundDirectory.Crabulon + "CrabulonGrappleLoop") { IsLooped = true };
        public static readonly SoundStyle GrappleDetachSound = new(SoundDirectory.Crabulon + "CrabulonDetach");
        public static readonly SoundStyle DashSound = new(SoundDirectory.Crabulon + "CrabulonDashNormal");
        public static readonly SoundStyle DashExpertSound = new(SoundDirectory.Crabulon + "CrabulonDashExpert");


        public SlotId GrappleSoundSlot;

        public static readonly SoundStyle HorrorScreamSound = new(SoundDirectory.Crabulon + "CrabulonMushroomCaveIncident") { SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, MaxInstances = 1};
        public static readonly SoundStyle HorrorSound = new(SoundDirectory.Crabulon + "CrabulonMushroomHorrorLoop") { Volume = 0.9f, IsLooped = true };
        public SlotId HorrorSoundSlot;
        #endregion

        #region AI states
        public enum ActionState
        {
            Chasing,
            Chasing_FallingDown,
            Chasing_Scuttle,
            Chasing_WallCrawl,
            Chasing_WallFastCrawl,
            Chasing_JumpUp,

            SporeMines = 10, //Crabulon shakes around, emitting spores. Multiple spore buds grow around the player and itself that emit toxic gas
            SporeMines_Telegraph,
            SporeMines_Recovery,

            SporeBomb = 20, //Crabulon chucks a spore bomb ahead of the player if they try to flee
            SporeBomb_ChargeBomb,
            //Variant where he spawns the bomb on himself
            SporeBomb_SpawnOnSelfCharge,
            SporeBomb_Recovery,


            Charge = 30, //Crabulon screeches and dashes ahead towards the player (omnidirectional)
            Charge_Screech,
            Charge_DashForwards,
            Charge_Recovery,

            HuskDrop = 40, //Vines latch up to crabulon from the ceiling and reel him up above the player, before being dropped straight onto the player
            HuskDrop_VineAttach,
            HuskDrop_ReelUp,
            HuskDrop_Chase,
            HuskDrop_Drop,
            HuskDrop_Stunned,

            Snip = 50, //Crabulon does a fast forwards stab with its claw
            Snip_ReadyClaw,
            Snip_ThrustForwards,

            Slam = 60, //Crabulon does a large overhead slam
            Slam_ReadyClaw,
            Slam_SlamDown,

            Slingshot = 70, //Crabulon spits a mycelium web that connects to the player, before reeling itself in, overshooting the player and cornering them by following it up with a melee attack
            Slingshot_SpitAndWait,
            Slingshot_JumpSlowmo,
            Slingshot_Reel_in,
            Slingshot_Overshoot,

            Desperation = 80, //Crabulon does a repeated alterered version of husk drop after falling below 10% health
            Desperation_CinematicWait,
            Desperation_VineAttach,
            Desperation_ReelUp,
            Desperation_Chase,
            Desperation_Drop,
            Desperation_Stunned,

            Dead = 90, //Crabulon dies limping, and the player must hit him a few times to get the loot out
            Dead_Limping,
            Dead_InternalGoreSimulation, //Mode used to simulate the limping gore when it dies

            Despawning = 100,
            DespawningScurrySlowly,
            DespawningScurryFast,
            DespawningDropDownDesperation,

            Raving = 110,
            Raving_Hardswap,
            Raving_Circling,
            Raving_SideChaCha,
            Raving_Tapdance,

            SpawningUp = 120,
            SpawningUp_Emerge,
            SpawningUp_Wait,
            SpawningUp_Scream,
            SpawningUp_Accelerate,

            ClentaminatedAway = 130,
            ClentaminatedAway_DieHorribly,

            DebugDisplay = 660
        }

        public ActionState AIState {
            get => (ActionState)(NPC.ai[0] - (NPC.ai[0] % 10));
            set => NPC.ai[0] = (float)value;
        }

        public ActionState SubState {
            get => (ActionState)NPC.ai[0];
            set {
                //if (Main.netMode == NetmodeID.Server)
                //    new CrabulonSubstatePacket(this).Send(-1, -1, false);
                NPC.ai[0] = (float)value;
                NPC.netUpdate = true;
            }
        }

        public ActionState PreviousState {
            get => (ActionState)(NPC.localAI[0] - (NPC.localAI[0] % 10));
            set => NPC.localAI[0] = (float)value;
        }
        #endregion

        #region Useful variables
        public ref float AttackTimer => ref NPC.ai[1];
        public ref float ExtraMemory => ref NPC.ai[3];
        public bool DesperationPhaseReached => DifficultyScale >= 2 && NPC.life / (float)NPC.lifeMax <= DesperationPhaseTreshold;

        public Vector2 goalPosition;
        public Vector2 oldPosition;
        #endregion

        #region Balance values
        public static int Stat_LifeMax = 5000;
        public static int Stat_LifeMaxExpert = 7600;
        public static int Stat_Defense = 4;
        public static int Stat_Damage = 30;

        public static float DesperationPhaseTreshold = 0.1f;

        public static int SporeInfestation_BudLifeMax = 110;
        public static int SporeInfestation_GasRadiusMin = 60;
        public static int SporeInfestation_GasRadiusMax = 120;
        public static int SporeInfestation_InflictionDamage = 100;
        public static float SporeInfestation_MinInflictionTime = 1.5f;
        public static float SporeInfestation_MaxInflictionTime = 3;
        public static float SporeInfestation_InflictionRampupTime = 0.5f;

        public static int SporeBomb_BudLifeMax = 130;
        public static int SporeBomb_GasRadiusMax = 140;

        public static int HuskDrop_ShockwaveDamage = 80;
        public static int HuskDrop_FallingSkullDamage = 22;
        public static int HuskDrop_DropContactDamage = HuskDrop_ShockwaveDamage;

        public static int Charge_ContactDamage = 35;
        public static float Charge_TrailLingerTime = 2;

        public static int SporeHeart_UnsporedHealing = 10;
        public static int SporeHeart_SporedHealing = 50;

        public static int Slam_SlamDamage = 35;
        public static int Snip_SnipDamage = 35;

        public static float Desperation_DamageResist = 0.6f;

        public static float NormalDamageBoost => 1.25f;
        public static float DamageMultiplier => (!Main.expertMode ? NormalDamageBoost : 1f) * 0.5f;
        #endregion

        public int biomeDespawnTimer = 5 * 60;
        public Player target => Main.player[NPC.target];
        public Vector2 movementTarget;
        public Vector2 movementTarget2;

        public List<CrabulonLeg> Legs;
        public List<Vector2> IdealStepPositions;
        public List<Vector2> PreviousIdealStepPositions;

        public override void Load()
        {
            On_NPC.ScaleStats_UseStrengthMultiplier += UnscaleCrabsHealth;
            FablesDrawLayers.DrawThingsBehindSolidTilesAndBackgroundNPCsEvent += DrawCrabulonStrings;
            FablesNPC.ModifyBossMapIconDrawingEvent += ModifyMapIcon;

            FablesGeneralSystemHooks.PostUpdateEverythingEvent += SetScreenshaderEffect;
            SoundHandler.ModifyMusicChoiceEvent += SetMusic;

            LoadMapIcons();
            LoadGores();

            RelicType = BossRelicLoader.LoadBossRelic(Name, "Crabulon", AssetDirectory.CrabulonDrops);
            TrophyType = BossTrophyLoader.LoadBossTrophy(Name, "Crabulon", AssetDirectory.CrabulonDrops);
            BossBagType = BossBagLoader.LoadBossBag(Name, "Crabulon", AssetDirectory.CrabulonDrops, true, out TreasureBag);
            FablesGeneralSystemHooks.LogBossChecklistEvent += AddToChecklist;
        }
        public void LoadGores()
        {
            Mod.AddContent(new CrabulonBodyGore("CrabbyBodyGore1"));
            Mod.AddContent(new CrabulonBodyGore("CrabbyBodyGore2"));

            Mod.AddContent(new CrabulonBodyGore("HairyBodyGore1"));
            Mod.AddContent(new CrabulonBodyGore("HairyBodyGore2"));

            Mod.AddContent(new CrabulonBodyGore("ShroomyBodyGore1"));
            Mod.AddContent(new CrabulonBodyGore("ShroomyBodyGore2"));
        }

        private void AddToChecklist(Mod bossChecklist)
        {
            var collectibleDrops = new List<int>()
                {
                    RelicType, TrophyType, ModContent.ItemType<CrabulonBossMask>(), ModContent.ItemType<CrabulonMusicBoxItem>(), ModContent.ItemType<ClawShell>()
                    , ModContent.ItemType<FungalThroatInfection>(), ModContent.ItemType<MushroomBoots>(), ModContent.ItemType<TechwearSporeMask>()
                };

            bossChecklist.Call("LogBoss", Mod, nameof(Crabulon),
                3.6f, //After evils, before bee
                () => WorldProgressionSystem.DefeatedCrabulon,
                Type,
                new Dictionary<string, object>()
                {
                    ["spawnInfo"] = LocalizationRoundabout.DefaultText("Compat.BossChecklist.Crabulon.SpawnInfo", "Can naturally spawn peeking out in the mushroom caves before its defeat. Draw its attention by using a [i:{0}] hookstaff on mushroom grass.")
                    .WithFormatArgs(ModContent.ItemType<Hookstaff>()),
                    ["despawnMessage"] = LocalizationRoundabout.DefaultText("Compat.BossChecklist.Crabulon.Despawn", "The mushroom caves become silent again..."),
                    ["collectibles"] = collectibleDrops,
                    ["spawnItems"] = ModContent.ItemType<Hookstaff>(),
                    ["customPortrait"] = DrawBossChecklistPortrait
                });
        }


        public override string Texture => AssetDirectory.Crabulon + "Crabby/Body";
        public override string BossHeadTexture => AssetDirectory.Crabulon + "CrabulonMap";


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crabulon");

            //Set the type data for the treasure bag & the drops
            TreasureBag.NPCType = Type;
            TreasureBag.bossLoot = CommonDrops;

            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                PortraitPositionXOverride = 2,
                PortraitPositionYOverride = 2
            };

            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<CrabulonDOT>()] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<InfestationDOT>()] = true;
        }

        public override void SetDefaults()
        {
            NPC.lifeMax = Stat_LifeMax;
            NPC.defense = Stat_Defense;
            NPC.damage = Stat_Damage;
            NPC.npcSlots = 12f;
            NPC.width = 140;
            NPC.height = 100;

            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.value = Item.buyPrice(0, 5, 0, 0);
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = HitSound;
            NPC.DeathSound = DeathSound;
            NPC.netAlways = true;
            NPC.scale = 1f;
            Music = SoundHandler.GetMusic("Crabulon", MusicID.OtherworldlyBoss1);

            NPC.behindTiles = true;
            NPC.dontTakeDamage = true;

            if (!Main.dedServ)
            {
                //Seed advances per each crabulon defeated
                int seed = Main.ActiveWorldFileData.Seed + WorldProgressionSystem.crabulonsDefeated;
                //If more than 1 crabulon are alive at a time somehow, advance the cycle
                int activeCrabulons = Main.npc.Count(n => n.active && n.type == Type);
                seed += activeCrabulons;
                UnifiedRandom randSeed = new UnifiedRandom(seed);

                for (int i = 0; i < 7; i++)
                {
                    int skin_count = i == 0 ? BODY_VARIANTS : i < 5 ? LEG_VARIANTS : i == 5 ? VIOLIN_ARM_VARIANTS : ARM_VARIANTS;
                    chosenSkinIndices[i] = randSeed.Next(0, skin_count);
                }

                //The first crabulon is always fully crabby
                if (WorldProgressionSystem.crabulonsDefeated == 0 && activeCrabulons == 0)
                {
                    for (int i = 0; i < 7; i++)
                        chosenSkinIndices[i] = 0;
                }

                ApplySkin(BodySkins[chosenSkinIndices[0]]);
            }
            else
                chosenSkinIndices = new int[7];
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundMushroom,

                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.Crabulon")
            });
        }
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) => ScaleExpertStats(NPC, numPlayers, balance);
        public static void ScaleExpertStats(NPC npc, int numPlayers, float bossLifeScale)
        {
            int usedLifemax = Main.expertMode ? Stat_LifeMaxExpert : Stat_LifeMax;
            if (Main.masterMode)
                usedLifemax += Stat_LifeMaxExpert - Stat_LifeMax;

            npc.lifeMax = (int)(usedLifemax * bossLifeScale);
            npc.damage = Stat_Damage;
        }

        private void UnscaleCrabsHealth(On_NPC.orig_ScaleStats_UseStrengthMultiplier orig, NPC self, float strength)
        {
            if (self.type == Type)
            {
                if (strength < 1)
                    strength = MathHelper.Lerp(strength, 1f, 0.7f);
                else
                    strength = MathHelper.Lerp(strength, 1f, 0.9f);
            }
            orig(self, strength);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.behindTiles);
            writer.Write(NPC.dontTakeDamage);
            writer.WriteVector2(oldPosition);
            writer.WriteVector2(goalPosition);
            writer.Write(biomeDespawnTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.behindTiles = reader.ReadBoolean();
            NPC.dontTakeDamage = reader.ReadBoolean();
            oldPosition = reader.ReadVector2();
            goalPosition = reader.ReadVector2();
            biomeDespawnTimer = reader.ReadInt32();
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
            }

            AttackTimer = 1;
            AIState = ActionState.SpawningUp;

            if (!Main.dedServ)
                InitializeLimbs();
        }
        #endregion

        #region AI
        public override bool PreAI()
        {
            NPC.netOffset = Vector2.Zero;
            return base.PreAI();
        }

        public override void AI()
        {
            bool swapToNextAttack = false;

            //NPC.velocity = Vector2.Zero;
            //NPC.direction = -(NPC.Center.X - Main.LocalPlayer.Center.X).NonZeroSign();

            //Reset arm openness here because doing it afterwards means that attacks cant mess with it
            if (Arms != null && Arms.Count >= 2)
                Arms[1].clawOpennessOverride = null;

            if (flickerOpacity == 0 && flickerBackground != 0)
                flickerBackground = 0;

            #region Despawn checks
            if (AIState == ActionState.Chasing || SubState == ActionState.HuskDrop_Chase || (AIState == ActionState.Desperation && (int)SubState == (int)ActionState.Desperation_Chase))
            {
                //Despawn in a unique way if despawning from the desperation attack
                ActionState despawnState = (AIState == ActionState.Desperation || SubState == ActionState.HuskDrop_Chase) ? ActionState.DespawningDropDownDesperation : ActionState.Despawning;

                //If we lack a target and cant find any player to retarget to
                if (HasNoValidTarget && !GetClosestMushroomCavesPlayer(16 * 160))
                {
                    AttackTimer = 1;
                    AIState = despawnState;
                    NPC.netUpdate = true;
                }

                if (!PlayerInMushroomCaves(target))
                    GetClosestMushroomCavesPlayer(16 * 160);

                if (biomeDespawnTimer <= 0)
                {
                    AttackTimer = 1;
                    AIState = despawnState;
                    NPC.netUpdate = true;
                }
            }

            if (!PlayerInMushroomCaves(target) && AIState != ActionState.Dead && AIState != ActionState.SpawningUp && AIState != ActionState.ClentaminatedAway)
                biomeDespawnTimer--;
            else
                biomeDespawnTimer = 60 * 5;
            #endregion

            #region Behavior (See NPCs.CrabulonAttacks.cs)
            if (AIState == ActionState.Chasing)
                swapToNextAttack = IdleMotion();
            else if (AIState == ActionState.Charge)
                swapToNextAttack = ChargeAttack();
            else if (AIState == ActionState.SporeMines)
                swapToNextAttack = SporeMineAttack();
            else if (AIState == ActionState.SporeBomb)
                swapToNextAttack = SporeBombAttack();
            else if (AIState == ActionState.HuskDrop)
                swapToNextAttack = HuskDrop();
            else if (AIState == ActionState.Slam)
                swapToNextAttack = ClawSlam();
            else if (AIState == ActionState.Snip)
                swapToNextAttack = ClawSnip();
            else if (AIState == ActionState.Slingshot)
                swapToNextAttack = Slingshot();
            else if (AIState == ActionState.Desperation)
                DesperationSlams();
            else if (AIState == ActionState.Despawning)
                DespawnBehavior();
            else if (AIState == ActionState.Raving)
                RaveOnACorpse();
            else if (AIState == ActionState.Dead)
                DeathRagdoll();
            else if (AIState == ActionState.SpawningUp)
                swapToNextAttack = SpawnAnimation();
            else if (AIState == ActionState.ClentaminatedAway)
                ClentaminatorDeath();
            else if (AIState == ActionState.DebugDisplay)
            {
                if (Main.mouseRight)
                    NPC.Center = Main.MouseWorld;
                NPC.velocity = Vector2.Zero;
            }
            #endregion
            if (swapToNextAttack)
                SelectNextAttack();

            //Go to the final desperation phase
            if (AIState == ActionState.Chasing && DesperationPhaseReached)
            {
                AIState = ActionState.Desperation;
                AttackTimer = 0;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                CustomHurtbox();
                CheckForClentaminator();
            }

            if (!Main.dedServ)
            {
                SimulateLimbs();
                HandlePureVisualStuff();
            }

            float lightAmount = 1f;
            if (SubState == ActionState.HuskDrop_Stunned)
            {
                lightAmount -= AttackTimer * Main.rand.NextFloat(0.5f);
            }

            if (SubState != ActionState.SpawningUp_Emerge)
                Lighting.AddLight(NPC.Center, CommonColors.MushroomDeepBlue.ToVector3() * lightAmount);


            if (Main.zenithWorld && (int)SubState % 10 == 1)
            {
                for (int i = 0; i < 7; i++)
                {
                    int skin_count = i == 0 ? BODY_VARIANTS : i < 5 ? LEG_VARIANTS : i == 5 ? VIOLIN_ARM_VARIANTS : ARM_VARIANTS;
                    chosenSkinIndices[i] = Main.rand.Next(0, skin_count);
                }
                ApplySkin(BodySkins[chosenSkinIndices[0]]);
            }

            //Malice mode
            if (false && (AIState == ActionState.Chasing ||
                SubState == ActionState.SporeMines_Recovery ||
                SubState == ActionState.SporeBomb_Recovery ||
                SubState == ActionState.Charge_Recovery ||
                SubState == ActionState.HuskDrop_Stunned))
                AttackTimer = 0f;
        }

        public bool HasNoValidTarget => NPC.target < 0 || NPC.target == Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active || NPC.Distance(target.Center) > 200 * 16;

        public bool PlayerInMushroomCaves(Player p) => (p.ZoneGlowshroom || (CalamityFables.RemnantsEnabled && Main.netMode != NetmodeID.SinglePlayer)) && (p.ZoneDirtLayerHeight || p.ZoneRockLayerHeight);

        public bool GetClosestMushroomCavesPlayer(float distance)
        {
            IEnumerable<Player> potentialTargets = Main.player.Where(p => p.active && !p.dead && PlayerInMushroomCaves(p) && p.Distance(NPC.Center) < distance);
            if (potentialTargets.Count() == 0)
                return false;

            Player targetChoice = potentialTargets.OrderBy(p => p.Distance(NPC.Center)).FirstOrDefault();
            if (targetChoice == null)
                return false;

            int oldTarget = NPC.target;
            NPC.target = targetChoice.whoAmI;
            if (oldTarget != NPC.target)
                NPC.netUpdate = true;
            return true;
        }

        #endregion

        #region Inverse Kinematics
        public void SimulateLimbs()
        {
            if (Legs == null)
                InitializeLimbs();

            float highestReleaseScore = float.MinValue;
            CrabulonLeg highestReleaseLeg = null;
            int attachedLegs = 0;

            foreach (CrabulonLeg limb in Legs)
            {
                limb.Update();
                if (limb.latchedOn)
                    attachedLegs++;

                if (limb.ReleaseScore() > highestReleaseScore && (NPC.scale < 2f || limb.stepTimer <= 0))
                {
                    highestReleaseLeg = limb;
                    highestReleaseScore = limb.ReleaseScore();
                }

            }

            if (NPC.velocity.Length() > 1f && attachedLegs > 3 && highestReleaseLeg != null)
            {
                highestReleaseLeg.ReleaseGrip();
            }
        }

        public void InitializeLimbs()
        {
            Legs = new List<CrabulonLeg>();
            for (int i = 0; i < 4; i++)
            {
                float baseRotation = MathHelper.Lerp(MathHelper.PiOver4 * 1.5f, -MathHelper.PiOver4 * 1.5f, i / 3f) + MathHelper.PiOver2;
                CrabulonLeg leg = new CrabulonLeg(this, i < 1 || i > 2, i < 2, baseRotation, LegSkins[chosenSkinIndices[i + 1]]);
                Legs.Add(leg);
            }

            for (int i = 0; i < 4; i++)
            {
                int set = i < 2 ? 0 : 2;
                int otherSisterOffset = i % 2 == 0 ? 1 : 0;
                int pairedleg = i == 3 ? 0 : (i == 0 ? 3 : (i == 1 ? 2 : 1));

                Legs[i].pairedLeg = Legs[pairedleg];
                Legs[i].sisterLeg = Legs[set + otherSisterOffset];
            }
        }
        #endregion

        #region Hitting, getting hit, dying
        //NO contact damage
        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            //Resist damage when should transition into final phase
            if (AIState != ActionState.Desperation && DesperationPhaseReached)
                modifiers.TargetDamageMultiplier *= 0.1f;

            if (AIState == ActionState.Desperation && (SubState != ActionState.Desperation_Stunned && SubState != ActionState.Desperation_Drop))
                modifiers.FinalDamage *= Desperation_DamageResist;
        }

        public void CustomHurtbox()
        {
            if (SubState == ActionState.Snip_ThrustForwards)
            {
                float lineThickness = 70;

                Vector2 lineStart = FloorPosition - Vector2.UnitY * lineThickness * 0.5f;
                Vector2 lineEnd = lineStart + Vector2.UnitX * NPC.direction * NPC.scale * 250f;

                //Dust.QuickDustLine(lineStart, lineEnd, 20, Color.Red);

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player potentiallyHurtPlayer = Main.player[i];
                    //First off , check if the player has even a remote chance of being hittable
                    if (potentiallyHurtPlayer.active && !potentiallyHurtPlayer.dead)
                    {
                        float uselses = 0f;

                        if (Collision.CheckAABBvLineCollision(potentiallyHurtPlayer.position, potentiallyHurtPlayer.Size, lineStart, lineEnd, lineThickness, ref uselses))
                            Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), potentiallyHurtPlayer.Center + Vector2.UnitX * (NPC.Center.X - potentiallyHurtPlayer.Center.X).NonZeroSign(), Vector2.Zero, ModContent.ProjectileType<CrabulonClawHitbox>(), (int)(Snip_SnipDamage * DamageMultiplier), 1, Main.myPlayer, i, NPC.direction * 10f, -6);
                    }
                }

                return;
            }

            int damage = 0;
            float radius = 0f;
            Vector2 origin = NPC.Center;
            int type = ModContent.ProjectileType<CrabulonClawHitbox>();

            if ((SubState == ActionState.HuskDrop_Drop || SubState == ActionState.Desperation_Drop) && NPC.velocity.Y > 0)
            {
                type = ModContent.ProjectileType<CrabulonDropHitbox>();
                damage = HuskDrop_DropContactDamage;
                radius = 70f;
            }
            else if (SubState == ActionState.Charge_DashForwards || SubState == ActionState.Charge_Recovery)
            {
                damage = Charge_ContactDamage;
                radius = SubState == ActionState.Charge_DashForwards ? 60 : 50;
            }
            else if (SubState == ActionState.Slam_SlamDown && AttackTimer > 0.5f)
            {
                damage = Slam_SlamDamage;
                radius = Math.Min(280, 235 + 15f * DifficultyScale); //Slightly tighter dodge in expert
                origin = FloorPosition;

                //Debug radius
                //for (int j = 0; j < 40; j++)
                //    Dust.QuickDust(origin + (j / 40f * MathHelper.TwoPi).ToRotationVector2() * radius * NPC.scale, Color.Red);
            }

            if (radius == 0)
                return;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player potentiallyHurtPlayer = Main.player[i];
                //First off , check if the player has even a remote chance of being hittable
                if (potentiallyHurtPlayer.active && !potentiallyHurtPlayer.dead && NPC.Distance(potentiallyHurtPlayer.Center) < radius * NPC.scale)
                {
                    if (SubState == ActionState.Slam_SlamDown && (
                        (potentiallyHurtPlayer.Center.X - origin.X) * NPC.direction <= 0 || potentiallyHurtPlayer.Top.Y > origin.Y))
                        continue;

                    if (FablesUtils.AABBvCircle(potentiallyHurtPlayer.Hitbox, origin, radius * NPC.scale))
                    {
                        Vector2 knockback = Vector2.Zero;
                        if (SubState == ActionState.Slam_SlamDown)
                            knockback = new Vector2((potentiallyHurtPlayer.Center.X - origin.X).NonZeroSign() * 10f, -6f);
                        else if (SubState == ActionState.Charge_DashForwards)
                            knockback = NPC.velocity.SafeNormalize(-Vector2.UnitY).RotatedBy(MathHelper.PiOver4 * -NPC.velocity.X.NonZeroSign()) * 10f;
                        Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), potentiallyHurtPlayer.Center + Vector2.UnitX * (NPC.Center.X - potentiallyHurtPlayer.Center.X).NonZeroSign(), Vector2.Zero, type, (int)(damage * DamageMultiplier), 1, Main.myPlayer, i, knockback.X, knockback.Y);
                    }
                }
            }
        }

        public override bool CheckDead()
        {
            if (AIState != ActionState.Dead)
            {
                if (SubState == ActionState.Desperation_Stunned || SubState == ActionState.HuskDrop_Stunned)
                {
                    NPC.velocity.Y = -20;
                    NPC.position.Y -= 50;
                }
                else
                    NPC.velocity.Y = 0;

                NPC.dontTakeDamage = true;
                NPC.life = 5;
                NPC.SuperArmor = true;
                AttackTimer = 0f;
                AIState = ActionState.Dead;
                return false;
            }

            //Spawn gores
            if (!Main.dedServ)
                DeathVisualEffects();
            
            WorldProgressionSystem.DefeatedCrabulon = true;
            WorldProgressionSystem.crabulonsDefeated++;
            return true;
        }

        public void DeathVisualEffects()
        {
            Gore segment;

            switch (chosenSkinIndices[0])
            {
                default:
                    segment = Gore.NewGoreDirect(NPC.GetSource_Death(), VisualCenter + new Vector2(35, -4).RotatedBy(visualRotation), Vector2.Zero, Mod.Find<ModGore>("CrabbyBodyGore2").Type, NPC.scale);
                    segment.rotation = visualRotation;
                    segment.velocity = Vector2.UnitX * 1.2f - Vector2.UnitY * 0.4f;

                    segment = Gore.NewGoreDirect(NPC.GetSource_Death(), VisualCenter + new Vector2(-55, -10).RotatedBy(visualRotation), Vector2.Zero, Mod.Find<ModGore>("CrabbyBodyGore1").Type, NPC.scale);
                    segment.rotation = visualRotation;
                    segment.velocity = -Vector2.UnitX * 1.2f - Vector2.UnitY * 0.4f;
                    break;
                case 1:
                    segment = Gore.NewGoreDirect(NPC.GetSource_Death(), VisualCenter + new Vector2(32, -5).RotatedBy(visualRotation), Vector2.Zero, Mod.Find<ModGore>("HairyBodyGore2").Type, NPC.scale);
                    segment.rotation = visualRotation;
                    segment.velocity = Vector2.UnitX * 1.2f - Vector2.UnitY * 0.4f;

                    segment = Gore.NewGoreDirect(NPC.GetSource_Death(), VisualCenter + new Vector2(-46, -6).RotatedBy(visualRotation), Vector2.Zero, Mod.Find<ModGore>("HairyBodyGore1").Type, NPC.scale);
                    segment.rotation = visualRotation;
                    segment.velocity = -Vector2.UnitX * 1.2f - Vector2.UnitY * 0.4f;
                    break;
                case 2:
                    segment = Gore.NewGoreDirect(NPC.GetSource_Death(), VisualCenter + new Vector2(-36, -22).RotatedBy(visualRotation), Vector2.Zero, Mod.Find<ModGore>("ShroomyBodyGore1").Type, NPC.scale);
                    segment.rotation = visualRotation;
                    segment.velocity = -Vector2.UnitX * 1.2f - Vector2.UnitY * 0.4f;

                    segment = Gore.NewGoreDirect(NPC.GetSource_Death(), VisualCenter + new Vector2(38, -22).RotatedBy(visualRotation), Vector2.Zero, Mod.Find<ModGore>("ShroomyBodyGore2").Type, NPC.scale);
                    segment.rotation = visualRotation;
                    segment.velocity = Vector2.UnitX * 1.2f - Vector2.UnitY * 0.4f;
                    break;

            }

            Particle limbGore = new CrabulonLimbsGoreSimulator(this);
            ParticleHandler.SpawnParticle(limbGore);

            for (int i = 0; i < 30; i++)
            {
                float smokeSize = Main.rand.NextFloat(3.5f, 3.6f);
                Vector2 smokeCenter = VisualCenter + Main.rand.NextVector2Circular(30f, 40f);

                Particle smoke = new SporeGas(smokeCenter, Main.rand.NextVector2Circular(2f, 2f) - Vector2.UnitY * 2f, VisualCenter, 522f, smokeSize, 0.01f);
                ParticleHandler.SpawnParticle(smoke);
            }

            for (int i = 0; i < 50; i++)
            {
                Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(230f, 70f) * NPC.scale;
                Vector2 dustVel = NPC.Center.SafeDirectionTo(dustPos) * Main.rand.NextFloat(0.5f, 1.6f) - Vector2.UnitY * Main.rand.NextFloat(0.5f, 2f);

                Dust d = Dust.NewDustPerfect(dustPos, DustID.MushroomSpray, dustVel);
                d.scale = Main.rand.NextFloat(0.8f, 1.2f);

                if (Main.rand.NextBool(5))
                    d.noGravity = false;
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (AIState != ActionState.Dead)
                return;
            modifiers.DisableCrit();
            modifiers.SetMaxDamage(1); //Superarmor should already prevent that , but just in case
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (AIState != ActionState.Dead)
                return;
            modifiers.DisableCrit();
            modifiers.SetMaxDamage(1); //Superarmor should already prevent that , but just in case
        }

        public float lastHitDirection = 0;

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            lastHitDirection = hit.HitDirection;

            if (AIState == ActionState.Dead)
            {
                if (!Main.dedServ)
                    DeadHitEffect();
                deathHitTimer = 1;
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            lastHitDirection = hit.HitDirection;
            if (AIState == ActionState.Dead)
            {
                if (!Main.dedServ)
                    DeadHitEffect();
                deathHitTimer = 1;
            }
        }
        #endregion

        #region Loot
        //public static int MaskType;
        public static int TrophyType;
        public static int RelicType;
        public static int BossBagType;
        public static AutoloadedBossBag TreasureBag;

        public static void CommonDrops(ILoot loot, bool bossBag = false)
        {
            if (!bossBag)
            {
                loot.Add(ItemDropRule.BossBag(BossBagType));
                loot.Add(TrophyType, 10);

                //MM Drops
                loot.Add(ItemDropRule.MasterModeCommonDrop(RelicType)); 
                loot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<ClawShell>(), 4));

                LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
                notExpertRule.Add(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<CrabulonBossMask>(), 7));

                //Weapons
                notExpertRule.Add(DropHelper.CalamityStyle(new Fraction(1, 4),
                    ModContent.ItemType<OvergrownClaw>(),
                    ModContent.ItemType<Sporethrower>(),
                    ModContent.ItemType<SyringeGun>(),
                    ModContent.ItemType<InkyBluePendant>()
                ));

                //Pickaxe
                notExpertRule.Add(ModContent.ItemType<MoldyPicklaw>(), 4);
                //Hook
                notExpertRule.Add(ModContent.ItemType<SerratedFiberCloak>(), 7);
                //Spore pods
                notExpertRule.Add(ModContent.ItemType<SporePod>(), 1, 10, 15);

                //Extra fun stuff
                notExpertRule.Add(ModContent.ItemType<FungalThroatInfection>(), 20);
                notExpertRule.Add(ModContent.ItemType<MushroomBoots>(), 20);
                notExpertRule.Add(ModContent.ItemType<TechwearSporeMask>(), 20);
                loot.Add(notExpertRule);

                DropOneByOne.Parameters dropParams = new();
                dropParams.MinimumItemDropsCount = 12;
                dropParams.MaximumItemDropsCount = 20;
                dropParams.ChanceNumerator = 1;
                dropParams.ChanceDenominator = 1;
                dropParams.MinimumStackPerChunkBase = 1;
                dropParams.MaximumStackPerChunkBase = 3;
                dropParams.BonusMinDropsPerChunkPerPlayer = 0;
                dropParams.BonusMaxDropsPerChunkPerPlayer = 0;

                loot.Add(new DropOneByOne(ItemID.GlowingMushroom, dropParams));


                dropParams.MinimumStackPerChunkBase = 3;
                dropParams.MaximumStackPerChunkBase = 5;
                loot.Add(new DropOneByOne(ModContent.ItemType<MyceliumMoldItem>(), dropParams));
            }

            else
            {
                loot.Add(ModContent.ItemType<LuminousMixture>(), 1); //Expert item
                loot.Add(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<CrabulonBossMask>(), 7));

                //Weapons
                loot.Add(DropHelper.CalamityStyle(new Fraction(1, 3),
                    ModContent.ItemType<OvergrownClaw>(),
                    ModContent.ItemType<Sporethrower>(),
                    ModContent.ItemType<SyringeGun>(),
                    ModContent.ItemType<InkyBluePendant>()
                ));

                //Pickaxe
                loot.Add(ModContent.ItemType<MoldyPicklaw>(), 3);
                //Hook
                loot.Add(ModContent.ItemType<SerratedFiberCloak>(), 4);
                //Spore pods
                loot.Add(ModContent.ItemType<SporePod>(), 1, 10, 15);

                //Extra fun stuff
                loot.Add(ModContent.ItemType<FungalThroatInfection>(), 10);
                loot.Add(ModContent.ItemType<MushroomBoots>(), 9);
                loot.Add(ModContent.ItemType<TechwearSporeMask>(), 5);
            }

        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) => CommonDrops(npcLoot);
        #endregion
    }
}
