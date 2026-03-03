using CalamityFables.Content.Items.SirNautilusDrops;
using CalamityFables.Particles;
using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalamityFables.Content.Items.SunlessSea
{
    public class CoraletiSpear : ModItem
    {
        public static int MAX_SPEAR_POWER = 5;
        public static int MIN_USE_TIME = 24;
        public static int MAX_USE_TIME = 32;
        public static float MAX_POWER_SPEED_REDUCTION = 0.3f;

        public int spearPower;

        public override string Texture => AssetDirectory.SunlessSeaItems + "CoraletiSpear";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Coraleti Spear");
            Tooltip.SetDefault("Striking enemies causes the spear to grow\n" +
                "At max growth, the spear will shatter into lingering coral fragments");
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 56;

            Item.damage = 23;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.useTurn = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 22;
            Item.knockBack = 6f;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);

            Item.shoot = ModContent.ProjectileType<CoraletiSpearHeld>();
            Item.shootSpeed = 44f;
        }

        public override float UseSpeedMultiplier(Player player)
        {
            return (1f - MAX_POWER_SPEED_REDUCTION * (spearPower / (float)MAX_SPEAR_POWER));
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity = velocity.RotatedByRandom(0.1f);
        }
    }

    public class CoraletiSpearHeld : ModProjectile
    {
        public static float SPEAR_LENGTH_MIN = 70;
        public static float SPEAR_LENGTH_MAX = 90;

        public static int SPEAR_SPRITE_HEIGHT = 126;
        public static int SPEAR_SPRITE_WIDTH = 126;
        public static int SPEAR_SHEET_PADDING = 2;

        public static float MAX_POWER_DAMAGE_BONUS = 1f;

        public override string Texture => AssetDirectory.SunlessSeaItems + "CoraletiSpearHeld";

        public float HeadDistance => MathHelper.Lerp(SPEAR_LENGTH_MIN, SPEAR_LENGTH_MAX, Power / (float)CoraletiSpear.MAX_SPEAR_POWER) - 14f;
        public float AttackProgress => 1f - Owner.itemTime / (float)Owner.itemTimeMax;
        public Player Owner => Main.player[Projectile.owner];
        public int Power
        {
            get
            {
                if (Owner.ItemAnimationActive && Owner.HeldItem is not null && Owner.HeldItem.ModItem is CoraletiSpear spear)
                    return spear.spearPower;
                else
                    return 0;
            }
            set
            {
                if (Owner.ItemAnimationActive && Owner.HeldItem is not null && Owner.HeldItem.ModItem is CoraletiSpear spear)
                    spear.spearPower = value;
            }
        }

        private bool hasPassedMidThrust;
        private bool hitThisThrust;
        private bool hitAtMaxPower;

        private float empowerVisualFlashTimer;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spear");
            ProjectileID.Sets.AllowsContactDamageFromJellyfish[Type] = true;
            ProjectileID.Sets.NoMeleeSpeedVelocityScaling[Type] = true;
            //ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.ownerHitCheck = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;

            Projectile.hide = true;
            //hide = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => AttackProgress < 0.5f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float spearLength = MathHelper.Lerp(SPEAR_LENGTH_MIN, SPEAR_LENGTH_MAX, Power / (float)CoraletiSpear.MAX_SPEAR_POWER);

            Vector2 startPosition = Projectile.Center + Projectile.velocity.Normalized() * (spearLength - SPEAR_LENGTH_MAX);
            Vector2 endPosition = Projectile.Center - Projectile.velocity.Normalized() * SPEAR_LENGTH_MAX / 3;

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), startPosition, endPosition);
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            Owner.UpdateBasicHoldoutVariables(Projectile, -1);

            Owner.direction = Projectile.velocity.X.NonZeroSign();

            Vector2 ownerOrigin = Owner.RotatedRelativePoint(Owner.MountedCenter);

            Projectile.Center = ownerOrigin;

            float distanceFactor = AttackProgress < 0.5f ? AttackProgress * 2f : 1f - (AttackProgress - 0.5f) * 2f;

            float distanceOut = 0;

            if (AttackProgress < 0.5f)
            {
                distanceOut = FablesUtils.ExpOutEasing(Math.Clamp(distanceFactor * 0.8f, 0f, 1f), 2f);
            }
            else
            {
                distanceOut = FablesUtils.PolyInOutEasing(distanceFactor, 1.5f);
            }

            distanceOut *= 90f;

            float rotationOffset = 0;

            if (AttackProgress > 0.5f)
            {
                rotationOffset += FablesUtils.PolyInEasing(1f - distanceFactor, 0.8f) * 0.3f * -Owner.direction;
            }

            Owner.SetCompositeArmFront(true, distanceFactor.ToStretchAmount(), rotationOffset + Projectile.velocity.ToRotation() - MathHelper.PiOver2);

            Projectile.rotation = rotationOffset + Projectile.velocity.ToRotation() + MathHelper.Pi / 4;
            Projectile.Center += Projectile.velocity.Normalized().RotatedBy(rotationOffset) * (distanceOut + 40f);

            if (Owner.whoAmI == Main.myPlayer && Owner.itemAnimation <= 2)
            {
                Projectile.Kill();
                Owner.reuseDelay = 1;
            }

            // Lower power if you miss a swing
            if (AttackProgress > 0.4 && !hasPassedMidThrust)
            {
                hasPassedMidThrust = true;

                if (!hitThisThrust)
                {
                    if (Power > 1)
                    {
                        Power--;

                        for (int i = 0; i < 4 + Power; i++)
                        {
                            Vector2 dustPos = Main.rand.NextVector2FromRectangle(Projectile.Hitbox) + Projectile.velocity.Normalized() * (HeadDistance - SPEAR_LENGTH_MAX);
                            Vector2 dustVel = Projectile.velocity.Normalized() * Main.rand.NextFloat(1f, 2f) + Main.rand.NextVector2Circular(2f, 2f);

                            var d = Dust.NewDustPerfect(dustPos, ModContent.DustType<CoralDust>(), dustVel, Scale: Main.rand.NextFloat(1f, 1.4f));
                            d.noGravity = true;
                        }

                        SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt, Projectile.Center);
                    }
                }
            }

            // Break off the spearhead slightly earlier in the animation
            if (AttackProgress > 0.2f && Power == CoraletiSpear.MAX_SPEAR_POWER)
            {
                if (hitAtMaxPower)
                {
                    ShatterSpear();
                }
            }

            empowerVisualFlashTimer = Math.Max(0f, empowerVisualFlashTimer - 0.08f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 1f + MAX_POWER_DAMAGE_BONUS * (Power / (float)CoraletiSpear.MAX_SPEAR_POWER);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!target.CountsAsACritter && !hitThisThrust)
            {
                hitThisThrust = true;

                if (Power == CoraletiSpear.MAX_SPEAR_POWER)
                {
                    hitAtMaxPower = true;
                    //ShatterSpear();

                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 dustPos = Main.rand.NextVector2FromRectangle(Projectile.Hitbox) + Projectile.velocity.Normalized() * (HeadDistance - SPEAR_LENGTH_MAX + 20f);
                        Vector2 dustVel = Projectile.velocity.Normalized() * Main.rand.NextFloat(1f, 2f) + Main.rand.NextVector2Circular(2f, 2f);

                        var d = Dust.NewDustPerfect(dustPos, ModContent.DustType<CoralDust>(), dustVel, Scale: Main.rand.NextFloat(1f, 1.4f));
                        d.noGravity = true;
                    }

                    SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt, Projectile.Center);
                }
                else
                {
                    Power = Math.Clamp(Power + 1, 0, CoraletiSpear.MAX_SPEAR_POWER);

                    empowerVisualFlashTimer = 1;

                    for (int i = 0; i < 6 + Power; i++)
                    {
                        Vector2 pos = Main.rand.NextVector2FromRectangle(Projectile.Hitbox) + Projectile.velocity.Normalized() * (HeadDistance - SPEAR_LENGTH_MAX + 20f);
                        pos += Projectile.velocity.Normalized() * Main.rand.NextFloat(-16f, 16f);

                        var p = new GenericDust(pos, Projectile.velocity.Normalized() * 2f + Main.rand.NextVector2Circular(1f, 1f), Color.White, new Color(255, 120, 180), scale: Main.rand.NextFloat(1f, 2f), alphaScale: 0.8f, scaleDecay: 0.9f);
                        ParticleHandler.SpawnParticle(p);
                    }
                }
            }
        }

        private void ShatterSpear()
        {
            Power = 0;

            List<int> variants = [1, 2, 3, 4, 0];

            for (int i = 0; i < 3; i++)
            {
                Vector2 shardPosition = Projectile.Center - Projectile.velocity.Normalized() * SPEAR_LENGTH_MAX / 3;

                if (!Collision.CanHit(Owner.Center, 8, 8, shardPosition, 8, 8))
                {
                    shardPosition = Owner.Center;
                }

                Vector2 vel = Projectile.velocity.Normalized().RotatedBy(((i - 1) / 3f) * 0.6f);
                vel *= Main.rand.NextFloat(2f, 8f);
                vel += new Vector2(0, -Main.rand.NextFloat(1f, 4f));

                int variant = Main.rand.NextFromCollection(variants);
                variants.Remove(variant);

                if (Main.myPlayer == Projectile.owner)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), shardPosition, vel, ModContent.ProjectileType<CoraletiSpearShrapnel>(), Projectile.damage, 0f, Projectile.owner, variant);
            }

            /*for (int i = 0; i < 4; i++)
            {
                Color smokeColor = new Color(242, 130, 159, 255);
                Vector2 pos = Main.rand.NextVector2FromRectangle(Projectile.Hitbox) + Projectile.velocity.Normalized() * (HeadDistance - SPEAR_LENGTH_MAX);

                var p = new LingeringExplosionSmoke(pos, Projectile.velocity.Normalized().RotatedByRandom(0.4f) * Main.rand.NextFloat(0f, 4f), Color.LightCoral * 0.3f, smokeColor * 0.4f, smokeColor * 0.6f, Main.rand.NextFloat(0.8f, 1.2f), lighted: true);
                ParticleHandler.SpawnParticle(p);
            }*/

            for (int i = 0; i < 16; i++)
            {
                Vector2 dustPos = Main.rand.NextVector2FromRectangle(Projectile.Hitbox) - Projectile.velocity.Normalized() * 20f;
                Vector2 dustVel = Projectile.velocity.Normalized() * Main.rand.NextFloat(1f, 5f) + Main.rand.NextVector2Circular(2f, 2f);

                var d = Dust.NewDustPerfect(dustPos, ModContent.DustType<CoralDust>(), dustVel, Scale: Main.rand.NextFloat(1f, 1.4f));
                d.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Shatter, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bodyTexture = TextureAssets.Projectile[Type].Value;
            Texture2D lensFlare = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;

            int frameX = Power + (hitAtMaxPower && Power == CoraletiSpear.MAX_SPEAR_POWER ? 1 : 0);

            Rectangle bodySourceRect = new((SPEAR_SPRITE_WIDTH + SPEAR_SHEET_PADDING) * frameX, 0, SPEAR_SPRITE_WIDTH, SPEAR_SPRITE_HEIGHT);
            Rectangle headSourceRect = new((SPEAR_SPRITE_WIDTH + SPEAR_SHEET_PADDING) * frameX, (SPEAR_SPRITE_WIDTH) * 1, SPEAR_SPRITE_WIDTH, SPEAR_SPRITE_HEIGHT);
            Rectangle glowSourceRect = new((SPEAR_SPRITE_WIDTH + SPEAR_SHEET_PADDING) * frameX, (SPEAR_SPRITE_WIDTH) * 2, SPEAR_SPRITE_WIDTH, SPEAR_SPRITE_HEIGHT);
            Rectangle aboveGlowSourceRect = new((SPEAR_SPRITE_WIDTH + SPEAR_SHEET_PADDING) * frameX, (SPEAR_SPRITE_WIDTH) * 3, SPEAR_SPRITE_WIDTH, SPEAR_SPRITE_HEIGHT);

            float flashOpacity = FablesUtils.PolyOutEasing(empowerVisualFlashTimer, 3f);

            Vector2 drawPos = Projectile.Center;// - Projectile.velocity.Normalized() * HeadDistance;
            Vector2 drawOrigin = new(bodySourceRect.Width - 10, 10);

            //Vector2 shake = Main.rand.NextVector2Circular(2f, 2f) * flashOpacity;

            Texture2D bloomTex = AssetDirectory.CommonTextures.BloomCircle.Value;

            Vector2 headOffset = (Projectile.rotation - MathHelper.Pi / 4).ToRotationVector2() * 8f * flashOpacity;
            float headScale = Power == 1 ? 1f : Projectile.scale * (1f + 0.2f * flashOpacity);

            // Dont scale head for final frame
            if (hitAtMaxPower && Power == CoraletiSpear.MAX_SPEAR_POWER)
            {
                headScale = 1f;
                headOffset = Vector2.Zero;
            }

            Color glowColor = Power == 1 ? Color.White : Color.Lerp(Color.HotPink, Color.Pink, 0.3f);

            Vector2 glowPos = drawPos + Projectile.velocity.Normalized() * (HeadDistance - SPEAR_LENGTH_MAX + 4f);

            Vector2 bloomScale = Vector2.Lerp(new Vector2(0.5f, 0.2f), new Vector2(0.7f, 0.5f), Power / (float)CoraletiSpear.MAX_SPEAR_POWER) * flashOpacity;

            // Flashing bloom behind spearhead
            Main.EntitySpriteDraw(bloomTex, glowPos - Main.screenPosition, null, Color.HotPink with { A = 0 } * flashOpacity * 0.3f, Projectile.velocity.ToRotation(), bloomTex.Size() / 2, bloomScale, SpriteEffects.None);
            // Draw spearhead
            Main.EntitySpriteDraw(bodyTexture, drawPos - Main.screenPosition + headOffset, headSourceRect, lightColor, Projectile.rotation, drawOrigin, headScale, SpriteEffects.None);
            // Draw spearhead glowmask
            Main.EntitySpriteDraw(bodyTexture, drawPos - Main.screenPosition + headOffset, glowSourceRect, glowColor * flashOpacity, Projectile.rotation, drawOrigin, headScale, SpriteEffects.None);
            // Draw spear handle
            Main.EntitySpriteDraw(bodyTexture, drawPos - Main.screenPosition, bodySourceRect, lightColor, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None);

            // Draw spear handle glowmask with different parameters depending on whether its the full spearhead glow or not
            if (Power != 1)
                Main.EntitySpriteDraw(bodyTexture, drawPos - Main.screenPosition, aboveGlowSourceRect, glowColor with { A = 0 } * flashOpacity, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None);
            else
                Main.EntitySpriteDraw(bodyTexture, drawPos - Main.screenPosition + headOffset, aboveGlowSourceRect, glowColor * flashOpacity, Projectile.rotation, drawOrigin, headScale, SpriteEffects.None);

            if (Power == 1)
            {
                Main.EntitySpriteDraw(lensFlare, glowPos - Main.screenPosition, null, Color.Pink with { A = 100 } * flashOpacity, Projectile.velocity.ToRotation() + MathHelper.PiOver2, lensFlare.Size() / 2, 1f * flashOpacity, SpriteEffects.None);
                Main.EntitySpriteDraw(lensFlare, glowPos - Main.screenPosition, null, Color.White with { A = 100 } * flashOpacity * 0.2f, Projectile.velocity.ToRotation() + MathHelper.PiOver2, lensFlare.Size() / 2, 0.8f * flashOpacity, SpriteEffects.None);
            }

            return false;
        }
    }

    public class CoraletiSpearShrapnel : ModProjectile
    {
        public static int SHARD_SPRITE_WIDTH = 28;
        public static int SHARD_SPRITE_VARIANTS = 5;

        public static float SHARD_FLYING_DAMAGE_MULT = 0.5f;

        public override string Texture => AssetDirectory.SunlessSeaItems + "CoraletiSpearShards";

        //private List<Vector2> trailCache = [];
        //private PrimitiveTrail trail;

        public int Variant
        {
            get
            {
                return (int)Projectile.ai[0];
            }
            set
            {
                Projectile.ai[0] = value;
            }
        }

        private bool landed;

        private float hitShake;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.penetrate = 6;
            Projectile.timeLeft = 1200;

            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 30;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.3f;

            if (!landed)
            {
                Projectile.velocity.X *= 0.995f;

                Projectile.rotation += Projectile.velocity.X / 30;
            }
            else
            {
                if (hitShake > 0f)
                {
                    hitShake -= 0.1f;
                }
            }

            if (Projectile.velocity.Length() > 1f)
            {
                landed = false;
            }

            /*if (!Main.dedServ)
            {
                UpdateTrail();
            }*/
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!landed)
            {
                modifiers.FinalDamage *= SHARD_FLYING_DAMAGE_MULT;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (landed)
            {
                hitShake = 1f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!landed)
            {
                if (oldVelocity.Length() > 3f)
                {
                    if (Projectile.velocity.X != oldVelocity.X)
                        Projectile.velocity.X = -oldVelocity.X;
                    if (Projectile.velocity.Y != oldVelocity.Y)
                        Projectile.velocity.Y = -oldVelocity.Y;

                    SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
                }
                else
                {
                    if (!landed)
                    {
                        landed = true;

                        //Projectile.penetrate = 4;
                    }
                }
            }

            Projectile.velocity *= 0.5f;

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (timeLeft > 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 dustPos = Main.rand.NextVector2FromRectangle(Projectile.Hitbox);
                    Vector2 dustVel = Main.rand.NextVector2Circular(2f, 2f) + new Vector2(0, -2f);

                    var d = Dust.NewDustPerfect(dustPos, ModContent.DustType<CoralDust>(), dustVel, Scale: Main.rand.NextFloat(1f, 1.6f));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Rectangle sourceRect = new(SHARD_SPRITE_WIDTH * Variant, 0, SHARD_SPRITE_WIDTH, SHARD_SPRITE_WIDTH);

            float shake = MathF.Sin((float)Main.timeForVisualEffects * 0.5f) * 0.3f * hitShake;

            float opacity = Math.Clamp(Projectile.timeLeft / 100f, 0f, 1f);

            Main.EntitySpriteDraw(TextureAssets.Projectile[Type].Value, Projectile.Center - Main.screenPosition, sourceRect, lightColor * opacity, Projectile.rotation + shake, sourceRect.Size() / 2, Projectile.scale, SpriteEffects.None);

            return false;
        }

        /*private void UpdateTrail()
        {
            trail ??= new PrimitiveTrail(16, x => x * 8f, x => Color.Pink.MultiplyRGB(Lighting.GetColor(Projectile.Center.ToTileCoordinates())) * 0.2f * x);

            trailCache.Add(Projectile.Center);
            while (trailCache.Count > 16)
                trailCache.RemoveAt(0);

            trail.SetPositions(trailCache);
        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            trail.Render(null, -Main.screenPosition);
        }*/
    }

    public class CoralDust : ModDust
    {
        public override string Texture => AssetDirectory.SunlessSeaItems + Name;

        public override bool Update(Dust dust)
        {
            if (!dust.noGravity)
                dust.velocity.Y += 0.2f;
            else
                dust.velocity.Y *= 0.97f;

            dust.velocity.X *= 0.97f;

            dust.position += dust.velocity;
            dust.rotation += dust.velocity.X / 30f;

            dust.scale *= 0.95f;

            if (dust.scale <= 0.2f)
                dust.active = false;

            return false;
        }
    }
}
