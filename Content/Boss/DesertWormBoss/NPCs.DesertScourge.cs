using CalamityFables.Content.Buffs;
using CalamityFables.Content.Items.DesertScourgeDrops;
using CalamityFables.Content.Tiles.Graves;
using CalamityFables.Content.Tiles.MusicBox;
using MonoMod.RuntimeDetour;
using ReLogic.Utilities;
using System.IO;
using System.Reflection;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    //Credits goes to turingcomplete for making a very goodworm boss with convective wanderer which has been a useful source of reference as to how to execute worm bosses
    [AutoloadBossHead]
    [ReplacingCalamity("DesertScourgeHead", "DesertScourgeBody", "DesertScourgeTail", "DesertNuisanceBody", "DesertNuisanceTail", "DesertNuisanceHead")]
    public partial class DesertScourge : ModNPC, ICustomDeathMessages
    {
        #region Sounds
        //Generic stuff
        public static readonly SoundStyle DigSound = new(SoundDirectory.DesertScourge + "DesertScourgeDig");
        public static readonly SoundStyle DigLoopSound = new(SoundDirectory.DesertScourge + "DesertScourgeDigLoop") { IsLooped = true, PlayOnlyIfFocused = true, MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };
        public SlotId digLoopSlot;


        public static readonly SoundStyle HitSound = new(SoundDirectory.DesertScourge + "DesertScourgeHit", 2) { PitchVariance = 0.1f };
        public static readonly SoundStyle MandibleTwitchSound = new(SoundDirectory.DesertScourge + "DesertScourgeTwitch", 4) { Volume = 0.3f };

        public static readonly SoundStyle WooshSound = new(SoundDirectory.DesertScourge + "DesertScourgeWoosh");
        public static readonly SoundStyle BurrowSound = new(SoundDirectory.DesertScourge + "DesertScourgeBurrowIn");
        public static readonly SoundStyle EmergeSound = new(SoundDirectory.DesertScourge + "DesertScourgeBurrowOut");

        public static readonly SoundStyle FastLungeStartSound = new(SoundDirectory.DesertScourge + "DesertScourgeFastDashBurrow");
        public static readonly SoundStyle FastLungeEmergeSound = new(SoundDirectory.DesertScourge + "DesertScourgeBurrowOutRoar");
        public static readonly SoundStyle FastLungeTelegraphSound = new(SoundDirectory.DesertScourge + "DesertScourgeFastLungeTelegraph");

        public static readonly SoundStyle SideLungeLeftTelegraph = new(SoundDirectory.DesertScourge + "DesertScourgeLeviathanLeft");
        public static readonly SoundStyle SideLungeRightTelegraph = new(SoundDirectory.DesertScourge + "DesertScourgeLeviathanRight");
        public static readonly SoundStyle SideLungeDashRoar = new(SoundDirectory.DesertScourge + "DesertScourgeLongRoar");
        public SlotId sideLungeRoarSlot;

        public static readonly SoundStyle PreyBelchVomitSound = new(SoundDirectory.DesertScourge + "DesertScourgeVomit");
        public static readonly SoundStyle PreyBelchDebrisImpactSound = new(SoundDirectory.DesertScourge + "AntlionGoreImpact", 4) { MaxInstances = 0 };
        public static readonly SoundStyle PreyBelchStormDebrisImpactSound = new(SoundDirectory.DesertScourge + "StormlionGoreImpact", 2) { MaxInstances = 0 };
        public static readonly SoundStyle PreyBelchBurpSound = new(SoundDirectory.DesertScourge + "DesertScourgeGurgle");
        public SlotId gurgleSoudnSlot;

        public static readonly SoundStyle ElectroChargeSound = new(SoundDirectory.DesertScourge + "DesertScourgeElectroCharge");
        public static readonly SoundStyle ElectroJumpSound = new(SoundDirectory.DesertScourge + "DesertScourgeElectroJump");
        public static readonly SoundStyle ElectroLoopSound = new(SoundDirectory.DesertScourge + "DesertScourgeElectricLoop") { IsLooped = true, PlayOnlyIfFocused = true, MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };
        public static readonly SoundStyle ElectroBlastSound = new(SoundDirectory.DesertScourge + "DesertScourgeElectroImpact");
        public static readonly SoundStyle PlatformElectrificationSound = new(SoundDirectory.DesertScourge + "PlatformElectrification");
        public SlotId electroLoopSlot;

        public static readonly SoundStyle ChungryLungeSound = new(SoundDirectory.DesertScourge + "DesertScourgeChungryLunge");

        public static readonly SoundStyle SpawnEmergeSound = new(SoundDirectory.DesertScourge + "DesertScourgeIntroEmerge");
        public SlotId emergeSoundSlot;
        public float emergeSoundTimer;
        public static readonly SoundStyle SpawnBiteSound = new(SoundDirectory.DesertScourge + "DesertScourgeBite");
        public static readonly SoundStyle SpawnSnapSound = new(SoundDirectory.DesertScourge + "DesertScourgeSnap");
        public static readonly SoundStyle SlapLarvaIntoTheAir = new("CalamityFables/Sounds/WetSlap", 4);


        public static readonly SoundStyle BurnoutChargeSound = new(SoundDirectory.DesertScourge + "DesertScourgeDeathCharge");
        public static readonly SoundStyle BurnoutLoopSound = new(SoundDirectory.DesertScourge + "DesertScourgeBurnoutLoop") { IsLooped = true, PlayOnlyIfFocused = true, MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };
        public SlotId burnoutLoopSlot;
        public static readonly SoundStyle ElectricFizzleSound = new(SoundDirectory.DesertScourge + "DesertScourgeElectricFizzle");
        public static readonly SoundStyle HeadPopSound = new(SoundDirectory.DesertScourge + "DesertScourgeHeadDislocate");
        public static readonly SoundStyle SkullStompSound = new(SoundDirectory.DesertScourge + "DesertScourgeSkullStomp");
        public static readonly SoundStyle SongFinalNote = new(SoundDirectory.Music+ "DesertScourgePlonk") { Type = SoundType.Music, Volume = 0.25f };
        public bool playedSound = false;
        #endregion

        #region Variables & Setup

        private static float DifficultyScale => Main.getGoodWorld ? 4 : Main.masterMode ? 2.5f : Main.expertMode ? 2 : 0;
        public static float LifePercentForFasterAttacks => 0.5f;
        public bool SecondPhase => (NPC.life / (float)NPC.lifeMax) < LifePercentForFasterAttacks;

        public int nextHitbox;

        #region AI states
        public enum ActionState
        {
            IdleMovement,
            IdleMovement_LargeArcs,
            IdleMovement_SmallArcs,
            IdleMovement_SlowArcs,
            IdleMovement_MeleeArcs,

            PreyBelch = 10,
            PreyBelch_TransitionIntoAttack,
            PreyBelch_Position,
            PreyBelch_Telegraph,
            PreyBelch_BackInTheGround,

            ElectroLunge = 20,
            ElectroLunge_BurrowBack, //Placing it earlier than it is in the order, because the other after it actually all do the zappies by default.
            ElectroLunge_TransitionIntoAttack,
            ElectroLunge_Position,
            ElectroLunge_Peek,
            ElectroLunge_Backpedal,
            ElectroLunge_Jump,

            FastLunge = 30,
            FastLunge_TransitionIntoAttack,
            FastLunge_Burrow,
            FastLunge_Anticipation,
            FastLunge_Lunge,
            FastLunge_FallDown,

            LeviathanLunge = 40,
            LeviathanLunge_TransitionIntoAttack,
            LeviathanLunge_Position,
            LeviathanLunge_Anticipation,
            LeviathanLunge_Lunge,

            ChungryLunge = 50,
            ChungryLunge_TransitionIntoAttack,
            ChungryLunge_Reposition,
            ChungryLunge_Anticipation,
            ChungryLunge_Lunge,
            ChungryLunge_FallDown,

            CutsceneFightStart = 100,
            CutsceneFightStart_CameraMagnetize,
            CutsceneFightStart_ThrowCreatureAndWait,
            CutsceneFightStart_EatCreature,
            CutsceneFightStart_FallBack,

            CutsceneDeath = 110,
            CutsceneDeath_TransitionIntoPose,
            CutsceneDeath_Position,
            CutsceneDeath_Peek,
            CutsceneDeath_Backpedal,
            CutsceneDeath_Jump,
            CutsceneDeath_SegmentsBurn,
            CutsceneDeath_ComedicPause,

            UnnagroedMovement = 120,
            UnnagroedMovement_Idle,
            UnnagroedMovement_SinkDown,

            Despawning = 130,

            ManualControl = 140,
        }

        public ActionState AIState {
            get => (ActionState)(NPC.ai[0] - (NPC.ai[0] % 10));
            set => NPC.ai[0] = (float)value;
        }

        public ActionState SubState {
            get => (ActionState)NPC.ai[0];
            set {
                //if (Main.netMode == NetmodeID.Server)
                //    new DesertScourgeSubstatePacket(this).Send(-1, -1, false);
                NPC.ai[0] = (float)value;
            }
        }

        public ActionState PreviousState {
            get => (ActionState)(NPC.localAI[0] - (NPC.localAI[0] % 10));
            set => NPC.localAI[0] = (float)value;
        }
        #endregion

        #region Useful variables
        public ref float AttackTimer => ref NPC.ai[1];
        public ref float AntiGravityCharge => ref NPC.ai[2];
        public ref float ExtraMemory => ref NPC.ai[3];

        public bool ForceRotation {
            get => NPC.localAI[3] == 1f; set => NPC.localAI[3] = value ? 1f : 0f;
        }

        public bool PreviouslyInTheFloor {
            get => NPC.localAI[1] == 1f; set => NPC.localAI[1] = value ? 1f : 0f;
        }
        public bool PreviouslyOnlyInsidePlatforms {
            get => NPC.localAI[2] == 1f; set => NPC.localAI[2] = value ? 1f : 0f;
        }

        public bool CanPlayBurrowSound(bool inFloor, bool onlyTopSurfaces)
        {
            //Can't burrow into platforms or if not inside the ground
            if (!inFloor || onlyTopSurfaces)
                return false;

            //Can't play the sound if was in the floor before
            if (PreviouslyInTheFloor && !PreviouslyOnlyInsidePlatforms)
                return false;

            //Can't play sound if fully walled in
            if (FablesUtils.FullyWalled(NPC.position, NPC.width, NPC.height))
                return false;

            return true;
        }

        public bool CanPlayEmergeSound(bool inFloor, bool onlyTopSurfaces)
        {
            //Can't emerge if still in the ground
            if (inFloor && !onlyTopSurfaces)
                return false;

            //Can't play the sound if wasn't in solid floor before
            if (!PreviouslyInTheFloor || PreviouslyOnlyInsidePlatforms)
                return false;

            //Can't play sound if fully walled in
            if (FablesUtils.FullyWalled(NPC.position, NPC.width, NPC.height))
                return false;

            return true;
        }
        #endregion

        #region Balance values
        public static int Stat_LifeMax = 3400;
        public static int Stat_LifeMaxExpert = 5000;
        public static int Stat_LifeMaxMaster = 6400;
        public static int Stat_Defense = 4;
        public static int Stat_ContactDamage = 30; 
        public static int Stat_ContactDamageCharging = 45;
        public static int Stat_ContactDamagChungryLunge = 60;
        public static float Stat_ContactDamageExpertMultiplier = 2f;
        public static float Stat_ContactDamageMasterMultiplier = 3f;


        public static float MeleeWeakness = 1.75f;
        public static float NoHurtingTailTipStart = 0.7f;

        public static int PreyBelch_AntlionChunkDamage = 36;
        public static int PreyBelch_StormlionChunkDamage = 48;
        public static int PreyBelch_StormlionExplosionDamage = 48;

        public static int ElectroLunge_FinalBlastDamage = 75;
        public static int ElectroLunge_PlatformElectrificationDamage = 30;

        public static int ChungryLunge_MeatDamage = 50;
        public static int ChungryLunge_MeatHealth = 120;
        public static int ChungryLunge_ScourgeMeatHealth = 180;
        public static float ChungryLunge_MeatExplosionResist = 0.2f;
        public static float ChungryLunge_BufferTime = 5f;
        #endregion

        public static int SegmentCount => Main.getGoodWorld ? 3 : 20;
        public const float SEGMENTSEPARATION = 46;

        public int biomeDespawnTimer = 1000;
        public int distanceDespawnTimer = 1000;

        public Player target => Main.player[NPC.target];
        public Vector2[] segmentPositions;
        public float[] segmentDamage;

        public Vector2 movementTarget;
        public Vector2 rotationAxis;

        // It's possible for two or more hits to happen on the same frame at death, when the CutsceneDeath state is triggered.
        // If this happens, the game mistakes the second hit as a proper death and ignores the animation entirely.
        // The purpose of this variable is to serve as a one-frame buffer at the time of death to prevent more than one hit from registering.
        // It is purposefully not synced so that it doesn't potentially cause wacky troubles if two players cause this problem.
        public bool JustDied;

        private static readonly MethodInfo UpdateLifeRegenMethod = typeof(NPCLoader).GetMethod(nameof(NPCLoader.UpdateLifeRegen));
        public delegate void orig_UpdateLifeRegen(NPC n, ref int damage);
        public delegate void hook_UpdateLifeRegen(orig_UpdateLifeRegen orig, NPC n, ref int damage);
        public static Hook RemoveDOTOnSegmentsHook;

        public override void Load()
        {
            SoundHandler.ModifyMusicChoiceEvent += SelectMusic;
            FablesNPC.ModifyBossMapIconDrawingEvent += FadeMapIcon;

            On_NPC.ScaleStats_UseStrengthMultiplier += MakeDSHealthConstant;

            if (UpdateLifeRegenMethod != null)
                RemoveDOTOnSegmentsHook = new Hook(UpdateLifeRegenMethod, (hook_UpdateLifeRegen)SegmentsIgnoreDOT);

            LoadTextures();
            if (!Main.dedServ)
                LoadGores();
            FablesUtils.AutoloadCommonBossDrops(Name, "Desert Scourge", AssetDirectory.DesertScourgeDrops, out MaskType, out TrophyType, out RelicType, out BossBagType, out TreasureBag, true);
            FablesGeneralSystemHooks.LogBossChecklistEvent += AddToChecklist;
        }
        private void AddToChecklist(Mod bossChecklist)
        {
            var collectibleDrops = new List<int>()
                {
                    RelicType, TrophyType, MaskType, ModContent.ItemType<DesertScourgeMusicBoxItem>(), ModContent.ItemType<StormlionChewtoy>()
                };

            bossChecklist.Call("LogBoss", Mod, nameof(DesertScourge),
                2.5f, //After eye
                () => WorldProgressionSystem.DefeatedDesertScourge,
                Type,
                new Dictionary<string, object>()
                {
                    ["spawnInfo"] = LocalizationRoundabout.DefaultText("Compat.BossChecklist.DesertScourge.SpawnInfo", "Lure out the Desert Scourge using [i:{0}][i:{1}][i:{2}] tasty stormlion grubs.")
                    .WithFormatArgs(ModContent.ItemType<StormlionLarvaItem>(), ModContent.ItemType<DeadStormlionLarvaItem>(), ModContent.ItemType<BucketOfLarvae>()),
                    ["despawnMessage"] = LocalizationRoundabout.DefaultText("Compat.BossChecklist.DesertScourge.Despawn", "The Desert Scourge dissapears below the shifting sands..."),
                    ["collectibles"] = collectibleDrops,
                    ["spawnItems"] = new List<int>() { ModContent.ItemType<StormlionLarvaItem>(), ModContent.ItemType<DeadStormlionLarvaItem>(), ModContent.ItemType<BucketOfLarvae>() },
                    ["customPortrait"] = DrawBossChecklistPortrait
                });
        }

        public override void Unload()
        {
            RemoveDOTOnSegmentsHook?.Undo();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Desert Scourge");
            TreasureBag.NPCType = Type;
            TreasureBag.bossLoot = CommonDrops;

            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Scale = 0.5f,
                PortraitScale = 0.6f,
                CustomTexturePath = "CalamityFables/Assets/Boss/DesertScourge/DesertScourgeBestiary",
                PortraitPositionXOverride = 40,
                PortraitPositionYOverride = 30,
                Position = new Vector2(60, 25)
            };

            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
        }

        public override void SetDefaults()
        {
            NPC.lifeMax = Stat_LifeMax;
            NPC.defense = Stat_Defense;
            NPC.damage = Stat_ContactDamage;
            NPC.npcSlots = 12f;
            NPC.width = 100;
            NPC.height = 100;

            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.value = Item.buyPrice(0, 5, 0, 0);
            NPC.behindTiles = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = HitSound;
            NPC.DeathSound = HeadPopSound;
            NPC.netAlways = true;
            segmentPositions = new Vector2[SegmentCount];
            segmentDamage = new float[SegmentCount];

            if (Main.getGoodWorld)
            {
                float damage = Main.rand.NextFloat(4f);
                for (int i = 0; i < SegmentCount; i++)
                    segmentDamage[i] = damage;
            }

            SetMusic();

            NPC.dontTakeDamage = true;
            quietMusic = false;
            NPC.rotation = -MathHelper.PiOver2;
        }

        public void SetMusic()
        {
            Music = SoundHandler.GetMusic("DesertScourge", MusicID.OtherworldlyBoss1);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert,
                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.DesertScourge")
            });
        }
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) => ScaleExpertStats(NPC, numPlayers, balance);
        public static void ScaleExpertStats(NPC npc, int numPlayers, float bossLifeScale)
        {
            int usedLifemax = Main.masterMode ? Stat_LifeMaxMaster : Main.expertMode ? Stat_LifeMaxExpert : Stat_LifeMax;

            //Theres multiple scourges in ftw so
            if (Main.getGoodWorld)
                usedLifemax = Stat_LifeMaxExpert / 7;

            npc.lifeMax = (int)(usedLifemax * bossLifeScale);
            npc.damage = Stat_ContactDamage;
        }

        //Avoids DS's health being way too high or too low. It shouldn't scale as much in journey
        private void MakeDSHealthConstant(On_NPC.orig_ScaleStats_UseStrengthMultiplier orig, NPC self, float strength)
        {
            if (self.type == Type || self.type == ModContent.NPCType<DesertScourgeHitbox>())
            {
                if (strength < 1)
                    strength = MathHelper.Lerp(strength, 1f, 0.7f);
                else
                    strength = MathHelper.Lerp(strength, 1f, 0.9f);
            }

            if (self.type == ModContent.NPCType<GroundBeef>() || self.type == ModContent.NPCType<ScourgeFlesh>())
                return;

            orig(self, strength);
        }

        public static void SegmentsIgnoreDOT(orig_UpdateLifeRegen orig, NPC n, ref int damage)
        {
            orig(n, ref damage);

            if (n.type == ModContent.NPCType<DesertScourgeHitbox>())
            {
                n.lifeRegen = 0;
                damage = 0;
            }
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
            }

            segmentPositions[0] = NPC.Center;
            for (int i = 1; i < segmentPositions.Length; i++)
            {
                segmentPositions[i] = segmentPositions[i - 1] - new Vector2(NPC.width, 0).RotatedBy(NPC.rotation);
            }

            for (int i = 0; i < segmentDamage.Length; i++)
            {
                segmentDamage[i] = 0;
            }

            AIState = ActionState.CutsceneFightStart;
            SpawnHitboxes();
        }
        #endregion

        /// <summary>
        /// Sets <see cref="NPC.dontTakeDamage"/> on all segments at once to 
        /// </summary>
        /// <param name="value"></param>
        public void SetDontTakeDamage(bool value)
        {
            NPC.dontTakeDamage = value;
            NPC segment = NPC;
            while (ValidSegment((int)segment.ai[1], NPC))
            {
                Main.npc[(int)segment.ai[1]].dontTakeDamage = value; 
                segment = Main.npc[(int)segment.ai[1]];
            }
        }

        #region AI
        public override bool PreAI()
        {
            NPC.netOffset = Vector2.Zero;
            return base.PreAI();
        }

        public override void AI()
        {
            if (NPC.dontTakeDamage && AIState != ActionState.CutsceneFightStart && AIState != ActionState.CutsceneDeath)
                SetDontTakeDamage(false);

            //Not targetable if unnagroed
            NPC.chaseable = AIState != ActionState.UnnagroedMovement;

            // Reset the just-died variable. It is only relevant for one frame during hit effects.
            JustDied = false;

            ForceRotation = false;
            float rotationFade = 95f;
            float rotationAmount = 0.01f;
            float velocityWiggle = 0.2f;
            float velocityWiggleFrequency = 4f;
            bool updateSegments = true;
            bool insideTiles;
            bool onlyInsideTopSurfaces;
            bool swapToNextAttack = false;

            insideTiles = FablesUtils.TileCollision(NPC.position, NPC.width, NPC.height, out onlyInsideTopSurfaces);

            //If not inside any tile, check if inside liquids, and if so, consider as if the water is platforms
            if (!insideTiles)
            {
                insideTiles = Collision.WetCollision(NPC.position, NPC.width, NPC.height);
                if (insideTiles)
                    onlyInsideTopSurfaces = true;
            }
            //Counts as inside tiles always if too far away and the player is above DS
            if (!insideTiles && target.Distance(NPC.Center) > 1300 && NPC.Center.Y >= target.Center.Y)
                insideTiles = true;

            if (insideTiles && AIState != ActionState.CutsceneFightStart)
                AntiGravityCharge = Math.Min(AntiGravityCharge + 0.03f, 1);

            if (!Main.dedServ)
            {
                ManageSideLungeRoarSound(insideTiles, onlyInsideTopSurfaces);
                ManageDigSound(insideTiles, onlyInsideTopSurfaces);
                ManageMandibles(insideTiles, onlyInsideTopSurfaces);
                ManagePoisonVisuals();
            }
            ManageSandstormCoolEffects();

            if (SubState == ActionState.LeviathanLunge_Lunge || SubState == ActionState.FastLunge_Lunge)
                NPC.damage = Stat_ContactDamageCharging;
            else if (SubState == ActionState.ChungryLunge_Lunge)
                NPC.damage = Stat_ContactDamagChungryLunge;
            else
                NPC.damage = Stat_ContactDamage;

            //Apply master & expert multipliers
            if (Main.masterMode)
                NPC.damage = (int)(NPC.damage * Stat_ContactDamageMasterMultiplier);
            else if (Main.expertMode)
                NPC.damage = (int)(NPC.damage * Stat_ContactDamageExpertMultiplier);

            //Attacks
            swapToNextAttack = ActBasedOnSubstate(insideTiles, onlyInsideTopSurfaces, ref rotationAmount, ref rotationFade, ref velocityWiggle, ref velocityWiggleFrequency);

            if (AIState == ActionState.ManualControl)
                ManualControlBehavior(ref rotationAmount, ref rotationFade, ref velocityWiggle, ref velocityWiggleFrequency, ref updateSegments);

            if (NPC.velocity != Vector2.Zero)
                NPC.velocity = NPC.velocity.RotatedBy((float)Math.Sin(Main.GlobalTimeWrappedHourly * velocityWiggleFrequency) * velocityWiggle / NPC.velocity.Length());
            if (updateSegments)
                UpdateSegmentPositions(rotationFade, rotationAmount);

            if (swapToNextAttack)
                SelectNextAttack();

            if (!target.ZoneDesert && AIState != ActionState.CutsceneFightStart && AIState != ActionState.CutsceneDeath)
                biomeDespawnTimer--;
            else
                biomeDespawnTimer = 60 * 8;

            if (NPC.target >= 0 && NPC.target < Main.maxPlayers && NPC.Distance(target.Center) > 200 * 16)
                distanceDespawnTimer--;
            else
                distanceDespawnTimer = 60 * 5;

            PreviouslyInTheFloor = insideTiles;
            PreviouslyOnlyInsidePlatforms = onlyInsideTopSurfaces;

            ManageMapIconOpacity(insideTiles, onlyInsideTopSurfaces);
            UpdateSegmentHitboxPositions();

            if (Main.getGoodWorld)
            {
                CameraManager.Shake = Math.Min(CameraManager.Shake, 5);
            }

            NPC.netUpdate = true;
        }

        public bool GetRandomTarget(float distance)
        {
            IEnumerable<Player> potentialTargets = Main.player.Where(p => p.active && !p.dead && p.ZoneDesert && p.Distance(NPC.Center) < distance);
            if (potentialTargets.Count() == 0)
            {
                NPC.TargetClosest(false);
                return false;
            }

            Player targetChoice = Main.rand.Next(potentialTargets);
            int oldTarget = NPC.target;
            NPC.target = targetChoice.whoAmI;
            if (oldTarget != NPC.target)
                NPC.netUpdate = true;
            return true;
        }

        public bool GetClosestDesertPlayer(float distance)
        {
            IEnumerable<Player> potentialTargets = Main.player.Where(p => p.active && !p.dead && p.ZoneDesert && p.Distance(NPC.Center) < distance);
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

        #region hitboxes, iframes, and death
        public void SpawnHitboxes()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int lastHitbox = 0;
            int hitbox = 0;

            for (int i = 1; i < segmentPositions.Length; i++)
            {
                Vector2 hitboxPosition = SegmentPosition(i);
                hitbox = NPC.NewNPC(NPC.GetSource_FromAI(), (int)hitboxPosition.X, (int)hitboxPosition.Y, ModContent.NPCType<DesertScourgeHitbox>(), NPC.whoAmI);

                if (i == 1)
                    nextHitbox = hitbox;

                Main.npc[hitbox].realLife = NPC.whoAmI;
                Main.npc[hitbox].ai[0] = NPC.whoAmI;
                Main.npc[hitbox].ai[2] = i;

                if (lastHitbox != 0)
                    Main.npc[lastHitbox].ai[1] = hitbox;

                if (i == segmentPositions.Length - 1)
                    Main.npc[hitbox].ai[1] = -1;

                lastHitbox = hitbox;
            }

            //Sync all the hitboxes
            if (Main.dedServ)
            {
                hitbox = nextHitbox;
                for (int i = 1; i < segmentPositions.Length - 1; i++)
                {
                    hitbox = (int)Main.npc[hitbox].ai[1];
                    if (hitbox < 200)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, hitbox);
                }
            }


            NPC.netUpdate = true;
        }

        public void UpdateSegmentHitboxPositions()
        {
            //Move the hitboxes around
            int segmentIndex = nextHitbox;
            if (nextHitbox == 0)
                return;

            for (int i = 1; i < segmentPositions.Length; i++)
            {
                if (!ValidSegment(segmentIndex, NPC))
                    break;

                NPC hitbox = Main.npc[segmentIndex];
                hitbox.Center = SegmentPosition(i);

                segmentIndex = (int)hitbox.ai[1];
            }
        }

        public static bool ValidSegment(int index, NPC head)
        {
            if (index < 0 || index > Main.maxNPCs)
                return false;
            if (!Main.npc[index].active)
                return false;
            if (Main.npc[index].ai[0] != head.whoAmI) //Checks if the segment is "owned" by this
                return false;
            if (Main.npc[index].type != ModContent.NPCType<DesertScourgeHitbox>())
                return false;
            return true;
        }

        public void RecursivelySpoofProjectileIframes(NPC segment, Projectile proj)
        {
            segment.VanillaSpoofProjectileHitIFrames(proj);
            if (ValidSegment((int)segment.ai[1], NPC))
                RecursivelySpoofProjectileIframes(Main.npc[(int)segment.ai[1]], proj);
        }

        public void RecursivelySpoofPlayerHitIFrames(NPC segment, Player player)
        {
            segment.VanillaSpoofPlayerHitIFrames(player);
            if (ValidSegment((int)segment.ai[1], NPC))
                RecursivelySpoofPlayerHitIFrames(Main.npc[(int)segment.ai[1]], player);
        }

        public override bool? CanBeHitByItem(Player player, Item item) => NPC.VanillaCanBeHitByPlayer(player);
        public override bool? CanBeHitByProjectile(Projectile projectile) => NPC.VanillaCanBeHitByProjectile(projectile);

        public override bool CheckDead()
        {
            if (AIState != ActionState.CutsceneDeath || JustDied || (int)SubState < (int)ActionState.CutsceneDeath_SegmentsBurn)
            {
                SetDontTakeDamage(true);
                NPC.life = 1;
                AttackTimer = 0f;
                AIState = ActionState.CutsceneDeath;
                NPC.RemoveBuff(ModContent.BuffType<ScourgePoison>());
                JustDied = true;
                return false;
            }

            if (SoundEngine.TryGetActiveSound(digLoopSlot, out var sound))
                sound.Stop();
            if (SoundEngine.TryGetActiveSound(electroLoopSlot, out sound))
                sound.Stop();
            if (SoundEngine.TryGetActiveSound(burnoutLoopSlot, out sound))
                sound.Stop();


            //Do the cal death effects
            if (CalamityFables.CalamityEnabled && !(bool)ParasiteCoreSystem.CalamityDesertScourgeDownedProperty.GetValue(null))
            {
                string key = "Mods.CalamityMod.Status.Progression.OpenSunkenSea";
                if (Main.netMode == NetmodeID.SinglePlayer)
                    Main.NewText(Language.GetTextValue(key), Color.Aquamarine);
                else
                    ChatHelper.BroadcastChatMessage(NetworkText.FromKey(key), Color.Aquamarine);
            }

            WorldProgressionSystem.DefeatedDesertScourge = true;
            return true;
        }
        #endregion

        #region Segment managment
        Vector2 BaseSegmentPosition(int index)
        {
            if (index < 0)
                return segmentPositions[0] + new Vector2(-SEGMENTSEPARATION * index, 0).RotatedBy(SegmentRotation(1));

            return segmentPositions[index];
        }
        public Vector2 SegmentPosition(float index)
        {
            if (index % 1 == 0)
                return (BaseSegmentPosition((int)index) + BaseSegmentPosition((int)index - 1)) / 2;
            return Vector2.Lerp(SegmentPosition((int)Math.Floor(index)), SegmentPosition((int)Math.Ceiling(index)), index % 1);
        }
        public float SegmentRotation(float index)
        {
            if (index % 1 == 0)
                return (BaseSegmentPosition((int)index - 1) - BaseSegmentPosition((int)index)).ToRotation();
            return Utils.AngleLerp(SegmentRotation((int)Math.Floor(index)), SegmentRotation((int)Math.Ceiling(index)), index % 1);
        }

        public void UpdateSegmentPositions(float rotationFade, float rotationAmount)
        {
            segmentPositions[0] = NPC.Center + NPC.velocity;

            Vector2 rotationGoal = Vector2.Zero;
            if (ForceRotation)
            {
                segmentPositions[1] = segmentPositions[0] + new Vector2(-SEGMENTSEPARATION, 0).RotatedBy(NPC.rotation);
                rotationGoal = segmentPositions[1] - segmentPositions[0];
            }

            for (int i = 1; i < segmentPositions.Length; i++)
            {
                //this is mainly just used when going backwards
                float recoilStrenght = 0.0f;
                if (SubState == ActionState.PreyBelch_BackInTheGround || SubState == ActionState.ElectroLunge_Backpedal)
                    recoilStrenght = 0.6f;

                segmentPositions[i] += NPC.velocity * (float)Math.Pow(recoilStrenght, i);


                if (i > (ForceRotation ? 2 : 1))
                {
                    Vector2 idealRotation = segmentPositions[i - 1] - segmentPositions[i - 2]; //Direction the earlier segment took towards the even-earlier segment
                    rotationGoal = Vector2.Lerp(rotationGoal, idealRotation, 1 / rotationFade);
                }

                //Tilt the angle between the 2 segments by the rotation amount
                Vector2 directionFromPreviousSegment = (rotationAmount * rotationGoal + (segmentPositions[i] - segmentPositions[i - 1]).SafeNormalize(Vector2.Zero)).SafeNormalize(Vector2.Zero);
                float segmentSeparation = (SEGMENTSEPARATION + (i == 1 ? 12 : 0)) * NPC.scale;
                segmentPositions[i] = segmentPositions[i - 1] + directionFromPreviousSegment * segmentSeparation;
            }

            if (!ForceRotation)
                NPC.rotation = SegmentRotation(0);
        }
        #endregion

        #region Managing visuals and sounds and other stuff
        public void TelegraphSand(float telegraphWidth, float dustSpeed, float dustProbability, int maxSurfaceDistance = 100, bool noFalloff = true, bool screenshake = true)
        {
            int x = (int)(NPC.Center.X / 16);
            int y = (int)(NPC.Center.Y / 16);
            int halfWidth = (int)(telegraphWidth / 32);

            for (int i = x - halfWidth; i < x + halfWidth; i++)
            {
                for (int j = 0; j < maxSurfaceDistance; j++)
                {
                    Tile tile = Framing.GetTileSafely(i, y - j);
                    if ((!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || TileID.Sets.Platforms[tile.TileType]) && (tile.WallType == 0 || Main.wallHouse[tile.WallType]))
                    {
                        Tile tileBelow = Framing.GetTileSafely(i, y - j + 1);
                        if (!tileBelow.HasTile || !Main.tileSolid[tileBelow.TileType] || Main.tileSolidTop[tileBelow.TileType])
                            continue;

                        float sideness = (1 - Math.Abs(i - x) / (float)halfWidth);

                        float probability = dustProbability;
                        if (!noFalloff)
                            probability *= 1 - j / (float)maxSurfaceDistance;

                        for (int d = 0; d < 10; d++)
                        {
                            if (Main.rand.NextFloat() < probability * (float)Math.Pow(sideness, 0.5f))
                            {
                                Vector2 dustPos = new Vector2(i, y - j) * 16f;
                                dustPos += Vector2.UnitX * Main.rand.NextFloat(16f) + Vector2.UnitY * 16f;

                                Dust floorDust = Main.dust[WorldGen.KillTile_MakeTileDust(i, y - j + 1, tileBelow)];
                                floorDust.position = dustPos;
                                floorDust.velocity = -Vector2.UnitY * dustSpeed * Main.rand.NextFloat(0.5f, 1.2f);
                                floorDust.velocity = floorDust.velocity.RotatedByRandom(MathHelper.PiOver4);
                                floorDust.scale = Main.rand.NextFloat(0.6f, 2f) * sideness;

                                //Dust dus = Dust.NewDustPerfect(dustPos, DustID.Sand, -Vector2.UnitY * dustSpeed * Main.rand.NextFloat(0.5f, 1.2f), 0);
                                //dus.noGravity = false;
                                //dus.velocity = dus.velocity.RotatedByRandom(MathHelper.PiOver4);
                                //dus.scale = Main.rand.NextFloat(0.6f, 2f) * sideness;
                                if (!noFalloff)
                                {
                                    //dus.velocity *= 0.2f + 0.8f * (1 - j / (float)maxSurfaceDistance);
                                    //dus.scale *= 0.2f + 0.8f * (1 - j / (float)maxSurfaceDistance);

                                    floorDust.velocity *= 0.2f + 0.8f * (1 - j / (float)maxSurfaceDistance);
                                    floorDust.scale *= 0.2f + 0.8f * (1 - j / (float)maxSurfaceDistance);
                                }
                            }
                        }
                        break;
                    }
                }
            }

            if (!screenshake)
                return;

            float screenshakeWidth = 300f;
            float screenshakeDepth = 400f;

            if (screenshake && NPC.Center.Y > Main.LocalPlayer.Center.Y + 60f && NPC.Center.Y < Main.LocalPlayer.Center.Y + screenshakeDepth && Math.Abs(NPC.Center.X - Main.LocalPlayer.Center.X) < screenshakeWidth)
            {
                float shakeMult = Math.Abs(NPC.Center.X - Main.LocalPlayer.Center.X) / screenshakeWidth;
                shakeMult *= 0.7f + 0.8f * Utils.GetLerpValue(screenshakeDepth, 60f, NPC.Center.Y - Main.LocalPlayer.Center.Y, true);

                CameraManager.Shake = Math.Max(CameraManager.Shake, 9f * shakeMult);
            }
        }

        public void ManageSideLungeRoarSound(bool inGround, bool onlyInsideTopSurfaces)
        {
            if (SoundEngine.TryGetActiveSound(sideLungeRoarSlot, out var rar))
            {
                rar.Position = NPC.Center;

                if (SubState == ActionState.LeviathanLunge_Lunge)
                {
                    float howFrontIsThePlayer = FablesUtils.PolyInOutEasing(Vector2.Dot(NPC.velocity.SafeNormalize(Vector2.One), NPC.DirectionTo(Main.LocalPlayer.Center)) * 0.5f + 0.5f, 3);
                    rar.Volume = (float)Math.Pow(1 - Math.Clamp(NPC.Distance(Main.LocalPlayer.Center) / (1300f - howFrontIsThePlayer * 300f), 0f, 1f), 0.85f);
                    rar.Sound.Pitch = -0.15f + 0.3f * howFrontIsThePlayer;
                    if (inGround && !onlyInsideTopSurfaces)
                    {
                        rar.Volume *= 0.8f;
                        rar.Sound.Pitch -= 0.1f;
                    }
                    return;
                }


                rar.Volume *= 0.95f;
                rar.Volume -= 0.05f;
                rar.Update();

                if (rar.Volume <= 0)
                {
                    rar.Stop();
                    sideLungeRoarSlot = SlotId.Invalid;
                }
            }
        }

        public void ManageDigSound(bool inGround, bool onlyInsideTopSurfaces)
        {
            if (inGround)
            {
                if (NPC.soundDelay <= 0)
                {
                    float distanceToPlayer = NPC.Distance(Main.LocalPlayer.Center);
                    float digVolume = 0.2f + 0.8f * Utils.GetLerpValue(1000f, 300f, distanceToPlayer, true);

                    if (SubState == ActionState.FastLunge_Anticipation || SubState == ActionState.LeviathanLunge_Anticipation)
                        digVolume = 0.2f + AttackTimer * 0.8f;

                    SoundEngine.PlaySound(DigSound with
                    {
                        Volume = DigSound.Volume * digVolume
                    }, NPC.Center);
                    NPC.soundDelay = (int)(12f + 12f * Utils.GetLerpValue(300f, 800f, distanceToPlayer, true));
                }
            }


            if (!SoundEngine.TryGetActiveSound(digLoopSlot, out var digLoop))
                digLoopSlot = SoundEngine.PlaySound(DigLoopSound, NPC.Center, FadeInDigLoopSound);

            if (SoundEngine.TryGetActiveSound(digLoopSlot, out digLoop))
            {
                digLoop.Position = NPC.Center;

                if (inGround && !onlyInsideTopSurfaces)
                {
                    if (!digLoop.IsPlaying)
                        digLoop.Resume();
                    digLoop.Volume = MathHelper.Lerp(digLoop.Volume, 0.5f, 0.02f);
                    digLoop.Sound.Pitch = MathHelper.Lerp(digLoop.Sound.Pitch, -0.5f * Utils.GetLerpValue(200f, 600f, NPC.Center.Y - Main.LocalPlayer.Center.Y, true), 0.3f);

                    digLoop.Update();
                }

                else
                {
                    if (digLoop.IsPlaying)
                        digLoop.Pause();
                    digLoop.Sound.Volume = MathHelper.Lerp(digLoop.Volume, 0f, 0.1f);
                    digLoop.Update();
                }
            }

            //Fade the sound in so its not jarring when DS spawns
            if (digLoopSoundFadeIn < 1f)
            {
                digLoopSoundFadeIn += 0.5f;
                if (digLoopSoundFadeIn > 1)
                    digLoopSoundFadeIn = 1f;
            }

            SoundHandler.TrackSound(digLoopSlot);

            //Substates where we dont need burrow/emerge sounds
            if (SubState == ActionState.FastLunge_Anticipation ||
                SubState == ActionState.FastLunge_Lunge)
                return;

            if (FablesUtils.FullyWalled(NPC.position, NPC.width, NPC.height))
                return;


            if (CanPlayBurrowSound(inGround, onlyInsideTopSurfaces))
            {
                SoundEngine.PlaySound(BurrowSound, NPC.Center);

                if (Main.LocalPlayer.WithinRange(NPC.Center, 2000f))
                    CameraManager.Shake += 8f * Utils.GetLerpValue(2000f, 1500f, Main.LocalPlayer.Distance(NPC.Center), true);
            }
            else if (CanPlayEmergeSound(inGround, onlyInsideTopSurfaces))
            {
                SoundStyle emergeSound = EmergeSound;
                float emergeVolume = 0.5f;
                if (SubState == ActionState.LeviathanLunge_Lunge)
                    emergeVolume = 0.9f;
                if (SubState == ActionState.CutsceneFightStart_EatCreature)
                {
                    emergeVolume = 1f;
                    emergeSound = SpawnEmergeSound;
                }

                SlotId emerge = SoundEngine.PlaySound(emergeSound with
                {
                    Volume = emergeVolume
                }, NPC.Center);

                if (Main.LocalPlayer.WithinRange(NPC.Center, 2000f))
                    CameraManager.Shake += 8f * Utils.GetLerpValue(2000f, 1500f, Main.LocalPlayer.Distance(NPC.Center), true);

                if (SubState == ActionState.CutsceneFightStart_EatCreature)
                {
                    emergeSoundSlot = emerge;
                    emergeSoundTimer = 0;
                }
            }
        }


        public float digLoopSoundFadeIn = 0f;
        public bool FadeInDigLoopSound(ActiveSound soundInstance)
        {
            soundInstance.Volume = Math.Min(digLoopSoundFadeIn, soundInstance.Volume);
            return true;
        }

        public void ManagePoisonVisuals()
        {
            if (Main.rand.NextBool(3) && NPC.GetGlobalNPC<DOTNPC>().scourgePoison > 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust bubel = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 256, 0, 0, Main.rand.Next(20, 100));
                    bubel.scale = Main.rand.NextFloat(2f, 2.5f);
                    bubel.velocity = -NPC.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(1.2f, 2f);
                    bubel.noLight = true;
                    bubel.noGravity = true;
                    bubel.rotation = Main.rand.NextFloat(MathHelper.Pi);
                }
            }
        }
        #endregion


        #region Hit stuff
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            if (AIState == ActionState.CutsceneFightStart ||
                AIState == ActionState.CutsceneDeath ||
                AIState == ActionState.UnnagroedMovement ||
                AIState == ActionState.Despawning ||
                SubState == ActionState.FastLunge_Burrow ||
                SubState == ActionState.FastLunge_Anticipation ||
                SubState == ActionState.LeviathanLunge_Position ||
                SubState == ActionState.LeviathanLunge_Anticipation ||
                SubState == ActionState.PreyBelch_Position ||
                SubState == ActionState.ElectroLunge_Position ||
                SubState == ActionState.ElectroLunge_BurrowBack
                )
                return false;

            return base.CanHitPlayer(target, ref cooldownSlot);
        }

        public static float SegmentHurtboxSizeMultiplier(NPC npc)
        {
            if (npc.ModNPC is DesertScourge head && (
                head.SubState == ActionState.PreyBelch_Telegraph ||
                head.SubState == ActionState.ElectroLunge_Peek ||
                head.SubState == ActionState.ElectroLunge_Backpedal
                ))
            {
                return 0.6f;
            }

            return 1f;
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            ApplyMeleeWeakness(ref modifiers);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.IsContactMelee())
                ApplyMeleeWeakness(ref modifiers);
        }

        public static void ApplyMeleeWeakness(ref NPC.HitModifiers modifiers) => modifiers.FinalDamage *= MeleeWeakness;

        public override void HitEffect(NPC.HitInfo hit)
        {
            TakeSegmentDamage(0, hit.Damage, 1);

            if (NPC.life <= 0)
            {
                if (SoundEngine.TryGetActiveSound(digLoopSlot, out var digLoop))
                {
                    digLoop.Stop();
                }

                if (SoundEngine.TryGetActiveSound(sideLungeRoarSlot, out var roarSound))
                {
                    roarSound.Stop();
                }

                if (SoundEngine.TryGetActiveSound(electroLoopSlot, out var eletro))
                {
                    eletro.Stop();
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (SubState == ActionState.FastLunge_Lunge)
            {
                target.velocity.Y -= 15f;
                target.velocity.X -= (NPC.Center.X - target.Center.X).NonZeroSign() * 3f;
            }
        }
        #endregion

        #region Custom death messages
        public bool CustomDeathMessage(Player player, ref PlayerDeathReason customDeath)
        {
            if (AIState == ActionState.ChungryLunge)
            {
                customDeath.CustomReason = Language.GetText("Mods.CalamityFables.Extras.DeathMessages.DesertScourgeChungry." + Main.rand.Next(1, 5).ToString()).ToNetworkText(player.name);
                return true;
            }

            return false;
        }
        #endregion

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(nextHitbox);
            writer.Write(biomeDespawnTimer);
            writer.Write(distanceDespawnTimer);
            writer.WriteVector2(movementTarget);
            writer.WriteVector2(rotationAxis);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            nextHitbox = reader.ReadInt32();
            biomeDespawnTimer = reader.ReadInt32();
            distanceDespawnTimer = reader.ReadInt32();
            movementTarget = reader.ReadVector2();
            rotationAxis = reader.ReadVector2();
        }

        #region Loot
        public static int MaskType;
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
                loot.Add(ItemDropRule.MasterModeCommonDrop(RelicType)); //Todo figure out something thats not master for relic
                loot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<StormlionChewtoy>(), 4));

                LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
                notExpertRule.Add(ItemDropRule.NotScalingWithLuck(MaskType, 7));
                notExpertRule.Add(DropHelper.CalamityStyle(new Fraction(1, 4),
                    ModContent.ItemType<TornElectrosac>(),
                    ModContent.ItemType<AmpstringBow>(),
                    ModContent.ItemType<CarrionDetonatorStaff>(),
                    ModContent.ItemType<StormlionWhip>()
                ));

                if (CalamityFables.CalamityEnabled && ModContent.TryFind("CalamityMod", "PearlShard", out ModItem pearlShard))
                {
                    notExpertRule.Add(ItemID.Coral, 1, 25, 30);
                    notExpertRule.Add(ItemID.Seashell, 1, 25, 30);
                    notExpertRule.Add(ItemID.Starfish, 1, 25, 30);
                    notExpertRule.Add(DropHelper.PerPlayer(pearlShard.Type, 1, 25, 30));
                }

                loot.Add(notExpertRule);
            }

            else
            {
                loot.Add(ModContent.ItemType<IntestinalScarf>(), 1); //Expert item
                loot.Add(ItemDropRule.NotScalingWithLuck(MaskType, 7));
                loot.Add(DropHelper.CalamityStyle(new Fraction(1, 3),
                    ModContent.ItemType<TornElectrosac>(),
                    ModContent.ItemType<AmpstringBow>(),
                    ModContent.ItemType<CarrionDetonatorStaff>(),
                    ModContent.ItemType<StormlionWhip>()
                ));

                if (CalamityFables.CalamityEnabled && ModContent.TryFind("CalamityMod", "PearlShard", out ModItem pearlShard ))
                {
                    loot.Add(ItemID.Coral, 1, 30, 40);
                    loot.Add(ItemID.Seashell, 1, 30, 40);
                    loot.Add(ItemID.Starfish, 1, 30, 40);
                    loot.Add(DropHelper.PerPlayer(pearlShard.Type, 1, 30, 40));
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) => CommonDrops(npcLoot);
        #endregion
    }
}
