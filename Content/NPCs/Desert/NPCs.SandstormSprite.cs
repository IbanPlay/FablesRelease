using CalamityFables.Content.Items.EarlyGameMisc;
using CalamityFables.Content.Items.Sky;
using CalamityFables.Content.NPCs.Sky;
using System.IO;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.Utilities;
using CalamityFables.Content.Items.EarlyGameMisc;
using static CalamityFables.Helpers.FablesUtils;
using Terraria.DataStructures;
using static CalamityFables.Content.Items.BurntDesert.DustDevil;
using static CalamityFables.Content.NPCs.Desert.SandstormSprite;
using Terraria.Localization;
using CalamityFables.Content.Items.BurntDesert;
using ReLogic.Utilities;


namespace CalamityFables.Content.NPCs.Desert
{
    public class SandstormSprite : ModNPC, ITemporaryRenderTargetHolder
    {
        public override string Texture => AssetDirectory.DesertNPCs + Name;
        public Player target => Main.player[NPC.target];

        public const int ITEM_CHANCE = 17;

        public static readonly SoundStyle HitSound = new SoundStyle("CalamityFables/Sounds/SandstormSpriteHurt", 2) { PitchVariance = 0.3f };
        public static readonly SoundStyle DeathSound = new SoundStyle("CalamityFables/Sounds/SandstormSpriteDeath");
        public static readonly SoundStyle WindupSound = new SoundStyle("CalamityFables/Sounds/SandstormSpriteAttackCharge");
        public static readonly SoundStyle PunchSound = new SoundStyle("CalamityFables/Sounds/DesertProwlerJump");

        public static int BannerType;
        public static AutoloadedBanner bannerTile;

        public ref float AICounter => ref NPC.ai[0];
        public ref float DustDevilSummonCharge => ref NPC.ai[1];
        public bool DyingAnimation
        {
            get => NPC.ai[2] >= 1;
            set => NPC.ai[2] = value ? 1 : 0;
        }
        public int HeldItemIndex
        {
            get => (int)NPC.ai[3];
            set => NPC.ai[3] = value;
        }


        public float spinDirection = 1f;
        public float spinMomentum;

        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Sandstorm Sprite", AssetDirectory.Banners, out bannerTile);
        }

        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                SpriteDirection = 1
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            Main.npcFrameCount[Type] = 16;
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
            NPC.HitSound = HitSound;
            NPC.DeathSound = DeathSound;
            Banner = NPC.type;
            BannerItem = BannerType;

            cloudTintVariant = Main.rand.Next(3);
        }

        /// <summary>
        /// Position of the sprite's eye in the world
        /// </summary>
        public Vector2 EyeWorldPosition
        {
            get
            {
                Vector2 eyeRotationVector = eyeRotation.ToRotationVector2();
                eyeRotationVector.Y = 0;
                float eyeRotationStrength = 0.15f + spinMomentum * 0.2f;
                float eyeAxisDistance = 20f + MathF.Pow(Math.Max(spinMomentum, 0f), 1.2f) * 12f;

                eyeRotationVector = eyeRotationVector.RotatedBy(MathF.Sin(eyeRotation * 0.5f + Main.GlobalTimeWrappedHourly * 0.04f) * eyeRotationStrength);
                return NPC.Center + Vector2.UnitY * 9f + eyeRotationVector * eyeAxisDistance;
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
        public override bool? CanFallThroughPlatforms() => true;
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Events.Sandstorm,

                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.SandstormSprite")
            });
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (source is not EntitySource_Parent { Entity: Player })
            {
                int choice = 0;

                //Random 1.0 item (non prehardmode)
                if (Main.zenithWorld)
                    choice = Main.rand.Next(364);

                else if (Main.rand.NextBool(ITEM_CHANCE))
                    choice = ItemID.SandstorminaBottle;

                else if (Main.rand.NextBool(ITEM_CHANCE))
                    choice = Main.rand.Next([
                        ModContent.ItemType<DesertProwlerHat>(),
                        ModContent.ItemType<DesertProwlerShirt>(),
                        ModContent.ItemType<DesertProwlerPants>(),
                    ]);

                else if (Main.rand.NextBool(ITEM_CHANCE))
                    choice = ItemID.Bottle;

                HeldItemIndex = choice;
                NPC.netUpdate = true;
            }
        }

        public override void AI()
        {
            //Repeatedly reset the timer for the rendertargets being unloaded
            TicksSinceLastUsedRenderTargets = 0;

            //Face the player
            if (target.dead || DyingAnimation)
            {
                NPC.TargetClosest(false);
                DustDevilSummonCharge = 0;
            }

            NPC.spriteDirection = NPC.direction;
            if (spinDirection == 0)
                spinDirection = NPC.velocity.X.NonZeroSign();

            if (NPC.direction != 0f)
                spinDirection = NPC.direction;

            if (NPC.collideX)
            {
                NPC.velocity.X *= 1;
                NPC.velocity.Y *= 1.2f;

                if (NPC.velocity.X == 0)
                    NPC.velocity.X = 1.3f * NPC.direction;

                else if (Math.Abs(NPC.velocity.X) < 1.3)
                    NPC.velocity.X = -1.3f * NPC.velocity.X.NonZeroSign();
            }

            NPC.TargetClosest(true);
            if (DustDevilSummonCharge == 0)
            {
                spinMomentum = MathHelper.Lerp(spinMomentum, 0f, 0.03f);
                if (Math.Abs(spinMomentum) < 0.02f)
                    spinMomentum = 0f;

                if (DyingAnimation)
                {
                    NPC.velocity.X *= 0.95f;
                    if (Main.rand.NextFloat() < AICounter / 50f)
                    {
                        Dust d = Dust.NewDustPerfect(EyeWorldPosition + Main.rand.NextVector2Circular(15f, 15), 
                            Main.rand.NextBool(5) ? DustID.SandstormInABottle : DustID.Sandnado, 
                            Main.rand.NextVector2Circular(2f, 2f), 
                            Scale: Main.rand.NextFloat(1f, 2f));
                        d.noGravity = true;
                    }
                }
                else
                {
                    //Sluggishly hover towards the player, accelerating fast when away
                    if (Math.Abs(NPC.Center.X - target.Center.X) > 200)
                        NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, (target.Center.X - NPC.Center.X).NonZeroSign() * 3.5f, 0.02f);
                    else if (NPC.velocity.X == 0)
                        NPC.velocity.X = NPC.direction * 0.2f;
                    else if (Math.Abs(NPC.velocity.X) < 0.4f)
                        NPC.velocity.X *= 1.26f;
                }

                //Anti crowding
                for (int i = 0; i < Main.maxNPCs;i ++)
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
            }

            //Hover up n down
            if (NPC.Center.Y > target.Center.Y - 60)
                NPC.velocity.Y -= 0.05f;
            else if (NPC.Center.Y < target.Center.Y - 230)
            {
                NPC.velocity.Y += 0.08f;
                if (NPC.velocity.Y > 4)
                    NPC.velocity.Y = 4;
            }
            else
                NPC.velocity.Y *= 0.98f;


            //Decide to summon a dust devil
            if (!DyingAnimation && DustDevilSummonCharge == 0 && !target.dead && AICounter > 300 && NPC.WithinRange(target.Center, 600) && Collision.CanHitLine(target.position, target.width, target.height, NPC.position, NPC.width, NPC.height))
            {
                NPC.netUpdate = true;
                AICounter = 0;
                SoundEngine.PlaySound(WindupSound with { Volume = 0.6f }, NPC.Center);
                DustDevilSummonCharge = 1f;
            }

            else if (DustDevilSummonCharge > 0)
            {
                NPC.velocity.X *= 0.98f;
                DustDevilSummonCharge += 1 / (60f * 1.3f);
                if (NPC.velocity.Y > -1.2 && target.Center.Y < NPC.Bottom.Y + 400 )
                    NPC.velocity.Y -= 0.12f;

                spinMomentum = MathHelper.Lerp(spinMomentum, 1f, 0.03f);

                //Releasing the whirlwind
                if (DustDevilSummonCharge >= 2)
                {
                    SoundEngine.PlaySound(PunchSound, NPC.Center);
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

                        Vector2 twisterVelocity = (target.Center.X - NPC.Center.X).NonZeroSign() * Vector2.UnitX * 2f;
                        Vector2 twisterPosition = groundCheck.ToWorldCoordinates() - Vector2.UnitY * 70f;

                        //Double twisters in gfb
                        if (Main.getGoodWorld)
                        {
                            twisterVelocity *= 1.25f;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), twisterPosition, -twisterVelocity, ModContent.ProjectileType<SandstormSpriteTwister>(), 5, 0, Main.myPlayer);
                        }

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), twisterPosition, twisterVelocity, ModContent.ProjectileType<SandstormSpriteTwister>(), 5, 0, Main.myPlayer);
                        
                        
                        
                        AICounter = Main.rand.NextFloat(-300f, 0f);
                        NPC.netUpdate = true;
                    }
                    else
                        AICounter = -300;

                    spinMomentum += 0.4f;
                    DustDevilSummonCharge = 0f;
                    NPC.velocity += NPC.SafeDirectionTo(target.Center) * 2f;
                }
            }

            if (DyingAnimation && AICounter > 100)
            {
                NPC.life = 0;
                NPC.dontTakeDamage = false;
                NPC.checkDead();
            }

            if (!Main.dedServ)
            {
                UpdateAndSpawnClouds();
                UpdateAndSpawnSwirls();
            }
            AICounter++;
        }

        #region Visual effects

        #region Spinny eye
        public float eyeRotation;
        public float eyeRotationDirection;

        public override void FindFrame(int frameHeight)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                //Initialize clouds and swirls
                if (clouds.Count <= 0)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        UpdateAndSpawnClouds();
                        UpdateAndSpawnSwirls();
                    }
                }

                UpdateAndSpawnClouds();
                UpdateAndSpawnSwirls();
            }

            //Simple spin
            float spinValue = (0.2f + spinMomentum * 0.4f);
            //Slow down when dying
            if (DyingAnimation)
                spinValue *= Utils.GetLerpValue(80f, 10f, AICounter, true);

            eyeRotationDirection = MathHelper.Lerp(eyeRotationDirection, spinDirection, 0.02f);
            if (Math.Abs(eyeRotationDirection - spinDirection) < 0.05f)
                eyeRotationDirection = spinDirection;

            //Spin in the direction
            eyeRotation += eyeRotationDirection * spinValue;

            healthBarRotation += eyeRotationDirection * spinValue * 0.73f;

            NPC.frame.Width = NPC.width;
            NPC.frame.Height = NPC.height;
        }
        #endregion

        public int cloudTintVariant = 0;
        public float dustSpawnTimer = 0f;
        public List<SandSpriteCloud> clouds = new List<SandSpriteCloud>();
        public List<DustDevilSwirl> swirls = new List<DustDevilSwirl>();
        public MergeBlendTextureContent frontCloudRT;
        public MergeBlendTextureContent backCloudRT;
        public DrawActionTextureContent primSwirlsRT;
        public DrawActionTextureContent healthBarRT;

        public int TicksSinceLastUsedRenderTargets { get; set; }
        public bool RenderTargetsDisposed { get; set; } = true;
        public int AutoDisposeTime => 120;


        public void InitializeRenderTargets()
        {
            frontCloudRT = new MergeBlendTextureContent(DrawCloudsFront, 600, 600);
            Main.ContentThatNeedsRenderTargets.Add(frontCloudRT);
            backCloudRT = new MergeBlendTextureContent(DrawCloudsBack, 600, 600);
            Main.ContentThatNeedsRenderTargets.Add(backCloudRT);
            primSwirlsRT = new DrawActionTextureContent(DrawSwirls, 400, 200, startSpritebatch: false);
            Main.ContentThatNeedsRenderTargets.Add(primSwirlsRT);

            healthBarRT = new DrawActionTextureContent(DrawHealthBarRT, 36, 12);
            Main.ContentThatNeedsRenderTargets.Add(healthBarRT);

            RenderTargetsManager.AddTemporaryTarget(this);
        }

        public void DisposeOfRenderTargets()
        {
            clouds.Clear();
            swirls.Clear();

            frontCloudRT?.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(frontCloudRT);
            frontCloudRT.GetTarget()?.Dispose();
            backCloudRT?.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(backCloudRT);
            backCloudRT.GetTarget()?.Dispose();
            primSwirlsRT?.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(primSwirlsRT);
            primSwirlsRT.GetTarget()?.Dispose();

            healthBarRT?.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(healthBarRT);
            healthBarRT.GetTarget()?.Dispose();
        }

        #region Dust clouds
        public static Color CloudBackColor = new Color(200, 160, 160);

        public static Asset<Texture2D> CloudTexture;

        public class SandSpriteCloud
        {
            public int variant;
            public int frameCount;
            public int frameSpeed;
            public int frame;
            public float rotation;
            public Vector2 velocity;
            public Vector2 radialVelocity;
            public Vector2 worldPosition;
            public Vector2 radialPosition;

            public SandSpriteCloud(Vector2 position, Vector2 velocity, bool small = false)
            {
                worldPosition = position;
                this.velocity = velocity;


                variant = Main.rand.Next(3);
                if (small)
                    variant += 3;
                frameSpeed = Main.rand.Next(6, 7);

                frameCount = 0;
                frame = 0;
                radialVelocity = -Vector2.UnitY * Main.rand.NextFloat(0f, 0.6f);
                radialPosition = new Vector2(Main.rand.NextFloat(0f, 2f), -Main.rand.NextFloat(0f, 10f));
                rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            }
        }

        public void UpdateAndSpawnClouds()
        {
            if (!DyingAnimation || AICounter < 50f)
            {
                dustSpawnTimer++;
                int dustAmount = Main.rand.NextBool(2) ? 3 : 2;
                if (dustSpawnTimer > 1f)
                {
                    dustSpawnTimer = 0;
                    for (int i = 0; i < dustAmount; i++)
                    {
                        SandSpriteCloud newCloud = new SandSpriteCloud(NPC.Center + Vector2.UnitY * 20f, NPC.velocity);
                        if (Main.rand.NextBool(3))
                            newCloud.frameSpeed++;
                        newCloud.worldPosition.Y += Main.rand.NextFloat(-10f, 5f);

                        newCloud.radialPosition.Y += Main.rand.NextFloat(-10f, 15f);
                        clouds.Add(newCloud);
                    }
                }
                SandSpriteCloud smallCloud = new SandSpriteCloud(NPC.Bottom + Vector2.UnitY * 10f, NPC.velocity, true);
                smallCloud.worldPosition.Y += Main.rand.NextFloat(-30f, 10f);
                clouds.Add(smallCloud);
            }

            //Update clouds
            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                SandSpriteCloud cloud = clouds[i];

                //Animate clouds and despawn the old ones
                cloud.frameCount++;
                if (cloud.frameCount > cloud.frameSpeed)
                {
                    cloud.frameCount = 0;
                    cloud.frame++;
                    if (cloud.frame >= 7)
                    {
                        clouds.RemoveAt(i);
                        continue;
                    }
                }

                //Accelerate
                if (cloud.frame < 4)
                {
                    cloud.radialVelocity.X = MathHelper.Lerp(cloud.radialVelocity.X, 0.06f * spinDirection, 0.03f);
                    cloud.radialVelocity.Y = MathHelper.Lerp(cloud.radialVelocity.Y, -1.9f, 0.05f);
                }
                //Slown down
                else
                    cloud.radialVelocity = Vector2.Lerp(cloud.radialVelocity, Vector2.Zero, 0.04f);

                cloud.radialPosition += cloud.radialVelocity;

                cloud.worldPosition += cloud.velocity;
                cloud.velocity *= 0.91f;

                //Warp around
                while (cloud.radialPosition.X > 2)
                    cloud.radialPosition.X -= 2;
                while (cloud.radialPosition.X < 0)
                    cloud.radialPosition.X += 2;

                cloud.rotation -= spinDirection * 0.012f;
            }
        }

        public void DrawClouds(SpriteBatch spriteBatch, bool backgroundPass, bool frontClouds)
        {
            CloudTexture ??= ModContent.Request<Texture2D>(AssetDirectory.DesertNPCs + "SandstormSpriteClouds");
            Texture2D tex = CloudTexture.Value;
            Vector2 textureOrigin = new Vector2(300f, 300f);
            Color color = backgroundPass ? Color.Black : frontClouds ? Color.White : CloudBackColor;

            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                SandSpriteCloud cloud = clouds[i];
                if (frontClouds && cloud.radialPosition.X >= 1f)
                    continue;
                else if (!frontClouds && cloud.radialPosition.X < 1)
                    continue;

                Rectangle frame = tex.Frame(7 * 3, 6 * 4, cloud.frame + cloudTintVariant * 7, cloud.variant);

                Vector2 offset = cloud.radialPosition;

                float ringWidth = 0.3f + 0.5f * MathF.Pow(Utils.GetLerpValue(10f, -15f, offset.Y, true), 0.6f) ;

                ringWidth -= Utils.GetLerpValue(-10f, -50f, offset.Y, true) * 0.5f;

                offset.X -= frontClouds ? 0.5f : 1.5f;
                offset.X *= 53f * ringWidth;
                //Back clouds go in the opposite direction to make them appear as if they are rotating backwards
                if (!frontClouds)
                    offset.X *= -1;

                Color usedColor = color;
                //Front clouds turn towards darkness at the edges
                if (!backgroundPass && frontClouds)
                    usedColor = Color.Lerp(color, CloudBackColor, Utils.GetLerpValue(0.2f, 0f, cloud.radialPosition.X, true) + Utils.GetLerpValue(0.8f, 1f, cloud.radialPosition.X, true));

                offset += cloud.worldPosition - NPC.Center;

                if (cloud.frame >= 6)
                    usedColor *= 0.4f;


                FablesUtils.GetBiomeInfluences(out float corroInfluence, out float crimInfluence, out float hallowInfluence);
                float baseInfluence = MathF.Pow(Math.Max(0f, 1 - hallowInfluence - corroInfluence - crimInfluence), 0.1f);
                if (backgroundPass)
                    baseInfluence = 1f;


                spriteBatch.Draw(tex, textureOrigin + offset, frame, usedColor * baseInfluence, cloud.rotation, frame.Size() / 2f, 1f, 0, 0);


                if (backgroundPass)
                    continue;

                if (hallowInfluence > 0f)
                {
                    Rectangle hallowFrame = frame;
                    hallowFrame.Y += 204;
                    spriteBatch.Draw(tex, textureOrigin + offset, hallowFrame, usedColor * hallowInfluence, cloud.rotation, frame.Size() / 2f, 1f, 0, 0);
                }

                if (corroInfluence > 0f)
                {
                    Rectangle corroFrame = frame;
                    corroFrame.Y += 204 * 2;
                    spriteBatch.Draw(tex, textureOrigin + offset, corroFrame, usedColor * corroInfluence, cloud.rotation, frame.Size() / 2f, 1f, 0, 0);
                }

                if (crimInfluence > 0f)
                {
                    Rectangle crimFrame = frame;
                    crimFrame.Y += 204 * 3;
                    spriteBatch.Draw(tex, textureOrigin + offset, crimFrame, usedColor * crimInfluence, cloud.rotation, frame.Size() / 2f, 1f, 0, 0);
                }

            }
        }

        public void DrawCloudsFront(SpriteBatch spriteBatch, bool backgroundPass) => DrawClouds(spriteBatch, backgroundPass, true);
        public void DrawCloudsBack(SpriteBatch spriteBatch, bool backgroundPass) => DrawClouds(spriteBatch, backgroundPass, false);
        
        public static void DrawCloudLayer(SpriteBatch spriteBatch, MergeBlendTextureContent renderTarget, Vector2 position, Color drawColor)
        {
            if (renderTarget == null)
                return;
            renderTarget.Request();
            if (!renderTarget.IsReady)
                return;

           // spriteBatch.Draw(renderTarget.GetTarget(), position + Vector2.UnitY * 2f, null, Color.OrangeRed * 0.0f, 0, new Vector2(300f, 300f), 1f, 0, 0);
            spriteBatch.Draw(renderTarget.GetTarget(), position, null, drawColor * 0.7f, 0, new Vector2(300f, 300f), 1f, 0, 0);
        }
        #endregion

        #region Swirls
        public float swirlSpawnTimer = 0f;

        public void UpdateAndSpawnSwirls()
        {
            swirlSpawnTimer++;
            if (swirlSpawnTimer > 8 && Main.rand.NextBool(4) && (!DyingAnimation || AICounter < 60f))
            {
                swirlSpawnTimer = 0;

                Color colorFront = Main.rand.NextBool(3) ? Color.BurlyWood : Main.rand.NextBool() ? Color.SandyBrown : Color.Wheat;
                DustDevilSwirl newSwirl = new DustDevilSwirl(colorFront, Color.DarkGoldenrod, Main.rand.NextFloat(0.2f, 0.4f));

                if (Main.LocalPlayer.ZoneHallow)
                {
                    newSwirl.color = Main.rand.NextBool(3) ? Color.FloralWhite : Main.rand.NextBool() ? Color.Honeydew : Color.SeaShell;
                    newSwirl.backColor = Color.Pink;
                }
                else if (Main.LocalPlayer.ZoneCorrupt)
                {
                    newSwirl.color = Main.rand.NextBool(3) ? Color.MediumPurple : Main.rand.NextBool() ? Color.MediumOrchid : Color.Plum;
                    newSwirl.backColor = Color.SlateBlue;
                }
                else if (Main.LocalPlayer.ZoneCrimson)
                {
                    newSwirl.color = Main.rand.NextBool(3) ? Color.Crimson : Main.rand.NextBool() ? Color.Salmon : Color.LightCoral;
                    newSwirl.backColor = Color.DarkRed;
                }

                if (cloudTintVariant == 1)
                {
                    newSwirl.color *= 1.2f;
                    newSwirl.backColor *= 1.2f;
                }

                float swirlVerticalOffset = Main.rand.NextFloat(-16f, 20f);

                newSwirl.spawnPosition = NPC.Center;
                newSwirl.flatPos = new Vector2(Main.rand.NextFloat(), swirlVerticalOffset);
                newSwirl.flatTrajectory = new Vector2(Main.rand.NextFloat(0.3f, 0.6f), -5);
                //Increase and decrease the radius based on the height on the sprite to have a double cone shape
                newSwirl.startRadius = Main.rand.NextFloat(15f, 22f);
                newSwirl.startRadius -= Utils.GetLerpValue(0f, -16f, swirlVerticalOffset, true) * 6f;
                newSwirl.startRadius -= Utils.GetLerpValue(14f, 20f, swirlVerticalOffset, true) * 6f;

                newSwirl.radiusChange = -6f;
                newSwirl.extraThin = true;
                newSwirl.horizontalTwirlMultiplier = -spinDirection;
                swirls.Add(newSwirl);
            }

            //Update swirls
            for (int i = swirls.Count - 1; i >= 0; i--)
            {
                DustDevilSwirl swirl = swirls[i];
                swirl.timer += 1 / (60f * swirl.lifetime);
                if (swirl.timer > 1)
                {
                    swirls.RemoveAt(i);
                    continue;
                }

                swirl.spawnPosition = Vector2.Lerp(swirl.spawnPosition, NPC.Center, 0.3f * Utils.GetLerpValue(0.5f, 0.0f, swirl.timer, true));
                swirl.drawOffset = (swirl.spawnPosition - NPC.Center) * 0.5f;

                swirl.flatPos.X += swirl.horizontalTwirlMultiplier * 0.02f;
            }
        }

        public void DrawSwirls(SpriteBatch spriteBatch)
        {
            Main.spriteBatch.Begin();
            Main.spriteBatch.End();

            int swirlCount = swirls.Count;
            if (swirlCount == 0)
                return;

            //translation so its got the proper origin
            Matrix translation = Matrix.CreateTranslation(100, 100, 0);
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, 400, 200, 0, -1, 1);
            Effect swirlEffect = Scene["DustDevilPrimitive"].GetShader().Shader;
            swirlEffect.Parameters["uWorldViewProjection"].SetValue(translation * projection);

            //Multiply the values by 2 because every swirl will draw twice, once for the front side and once for the back side
            int verticesPerSwirl = 2 * DustDevilSwirl.SWIRL_TRAIL_DEFINITION * 2;
            int indicesPerSwirl = 6 * (DustDevilSwirl.SWIRL_TRAIL_DEFINITION - 1) * 2;

            int vertexCount = verticesPerSwirl * swirlCount;
            int indexCount = indicesPerSwirl * swirlCount;

            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[vertexCount];
            short[] indices = new short[indexCount];

            for (int i = 0; i < swirlCount; i++)
            {
                swirls[i].ConstructPrimitives(i * verticesPerSwirl, i * indicesPerSwirl, ref vertices, ref indices, Vector2.UnitX * 200);
            }

            swirlEffect.CurrentTechnique.Passes[0].Apply();
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount, indices, 0, indexCount / 3);
        }

        public static void DrawSwirlLayer(SpriteBatch spriteBatch, DrawActionTextureContent renderTarget, Vector2 position, Color drawColor, bool back)
        {
            if (renderTarget == null)
                return;
            renderTarget.Request();
            if (renderTarget.IsReady)
                spriteBatch.Draw(renderTarget.GetTarget(), position, new Rectangle(back ? 200 : 0, 0, 200, 200), drawColor * 0.8f, 0, new Vector2(100f, 100f), 2f, 0, 0);
        }
        #endregion

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = NPC.TintFromBuffAesthetic(drawColor);

            //Fsr the icon dummy uses a fullblack color by default?
            if (NPC.IsABestiaryIconDummy)
            {
                drawColor = Color.White;
                TicksSinceLastUsedRenderTargets = 0;

                if (clouds.Count == 0)
                {
                    eyeRotation = 1.2f;
                }
            }

            //Rotation vector that represents the eye's rotation around the sprite's center
            //Y coordinate is used for the front/back, while X coordinate is for side to side
            Vector2 eyeRotationVector = eyeRotation.ToRotationVector2();
            bool eyeBehind = eyeRotationVector.Y < -0.2f;

            if (eyeBehind)
                DrawEye(drawColor, screenPos);

            if (RenderTargetsDisposed || backCloudRT == null || frontCloudRT == null || primSwirlsRT == null)
                InitializeRenderTargets();

            DrawSwirlLayer(spriteBatch, primSwirlsRT, NPC.Center - screenPos, drawColor, true);
            DrawCloudLayer(spriteBatch, backCloudRT, NPC.Center - screenPos, drawColor);

            if (HeldItemIndex != 0)
            {
                Texture2D heldItemTexture = TextureAssets.Item[HeldItemIndex].Value;
                Vector2 itemOffset =  -eyeRotationVector * 4f;
                itemOffset.Y += MathF.Sin(Main.GlobalTimeWrappedHourly) * 10f;

                eyeRotationVector.Y = 0;
                float eyeRotationStrength = 0.15f + spinMomentum * 0.2f;
                eyeRotationVector = eyeRotationVector.RotatedBy(MathF.Sin(eyeRotation * 0.5f + Main.GlobalTimeWrappedHourly * 0.04f) * eyeRotationStrength);
                float itemRotation = eyeRotationVector.Y * 0.4f;

                Main.spriteBatch.Draw(heldItemTexture, NPC.Center + itemOffset - screenPos, null, Color.White, itemRotation, heldItemTexture.Size() / 2f, 1f, 0, 0f);

            }

            DrawCloudLayer(spriteBatch, frontCloudRT, NPC.Center - screenPos, drawColor);
            DrawSwirlLayer(spriteBatch, primSwirlsRT, NPC.Center - screenPos, drawColor, false);

            if (!eyeBehind)
                DrawEye(drawColor, screenPos);
            return false;
        }

        public void DrawEye(Color drawColor, Vector2 screenPos)
        {
            Texture2D eyeTex = TextureAssets.Npc[Type].Value;
            Vector2 eyePos = EyeWorldPosition;

            //Get the eye's frame based on the rotation cycle
            float wrappedRotation = (eyeRotation.Modulo(MathHelper.TwoPi) / MathHelper.TwoPi);
            int rotationFrame = (int)(wrappedRotation * 8);
            if (eyeRotationDirection > 0)
                rotationFrame--;
            else
                rotationFrame -= 2;
            rotationFrame = rotationFrame.Modulo(8);

            Rectangle eyeFrame = eyeTex.Frame(10, 16, 0, rotationFrame, -2, -2);
            //Alt sprite based on the eye direction
            if (eyeRotationDirection < 0)
                eyeFrame.X += 36;
            //Alt sprite when rotating slower
            if (Math.Abs(eyeRotationDirection) < 0.85f)
                eyeFrame.Y += 160;

            float eyeGlowOpacity = 0f;
            Rectangle eyeGlowFrame = eyeFrame;
            eyeGlowFrame.X += 36 * 2;
            if (DyingAnimation)
            {
                //Shake the eye
                eyePos += Main.rand.NextVector2Circular(4f, 4f) * Utils.GetLerpValue(0f, 70f, AICounter, true);
                eyeGlowOpacity = MathF.Pow(Utils.GetLerpValue(10f, 50f, AICounter, true), 2f);
            }


            Vector2 eyeRotationVector = eyeRotation.ToRotationVector2();
            eyeRotationVector.Y = 0;
            float eyeRotationStrength = 0.15f + spinMomentum * 0.2f;
            eyeRotationVector = eyeRotationVector.RotatedBy(MathF.Sin(eyeRotation * 0.5f + Main.GlobalTimeWrappedHourly * 0.04f) * eyeRotationStrength);
            float drawRotation = -eyeRotationVector.Y;

            Vector2 eyeScale = new Vector2(NPC.scale);
            eyeScale.X += spinMomentum * 0.2f;
            eyeScale.Y -= spinMomentum * 0.1f;

            Main.spriteBatch.Draw(eyeTex, eyePos - screenPos, eyeFrame, Color.White * (drawColor.A / 255f), drawRotation, eyeFrame.Size() / 2f, eyeScale, 0, 0f);

            //Tints the eye based on the biome
            FablesUtils.GetBiomeInfluences(out float corroInfluence, out float crimInfluence, out float hallowInfluence);

            if (hallowInfluence > 0)
            {
                Rectangle hallowEyeFrame = eyeFrame;
                hallowEyeFrame.X += 36 * 4;
                Main.spriteBatch.Draw(eyeTex, eyePos - screenPos, hallowEyeFrame, Color.White * (drawColor.A / 255f) * hallowInfluence, drawRotation, eyeFrame.Size() / 2f, eyeScale, 0, 0f);
            }
            if (corroInfluence > 0)
            {
                Rectangle corroEyeFrame = eyeFrame;
                corroEyeFrame.X += 36 * 6;
                Main.spriteBatch.Draw(eyeTex, eyePos - screenPos, corroEyeFrame, Color.White * (drawColor.A / 255f) * corroInfluence, drawRotation, eyeFrame.Size() / 2f, eyeScale, 0, 0f);
            }
            if (crimInfluence > 0)
            {
                Rectangle crimEyeFrame = eyeFrame;
                crimEyeFrame.X += 36 * 8;
                Main.spriteBatch.Draw(eyeTex, eyePos - screenPos, crimEyeFrame, Color.White * (drawColor.A / 255f) * crimInfluence, drawRotation, eyeFrame.Size() / 2f, eyeScale, 0, 0f);
            }

            //Glowing eye
            Main.spriteBatch.Draw(eyeTex, eyePos - screenPos, eyeGlowFrame, Color.White * eyeGlowOpacity, drawRotation, eyeFrame.Size() / 2f, eyeScale, 0, 0f);

        }

        #region Healthbar
        public float healthBarRotation;
        public void DrawHealthBarRT(SpriteBatch spriteBatch)
        {
            Texture2D barBack = TextureAssets.Hb2.Value;
            Texture2D barFront = TextureAssets.Hb1.Value;

            float healthPercent = NPC.life / (float)NPC.lifeMax;
            if (healthPercent > 1f)
                healthPercent = 1f;

            int barPixelTrim = (int)(36f * healthPercent);

            Color barColor;
            if (healthPercent > 0.5f)
                barColor = Color.Lerp(new Color(0, 1f, 0f), new Color(1f, 1f, 0f), Utils.GetLerpValue(1f, 0.5f, healthPercent, true));
            else
                barColor = Color.Lerp(new Color(1, 1f, 0f), new Color(1f, 0f, 0f), Utils.GetLerpValue(0.5f, 0f, healthPercent, true));

            if (barPixelTrim < 3)
                barPixelTrim = 3;

            spriteBatch.Draw(barBack, Vector2.Zero, null, barColor, 0f, Vector2.Zero, 1f, 0, 0f);
            if (barPixelTrim < 34)
            {
                spriteBatch.Draw(barFront, Vector2.Zero, new Rectangle(0, 0, barPixelTrim - 2, barFront.Height), barColor, 0f, Vector2.Zero, 1f, 0, 0f);
                spriteBatch.Draw(barFront, new Vector2(barPixelTrim - 2, 0), new Rectangle(32, 0, 2, barFront.Height), barColor, 0f, Vector2.Zero, 1f, 0, 0f);
            }
            else
                spriteBatch.Draw(barFront, Vector2.Zero, new Rectangle(0, 0, barPixelTrim, barFront.Height), barColor, 0f, Vector2.Zero, 1f, 0, 0f);
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            if (healthBarRT == null)
                InitializeRenderTargets();

            if (Main.LocalPlayer.gravDir == -1f)
            {
                position.Y -= Main.screenPosition.Y;
                position.Y = Main.screenPosition.Y + (float)Main.screenHeight - position.Y;
            }
            position -= Main.screenPosition;


            float barOpacity = Lighting.Brightness((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16) * 0.95f;
            healthBarRT.Request();
            if (healthBarRT.IsReady)
            {
                Texture2D hpBar = healthBarRT.GetTarget();
                Vector2 eyeRotationVector = healthBarRotation.ToRotationVector2();
                Vector2 scaleVec = new Vector2(1f - Math.Abs(eyeRotationVector.X ) * 0.8f, 1f);

                Color drawColor = Color.Lerp(Color.White, Color.DarkGray, Utils.GetLerpValue(0.3f, -0.1f, eyeRotationVector.Y, true));
                SpriteEffects effects = eyeRotationVector.Y < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                eyeRotationVector.Y = 0;
                Main.spriteBatch.Draw(hpBar, position + eyeRotationVector * 29f, null, drawColor * barOpacity, 0f, hpBar.Size() / 2f, scaleVec * scale, effects, 0);
            }

            return false;
        }
        #endregion
        #endregion

        #region Spawn, Hit, death, and loot
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.PlayerInTown || spawnInfo.Water)
                return 0f;

            bool playerInBiome = spawnInfo.Player.ZoneDesert && !spawnInfo.Player.ZoneUndergroundDesert;
            if (CalamityFables.SpiritEnabled && CalamityFables.SpiritReforged.TryFind("SavannaBiome", out ModBiome savanna))
                playerInBiome |= spawnInfo.Player.InModBiome(savanna);

            if (playerInBiome)
            {
                if (Sandstorm.Happening)
                    return SpawnCondition.SandstormEvent.Chance * 0.5f;
                else
                    return SpawnCondition.OverworldDayDesert.Chance * 0.15f;
            }

            return 0f;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (spinMomentum < 2f)
                spinMomentum += 0.5f;
        }

        public override bool CheckDead()
        {
            if (!DyingAnimation)
            {
                NPC.dontTakeDamage = true;
                DyingAnimation = true;
                NPC.life = 1;
                NPC.netUpdate = true;
                AICounter = 0;
                return false;
            }

            for (int i = 0; i < 22; i++)
            {
                Dust d = Dust.NewDustPerfect(EyeWorldPosition + Main.rand.NextVector2Circular(15f, 15),
                    Main.rand.NextBool(5) ? DustID.SandstormInABottle : DustID.Sandnado,
                    Main.rand.NextVector2CircularEdge(3f, 3f),
                    Scale: Main.rand.NextFloat(1.4f, 2f));
                d.noGravity = true;
                if (Main.rand.NextBool(7))
                    d.velocity *= 2f;
                if (Main.rand.NextBool(7))
                    d.velocity *= 2f;
            }

            return true;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            var sandScatterParams = new DropOneByOne.Parameters()
            {
                ChanceNumerator = 1,
                ChanceDenominator = 1,
                MinimumStackPerChunkBase = 1,
                MaximumStackPerChunkBase = 1,
                MinimumItemDropsCount = 4,
                MaximumItemDropsCount = 10,
                BonusMinDropsPerChunkPerPlayer = 0
            };
            npcLoot.Add(new DropOneByOne(ItemID.SandBlock, sandScatterParams));
            npcLoot.Add(new SandstormSpriteContentDropRule());
        }
        #endregion
    }

    public class SandstormSpriteContentDropRule : IItemDropRule
    {
        public List<IItemDropRuleChainAttempt> ChainedRules { get; private set; }

        public SandstormSpriteContentDropRule()
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
                itemId = ModContent.ItemType<SandstormSpriteInABottle>();

            CommonCode.DropItem(info, itemId, 1);
            ItemDropAttemptResult result = default(ItemDropAttemptResult);
            result.State = ItemDropAttemptResultState.Success;
            return result;
        }

        public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
        {
            float successChance = 1f / (float)SandstormSprite.ITEM_CHANCE;
            float failChance = (SandstormSprite.ITEM_CHANCE - 1) /(float)SandstormSprite.ITEM_CHANCE;

            float desertProwlerDropRate = failChance * successChance / 3f;
            float vanityDropRate = failChance * failChance * successChance;

            //Fake a report
            drops.Add(new DropRateInfo(ItemID.SandstorminaBottle, 1, 1, successChance));
            drops.Add(new DropRateInfo(ModContent.ItemType<DesertProwlerHat>(), 1, 1, desertProwlerDropRate));
            drops.Add(new DropRateInfo(ModContent.ItemType<DesertProwlerShirt>(), 1, 1, desertProwlerDropRate));
            drops.Add(new DropRateInfo(ModContent.ItemType<DesertProwlerPants>(), 1, 1, desertProwlerDropRate));
            drops.Add(new DropRateInfo(ModContent.ItemType<SandstormSpriteInABottle>(), 1, 1, vanityDropRate));

            Chains.ReportDroprates(ChainedRules, 1f, drops, ratesInfo);
        }
    }

    public class SandstormSpriteTwister : ModProjectile, ITemporaryRenderTargetHolder
    {
        public override string Texture => AssetDirectory.Invisible;

        public bool FadingOut
        {
            get => Projectile.ai[0] == 1;
            set => Projectile.ai[0] = value ? 1 : 0;
        }

        public SlotId twisterSoundSlot = SlotId.Invalid;

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 125;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 370;
            Projectile.tileCollide = false;
        }

        public override bool CanHitPlayer(Player target) => !FadingOut && !target.HasBuff<WhiskedDebuff>();

        //Set knockback to zero here so that we can apply our custom kb in the projectile's AI
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) => modifiers.Knockback *= 0;

        public override void AI()
        {
            if (Projectile.timeLeft < 50)
                FadingOut = true;

            if (FadingOut)
                Projectile.velocity.X *= 0.98f;

            bool floorBelow = Collision.SolidCollision(Projectile.BottomLeft, Projectile.width, 10) || Collision.WetCollision(Projectile.BottomLeft, Projectile.width, 10);
            bool insideTerrain = Collision.SolidCollision(Projectile.BottomLeft + new Vector2(10, -20), Projectile.width - 20, 20);

            if (insideTerrain)
                Projectile.velocity.Y -= 0.12f;
            else if (!floorBelow)
                Projectile.velocity.Y += 0.1f;
            else
                Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, 0f, 0.2f);

            if (!FadingOut && FullSolidCollision(Projectile.TopLeft + new Vector2(10, 20), Projectile.width - 20, Projectile.height - 20))
            {
                FadingOut = true;
                Projectile.timeLeft = 50;
                Projectile.velocity.X *= 0.9f;
            }

            Projectile.velocity.Y = Math.Clamp(Projectile.velocity.Y, -2f, 2f);

            //Woosh items up
            for (int i = 0; i < Main.maxItems; i++)
            {
                Item item = Main.item[i];
                if (item.active && item.Hitbox.Intersects(Projectile.Hitbox))
                {
                    item.velocity.Y -= 0.2f;

                    Rectangle shrunkenHitbox = Projectile.Hitbox;
                    shrunkenHitbox.Inflate(-10, 0);
                    if (item.Hitbox.Intersects(shrunkenHitbox))
                        item.velocity.X += Projectile.velocity.X * 0.14f;
                }
            }

            //Woosh players up
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.Hitbox.Intersects(Projectile.Hitbox))
                {
                    player.RemoveAllGrapplingHooks();

                    if (player.velocity.Y == 0)
                        player.velocity.Y = -3;

                    if (player.velocity.Y > -18f)
                        player.velocity.Y -= 1.5f;


                    player.jump = Player.jumpHeight / 2;
                    player.AddBuff(ModContent.BuffType<WhiskedDebuff>(), 60 * 8);

                    Rectangle shrunkenHitbox = Projectile.Hitbox;
                    shrunkenHitbox.Inflate(-10, 0);
                    if (player.Hitbox.Intersects(shrunkenHitbox))
                        player.velocity.X += Projectile.velocity.X * 0.14f;
                }
            }

            //Dust from floor
            if (Main.rand.NextBool(4) && Main.rand.NextFloat() < Utils.GetLerpValue(0, 40, Projectile.timeLeft, true))
            {
                Point tileBelow = Projectile.Bottom.ToTileCoordinates();
                tileBelow.X += Main.rand.Next(-2, 3);
                tileBelow.Y += 1;
                if (Main.tile[tileBelow].HasUnactuatedTile)
                {
                    Dust floorDust = Main.dust[WorldGen.KillTile_MakeTileDust(tileBelow.X, tileBelow.Y, Main.tile[tileBelow])];
                    floorDust.position.Y -= Main.rand.NextFloat(0f, 30f);
                    floorDust.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 6f);
                    floorDust.velocity.X = floorDust.velocity.Y * 0.2f * Projectile.velocity.X.NonZeroSign();
                    if (Main.rand.NextBool(3))
                        floorDust.velocity.X *= -1;
                }
            }

            TicksSinceLastUsedRenderTargets = 0;

            if (Main.dedServ)
                return;

            UpdateAndSpawnClouds();
            UpdateAndSpawnSwirls();
            UpdateSound();
        }


        public void UpdateSound()
        {
            if (!SoundEngine.TryGetActiveSound(twisterSoundSlot, out ActiveSound swoosh))
            {
                twisterSoundSlot = SoundEngine.PlaySound(DesertProwlerHat.WindLoopSound, Projectile.Center, SyncSoundWithCharge);
                SoundEngine.TryGetActiveSound(twisterSoundSlot, out swoosh);
            }
            if (swoosh != null)
                SoundHandler.TrackSoundWithFade(twisterSoundSlot, 4);
        }

        public bool SyncSoundWithCharge(ActiveSound soundInstance)
        {
            soundInstance.Position = Projectile.Center;
            soundInstance.Volume = Utils.GetLerpValue(1f, 61f, Projectile.timeLeft, true) * 0.6f;
            soundInstance.Pitch = (float)Math.Sin(Projectile.timeLeft * 0.05f) * 0.1f;
            return true;
        }

        public float dustSpawnTimer = 0f;
        public List<SandSpriteCloud> clouds = new List<SandSpriteCloud>();
        public List<DustDevilSwirl> swirls = new List<DustDevilSwirl>();
        public MergeBlendTextureContent frontCloudRT;
        public MergeBlendTextureContent backCloudRT;
        public DrawActionTextureContent primSwirlsRT;

        public int TicksSinceLastUsedRenderTargets { get; set; }
        public int AutoDisposeTime => 120;
        public bool RenderTargetsDisposed { get; set; } = true;

        public void InitializeRenderTargets()
        {
            frontCloudRT = new MergeBlendTextureContent(DrawCloudsFront, 600, 600);
            Main.ContentThatNeedsRenderTargets.Add(frontCloudRT);
            backCloudRT = new MergeBlendTextureContent(DrawCloudsBack, 600, 600);
            Main.ContentThatNeedsRenderTargets.Add(backCloudRT);
            primSwirlsRT = new DrawActionTextureContent(DrawSwirls, 400, 200, startSpritebatch: false);
            Main.ContentThatNeedsRenderTargets.Add(primSwirlsRT);

            RenderTargetsManager.AddTemporaryTarget(this);
        }

        public void DisposeOfRenderTargets()
        {
            frontCloudRT?.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(frontCloudRT);
            frontCloudRT.GetTarget()?.Dispose();
            backCloudRT?.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(backCloudRT);
            backCloudRT.GetTarget()?.Dispose();
            primSwirlsRT?.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(primSwirlsRT);
            primSwirlsRT.GetTarget()?.Dispose();
        }

        #region Dust clouds
        public void UpdateAndSpawnClouds()
        {
            if (!FadingOut)
            {
                dustSpawnTimer++;
                int dustAmount = Main.rand.NextBool(2) ? 4 : 3;
                if (dustSpawnTimer > 1f)
                {
                    dustSpawnTimer = 0;
                    for (int i = 0; i < dustAmount; i++)
                    {
                        SandSpriteCloud newCloud = new SandSpriteCloud(Projectile.Bottom, Projectile.velocity);
                        newCloud.frameSpeed += 2;
                        newCloud.radialVelocity.Y -= 2f;

                        //newCloud.worldPosition.Y += Main.rand.NextFloat(-20f, 5f);
                        newCloud.radialPosition.Y = 0f;
                        clouds.Add(newCloud);
                    }
                }
                SandSpriteCloud smallCloud = new SandSpriteCloud(Projectile.Bottom, Projectile.velocity, true);
                smallCloud.worldPosition.Y += Main.rand.NextFloat(-30f, 10f);
                clouds.Add(smallCloud);
            }

            //Update clouds
            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                SandSpriteCloud cloud = clouds[i];

                //Animate clouds and despawn the old ones
                cloud.frameCount++;
                if (cloud.frameCount > cloud.frameSpeed)
                {
                    cloud.frameCount = 0;
                    cloud.frame++;
                    if (cloud.frame >= 7)
                    {
                        clouds.RemoveAt(i);
                        continue;
                    }
                }

                //Accelerate
                if (cloud.frame < 4)
                {
                    cloud.radialVelocity.X = MathHelper.Lerp(cloud.radialVelocity.X, 0.06f * Projectile.velocity.X.NonZeroSign(), 0.03f);
                    cloud.radialVelocity.Y = MathHelper.Lerp(cloud.radialVelocity.Y, -1.9f, 0.05f);
                }
                //Slown down
                else
                    cloud.radialVelocity = Vector2.Lerp(cloud.radialVelocity, Vector2.Zero, 0.04f);

                cloud.radialPosition += cloud.radialVelocity;

                cloud.worldPosition += cloud.velocity;
                cloud.velocity *= 0.995f;

                //Warp around
                while (cloud.radialPosition.X > 2)
                    cloud.radialPosition.X -= 2;
                while (cloud.radialPosition.X < 0)
                    cloud.radialPosition.X += 2;

                cloud.rotation -= Projectile.velocity.X * 0.012f;
            }
        }

        public void DrawClouds(SpriteBatch spriteBatch, bool backgroundPass, bool frontClouds)
        {
            SandstormSprite.CloudTexture ??= ModContent.Request<Texture2D>(AssetDirectory.DesertNPCs + "SandstormSpriteClouds");
            Texture2D tex = CloudTexture.Value;
            Vector2 textureOrigin = new Vector2(300f, 300f);
            Color color = backgroundPass ? Color.Black : frontClouds ? Color.White : CloudBackColor;

            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                SandSpriteCloud cloud = clouds[i];
                if (frontClouds && cloud.radialPosition.X >= 1f)
                    continue;
                else if (!frontClouds && cloud.radialPosition.X < 1)
                    continue;

                Rectangle frame = tex.Frame(7 * 3, 6 * 4, cloud.frame + 7, cloud.variant);
                Vector2 offset = cloud.radialPosition;

                float ringWidth = MathHelper.Lerp(180f, 40f, MathF.Pow(Utils.GetLerpValue(0f, -40f, offset.Y, true), 0.25f));

                offset.X -= frontClouds ? 0.5f : 1.5f;
                offset.X *= ringWidth;
                //Back clouds go in the opposite direction to make them appear as if they are rotating backwards
                if (!frontClouds)
                    offset.X *= -1;

                offset.X += MathF.Sin(cloud.radialPosition.Y * 0.04f + Main.GlobalTimeWrappedHourly * 3f) * 4.70f;

                Color usedColor = color;
                //Front clouds turn towards darkness at the edges
                if (!backgroundPass && frontClouds)
                    usedColor = Color.Lerp(color, CloudBackColor, Utils.GetLerpValue(0.2f, 0f, cloud.radialPosition.X, true) + Utils.GetLerpValue(0.8f, 1f, cloud.radialPosition.X, true));

                offset += cloud.worldPosition - Projectile.Center;

                FablesUtils.GetBiomeInfluences(out float corroInfluence, out float crimInfluence, out float hallowInfluence);
                float baseInfluence = MathF.Pow(Math.Max(0f, 1 - hallowInfluence - corroInfluence - crimInfluence), 0.1f);
                if (backgroundPass)
                    baseInfluence = 1f;
                spriteBatch.Draw(tex, textureOrigin + offset, frame, usedColor * baseInfluence, cloud.rotation, frame.Size() / 2f, 1f, 0, 0);


                if (backgroundPass)
                    continue;
                //Tint the clouds
                if (hallowInfluence > 0f)
                {
                    Rectangle hallowFrame = frame;
                    hallowFrame.Y += 204;
                    spriteBatch.Draw(tex, textureOrigin + offset, hallowFrame, usedColor * hallowInfluence, cloud.rotation, frame.Size() / 2f, 1f, 0, 0);
                }
                if (corroInfluence > 0f)
                {
                    Rectangle corroFrame = frame;
                    corroFrame.Y += 204 * 2;
                    spriteBatch.Draw(tex, textureOrigin + offset, corroFrame, usedColor * corroInfluence, cloud.rotation, frame.Size() / 2f, 1f, 0, 0);
                }
                if (crimInfluence > 0f)
                {
                    Rectangle crimFrame = frame;
                    crimFrame.Y += 204 * 3;
                    spriteBatch.Draw(tex, textureOrigin + offset, crimFrame, usedColor * crimInfluence, cloud.rotation, frame.Size() / 2f, 1f, 0, 0);
                }
            }
        }

        public void DrawCloudsFront(SpriteBatch spriteBatch, bool backgroundPass) => DrawClouds(spriteBatch, backgroundPass, true);
        public void DrawCloudsBack(SpriteBatch spriteBatch, bool backgroundPass) => DrawClouds(spriteBatch, backgroundPass, false);
        #endregion

        #region Swirls
        public float swirlSpawnTimer = 0f;

        public void UpdateAndSpawnSwirls()
        {
            swirlSpawnTimer++;
            if (swirlSpawnTimer > 7 && Main.rand.NextBool(4) && !FadingOut)
            {
                swirlSpawnTimer = 0;

                Color colorFront = Main.rand.NextBool(3) ? Color.BurlyWood : Main.rand.NextBool() ? Color.SandyBrown : Color.Wheat;
                DustDevilSwirl newSwirl = new DustDevilSwirl(colorFront, Color.DarkGoldenrod, Main.rand.NextFloat(0.3f, 0.5f));

                if (Main.LocalPlayer.ZoneHallow)
                {
                    newSwirl.color = Main.rand.NextBool(3) ? Color.FloralWhite : Main.rand.NextBool() ? Color.Honeydew : Color.SeaShell;
                    newSwirl.backColor = Color.Pink;
                }
                else if (Main.LocalPlayer.ZoneCorrupt)
                {
                    newSwirl.color = Main.rand.NextBool(3) ? Color.MediumPurple : Main.rand.NextBool() ? Color.MediumOrchid : Color.Plum;
                    newSwirl.backColor = Color.SlateBlue;
                }
                else if (Main.LocalPlayer.ZoneCrimson)
                {
                    newSwirl.color = Main.rand.NextBool(3) ? Color.Crimson : Main.rand.NextBool() ? Color.Salmon : Color.LightCoral;
                    newSwirl.backColor = Color.DarkRed;
                }

               newSwirl.color *= 1.2f;
               newSwirl.backColor *= 1.2f;

                float swirlVerticalOffset = Main.rand.NextFloat(0f, 50f);

                newSwirl.spawnPosition = Projectile.Center;
                newSwirl.flatPos = new Vector2(Main.rand.NextFloat(), -swirlVerticalOffset);
                newSwirl.flatTrajectory = new Vector2(Main.rand.NextFloat(0.3f, 0.6f), -11);
                //Increase and decrease the radius based on the height on the sprite to have a double cone shape
                newSwirl.startRadius = Main.rand.NextFloat(25f, 33f);
                newSwirl.startRadius -= MathF.Pow(Utils.GetLerpValue(0f, 56f, swirlVerticalOffset, true), 0.7f) * 16f;
                newSwirl.radiusChange = -6f;

                float twistBase = MathF.Pow(Utils.GetLerpValue(10f, 0f, swirlVerticalOffset, true), 1.7f);
                newSwirl.startRadius += twistBase * 16f;
                newSwirl.radiusChange -= twistBase * 10f;

                newSwirl.extraThin = true;
                newSwirl.horizontalTwirlMultiplier = -Projectile.velocity.X.NonZeroSign();
                swirls.Add(newSwirl);
            }

            //Update swirls
            for (int i = swirls.Count - 1; i >= 0; i--)
            {
                DustDevilSwirl swirl = swirls[i];
                swirl.timer += 1 / (60f * swirl.lifetime);
                if (swirl.timer > 1)
                {
                    swirls.RemoveAt(i);
                    continue;
                }

                swirl.spawnPosition = Vector2.Lerp(swirl.spawnPosition, Projectile.Center, 0.2f * Utils.GetLerpValue(0.5f, 0.0f, swirl.timer, true));
                swirl.drawOffset = (swirl.spawnPosition - Projectile.Center) * 0.5f;

                swirl.flatPos.X += swirl.horizontalTwirlMultiplier * 0.02f;
            }
        }

        public void DrawSwirls(SpriteBatch spriteBatch)
        {
            Main.spriteBatch.Begin();
            Main.spriteBatch.End();

            int swirlCount = swirls.Count;
            if (swirlCount == 0)
                return;

            //translation so its got the proper origin
            Matrix translation = Matrix.CreateTranslation(100, 100, 0);
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, 400, 200, 0, -1, 1);
            Effect swirlEffect = Scene["DustDevilPrimitive"].GetShader().Shader;
            swirlEffect.Parameters["uWorldViewProjection"].SetValue(translation * projection);

            //Multiply the values by 2 because every swirl will draw twice, once for the front side and once for the back side
            int verticesPerSwirl = 2 * DustDevilSwirl.SWIRL_TRAIL_DEFINITION * 2;
            int indicesPerSwirl = 6 * (DustDevilSwirl.SWIRL_TRAIL_DEFINITION - 1) * 2;

            int vertexCount = verticesPerSwirl * swirlCount;
            int indexCount = indicesPerSwirl * swirlCount;

            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[vertexCount];
            short[] indices = new short[indexCount];

            for (int i = 0; i < swirlCount; i++)
            {
                swirls[i].ConstructPrimitives(i * verticesPerSwirl, i * indicesPerSwirl, ref vertices, ref indices, Vector2.UnitX * 200);
            }

            swirlEffect.CurrentTechnique.Passes[0].Apply();
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount, indices, 0, indexCount / 3);
        }
        #endregion


        public override bool PreDraw(ref Color lightColor)
        {
            if (RenderTargetsDisposed || backCloudRT == null || frontCloudRT == null || primSwirlsRT == null)
                InitializeRenderTargets();

            Vector2 drawOffset = -Main.screenPosition + Vector2.UnitY * 6f;

            DrawSwirlLayer(Main.spriteBatch, primSwirlsRT, Projectile.Bottom + drawOffset, lightColor, true);
            DrawCloudLayer(Main.spriteBatch, backCloudRT, Projectile.Center + drawOffset, lightColor);
            DrawCloudLayer(Main.spriteBatch, frontCloudRT, Projectile.Center + drawOffset, lightColor);
            DrawSwirlLayer(Main.spriteBatch, primSwirlsRT, Projectile.Bottom + drawOffset, lightColor, false);

            return false;
        }
    }

    public class WhiskedDebuff : ModBuff, ICustomDeathMessages
    {
        public float DoTDeathMessagePriority => 1f;
        public bool CustomDeathMessage(Player player, ref PlayerDeathReason deathMessage)
        {
            deathMessage.CustomReason = Language.GetText("Mods.CalamityFables.Extras.DeathMessages.SandTwisterFall." + Main.rand.Next(1, 5).ToString()).ToNetworkText(player.name);
            return true;
        }

        public override string Texture => AssetDirectory.DesertNPCs + Name;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            //Keep track of fallstart for the highest the player went
            if (player.velocity.Y < 0)
                player.fallStart = (int)(player.position.Y / 16f);
            //Easier to take fall dmg
            player.extraFall -= 2;

            if (player.velocity.Y == 0)
            {
                if (Main.myPlayer == player.whoAmI)
                    CameraManager.AddCameraEffect(new DirectionalCameraTug(Vector2.UnitY * 2f, 3f, 25));
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}


