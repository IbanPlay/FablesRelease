using CalamityFables.Particles;
using ReLogic.Utilities;
using Terraria.GameContent.ItemDropRules;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    [ReplacingCalamity("TeardropCleaver")]
    public class EyetoothRipper : ModItem
    {
        public static readonly SoundStyle StopUseSound = new SoundStyle("CalamityFables/Sounds/EyetoothRipperEndUse");
        public static readonly SoundStyle DryRevvingSound = new SoundStyle("CalamityFables/Sounds/EyetoothRipperDryRevving");
        public static readonly SoundStyle WetRevvingSound = new SoundStyle("CalamityFables/Sounds/EyetoothRipperWetRevving");
        public static readonly SoundStyle RoarRevvingSound = new SoundStyle("CalamityFables/Sounds/EyetoothRipperRoarRevving");
        public static readonly SoundStyle EngineLoopSound = new SoundStyle("CalamityFables/Sounds/EyetoothRipperStuckLoop") { IsLooped = true, MaxInstances = 0, PlayOnlyIfFocused = true };
        public static readonly SoundStyle StuckClickSound = new SoundStyle("CalamityFables/Sounds/EyetoothRipperStuckClick");

        public static readonly SoundStyle CuttingLoopSound = new SoundStyle("CalamityFables/Sounds/EyetoothRipperCuttingLoop") { IsLooped = true, MaxInstances = 0, PlayOnlyIfFocused = true };

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void Load()
        {
            FablesNPC.ModifyNPCLootEvent += DropFromEOC;
            FablesItem.ModifyItemLootEvent += DropFromEOCBossBag;
        }

        private void DropFromEOCBossBag(Item item, ItemLoot itemLoot)
        {
            if (item.type == ItemID.EyeOfCthulhuBossBag)
            {
                itemLoot.Add(Type, new Fraction(1, 3));
            }
        }

        private void DropFromEOC(NPC npc, NPCLoot npcloot)
        {
            if (npc.type == NPCID.EyeofCthulhu)
            {
                LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
                notExpertRule.Add(Type, new Fraction(1, 3));
                npcloot.Add(notExpertRule);
            }
        }

        public static float HITSTOFULLYEXPAND = 6;
        public static float HITSTOFULLYCLOG = 10;
        public static float MINIMUMCLOGTOFIRE = 0.33f;


        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Eyetooth Ripper");
            Tooltip.SetDefault("Clogs up with gore when striking enemies\n" +
                "Right click to expel the clogged up gore\n" +
                "[c/A1A1A1:'What are you, some sort of circular-saw man?']"
                );

            if (Main.dedServ)
                return;
            for (int i = 1; i < 7; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("MincedMeatGore" + i.ToString()).Type] = true;
        }

        public override void SetDefaults()
        {
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.damage = 16;
            Item.ArmorPenetration = 5;
            Item.knockBack = 4f;
            Item.axe = 9;

            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.width = 36;
            Item.height = 18;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = Item.sellPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = DryRevvingSound;// SoundID.Item23;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EyetoothRipperProjectile>();
        }

        public override void HoldItem(Player player)
        {
            player.SyncMousePosition();
            player.SyncRightClick();
        }
    }

    public struct EyetoothRipperGore
    {
        public int variant;
        public float rotation;
        public float distance;
        public float spin;

        public EyetoothRipperGore(float radius)
        {
            variant = Main.rand.Next(6);
            rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            spin = Main.rand.NextFloat(MathHelper.TwoPi);
            distance = Main.rand.NextFloat(0.15f, 1f) * radius;
        }
    }

    public class EyetoothRipperProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Eyetooth Ripper");
            ProjectileID.Sets.AllowsContactDamageFromJellyfish[Type] = true;
        }

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        public static Asset<Texture2D> ScrewTex;
        public static Asset<Texture2D> ToothTex;
        public static Asset<Texture2D> ToothChainTex;
        public static Asset<Texture2D> ChainTex;

        private SlotId cuttingSoundSlot;
        private SlotId engineLoopSlot;

        public Player Owner => Main.player[Projectile.owner];
        public float Charge {
            get {
                return MathHelper.Clamp(Projectile.ai[0], 0f, 1f);
            }
        }

        public ref float FullCharge => ref Projectile.ai[0];
        public bool Clogged => FullCharge >= 2;

        public ref float Timer => ref Projectile.localAI[0];

        public bool hasBlinked = false;
        public float blinkTimer = 0;
        public float afterImageTimer = 0;
        public float cuttingSoundTimer = 0;
        public float cuttingSoundPitchStreak = 0;
        public float clogStuckTimer = 0;
        public const float MAXGORE = 10;

        public List<EyetoothRipperGore> attachedGore;

        public float ScrewOffset {
            get {
                float offset = 46f + 22f * FablesUtils.PolyInOutEasing(Charge);
                if (Clogged)
                    offset -= 7f;
                return offset;
            }
        }
        public Vector2 ScrewPosition {
            get {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                return (Owner.MountedCenter + Vector2.UnitY * Owner.gfxOffY) + direction * ScrewOffset;
            }
        }
        public float AttackRadius => (4f + 10f * FablesUtils.PolyInOutEasing(Charge));

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            // Kill the projectile if the player dies or gets crowd controlled
            if (!Owner.channel || !Owner.active || Owner.dead || Owner.noItems || Owner.CCed || Vector2.Distance(Projectile.Center, Owner.Center) > 900f)
            {
                Projectile.Kill();
                return;
            }
            if (Main.myPlayer == Projectile.owner && Main.mapFullscreen)
            {
                Projectile.Kill();
                return;
            }


            #region Spinning the wheel
            //Make the wheel spin. Wheel spins faster right after a hit, and spins faster as charged up.
            float timerIncrease = 1f + afterImageTimer * 0.6f + Charge * 0.7f;
            //Slow down if clogged
            if (Clogged)
                timerIncrease *= 0.1f;
            else if (FullCharge > 1)
                timerIncrease *= 0.5f + 0.5f * (1 - attachedGore.Count / MAXGORE);
            Timer += timerIncrease;
            #endregion

            #region Clog effects
            if (Clogged)
            {
                //Make the wheel go back like it's stuck when clogged
                if (clogStuckTimer >= 1)
                {
                    Timer -= timerIncrease * 23;
                    clogStuckTimer = 0f;
                    SoundEngine.PlaySound(EyetoothRipper.StuckClickSound, Projectile.Center);
                }

                clogStuckTimer += 1 / 30f;

                //Make smoke come out
                if (Main.rand.NextBool(5))
                {
                    Particle smoke = new SmokeParticle(Vector2.Lerp(Owner.Center, ScrewPosition, Main.rand.NextFloat(0.3f, 0.6f)), -Vector2.UnitY, Color.Black * 0.4f, Color.DimGray * 0.4f, Main.rand.NextFloat(0.8f, 1f), 1f, 20, 0.03f);
                    ParticleHandler.SpawnParticle(smoke);
                }
            }
            #endregion

            ManageSounds();

            if (attachedGore == null)
                attachedGore = new List<EyetoothRipperGore>();

            //Add extra gore to the wheel as it charges
            if (FullCharge - 1f > 0f && attachedGore.Count < (FullCharge - 1) * MAXGORE)
                attachedGore.Add(new EyetoothRipperGore(AttackRadius));


            if (Owner.RightClicking())
                VomitChunks();

            //Visuals for when fully charged up.
            if (Main.myPlayer == Projectile.owner && !hasBlinked && Charge >= 1f)
            {
                hasBlinked = true;
                blinkTimer = 1f;
                SoundEngine.PlaySound(SpikedBall.FullChargeSound);
            }

            //Point at mouse
            Projectile.velocity = (Owner.MouseWorld() - Owner.MountedCenter).SafeNormalize(Vector2.UnitY);

            //Create blood particles around the wheel
            if (Main.rand.NextBool(3) && !Clogged)
            {
                Vector2 screwPosition = ScrewPosition;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 dustSpeed = Main.rand.NextVector2Circular(1f, 1f) * (AttackRadius + 10);
                    Vector2 dustPosition = screwPosition + dustSpeed;
                    dustSpeed = dustSpeed.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2 * Owner.direction) * Main.rand.NextFloat(2f, 4f);

                    Dust d = Dust.NewDustPerfect(dustPosition, DustID.Blood, dustSpeed, Scale: Main.rand.NextFloat(0.8f, 1.2f) + 0.4f * Charge);
                    d.noGravity = true;
                }
            }

            //Manipulate player variables
            Owner.heldProj = Projectile.whoAmI;
            Owner.ChangeDir(Math.Sign(Projectile.velocity.X));
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation() * Owner.gravDir - MathHelper.PiOver2);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation() * Owner.gravDir - MathHelper.PiOver2 - MathHelper.PiOver4 * 0.5f * Owner.direction);

            Owner.SetDummyItemTime(2);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Center = Owner.MountedCenter;
            Projectile.timeLeft = 2;
        }

        //Circular hitbox centered on the screw
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => FablesUtils.AABBvCircle(targetHitbox, ScrewPosition, AttackRadius + 20);

        public override void CutTiles()
        {
            //This isn't even used lol
            DelegateMethods.tilecut_0 = Terraria.Enums.TileCuttingContext.AttackProjectile;

            Point sawCenter = ScrewPosition.ToTileCoordinates();


            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    DelegateMethods.CutTiles(sawCenter.X + i, sawCenter.Y + j);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!target.CountsAsACritter)
            {
                Projectile.netUpdate = true;
                bool unclogged = !Clogged;

                //Expand/Clog at different times
                if (FullCharge < 1)
                    FullCharge += 1 / EyetoothRipper.HITSTOFULLYEXPAND;
                else
                {
                    FullCharge += 1 / EyetoothRipper.HITSTOFULLYCLOG;
                    SoundEngine.PlaySound(SoundID.NPCDeath32 with { Volume = 0.3f, PitchVariance = 0.4f, MaxInstances = 0 }); //Play a fleshy sound as it accumulates flesh
                }


                //Visual effects if it just got clogged
                if (unclogged && Clogged)
                {
                    CameraManager.Quake += 10;
                    VignetteFadeEffects.AddVignetteEffect(new VignettePunchModifier(30, 0.2f));
                    SoundEngine.PlaySound(SoundID.NPCDeath23);
                }

                //Make the wheel spin faster
                afterImageTimer = 1f;
                cuttingSoundTimer = 1f;
            }

            //Create blood effects
            Vector2 screwPosition = ScrewPosition;
            for (int j = 0; j < 4; j++)
            {
                Vector2 dustCenter = screwPosition + Main.rand.NextVector2Circular(5f, 5f);
                dustCenter += Projectile.velocity.RotatedBy(MathHelper.PiOver4) * Main.rand.NextFloat(-1f, 1f) * 11f;

                int dustCount = Main.rand.Next(2, 7);
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 dustSpeed = Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.56f) * Main.rand.NextFloat(2f, 5.5f);
                    Dust.NewDustPerfect(dustCenter, DustID.Blood, dustSpeed, Scale: Main.rand.NextFloat(1.3f, 2f));
                }
            }
        }

        public override bool? CanDamage() => !Clogged ? null : false;

        public override void OnKill(int timeLeft)
        {
            Owner.SetDummyItemTime(20);
            SoundEngine.PlaySound(EyetoothRipper.StopUseSound with { Volume = 0.3f }, Projectile.Center);
            VomitChunks();
            base.OnKill(timeLeft);
        }

        public void VomitChunks()
        {
            float clogProgress = FullCharge - 1f;

            if (clogProgress <= 0f)
                return;

            //Screenshake
            if (Main.myPlayer == Projectile.owner)
                CameraManager.Quake += 8 * (float)Math.Pow(clogProgress, 0.6f);


            Vector2 screwPosition = ScrewPosition;
            bool canFireGore = clogProgress > EyetoothRipper.MINIMUMCLOGTOFIRE;
            bool fullFire = clogProgress >= 1;

            if (!canFireGore)
                SoundEngine.PlaySound(EyetoothRipper.DryRevvingSound, Projectile.Center);
            else if (!fullFire)
                SoundEngine.PlaySound(EyetoothRipper.WetRevvingSound, Projectile.Center);
            else if (fullFire)
                SoundEngine.PlaySound(EyetoothRipper.RoarRevvingSound with { Volume = 0.5f }, Projectile.Center);

            //Vomit the gore out
            if (!Main.dedServ)
            {
                if (!canFireGore)
                {
                    foreach (EyetoothRipperGore gore in attachedGore)
                    {
                        Vector2 gorePosition = screwPosition + Main.rand.NextVector2Circular(4f, 4f);
                        Vector2 goreVelocity = Main.rand.NextVector2Circular(3f, 3f) + Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 1.5f) * Main.rand.NextFloat(0.5f, 1.5f) * (0.3f + 0.5f * clogProgress);
                        goreVelocity -= Vector2.UnitY * 2f;

                        Gore g = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), gorePosition, goreVelocity, Mod.Find<ModGore>("MincedMeatGore" + (gore.variant + 1).ToString()).Type, Main.rand.NextFloat(0.7f, 1f));
                        g.alpha = Main.rand.Next(100);
                    }
                }

                for (int j = 0; j < 4; j++)
                {
                    Vector2 dustCenter = screwPosition + Main.rand.NextVector2Circular(5f, 5f);
                    dustCenter += Projectile.velocity.RotatedBy(MathHelper.PiOver4) * Main.rand.NextFloat(-1f, 1f) * 11f;

                    int dustCount = Main.rand.Next(2, 7);
                    for (int i = 0; i < dustCount; i++)
                    {
                        Vector2 dustSpeed = Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.56f) * Main.rand.NextFloat(0.5f, 3.5f) - Vector2.UnitY * 2f;
                        Dust.NewDustPerfect(dustCenter, DustID.Blood, dustSpeed, Scale: Main.rand.NextFloat(1.3f, 2f));
                    }
                }

                for (int i = 0; i < 10 + 20 * clogProgress; i++)
                {
                    Dust.NewDustPerfect(screwPosition + Main.rand.NextVector2Circular(5f, 5f), DustID.Blood, Main.rand.NextVector2Circular(5f, 5f), Scale: Main.rand.NextFloat(1.3f, 2f));
                }
            }

            if (canFireGore && Main.myPlayer == Projectile.owner)
            {
                CameraManager.Quake += 12;

                float projSpeed = 10f + 8f * clogProgress;
                int projDamage = (int)(Projectile.damage * (3f + 2f * clogProgress));
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), screwPosition, Projectile.velocity * projSpeed, ModContent.ProjectileType<EyetoothMincedMeat>(), projDamage, Projectile.knockBack * clogProgress, Main.myPlayer, clogProgress);
            }

            FullCharge = 0.65f;
            attachedGore.Clear();
            clogStuckTimer = 0;
            afterImageTimer = 1f;
            hasBlinked = false;
            Projectile.netUpdate = true;
        }

        public void ManageSounds()
        {
            //Play the engine loop sound
            if (!SoundEngine.TryGetActiveSound(engineLoopSlot, out var sound))
                engineLoopSlot = SoundEngine.PlaySound(EyetoothRipper.EngineLoopSound, Projectile.Center);
            if (SoundEngine.TryGetActiveSound(engineLoopSlot, out sound))
            {
                sound.Position = Projectile.Center;
                sound.Volume = 0.3f + 0.7f * Charge;

                sound.Update();
            }

            if (!SoundEngine.TryGetActiveSound(cuttingSoundSlot, out sound))
                cuttingSoundSlot = SoundEngine.PlaySound(EyetoothRipper.CuttingLoopSound, Projectile.Center);
            if (SoundEngine.TryGetActiveSound(cuttingSoundSlot, out sound))
            {
                sound.Position = Projectile.Center;

                if (cuttingSoundTimer > 0)
                {
                    sound.Sound.Pitch = 0 + (float)Math.Pow(cuttingSoundTimer, 2f) * 0.1f + cuttingSoundPitchStreak * 0.4f;
                    sound.Volume = (float)Math.Pow(cuttingSoundTimer, 0.7f);
                    SoundHandler.TrackSoundWithFade(cuttingSoundSlot);
                }

                else
                    sound.Volume = 0;

                sound.Update();
            }

            float cuttingSoundFadeTime = Clogged ? 0.6f : 1f;
            cuttingSoundTimer -= 1 / (60f * cuttingSoundFadeTime);

            if (cuttingSoundTimer <= 0.4f)
                cuttingSoundPitchStreak = Math.Max(0, cuttingSoundPitchStreak - 1 / (60f * 0.3f));
            else
                cuttingSoundPitchStreak = Math.Min(1, cuttingSoundPitchStreak + 1 / (60f * 2f));

            //Track the loops to prevent accidental forever loops
            SoundHandler.TrackSound(engineLoopSlot);
        }

        #region Drawing
        public override bool PreDraw(ref Color lightColor)
        {
            if (!Projectile.active)
                return false;

            ChainTex = ChainTex ?? ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "EyetoothRipperChain");
            ScrewTex = ScrewTex ?? ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "EyetoothRipperScrew");
            ToothTex = ToothTex ?? ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "EyetoothRipperTooth");
            ToothChainTex = ToothChainTex ?? ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "EyetoothRipperToothConnection");

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.Zero);

            DrawBlade(lightColor, direction);

            //Draw the holdout.
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = tex.Frame(1, 4, 0, (int)(Charge * 2), 0, -2);
            if (Clogged)
                frame = tex.Frame(1, 4, 0, 3, 0, -2);

            Vector2 origin = new Vector2(10, frame.Height / 2f);
            SpriteEffects effect = SpriteEffects.None;
            if (Owner.direction * Owner.gravDir < 0)
            {
                effect = SpriteEffects.FlipVertically;
            }
            Vector2 position = Owner.MountedCenter + direction * 10f + Vector2.UnitY * Owner.gfxOffY + Main.rand.NextVector2Circular(1f, 1f) * (0.5f + 0.8f * Charge);
            Main.EntitySpriteDraw(tex, position - Main.screenPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effect, 0);


            if (blinkTimer > 0f)
            {
                //Glow over the thing
                Main.EntitySpriteDraw(tex, position - Main.screenPosition, frame, (Color.Crimson with { A = 0 }) * (float)Math.Pow(blinkTimer, 2f), Projectile.rotation, origin, Projectile.scale, effect, 0);

                //Make the eye have a glow
                Vector2 eyePosition = position + new Vector2(15f, 6f * Owner.direction * Owner.gravDir).RotatedBy(Projectile.rotation);
                Texture2D shineTex = AssetDirectory.CommonTextures.BloomStreak.Value;
                Texture2D bloomTex = AssetDirectory.CommonTextures.BloomCircle.Value;

                Vector2 shineScale = new Vector2(0.5f, 1.5f - (float)Math.Pow(blinkTimer, 0.5f) * 1f) * 0.8f;

                Color shineColor = Color.Crimson;
                shineColor.A = 0;
                float shineOpacity = (float)Math.Pow(blinkTimer, 0.6f);

                Main.EntitySpriteDraw(bloomTex, eyePosition - Main.screenPosition, null, shineColor * shineOpacity * 0.2f, MathHelper.PiOver2, bloomTex.Size() / 2f, shineScale * Projectile.scale * 0.3f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(shineTex, eyePosition - Main.screenPosition, null, shineColor * shineOpacity * 0.7f, MathHelper.PiOver2, shineTex.Size() / 2f, shineScale * Projectile.scale, SpriteEffects.None, 0);

                blinkTimer -= 1 / (60f * 0.5f);
            }

            return false;
        }

        public void DrawBlade(Color lightColor, Vector2 direction)
        {
            SpriteEffects flip = (Owner.direction * Owner.gravDir > 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Vector2 offsetRandom = Main.rand.NextVector2Circular(1f, 1f) * (0.5f + 0.8f * Charge);

            Vector2 positionBase = Owner.MountedCenter + Vector2.UnitY * Owner.gfxOffY + offsetRandom;
            Vector2 screwPosition = ScrewPosition + offsetRandom;

            //Draw the chain connecting the screw to the main body
            Texture2D chain = ChainTex.Value;
            Vector2 chainPosition = positionBase + direction * 20f;
            Vector2 chainScale = new Vector2((chainPosition - screwPosition).Length() / (float)chain.Width, 1f);
            Vector2 chainOrigin = new Vector2(chain.Width, chain.Height / 2);
            Main.EntitySpriteDraw(chain, screwPosition - Main.screenPosition, null, Projectile.GetAlpha(lightColor), direction.ToRotation(), chainOrigin, chainScale * Projectile.scale, flip, 0);

            //Draw the wheel of teeth
            float sawRotation = Timer * 0.1f * Owner.direction;

            //Draw afterimages
            if (afterImageTimer > 0f)
            {
                int afterimageCount = 5;
                for (int i = 0; i < afterimageCount; i++)
                {
                    float progress = (i / 5f);

                    float opacity = (1 - (i / (float)(afterimageCount))) * 0.8f * afterImageTimer;

                    DrawTeethWheel(Projectile.GetAlpha(lightColor) * opacity, screwPosition, sawRotation - 0.1f * i * Owner.direction * Owner.gravDir, flip);
                }

                afterImageTimer -= 1 / (60f * 1.2f);
            }

            DrawTeethWheel(Projectile.GetAlpha(lightColor), screwPosition, sawRotation, flip);

            DrawGore(lightColor, screwPosition, sawRotation);

            //Draw the screw above the wheel
            Texture2D screw = ScrewTex.Value;
            Main.EntitySpriteDraw(screw, screwPosition - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, screw.Size() / 2, Projectile.scale, 0, 0);
        }

        public void DrawTeethWheel(Color drawColor, Vector2 screwPosition, float wheelRotation, SpriteEffects flip)
        {
            Texture2D tooth = ToothTex.Value;
            Vector2 toothOrigin = new Vector2(tooth.Width / 2, tooth.Height);
            float toothSpacing = AttackRadius;

            Texture2D toothChain = ToothChainTex.Value;
            Vector2 toothChainScale = new Vector2(1f, toothSpacing / (float)toothChain.Height);
            Vector2 toothChainOrigin = new Vector2(toothChain.Width / 2, toothChain.Height);


            for (int i = 0; i < 8; i++)
            {
                float toothRotation = i / 8f * MathHelper.TwoPi + wheelRotation;
                Vector2 teethPosition = screwPosition + toothRotation.ToRotationVector2() * toothSpacing * Projectile.scale;

                Main.EntitySpriteDraw(toothChain, screwPosition - Main.screenPosition, null, drawColor, toothRotation + MathHelper.PiOver2, toothChainOrigin, toothChainScale * Projectile.scale, flip, 0);
                Main.EntitySpriteDraw(tooth, teethPosition - Main.screenPosition, null, drawColor, toothRotation + MathHelper.PiOver2, toothOrigin, Projectile.scale, flip, 0);
            }

        }

        public void DrawGore(Color lightColor, Vector2 screwPosition, float wheelRotation)
        {
            if (attachedGore is null)
                return;

            Texture2D goreTex = ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "EyetoothRipperGoreChunk").Value;
            foreach (EyetoothRipperGore gore in attachedGore)
            {
                Vector2 gorePosition = screwPosition + (gore.rotation + wheelRotation).ToRotationVector2() * gore.distance;
                Rectangle frame = goreTex.Frame(1, 6, 0, gore.variant, 0, -2);

                Main.EntitySpriteDraw(goreTex, gorePosition - Main.screenPosition, frame, lightColor, wheelRotation + gore.spin, frame.Size() / 2, Projectile.scale, 0, 0);

            }
        }
        #endregion
    }

    public class EyetoothMincedMeat : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;
        public Player Owner => Main.player[Projectile.owner];

        public List<EyetoothRipperGore> attachedGore;

        public ref float Charge => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Minced Meat");
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = 3;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.rotation = Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver2);
        }

        public override void AI()
        {
            Projectile.rotation += Projectile.velocity.X.NonZeroSign() * Projectile.velocity.Length() * 0.01f;
            Projectile.velocity.Y += 0.2f;


            attachedGore = attachedGore ?? new List<EyetoothRipperGore>();
            if (attachedGore.Count < 6 + Charge * 4)
            {
                attachedGore.Add(new EyetoothRipperGore(14f));
            }

            if (Main.rand.NextBool(3))
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), DustID.Blood, Main.rand.NextVector2Circular(3f, 3f), Scale: Main.rand.NextFloat(1.3f, 2f));
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath23 with { Volume = 0.8f }, Projectile.Center);
            FuckingExplode();
        }

        public void FuckingExplode()
        {
            Projectile.velocity = Projectile.velocity.SafeNormalize(-Vector2.UnitY);

            for (int k = 0; k <= 30; k++)
            {
                Vector2 dustPosition = Projectile.Bottom + Main.rand.NextFloatDirection() * Projectile.width * 0.8f * Vector2.UnitX - Vector2.UnitY * Main.rand.NextFloat(0f, 10f);
                Vector2 dustVelocity = (Projectile.Bottom + Vector2.UnitY * 30f).DirectionTo(dustPosition) * Main.rand.NextFloat(0.4f, 4f);

                Dust.NewDustPerfect(dustPosition, 5, dustVelocity, 0, default, Main.rand.NextFloat(0.8f, 1.1f));
            }

            if (attachedGore != null)
            {
                foreach (EyetoothRipperGore gore in attachedGore)
                {
                    Vector2 gorePosition = Projectile.Center + Main.rand.NextVector2Circular(4f, 4f);
                    Vector2 goreVelocity = Main.rand.NextVector2Circular(3f, 3f) - Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 1.5f) * Main.rand.NextFloat(0.5f, 1.5f);
                    goreVelocity -= Vector2.UnitY * 2f;

                    Gore g = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), gorePosition, goreVelocity, Mod.Find<ModGore>("MincedMeatGore" + (gore.variant + 1).ToString()).Type, Main.rand.NextFloat(0.7f, 1f));
                    g.alpha = Main.rand.Next(100);
                }

            }
        }



        public override bool PreDraw(ref Color lightColor)
        {
            if (attachedGore is null)
                return false;

            if (Charge >= 1)
            {
                SpriteEffects flip = (Owner.direction * Owner.gravDir > 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Texture2D tooth = EyetoothRipperProjectile.ToothTex.Value;
                Vector2 toothOrigin = new Vector2(tooth.Width / 2, tooth.Height);
                float toothSpacing = 13f;
                Texture2D toothChain = EyetoothRipperProjectile.ToothChainTex.Value;
                Vector2 toothChainScale = new Vector2(1f, toothSpacing / (float)toothChain.Height);
                Vector2 toothChainOrigin = new Vector2(toothChain.Width / 2, toothChain.Height);

                for (int i = 0; i < 6; i++)
                {
                    float toothRotation = i / 6f * MathHelper.TwoPi + Projectile.rotation;
                    Vector2 teethPosition = Projectile.Center + toothRotation.ToRotationVector2() * toothSpacing * Projectile.scale;

                    Main.EntitySpriteDraw(toothChain, Projectile.Center - Main.screenPosition, null, lightColor, toothRotation + MathHelper.PiOver2, toothChainOrigin, toothChainScale * Projectile.scale, flip, 0);
                    Main.EntitySpriteDraw(tooth, teethPosition - Main.screenPosition, null, lightColor, toothRotation + MathHelper.PiOver2, toothOrigin, Projectile.scale, flip, 0);
                }
            }

            Texture2D goreTex = ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "EyetoothRipperGoreChunk").Value;
            foreach (EyetoothRipperGore gore in attachedGore)
            {
                Vector2 gorePosition = Projectile.Center + (gore.rotation + Projectile.rotation).ToRotationVector2() * gore.distance;
                Rectangle frame = goreTex.Frame(1, 6, 0, gore.variant, 0, -2);

                Main.EntitySpriteDraw(goreTex, gorePosition - Main.screenPosition, frame, lightColor, Projectile.rotation + gore.spin, frame.Size() / 2, Projectile.scale, 0, 0);

            }

            return false;
        }


    }
}
