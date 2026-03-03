using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Particles;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.Utilities;
using static CalamityFables.Helpers.FablesUtils;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.NPCs.Wulfrum
{
    interface ISuperchargable
    {
        bool IsSupercharged {
            get; set;
        }
    }

    [ReplacingCalamity("WulfrumAmplifier", "WulfrumPylon")]
    public class WulfrumNexus : ModNPC
    {
        #region Squad paramters stuff

        public static int[,] FormationsOf3;

        public static int[,] FormationsOf4;

        public static int[,] FormationsOf5;

        #region formation delegates
        public delegate Vector2 SpawnOffsetDelegate(int index, int max, int direction);
        public delegate Vector4 SpawnAIParamsDelegate();
        public static Vector4 BaseAIParams() => new Vector4(0);

        public static readonly int[] WulfrumRobots = new int[] { NPCType<WulfrumRoller>(), NPCType<WulfrumGrappler>(), NPCType<WulfrumMagnetizer>(), NPCType<WulfrumMortar>(), NPCType<WulfrumRover>() };

        //Magnetizer
        public static Vector2 MagnetizerSpawnPosition(int index, int max, int direction)
        {
            float distanceFromNexus = 140f;
            Vector2 offsetey = -Vector2.UnitY * distanceFromNexus;
            if (max > 1)
            {
                float maxAngle = MathHelper.PiOver4;
                offsetey = offsetey.RotatedBy(-maxAngle + maxAngle * 2 * (index / (float)(max - 1)));
            }
            return offsetey;
        }
        public static Vector4 MagnetizerSpawnAIParams() => new Vector4(Main.rand.NextFloat(0f, 40f), 0f, 0f, 0f);
        //Rover
        public static Vector2 RoverSpawnPosition(int index, int max, int direction)
        {
            Vector2 offsetey = -Vector2.UnitY * 20;
            if (max > 1)
            {
                float maxWidth = 200;
                offsetey += Vector2.UnitX * (-maxWidth + maxWidth * 2 * (index / (float)(max - 1)));
            }
            return offsetey;
        }
        //Roller
        public static Vector2 RollerSpawnPosition(int index, int max, int direction)
        {
            float distanceFromNexus = 90f;
            Vector2 offsetey = -Vector2.UnitY * distanceFromNexus;
            if (max > 1)
            {
                float maxAngle = MathHelper.PiOver2;
                offsetey = offsetey.RotatedBy(-maxAngle + maxAngle * 2 * (index / (float)(max - 1)));
            }
            return offsetey;
        }
        public static Vector4 RollerSpawnAIParams() => new Vector4(0f, Main.rand.NextFloat(0f, 2.5f * 60f), 0f, 0f);
        //Grappler
        public static Vector2 GrapplerSpawnPosition(int index, int max, int direction)
        {
            Vector2 offsetey = -Vector2.UnitY * Main.rand.NextFloat(30f, 50f);
            if (max > 1)
            {
                float maxWidth = 240;
                offsetey += Vector2.UnitX * (-maxWidth + maxWidth * 2 * (index / (float)(max - 1)));
            }
            return offsetey;
        }
        public static Vector4 GrapplerSpawnAIParams() => new Vector4(Main.rand.NextFloat(-1f, 0.4f), 0f, 0f, 0f);
        //Mortar
        public static Vector2 MortarSpawnPosition(int index, int max, int direction)
        {
            Vector2 offsetey = -Vector2.UnitY * Main.rand.NextFloat(10f, 30f) + 60f * Vector2.UnitX * direction;
            if (max > 1)
            {
                float maxWidth = 240;
                offsetey += Vector2.UnitX * (maxWidth * (index / (float)(max - 1)) * direction);
            }
            return offsetey;
        }
        public static Vector4 MortarSpawnAIParams() => new Vector4(Main.rand.NextFloat(0f, 55f), 0f, 0f, 0f);
        #endregion
        #endregion

        public static readonly SoundStyle DeathSound = new SoundStyle(SoundDirectory.Wulfrum + "WulfrumNexusDeath");
        public static readonly SoundStyle ChargeupAttackSound = new SoundStyle(SoundDirectory.Wulfrum + "WulfrumNexusCharge") { Volume = 0.5f };

        public static readonly SoundStyle SpawnEnemyWaveSound = new SoundStyle(SoundDirectory.Wulfrum + "WulfrumNexusSpawnWave");


        public const float SPAWN_RADIUS = 1100;
        public const float ACTIVATION_RADIUS = 700;
        public const float DESPAWN_RADIUS = 2300;
        public const float SUMMONS_HEALTH_MULTIPLIER = 1.33f;
        public const int BUNKER_WAVE_COUNT = 3;

        public override string Texture => AssetDirectory.WulfrumNPC + Name + "_Base";
        public Player target => Main.player[NPC.target];
        public ref float Initialized => ref NPC.ai[0];
        public ref float FireCooldown => ref NPC.ai[1];
        public Point16 AttachedTileEntityPos => new Point16((int)NPC.ai[2], (int)NPC.ai[3]);

        public float legAngle;
        public float squishyY = 1;

        public int yFrame;
        public float cogFrame;
        public static Asset<Texture2D> NoiseTex;
        public float newWaveTimer;


        public static int BannerType;
        public static AutoloadedBanner bannerTile;
        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Wulfrum Nexus", AssetDirectory.WulfrumBanners, out bannerTile, killCount: 10);

            FablesGeneralSystemHooks.ClearWorldEvent += ClearWorld;
            FablesGeneralSystemHooks.LoadWorldDataEvent += LoadWorldData;
            FablesGeneralSystemHooks.SaveWorldDataEvent += SaveWorldData;
            FablesGeneralSystemHooks.NetSendEvent += NetSend;
            FablesGeneralSystemHooks.NetReceiveEvent += NetReceive;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Nexus");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                SpriteDirection = 1

            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            Main.npcFrameCount[Type] = 5;
            FablesSets.WulrumNPCs[Type] = true;
            bannerTile.NPCType = Type;

            FormationsOf3 = new int[,]
            {
                { NPCType<WulfrumMortar>(), NPCType<WulfrumGrappler>(), NPCType<WulfrumRoller>() }, //"Complete chaos"
                { NPCType<WulfrumMagnetizer>(), NPCType<WulfrumMagnetizer>(), NPCType<WulfrumRoller>() },
                { NPCType<WulfrumMortar>(), NPCType<WulfrumMagnetizer>(), NPCType<WulfrumRoller>() } //Super bomba
            };

            FormationsOf4 = new int[,]
            {
                { NPCType<WulfrumGrappler>(), NPCType<WulfrumGrappler>(), NPCType<WulfrumGrappler>() ,NPCType<WulfrumMagnetizer>() }, //"Swingies"
                { NPCType<WulfrumMagnetizer>(), NPCType<WulfrumRoller>(), NPCType<WulfrumRoller>() ,NPCType<WulfrumRover>() }, //"Good samaritans
                { NPCType<WulfrumGrappler>(), NPCType<WulfrumGrappler>(), NPCType<WulfrumGrappler>(), NPCType<WulfrumRover>() }, //"Strange squad"
                { NPCType<WulfrumGrappler>(), NPCType<WulfrumMagnetizer>(), NPCType<WulfrumMortar>(), NPCType<WulfrumRover>() } //Random juice
            };

            FormationsOf5 = new int[,]
            {
                { NPCType<WulfrumRoller>(), NPCType<WulfrumMortar>(), NPCType<WulfrumMortar>(), NPCType<WulfrumMortar>(), NPCType<WulfrumRover>() }, //"Firing squad
                { NPCType<WulfrumMagnetizer>(), NPCType<WulfrumMagnetizer>(), NPCType<WulfrumMagnetizer>(), NPCType<WulfrumMortar>(), NPCType<WulfrumRoller>() }, //"Bombing squad"
                { NPCType<WulfrumMagnetizer>(), NPCType<WulfrumMagnetizer>(), NPCType<WulfrumMagnetizer>(), NPCType<WulfrumMortar>(), NPCType<WulfrumRover>()}, //""Bombing squad 2
                { NPCType<WulfrumRoller>(), NPCType<WulfrumRoller>(), NPCType<WulfrumRoller>(), NPCType<WulfrumMortar>(), NPCType<WulfrumMortar>() }, //"Self destructive"
                { NPCType<WulfrumRoller>(), NPCType<WulfrumRoller>(), NPCType<WulfrumRoller>(), NPCType<WulfrumMagnetizer>(), NPCType<WulfrumMagnetizer>() }, //"Magnet roller"
                { NPCType<WulfrumRoller>(), NPCType<WulfrumGrappler>(), NPCType<WulfrumMagnetizer>(), NPCType<WulfrumMortar>(), NPCType<WulfrumRover>() } //The whole gang
            };


            if (Main.dedServ)
                return;
            //Sets generic gores to be safe
            for (int i = 1; i < 12; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("WulfrumEnemyGore" + i.ToString()).Type] = true;
            for (int i = 0; i < 5; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("WulfrumNexusGore" + i.ToString()).Type] = true;
        }

        public override void SetDefaults()
        {
            legAngle = MathHelper.PiOver4 * 0.89f;
            squishyY = 1;
            AIType = -1;
            NPC.aiStyle = -1;
            NPC.damage = 14;
            NPC.width = 50;
            NPC.height = 70;
            NPC.defense = 0;
            NPC.lifeMax = 10;
            NPC.knockBackResist = 0f;
            NPC.value = Item.buyPrice(0, 0, 10, 15);
            NPC.HitSound = SoundDirectory.CommonSounds.WulfrumNPCHitSound;
            NPC.DeathSound = DeathSound;
            NPC.dontTakeDamage = true;
            NPC.netAlways = true;
            Banner = Type;
            BannerItem = BannerType;

            WulfrumBunkerRaidScene.NexusType = Type;
        }

        #region Check if can spawn stuff
        public static bool CanWulfrumNexusSpawnNaturally = false;

        public void ClearWorld()
        {
            CanWulfrumNexusSpawnNaturally = false;
        }

        public void SaveWorldData(TagCompound tag)
        {
            tag["CanWulfrumNexusSpawnNaturally"] = CanWulfrumNexusSpawnNaturally;
        }

        public void LoadWorldData(TagCompound tag)
        {
            CanWulfrumNexusSpawnNaturally = tag.GetBool("CanWulfrumNexusSpawnNaturally");

            //If can't spawn naturally, check if theres any tile entities that may spawn them naturally anyways
            if (!CanWulfrumNexusSpawnNaturally)
            {
                foreach (var item in TileEntity.ByID)
                {
                    if (item.Value.type == TileEntityType<WulfrumNexusSpawner>())
                    {
                        return;
                    }
                }

                //if no nexus tile entities were found, make them spawn naturally
                CanWulfrumNexusSpawnNaturally = true;
            }
        }

        public void NetSend(BinaryWriter writer)
        {
            writer.Write(CanWulfrumNexusSpawnNaturally);
        }
        public void NetReceive(BinaryReader reader)
        {
            CanWulfrumNexusSpawnNaturally = reader.ReadBoolean();
        }

        #endregion

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            if (Main.masterMode)
                NPC.damage = (int)(NPC.damage * 0.75f);
            NPC.lifeMax = 10;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(NPC.GetBestiaryCreditId(), true);
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.DayTime,

                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.WulfrumNexus")
            });
        }

        internal bool playedSpawnSound = false;

        public override void AI()
        {
            if (AttachedTileEntityPos != Point16.Zero)
                NPC.noGravity = true;

            if (!playedSpawnSound)
            {
                SoundEngine.PlaySound(WulfrumMortar.ReadyMortar with { Pitch = -0.5f, Volume = 0.5f }, NPC.Center);
                playedSpawnSound = true;
            }

            Lighting.AddLight(NPC.Center, (Color.GreenYellow * 0.8f).ToVector3());

            //Prevent the on-spawn "stuck" issue by forcing it to have an initial direction
            if (NPC.direction == 0)
            {
                NPC.TargetClosest(false);
                NPC.direction = (target.Center.X - NPC.Center.X).NonZeroSign();
            }

            if (NPC.velocity.Y > 0)
            {
                squishyY = MathHelper.Lerp(squishyY, 0.2f, 0.1f);
            }
            else if (squishyY > 0)
                squishyY = -0.2f;
            else if (squishyY < 0)
                squishyY = MathHelper.Lerp(squishyY, 0f, 0.1f);

            if (Initialized == 0 && target.Distance(NPC.Center) < ACTIVATION_RADIUS)
            {
                if (newWaveTimer == 0)
                    SoundEngine.PlaySound(SpawnEnemyWaveSound, NPC.Center);
                if (newWaveTimer < 1f)
                {
                    newWaveTimer += 1 / (60 * 1.9f);
                    return;
                }

                Initialized = 1;
                newWaveTimer = 0f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int squadSize = 4;
                    if (Main.rand.NextBool())
                    {
                        if (Main.expertMode)
                            squadSize = 5;
                        else
                            squadSize = 3;
                    }

                    int[] formation = GetFormation(squadSize);
                    IEntitySource dreadedFuckingSource = NPC.GetSource_FromAI();
                    int direction = (NPC.Center.X - target.Center.X).NonZeroSign();

                    SpawnFormation(NPC.Center, formation, dreadedFuckingSource, direction);
                    NPC.netUpdate = true;
                }
                else
                    Initialized = 0.5f;
            }

            if (Initialized >= 1 && NPC.dontTakeDamage)
            {
                if (Main.expertMode)
                {
                    FireCooldown++;
                    if (FireCooldown > 60 * 5 && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1) && NPC.Distance(target.Center) < 700)
                    {
                        //Go in the negatives as a readyup
                        FireCooldown = -40;
                        SoundEngine.PlaySound(ChargeupAttackSound, NPC.Center);
                    }
                    if (FireCooldown == -1)
                    {
                       if (Main.netMode != NetmodeID.MultiplayerClient)
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, NPC.DirectionTo(target.Center) * 13f, ProjectileType<WulfrumNexusShot>(), NPC.damage / 2, 2, Main.myPlayer);
                    }
                }


                List<NPC> npcThatProtects = Main.npc.ToList().FindAll(n =>
                n.active &&
                WulfrumRobots.Contains(n.type) &&
                n.Distance(NPC.Center) < DESPAWN_RADIUS &&
                n.ModNPC is ISuperchargable chargableRobot &&
                chargableRobot.IsSupercharged
                );

                if (AttachedTileEntityPos != Point16.Zero && PointOfInterestMarkerSystem.WulfrumBunkerPos != Point.Zero)
                {
                    Rectangle bunkerBounds = PointOfInterestMarkerSystem.WulfrumBunkerRectangle;
                    bunkerBounds.Inflate(5, 5);
                    bunkerBounds.X *= 16;
                    bunkerBounds.Y *= 16;
                    bunkerBounds.Width *= 16;
                    bunkerBounds.Height *= 16;

                    npcThatProtects.RemoveAll(n => !bunkerBounds.Contains(n.Hitbox));
                }

                //Connection to the protectors
                if (npcThatProtects.Any())
                {
                    if (Main.rand.NextBool(10))
                    {
                        Dust indicator = Dust.NewDustPerfect(NPC.Center, DustID.Vortex, NPC.Center.DirectionTo(npcThatProtects[Main.rand.Next(0, npcThatProtects.Count())].Center) * 10f);
                        indicator.noGravity = true;
                    }

                    return;
                }

                //If not naturally spawned, we spawn a few extra waves
                if (AttachedTileEntityPos != Point16.Zero && Initialized < BUNKER_WAVE_COUNT)
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        //new spawn wave sound
                        if (newWaveTimer == 0)
                            SoundEngine.PlaySound(SpawnEnemyWaveSound, NPC.Center);

                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (newWaveTimer < 1f)
                        {
                            newWaveTimer += 1 / (60f * 1.9f);
                            return;
                        }

                        int squadSize = 4;
                        if (Main.rand.NextBool())
                        {
                            if (Main.expertMode)
                                squadSize = 5;
                            else
                                squadSize = 3;
                        }

                        int[] formation = GetFormation(squadSize);
                        IEntitySource dreadedFuckingSource = NPC.GetSource_FromAI();
                        int direction = (NPC.Center.X - target.Center.X).NonZeroSign();

                        Initialized++;
                        newWaveTimer = 0f;
                        SpawnFormation(NPC.Center, formation, dreadedFuckingSource, direction);
                        NPC.netUpdate = true;
                    }
                    //The waves arent spawned MP client side, but we still increment the timer so the wave spawn sound doesnt get repeated
                    else
                        newWaveTimer++;

                    return;
                }

                SoundEngine.PlaySound(RoverDrive.BreakSound, NPC.Center);
                CameraManager.Shake += Utils.GetLerpValue(1000, 500, NPC.Distance(Main.LocalPlayer.Center), true) * 4;

                for (int i = 0; i < 20; i++)
                {
                    Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                    Particle fragment = new TechyHoloysquareParticle(NPC.Center + direction * 20f, direction * Main.rand.NextFloat(2f, 5f), 2f, CommonColors.WulfrumBlue, Main.rand.Next(20, 35));
                    ParticleHandler.SpawnParticle(fragment);
                }

                NPC.dontTakeDamage = false;
            }
        }

        public static int[] GetFormation(int count = 4)
        {
            int[,] spawningPool = FormationsOf4;
            if (count == 3)
                spawningPool = FormationsOf3;
            if (count == 5)
                spawningPool = FormationsOf5;

            int formationVariant = Main.rand.Next(0, spawningPool.GetLength(0));
            return spawningPool.SliceRow(formationVariant).ToArray();
        }

        public static void SpawnFormation(Vector2 center, int[] formation, IEntitySource source, int direction, bool superCharge = true)
        {
            int magnetizers = formation.Where(p => p == NPCType<WulfrumMagnetizer>()).Count();
            int rovers = formation.Where(p => p == NPCType<WulfrumRover>()).Count();
            int rollers = formation.Where(p => p == NPCType<WulfrumRoller>()).Count();
            int grapplers = formation.Where(p => p == NPCType<WulfrumGrappler>()).Count();
            int mortars = formation.Where(p => p == NPCType<WulfrumMortar>()).Count();

            int target = Player.FindClosest(center, 1, 1);

            if (magnetizers > 0)
                SpawnNPCGroup(source, center, NPCType<WulfrumMagnetizer>(), magnetizers, MagnetizerSpawnPosition, MagnetizerSpawnAIParams, direction, target, superCharge);
            if (rovers > 0)
                SpawnNPCGroup(source, center, NPCType<WulfrumRover>(), rovers, RoverSpawnPosition, BaseAIParams, direction, target, superCharge);
            if (rollers > 0)
                SpawnNPCGroup(source, center, NPCType<WulfrumRoller>(), rollers, RollerSpawnPosition, RollerSpawnAIParams, direction, target, superCharge);
            if (grapplers > 0)
                SpawnNPCGroup(source, center, NPCType<WulfrumGrappler>(), grapplers, GrapplerSpawnPosition, GrapplerSpawnAIParams, direction, target, superCharge);
            if (mortars > 0)
                SpawnNPCGroup(source, center, NPCType<WulfrumMortar>(), mortars, MortarSpawnPosition, MortarSpawnAIParams, direction, target, superCharge);
        }

        public static void SpawnNPCGroup(IEntitySource source, Vector2 center, int npcType, int npcCount, SpawnOffsetDelegate spawnPos, SpawnAIParamsDelegate aiParams, int direction, int target, bool superCharge = true)
        {
            if (aiParams is null)
                aiParams = BaseAIParams;

            Vector2[] spawnPositions = new Vector2[npcCount];

            for (int i = 0; i < npcCount; i++)
            {
                Vector2 spawnPosition = center + spawnPos(i, npcCount, direction);

                int loops = 0;
                while (Collision.SolidCollision(spawnPosition - Vector2.One * 16, 32, 32, false))
                {
                    spawnPosition -= Vector2.UnitY * 16;
                    if (loops > 60)
                        break;
                }
                if (loops > 60)
                    continue;

                spawnPositions[i] = spawnPosition;

                Vector4 npcAI = aiParams();
                NPC npc = NPC.NewNPCDirect(source, spawnPosition, npcType, 0, npcAI.X, npcAI.Y, npcAI.Z, npcAI.W, target: target);
                npc.lifeMax = (int)(npc.lifeMax * SUMMONS_HEALTH_MULTIPLIER);
                npc.life = npc.lifeMax;

                if (superCharge && npc != null && npc.ModNPC is ISuperchargable charger)
                    charger.IsSupercharged = true;

                npc.netUpdate = true;
                for (int iy = 0; iy < 16; iy++)
                {
                    Dust zapDust = Dust.NewDustPerfect(spawnPosition + Main.rand.NextVector2Circular(1f, 1f) * 20f, 226, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(1f, 2.3f) - Vector2.UnitY * 6f);
                    zapDust.noGravity = true;
                }
            }

            if (Main.netMode == NetmodeID.Server)
                new WulfrumSquadSpawnEffectsPacket(spawnPositions).Send(runLocally:false);
        }

        #region Drawing

        public int coreFrameNumber;
        public override void FindFrame(int frameHeight)
        {
            //float velocity = NPC.IsABestiaryIconDummy ? 2 : Math.Abs(NPC.velocity.X);
            NPC.frameCounter += 0.16;
            yFrame = (int)(NPC.frameCounter % 5);
            NPC.frame = new Rectangle(0, 0, NPC.width, NPC.height);

            coreFrameNumber = (int)(Main.GlobalTimeWrappedHourly * 8f) % 5;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = NPC.TintFromBuffAesthetic(drawColor);
            //Fsr the icon dummy uses a fullblack color by default?
            if (NPC.IsABestiaryIconDummy)
                drawColor = Color.White;

            if (NPC.IsABestiaryIconDummy)
            {
                legAngle = -0.3f;
            }


            Texture2D bodyTex = TextureAssets.Npc[Type].Value;
            Texture2D coreTex = Request<Texture2D>(AssetDirectory.WulfrumNPC + Name + "_Core").Value;
            Texture2D legTex = Request<Texture2D>(AssetDirectory.WulfrumNPC + Name + "_Leg").Value;
            Texture2D topTex = Request<Texture2D>(AssetDirectory.WulfrumNPC + Name + "_Top").Value;

            Vector2 gfxOffY = NPC.GfxOffY() + Vector2.UnitY * squishyY * 9f;
            Rectangle bodyFrame = bodyTex.Frame(1, 5, 0, yFrame, 0, -2);
            Rectangle coreFrame = coreTex.Frame(1, 5, 0, coreFrameNumber, 0, -2);

            Main.spriteBatch.Draw(bodyTex, NPC.Center + gfxOffY - screenPos, bodyFrame, drawColor, NPC.rotation, bodyFrame.Size() / 2f, NPC.scale, 0f, 0f);
            Main.spriteBatch.Draw(coreTex, NPC.Center - Vector2.UnitY.RotatedBy(NPC.rotation) * 2f + gfxOffY - screenPos, coreFrame, drawColor, NPC.rotation, coreFrame.Size() / 2f, NPC.scale, 0f, 0f);

            //Draw the 2 legs
            Vector2 legOrigin = new Vector2(legTex.Width / 2, 4);
            Vector2 legPosition = NPC.Center + Vector2.UnitY.RotatedBy(NPC.rotation) * 24f * NPC.scale;
            Vector2 legOffset = Vector2.UnitX.RotatedBy(NPC.rotation) * 10f * NPC.scale;

            for (int i = -1; i <= 1; i += 2)
            {
                SpriteEffects flip = i == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                float legRotation = NPC.rotation - (legAngle + squishyY * 0.9f) * i;

                Main.spriteBatch.Draw(legTex, legPosition + i * legOffset + gfxOffY - screenPos, null, drawColor, legRotation, legOrigin, NPC.scale, flip, 0f);
            }
            Main.spriteBatch.Draw(topTex, NPC.Center + gfxOffY - screenPos, null, drawColor, NPC.rotation, topTex.Size() / 2f, NPC.scale, 0f, 0f);

            if (NPC.dontTakeDamage && !NPC.IsABestiaryIconDummy)
            {
                float noiseScale = MathHelper.Lerp(0.4f, 0.8f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.5f + 0.5f);

                Effect shieldEffect = Scene["RoverDriveShield"].GetShader().Shader;
                shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.24f);
                shieldEffect.Parameters["blowUpPower"].SetValue(2.5f);
                shieldEffect.Parameters["blowUpSize"].SetValue(0.5f);
                shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

                float baseShieldOpacity = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f);
                shieldEffect.Parameters["shieldOpacity"].SetValue(baseShieldOpacity);
                shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f);

                Color blueTint = new Color(51, 102, 255);
                Color cyanTint = new Color(71, 202, 255);
                Color wulfGreen = new Color(194, 255, 67) * 0.8f;
                Color edgeColor = MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.2f, blueTint, cyanTint, wulfGreen);
                shieldEffect.Parameters["shieldColor"].SetValue(blueTint.ToVector3());
                shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shieldEffect, Main.GameViewMatrix.TransformationMatrix);

                if (NoiseTex == null)
                    NoiseTex = ModContent.Request<Texture2D>(AssetDirectory.Noise + "TechyNoise");

                float scale = 0.15f + 0.03f * (0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.5f));

                if (FireCooldown < 0)
                    scale *= 1f + 0.2f * MathF.Pow(Utils.GetLerpValue(-40, 0f, FireCooldown, true), 2f);
                else
                    scale *= 1f - 0.1f * MathF.Pow(Utils.GetLerpValue(15f, 0f, FireCooldown, true), 0.7f);

                Texture2D tex = NoiseTex.Value;
                Vector2 pos = NPC.Center + NPC.GfxOffY() - Main.screenPosition;

                Main.spriteBatch.Draw(tex, pos, null, Color.White, 0, tex.Size() / 2f, scale, 0, 0);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            }
            return false;
        }
        #endregion

        #region Unimportant stuff & spawn chance
        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
        public override bool? CanFallThroughPlatforms() => false;

        public override bool CheckActive()
        {
            if (Initialized == 0)
                return true;

            for (int i = 0; i < 255; i++)
            {
                if (!Main.player[i].active)
                    continue;

                if (Main.player[i].Distance(NPC.Center) > DESPAWN_RADIUS)
                    return true;
            }

            return false;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.PlayerSafe || !spawnInfo.Player.ZoneForest || !CanWulfrumNexusSpawnNaturally)
                return 0f;

            if (NPC.AnyNPCs(NPCType<WulfrumNexus>()))
                return 0f;

            Vector2 spawnPosition = new Vector2(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY).ToWorldCoordinates();

            foreach (var item in TileEntity.ByID)
            {
                if (item.Value.type == TileEntityType<WulfrumNexusSpawner>() && item.Value.Position.ToWorldCoordinates().Distance(spawnPosition) < SPAWN_RADIUS)
                {
                    return 0f;
                }
            }

            // Spawn less frequently in the inner third of the world.
            if (spawnInfo.PlayerFloorX > Main.maxTilesX * 0.333f && spawnInfo.PlayerFloorX < Main.maxTilesX - Main.maxTilesX * 0.333f)
                return SpawnCondition.OverworldDaySlime.Chance * (Main.hardMode ? 0.01f : 0.03f);

            return SpawnCondition.OverworldDaySlime.Chance * (Main.hardMode ? 0.020f : 0.05f);
        }

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

                    for (int i = 0; i < 5; i++)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumNexusGore" + i.ToString()).Type, 1f);
                    }


                    int randomGoreCount = Main.rand.Next(0, 2);
                    for (int i = 0; i < randomGoreCount; i++)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("WulfrumEnemyGore" + Main.rand.Next(1, 11).ToString()).Type, 1f);
                    }

                }
            }
        }

        public override void OnKill()
        {
            CanWulfrumNexusSpawnNaturally = true;

            //Remove the spawner entity
            if (AttachedTileEntityPos != Point16.Zero)
            {
                if (TileEntity.ByPosition.TryGetValue(AttachedTileEntityPos, out TileEntity te))
                {
                    (te as ModTileEntity).Kill(AttachedTileEntityPos.X, AttachedTileEntityPos.Y);
                    if (Main.netMode == NetmodeID.Server)
                        new KillTileEntityPacket(AttachedTileEntityPos).Send(runLocally: false);
                }
                else
                {
                    //Bizzare edge case where the TE exists in byvalue but not byposition. Probably solved when it happens during worldgen and not with debugitem but oh well
                    foreach (var teKeyPair in TileEntity.ByID)
                    {
                        if (teKeyPair.Value.Position == AttachedTileEntityPos)
                        {
                            ((ModTileEntity)teKeyPair.Value).OnKill();
                            TileEntity.ByID.Remove(teKeyPair.Value.ID);
                            if (Main.netMode == NetmodeID.Server)
                                new KillTileEntityPacket(AttachedTileEntityPos).Send(runLocally: false);
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(newWaveTimer);
        public override void ReceiveExtraAI(BinaryReader reader) => newWaveTimer = reader.ReadSingle();

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemType<WulfrumVoiceBox>(), new Fraction(20, 100));
            npcLoot.Add(ItemType<AbandonedWulfrumHelmet>(), new Fraction(5, 100));
            npcLoot.Add(ItemType<WulfrumMetalScrap>(), 1, 2, 3);
            npcLoot.Add(ItemType<EnergyCore>());
        }
    }

    [Serializable]
    public class WulfrumSquadSpawnEffectsPacket : Module
    {
        Vector2[] positions;

        public WulfrumSquadSpawnEffectsPacket(Vector2[] positions)
        {
            this.positions = positions;
        }

        protected override void Receive()
        {
            for (int i = 0; i < positions.Length; i++)
            {
                for (int iy = 0; iy < 16; iy++)
                {
                    Dust zapDust = Dust.NewDustPerfect(positions[i] + Main.rand.NextVector2Circular(1f, 1f) * 20f, 226, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(1f, 2.3f) - Vector2.UnitY * 6f);
                    zapDust.noGravity = true;
                }
            }
        }
    }

    public class WulfrumNexusSpawner : ModTileEntity
    {
        public Vector2 WorldPosition => Position.ToVector2() * 16;
        public static int VaultType;

        public override void Update()
        {
            CheckForNearbyPlayers();
        }

        public override bool IsTileValidForEntity(int x, int y) => Main.tile[x, y].TileType == VaultType;

        public void CheckForNearbyPlayers()
        {
            bool playerNear = false;
            bool checkForBunkerBounds = PointOfInterestMarkerSystem.WulfrumBunkerPos != Point.Zero;
            if (!checkForBunkerBounds)
                return;

            Rectangle bunkerBounds = PointOfInterestMarkerSystem.WulfrumBunkerRectangle;
            bunkerBounds.Inflate(-2, -2);
            bunkerBounds.Y += 12;

            bunkerBounds.X *= 16;
            bunkerBounds.Y *= 16;
            bunkerBounds.Width *= 16;
            bunkerBounds.Height *= 16;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.Distance(WorldPosition) < WulfrumNexus.SPAWN_RADIUS)
                {
                    if (checkForBunkerBounds && !player.Hitbox.Intersects(bunkerBounds))
                        continue;

                    playerNear = true;
                    break;
                }
            }

            if (!playerNear)
                return;


            int nexusType = NPCType<WulfrumNexus>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == nexusType && npc.Distance(WorldPosition) < 50)
                {
                    return;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC nexus = NPC.NewNPCDirect(new EntitySource_TileEntity(this), (int)WorldPosition.X + 40, (int)WorldPosition.Y + 8, nexusType, ai2: Position.X, ai3: Position.Y);

                new WulfrumSquadSpawnEffectsPacket([new Vector2((int)WorldPosition.X + 40, (int)WorldPosition.Y + 8)]).Send();
            }

            //Kill(Position.X, Position.Y);
        }

        public override void OnKill()
        {
            base.OnKill();
        }
    }

    public class WulfrumNexusShot : ModProjectile
    {
        internal PrimitiveTrail TrailDrawer;
        internal Color PrimColorMult = Color.White;
        public override string Texture => AssetDirectory.Invisible;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("energy bolt");

            ProjectileID.Sets.PlayerHurtDamageIgnoresDifficultyScaling[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            FablesSets.WulfrumProjectiles[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 140;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (Color.GreenYellow * 0.8f).ToVector3() * 0.5f);
            if (Projectile.timeLeft < 80)
                Projectile.velocity.Y += 0.2f;
            if (Projectile.velocity.Y > 16)
                Projectile.velocity.Y = 16;

            Projectile.velocity *= 0.983f;

            //Blast off.
            if (Projectile.timeLeft == 140)
            {
                SoundEngine.PlaySound(WulfrumProsthesis.ShootSound, Projectile.Center);
                Vector2 dustCenter = Projectile.Center + Projectile.velocity * 1f;

                for (int i = 0; i < 5; i++)
                {
                    Dust chust = Dust.NewDustPerfect(dustCenter, 15, Projectile.velocity.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.2f, 0.5f), Scale: Main.rand.NextFloat(1.2f, 1.8f));
                    chust.noGravity = true;
                }
            }

            if (Projectile.timeLeft <= 137)
            {
                if (Main.rand.NextBool(4))
                {
                    Vector2 dustCenter = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);

                    Dust chust = Dust.NewDustPerfect(dustCenter, 15, -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.5f), Scale: Main.rand.NextFloat(1.2f, 1.8f));
                    chust.noGravity = true;
                }

                if (Main.rand.NextBool(4))
                {
                    Vector2 dustCenter = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);

                    Dust largeDust = Dust.NewDustPerfect(dustCenter, 257, -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.4f), Scale: Main.rand.NextFloat(0.4f, 1f));
                    largeDust.noGravity = true;
                    largeDust.noLight = true;
                }

                if (Main.rand.NextBool(5))
                {
                    Vector2 center = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);

                    Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(MathHelper.Pi / 6f) * Main.rand.NextFloat(4, 10);
                    ParticleHandler.SpawnParticle(new TechyHoloysquareParticle(center, velocity, Main.rand.NextFloat(1f, 2f), Main.rand.NextBool() ? new Color(99, 255, 229) : new Color(25, 132, 247), 25));

                }
            }

            Projectile.rotation += 0.1f;

            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 dustPos = Projectile.Center + Projectile.rotation.ToRotationVector2() * 10f * i;

                Dust chmust = Dust.NewDustPerfect(dustPos, 163, -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.5f), Scale: Main.rand.NextFloat(0.6f, 1f));
                chmust.noGravity = true;
            }

            if (!Main.dedServ)
                ManageTrail();
        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction, new TriangularTip(8f));

            TrailDrawer.SetPositionsSmart(Projectile.oldPos.Reverse(), Projectile.position, FablesUtils.SmoothBezierPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.position + Projectile.velocity;
        }

        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = (float)Math.Sqrt(completionRatio);
            return (Color.DeepSkyBlue.MultiplyRGB(PrimColorMult) * fadeOpacity) with { A = 0 };
        }

        internal float WidthFunction(float completionRatio)
        {
            return 12.4f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;
            effect.Parameters["time"].SetValue(0f);
            effect.Parameters["fadeDistance"].SetValue(0.3f);
            effect.Parameters["fadePower"].SetValue(1 / 6f);
            effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);

            DrawChromaticAberration(Vector2.UnitX, 3.5f, delegate (Vector2 offset, Color colorMod) {
                PrimColorMult = colorMod;
                TrailDrawer?.Render(effect, Projectile.Size * 0.5f - Main.screenPosition + offset);
            });
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 13; i ++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 dustPos = Projectile.Center + offset * Main.rand.NextFloat(1f, 10f);
                Dust chmust = Dust.NewDustPerfect(dustPos, 163, Projectile.velocity * 0.2f  + offset * 3f, Scale: Main.rand.NextFloat(0.6f, 1f));
                chmust.noGravity = true;
            }

            SoundEngine.PlaySound(WulfrumProsthesis.HitSound, Projectile.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            return base.OnTileCollide(oldVelocity);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int numParticles = Main.rand.Next(4, 7);
            for (int i = 0; i < numParticles; i++)
            {
                Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(MathHelper.Pi / 6f) * Main.rand.NextFloat(3, 14);
                ParticleHandler.SpawnParticle(new TechyHoloysquareParticle(target.Center, velocity, Main.rand.NextFloat(2.5f, 3f), Main.rand.NextBool() ? new Color(99, 255, 229) : new Color(25, 132, 247), 25));
            }
        }
    }
}
