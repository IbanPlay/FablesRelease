using CalamityFables.Content.Boss.DesertWormBoss;
using CalamityFables.Content.Buffs;
using CalamityFables.Content.Dusts;
using CalamityFables.Particles;
using ReLogic.Utilities;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Items.DesertScourgeDrops
{
    [ReplacingCalamity("Barinade")]
    public class AmpstringBow : ModItem
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;

        public static int STUCK_NEEDLE_LIFETIME = 1200;
        public static int STUCK_NEEDLE_TETHER_DISTANCE = 1000;
        public static int STUCK_NEEDLE_HIT_COOLDOWN = 10;
        public static float NEEDLE_DAMAGE_MULTIPLIER = 2.5f;
        public static int ELECTRO_FIELD_DAMAGE = 5;
        public static float OVERAMPED_DAMAGE_BOOST = 0.25f;
        public static int ARROW_MIN_PENETRATE = 2;
        public static float ARROW_MULTIHIT_PENALTY = 0.1f;

        public static LocalizedText ElectricFieldDamageText;

        public override void Load() => AmpstringArrowHandling.Load();

        public override void Unload() => AmpstringArrowHandling.Unload();

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ampstring Bow");
            Tooltip.SetDefault("Right clicking launches conductive needles\n" +
                               "Needles arc electricity between each other\n" +
                               "Enemies caught in the electric field are more vulnerable to arrows");
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
            ElectricFieldDamageText = Mod.GetLocalization("Extras.ItemTooltipExtras.AmpstringBowElectricFieldDamage");
        }

        public override void SetDefaults()
        {
            Item.damage = 22;
            Item.width = 36;
            Item.height = 48;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.DamageType = DamageClass.Ranged;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2f;
            Item.value = Item.sellPrice(silver: 40);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item5;
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.autoReuse = true;
            Item.shootSpeed = 15f;
            Item.useAmmo = AmmoID.Arrow;
            Item.ChangePlayerDirectionOnShoot = false;
        }

        public override bool AltFunctionUse(Player player) => true;
        public override void HoldItem(Player player) => player.SyncMousePosition();
        public override bool CanConsumeAmmo(Item ammo, Player player) => player.altFunctionUse != 2;

        public override float UseSpeedMultiplier(Player player)
        {
            if (player.altFunctionUse == 2)
                return 0.5f;
            return 1f;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                type = ModContent.ProjectileType<AmpstringNeedle>();
                velocity *= 1.2f;
                damage = (int)(damage * NEEDLE_DAMAGE_MULTIPLIER);
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                int needleType = ModContent.ProjectileType<AmpstringNeedle>();
                int oldestNeedleIndex = 0;
                int oldestNeedleAge = -1;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.owner != player.whoAmI || !p.active || p.type != needleType)
                        continue;

                    //Increase the index of all the projectiles and make them forget their pairing
                    p.ai[0]++;
                    p.ai[1] = -1;

                    //Refresh the lifetime of the other nails
                    if (p.tileCollide && p.timeLeft < STUCK_NEEDLE_LIFETIME - 60)
                        p.timeLeft = STUCK_NEEDLE_LIFETIME - 60;

                    p.netUpdate = true;

                    //Get the oldest needle
                    if (p.ai[0] > oldestNeedleAge)
                    {
                        oldestNeedleIndex = i;
                        oldestNeedleAge = (int)p.ai[0];
                    }
                }

                //Kill the oldest needle if we already have 2
                if (player.ownedProjectileCounts[needleType] >= 2)
                    Main.projectile[oldestNeedleIndex].Kill();
            }

            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }

        public override void UseAnimation(Player player)
        {
            Item.UseSound = DesertScourge.ElectroJumpSound with { Volume = 0.4f, MaxInstances = 0, Identifier = "AmpstringDefaultShoot", Pitch = 0.5f };
            if (player.altFunctionUse == 2)
                Item.UseSound = DesertScourge.PlatformElectrificationSound with { Volume = 0.7f, MaxInstances = 0, Identifier = "AmpstringShoot", Pitch = 0.4f };
        }

        public override void UseItemFrame(Player player)
        {
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);
            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - player.MouseWorld()).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.4f && player.altFunctionUse == 2)
                rotation += -0.45f * (float)Math.Pow((0.4f - animProgress) / 0.4f, 2) * player.direction;

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);

            if (player.altFunctionUse == 2 && player.itemTime < player.itemTimeMax && Main.rand.NextBool(5))
            {
                Vector2 pointingVector = (rotation + MathHelper.PiOver2).ToRotationVector2();
                Vector2 bowTipPosition = player.MountedCenter + pointingVector * 25f + Main.rand.NextVector2Circular(3f, 3f);

                Dust sparks = Dust.NewDustPerfect(bowTipPosition, DustID.Electric, Vector2.Zero, 180);
                sparks.velocity = pointingVector.RotatedByRandom(MathHelper.PiOver4 * 0.4f) * Main.rand.NextFloat(2f, 7f) * (float)Math.Pow(1 - animProgress, 2.5f);
                sparks.noGravity = true;
                sparks.scale = Main.rand.NextFloat(0.1f + 2.4f * (float)Math.Pow(1 - animProgress, 1.5f));
            }
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            float animProgress = player.itemTime / (float)player.itemTimeMax;
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 7f;
            Vector2 itemSize = new Vector2(38, 46);
            Vector2 itemOrigin = new Vector2(-10, 4);

            CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int damageIndex = tooltips.FindIndex(tooltip => tooltip.Name == "Damage" && tooltip.Mod == "Terraria");
            TooltipLine fieldDot = new TooltipLine(Mod, "CalamityFables:FieldDamage", ElectricFieldDamageText.Format(ELECTRO_FIELD_DAMAGE));
            fieldDot.OverrideColor = Color.Lerp(Color.White, Color.Turquoise, (float)Math.Pow(0.5 + 0.5 * Math.Sin(Main.GlobalTimeWrappedHourly * 2f), 4f));
            tooltips.Insert(damageIndex + 1, fieldDot);
        }
    }

    public class AmpstringArrowHandling
    {
        internal static Asset<Texture2D> AmpstringArrow;

        public static void Load()
        {
            FablesProjectile.OnSpawnEvent += ConvertSpawnedProjectiles;
            FablesProjectile.AIEvent += AI;
            FablesProjectile.ModifyHitNPCEvent += ModifyHitNPC;
            FablesProjectile.OnHitNPCEvent += OnHitNPC;
            FablesProjectile.OnKillEvent += OnKill;
            FablesProjectile.PreDrawEvent += PreDraw;
            FablesProjectile.SetVelocityCapEvent += SetVelocityCap;

            FablesProjectile.RegisterSyncedData(typeof(AmpstringArrowData), SendInbuedDartProjectileData, RecieveInbuedDartProjectileData);
        }

        public static void Unload()
        {
            FablesProjectile.OnSpawnEvent -= ConvertSpawnedProjectiles;
            FablesProjectile.AIEvent -= AI;
            FablesProjectile.ModifyHitNPCEvent -= ModifyHitNPC;
            FablesProjectile.OnHitNPCEvent -= OnHitNPC;
            FablesProjectile.OnKillEvent -= OnKill;
            FablesProjectile.PreDrawEvent -= PreDraw;
        }

        #region Global Data
        public class AmpstringArrowData : CustomGlobalData
        {
            public int MinPenetrate;
            public float Charge = 1f;
            public float BaseSpeed;

            public List<Vector2> Cache;
            public PrimitiveTrail Trail;
            public bool UpdatedValues = false;

            public AmpstringArrowData(int extraPenetrate, float baseSpeed)
            {
                MinPenetrate = extraPenetrate;
                BaseSpeed = baseSpeed;
            }
        }

        public static void SendInbuedDartProjectileData(CustomGlobalData data, Projectile proj, BitWriter bitWriter, BinaryWriter writer)
        {
            if (data is not AmpstringArrowData arrowData)
                return;

            writer.Write(arrowData.MinPenetrate);
            writer.Write(arrowData.Charge);
            writer.Write(arrowData.BaseSpeed);
        }

        public static void RecieveInbuedDartProjectileData(CustomGlobalData data, Projectile proj, BitReader bitReader, BinaryReader reader)
        {
            if (data is not AmpstringArrowData arrowData)
                return;

            arrowData.MinPenetrate = reader.ReadInt32();
            arrowData.Charge = reader.ReadSingle();
            arrowData.BaseSpeed = reader.ReadSingle();
        }

        #endregion

        public static void ConvertSpawnedProjectiles(Projectile projectile, IEntitySource source)
        {
            // Apply modification data to projectiles shot by the ampstring bow
            if (projectile.ModProjectile is not AmpstringNeedle && source is EntitySource_ItemUse_WithAmmo { Item: Item item } && item != null && item.ModItem is AmpstringBow)
                projectile.SetProjectileData(new AmpstringArrowData(AmpstringBow.ARROW_MIN_PENETRATE, projectile.velocity.Length()) { needsSyncing_forProjectiles = true });
        }

        private static void AI(Projectile projectile)
        {
            if (!projectile.GetProjectileData<AmpstringArrowData>(out var data))
                return;

            if (!data.UpdatedValues)
            {
                if (projectile.penetrate > 0)
                {
                    // Apply local Iframes to arrows that hit once
                    if (projectile.penetrate == 1)
                    {
                        projectile.usesLocalNPCImmunity = true;
                        projectile.localNPCHitCooldown = 10;
                    }
                    projectile.penetrate = Math.Max(projectile.penetrate, data.MinPenetrate);             
                }

                data.UpdatedValues = true;
            }

            ref var charge = ref data.Charge;

            // Speedy on spawn
            if (charge > 0.45f)
            {
                float chargeLerp = Utils.GetLerpValue(0.5f, 1f, charge, true);
                projectile.velocity.Normalize();
                projectile.velocity *= data.BaseSpeed * (1f + MathF.Pow(chargeLerp, 0.4f));
            }

            // Charged dust
            if (charge > 0f && Main.rand.NextFloat() < charge * 0.8f && Main.rand.NextBool(3))
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 dustPos = projectile.Center + Main.rand.NextVector2Circular(4f, 4f);
                    int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();

                    Dust dust = Dust.NewDustPerfect(dustPos, dusType, projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * 3f + Main.rand.NextVector2Circular(2f, 2f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.4f, 1f) * (0.7f + 0.3f * charge);
                }
            }

            else if (Main.rand.NextBool(6))
            {
                Vector2 dustPos = projectile.Center + Main.rand.NextVector2Circular(4f, 4f);
                int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();

                Dust dust = Dust.NewDustPerfect(dustPos, dusType, projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * 3f + Main.rand.NextVector2Circular(2f, 2f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.3f, 0.5f);
            }

            ManageTrail(projectile, data);
            charge -= 1 / 20f;
        }

        private static bool SetVelocityCap(Projectile projectile) => !projectile.GetProjectileData<AmpstringArrowData>(out var data) || data.Charge <= 0.45f;

        private static void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (projectile.GetProjectileData<AmpstringArrowData>(out _) && target.HasBuff<Overamped>())
                modifiers.ScalingBonusDamage += AmpstringBow.OVERAMPED_DAMAGE_BOOST;
        }

        private static void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!projectile.GetProjectileData<AmpstringArrowData>(out _))
                return;

            projectile.damage = (int)(projectile.damage * (1f - AmpstringBow.ARROW_MULTIHIT_PENALTY));
            Particle flash = new WooshingStreakParticle(projectile.Center + Main.rand.NextVector2Circular(8f, 8f) - projectile.velocity, projectile.rotation - MathHelper.Pi, 18f, Color.White with { A = 0 }, Color.DodgerBlue with { A = 100 } * 0.4f, (Color.Aqua * 0.2f) with { A = 0 }, 0.45f, 10);
            ParticleHandler.SpawnParticle(flash);
        }

        private static void OnKill(Projectile projectile, int timeLeft)
        {
            if (!projectile.GetProjectileData<AmpstringArrowData>(out var data))
                return;

            SoundEngine.PlaySound(DesertScourge.PlatformElectrificationSound with { Volume = 0.2f, Pitch = 0.1f }, projectile.Center);

            if (projectile.penetrate != 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 dustPosition = projectile.Center + Main.rand.NextVector2Circular(4f, 4f) + projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(-5f, 5f);
                    Vector2 dustVelocity = projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * 3f + Main.rand.NextVector2Circular(2f, 2f);
                    int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();

                    Dust dust = Dust.NewDustPerfect(dustPosition, dusType, dustVelocity * 1.4f, Scale: Main.rand.NextFloat(0.3f, 0.9f));
                    dust.noGravity = true;
                }

                Particle flash = new HitStreakParticle(projectile.Center, projectile.rotation - MathHelper.Pi, Color.White with { A = 100 }, Color.RoyalBlue with { A = 100 } * 0.4f, (Color.Gold * 0.2f) with { A = 0 }, 0.45f, 10);
                ParticleHandler.SpawnParticle(flash);
            }
        }

        private static void ManageTrail(Projectile projectile, AmpstringArrowData data)
        {
            if (Main.dedServ)
                return;

            Vector2 position = projectile.Center + projectile.velocity;
            ref var cache = ref data.Cache;
            ref var trail = ref data.Trail;

            cache ??= [];

            cache.Add(position);
            while (cache.Count > 20)
                cache.RemoveAt(0);

            trail ??= new PrimitiveTrail(30, WidthFunction, ColorFunction);
            trail.SetPositionsSmart(cache, position, RigidPointRetreivalFunction);
            trail.NextPosition = position;

            float WidthFunction(float progress) => 10f * MathF.Pow(progress, 0.3f) * MathF.Pow(data.Charge, 0.2f);

            Color ColorFunction(float progress)
            {
                Color color = Color.Lerp(Color.DodgerBlue, Color.Aqua, MathF.Pow(progress, 4f));
                color.A = 0;
                return color * MathF.Pow(progress, 1.2f) * MathF.Pow(data.Charge, 0.7f);
            }
        }

        private static bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (!projectile.GetProjectileData<AmpstringArrowData>(out var data))
                return true;

            AmpstringArrow ??= ModContent.Request<Texture2D>(AssetDirectory.DesertScourgeDrops + "AmpstringArrow");
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            float rotation = projectile.rotation - MathHelper.Pi;

            bool charged = data.Charge > 0;
            if (charged)
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

                data.Trail?.Render(effect, -Main.screenPosition);
            }

            Main.EntitySpriteDraw(AmpstringArrow.Value, drawPosition, null, lightColor, rotation, AmpstringArrow.Size() / 2, projectile.scale, 0, 0);

            if (charged)
            {
                Color overlayColor = Color.SkyBlue with { A = 0 } * data.Charge;
                Main.EntitySpriteDraw(AmpstringArrow.Value, drawPosition, null, overlayColor, rotation, AmpstringArrow.Size() / 2, projectile.scale, 0, 0);
                Main.EntitySpriteDraw(AmpstringArrow.Value, drawPosition, null, overlayColor, rotation, AmpstringArrow.Size() / 2, projectile.scale, 0, 0);
            }

            return false;
        }
    }

    public class AmpstringNeedle : ModProjectile
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;

        internal PrimitiveTrail TrailDrawer;
        internal List<Vector2> cache;

        public Vector2 NailTip => Projectile.Center + (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * 30f;
        public Vector2 CordCenter => Projectile.Center - (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * 20f;

        public SlotId electrofieldLoopSlot;

        public float TrailOpacity => Projectile.tileCollide ? 1 : (Projectile.timeLeft - (AmpstringBow.STUCK_NEEDLE_LIFETIME - 30)) / 30f;

        public bool IsTheBrain => Projectile.ai[0] == 0;
        public bool IsPaired => OtherArrowIndex != -1;

        public int OtherArrowIndex {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public bool OtherArrowValid => Projectile.Distance(Main.projectile[OtherArrowIndex].Center) < AmpstringBow.STUCK_NEEDLE_TETHER_DISTANCE;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ampstring Needle");
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 16000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.friendly = true;
            Projectile.arrow = true;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 600;
            Projectile.extraUpdates = 8;
            Projectile.hide = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = AmpstringBow.STUCK_NEEDLE_HIT_COOLDOWN;
        }

        public override bool? CanDamage()
        {
            if (Projectile.tileCollide || (IsTheBrain && IsPaired)) //Can only deal damage while flying or paired
                return null;

            return false;
        }

        public override void AI()
        {
            if (Projectile.timeLeft == 600 && Projectile.tileCollide)
                OtherArrowIndex = -1;

            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (Projectile.tileCollide)
            {
                if (Main.rand.NextBool(2))
                {
                    Dust sparks = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), DustID.UltraBrightTorch, Vector2.Zero, 180);
                    sparks.position += Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-15f, 15f);
                    sparks.velocity = -Projectile.velocity.SafeNormalize(Vector2.Zero) * 5f;
                    sparks.noGravity = true;
                    sparks.scale = Main.rand.NextFloat(0.5f, 1f);
                }
            }
            else
            {
                //If stuck inside a tile, and the tile is gone, break.
                if (!Collision.SolidCollision(Projectile.position - Vector2.One * 16f, Projectile.width + 32, Projectile.height + 32))
                {
                    Projectile.Kill();
                    return;
                }

                Lighting.AddLight(Projectile.Center, Color.RoyalBlue.R / 255f, Color.RoyalBlue.G / 255f, Color.RoyalBlue.B / 255f);

                if (IsPaired && IsTheBrain)
                {
                    if (Main.projectile[OtherArrowIndex].type != Type || !Main.projectile[OtherArrowIndex].active || Main.projectile[OtherArrowIndex].owner != Projectile.owner)
                        OtherArrowIndex = -1;

                    else if (OtherArrowValid)
                    {
                        Vector2 otherProjectilePosition = Main.projectile[OtherArrowIndex].Center - (Main.projectile[OtherArrowIndex].rotation + MathHelper.PiOver2).ToRotationVector2() * 20f;
                        Vector2 zapAnchor1 = CordCenter + (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(-10f, 30f);
                        Vector2 zapAnchor2 = otherProjectilePosition + (Main.projectile[OtherArrowIndex].rotation + MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(-10f, 30f);

                        DelegateMethods.v3_1 = Color.RoyalBlue.ToVector3() * 0.5f;
                        Utils.PlotTileLine(CordCenter, otherProjectilePosition, 8f, DelegateMethods.CastLightOpen);

                        if (Projectile.timeLeft % 5 == 0)
                        {
                            Particle zap = new ElectricArcPrim(zapAnchor1, zapAnchor2, Vector2.Zero, 2f, true);
                            ParticleHandler.SpawnParticle(zap);
                        }


                        if (!SoundEngine.TryGetActiveSound(electrofieldLoopSlot, out var soundLoop))
                            electrofieldLoopSlot = SoundEngine.PlaySound(DesertScourge.ElectroLoopSound with { Volume = 0.3f, Identifier = "Ampstring Field" }, Vector2.Lerp(zapAnchor1, zapAnchor2, 0.5f));
                        else
                            soundLoop.Position = Vector2.Lerp(zapAnchor1, zapAnchor2, 0.5f);


                        SoundHandler.TrackSoundWithFade(electrofieldLoopSlot);
                    }
                }
            }

            ManageCache();
            ManageTrail();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Projectile.tileCollide && IsPaired && IsTheBrain && OtherArrowValid)
            {
                float theZaza = 0f;
                Vector2 otherProjectilePosition = Main.projectile[OtherArrowIndex].Center - (Main.projectile[OtherArrowIndex].rotation + MathHelper.PiOver2).ToRotationVector2() * 20f;
                return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), CordCenter, otherProjectilePosition, 20f, ref theZaza);
            }

            return base.Colliding(projHitbox, targetHitbox);
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (!Projectile.tileCollide && target.GetIFramesProvider(out int iframesProvider))
            {
                //unable to hit worm segments if the parent is already immune
                if (Projectile.localNPCImmunity[iframesProvider] != 0)
                    return false;
            }

            return base.CanHitNPC(target);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!Projectile.tileCollide)
            {
                int damage = AmpstringBow.ELECTRO_FIELD_DAMAGE;
                //Using flat damage is kinda hard damn!
                modifiers.Knockback *= 0;
                modifiers.ScalingArmorPenetration += 1;
                NPC.HitModifiers noRefModifiers = modifiers;
                modifiers.ModifyHitInfo += (ref NPC.HitInfo info) => info = info.SetUnscaledDamage(damage, noRefModifiers);
            }
            //Falloff damage for piercing
            else
            {
                int targetBaseDamage = (int)MathHelper.Lerp(Projectile.damage, 2f, Math.Min(Projectile.numHits / 5f, 1));
                modifiers.SourceDamage *= targetBaseDamage / (float)Projectile.damage;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Projectile.tileCollide && target.GetIFramesProvider(out int iframesProvider))
            {
                Main.npc[iframesProvider].immune[Projectile.owner] = 0;
                Projectile.localNPCImmunity[iframesProvider] = Projectile.localNPCHitCooldown / 2;
            }

            target.AddBuff(ModContent.BuffType<Overamped>(), 60);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            ImpactEffects(oldVelocity.SafeNormalize(Vector2.Zero));
            Projectile.position += Projectile.velocity + Projectile.oldVelocity.SafeNormalize(Vector2.Zero) * 4f;
            Projectile.velocity = Vector2.Zero;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 0;
            Projectile.timeLeft = AmpstringBow.STUCK_NEEDLE_LIFETIME;
            Projectile.tileCollide = false;
            Projectile.ArmorPenetration += 6;

            //Find another arrow
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (i == Projectile.whoAmI || p.owner != Projectile.owner || !p.active || p.type != Type)
                    continue;

                p.ai[1] = Projectile.whoAmI;
                OtherArrowIndex = i;
                break;
            }

            return false;
        }

        public void ImpactEffects(Vector2 impactNormal)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);

            if (Main.myPlayer != Projectile.owner || Projectile.Distance(Main.LocalPlayer.Center) < 1000f)
                SoundEngine.PlaySound(DesertScourge.ElectroJumpSound, Projectile.position);
            else
                SoundEngine.PlaySound(DesertScourge.ElectroJumpSound with { Volume = 0.2f });

            for (int i = 0; i < 5; i++)
            {
                Vector2 sparkVelocity = (impactNormal.ToRotation().AngleLerp(-MathHelper.PiOver2, 0.5f)).ToRotationVector2();
                sparkVelocity = sparkVelocity.RotatedByRandom(MathHelper.PiOver4);
                sparkVelocity *= Main.rand.NextFloat(6f, 16f);
                Particle etincelle = new ElectroFireEtincelle(NailTip, sparkVelocity);
                ParticleHandler.SpawnParticle(etincelle);
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 smokePosition = Projectile.Center;
                Vector2 smokeVelocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 2f) + Main.rand.NextVector2Circular(2.6f, 2.6f) * 3f;
                Color smokeFireColor = Main.rand.NextBool(3) ? Color.PaleGreen : Color.OrangeRed;
                Particle sparkZap = new ExplosionSmoke(smokePosition, smokeVelocity, smokeFireColor, Color.DarkGray * 0.2f, Color.Black * 0.4f, Main.rand.NextFloat(1f, 2f), 0.03f);
                ParticleHandler.SpawnParticle(sparkZap);
            }

            Particle zap = new ElectricArcPrim(Projectile.Center, cache[0], Vector2.Zero, 4f);
            ParticleHandler.SpawnParticle(zap);

            Particle flasj = new HitStreakParticle(NailTip, Projectile.rotation, Color.White with { A = 100 }, Color.RoyalBlue with { A = 100 } * 0.4f, (Color.Gold * 0.3f) with { A = 0 }, 1f);
            ParticleHandler.SpawnParticle(flasj);

            if (Main.myPlayer == Projectile.owner)
                CameraManager.Quake += 10;
        }

        public override void OnKill(int timeLeft)
        {
            SoundStyle breakSound = timeLeft == 0 ? DesertScourge.ElectricFizzleSound : SoundID.DD2_SkeletonHurt;
            SoundEngine.PlaySound(breakSound, Projectile.Center);
        }

        public void ManageCache()
        {
            //Initialize the cache
            if (cache == null)
            {
                cache = new List<Vector2>();

                for (int i = 0; i < 45; i++)
                {
                    cache.Add(Projectile.Center + Projectile.velocity);
                }
            }

            if (Projectile.tileCollide)
            {
                cache.Add(Projectile.Center + Projectile.velocity);

                while (cache.Count > 500)
                {
                    cache.RemoveAt(0);
                }
            }
        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction);
            TrailDrawer.SetPositionsSmart(cache, Projectile.Center + Projectile.velocity, RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.Center + Projectile.velocity;
        }

        internal float WidthFunction(float completionRatio)
        {
            return (4f * (float)Math.Pow(completionRatio, 0.3f)) * TrailOpacity;
        }

        internal Color ColorFunction(float completionRatio)
        {
            Color color = Color.RoyalBlue * (float)Math.Pow(completionRatio, 1.2f) * TrailOpacity;
            color.A = 200;
            return color;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TrailOpacity > 0)
            {
                Effect effect = AssetDirectory.PrimShaders.GlowingCoreWithOverlaidNoise;
                effect.Parameters["scroll"].SetValue(-Main.GlobalTimeWrappedHourly * 0.7f * 2.5f);
                effect.Parameters["overlayScroll"].SetValue(-Main.GlobalTimeWrappedHourly * 0.7f);

                
                effect.Parameters["repeats"].SetValue(2f);
                effect.Parameters["overlayRepeats"].SetValue(2f * 0.2f);
                effect.Parameters["coreShrink"].SetValue(0.9f);
                effect.Parameters["coreOpacity"].SetValue(0.1f);
                effect.Parameters["overlayVerticalScale"].SetValue(0.5f);
                effect.Parameters["overlayMaxOpacityOverlap"].SetValue(2f);

                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
                effect.Parameters["overlayNoise"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "FireTrail").Value);
                TrailDrawer?.Render(effect, -Main.screenPosition);
            }

            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2, Projectile.scale, 0, 0);

            //Draw an extra glow
            if (TrailOpacity > 0f)
            {
                float glowScale = Projectile.tileCollide ? 1f : (1f + 0.3f * TrailOpacity);
                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, (Color.White with { A = 0 }) * TrailOpacity, Projectile.rotation, tex.Size() / 2, Projectile.scale * glowScale, 0, 0);
            }

            //Make the string have a lil glow effect when stuck in the ground
            if (!Projectile.tileCollide)
            {
                Texture2D stringGlowTex = ModContent.Request<Texture2D>(Texture + "StringGlow").Value;
                float opacity = 1 - TrailOpacity;
                Vector2 wideScale = new Vector2(1.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f), 1f);

                Main.EntitySpriteDraw(stringGlowTex, Projectile.Center - Main.screenPosition, null, (Color.RoyalBlue with { A = 0 }) * opacity * 0.5f, Projectile.rotation, stringGlowTex.Size() / 2, wideScale * Projectile.scale, 0, 0);
            }

            return false;
        }
    }
}
