using CalamityFables.Content.Items.EarlyGameMisc;
using CalamityFables.Content.Items.Sky;
using System.IO;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.NPCs.Sky
{
    public class CloudSprite : ModNPC
    {
        public override string Texture => AssetDirectory.SkyNPCs + Name;
        public static Asset<Texture2D> SunTexture;

        public Player target => Main.player[NPC.target];

        public static readonly SoundStyle HitSound = new SoundStyle("CalamityFables/Sounds/CloudSpriteHit") { PitchVariance = 0.4f };
        public static readonly SoundStyle DeathSound = new SoundStyle("CalamityFables/Sounds/CloudSpriteDeath");
        public static readonly SoundStyle WindupSound = new SoundStyle("CalamityFables/Sounds/CloudSpriteWindup");
        public static readonly SoundStyle PunchSound = new SoundStyle("CalamityFables/Sounds/CloudSpritePunch");

        public static int BannerType;
        public static AutoloadedBanner bannerTile;
        public static int cloudDustType;
        public static int fastCloudDustType;


        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Cloud Sprite", AssetDirectory.Banners, out bannerTile);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cloud Sprite");
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                SpriteDirection = 1
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;

            Main.npcFrameCount[Type] = 17;
            bannerTile.NPCType = Type;
            CloudMetaballLayer.cloudSpriteType = Type;
        }

        public override void SetDefaults()
        {
            NPC.noGravity = true;
            NPC.aiStyle = -1;
            NPC.damage = 13;
            NPC.width = 40;
            NPC.height = 40;
            NPC.defense = 5;
            NPC.lifeMax = 90;
            NPC.knockBackResist = 0.55f;
            NPC.value = Item.buyPrice(0, 0, 1, 15);
            NPC.HitSound = HitSound;
            NPC.DeathSound = DeathSound;
            Banner = NPC.type;
            BannerItem = BannerType;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Events.WindyDay,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Events.Rain,

                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.CloudSprite")
            });
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(punchOffset);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            punchOffset = reader.ReadVector2();
        }

        Vector2 eyeOffset = Vector2.Zero;
        public Vector2 SunPosition => NPC.Center - eyeOffset * 2f - Vector2.UnitY * 10f;
        public float lookStrenght = 0.01f;


        public ref float AICounter => ref NPC.ai[0];
        public ref float PunchChargeup => ref NPC.ai[1];
        public bool Punching => PunchChargeup > 0;
        public Vector2 punchOffset = Vector2.Zero;
        public float postPunchAnimation;

        public override void AI()
        {
            //Face the player
            if (target.dead)
            {
                NPC.TargetClosest(true);
                PunchChargeup = 0;
            }

            NPC.spriteDirection = NPC.direction;

            if (eyeOffset == Vector2.Zero)
                eyeOffset = Vector2.UnitX * NPC.direction * 13f;

            float idealAngle = NPC.Center.AngleTo(target.Center);
            float currentAngle = eyeOffset.ToRotation();
            eyeOffset = currentAngle.AngleTowards(idealAngle, 0.06f).ToRotationVector2() * 13f;
            ouchTimer -= 1 / (60f * 0.2f);
            postPunchAnimation -= 1 / (60f * 0.45f);

            if (NPC.collideX)
            {
                NPC.velocity.X *= 1;
                NPC.velocity.Y *= 1.2f;

                if (NPC.velocity.X == 0)
                    NPC.velocity.X = 1.3f * NPC.direction;

                else if (Math.Abs(NPC.velocity.X) < 1.3)
                    NPC.velocity.X = -1.3f * NPC.velocity.X.NonZeroSign();
            }

            if (!Punching)
            {
                NPC.TargetClosest(true);
                if (Math.Abs(NPC.Center.X - target.Center.X) > 200)
                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, (target.Center.X - NPC.Center.X).NonZeroSign() * 3.5f, 0.02f);
                else if (NPC.velocity.X == 0)
                    NPC.velocity.X = NPC.direction * 0.2f;
                else if (Math.Abs(NPC.velocity.X) < 0.4f)
                    NPC.velocity *= 1.06f;

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

                //Decide to punch
                if (AICounter > 300 && NPC.WithinRange(target.Center, 600) && Collision.CanHitLine(target.position, target.width, target.height, NPC.position, NPC.width, NPC.height))
                {
                    punchOffset = new Vector2((NPC.Center.X - target.Center.X).NonZeroSign() * Main.rand.NextFloat(100f, 260f), Main.rand.NextFloat(100f, 200f));
                    if (Main.rand.NextBool(3))
                        punchOffset.X *= -1;

                    NPC.netUpdate = true;

                    AICounter = 0;
                    PunchChargeup = 1f;
                }
            }

            else
            {
                NPC.direction = (target.Center.X - NPC.Center.X).NonZeroSign();
                Vector2 targetPosition = target.Center - punchOffset;
                float impatience = Utils.GetLerpValue(60 * 5, 60 * 10, AICounter, true);

                //Getting to the offset
                if (PunchChargeup == 1 && !NPC.WithinRange(targetPosition, 20 + impatience * 40))
                {
                    float speed = 3f + 8f * Utils.GetLerpValue(200, 600, NPC.Distance(targetPosition), true) + 3f * impatience;
                    NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(targetPosition) * speed, 0.04f);
                }

                //Reached punch position
                else if (PunchChargeup == 1)
                {
                    NPC.velocity *= 0.5f;
                    PunchChargeup -= 0.01f;
                    SoundEngine.PlaySound(WindupSound with { Volume = 0.6f}, NPC.Center);
                }

                //Chargeup of punch
                else
                {
                    NPC.velocity *= 0.97f;
                    PunchChargeup -= 1 / (60f * 0.7f);

                    Vector2 fistPosition = NPC.Center - NPC.direction * Vector2.UnitX * 55f;
                    Vector2 fistSpin = Vector2.UnitY.RotatedBy(PunchChargeup * MathHelper.TwoPi * 2f);

                    for (int i = 0; i < 2; i++)
                    {
                        Dust d = Dust.NewDustPerfect(fistPosition + fistSpin * 13f + Main.rand.NextVector2Circular(7f, 7f), fastCloudDustType, fistSpin.RotatedBy(MathHelper.PiOver2 * NPC.direction) * 0.5f, 0, Color.White);
                        d.scale = Main.rand.NextFloat(0.4f, 0.9f);
                    }
                }

                //Releasing the punch
                if (!Punching)
                {
                    SoundEngine.PlaySound(PunchSound, NPC.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.DirectionTo(target.Center) * 10f + Vector2.UnitY * Math.Abs(target.Center.X - NPC.Center.X) / 120f, ModContent.ProjectileType<CloudSpriteFist>(), 20, 10, Main.myPlayer, target.Center.X, target.Center.Y);
                        AICounter = Main.rand.NextFloat(-300f, 0f);
                        NPC.netUpdate = true;
                    }
                    else
                        AICounter = -300;

                    NPC.velocity += NPC.SafeDirectionTo(target.Center) * 2f;
                    postPunchAnimation = 1;
                }

                //Stop trying if after 10 seconds we can't reach the target
                else if (AICounter > 60 * 10)
                {
                    PunchChargeup = 0;
                    AICounter = 0;
                }
            }

            AICounter++;

            Lighting.AddLight(SunPosition, Color.Goldenrod.ToVector3());
            //Tilt in the direction of the movement
            NPC.rotation = Math.Clamp(NPC.velocity.X * 0.04f, -MathHelper.PiOver4, MathHelper.PiOver4);

            if (Main.rand.NextBool(4))
            {
                Vector2 dustDirection = Main.rand.NextVector2Circular(1f, 1f);
                dustDirection.X *= 0.6f;

                Dust d = Dust.NewDustPerfect(NPC.Center - Vector2.UnitY * 15f + dustDirection * 30f, fastCloudDustType, dustDirection * 0.5f, 0, Color.White);
                d.velocity.X *= Main.rand.NextFloat(0.2f, 0.7f);

                d.scale = Main.rand.NextFloat(0.5f, 0.9f);
                d.velocity += NPC.velocity * 0.2f;
            }

            if (!Main.rand.NextBool(5))
            {
                Vector2 dustDirection = Main.rand.NextVector2Circular(1f, 1f);
                Dust d = Dust.NewDustPerfect(NPC.Center + dustDirection * 10f, cloudDustType, dustDirection, 0, Color.White);
                d.velocity.X *= Main.rand.NextFloat(0.2f, 0.7f);
                if (d.velocity.Y > 0)
                    d.velocity.Y *= 0.4f;

                if (d.velocity.X.NonZeroSign() == NPC.direction)
                    d.velocity.X *= 0.4f;

                d.scale = Main.rand.NextFloat(1f, 1.3f);
                d.velocity += NPC.velocity * 0.8f;

                d.velocity.Y -= Main.rand.NextFloat(0f, 0.3f);
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
        public override bool? CanFallThroughPlatforms() => true;

        public float ouchTimer;
        public int bestiaryFrameCounter = 0;
        public float bestiaryFrameTick;

        public override void FindFrame(int frameHeight)
        {
            NPC.frame.Width = NPC.width;
            NPC.frame.Height = NPC.height;


            NPC.frameCounter += 0.1;
            if (NPC.frameCounter > 1)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = (NPC.frame.Y + 1) % 6;
            }

            if (Punching && PunchChargeup < 1)
                NPC.frame.Y = 6 + (int)(6 * (1 - PunchChargeup));

            if (ouchTimer > 0)
                NPC.frame.Y = 7 + (int)((1 - ouchTimer) * 2.999f);

            if (postPunchAnimation > 0)
                NPC.frame.Y = 12 + (int)(5 * (1 - postPunchAnimation));

            if (NPC.IsABestiaryIconDummy)
            {
                bestiaryFrameTick++;
                if (bestiaryFrameTick > 4)
                {
                    bestiaryFrameTick = 0;
                    bestiaryFrameCounter++;
                }    
            }
        }

        public static void DrawCloudSpriteSun(Vector2 screenPos, Vector2 sunPos, float scale, float lensFlareOpacity = 1f)
        {
            SunTexture ??= ModContent.Request<Texture2D>(AssetDirectory.SkyNPCs + "CloudSprite_Sun");
            Texture2D sun = SunTexture.Value;
            Texture2D sunBloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;

            Rectangle sunFrame = sun.Frame(1, 6, 0, (int)(Main.GlobalTimeWrappedHourly * 3f % 6), 0, -2);
            float wobble = MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.2f;

            Main.spriteBatch.Draw(sun, sunPos - screenPos, sunFrame, Color.White, 0f, sunFrame.Size() / 2f, scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(sunBloom, sunPos - screenPos, null, Color.OrangeRed with { A = 0 } * 0.2f, 0f, sunBloom.Size() / 2f, scale * (0.77f + wobble), SpriteEffects.None, 0);

            Main.spriteBatch.Draw(lensFlare, sunPos - screenPos, null, Color.White with { A = 0 } * 0.7f * lensFlareOpacity, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(0.4f, 1.7f + wobble) * scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(lensFlare, sunPos - screenPos, null, Color.Orange with { A = 0 } * 0.4f * lensFlareOpacity, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(1f - wobble, 1.7f + wobble) * scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(lensFlare, sunPos - screenPos, null, Color.Orange with { A = 0 } * 0.2f * lensFlareOpacity, MathHelper.PiOver2, lensFlare.Size() / 2f, new Vector2(1f - wobble, 1.7f + wobble) * 1.2f * scale, SpriteEffects.None, 0);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = NPC.TintFromBuffAesthetic(drawColor);

            //Fsr the icon dummy uses a fullblack color by default?
            if (NPC.IsABestiaryIconDummy)
            {
                screenPos.Y -= 16;
                eyeOffset = Vector2.UnitX * 13;
                drawColor = Color.White;
                DrawCloudSpriteSun(screenPos + Vector2.UnitY * 10f, SunPosition, NPC.scale);

                Texture2D bestiaryPlaceholder = ModContent.Request<Texture2D>(Texture + "BestiarySmoke").Value;
                Rectangle bestiaryFrame = bestiaryPlaceholder.Frame(31, 1, bestiaryFrameCounter % 31, 0, -2);
                Main.spriteBatch.Draw(bestiaryPlaceholder, NPC.Center - Vector2.UnitY * 30 - screenPos, bestiaryFrame, drawColor, 0, bestiaryFrame.Size() / 2f, NPC.scale, 0, 0f);
            }

            Texture2D tex = TextureAssets.Npc[Type].Value;
            Rectangle frame = tex.Frame(1, 17, 0, NPC.frame.Y, 0, -2);
            SpriteEffects flip = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 eyePos = NPC.Center + (eyeOffset with { X = eyeOffset.X * 0.6f}) - Vector2.UnitY * 5f;


            Vector2 directionEye = eyeOffset;
            if (eyeOffset.X < 0)
                directionEye *= -1;
            float eyeRotation = directionEye.ToRotation() * 0.4f;
            float scale = NPC.scale;
            

            if (postPunchAnimation > 0)
            {
                scale *= 1 + (float)Math.Pow(postPunchAnimation, 2f) * 0.7f;
                eyePos += Main.rand.NextVector2Circular(5f, 16f) * postPunchAnimation;
            }

            else if (ouchTimer > 0)
            {
                eyePos += Main.rand.NextVector2Circular(12f, 12f) * ouchTimer;
                scale += Main.rand.NextFloat(0.3f, 0.7f) * ouchTimer;
            }

            Main.spriteBatch.Draw(tex, eyePos - screenPos, frame, Color.White * (drawColor.A / 255f), eyeRotation, frame.Size() / 2f, scale, flip, 0f);

            return false;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (Main.raining || Main.IsItAHappyWindyDay)
                return SpawnCondition.Sky.Chance;
            return 0f;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (!Main.dedServ)
            {
                ouchTimer = 1f;

                if (NPC.life <= 0)
                {

                    for (int i = 0; i < 13; i++)
                    {
                        Vector2 dustDirection = Main.rand.NextVector2Circular(1f, 1f);
                        Dust d = Dust.NewDustPerfect(SunPosition + Main.rand.NextVector2Circular(10f, 10f), DustID.SolarFlare, dustDirection * Main.rand.NextFloat(4f, 7f), 0, Color.White);
                        d.scale = Main.rand.NextFloat(0.9f, 1.9f);
                        d.noGravity = true;
                    }

                    for (int i = 0; i < 20;i ++)
                    {
                        Vector2 dustDirection = Main.rand.NextVector2Circular(1f, 1f);
                        Dust d = Dust.NewDustPerfect(NPC.Center + dustDirection * 32f, fastCloudDustType, dustDirection.RotatedBy(MathHelper.PiOver2 * NPC.direction) * 2.5f, 0, Color.White);
                        d.scale = Main.rand.NextFloat(0.4f, 0.9f);
                    }
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemID.Cloud, 1, 5, 10);
            npcLoot.Add(ModContent.ItemType<CloudInAMace>(), 30);
            npcLoot.Add(ItemID.CloudinaBottle, 30);
            npcLoot.Add(ModContent.ItemType<CloudSpriteInABottle>(), 30);
        }
    }

    public class CloudSpriteFist : ModProjectile
    {
        public override string Texture => AssetDirectory.SkyNPCs + "CloudSprite_Fist";

        internal PrimitiveTrail TrailDrawer;
        private List<Vector2> cache;
        public ref float CurveBackDirection => ref Projectile.ai[0];
        public Vector2 TargetPosition => new Vector2(Projectile.ai[0], Projectile.ai[1]);


        public bool CurvingBackUp {
            get => Projectile.ai[2] == 1;
            set => Projectile.ai[2] = value ? 1 : 0;
        }


        public float originalHeight = -1;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cloudy Fist");
        }

        public override void SetDefaults()
        {
            Projectile.width = 25;
            Projectile.height = 25;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 70;
            Projectile.extraUpdates = 1;
            Projectile.rotation = Main.rand.Next() * MathHelper.TwoPi;
            originalHeight = Projectile.Center.Y;
            Projectile.hide = true;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(Projectile.whoAmI);
        }

        public override void AI()
        {
            if (!CurvingBackUp)
            {
                Vector2 directionToTarget = Projectile.Center.SafeDirectionTo(TargetPosition);
                if (directionToTarget == Vector2.Zero)
                    directionToTarget = Projectile.velocity.X.NonZeroSign() * Vector2.UnitX;

                //Aims harder the closest to the target height we are. This is mostly to avoid a crash into the floor because it can't correct itself to aim up fast enough
                float lerpStrenght = 0.06f + Utils.GetLerpValue(Math.Max(originalHeight, TargetPosition.Y - 60), TargetPosition.Y, Projectile.Center.Y, true) * 0.2f;


                Projectile.velocity = Vector2.Lerp(Projectile.velocity, directionToTarget * (Projectile.velocity.Length() + 0.1f), lerpStrenght);

                if (Projectile.velocity.Length() < 6f)
                    Projectile.velocity *= 1.05f;

                //Projectile.velocity.Y += 0.06f;

                if (Projectile.Center.Y > TargetPosition.Y || Projectile.velocity.Y <= 0.5f)
                {
                    CurvingBackUp = true;
                    CurveBackDirection = Projectile.velocity.X.NonZeroSign();
                }
            }

            else
            {
                Projectile.velocity.X -= CurveBackDirection * 0.2f;
                Projectile.velocity.X *= 0.98f;
                Projectile.velocity.Y -= 0.2f;
                if (Projectile.velocity.Y < -2.5f)
                    Projectile.velocity.Y = -2.5f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.rand.NextBool())
            {
                Vector2 dustDirection = Main.rand.NextVector2Circular(1f, 1f);
                Dust d = Dust.NewDustPerfect(Projectile.Center + dustDirection * 10f, CloudSprite.fastCloudDustType, dustDirection * 0.5f, 0, Color.White);
                d.velocity.X *= Main.rand.NextFloat(0.2f, 0.7f);
                d.scale = Main.rand.NextFloat(0.5f, 0.9f);
                d.velocity -= Projectile.velocity * 0.2f;
                d.alpha = Main.rand.Next(10, 20);
            }

            Dust.NewDustPerfect(Projectile.Center, CloudSprite.cloudDustType, Projectile.velocity * 0.4f, 0, Color.White, Main.rand.NextFloat(0.5f, 0.7f));
            Dust cloudDust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f), DustID.Cloud, -Projectile.velocity * 0.2f, 0, Color.White, Main.rand.NextFloat(0.9f, 1.7f));
            cloudDust.noGravity = true;

            if (!Main.dedServ)
            {
                ManageCache();
                ManageTrail();
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //Dies on wall collision
            if (oldVelocity.X != Projectile.velocity.X || oldVelocity.Y < 0)
                return true;

            Projectile.velocity.Y = oldVelocity.Y *= -0.7f;
            return false;
        }

        //Set knockback to zero here so that we can apply our custom kb in onhit
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
             modifiers.Knockback *= 0;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (target.noKnockback)
                return;

            target.velocity = new Vector2(Projectile.velocity.X.NonZeroSign() * 14f, -7);
            target.jump = Player.jumpHeight / 2;
            target.fallStart = (int)(target.position.Y / 16f);
        }

        //Can't hit the player after you start curving too far back
        public override bool CanHitPlayer(Player target) => CurvingBackUp && Projectile.velocity.X * CurveBackDirection > 0.4f;

        #region Prims
        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);

            if (Main.dedServ)
                return;

            Dust.NewDustPerfect(Projectile.Center, CloudSprite.cloudDustType, Projectile.velocity * 0.12f, 0, Color.White, Main.rand.NextFloat(0.9f, 1.2f));
            Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), CloudSprite.cloudDustType, Projectile.velocity * 0.16f, 0, Color.White, Main.rand.NextFloat(0.5f, 0.7f));

            GhostTrail clone = new GhostTrail(cache, TrailDrawer, 0.5f, null, "Primitive_StreakyTrail", delegate (Effect effect, float fading) {
                CloudMetaballLayer.GetCloudPalette(out Vector3 cloudColor, out _, out Vector3 skyColor, out _, out _);
                CloudFistColor = new Color(cloudColor * skyColor);

                effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
                effect.Parameters["verticalStretch"].SetValue(0.5f);
                effect.Parameters["repeats"].SetValue(4f);
                effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
                effect.Parameters["overlayOpacity"].SetValue(0.5f);
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
                effect.Parameters["streakNoiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise").Value);
                effect.Parameters["streakScale"].SetValue(1f);
            });
            clone.DrawLayer = DrawhookLayer.BehindTiles;
            clone.ShrinkTrailLenght = true;

            GhostTrailsHandler.LogNewTrail(clone);
        }
        private void ManageCache()
        {
            if (cache == null)
            {
                cache = new List<Vector2>();
                for (int i = 0; i < 50; i++)
                    cache.Add(Projectile.Center);
            }

            cache.Add(Projectile.Center);
            while (cache.Count > 50)
                cache.RemoveAt(0);
        }

        private void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, PrimWidthFunction, PrimColorFunction);

            TrailDrawer.SetPositions(cache);
            TrailDrawer.NextPosition = Projectile.Center + Projectile.velocity;
        }

        public static Color CloudFistColor;
        public float PrimWidthFunction(float completion)
        {
            return completion * 15;
        }

        public Color PrimColorFunction(float completion)
        {
            return CloudFistColor * 0.5f * (float)Math.Pow(completion, 2f);
        }


        #endregion

        public override bool PreDraw(ref Color lightColor)
        {
            CloudMetaballLayer.GetCloudPalette(out Vector3 cloudColor, out _, out Vector3 skyColor, out Vector3 shadowColor, out Vector3 outlineColor);
            cloudColor *= skyColor;
            shadowColor *= cloudColor;

            if (TrailDrawer != null)
            {
                Effect effect = AssetDirectory.PrimShaders.StreakyTrail;
                effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
                effect.Parameters["verticalStretch"].SetValue(0.5f);
                effect.Parameters["repeats"].SetValue(4f);
                effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
                effect.Parameters["overlayOpacity"].SetValue(0.5f);
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
                effect.Parameters["streakNoiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise").Value);
                effect.Parameters["streakScale"].SetValue(1f);

                CloudFistColor = new Color(cloudColor);
                TrailDrawer?.Render(effect, -Main.screenPosition);
            }

            Vector2 position = Projectile.Center - Main.screenPosition;
            float rotation = Projectile.rotation + MathHelper.PiOver2;

            Texture2D fistTex = TextureAssets.Projectile[Type].Value;
            Texture2D fistShadowTex = ModContent.Request<Texture2D>(Texture + "Shadow").Value;
            Texture2D fistOutlineTex = ModContent.Request<Texture2D>(Texture + "Outline").Value; 
            Texture2D fistOutlineShadowTex = ModContent.Request<Texture2D>(Texture + "OutlineShadow").Value;
            Vector2 origin = fistTex.Size() * 0.5f;
            SpriteEffects effects = Projectile.velocity.X > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            float scale = Projectile.scale;
            if (Projectile.timeLeft > 30)
            {
                scale *= 1f + (float)Math.Pow((Projectile.timeLeft - 30f) / 40f, 1.6f);
                position += Main.rand.NextVector2Circular(3f, 3f) * (float)Math.Pow((Projectile.timeLeft - 30f) / 40f, 1.6f);
            }

            Main.EntitySpriteDraw(fistOutlineTex, position, null, new Color(cloudColor * outlineColor), rotation, origin, scale, effects);
            Main.EntitySpriteDraw(fistOutlineShadowTex, position, null, new Color(shadowColor * outlineColor), rotation, origin, scale, effects);
            Main.EntitySpriteDraw(fistTex, position, null, new Color(cloudColor), rotation, origin, scale, effects);
            Main.EntitySpriteDraw(fistShadowTex, position, null, new Color(shadowColor), rotation, origin, scale, effects);

            return false;
        }
    }
}

