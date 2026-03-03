using ReLogic.Utilities;
using Terraria.Localization;
using static CalamityFables.Content.Items.EarlyGameMisc.FanaticalMask;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class FanaticalMask : ModItem
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        internal static LocalizedText TendrilDamageText;

        public static readonly SoundStyle ScreamSound = new SoundStyle("CalamityFables/Sounds/HorrorScream", 3);
        public static readonly SoundStyle NeckBreakSound = new SoundStyle("CalamityFables/Sounds/NeckBreak");
        public static readonly SoundStyle FullChargeSound = new SoundStyle("CalamityFables/Sounds/FanaticalMaskFullCharge");
        public static readonly SoundStyle FullChargeLoopSound = new SoundStyle("CalamityFables/Sounds/FanaticalMaskFullChargeLoop") { IsLooped = true, PlayOnlyIfFocused = true };

        public static int HITS_TO_FULL_CHARGE = 15;
        public static float TENDRIL_DAMAGE_MULT = 6f;
        public static float TENDRIL_KNOCKBACK_MULT = 5f;

        public static int GLOBULE_HOMING_RANGE = 200;
        public static float GLOBULE_HOMING_STRENGTH = 0.05f;

        public override void Load() => FablesNPC.ModifyNPCLootEvent += DropFromBloodMoons;

        public override void SetStaticDefaults() => TendrilDamageText = Mod.GetLocalization("Extras.ItemTooltipExtras.FanaticalMaskTendrilDamage");

        public override void SetDefaults()
        {
            Item.damage = 11;
            Item.DamageType = DamageClass.Magic;
            Item.width = 20;
            Item.height = 20;
            Item.useTime = 4;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 1.1f;
            Item.value = Item.sellPrice(silver: 75);
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<FanaticalMaskProjectile>();
            Item.autoReuse = true;
            Item.channel = true;
            Item.shootSpeed = 10f;
            Item.mana = 5;

            Item.noUseGraphic = true;
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override void HoldItem(Player player) => player.SyncMousePosition();

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int damageIndex = tooltips.FindIndex(tooltip => tooltip.Name == "Damage" && tooltip.Mod == "Terraria");

            int tendrilDamage = (int)(Main.LocalPlayer.GetWeaponDamage(Item, true) * TENDRIL_DAMAGE_MULT);

            TooltipLine horrorDamageLine = new TooltipLine(Mod, "CalamityFables:TendrilDamage", TendrilDamageText.Format(tendrilDamage));
            horrorDamageLine.OverrideColor = Color.Lerp(Color.White, new Color(196, 43, 15), MathF.Pow(0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2f), 4f));

            tooltips.Insert(damageIndex + 1, horrorDamageLine);
        }

        private void DropFromBloodMoons(NPC npc, NPCLoot npcloot)
        {
            if (npc.type == NPCID.BloodZombie || npc.type == NPCID.Drippler)
                npcloot.Add(Type, new Fraction(1, 100));
        }
    }

    public class FanaticalMaskProjectile : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "FanaticalMask_Mask";

        public ref float DeployedFrames => ref Projectile.ai[0];
        public ref float BloodCharge => ref Projectile.ai[1];

        private int AnimationTime = 0;
        private int BurstTime = 0;
        private int ShotsInBurst = 0;
        private float VignetteStrength = 0f;
        private SlotId FullChargeLoopSlot;

        public Player Owner => Main.player[Projectile.owner];
        public float PutOnMaskProgress => FablesUtils.SineInEasing(1f - Utils.GetLerpValue(0, 25, DeployedFrames, true));

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Player player = Owner;
            bool localPlayer = Main.LocalPlayer == player;

            // Kill the projectile if the player dies or gets crowd controlled
            if (!player.active || player.dead || player.noItems || player.CCed || !player.channel || (localPlayer && Main.mapFullscreen))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = new Vector2(6f * player.direction, -9f * player.gravDir) + player.MountedCenter;
            ManageDummyItem(player);

            // Shoot and manage vignette on local client
            if (localPlayer)
            {
                if (DeployedFrames > 20)
                    ManageShooting(player);

                // Manage vignette
                ScreenDesaturation.desaturationOverride = BloodCharge * 0.24f;
                VignetteFadeEffects.vignetteOpacityOverride = (BloodCharge + FablesUtils.SineOutEasing(VignetteStrength)) * 0.3f;

                VignetteStrength *= 0.965f;

                // Loop sound
                if (BloodCharge >= 1)
                {
                    if (!SoundEngine.TryGetActiveSound(FullChargeLoopSlot, out var loopSound))
                        FullChargeLoopSlot = SoundEngine.PlaySound(FullChargeLoopSound);

                    if (FullChargeLoopSlot != SlotId.Invalid)
                        SoundHandler.TrackSound(FullChargeLoopSlot);
                }
            }

            DeployedFrames++;
        }

        public override void OnKill(int timeLeft)
        {
            if (BloodCharge < 1)
                return;

            Player player = Owner;

            SoundEngine.PlaySound(ScreamSound, Projectile.Center);
            if (Math.Abs(Vector2.Dot(Vector2.UnitY, Projectile.DirectionTo(player.MouseWorld()))) > 0.5f)
                SoundEngine.PlaySound(NeckBreakSound, Projectile.Center);

            // Spawn gores
            if (!Main.dedServ)
            {
                int[] goreTypes = [GoreID.BloodZombieChunk, GoreID.BloodZombieChunk2, GoreID.DripplerChunk, GoreID.DripplerChunk2, GoreID.DripplerChunk3, GoreID.BloodSquid4, GoreID.BloodSquid3, GoreID.BloodSquid2];

                for (int i = 0; i < 3; i++)
                {
                    Vector2 gorePosition = Projectile.Center;
                    Vector2 goreVelocity = Projectile.DirectionTo(player.MouseWorld()).RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(1f, 5f);
                    float goreScale = Main.rand.NextFloat(0.6f, 0.9f);

                    Gore.NewGore(Projectile.GetSource_FromThis(), gorePosition, goreVelocity, Main.rand.Next(goreTypes), goreScale);
                }
            }

            // Spawn tendril
            if (Main.LocalPlayer == player)
            {
                Vector2 projectilePosition = Projectile.Center;
                Vector2 projectileDirection = Projectile.Center.DirectionTo(player.MouseWorld());
                int type = ModContent.ProjectileType<FanaticalMaskTendril>();
                int damage = (int)(TENDRIL_DAMAGE_MULT * Projectile.damage);

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), projectilePosition, projectileDirection, type, damage, Projectile.knockBack * TENDRIL_KNOCKBACK_MULT, Projectile.owner);

                ScreenDesaturation.desaturationOverride = 0f;
                FablesUtils.GoScary();
            }
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;

        private void ManageDummyItem(Player player)
        {
            Projectile.timeLeft = 2;
            player.heldProj = Projectile.whoAmI;
            player.direction = (player.MouseWorld() - player.Center).X.NonZeroSign();
            //if (player.mount.Active)
            //    player.mount.Dismount(player);

            // Only set item time if the user has mana. Allows it to recharge if the user runs out of mana
            if (player.CheckMana(player.ActiveItem()))
                player.SetDummyItemTime(2);

            float armRotation = (5.9f * MathHelper.PiOver4 + PutOnMaskProgress * MathHelper.PiOver4) * player.direction;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRotation);
        }

        private void ManageShooting(Player player)
        {
            Item heldItem = player.ActiveItem();
            int animationTime = heldItem.useAnimation;
            int burstTime = heldItem.useTime;

            // Manage animation
            if (AnimationTime > 0)
                AnimationTime--;

            // Manage burst time
            if (BurstTime > 0)
                BurstTime--;

            // Reset animation if counter is zero and check mana. Dont reset without any mana
            if (AnimationTime <= 0 && player.CheckMana(heldItem))
            {
                AnimationTime = animationTime;
                ShotsInBurst = 0;
            }

            // Shoot when burst time is zero and shots in burst is less than 4
            if (BurstTime <= 0 && ShotsInBurst < 4 && player.CheckMana(heldItem, pay: true))
            {
                BurstTime = burstTime;
                ShotsInBurst++;

                // Play sound. Volume dependant on charge
                float shootVolume = 1f - MathF.Pow(BloodCharge, 2f);
                SoundEngine.PlaySound(SoundID.NPCHit19 with { Volume = SoundID.NPCHit19.Volume * shootVolume }, Projectile.Center);
                new MaskShootSoundPacket(Projectile.Center, shootVolume).Send();    // Send a packet since shooting is handled on the client

                // Find spawn position. Randomized behind player
                Vector2 spawnPosition = player.MountedCenter - Vector2.UnitY * player.gravDir * 10f;
                float angleOffset = Main.rand.NextFloat(0.85f);
                Vector2 direction = player.DirectionFrom(player.MouseWorld()).RotatedBy(angleOffset * player.direction);
                int length = (int)(40f / MathF.Cos(angleOffset));   // Increase length depending on angle
                FablesUtils.ShiftShootPositionAhead(ref spawnPosition, direction, length);  // Use offset method to prevent collision with tiles behind user

                int type = ModContent.ProjectileType<FanaticalMaskGlobule>();
                Vector2 velocity = spawnPosition.DirectionTo(player.MouseWorld()) * heldItem.shootSpeed;

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, velocity, type, Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
        }

        public void ChargeUp()
        {
            // Do nothing if at max charge
            if (BloodCharge >= 1f)
                return;

            BloodCharge += 1f / HITS_TO_FULL_CHARGE;

            // Effects upon reaching max charge
            if (BloodCharge >= 1f)
            {
                BloodCharge = 1f;

                if (Main.LocalPlayer == Owner)
                {
                    SoundEngine.PlaySound(FullChargeSound);
                    VignetteStrength = 1f;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

            Player player = Owner;
            var spriteEffects = player.direction * player.gravDir == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Animated positon
            float maskRotation = PutOnMaskProgress * MathHelper.PiOver4 * player.direction * player.gravDir;
            Vector2 offset = new Vector2(6f * player.direction, -9f * player.gravDir).RotatedBy(maskRotation) + player.MountedCenter.Floor();

            // Final drawing position
            // Basically the best we can do to work with everything in vanilla. Rotation from modded sources gets wacky tho
            Vector2 drawPosition = player.RotatedRelativePoint(offset) - Main.screenPosition;
            Vector2 bloomOffset = Vector2.UnitX * player.direction * 3;

            // Final drawing rotation
            maskRotation += player.fullRotation;
            if (player.gravDir == -1)
                maskRotation += MathHelper.Pi;

            Color bloomColor = Color.Red * MathF.Pow(BloodCharge, 2f);
            bloomColor.A = 0;

            Main.EntitySpriteDraw(bloom, drawPosition - bloomOffset, null, bloomColor, 0, bloom.Size() / 2f, 0.24f, 0, 1);
            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, maskRotation, texture.Size() / 2f, 1f, spriteEffects, 1);

            return false;
        }
    }

    [Serializable]
    public class MaskShootSoundPacket(Vector2 position, float volume) : SyncSoundPacket(position, volume)
    {
        public override SoundStyle SyncedSound => SoundID.NPCHit19;    
    }

    public class FanaticalMaskTendril : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "FanaticalMask_Tendril";

        private int Lifetime;

        private Player Owner => Main.player[Projectile.owner];
        private float LifetimeProgress => Utils.GetLerpValue(Lifetime, 0, Projectile.timeLeft, true);
        private float Extension => FablesUtils.PiecewiseAnimation(LifetimeProgress, [new(FablesUtils.SineOutEasing, 0f, 1f, 0.25f), new(FablesUtils.PolyInEasing, 0.3f, 1.25f, -0.4f), new(FablesUtils.PolyOutEasing, 0.67f, 0.85f, -0.7f, 2.5f)]);

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime = 33;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Player player = Owner;

            // Kill the projectile if the player dies
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = new Vector2(6f * player.direction, -9f * player.gravDir) + player.MountedCenter;

            PassiveEffects();

            player.heldProj = Projectile.whoAmI;
            player.SetDummyItemTime(2);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, 0);
            player.SetHeadRotation(Projectile.velocity.ToRotation() + (player.direction == -1 ? MathHelper.Pi : 0));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float grug = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.Zero) * 120f;

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 20f, ref grug);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Why is this the best way to restore mana
            if (Projectile.numHits == 0)
                Owner.QuickSpawnItem(Projectile.GetSource_FromThis(), ItemID.Star);

            target.AddBuff(BuffID.Bleeding, 300);

            OnHitEffects(target);
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanCutTiles() => false;

        #region Visuals
        private void PassiveEffects()
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextFloat(1.5f) > LifetimeProgress)
            {
                Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(4f, 4f);
                dustPosition += Projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-11f, 11f);

                int dustCount = Main.rand.Next(2, 7);
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 dustVelocity = Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.56f) * Main.rand.NextFloat(1f, 6f);
                    Dust.NewDustPerfect(dustPosition, DustID.Blood, dustVelocity, Scale: Main.rand.NextFloat(1f, 1.4f));
                }
            }
        }

        private void OnHitEffects(NPC target)
        {
            if (Main.dedServ)
                return;

            for (int j = 0; j < 3; j++)
            {
                Vector2 dustPosition = target.Center + Main.rand.NextVector2Circular(4f, 4f);
                dustPosition += Projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-11f, 11f);

                int dustCount = Main.rand.Next(2, 7);
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 dustSpeed = Projectile.velocity.RotatedByRandom(MathHelper.PiOver4 * 0.56f) * Main.rand.NextFloat(4f, 8f);
                    Dust.NewDustPerfect(dustPosition, DustID.Blood, dustSpeed, Scale: Main.rand.NextFloat(1.3f, 2f));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            Player player = Owner;
            bool flip = player.direction * player.gravDir == -1;
            var spriteEffects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Vector2 offset = new Vector2(0, -8f * player.gravDir) + player.MountedCenter.Floor();
            float rotation = Projectile.velocity.ToRotation();

            // Final drawing position
            // Basically the best we can do to work with everything in vanilla. Rotation from modded sources gets wacky tho
            Vector2 drawPosition = player.RotatedRelativePoint(offset) - Main.screenPosition;

            // Origin and rotation
            Vector2 origin = new Vector2(6f, texture.Height / 2);
            if (flip)
            {
                origin.X = texture.Width - origin.X;
                rotation += MathHelper.Pi;
            }

            Vector2 squish = new Vector2(Extension, Math.Min(1, 2f - Extension));

            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, rotation, origin, squish, spriteEffects, 1);

            return false;
        }

        #endregion
    }

    public class FanaticalMaskGlobule : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "FanaticalMask_Projectile";

        private int Lifetime;

        public Player Owner => Main.player[Projectile.owner];
        public float LifetimeProgress => Utils.GetLerpValue(Lifetime, 0, Projectile.timeLeft, true);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 9;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.timeLeft = Lifetime = 60;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;

            Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
        }

        public override void AI()
        {
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation();

            // Shrink over time
            Projectile.scale = 0.55f + 0.4f * MathF.Pow(1f - LifetimeProgress, 0.5f);

            // Home towards nearby targets
            NPC target = Projectile.FindHomingTarget(GLOBULE_HOMING_RANGE);
            if (target != null)
            {
                float idealRotation = Projectile.Center.DirectionTo(target.Center).ToRotation();
                float homingRotation = Projectile.rotation.AngleTowards(idealRotation, GLOBULE_HOMING_STRENGTH);

                Projectile.velocity = homingRotation.ToRotationVector2() * Projectile.velocity.Length();
            }

            // Gravity
            Projectile.velocity.Y += MathF.Pow(LifetimeProgress, 2.5f) * 2f;
            if (Projectile.velocity.Y > 13f)
                Projectile.velocity.Y = 13f;

            ManageFrames();
            PassiveEffects();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Charge up mask
            int heldProj = Owner.heldProj;
            if (heldProj != -1 && Main.projectile[heldProj].ModProjectile is FanaticalMaskProjectile mask)
                mask.ChargeUp();

            OnHitEffects(target);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.NPCDeath11 with { MaxInstances = 0 }, Projectile.Center);
            TileCollisionEffects();

            return true;
        }

        private void ManageFrames()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 7)
                Projectile.frame++;

            Projectile.frame %= Main.projFrames[Type];
        }

        #region Visuals
        private void PassiveEffects()
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool())
            {
                Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(4f, 4f) - Projectile.velocity * 0.5f;
                Vector2 direction = Projectile.rotation.ToRotationVector2();
                dustPosition += direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-4f, 4f);

                int dustCount = Main.rand.Next(1, 3);
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 dustVelocity = -direction.RotatedByRandom(MathHelper.PiOver4 * 0.56f) * Main.rand.NextFloat(0f, 2f);
                    Dust.NewDustPerfect(dustPosition, DustID.Blood, dustVelocity, Scale: Main.rand.NextFloat(0.7f, 1f));
                }
            }
        }

        private void OnHitEffects(NPC target)
        {
            if (Main.dedServ)
                return;

            for (int j = 0; j < 3; j++)
            {
                Vector2 dustPosition = target.Center + Main.rand.NextVector2Circular(2f, 2f);
                Vector2 direction = Projectile.rotation.ToRotationVector2();
                dustPosition += direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-7f, 7f);

                int dustCount = Main.rand.Next(2, 7);
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 dustVelocity = direction.RotatedByRandom(MathHelper.PiOver4 * 0.56f) * Main.rand.NextFloat(4f, 8f);
                    Dust.NewDustPerfect(dustPosition, DustID.Blood, dustVelocity, Scale: Main.rand.NextFloat(1.3f, 2f));
                }
            }
        }

        private void TileCollisionEffects()
        {
            if (Main.dedServ)
                return;

            int dustCount = Main.rand.Next(2, 6);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustVelocity = Main.rand.NextVector2Circular(0.2f, 0.2f) + Projectile.velocity * 0.5f;
                Dust.NewDustPerfect(Projectile.Center, DustID.Blood, dustVelocity, Scale: Main.rand.NextFloat(1f, 1.4f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloomStreak = AssetDirectory.CommonTextures.BloomStreak.Value;

            // Draw bloom
            Color bloomColor = Color.Red with { A = 120 };
            Vector2 bloomScale = new Vector2(1f, 0.7f) * Projectile.scale;

            Main.EntitySpriteDraw(bloomStreak, Projectile.Center - Main.screenPosition, null, bloomColor, Projectile.rotation + MathHelper.PiOver2, bloomStreak.Size() / 2, bloomScale, 0, 0);

            Rectangle frame = texture.Frame(1, 5, 0, Projectile.frame, 0, -2);

            Vector2[] oldPositions = Projectile.oldPos;
            for (int i = oldPositions.Length - 1; i >= 0; i--)
            {
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition;

                float cacheProgress = Utils.GetLerpValue(0, oldPositions.Length - 1, i);
                float drawOpacity = i == 0 ? 1f : MathHelper.Lerp(0.6f, 0f, cacheProgress);
                Color drawColor = Color.Lerp(lightColor, i == 0 ? Color.White : Color.DarkRed, 0.6f) * drawOpacity;

                Main.EntitySpriteDraw(texture, drawPosition, frame, drawColor, Projectile.rotation + MathHelper.PiOver2, frame.Size() / 2, Projectile.scale, 0, 0);
            }

            return false;
        }

        #endregion
    }
}