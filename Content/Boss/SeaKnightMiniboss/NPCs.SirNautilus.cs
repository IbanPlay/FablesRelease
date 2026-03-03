using CalamityFables.Content.Items.SirNautilusDrops;
using CalamityFables.Content.Projectiles;
using CalamityFables.Content.Tiles.Graves;
using CalamityFables.Content.Tiles.MusicBox;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;
using Terraria.Utilities;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public partial class SirNautilus : ModNPC
    {
        #region Sounds

        //Generic stuff
        public static readonly SoundStyle HitSound = new(SoundDirectory.Nautilus + "NautilusHit", 6);
        public static readonly SoundStyle DeathSound = new(SoundDirectory.Nautilus + "NautilusBreak");

        public static readonly SoundStyle TalkSound = new("CalamityFables/Sounds/CuteSpeech/NautilusNormal/NautilusVoiceMid", 20);
        public static readonly SoundStyle TalkEndSound = new("CalamityFables/Sounds/CuteSpeech/NautilusNormal/NautilusVoiceEnd", 8);

        public static readonly VoiceByte RegularSpeech = new(TalkSound, TalkEndSound);


        //public static readonly SoundStyle WeirdTalkSound = new("CalamityFables/Sounds/CuteSpeech/NautilusWeird/NautilusSpeechMid-", 30);
        //public static readonly SoundStyle WeirdTalkEndSound = new("CalamityFables/Sounds/CuteSpeech/NautilusWeird/NautilusSpeechEnd-", 10);
        //public static readonly VoiceByte WeirdSpeech = new(WeirdTalkSound, WeirdTalkEndSound);



        public static CurveSegment Stay => new(LinearEasing, 0f, 0f, -0.3f);
        public static CurveSegment Rise => new(LinearEasing, 0.51f, -0.3f, 1.3f);
        public float RegularTone(float progress) => PiecewiseAnimation(progress, new CurveSegment[] { Stay, Rise });


        //Signathion
        public static readonly SoundStyle SignathionHitSound = new(SoundDirectory.Nautilus + "SignathionHit", 3);
        public static readonly SoundStyle SignathionGoingGhost = new(SoundDirectory.Nautilus + "SignathionGhostMode");

        public static readonly SoundStyle SignathionStep = new(SoundDirectory.Nautilus + "SignathionStep", 4) { Volume = 0.6f };
        public static readonly SoundStyle SignathionScream = new(SoundDirectory.Nautilus + "SignathionScream", 3);
        public static readonly SoundStyle SignathionScreamShort = new(SoundDirectory.Nautilus + "SignathionScreamShort", 3);

        public static readonly SoundStyle SignathionWaterRifleCharge = new(SoundDirectory.Nautilus + "SignathionWaterRifleCharge");
        public static readonly SoundStyle SignathionWaterShotgunCharge = new(SoundDirectory.Nautilus + "SignathionWaterShotgunCharge");
        public static readonly SoundStyle SignathionWaterRifle = new(SoundDirectory.Nautilus + "SignathionWaterRifle");
        public static readonly SoundStyle SignathionWaterShotgun = new(SoundDirectory.Nautilus + "SignathionWaterShotgun");

        public static readonly SoundStyle SignathionHeavyStomp = new(SoundDirectory.Nautilus + "SignathionHeavyStomp", 2);

        public static readonly SoundStyle SpectralWaterSizzle = new(SoundDirectory.Nautilus + "SpectralWaterSizzle");

        public static readonly SoundStyle SignathionSpawnRoar = new(SoundDirectory.Nautilus + "SignathionRoar");
        public static readonly SoundStyle SignathionAppearSizzle = new(SoundDirectory.Nautilus + "SignathionAppear");
        public static readonly SoundStyle SignathionDisappearSizzle = new(SoundDirectory.Nautilus + "SignathionDisappear");

        //Nautilus attacks
        public static readonly SoundStyle TridentThrow = new(SoundDirectory.Nautilus + "NautilusTridentThrow", 2);
        public static readonly SoundStyle TridentHit = new(SoundDirectory.Nautilus + "NautilusTridentHit", 2);
        public static readonly SoundStyle TridentSwing = new(SoundDirectory.Nautilus + "NautilusSwing1") { Volume = 0.8f, PitchVariance = 0.2f };

        public static readonly SoundStyle CycloneStart = new(SoundDirectory.Nautilus + "NautilusCycloneReady");
        public static readonly SoundStyle CycloneCharge = new(SoundDirectory.Nautilus + "NautilusCycloneDash");

        //Nauties death
        public static readonly SoundStyle RattlingBonesSound = new(SoundDirectory.Nautilus + "NautilusRattlingBones");
        public static readonly SoundStyle BonesReformSound = new(SoundDirectory.Nautilus + "NautilusBonesReform");
        public static readonly SoundStyle BonesSutureSound = new(SoundDirectory.Nautilus + "NautilusBonesSuture");

        public static readonly SoundStyle LegoDeathSound = new(SoundDirectory.Nautilus + "LegoDie");
        public static readonly SoundStyle LegoBuildSound = new(SoundDirectory.Nautilus + "LegoBuild");
        public static readonly SoundStyle LegoBuildDoneSound = new(SoundDirectory.Nautilus + "LegoBuildDone");
        #endregion

        private Player Target => NPC.target == -1 ? null : Main.player[NPC.target];
        //private static float DifficultyScale => CalamityWorld.death ? 3 : CalamityWorld.revenge ? 2 : Main.expertMode ? 1 : 0;

        private static float DifficultyScale => Main.masterMode ? 3 : Main.expertMode ? 2 : 0;


        public static int SignathionNormalLifeMax = 1100;
        public static int NautilusNormalLifeMax = 1000;
        public static int SignathionExpertLifeMax = 1500;
        public static int NautilusExpertLifeMax = 1300;
        public static int SignathionMasterLifeMax = 1700;
        public static int NautilusMasterLifeMax = 1500;

        public static int SignathionBaseLifeMax => Main.masterMode ? SignathionMasterLifeMax : Main.expertMode ? SignathionExpertLifeMax : SignathionNormalLifeMax; 
        public static int NautilusBaseLifeMax => Main.masterMode ? NautilusMasterLifeMax : Main.expertMode ? NautilusExpertLifeMax : NautilusNormalLifeMax;


        public static int BaseLifeMax => SignathionBaseLifeMax + NautilusBaseLifeMax;


        public float OneVOneLifePercent => NautilusBaseLifeMax / (float)BaseLifeMax;
        public bool IsSignathionPresent => (OneVOneLifePercent < NPC.life / (float)NPC.lifeMax) || ((float)AIState > 0 && (float)AIState <= 40);
        public bool ShouldSignathionBeThere => OneVOneLifePercent < NPC.life / (float)NPC.lifeMax;

        #region AI states
        //Every attack is spread out by 10, with attack "subparts" being inbetween
        public enum ActionState
        {
            SlowWalk, //Nautilus slowly walks towards the player by default
            SlowWalkAway, //Backs off from the player if theyre too close
            SlowWalkStayPut, //Signathion exclusive
            SlowWalkForward, //Signathion exclusive
            FastWalkAway, //Signathion exclusive

            //Cnidrion-mounted attacks
            TailSwipe = 10,//Cnidrion tries to sweep its tail at the player if theyre too close
            TailSwipe_Telegraph,
            TailSwipe_Swipe,
            TailSwipe_Recovery,

            SpecterBolts = 20, //Cnidrion readies up and fires spectral bolts at the player if they're too far away
            SpecterBolts_Chargeup,
            SpecterBolts_FireBlasts,
            SpecterBolts_FireShotgun,
            SpecterBolts_Recovery,

            Rockfall = 30, //Cnidrion crouches and stomps the ground repeatedly, making rocks fall from the ceiling
            Rockfall_RepeatedStomps,

            Charge = 40, //Cnidrion charges towards the player. Nautilus may throw his trident at the player during the attack
            Charge_GetReady,
            Charge_Run,
            Charge_UpSlice,
            Charge_TridentThrow,
            Charge_Recovery,

            //Unmounted attacks
            TridentThrow = 50, //Nautilus throws his trident at the playeror at the ceiling, choosing based on the distance to the player
            TridentThrow_AtPlayer,
            TridentThrow_AtCeiling,
            TridentThrow_RecoveryAnim,

            DoubleSwipe = 60, //Nautilus does a wide downwards slice after dashing right in front of the player
            DoubleSwipe_CloseDistance1, //On death, nautilus doesn't even back away for the first half of the dash telegraph
            DoubleSwipe_FirstSwipe,
            DoubleSwipe_CloseDistance2, //On rev+, nautilus follows up with a second longer swing
            DoubleSwipe_SecondSwipe,

            JumpSlam = 70, //Nautilus jumps in the air spinning, then comes down at the player in a downwards dash
            JumpSlam_Jumping,
            JumpSlam_Holding, //In expert+, nautilus can transition to a trident throw attack if the player jumps above it
            JumpSlam_Diving,
            JumpSlam_Recovery,

            TridentSpin = 80, //Nautilus spins his trident forward as he dashes in front. Attack always gets followed by either variant of the trident throw
            TridentSpin_Windup,
            TridentSpin_DashStart,
            TridentSpin_Recovery,
            TridentSpin_WaitingAtTheStartOfTheAttackForThePlayerToHitTheFloor,

            CutsceneFightStart = 90,
            CutsceneFightStart_InitialPose,
            CutsceneFightStart_JumpOnSignathion,
            CutsceneFightStart_SignathionScream,

            CutsceneDismountSig = 100,
            CutsceneDismountSig_InitialWait,
            CutsceneDismountSig_NautilusJumpingOff,
            CutsceneDismountSig_NautilusEnGuarde,

            CutsceneDeath = 110,
            CutsceneDeath_WaitingForReformation,
            CutsceneDeath_ShakingBones,
            CutsceneDeath_ReformBones
        }

        public ActionState AIState {
            get => (ActionState)(NPC.ai[0] - (NPC.ai[0] % 10));
            set => NPC.ai[0] = (float)value;
        }

        public ActionState SubState {
            get => (ActionState)NPC.ai[0];
            set => NPC.ai[0] = (float)value;
        }

        public ActionState PreviousState {
            get => (ActionState)(NPC.localAI[0] - (NPC.localAI[0] % 10));
            set => NPC.localAI[0] = (float)value;
        }

        public bool InACutscene => NPC.ai[0] >= 90;
        #endregion

        public ref float AttackTimer => ref NPC.ai[1];
        public ref float Stamina => ref NPC.ai[2]; //Nautilus looses stamina after attacks. At zero stamina, he is forced back into his slow walk state until it recharges
        public float Patience { get => NPC.ai[3]; set => NPC.ai[3] = Math.Clamp(value, 0f, 1f); } //Nautilus grows impatient with the player if they keep running away
        public ref float UnboundedPatience_PhaseTimer => ref NPC.ai[3];

        public ref float ExtraMemory => ref NPC.localAI[1];
        public ref float PreviousAttackVariant => ref NPC.localAI[2];

        private Vector2 movementTarget = Vector2.Zero;
        private Vector2 oldPosition = Vector2.Zero;

        #region damage valuezz

        public static int SpecterBolts_DirectDamage = 20;
        public static float SpecterBolts_PuddleDamageReduction = 0.75f;

        public static int SpecterBolts_PuddleTime => (int)DifficultyScale * 20;
        public static int SpecterBolts_ShotgunPuddleTime => (int)((DifficultyScale * 0.4f + 1.2f) * 60);

        public static int Rockfall_RockDamage = 20;

        public static int Charge_TridentThrowDamage = 18;
        public static int Charge_ComboThrowDamage = 15;


        public static int TridentThrow_DirectDamage = 18;
        public static int TridentThrow_BoulderDamage = 24;

        public static int DoubleSwipe_FirstHitDamage = 16;
        public static int DoubleSwipe_SecondHitDamage = 18;

        public static int JumpSlam_SlamDamage = 24;

        public static int TridentSpin_DashDamage = 20;
        #endregion

        #region Constants and Variables
        public float StaminaRechargeRate {
            get {
                if (Main.getGoodWorld)
                    return 1f; //Instantly recharges in FTW (aka he does an uninterrupted combo always

                return 1f / (60f * (2f - DifficultyScale * 0.5f)); // 2 seconds in normal, 1.5 in expert, 1 in rev, 0.5 in death
            }
        }

        public static float MinimumImpatienceRange = 16 * 22;
        public static float MaxImpatienceRange = 16 * 40;

        public static float ImpatienceIncreaseRate = 1 / (60f * 2.5f); //Takes 2.5 seconds at full distance to build up entirely
        public static float ImpatienceDiminutionRate = 1 / (60f * 1f); //Takes 1 seconds to get freed of all impatience
        public float ImpatienceBuildupRate {
            get {
                MinimumImpatienceRange = 16 * 32;
                MaxImpatienceRange = 16 * 40;
                ImpatienceIncreaseRate = 1 / (60f * 5.5f);
                ImpatienceDiminutionRate = 1 / (60f * 3f);


                if (Target == null)
                    return 0;

                float distanceToTarget = (NPC.Center - Target.Center).Length();

                //Loose impatience if close to the player
                if (distanceToTarget < MinimumImpatienceRange)
                    return -1 * ImpatienceDiminutionRate;

                //Impatience builds up twice as fast in FTW, but half as fast in normal.
                float impatienceMultiplier = Main.getGoodWorld ? 2f : !Main.expertMode ? 0.5f : 1f;
                return ImpatienceIncreaseRate * Utils.GetLerpValue(MinimumImpatienceRange, MaxImpatienceRange, distanceToTarget, true) * impatienceMultiplier;
            }
        }

        public static Vector2 P1Size => new Vector2(140, 110);
        public static Vector2 P2Size => new Vector2(40, 48);



        public static float SignathionHeightAtWhichYouGoGhostModeMin = 135;
        public static float SignathionHeightAtWhichYouGoGhostModeMax = 400;
        public float SignathionImmunityPhasingCompletion => MathHelper.Clamp(UnboundedPatience_PhaseTimer - 1, 0, 1);
        #endregion

        public override void Load()
        {
            On_NPC.UpdateCollision += HijackCollisionCode;
            AutoloadCommonBossDrops(Name, "Sir Nautilus", AssetDirectory.SirNautilusDrops, out MaskType, out TrophyType, out RelicType, out BossBagType, out TreasureBag, true);
            SoundHandler.ModifyMusicChoiceEvent += ManageMusicFade;
            FablesGeneralSystemHooks.LogBossChecklistEvent += AddToChecklist;
            FablesGeneralSystemHooks.PostUpdateEverythingEvent += UpdateSignathionLightInfluence;

            phase1name = Mod.GetLocalization("Extras.SirNautilusPhase1Name");
        }

        private void AddToChecklist(Mod bossChecklist)
        {
            var collectibleDrops = new List<int>()
                {
                    RelicType, TrophyType, MaskType, ModContent.ItemType<SirNautilusMusicBoxItem>(), ModContent.ItemType<CoralCage>()
                    , ModContent.ItemType<DuatsFavor>(), ModContent.ItemType<FossilizedLarynx>()
                };

            bossChecklist.Call("LogBoss", Mod, nameof(SirNautilus),
                1.9f, //Right before eye
                () => SirNautilusDialogue.DefeatedNautilus,
                Type,
                new Dictionary<string, object>()
                {
                    ["spawnInfo"] = LocalizationRoundabout.DefaultText("Compat.BossChecklist.Nautilus.SpawnInfo", "Meet the lone soldier in a hidden chamber buried beneath the dunes... an ancient [i:{0}] graveyard marks the spot!")
                    .WithFormatArgs(SandstoneGrave.ItemInstances[0].Type),
                    ["despawnMessage"] = LocalizationRoundabout.DefaultText("Compat.BossChecklist.Nautilus.Despawn", "Nautilus wins the duel!"),
                    ["collectibles"] = collectibleDrops,
                    ["customPortrait"] = DrawBossChecklistPortrait
                });
        }

        public override void Unload()
        {
            On_NPC.UpdateCollision -= HijackCollisionCode;
        }

        private void HijackCollisionCode(On_NPC.orig_UpdateCollision orig, NPC self)
        {
            if (self.type == ModContent.NPCType<SirNautilus>())
            {
                SirNautilus myself = self.ModNPC as SirNautilus;

                if (myself.IsSignathionPresent)
                {
                    myself.SignathionCollision();
                    return;
                }
            }
            orig(self);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sir Nautilus");
            TreasureBag.NPCType = Type;
            TreasureBag.bossLoot = CommonDrops;

            Main.npcFrameCount[NPC.type] = 11;
            FablesSets.NoJourneyStengthScaling[Type] = true; //Otherwise it recieves double stat scaling in journey mode

            NPCID.Sets.TeleportationImmune[Type] = true;

            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                SpriteDirection = 1
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;

            if (!Main.dedServ)
            {
                signathionMusic = MusicLoader.GetMusicSlot(Mod, "Sounds/Music/Signathion");
                nautilusMusic = MusicLoader.GetMusicSlot(Mod, "Sounds/Music/Nautilus");
                secondPhaseMusicLayer = MusicLayers.LoadMusicSync(signathionMusic, nautilusMusic);
            }

            //Register nautilus to be undead for safekeeper's rings purpose
            if (CalamityFables.SpiritEnabled)
                CalamityFables.SpiritReforged.Call("AddUndead", Type);
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 100f; //Blocks most other spawns

            NPC.aiStyle = -1;
            NPC.width = (int)P1Size.X;
            NPC.height = (int)P1Size.Y;

            NPC.damage = 20;
            NPC.lifeMax = BaseLifeMax;
            NPC.defense = 6;

            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.waterMovementSpeed = 1f;
            //Should he be netalways? Maybe he should just so he can spawn his passive version when players are too far
            NPC.netAlways = true;
            NPC.boss = true; //Listed as a boss toa void despawn

            Music = MusicLoader.GetMusicSlot(Mod, "Sounds/Music/Signathion");

            NPC.value = Item.buyPrice(0, 0, 25, 0);
            NPC.rarity = 2; //Bestiary thing except its confusing

            NPC.HitSound = null;
            NPC.DeathSound = DeathSound;

            AttackTimer = 1;
            Stamina = 0f;
            SignathionFadeOpacity = 0f;
            if (NPC.IsABestiaryIconDummy)
                SignathionFadeOpacity = 1.5f;

            NPC.hide = false;
            NPC.dontTakeDamage = true;
            movementTarget = NPC.Center;
            oldPosition = NPC.Center;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.lifeMax = BaseLifeMax;
            NPC.lifeMax = (int)(NPC.lifeMax * balance);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(NPC.GetBestiaryCreditId(), true);
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundDesert,

                // Will move to localization whenever that is cleaned up.
                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.SirNautilus")
            });
        }

        public override void OnSpawn(IEntitySource source)
        {
            SirNautilusDialogue.LogDuelStart();
            NPC.TargetClosest();
            AIState = ActionState.CutsceneFightStart;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((float)PreviousState);
            writer.Write(ExtraMemory);
            writer.Write(PreviousAttackVariant);
            writer.WriteVector2(oldPosition);
            writer.WriteVector2(movementTarget);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            PreviousState = (ActionState)reader.ReadSingle();
            ExtraMemory = reader.ReadSingle();
            PreviousAttackVariant = reader.ReadSingle();
            oldPosition = reader.ReadVector2();
            movementTarget = reader.ReadVector2();
        }

        public override bool PreAI()
        {
            NPC.netOffset = Vector2.Zero;
            return base.PreAI();
        }

        public static LocalizedText phase1name;
        public override void ModifyTypeName(ref string typeName)
        {
            if (IsSignathionPresent)
                typeName = phase1name.Value;
            else
                typeName = DisplayName.Value;
        }

        public override void AI()
        {
            //Visuals for ghost fuzzy water
            if (NPC.hide && !InACutscene)
            {
                if (IsSignathionPresent)
                {
                    Vector2 dustPosition = NPC.Left + Vector2.UnitX * (NPC.width * 0.5f * Main.rand.NextFloat());
                    if (NPC.spriteDirection == -1)
                        dustPosition += Vector2.UnitX * NPC.width * 0.5f;

                    Dust cust = Dust.NewDustPerfect(dustPosition, ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, 3f);

                    cust.noGravity = true;
                    cust.velocity *= 0.5f;
                    cust.velocity -= Vector2.UnitY * 4f;
                    cust.velocity += Main.rand.NextVector2Circular(2f, 2f);

                    cust.rotation = Main.rand.NextFloat(0.5f, 3f);
                }

                else if (!Main.rand.NextBool(3))
                {
                    Vector2 dustPosition = NPC.Left + Vector2.UnitX * (NPC.width * 0.15f + NPC.width * 0.7f * Main.rand.NextFloat());
                    Dust cust = Dust.NewDustPerfect(dustPosition, ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, 3f);

                    cust.noGravity = true;
                    cust.velocity *= 0.5f;
                    cust.velocity -= Vector2.UnitY * 2f;
                    cust.velocity += Main.rand.NextVector2Circular(2f, 2f);

                    cust.rotation = Main.rand.NextFloat(0.3f, 2f);

                }
            }

            NPC.spriteDirection = (NPC.direction > 0) ? -1 : 1;

            if (!ShouldSignathionBeThere && NPC.Size == P1Size && AIState == ActionState.SlowWalk)
            {
                AttackTimer = 1f;
                SubState = ActionState.CutsceneDismountSig;
            }

            if (InACutscene)
            {
                if (AIState == ActionState.CutsceneFightStart)
                    SpawnAnimation();

                if (AIState == ActionState.CutsceneDismountSig)
                    DismountSignathion();

                if (AIState == ActionState.CutsceneDeath)
                    DeathBehavior();

                return;
            }

            //Despawn
            if (Target.dead || !Target.active || NPC.Distance(Target.Center) > 160 * 16)
            {
                NPC.TargetClosest(false);

                if (Target.dead || !Target.active || NPC.Distance(Target.Center) > 160 * 16)
                {
                    foreach (var item in TileEntity.ByID)
                    {
                        if (item.Value.type == ModContent.TileEntityType<TESirNautilusSpawner>())
                        {
                            NPC.Center = item.Value.Position.ToWorldCoordinates();
                            break;
                        }
                    }

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

                    if (Main.LocalPlayer.Distance(NPC.Center) < 1000)
                        CameraManager.Shake += 10;

                    SoundEngine.PlaySound(BonesSutureSound, NPC.Center);
                    NPC.active = false;
                    return;
                }
            }

            #region Impatience and deerclops-like invincibility
            if (Target != null)
            {
                //Manage impatience
                if (!IsSignathionPresent)
                    Patience += ImpatienceBuildupRate;

                else
                {
                    //Lower your aggression if the target is close enough
                    if (Target.Center.Y >= NPC.Center.Y - SignathionHeightAtWhichYouGoGhostModeMin)
                        UnboundedPatience_PhaseTimer -= 0.014f + 0.1f * Utils.GetLerpValue(SignathionHeightAtWhichYouGoGhostModeMin, 0f, NPC.Center.Y - Target.Center.Y, true);

                    else
                    {
                        UnboundedPatience_PhaseTimer += (1 / (60f * 1f)) * (float)Math.Pow(Utils.GetLerpValue(SignathionHeightAtWhichYouGoGhostModeMin, SignathionHeightAtWhichYouGoGhostModeMax, NPC.Center.Y - Target.Center.Y, true), 1.6f);

                        if (UnboundedPatience_PhaseTimer < 1.3f && Target.Center.Y < NPC.Center.Y - SignathionHeightAtWhichYouGoGhostModeMax)
                            UnboundedPatience_PhaseTimer = 1.3f;

                        if (UnboundedPatience_PhaseTimer > 2)
                            UnboundedPatience_PhaseTimer = 2;
                    }
                }
            }

            if (!NPC.dontTakeDamage && UnboundedPatience_PhaseTimer > 1)
            {
                NPC.dontTakeDamage = true;

                SoundEngine.PlaySound(SignathionGoingGhost with { Volume = SignathionGoingGhost.Volume * 0.7f }, NPC.Center);
                if (Main.LocalPlayer.Distance(NPC.Center) < 1000f)
                    CameraManager.Quake += 12;

                for (int i = 0; i < 23; i++)
                {
                    Dust cust = Dust.NewDustDirect(NPC.BottomLeft + NPC.width * 0.15f * Vector2.UnitX, (int)(NPC.width * 0.7f), 5, ModContent.DustType<SpectralWaterDustNoisy>(), 0f, 0f, 100, default, Main.rand.NextFloat(1f, 3f));
                    cust.noGravity = true;
                    cust.velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 3f);
                    cust.rotation = Main.rand.NextFloat(0.8f, 1.4f);
                }
            }

            if (NPC.dontTakeDamage && UnboundedPatience_PhaseTimer <= 1)
            {
                NPC.dontTakeDamage = false;
                UnboundedPatience_PhaseTimer -= 0.1f;
            }

            InvincibilitySpectreVisualsStrenght = MathHelper.Lerp(InvincibilitySpectreVisualsStrenght, SignathionImmunityPhasingCompletion, 0.3f);
            #endregion

            //Stamina recovery
            if (AIState == ActionState.SlowWalk && Stamina <= 0)
            {
                Stamina -= StaminaRechargeRate;
                if (Stamina <= -1)
                    Stamina = 1;
            }

            if (AttackTimer <= 0)
            {
                if (AIState != ActionState.SlowWalk)
                {
                    AttackTimer = 1;
                    //Make sure attacks that "ate away" into negative stamina are set back to zero
                    if (Stamina < 0)
                        Stamina = 0;

                    bool wasSighathionPresent = IsSignathionPresent;
                    AIState = ActionState.SlowWalk;

                    //If we went from riding sig to not riding sig, go straight into the cutscene
                    if (!IsSignathionPresent && wasSighathionPresent)
                    {
                        AIState = ActionState.CutsceneDismountSig;
                        DismountSignathion();
                        return;
                    }
                }


                else if (Stamina > 0 && Target != null && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    WeightedRandom<ActionState> attackPool = new WeightedRandom<ActionState>(Main.rand);

                    float distanceToTarget = NPC.Distance(Target.Center);
                    if (IsSignathionPresent)
                    {
                        if (distanceToTarget < 180 && PreviousState != ActionState.TailSwipe) //Signathion can't do two tail swipes in a row, but if the player is close enough he gets a higher chance to do it
                            attackPool.Add(ActionState.TailSwipe, 2.5f);

                        if (distanceToTarget > 60)
                            attackPool.Add(ActionState.SpecterBolts, 1.5f);

                        if (PreviousState != ActionState.Rockfall) //Signathion is more likely to use the rockfall attack if the player above it
                            attackPool.Add(ActionState.Rockfall, 0.45f + (Target.Bottom.Y < NPC.Top.Y - 50 ? 0.7f : 0f));

                        attackPool.Add(ActionState.Charge, 1.3f);
                    }

                    else //P2 attacks
                    {
                        if (distanceToTarget < 500)
                            attackPool.Add(ActionState.DoubleSwipe, 0.3f + 0.7f * Utils.GetLerpValue(500, 0, distanceToTarget, true));
                        if (distanceToTarget > 160)
                            attackPool.Add(ActionState.JumpSlam, 0.3f + 0.7f * (float)Math.Pow(Utils.GetLerpValue(160, 1200, distanceToTarget, true), 0.74f));

                        //Trident throw gets a lowered chance to get picked if the last attack is the trident spin, because trident spin is always followed up with a trident throw
                        attackPool.Add(ActionState.TridentThrow, PreviousState == ActionState.TridentSpin ? 0.1f : 1f);

                        if (distanceToTarget > 40)
                            attackPool.Add(ActionState.TridentSpin, Target.velocity.Y != 0 ? 1f : 0.6f);
                    }

                    ActionState potentialNewState = ActionState.SlowWalk;
                    for (int i = 0; i < attackPool.elements.Count; i++)
                    {
                        if (attackPool.elements[i].Item1 == PreviousState)
                            attackPool.elements[i] = new Tuple<ActionState, double>(PreviousState, attackPool.elements[i].Item2 * 0.1f);
                    }

                    if (attackPool.elements.Count > 0)
                        potentialNewState = attackPool.Get();

                    AIState = potentialNewState;
                    if (potentialNewState != ActionState.SlowWalk)
                    {
                        PreviousState = potentialNewState;

                        if (IsSignathionPresent)
                        {
                            SignathionCollision();
                        }
                    }
                    NPC.netUpdate = true;
                    NPC.netSpam = 0;

                    AttackTimer = 1;
                }
            }


            if (AIState == ActionState.SlowWalk)
            {
                if (IsSignathionPresent)
                    SignathionMovement();
                else
                    WalkBehavior();
            }
            else if (AIState != ActionState.TridentThrow)
                tridentReapparitionTimer = 0;


            //P1
            if (AIState == ActionState.TailSwipe)
                SignathionTailSwipe();
            if (AIState == ActionState.SpecterBolts)
                SignathionSpecterBolts();
            if (AIState == ActionState.Rockfall)
                SignathionRockfallAttack();
            if (AIState == ActionState.Charge)
                SignathionChargeAttack();

            //P2
            if (AIState == ActionState.TridentThrow)
                TridentThrowAttack();
            if (AIState == ActionState.DoubleSwipe)
                DoubleSlashAttack();
            if (AIState == ActionState.JumpSlam)
                JumpSlamAttack();
            if (AIState == ActionState.TridentSpin)
                TridentSpin();


            if (Main.netMode != NetmodeID.MultiplayerClient)
                CustomHurtbox();

            if (!Main.dedServ)
            {
                UpdateCaches();
                UpdateTrails();
                if (!IsSignathionPresent)
                    SpawnAmbienceParticles();

                if (SubState == ActionState.TridentSpin_Recovery)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        int index = Main.rand.Next(30, cycloneDashPoints.Count - 1);
                        Vector2 point = cycloneDashPoints[index];
                        Vector2 nextPoint = cycloneDashPoints[index + 1];

                        Dust zust = Dust.NewDustPerfect(point + Main.rand.NextVector2Circular(1f, 1f), 43, point.DirectionTo(nextPoint) * -0.1f, 200, !Main.rand.NextBool(3) ? Color.Yellow : Color.Gold, Main.rand.NextFloat(0.5f, 1f));
                        zust.noGravity = true;
                    }
                }
            }


            NPC.netUpdate = true;

            //YHARIM
            if (false && (AIState == ActionState.SlowWalk ||
                SubState == ActionState.Charge_Recovery ||
                SubState == ActionState.JumpSlam_Recovery ||
                SubState == ActionState.SpecterBolts_Recovery ||
                SubState == ActionState.TailSwipe_Recovery ||
                SubState == ActionState.TridentSpin_Recovery ||
                SubState == ActionState.TridentThrow_RecoveryAnim))
            {
                Stamina = 1;
                AttackTimer = 0;

                if (SubState == ActionState.TridentSpin_Recovery && Main.rand.NextBool())
                    AttackTimer += 0.001f;
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            if ((AIState == ActionState.JumpSlam && SubState != ActionState.JumpSlam_Recovery) ||
                SubState == ActionState.TridentSpin_DashStart || SubState == ActionState.TridentSpin_Recovery)
                return base.CanHitPlayer(target, ref cooldownSlot);

            //Crop cnid's hitbox to be only his legs & tail
            else if (SubState == ActionState.TailSwipe_Swipe)
            {
                float heightReduction = 0.5f;
                if (Collision.CheckAABBvAABBCollision(target.position, target.Size, NPC.position + Vector2.UnitY * NPC.height * heightReduction, new Vector2(NPC.width, NPC.height * (1 - heightReduction))))
                    return base.CanHitPlayer(target, ref cooldownSlot);

                return false;
            }

            else if (SubState == ActionState.Charge_Run)
            {
                float heightReduction = 0.3f;
                float tailReduction = 0.3f;
                Vector2 origin = NPC.position + Vector2.UnitY * NPC.height * heightReduction;

                if (NPC.direction > 0)
                    origin += Vector2.UnitX * NPC.width * heightReduction;

                if (Collision.CheckAABBvAABBCollision(target.position, target.Size, origin, new Vector2(NPC.width * (1 - tailReduction), NPC.height * (1 - heightReduction))))
                    return base.CanHitPlayer(target, ref cooldownSlot);

                return false;
            }

            return false;
        }

        public void CustomHurtbox()
        {
            //Check for slash stuff
            if (ActiveSlash > 0)
            {
                List<Vector2> slashPoints = GetSlashPoints(26);

                int end = slashPoints.Count;
                if (ActiveSlash == 2)
                {
                    end -= (int)(10 * Math.Min(1f, ActiveSlashCompletion));
                }

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player potentiallyHurtPlayer = Main.player[i];
                    //First off , check if the player has even a remote chance of being hittable
                    if (potentiallyHurtPlayer.active && !potentiallyHurtPlayer.dead && NPC.Distance(potentiallyHurtPlayer.Center) < DoubleSwipe_CurrentSwingReach * 2f)
                    {
                        bool playerHit = false;
                        float collisionPoint = 0f;

                        for (int j = 0; j < end; j++)
                        {
                            if (Collision.CheckAABBvLineCollision(potentiallyHurtPlayer.position, potentiallyHurtPlayer.Size, NPC.Center, slashPoints[j], 6f, ref collisionPoint))
                            {
                                playerHit = true;
                                break;
                            }
                        }

                        if (playerHit)
                        {
                            int swingDamage = CurrentSlashVariant == 1 ? DoubleSwipe_FirstHitDamage : DoubleSwipe_SecondHitDamage;
                            Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), potentiallyHurtPlayer.Center + Vector2.UnitX * (NPC.Center.X - potentiallyHurtPlayer.Center.X).NonZeroSign(), Vector2.Zero, ModContent.ProjectileType<HostileDirectStrike>(), swingDamage / 2, 1, Main.myPlayer, i);
                        }
                    }
                }
            }
        }

        public override bool? CanFallThroughPlatforms()
        {
            if (SubState == ActionState.JumpSlam_Diving)
                return !(Target != null && NPC.Bottom.Y >= Target.Hitbox.Center.Y);

            //Only accept platforms if youre at the same height or below the player. Prevents him from getting stuck above the player when on a platform
            if (Target != null && NPC.Bottom.Y >= Target.Hitbox.Top)
                return false;
            return true;
        }

        public override bool CheckDead()
        {
            SirNautilusDialogue.RegisterNautilusDefeat();

            //Ghost nautilus
            if (NPC.hide)
            {
                for (int i = 0; i < 53; i++)
                {
                    Dust cust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(10f, 10f), ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, Main.rand.NextFloat(1.2f, 1.5f));
                    cust.noGravity = false;

                    Vector2 dustVelocity = NPC.velocity.SafeNormalize(NPC.DirectionFrom(Main.LocalPlayer.Center)) * MathHelper.Clamp(NPC.velocity.Length(), 0.5f, 2f);
                    dustVelocity = dustVelocity.RotatedByRandom(MathHelper.PiOver4 * 0.5f) * Main.rand.NextFloat(2.4f, 3.9f);

                    cust.velocity = dustVelocity + Main.rand.NextVector2Circular(1.3f, 1.3f) - Vector2.UnitY * 2f;
                    cust.rotation = Main.rand.NextFloat(1f, 3f);

                    cust.customData = new Vector3(255, 0, 0);
                }

                return true;
            }

            if (AIState == ActionState.CutsceneDeath && (int)SubState > (int)ActionState.CutsceneDeath_WaitingForReformation)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int nautie = NPC.NewNPC(NPC.GetSource_Death(), (int)NPC.Center.X - 8, (int)NPC.Center.Y, ModContent.NPCType<SirNautilusPassive>());
                    Main.npc[nautie].netUpdate = true;
                }
                return true;
            }

            RagdollingBones.Clear();
            AIState = ActionState.CutsceneDeath;
            NPC.dontTakeDamage = true;
            NPC.life = 1;
            return false;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.soundDelay == 0)
            {
                NPC.soundDelay = Main.rand.Next(10, 26);
                SoundEngine.PlaySound(IsSignathionPresent ? SignathionHitSound : HitSound, NPC.Center);
            }
        }

        float DamageResistanceFromPhaseTransition => IsSignathionPresent && !ShouldSignathionBeThere ? 0.1f : 1f;
        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= DamageResistanceFromPhaseTransition;
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= DamageResistanceFromPhaseTransition;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (SubState == ActionState.Charge_Run)
            {
                yFrame = 0;

                if (NPC.velocity.Y < -2)
                    NPC.velocity.Y = -2;

                target.velocity.Y = -13;
                SubState = ActionState.Charge_UpSlice;
                AttackTimer = 1f;
                ExtraMemory = 0.1f; //Extramemory is used by the code to determine how much of an extra cooldown does the trident throw get (avoids iframes)
            }

            if (SubState == ActionState.TailSwipe_Swipe)
            {
                target.velocity.Y = -7;
                target.velocity.X = NPC.direction * 10f;
            }
        }

        public override bool CheckActive()
        {
            if (AIState == ActionState.CutsceneDeath)
                return false;
            return base.CheckActive();
        }

        #region Loot
        public static int TrophyType;
        public static int RelicType;
        public static int MaskType;
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
                loot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<CoralCage>(), 4));

                LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());

                //Armor guaranteed
                var armorDrop = DropHelper.PerPlayer(ModContent.ItemType<SeaRiderHelmet>(), 1);
                armorDrop.OnSuccess(DropHelper.PerPlayer(ModContent.ItemType<SeaRiderTunic>()));
                armorDrop.OnSuccess(DropHelper.PerPlayer(ModContent.ItemType<SeaRiderGreaves>()));
                notExpertRule.Add(armorDrop);

                //Useful loot
                notExpertRule.Add(DropHelper.CalamityStyle(new Fraction(1, 3),
                    ModContent.ItemType<PossessedTrident>(),
                    ModContent.ItemType<WarriorsAmphora>(),
                    ModContent.ItemType<GhostlySeasaddle>()
                ));

                //Big vanity
                notExpertRule.Add(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<DuatsFavor>(), 4));
                notExpertRule.Add(ItemDropRule.NotScalingWithLuck(MaskType, 7));

                //Minor vanity
                notExpertRule.Add(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<DriedFishTail>(), 10));
                notExpertRule.Add(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<FossilizedLarynx>(), 10));

                loot.Add(notExpertRule);
            }

            else
            {

                //Armor guaranteed
                var armorDrop = ItemDropRule.Common(ModContent.ItemType<SeaRiderHelmet>());
                armorDrop.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SeaRiderTunic>()));
                armorDrop.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SeaRiderGreaves>()));
                loot.Add(armorDrop);

                //Useful loot
                loot.Add(DropHelper.CalamityStyle(new Fraction(1, 3),
                    ModContent.ItemType<PossessedTrident>(),
                    ModContent.ItemType<WarriorsAmphora>(),
                    ModContent.ItemType<GhostlySeasaddle>()
                ));

                //Big vanity
                loot.Add(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<DuatsFavor>(), 4));
                loot.Add(ItemDropRule.NotScalingWithLuck(MaskType, 7));

                //Minor vanity
                loot.Add(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<DriedFishTail>(), 10));
                loot.Add(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<FossilizedLarynx>(), 10));
            }
        }


        public override void ModifyNPCLoot(NPCLoot npcLoot) => CommonDrops(npcLoot);
        #endregion
    }
}
