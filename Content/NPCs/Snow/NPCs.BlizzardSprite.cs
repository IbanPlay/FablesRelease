using CalamityFables.Content.NPCs.Sky;
using System.IO;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.Utilities;
using Terraria.DataStructures;
using Terraria.Localization;
using CalamityFables.Content.Items.Snow;
using CalamityFables.Particles;
using static CalamityFables.Helpers.FablesUtils;
using System.Security.Cryptography;

namespace CalamityFables.Content.NPCs.Snow
{
    public class BlizzardSprite : ModNPC
    {
        public override string Texture => AssetDirectory.SnowNPCs + Name;
        public static Asset<Texture2D> BlizzardSmokeTexture;
        public const int ITEM_CHANCE = 17;

        public Player target => Main.player[NPC.target];

        public static readonly SoundStyle HurtSound = new SoundStyle("CalamityFables/Sounds/BlizzardSpriteHurt", 2) { PitchVariance = 0.3f };
        public static readonly SoundStyle DeathSound = new SoundStyle("CalamityFables/Sounds/BlizzardSpriteDeath");

        public static readonly SoundStyle HeatHazeHurtSound = new SoundStyle("CalamityFables/Sounds/MirageSpriteHurt", 2) { PitchVariance = 0.3f };
        public static readonly SoundStyle HeatHazeDeathSound = new SoundStyle("CalamityFables/Sounds/MirageSpriteDeath");

        public static readonly SoundStyle WindupSound = new SoundStyle("CalamityFables/Sounds/BlizzardSpriteAttackCharge");
        public static readonly SoundStyle AchooSound = new SoundStyle("CalamityFables/Sounds/BlizzardSpriteAttack");



        public static int BannerType;
        public static AutoloadedBanner bannerTile;

        public ref float AICounter => ref NPC.ai[0];
        public ref float AttackCharge => ref NPC.ai[1];
        public bool DeathAnimation
        {
            get => NPC.ai[2] >= 1;
            set => NPC.ai[2] = value ? 1 : 0;
        }

        public float DeathAnimProgress => AICounter / 60f;

        public int HeldItemIndex
        {
            get => (int)NPC.ai[3];
            set => NPC.ai[3] = value;
        }

        public float ouchTimer;

        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Blizzard Sprite", AssetDirectory.Banners, out bannerTile);
            heatHazmeName = Mod.GetLocalization("Extras.HeatHazeSprite");
        }

        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                SpriteDirection = 1
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            Main.npcFrameCount[Type] = 15;
            bannerTile.NPCType = Type;
        }

        public override void SetDefaults()
        {
            NPC.noGravity = true;
            NPC.aiStyle = -1;
            NPC.damage = 13;
            NPC.width = 45;
            NPC.height = 60;
            NPC.defense = 5;
            NPC.lifeMax = 90;
            NPC.knockBackResist = 0.55f;
            NPC.value = Item.buyPrice(0, 0, 1, 15);
            NPC.HitSound = HurtSound;
            NPC.DeathSound = DeathSound;
            Banner = NPC.type;
            BannerItem = BannerType;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
        public override bool? CanFallThroughPlatforms() => true;
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Snow,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Events.Blizzard,

                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.BlizzardSprite")
            });
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (CalamityFables.SpiritEnabled && CalamityFables.SpiritReforged.TryFind("SaltBiome", out ModBiome saltFlats))
            {
                Player closest = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
                HeatHaze = closest.InModBiome(saltFlats);
                if (HeatHaze)
                {
                    NPC.HitSound = HeatHazeHurtSound;
                    NPC.DeathSound = HeatHazeDeathSound;
                    NPC.netUpdate = true;
                }
            }

            if (source is not EntitySource_Parent { Entity: Player })
            {
                int choice = 0;

                //Random 1.0 item (non prehardmode)
                if (Main.zenithWorld)
                    choice = Main.rand.Next(364);

                else if (Main.rand.NextBool(ITEM_CHANCE))
                    choice = ItemID.BlizzardinaBottle;
                else if (Main.rand.NextBool(ITEM_CHANCE))
                    choice = ModContent.ItemType<IceHat>();
                else if (Main.rand.NextBool(ITEM_CHANCE))
                    choice = ItemID.Bottle;

                HeldItemIndex = choice;
                NPC.netUpdate = true;
            }
        }

        #region Heat haze days 
        public bool HeatHaze = false;

        public static LocalizedText heatHazmeName;
        public override void ModifyTypeName(ref string typeName)
        {
            if (HeatHaze)
                typeName = heatHazmeName.Value;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(HeatHaze);
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            bool heatHaze = reader.ReadBoolean();
            if (!HeatHaze && heatHaze)
            {
                NPC.HitSound = HeatHazeHurtSound;
                NPC.DeathSound = HeatHazeDeathSound;
                HeatHaze = true;
            }
        }
        #endregion

        public override void AI()
        {
            mistCanvas?.KeepCanvasActive();
            ouchTimer -= 1 / (60f * 0.3f);

            Lighting.AddLight(NPC.Center, 0.5f, 0.6f, 0.6f);

            //Face the player
            if (target.dead || DeathAnimation)
            {
                NPC.TargetClosest(false);
                AttackCharge = 0;
            }

            NPC.spriteDirection = NPC.direction;

            if (NPC.collideX)
            {
                NPC.velocity.Y *= 1.2f;
                if (NPC.velocity.X == 0)
                    NPC.velocity.X = 1.3f * NPC.direction;
                else if (Math.Abs(NPC.velocity.X) < 1.3)
                    NPC.velocity.X = -1.3f * NPC.velocity.X.NonZeroSign();
            }

            NPC.TargetClosest(true);
            if (AttackCharge == 0)
            {
                //Sluggishly hover towards the player, accelerating fast when away
                if (Math.Abs(NPC.Center.X - target.Center.X) > 200)
                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, (target.Center.X - NPC.Center.X).NonZeroSign() * 2.5f, 0.02f);
                else if (NPC.velocity.X == 0)
                    NPC.velocity.X = NPC.direction * 0.2f;
                else if (Math.Abs(NPC.velocity.X) < 0.4f)
                    NPC.velocity.X *= 1.16f;
                
                //Anti crowding
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && i != NPC.whoAmI && n.type == NPC.type && NPC.DistanceSQ(n.Center) < 40f * 40f)
                    {
                        Vector2 awayFromNPC = (n.Center - NPC.Center);
                        awayFromNPC.Y *= 0.3f;
                        awayFromNPC = awayFromNPC.SafeNormalize(Vector2.UnitX);
                        NPC.velocity -= awayFromNPC * 0.2f;
                    }
                }


                if (NPC.Center.Y > target.Center.Y - 60)
                    NPC.velocity.Y -= 0.05f;
                else if (NPC.Center.Y < target.Center.Y - 230)
                {
                    NPC.velocity.Y += 0.08f;
                    if (NPC.velocity.Y > 4)
                        NPC.velocity.Y = 4;
                }
            }

            //Decide to summon icicles
            if (!DeathAnimation && AttackCharge == 0 && !target.dead && AICounter > 300 && NPC.WithinRange(target.Center, 600) && Collision.CanHitLine(target.position, target.width, target.height, NPC.position, NPC.width, NPC.height))
            {
                NPC.netUpdate = true;
                AICounter = 0;
                SoundEngine.PlaySound(WindupSound with { Volume = 0.6f }, NPC.Center);
                AttackCharge = 1f;
            }

            else if (AttackCharge > 0)
            {
                NPC.velocity.X *= 0.98f;
                NPC.velocity.Y *= 0.99f;


                float lastAttackCharge = AttackCharge;
                AttackCharge += 1 / (60f * 1.5f);

                float animProgressToAttack = 1.6f;

                if (AttackCharge < animProgressToAttack)
                    NPC.velocity.Y -= (AttackCharge - 1f) * 0.1f;

                //Releasing the whirlwind
                if (lastAttackCharge <= animProgressToAttack && AttackCharge > animProgressToAttack)
                {
                    SoundEngine.PlaySound(AchooSound, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        //Find ground
                        Point groundCheck = NPC.Center.ToTileCoordinates();

                        for (int i = 0; i < 40; i++)
                        {
                            Tile check = Main.tile[groundCheck];
                            if (check.IsTileSolidOrPlatform())
                                break;
                            groundCheck.Y++;
                        }

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), target.Center - Vector2.UnitY * 200f, Vector2.Zero, ModContent.ProjectileType<BlizzardSpriteIcicle>(), 15, 0, Main.myPlayer, 0f, 0f, HeatHaze ? 1 : 0);
                        AICounter = Main.rand.NextFloat(-300f, 0f);
                        NPC.netUpdate = true;
                    }
                    else
                        AICounter = -300;

                    sneezingClouds = true;
                    sneezed = true;

                    //Sneeze shoots upwards
                    NPC.velocity.Y = 2.5f;
                }

                //Slow down post sneeze
                if (AttackCharge > animProgressToAttack)
                    NPC.velocity.Y *= 0.97f;

                if (AttackCharge >= 2f)
                    AttackCharge = 0f;
            }

            if (DeathAnimation)
            {
                NPC.velocity *= 0.98f - 0.09f * DeathAnimProgress;

                if (Main.rand.NextFloat() < AICounter / 50f)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(15f, 15),
                        Main.rand.NextBool(5) ? 43 : DustID.Sandnado,
                        Main.rand.NextVector2Circular(2f, 2f),
                        Scale: Main.rand.NextFloat(1f, 2f));
                    d.noGravity = true;
                }
            }

            //Die!
            if (DeathAnimation && DeathAnimProgress >= 1f)
            {
                NPC.life = 0;
                NPC.dontTakeDamage = false;
                NPC.checkDead();
            }

            if (!Main.dedServ)
            {
                SpawnAndUpdateClouds();
            }
            AICounter++;
        }

        #region Drawing stuff
        public Rectangle eyeRealFrame = new Rectangle(0, 0, 30, 26);

        public override void FindFrame(int frameHeight)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                SpawnAndUpdateClouds();
            }

            mistCanvasPosition = NPC.Center;
            mistCanvasPosition.X = (int)(mistCanvasPosition.X / 4) * 4;
            mistCanvasPosition.Y = (int)(mistCanvasPosition.Y / 4) * 4;
            if (mistCanvas != null)
                mistCanvas.position = mistCanvasPosition;

            cloudCanvasPosition = NPC.Center;
            cloudCanvasPosition.X = (int)(mistCanvasPosition.X / 2) * 2;
            cloudCanvasPosition.Y = (int)(mistCanvasPosition.Y / 2) * 2;
            if (cloudTarget != null)
                cloudTarget.Position = cloudCanvasPosition;

            if (AttackCharge > 0f && !DeathAnimation)
            {
                int frameY = (int)((AttackCharge - 1f) * 15);
                frameY = Math.Min(frameY, 14);
                eyeRealFrame = new Rectangle(32, frameY * 28, 30, 26);
                if (HeatHaze)
                    eyeRealFrame.X += 94;
            }
            else if (ouchTimer > 0f || DeathAnimation)
            {
                eyeRealFrame = new Rectangle(64, 0, 30, 26);
                if (HeatHaze)
                    eyeRealFrame.X += 94;
            }
            else
            {
                NPC.frameCounter += 1 / 6f;
                if (NPC.frameCounter >= 1f)
                {
                    NPC.frameCounter = 0;
                    int frame = eyeRealFrame.Y / 28;
                    frame++;
                    frame %= 11;
                    eyeRealFrame = new Rectangle(0, frame * 28, 30, 26);

                    if (HeatHaze)
                        eyeRealFrame.X += 94;
                }
            }

            NPC.frame.Width = NPC.width;
            NPC.frame.Height = NPC.height;
        }

        public NavierStrokeCanvas mistCanvas;
        public Vector2 mistCanvasPosition;
        public BlizzardCloudRenderTarget cloudTarget;
        public Vector2 cloudCanvasPosition;
        public bool sneezingClouds;
        public int TicksSinceLastUsedRenderTargets { get; set; }
        public bool RenderTargetsDisposed { get; set; } = true;
        public int AutoDisposeTime => 120;

        public void SpawnAndUpdateClouds()
        {
            if (Main.dedServ)
                return;

            bool prebakeParticleSystem = false;

            if (cloudTarget == null)
            {
                cloudTarget = new BlizzardCloudRenderTarget(400);
                cloudTarget.PreUpdateParticlesEvent += TrimOffscreenParticlesAndAddSneezeVel;
                cloudTarget.PostUpdateParticlesEvent += ResetSneeze;

                if (NPC.IsABestiaryIconDummy)
                {
                    cloudTarget.NeedsManualParticleUpdate = true;
                    cloudTarget.useManualShaderTime = true;
                    prebakeParticleSystem = true;
                }
            }
            if (NPC.IsABestiaryIconDummy && !cloudTarget.Initialized)
                prebakeParticleSystem = true;

            if (prebakeParticleSystem)
                PrebakeCloudParticles();

            cloudTarget.HeatHaze = HeatHaze;

            if (!DeathAnimation)
            {
                int dustAmount = Main.rand.NextBool(2) ? 3 : 2;
                for (int i = 0; i < dustAmount; i++)
                {
                    Vector2 cloudPosition = NPC.Center - Vector2.UnitY * 5f + NPC.velocity * 1.5f;
                    cloudPosition += Main.rand.NextVector2Circular(16f, 22f);
                    Vector2 cloudVelocity = Main.rand.NextVector2Circular(1f, 1f) - Vector2.UnitY * 2f;
                    cloudVelocity *= Main.rand.NextFloat(0.1f, 0.3f);

                    BlizzardCloudParticle newCloud = new BlizzardCloudParticle(cloudPosition, cloudVelocity + NPC.velocity);
                    if (HeatHaze)
                        newCloud.frameSpeed -= 3;
                    cloudTarget.SpawnParticle(newCloud);
                }
            }

            if (NPC.IsABestiaryIconDummy)
            {
                cloudTarget.ManuallyUpdateTarget();
                cloudTarget.manualShaderTime += 1 /60f;
            }
        }

        public void PrebakeCloudParticles()
        {
            for (int l = 0; l < 30; l++)
            {
                int dustAmount = Main.rand.NextBool(2) ? 3 : 2;
                for (int i = 0; i < dustAmount; i++)
                {
                    Vector2 cloudPosition = NPC.Center - Vector2.UnitY * 5f + NPC.velocity * 1.5f;
                    cloudPosition += Main.rand.NextVector2Circular(16f, 22f);
                    Vector2 cloudVelocity = Main.rand.NextVector2Circular(1f, 1f) - Vector2.UnitY * 2f;
                    cloudVelocity *= Main.rand.NextFloat(0.1f, 0.3f);

                    BlizzardCloudParticle newCloud = new BlizzardCloudParticle(cloudPosition, cloudVelocity + NPC.velocity);
                    if (HeatHaze)
                        newCloud.frameSpeed -= 3;
                    cloudTarget.SpawnParticle(newCloud);
                }
                cloudTarget.ManuallyUpdateTarget();
            }
        }

        public void TrimOffscreenParticlesAndAddSneezeVel(RTParticle particle)
        {
            if (sneezingClouds)
                particle.Velocity += particle.Position.DirectionFrom(NPC.Center) * 2f * Utils.GetLerpValue(50f, 0f, particle.Position.Distance(NPC.Center), true);

            if (particle is BlizzardCloudParticle cloud)
                cloud.impartedRotation = NPC.velocity.X * -0.002f;

            //Trim outside of RT
            Vector2 drawPosition = (particle.Position - NPC.Center) / 2f + (cloudTarget.Size.ToVector2() / 2);
            if (drawPosition.X < 0 || drawPosition.X > cloudTarget.Size.X || drawPosition.Y < 0 || drawPosition.Y > cloudTarget.Size.Y)
                particle.Kill();
        }

        public void ResetSneeze() => sneezingClouds = false;

        #region Fluid mist
        public bool sneezed = false;

        private void InitializeMistCanvas()
        {
            mistCanvas = new NavierStrokeCanvas(new Point((int)(150), (int)(150)), new Point((int)(50), (int)(50)))
            {
                displacementMultiplier = 0.25f,
                densityDiffusion = 0.02f,
                velocityDiffusion = 0.02f,
                densityDissipation = 0.96f, //98
                velocityDissipation = 0.99f,
                projectionIterations = 20,
                advectionStrength = 0.9f,
                CellSizeMultiplier = 1f
            };

            mistCanvas.DrawDirectionalSourcesEvent += DrawMistSources;
            mistCanvas.DrawOmnidirectionalSourcesEvent += DrawOmniMistSources;
        }

        private void DrawMistCanvas(Vector2 screenPos)
        {
            if (!FablesConfig.Instance.FluidSimVFXEnabled)
                return;

            if (mistCanvas == null || mistCanvas.Canvas == null)
                return;

            mistCanvas.addedVelocity = Vector2.UnitX * Main.WindForVisuals * 0.1f;
            if (DeathAnimation)
            {
                mistCanvas.advectionStrength = 0.9f + 0.1f * AICounter / 90f;
                mistCanvas.densityDissipation = 0.96f - 0.1f * AICounter / 90f;
            }

            Effect blizzMistEffect = Scene["BlizzardMist"].GetShader().Shader;
            blizzMistEffect.Parameters["textureResolution"].SetValue(mistCanvas.Size * 2f);
            blizzMistEffect.Parameters["worldPos"].SetValue(mistCanvasPosition * 0.5f);
            float opacity = 1f;

            if (!HeatHaze)
            {
                blizzMistEffect.Parameters["baseColor"].SetValue(new Vector4(0.9f, 0.97f, 1.2f, 0.9f));
                opacity = 0.4f;
            }
            else
            {
                blizzMistEffect.Parameters["baseColor"].SetValue(new Vector4(0.7f, 0.46f, 0.1f, 0.2f));
                opacity = 0.3f;
            }

            if (DeathAnimation)
                opacity *= Utils.GetLerpValue(1f, 0.75f, DeathAnimProgress, true);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, blizzMistEffect, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(mistCanvas.Canvas, mistCanvasPosition - screenPos, null, Color.White * opacity, 0f, mistCanvas.Canvas.Size() / 2f, 4f, 0, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private void DrawMistSources(NavierStrokeCanvas canvas)
        {
            Vector2 center = mistCanvas.Size / 2f - Vector2.UnitY * 2f;

            if (justDied)
            {
                //POOF!
                canvas.DrawOnCanvas(0.2f, -Vector2.UnitY.RotatedByRandom(1f) * 5f, center, 13, true, 0f, Vector2.One);
            }

            if (DeathAnimation)
                return;

            Vector2 baseVel = Vector2.UnitX * Main.WindForVisuals * 0.03f - Vector2.UnitY * 0.02f;
            baseVel *= 0f;

            canvas.DrawOnCanvas(0.2f, (baseVel + Vector2.UnitY.RotatedBy(Main.GlobalTimeWrappedHourly) * 0.1f), center, 13, true, 0f, Vector2.One);
            canvas.DrawOnCanvas(0.1f, (baseVel - Vector2.UnitY.RotatedBy(Main.GlobalTimeWrappedHourly) * 0.1f), center, 11, true, 0f, Vector2.One);

            canvas.DrawOnCanvas(0.2f, baseVel, center + Vector2.UnitY * 3f, 13, false, 0f, Vector2.One);

            //NavierStrokeCanvas.DrawOnCanvas(0.0f, -Vector2.UnitY.RotatedByRandom(0.3f) * 0.3f, center, 6, false, 0f, Vector2.One);

            if (sneezed)
            {
                sneezed = false;
                for (int i = 0; i < 3; i++)
                {
                    canvas.DrawOnCanvas(1.0f, Vector2.UnitY * 4f, center, 7, false, 0f, Vector2.One);
                }
            }

            if (Main.rand.NextBool(9))
            {
                float rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 position = center + Vector2.UnitY.RotatedBy(rotation) * Main.rand.NextFloat(3f, 13f);
                Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 2f);

                canvas.DrawOnCanvas(1f, velocity, position, 6, true, 0f, Vector2.One);

                //NavierStrokeCanvas.DrawOnCanvas(0f, 1f, position, new Rectangle(0, 0, 1, 1), 0f, Vector2.One * 0.5f, Vector2.One);
            }
        }
        private void DrawOmniMistSources(NavierStrokeCanvas canvas)
        {
            if (DeathAnimation && !justDied)
                return;

            Vector2 center = mistCanvas.Size / 2f - Vector2.UnitY * 2f;
            canvas.DrawOnCanvas(0.2f, 0.1f, 0.2f, center, 8f);

            if (justDied)
            {
                justDied = false;

                //POOF!!!!
                canvas.DrawOnCanvas(-5.2f, 0f, 0.2f, center, 18f);
                canvas.DrawOnCanvas(0.0f, 60f, 0.2f, center + Main.rand.NextVector2Circular(6f, 6f), 16f);
                canvas.DrawOnCanvas(0.0f, 50f, 0.2f, center, 16f);
            }

            if (sneezed)
            {
                sneezed = false;
                for (int i = 0; i < 3; i++)
                {
                    canvas.DrawOnCanvas(-7.4f, 10f, 0.2f, center + Main.rand.NextVector2Circular(6f, 6f), 16f);
                }
            }
        }
        #endregion

        public Vector2 HeatHazeSunPosition => NPC.Center - Vector2.UnitY * 5f - Vector2.UnitX * NPC.velocity.X * 0.3f;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                screenPos.Y -= 10;
                //Because bestiary targets do not update automatically, we need to keep them alive through other means like that
                if (cloudTarget != null)
                    cloudTarget.TimeSinceLastUpdate = 0;
            }

            if (HeatHaze && eyeRealFrame.X < 94)
                FindFrame(2);

            if (!NPC.IsABestiaryIconDummy && FablesConfig.Instance.FluidSimVFXEnabled)
            {
                if (mistCanvas == null || mistCanvas.Disposed)
                    InitializeMistCanvas();
                DrawMistCanvas(screenPos);
            }

            if (HeatHaze && !DeathAnimation)
                CloudSprite.DrawCloudSpriteSun(screenPos, HeatHazeSunPosition, NPC.scale * 0.6f, 0.4f);


            if (HeldItemIndex != 0)
            {
                Texture2D heldItemTexture = TextureAssets.Item[HeldItemIndex].Value;
                Vector2 itemOffset = Vector2.UnitY * -5f;
                itemOffset.Y += MathF.Sin(Main.GlobalTimeWrappedHourly) * 7f;
                itemOffset.X += NPC.velocity.X * -2.6f;
                float itemRotation = NPC.velocity.X * 0.35f;
                float itemOpacity = 0.4f + (DeathAnimation ? DeathAnimProgress * 0.6f : 0f);

                if (HeldItemIndex == ModContent.ItemType<IceHat>())
                    itemOffset -= Vector2.UnitY.RotatedBy(itemRotation) * 20f;

                Main.spriteBatch.Draw(heldItemTexture, NPC.Center + itemOffset - screenPos, null, drawColor * itemOpacity, itemRotation, heldItemTexture.Size() / 2f, 1f, 0, 0f);
            }

            if (cloudTarget != null)
            {
                if (!cloudTarget.Initialized)
                    PrebakeCloudParticles();

                cloudTarget.DrawRenderTargetWithOffset(spriteBatch, screenPos, NPC.whoAmI, NPC.IsABestiaryIconDummy);
            }

            Texture2D eyeTex = TextureAssets.Npc[Type].Value;
            Vector2 eyePos = NPC.Center;
            drawColor = NPC.TintFromBuffAesthetic(drawColor);
            Rectangle eyeFrame = eyeRealFrame;

            if (ouchTimer > 0f)
                eyePos += Main.rand.NextVector2Circular(12f, 12f) * ouchTimer;
            if (DeathAnimation)
                eyePos += DeathAnimProgress * Main.rand.NextVector2Circular(15f, 15f);

            else if (AttackCharge > 1.6f)
                eyePos += Main.rand.NextVector2Circular(4f, 4f) * Utils.GetLerpValue(2f, 1.6f, AttackCharge, true);

            float eyeRotation = NPC.velocity.X * 0.1f;
            Vector2 eyeScale = new Vector2(NPC.scale);
            drawColor = Color.Lerp(drawColor, Color.White, 0.55f);
            Main.spriteBatch.Draw(eyeTex, eyePos - screenPos, eyeFrame, drawColor * (drawColor.A / 255f), eyeRotation, eyeFrame.Size() / 2f, eyeScale, 0, 0f);

            //eye shimmers
            if (HeatHaze)
            {
                Color shimmerColor = FablesUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.3f + NPC.whoAmI * 0.15f) % 1, Color.Orange, Color.YellowGreen);
                float offset = MathF.Sin(Main.GlobalTimeWrappedHourly * 0.3f) * Main.rand.NextFloat(1f, 2f) * 3f;

                for (int i = -1; i <= 1; i+= 2)
                {
                    Color color = shimmerColor;
                    if (i < 0)
                        color.R = 0;
                    else
                        color.B = 0;

                    color.A = 0;

                    Main.spriteBatch.Draw(eyeTex, eyePos + Vector2.UnitX * offset * i - screenPos, eyeFrame, color* 0.17f, eyeRotation, eyeFrame.Size() / 2f, eyeScale, 0, 0f);

                }
            }

            if (DeathAnimation)
            {
                eyeFrame.Y += 28;
                Main.spriteBatch.Draw(eyeTex, eyePos - screenPos, eyeFrame, Color.White * DeathAnimProgress, eyeRotation, eyeFrame.Size() / 2f, eyeScale, 0, 0f);
            }

            return false;
        }

        bool justDied = false;

        #endregion

        #region Spawn, Hit, death, and loot
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.PlayerInTown || spawnInfo.Water || spawnInfo.Invasion || ((Main.bloodMoon || Main.pumpkinMoon || Main.snowMoon) && !Main.dayTime) || (Main.eclipse && Main.dayTime) || spawnInfo.Player.ZoneOldOneArmy)
                return 0f;

            if (Framing.GetTileSafely(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY).WallType != WallID.None || spawnInfo.Player.ZoneRockLayerHeight || spawnInfo.Player.ZoneDirtLayerHeight)
                return 0f;

            int activeBlizzSprites = NPC.CountNPCS(Type);
            if (activeBlizzSprites >= 5)
                return 0f;

            if (CalamityFables.SpiritEnabled && CalamityFables.SpiritReforged.TryFind("SaltBiome", out ModBiome saltFlats))
            {
                if (spawnInfo.Player.InModBiome(saltFlats))
                    return 0.08f;
            }

            if (spawnInfo.Player.ZoneSnow)
            {
                //Mirage sprite needs to spawn in open air
                float baseChance = SpawnCondition.OverworldDaySlime.Chance;
                if (!Main.IsItDay())
                    baseChance = SpawnCondition.OverworldNightMonster.Chance * 0.8f;

                if (Main.cloudAlpha > 0f)
                    return baseChance * 0.55f;
                else
                    return baseChance * 0.05f;
            }
            return 0f;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            ouchTimer = 1f;
            if (NPC.life <= 0 && HeatHaze && !DeathAnimation)
            {
                for (int i = 0; i < 13; i++)
                {
                    Vector2 dustDirection = Main.rand.NextVector2Circular(1f, 1f);
                    Dust d = Dust.NewDustPerfect(HeatHazeSunPosition + Main.rand.NextVector2Circular(10f, 10f), DustID.SolarFlare, dustDirection * Main.rand.NextFloat(4f, 7f), 0, Color.White);
                    d.scale = Main.rand.NextFloat(0.9f, 1.9f);
                    d.noGravity = true;
                }
            }
        }

        public override bool CheckDead()
        {
            if (!DeathAnimation)
            {
                justDied = true;
                NPC.dontTakeDamage = true;
                DeathAnimation = true;
                NPC.life = 1;
                NPC.netUpdate = true;
                AICounter = 0;
                return false;
            }

            return true;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            var snowScatterPattern = new DropOneByOne.Parameters()
            {
                ChanceNumerator = 1,
                ChanceDenominator = 1,
                MinimumStackPerChunkBase = 1,
                MaximumStackPerChunkBase = 1,
                MinimumItemDropsCount = 4,
                MaximumItemDropsCount = 10,
                BonusMinDropsPerChunkPerPlayer = 0
            };

            LeadingConditionRule notHeatHaze = new LeadingConditionRule(new NotHeatHazeSpriteCondition());
            notHeatHaze.Add(new DropOneByOne(ItemID.Snowball, snowScatterPattern));

            if (CalamityFables.SpiritEnabled && CalamityFables.SpiritReforged.TryFind("SaltBlockReflectiveItem", out ModItem salt))
            {
                notHeatHaze.OnFailedConditions(new DropOneByOne(salt.Type, snowScatterPattern), true);
            }

            npcLoot.Add(notHeatHaze);
            npcLoot.Add(new BlizzardSpriteContentDropRule());
        }

        public class NotHeatHazeSpriteCondition : IItemDropRuleCondition
        {
            public bool CanDrop(DropAttemptInfo info) => info.npc.ModNPC is BlizzardSprite blizz && !blizz.HeatHaze;
            public bool CanShowItemDropInUI() => true;
            public string GetConditionDescription() => "";
        }
        #endregion
    }

    public class BlizzardSpriteContentDropRule : IItemDropRule
    {
        public List<IItemDropRuleChainAttempt> ChainedRules { get; private set; }

        public BlizzardSpriteContentDropRule()
        {
            ChainedRules = new List<IItemDropRuleChainAttempt>();
        }

        public bool CanDrop(DropAttemptInfo info)
        {
            if (info.npc.ai[3] > 0f)
                return info.npc.ai[3] < ItemLoader.ItemCount;

            return false;
        }

        public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
        {
            int itemId = (int)info.npc.ai[3];
            if (itemId == ItemID.Bottle)
                itemId = ModContent.ItemType<BlizzardSpriteInABottle>();
            CommonCode.DropItem(info, itemId, 1);
            ItemDropAttemptResult result = default(ItemDropAttemptResult);
            result.State = ItemDropAttemptResultState.Success;
            return result;
        }

        public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
        {
            float successChance = 1f / (float)BlizzardSprite.ITEM_CHANCE;
            float failChance = (BlizzardSprite.ITEM_CHANCE - 1) / (float)BlizzardSprite.ITEM_CHANCE;

            float magicHatChance = failChance * successChance;
            float vanityDropRate = failChance * failChance * successChance;

            //Fake a report
            drops.Add(new DropRateInfo(ItemID.BlizzardinaBottle, 1, 1, successChance));
            drops.Add(new DropRateInfo(ModContent.ItemType<IceHat>(), 1, 1, magicHatChance));
            drops.Add(new DropRateInfo(ModContent.ItemType<BlizzardSpriteInABottle>(), 1, 1, vanityDropRate));

            Chains.ReportDroprates(ChainedRules, 1f, drops, ratesInfo);
        }
    }

    public class BlizzardSpriteIcicle : ModProjectile
    {
        public static readonly SoundStyle CondensationSound = new(SoundDirectory.Sounds + "BlizzardSpriteCondensation");
        public static readonly SoundStyle FreezeInSound = new(SoundDirectory.Sounds + "BlizzardSpriteIcicleForm");
        public static readonly SoundStyle BreakSound = new(SoundDirectory.Sounds + "BlizzardSpriteIceBreak", 2);


        public override string Texture => AssetDirectory.SnowNPCs + Name;

        public static float ChargeTime = 60;

        public bool ChargingUp => Projectile.ai[0] < AppearDelay + ChargeTime;
        public bool NotAppeared => Projectile.ai[0] < AppearDelay;
        public float ChargeTimer => Utils.GetLerpValue(AppearDelay, AppearDelay + ChargeTime, Projectile.ai[0], true);

        public ref float AITimer => ref Projectile.ai[0];
        public float AppearDelay => Projectile.ai[1];
        public bool Saltcicle => Projectile.ai[2] != 0;

        public Vector2 CenterOfMass => Projectile.Center - Vector2.UnitY * 32f;

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 106;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }

        public override bool? CanDamage() => ChargingUp ? false : null;

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = true;
            hitboxCenterFrac = new Vector2(0.5f, 0.5f);
            height = 70;


            return true;
        }

        public override bool ShouldUpdatePosition() => !ChargingUp;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Chilled, 60 * 5, false);
        }

        private bool playedCondensationSound;
        private bool playedAppearSound;

        public override void AI()
        {
            if (NotAppeared)
                return;

            if (!playedCondensationSound)
            {
                SoundEngine.PlaySound(CondensationSound, CenterOfMass);
                playedCondensationSound = true;
            }

            AITimer++;
            canvasPosition = Projectile.Center;
            canvasPosition.X = (int)(canvasPosition.X / 2) * 2;
            canvasPosition.Y = (int)(canvasPosition.Y / 2) * 2;

            if (!Main.dedServ)
                SpawnAndUpdateClouds();
            if (cloudTarget != null)
                cloudTarget.Position = canvasPosition;

            if (ChargingUp)
                return;

            if (!playedAppearSound)
            {
                SoundEngine.PlaySound(FreezeInSound, CenterOfMass);
                playedAppearSound = true;
            }

            if (AITimer == ChargeTime + AppearDelay)
            {
                for (int i = 0; i < 20; i++)
                {
                    Vector2 position = CenterOfMass + Vector2.UnitY * Main.rand.NextFloat(60f);
                    Dust d = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(5f, 5),
                        Main.rand.NextBool(5) ? 43 : DustID.Sandnado,
                        Main.rand.NextVector2Circular(5f, 5f),
                        Scale: Main.rand.NextFloat(1f, 3f));
                    d.velocity.X += (d.position.X - CenterOfMass.X) * 0.5f;

                    d.noGravity = true;
                }
            }

            Projectile.tileCollide = true;
            Projectile.velocity.Y += 0.2f;
        }

        public BlizzardCloudRenderTarget cloudTarget;
        public Vector2 canvasPosition;

        public void SpawnAndUpdateClouds()
        {
            if (Main.dedServ)
                return;

            if (cloudTarget == null)
                cloudTarget = new BlizzardCloudRenderTarget(400);
            cloudTarget.HeatHaze = Saltcicle;

            if (ChargingUp)
            {
                int dustAmount = Main.rand.NextBool(2) ? 0 : 1;
                for (int i = 0; i < dustAmount; i++)
                {
                    float distanceMultiplier = 1f - 0.3f * ChargeTimer;

                    Vector2 usedCenter = CenterOfMass + Vector2.UnitY * Main.rand.NextFloat(0f, 50f);
                    Vector2 cloudPosition = usedCenter;
                    cloudPosition += Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(30f, 60f) * distanceMultiplier;
                    Vector2 cloudVelocity = cloudPosition.SafeDirectionTo(usedCenter);
                    cloudVelocity *= Main.rand.NextFloat(0.3f, 2.3f);
                    BlizzardCloudParticle newCloud = new BlizzardCloudParticle(cloudPosition, cloudVelocity);
                    cloudTarget.SpawnParticle(newCloud);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 dustPosition = Projectile.Bottom;
                Point tilePosition = dustPosition.ToSafeTileCoordinates();
                int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

                Dust dust = Main.dust[dustIndex];
                dust.position = dustPosition + (Vector2.UnitX * (Main.rand.NextFloat(Projectile.width) - Projectile.width / 2));
                dust.velocity.Y -= Main.rand.NextFloat(1.5f, 3f);
                dust.velocity.X *= 0.5f;
                dust.noLightEmittence = true;
            }

            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(BreakSound, Projectile.Center);

            for (int i = 0; i < 10; i++)
            {
                Color? colorOverride = Saltcicle ? Main.hslToRgb(Main.rand.NextFloat(0.52f, 0.86f), 0.74f, 0.84f) with { A = 190 } : null;
                Color? colorOutlineOverride = Saltcicle ? new Color(20, 20, 30, 0) : null;

                Vector2 position = CenterOfMass + Vector2.UnitY * i / 10f * 70f + Main.rand.NextVector2Circular(20f, 10f);

                Particle sharticle = new IceShardParticle(position, Main.rand.NextVector2Circular(4f, 4.5f) - Vector2.UnitY * 1.2f, Main.rand.NextFloat(1f, 1.6f), Main.rand.Next(30, 70), colorOverride, colorOutlineOverride);
                sharticle.Velocity.Y -= 4f;
                ParticleHandler.SpawnParticle(sharticle);
            }

            if (cloudTarget == null)
                return;

            canvasPosition = Projectile.Center;
            canvasPosition.X = (int)(canvasPosition.X / 2) * 2;
            canvasPosition.Y = (int)(canvasPosition.Y / 2) * 2;
            cloudTarget.Position = canvasPosition;

            for (int i = 0; i < 20; i++)
            {
                Vector2 usedCenter = Projectile.Bottom - Vector2.UnitY * Main.rand.NextFloat(0f, 30f);
                Vector2 cloudPosition = usedCenter;
                cloudPosition -= Vector2.UnitY.RotatedByRandom(1.2f) * Main.rand.NextFloat(2f, 8f);
                Vector2 cloudVelocity = usedCenter.SafeDirectionTo(cloudPosition);
                cloudVelocity *= Main.rand.NextFloat(0.3f, 2.3f);

                BlizzardCloudParticle newCloud = new BlizzardCloudParticle(cloudPosition, cloudVelocity, Main.rand.NextFloat(1.4f, 2f));
                cloudTarget.SpawnParticle(newCloud);
            }

            cloudTarget.GoUndead(DrawhookLayer.AboveTiles);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (NotAppeared)
                return false;

            if (!ChargingUp)
            {
                Texture2D tex = TextureAssets.Projectile[Type].Value;
                Rectangle frame = tex.Frame(3, 2, Saltcicle ? 1 : 0, 0, -2, -2);
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, lightColor * 0.6f, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0, 0);

                frame.Y = tex.Height / 2;
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0, 0);


                frame.X = tex.Width / 3 * 2;
                float sheenOpacity = Utils.GetLerpValue(ChargeTime + AppearDelay + 30f, ChargeTime + AppearDelay, AITimer, true);
                sheenOpacity = MathF.Pow(sheenOpacity, 3f);
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, Color.White * sheenOpacity, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0, 0);
            
                for (int i = -1; i <= 1; i+= 2)
                {
                    float offset = sheenOpacity * 4f;
                    Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + Vector2.UnitX * i * offset, frame, Color.White * sheenOpacity * 0.4f, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0, 0);

                }
            }

            else
            {
                Texture2D bloom = AssetDirectory.CommonTextures.BloomCircleTransparent.Value;
                float lensFlareOpacity = MathF.Pow(ChargeTimer, 0.8f);
                float sheenScale = MathF.Pow(ChargeTimer, 0.3f);

                float antiBloomOpacity = MathF.Pow(1 - ChargeTimer, 0.4f) * MathF.Pow(ChargeTimer, 0.3f);

                Main.spriteBatch.Draw(bloom, CenterOfMass - Main.screenPosition, null, Color.Black * 0.3f * antiBloomOpacity, 0f, bloom.Size() / 2f, sheenScale * 1.2f, 0, 0);
                Main.spriteBatch.Draw(bloom, CenterOfMass - Main.screenPosition, null, Color.Black * 0.6f * antiBloomOpacity, 0f, bloom.Size() / 2f, sheenScale * 1.4f, 0, 0);
                
                bloom = AssetDirectory.CommonTextures.BloomCircle.Value;
                Main.spriteBatch.Draw(bloom, CenterOfMass - Main.screenPosition, null, Color.CornflowerBlue with { A = 0 } * 0.3f * lensFlareOpacity, 0f, bloom.Size() / 2f, sheenScale * 0.5f, 0, 0);


                DrawLensFlare(MathHelper.PiOver2, lensFlareOpacity, 1f - MathF.Pow(ChargeTimer, 1.5f) * 0.6f, 1f + MathF.Pow(ChargeTimer, 2f) * 1.6f);
                DrawLensFlare(0f, lensFlareOpacity, 1f + MathF.Pow(ChargeTimer, 1.5f) * 3.3f, 1f - MathF.Pow(ChargeTimer, 2.5f) * 0.9f);
            }

            cloudTarget?.DrawRenderTargetWithOffset(Main.spriteBatch, Main.screenPosition, Projectile.whoAmI);
            return false;
        }

        public void DrawLensFlare(float rotation, float opacity, float thickness, float length)
        {
            Texture2D flareTex = AssetDirectory.CommonTextures.BloomStreak.Value;
            Vector2 center = CenterOfMass - Main.screenPosition;

            float sheenScale = MathF.Pow(ChargeTimer, 0.2f);

            Vector2 flareOrigin = flareTex.Size() / 2f;

            Main.spriteBatch.Draw(flareTex, center, null, Color.White with { A = 0 } * 0.7f * opacity, rotation, flareOrigin, new Vector2(0.4f * thickness, length) * sheenScale, 0, 0);
            Main.spriteBatch.Draw(flareTex, center, null, Color.CornflowerBlue with { A = 0 } * 0.4f * opacity, rotation, flareOrigin, new Vector2(thickness, length) * sheenScale, 0, 0);
            Main.spriteBatch.Draw(flareTex, center, null, Color.CornflowerBlue with { A = 0 } * 0.2f * opacity, rotation, flareOrigin, new Vector2(thickness, length) * sheenScale * 1.2f, 0, 0);
        }
    }

    public class BlizzardCloudParticle : RTParticle
    {
        public override string Texture => AssetDirectory.SnowNPCs + "BlizzardSpriteSmoke";
        public override AssetRequestMode TextureRequestMode => AssetRequestMode.ImmediateLoad;

        public int frameCount;
        public int frameSpeed;
        public float impartedRotation;

        public BlizzardCloudParticle(Vector2 position, Vector2 velocity, float scale = 1.4f)
        {
            Position = position;
            Velocity = velocity;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);

            frameSpeed = Main.rand.Next(11, 14); //11, 12
            frameCount = 0;
            Frame.X = 0;
            Frame.Y = Main.rand.Next(5);

            Scale = scale;
        }

        public override void Update()
        {
            base.Update();
            Position.Y -= 0.2f;
            Velocity *= 0.97f;
            Rotation += impartedRotation;
            impartedRotation = 0;

            //Animate clouds and despawn the old ones
            frameCount++;
            if (frameCount > frameSpeed)
            {
                frameCount = 0;
                Frame.X++;
                if (Frame.X >= 8)
                    Kill();
            }
        }

        public override void DrawParticle(SpriteBatch spriteBatch, Vector2 basePosition, Color passColor)
        {
           // spriteBatch.Draw(texture, (Position / 2) - basePosition, frame, color, Rotation, frame.Size() / 2, Scale * 0.5f, effect, 0);

            Texture2D texture = ParticleTexture;
            Rectangle frame = texture.Frame(8, 5, Frame.X, Frame.Y);
            Vector2 drawPosition = Position - basePosition;
            drawPosition /= 2;

            drawPosition += AssignedTarget.Origin;

            //The particle texture is 1x1 so we dont need to cut the scale in half
            float scale = Scale; 

            //For the non-fullscreen autotarget
            if (AssignedTarget != null && !AssignedTarget.AutoTarget)
            {
                Vector2 targetSize = AssignedTarget.Size.ToVector2();
                float trimDistance = 30f;
                scale *= Utils.GetLerpValue(0f, trimDistance, drawPosition.X, true) * Utils.GetLerpValue(targetSize.X, targetSize.X - trimDistance, drawPosition.X, true);
                scale *= Utils.GetLerpValue(0f, trimDistance, drawPosition.Y, true) * Utils.GetLerpValue(targetSize.Y, targetSize.Y - trimDistance, drawPosition.Y, true);
            }

            Color usedColor = passColor;
            if (passColor != Color.Black)
                usedColor = Color.Lerp(Color.White, Color.Black, Utils.GetLerpValue(4, 8, Frame.X + (frameCount / (float)frameSpeed), true) * 0.7f);
            spriteBatch.Draw(texture, drawPosition, frame, usedColor, Rotation, frame.Size() / 2f, scale, 0, 0);
        }
    }

    public class BlizzardCloudRenderTarget : ParticleRenderTarget
    {
        public MergeBlendTextureContent CloudRT;
        public Vector2 targetPosition;
        public bool HeatHaze;
        public override bool RegisterOnLoad => false;

        public bool useManualShaderTime = false;
        public float manualShaderTime = 0f;

        public BlizzardCloudRenderTarget()
        {
            Scale = 2f;
            Size = new Point(Size.X / 2, Size.Y / 2);
        }

        public BlizzardCloudRenderTarget(int size)
        {
            Scale = 2f;
            Size = new Point(size, size);
            Origin = new Vector2(size, size) / 2;
        }

        public override void Initialize()
        {
            CloudRT = new MergeBlendTextureContent(DrawBlizzardClouds, Size.X, Size.Y, false, false);
            Main.ContentThatNeedsRenderTargets.Add(CloudRT);
        }

        public override void Dispose()
        {
            CloudRT?.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(CloudRT);
            CloudRT.GetTarget()?.Dispose();
        }

        public void DrawBlizzardClouds(SpriteBatch spriteBatch, bool backgroundPass)
        {
            Color color = backgroundPass ? Color.Black : Color.White;
            Vector2 basePos = AutoTarget ? Main.screenPosition : Position;

            foreach (RTParticle particle in AssignedParticles)
            {
                particle.DrawParticle(spriteBatch, basePos, color);
            }
        }

        public override void DrawRenderTarget(SpriteBatch spritebatch, DrawhookLayer layer = DrawhookLayer.AbovePlayer, Rectangle? source = null) => DrawRenderTargetWithOffset(spritebatch, Vector2.Zero);

        public void DrawRenderTargetWithOffset(SpriteBatch spritebatch, Vector2 screenPos, int entityID = 0, bool bestiaryDraw = false)
        {
            if (CloudRT == null)
                return;
            CloudRT.Request();
            if (!CloudRT.IsReady)
                return;

            //Undead targets from icicles are always using a screen offset
            if (IsUndead)
                screenPos = Main.screenPosition;

            Vector2 drawPosition = AutoTarget ? Vector2.Zero : Position - screenPos;
            float shaderTime = useManualShaderTime ? manualShaderTime : Main.GlobalTimeWrappedHourly;

            RenderTarget2D target = CloudRT.GetTarget();
            Effect blizzMistEffect = Scene["BlizzardSmoke"].GetShader().Shader;
            blizzMistEffect.Parameters["textureResolution"].SetValue(target.Size());
            blizzMistEffect.Parameters["worldPos"].SetValue(drawPosition * 0.5f);

            blizzMistEffect.Parameters["displacementMap"].SetValue(AssetDirectory.NoiseTextures.DisplaceSmall.Value);
            blizzMistEffect.Parameters["time"].SetValue(shaderTime);

            if (HeatHaze)
            {
                Color shimmerColor = FablesUtils.MulticolorLerp((shaderTime * 0.3f + entityID * 0.25f) % 1, Color.Orange, Color.YellowGreen);
                Vector4 hazeColor = shimmerColor.ToVector4() * 0.5f;
                hazeColor.W = 0.2f;

                hazeColor *= 0.6f + 0.4f * MathF.Sin(shaderTime * 0.56f + entityID);

                blizzMistEffect.Parameters["baseColor"].SetValue(hazeColor);
                blizzMistEffect.Parameters["outlineColor"].SetValue(new Vector4(0.2f, 0.1f, 0.1f, 0f));
                blizzMistEffect.Parameters["displaceMapStrenght"].SetValue(0.03f);
            }
            else
            {
                blizzMistEffect.Parameters["baseColor"].SetValue(new Vector4(0.3f, 0.4f, 0.45f, 0.4f));
                blizzMistEffect.Parameters["outlineColor"].SetValue(new Vector4(0.3f, 0.3f, 0.3f, 0.1f));
                blizzMistEffect.Parameters["displaceMapStrenght"].SetValue(0.01f);

                if (bestiaryDraw)
                    blizzMistEffect.Parameters["baseColor"].SetValue(new Vector4(0.4f, 0.5f, 0.55f, 0.4f));
            }

            RasterizerState priorRasterizer = spritebatch.GraphicsDevice.RasterizerState;
            Rectangle priorScissorRectangle = spritebatch.GraphicsDevice.ScissorRectangle;
            spritebatch.End();
            spritebatch.GraphicsDevice.RasterizerState = priorRasterizer;
            spritebatch.GraphicsDevice.ScissorRectangle = priorScissorRectangle;
            spritebatch.GraphicsDevice.RasterizerState.ScissorTestEnable = bestiaryDraw;
            spritebatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, priorRasterizer, blizzMistEffect, Main.GameViewMatrix.TransformationMatrix);
            spritebatch.Draw(target, drawPosition, null, Color.White * 0.6f, 0f, Origin, Scale, 0, 0f);

            //Diffuse shimmer
            if (HeatHaze)
            {
                for (int i = -3; i <= 3; i++)
                {
                    if (i == 0)
                        continue;

                    float opacity = 1 - MathF.Abs(i) / 6f;
                    opacity *= 0.15f + 0.05f * MathF.Sin(Main.GlobalTimeWrappedHourly);
                    spritebatch.Draw(target, drawPosition + Vector2.UnitX * i * 2f, null, Color.White * opacity, 0f, Origin, Scale, 0, 0f);
                }
            }

            spritebatch.End();
            spritebatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, priorRasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            spritebatch.GraphicsDevice.ScissorRectangle = priorScissorRectangle;
            spritebatch.GraphicsDevice.RasterizerState.ScissorTestEnable = bestiaryDraw;
        }


        public void DrawRenderTargetForPlayerLayer(SpriteBatch spritebatch, float heatHaze, int entityID = 0)
        {
            CloudRT.Request();
            if (!CloudRT.IsReady)
                return;

            RenderTarget2D target = CloudRT.GetTarget();
            Effect blizzMistEffect = Scene["BlizzardSmoke"].GetShader().Shader;
            blizzMistEffect.Parameters["textureResolution"].SetValue(target.Size());
            blizzMistEffect.Parameters["worldPos"].SetValue(Position * 0.5f);

            blizzMistEffect.Parameters["displacementMap"].SetValue(AssetDirectory.NoiseTextures.DisplaceSmall.Value);
            blizzMistEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Vector4 baseColor = new Vector4(0.3f, 0.4f, 0.45f, 0.4f);
            Vector4 outlineColor = new Vector4(0.3f, 0.3f, 0.3f, 0.1f);
            float displaceStrength = 0.01f;

            if (heatHaze > 0f)
            {
                Color shimmerColor = FablesUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.3f + entityID * 0.25f) % 1, Color.Orange, Color.YellowGreen);
                Vector4 hazeColor = shimmerColor.ToVector4() * 0.5f;
                hazeColor.W = 0.2f;
                hazeColor *= 0.6f + 0.4f * MathF.Sin(Main.GlobalTimeWrappedHourly * 0.56f + entityID);
                baseColor = Vector4.Lerp(baseColor, hazeColor, heatHaze);
                outlineColor = Vector4.Lerp(outlineColor, new Vector4(0.2f, 0.1f, 0.1f, 0f), heatHaze);
                displaceStrength = MathHelper.Lerp(displaceStrength, 0.03f, heatHaze);
            }

            blizzMistEffect.Parameters["baseColor"].SetValue(baseColor);
            blizzMistEffect.Parameters["outlineColor"].SetValue(outlineColor);
            blizzMistEffect.Parameters["displaceMapStrenght"].SetValue(displaceStrength);


            spritebatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, blizzMistEffect, Matrix.Identity);
            spritebatch.Draw(target, Vector2.Zero, null, Color.White * 0.6f, 0f, Vector2.Zero, 2f, 0, 0f);

            //Diffuse shimmer
            if (heatHaze > 0f)
            {
                for (int i = -3; i <= 3; i++)
                {
                    if (i == 0)
                        continue;
                    float opacity = 1 - MathF.Abs(i) / 6f;
                    opacity *= 0.15f + 0.05f * MathF.Sin(Main.GlobalTimeWrappedHourly);
                    spritebatch.Draw(target, Vector2.UnitX * i * 2f, null, Color.White * opacity * heatHaze, 0f, Vector2.Zero, 2f, 0, 0f);
                }
            }

            spritebatch.End();
        }
    }
}


