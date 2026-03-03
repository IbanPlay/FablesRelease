using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Core.DrawLayers;
using CalamityFables.Particles;
using ReLogic.Utilities;
using Terraria.DataStructures;
using Terraria.Localization;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    [ReplacingCalamity("Fungicide")]
    public class Sporethrower : ModItem, ICustomHeldDraw
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        #region Sounds
        public static readonly SoundStyle FireStartSound = new SoundStyle(SoundDirectory.CrabulonDrops + "SporethrowerStart");
        public static readonly SoundStyle GasSound = new SoundStyle(SoundDirectory.CrabulonDrops + "SporethrowerGas");
        public static readonly SoundStyle SqueezeSound = new SoundStyle(SoundDirectory.CrabulonDrops + "SporethrowerMushroomSqueeze", 2);
        public static readonly SoundStyle SqueakSound = new SoundStyle(SoundDirectory.CrabulonDrops + "SporethrowerDepletedSqueak", 2);
        public static readonly SoundStyle RefillSound = new SoundStyle(SoundDirectory.CrabulonDrops + "SporethrowerRefill") { PauseBehavior = PauseBehavior.PauseWithGame };
        public static readonly SoundStyle ReadySound = new SoundStyle(SoundDirectory.CrabulonDrops + "SporethrowerReady");

        #endregion

        #region Reflection Fields
        public static Vector2 DAMAGE_MULT_RANGE = new(0.3f, 1f);
        public static float MAX_CHARGE = 70; //How much charge can the sporethrower hold
        public static float RECHARGE_RATE = 1f; // How much charge is restored per tick
        public static float RECHARGE_RATE_NOT_HELD = 0.5f;  // Recharge rate when not holding the item
        public static int FREE_SHOTS = 1; //How many shots can be fired without losing any power
        public static int SHOTS_PER_ATTACK = 8; //How many shots are fired per attack
        public static float CHARGE_LOST_PER_ATTACK = 20; //How much charge is lost per attack
        public static int DEBUFF_DOT = 12;
        public static Point DEBUFF_DURATION_RANGE = new(60, 480);

        #endregion

        public float charge;

        private SlotId refillSoundSlot; //track the refill sound so it can be ended early if needed

        public static float ChargeJuicePercent(float charge) => Utils.GetLerpValue(0, MAX_CHARGE - FREE_SHOTS * CHARGE_LOST_PER_ATTACK, charge, true);


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sporethrower");
            Tooltip.SetDefault("Generates its own ammunition from a homegrown mushroom\n" +
                               "Reduced effectiveness when over-exerting the mushroom\n" +
                               "Ignores 5 points of enemy Defense");

            FablesSets.HasCustomHeldDrawing[Type] = true;
            SporethrowerTankLayer.SporethrowerType = Type;
        }

        public override void SetDefaults()
        {
            Item.damage = 16;
            Item.ArmorPenetration = 10;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 23;
            Item.height = 8;

            Item.useAnimation = 30;
            Item.useTime = 4;
            Item.reuseDelay = 36;
            Item.useLimitPerAnimation = SHOTS_PER_ATTACK;

            Item.knockBack = 0f;
            Item.shoot = ModContent.ProjectileType<SporethrowerJet>();
            Item.shootSpeed = 6f;

            Item.useStyle = ItemUseStyleID.Shoot;
            //Item.UseSound = SoundID.Item34;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 7, 50, 0);
            Item.rare = ItemRarityID.Green;

            Item.noUseGraphic = true;
            Item.ChangePlayerDirectionOnShoot = false;
        }

        #region Held visuals
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 pointing = itemRotation.ToRotationVector2();

            Vector2 itemPosition = player.MountedCenter + pointing * 7f;
            Vector2 itemSize = new Vector2(74, 28);
            Vector2 itemOrigin = new Vector2(-25, 2);

            float animProgress = 1 - player.itemAnimation / (float)player.itemAnimationMax;
            float useProgress = 1 - player.itemTime / (float)player.itemTimeMax;

            if (player.reuseDelay > 0)
            {
                itemPosition += Main.rand.NextVector2Circular(3f, 3f) * MathF.Pow(1 - animProgress, 1.5f);

                itemRotation += Main.rand.NextFloat(-0.13f, 0.13f) * MathF.Pow(useProgress, 1.6f);
            }
            else
            {
                itemPosition += pointing * (5f - 12f * MathF.Pow(1 - player.itemTime / (float)Item.reuseDelay, 0.5f));
            }

            FablesUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);

            float animProgress = 1 - player.itemAnimation / (float)player.itemAnimationMax;
            float useProgress = 1 - player.itemTime / (float)player.itemTimeMax;

            float rotation = (player.Center - player.MouseWorld()).ToRotation() * player.gravDir + MathHelper.PiOver2;

            Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;

            if (player.reuseDelay != 0)
            {
                rotation += Main.rand.NextFloat(-0.13f, 0.13f) * MathF.Pow(animProgress, 1.6f);

                stretch = 1f.ToStretchAmount();

                //if (animProgress < 0.4f)
                //    rotation += 0.1f * animProgress * player.direction;
            }
            else
            {
                float reuseCooldown = player.itemTime / (float)Item.reuseDelay;
                rotation += MathF.Pow(reuseCooldown, 1.6f) * player.direction * -0.2f;
                stretch = MathF.Pow(reuseCooldown, 2f).ToStretchAmount();


                player.SetCompositeArmBack(true, MathF.Sin(reuseCooldown * MathHelper.Pi).ToStretchAmount(), rotation + MathHelper.PiOver4 * player.direction * player.gravDir);
            }

            player.SetCompositeArmFront(true, stretch, rotation);
        }

        public static Asset<Texture2D> ShroomlessGun;
        public static Asset<Texture2D> GunlessShroom;
        public static Asset<Texture2D> Bloom;

        public void DrawHeld(ref PlayerDrawSet drawInfo, Texture2D texture, Vector2 position, Rectangle frame, float rotation, Color color, float scale, Vector2 origin)
        {
            ShroomlessGun ??= ModContent.Request<Texture2D>(Texture + "Shroomless");
            GunlessShroom ??= ModContent.Request<Texture2D>(Texture + "Shroom");
            Bloom ??= ModContent.Request<Texture2D>(Texture + "Bloom");

            float animProgress = 1 - drawInfo.drawPlayer.itemAnimation / (float)drawInfo.drawPlayer.itemAnimationMax;
            bool cooldown = drawInfo.drawPlayer.reuseDelay == 0;
            if (cooldown)
                animProgress = drawInfo.drawPlayer.itemTime / (float)drawInfo.heldItem.reuseDelay;


            Vector2 bulbOffset = new Vector2(60, 7);
            Vector2 bulbOrigin = new Vector2(50, 21);
            drawInfo.AdjustItemOffsetOrigin(frame, ref bulbOffset, ref bulbOrigin);
            Vector2 bulbPosition = position + bulbOffset.RotatedBy(rotation) * scale;

            Vector2 bulbSize = Vector2.One;

            bulbSize.Y *= 1f + 0.4f * MathF.Pow(animProgress, 1.4f);
            bulbSize.X *= 0.6f + 0.5f * MathF.Pow(1 - animProgress, 1.4f);

            DrawData bulb = new DrawData(GunlessShroom.Value, bulbPosition, frame, color, rotation, bulbOrigin, bulbSize * scale, drawInfo.itemEffect);
            drawInfo.DrawDataCache.Add(bulb);


            float chargeJuice = ChargeJuicePercent((drawInfo.drawPlayer.HeldItem.ModItem as Sporethrower).charge);
            if (chargeJuice > 0.3f)
            {
                Color shroomBloom = new Color(90, 50, 255) * chargeJuice;
                bulb = new DrawData(GunlessShroom.Value, bulbPosition, frame, shroomBloom with { A = 0 }, rotation, bulbOrigin, bulbSize * scale, drawInfo.itemEffect);
                drawInfo.DrawDataCache.Add(bulb);
            }


            DrawData item = new DrawData(ShroomlessGun.Value, position, frame, color, rotation, origin, scale, drawInfo.itemEffect);
            drawInfo.DrawDataCache.Add(item);

            Color bloom = Color.Lerp(new Color(70, 30, 255), new Color(144, 90, 255), animProgress);
            bloom *= animProgress;
            item = new DrawData(Bloom.Value, position, frame, bloom with { A = 0 }, rotation, origin, scale, drawInfo.itemEffect);
            drawInfo.DrawDataCache.Add(item);
        }
        #endregion

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity *= 0.3f + 0.7f * ChargeJuicePercent(charge);
            damage = (int)(damage * MathHelper.Lerp(DAMAGE_MULT_RANGE.X, DAMAGE_MULT_RANGE.Y, ChargeJuicePercent(charge)));

            //Shift the position up, and then ahead
            position += velocity.RotatedBy(-MathHelper.PiOver2 * player.direction).SafeNormalize(Vector2.Zero) * 8f;
            Vector2 direction = velocity.SafeNormalize(Vector2.Zero);
            FablesUtils.ShiftShootPositionAhead(ref position, direction, 60);

        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float decrement = CHARGE_LOST_PER_ATTACK / SHOTS_PER_ATTACK;
            charge -= decrement;
            if (charge < 0)
                charge = 0;

            //If we rotate the velocity in ModifyShootStats the weapon will jerk weirdly
            if (charge < MAX_CHARGE * 0.5f)
                velocity = velocity.RotatedByRandom(0.5f * (1 - charge / MAX_CHARGE));

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0, charge, player.ItemUsesThisAnimation);
            return false;
        }

        public override void HoldItem(Player player)
        {
            player.SyncMousePosition();

            //Somehow the item moused over does not run update inventory
            if (player.inventory[PlayerItemSlotID.InventoryMouseItem] == Item)
            {
                UpdateInventory(player);
                if (Main.mouseItem.ModItem is Sporethrower sporethrower)
                    sporethrower.charge = charge;
            }
        }

        public override void UpdateInventory(Player player)
        {
            bool unchargedPreviously = charge < MAX_CHARGE;

            ActiveSound activeRefillSound = null;

            if (!Main.dedServ)
            {
                SoundEngine.TryGetActiveSound(refillSoundSlot, out activeRefillSound);
            }

            if (!player.ItemAnimationActive || player.HeldItem != Item)
            {
                charge += player.HeldItem == Item ? RECHARGE_RATE : RECHARGE_RATE_NOT_HELD;

                float minChargeForSound = player.HeldItem != Item ? MAX_CHARGE * ((1f - RECHARGE_RATE_NOT_HELD) / RECHARGE_RATE) : 0f;
                if (!Main.dedServ && activeRefillSound is null && charge >= minChargeForSound && charge <= MAX_CHARGE)
                {
                    refillSoundSlot = SoundEngine.PlaySound(RefillSound);
                    SoundEngine.TryGetActiveSound(refillSoundSlot, out activeRefillSound);
                }

                if (charge >= MAX_CHARGE)
                {
                    charge = MAX_CHARGE;
                    if (unchargedPreviously && player.whoAmI == Main.myPlayer)
                    {
                        SoundEngine.PlaySound(ReadySound);
                        for (int i = 0; i < 5; i++)
                        {
                            int num = Dust.NewDust(player.position, player.width, player.height, 45, 0f, 0f, 255, default(Color), (float)Main.rand.Next(20, 26) * 0.1f);
                            Main.dust[num].noLight = true;
                            Main.dust[num].noGravity = true;
                            Main.dust[num].velocity *= 0.5f;
                        }

                        if (!Main.dedServ && activeRefillSound != null)
                            activeRefillSound.Stop();
                    }
                }
            }
            else
            {
                if (!Main.dedServ && activeRefillSound != null)
                    activeRefillSound.Stop();
            }
        }

        public override ModItem Clone(Item Item)
        {
            ModItem clone = base.Clone(Item);
            (clone as Sporethrower).charge = (Item.ModItem as Sporethrower).charge;
            return clone;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int damageIndex = tooltips.FindIndex(tooltip => tooltip.Name == "Damage" && tooltip.Mod == "Terraria");

            // Return if index is -1
            if (damageIndex < 0)
                return;

            int baseDamage = Main.LocalPlayer.GetWeaponDamage(Item, true);
            int minDamage = (int)(baseDamage * DAMAGE_MULT_RANGE.X);
            int maxDamage = (int)(baseDamage * DAMAGE_MULT_RANGE.Y);

            // Modify damage line to display a range
            tooltips[damageIndex].Text = $"{maxDamage}-{minDamage}" + Language.GetText("LegacyTooltip.3");
        }
    }

    public class SporethrowerJet : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        private int IndexInSalvo => (int)Projectile.ai[2] - 1;
        private bool playedSound = false;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spores");
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.Flames);
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.aiStyle = -1;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.ignoreWater = true;
        }

        public ref float Time => ref Projectile.ai[0];
        public float ChargePercent => MathF.Pow(Sporethrower.ChargeJuicePercent(Projectile.ai[1]), 0.8f);

        public bool playedExtraSounds = false;

        public override void AI()
        {
            if (Time > 64)
                Projectile.velocity *= 0.95f;

            if (IndexInSalvo == 0 && !playedSound)
            {
                SoundEngine.PlaySound(SoundID.Item34 with { Pitch = 1 - ChargePercent, Volume = ChargePercent * 0.8f + 0.2f }, Projectile.Center);
                SoundEngine.PlaySound(Sporethrower.GasSound with { Pitch = 1 - ChargePercent }, Projectile.Center);

                playedSound = true;
            }

            if (IndexInSalvo == 0 && !playedExtraSounds && Time > 16)
            {
                SoundEngine.PlaySound(Sporethrower.SqueezeSound with { Pitch = Utils.GetLerpValue(0.4f, 0f, ChargePercent, true) }, Projectile.Center);

                if (ChargePercent < 0.5f)
                    SoundEngine.PlaySound(Sporethrower.SqueakSound with { Volume = Utils.GetLerpValue(0.5f, 0f, ChargePercent, true) }, Projectile.Center);

                playedExtraSounds = true;
            }

            if (Time <= 80)
            {
                if (Main.rand.NextBool(3 + (int)((1 - ChargePercent) * 3)))
                {
                    float scaleMultiplier = Utils.GetLerpValue(5, 50, Time, true) * 0.6f + 0.4f;

                    SporeGas incrediblyGassy = new SporeGas(Projectile.Center, Main.rand.NextVector2Circular(3f, 3f) + Projectile.velocity, Projectile.Center, 30f, Main.rand.NextFloat(2f, 4f) * scaleMultiplier);
                    if (!Main.rand.NextBool(6))
                        incrediblyGassy.forceNoDust = true;

                    incrediblyGassy.Scale *= 0.6f + 0.4f * ChargePercent;
                    incrediblyGassy.Velocity *= 0.6f + 0.4f * ChargePercent;

                    ParticleHandler.SpawnParticle(incrediblyGassy);
                }

                if (Main.rand.NextBool(5 + (int)((ChargePercent) * 4)))
                {
                    float radius = 40f * (0.6f + 0.4f * ChargePercent) * (Utils.GetLerpValue(50, 70, Time, true) + 1);
                    Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(radius, radius), DustID.GlowingMushroom, Projectile.velocity * Main.rand.NextFloat(3f, 6f) + Main.rand.NextVector2Circular(2f, 2f), Scale: Main.rand.NextFloat(0.8f, 1.3f));
                    d.noGravity = true;
                }

                if (Time < 6 && Main.rand.NextBool())
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), DustID.MushroomSpray, Projectile.velocity * Main.rand.NextFloat(1f, 3f) + Main.rand.NextVector2Circular(4f, 4f), Scale: Main.rand.NextFloat(1f, 2f));
                    d.noGravity = true;
                }
            }

            Time++;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //Slow down on tile collision
            Projectile.velocity = oldVelocity * 0.95f;
            Projectile.position -= Projectile.velocity;
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float hitboxWidth = 20f + Utils.GetLerpValue(50, 80, Time, true) * 40f * ChargePercent;
            return FablesUtils.AABBvCircle(targetHitbox, Projectile.Center, hitboxWidth);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CrabulonDOT>(), (int)MathHelper.Lerp(Sporethrower.DEBUFF_DURATION_RANGE.X, Sporethrower.DEBUFF_DURATION_RANGE.Y, ChargePercent));
            Projectile.damage = (int)(Projectile.damage * 0.85f);
        }
    }
}
