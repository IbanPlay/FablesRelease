using CalamityFables.Content.Items.BurntDesert;
using CalamityFables.Content.Items.Sky;
using CalamityFables.Cooldowns;
using CalamityFables.Core.DrawLayers;
using CalamityFables.Particles;
using ReLogic.Utilities;
using System.IO;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using static CalamityFables.Content.Items.EarlyGameMisc.OpulentDartgun;
using static CalamityFables.Content.Items.EarlyGameMisc.OpulentDartProjectileHandling;
using static CalamityFables.Content.Items.EarlyGameMisc.OpulentInjectionBuff;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class OpulentDartgun : ModItem, ICustomHeldDraw
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        internal static Asset<Texture2D> CartridgeTexture;
        internal static Asset<Texture2D> CartridgeOverlayTexture;

        #region Reflection Fields
        public static int SELF_INJECT_ANIM_TIME = 60;
        public static int SHOOT_SYRINGE_ANIM_TIME = 30;
        public static int CARTRIDGE_RECHARGE_TIME = 600;
        public static int SELF_OVERDOSE_BUFF_TIME = 480;
        public static int OTHER_OVERDOSE_BUFF_TIME = 900;

        public static int PLAYER_OVERDOSING_DOT = 12;
        public static int PLAYER_OVERDOSING_MIN_SELF_DAMAGE = 50;
        public static float PLAYER_OVERDOSING_DAMAGE_PER_HEALTH = 0.25f;
        public static float PLAYER_OVERDOSING_DAMAGE_BONUS = 0.15f;
        public static float PLAYER_OVERDOSING_ATTACK_SPEED_BONUS = 0.3f;
        /// <summary>
        /// If the player hasn't healed a single HP point during the last 4 seconds, the player can recieve this full heal by hitting a dart <br/>
        /// The average of the healing recieved during the last 4 seconds is substracted from this amount when a dart heals the player
        /// </summary>
        public static int PLAYER_OVERDOSING_HEALING_BUDGET = 12;
        public static int MIN_SLOWING_COUNTER = 180; // The counter number below which the slowness counter will have no effect on movement speed
        public static int MAX_SLOWING_COUNTER = 480; // The counter number at which slowness is at it's strongest
        public static float MAX_MOVE_SPEED_PENALTY = 0.5f;

        public static int NPC_OVERDOSING_DOT = 30;
        public static float NPC_OVERDOSING_DAMAGE_MULTIPLIER = 1.5f;

        #endregion

        #region Sounds
        internal static SoundStyle ShootSound = new(SoundDirectory.Sounds + "OpulentDartgunShoot");
        internal static SoundStyle SelfInjectSound = new(SoundDirectory.Sounds + "OpulentDartgunSelfInject");
        internal static SoundStyle SyringeShotSound = new(SoundDirectory.Sounds + "OpulentDartgunSpecialDartShoot");
        internal static SoundStyle SyringeReloadSound = new(SoundDirectory.Cooldowns + "OpulentDartgunCooldownRefresh");
        internal static SoundStyle SyringeTrippingLoop = new(SoundDirectory.Sounds + "OpulentDartgunTrippingBallsLoop") { IsLooped = true };

        #endregion

        public enum AltUseTypes
        {
            None,
            SelfInject,
            Shoot
        }

        public AltUseTypes AltUseType;

        public override void Load()
        {
            OpulentDartProjectileHandling.Load();

            FablesPlayer.ModifyHurtEvent += SelfInjectDamage;
            CameraManager.ImmunityExceptionEvent += IgnoreCutsceneImmunity;
        }

        public override void SetStaticDefaults()
        {
            FablesSets.HasCustomHeldDrawing[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 24;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 28;
            Item.height = 28;
            Item.useTime = Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2.25f;
            Item.value = Item.sellPrice(gold: 4, silver: 50);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.autoReuse = true;
            Item.useAmmo = AmmoID.Dart;
            Item.shootSpeed = 13f;

            Item.ChangePlayerDirectionOnShoot = false;
        }

        public override void HoldItem(Player player)
        {
            player.SyncRightClick();
            player.SyncMousePosition();
        }

        public override void UseAnimation(Player player)
        {
            player.SyncAltFunctionUse();
            AltUseType = AltUseTypes.None;

            // Dont use a sound on alt use
            Item.UseSound = player.altFunctionUse == 2 ? null : SoundID.Item98 with { Pitch = 0.2f, PitchVariance = 0.1f };
        }

        public override bool AltFunctionUse(Player player)
        {
            if (player.HasCooldown(OpulentSyringeReload.ID))
                return false;

            // Can't be used while overdosing
            return !player.GetPlayerData<OpulentInjectionPlayerData>(out _) || !player.GetPlayerFlag("OpulentInjectionBuff");
        }

        public override float UseSpeedMultiplier(Player player) => player.altFunctionUse == 2 ? 0.5f : 1f;

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            // UseStyle runs on a 'fake' clone of the item so we have to do this to save persistent data correctly
            var dartgun = GetDartgun(player);

            // Runs actual UseStyle code for the shoot animation
            RealUseStyle(player, dartgun);
            
            // Runs alt use logic, self-injection or shooting
            AltUseLogic(player, dartgun);
        }

        public static OpulentDartgun GetDartgun(Player player) => player.HeldItem.ModItem as OpulentDartgun;

        public static bool AltUse(Player player) => player.altFunctionUse == 2;

        #region Shooting
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Shift shot position up slightly
            Vector2 offset = velocity.SafeNormalize(Vector2.Zero).RotatedBy(-MathHelper.PiOver2) * 7f * player.direction;
            position += offset;

            // Increase velocity on alt use
            if (AltUse(player))
                velocity *= 1.2f;

            // Add spread on normal use
            else
                velocity = velocity.RotatedByRandom(0.05f);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Dont do anything if alt use and not shooting
            if (AltUse(player) && AltUseType != AltUseTypes.Shoot)
                return false;

            // Change source on normal use
            if (!AltUse(player))
            {
                // Use a custom entity source, which can be read in Projectile.SpawnProjectile which is called inside of NewProjectile
                // Doing it this way lets us set data before the projectile sync packet gets sent to the server in multiplayer
                // Otherwise we'll be setting data to the dart after its been sent on the server which means a race condition which is akward
                source = new EntitySource_OpulentDartgun(source, player.GetPlayerData(out OpulentInjectionPlayerData odData) && player.GetPlayerFlag("OpulentInjectionBuff"));
            }

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override bool CanConsumeAmmo(Item ammo, Player player) => !AltUse(player); // Dont use ammo on alt use

        private void AltUseLogic(Player player, OpulentDartgun dartgun)
        {
            if (!AltUse(player))
                return;

            // Determine if the use type is self-inject or syringe shoot
            // AltUseType is initially set to none and is determined 10 ticks before the animation ends
            if (dartgun.AltUseType == AltUseTypes.None && player.itemTime <= player.itemTimeMax - 10 && !player.ItemAnimationJustStarted)
            {
                // If holding right click, set to self inject
                if (player.RightClicking())
                {
                    // Set anim times and alt use type
                    player.itemTime = player.itemTimeMax = player.itemAnimation = player.itemAnimationMax = SELF_INJECT_ANIM_TIME;
                    dartgun.AltUseType = AltUseTypes.SelfInject;
                }
                // Otherwise, set to shoot
                else
                {
                    // Set anim times and alt use type
                    player.itemTime = player.itemTimeMax = player.itemAnimation = player.itemAnimationMax = SHOOT_SYRINGE_ANIM_TIME;
                    dartgun.AltUseType = AltUseTypes.Shoot;
                }
            }

            if (dartgun.AltUseType == AltUseTypes.SelfInject)
            {
                if (player.itemTime == 25)
                    SoundEngine.PlaySound(SelfInjectSound, player.MountedCenter);

                // Inject user
                if (player.itemTime == 20)
                {
                    if (Main.LocalPlayer == player)
                        InjectPlayer(player);

                    EjectCartridge(player);
                    ParticleHandler.SpawnParticle(new CircularPulseShine(player.Center, Color.Gold, 0.7f));
                    player.AddCooldown(OpulentSyringeReload.ID, CARTRIDGE_RECHARGE_TIME);
                }

                // Allow the inject animation to end without injecting if the player releases right click
                if (player.itemTime > player.itemTimeMax - (int)(player.itemTimeMax * 0.4f) && !player.RightClicking())
                {
                    dartgun.AltUseType = AltUseTypes.None;
                    player.itemTime = 5;
                    player.itemAnimation = 5;
                }
            }

            if (dartgun.AltUseType == AltUseTypes.Shoot)
            {
                // Run at end of animation
                if (player.itemTime == player.itemTimeMax - 1)
                {
                    if (Main.LocalPlayer == player)
                    {
                        // Setup shoot parameters
                        Vector2 velocity = player.MountedCenter.SafeDirectionTo(player.MouseWorld()) * Item.shootSpeed;
                        Vector2 position = player.MountedCenter;
                        int type = ModContent.ProjectileType<OpulentDartgunBigSyringe>();
                        int damage = Item.damage;
                        float knockback = Item.knockBack;
                        var source = player.GetSource_ItemUse_WithPotentialAmmo(Item, Item.ammo);

                        // Run for compat and stuff
                        ItemLoader.ModifyShootStats(Item, player, ref position, ref velocity, ref type, ref damage, ref knockback);
                        ItemLoader.Shoot(Item, player, (EntitySource_ItemUse_WithAmmo)source, position, velocity, type, damage, knockback);
                    }

                    SoundEngine.PlaySound(SyringeShotSound, player.MountedCenter);
                    EjectCartridge(player);
                    player.AddCooldown(OpulentSyringeReload.ID, CARTRIDGE_RECHARGE_TIME);
                }
            }
        }
        #endregion

        #region Applying Overdose
        public static void InjectPlayer(Player player, Projectile sourceProjectile = null)
        {
            PlayerDeathReason deathReason;

            // Self damage 
            if (sourceProjectile is null)
            {
                ApplyOverdose(player, true, SELF_OVERDOSE_BUFF_TIME);

                deathReason = PlayerDeathReason.ByPlayerItem(player.whoAmI, player.HeldItem);
                deathReason.CustomReason = NetworkText.FromKey("Mods.CalamityFables.Extras.DeathMessages.OpulentSelfInjection." + Main.rand.Next(1, 3).ToString(), player.name);
            }
            // From big syringe
            else
            {
                ApplyOverdose(player, true, OTHER_OVERDOSE_BUFF_TIME);

                deathReason = PlayerDeathReason.ByProjectile(sourceProjectile.owner, sourceProjectile.whoAmI);

                string attackerName = sourceProjectile.GetOwner()?.name ?? "???";
                deathReason.CustomReason = NetworkText.FromKey("Mods.CalamityFables.Extras.DeathMessages.OpulentInjection." + Main.rand.Next(1, 3).ToString(), player.name, attackerName);
            }

            // Placeholder damage
            player.Hurt(deathReason, 1, 0, dodgeable: false);
        }

        public static void ApplyOverdose(Player player, bool healthDrain, int duration)
        {
            // Initialize data
            if (!player.GetPlayerData(out OpulentInjectionPlayerData data))
            {
                data = new OpulentInjectionPlayerData();
                player.SetPlayerData(data);
            }

            data.HealthDrain = healthDrain;
            player.AddBuff(ModContent.BuffType<OpulentInjectionBuff>(), duration, false);
            player.SyncPlayerData<OpulentInjectionPlayerData, SyncOverdoseDataModule>();
        }

        private static void SelfInjectDamage(Player player, ref Player.HurtModifiers modifiers)
        {
            var damageSource = modifiers.DamageSource;

            // Check if source was opulent dartgun big syringe or self injection
            if (damageSource.SourceProjectileType != ModContent.ProjectileType<OpulentDartgunBigSyringe>()
                && (damageSource.SourcePlayerIndex != player.whoAmI || damageSource.SourceItem is null || damageSource.SourceItem.ModItem is not OpulentDartgun))
                return;

            int injectDamage = (int)Math.Max(player.statLife * PLAYER_OVERDOSING_DAMAGE_PER_HEALTH, PLAYER_OVERDOSING_MIN_SELF_DAMAGE);

            // Set final damage to the inject damage to bypass any damage reduction
            modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => info.Damage = injectDamage;
        }

        private static bool IgnoreCutsceneImmunity(Player player, PlayerDeathReason damageSource)
        {
            // Self damage from opulent dartgun will ignore cutscene immunity
            return damageSource.SourcePlayerIndex == player.whoAmI && damageSource.SourceItem != null && damageSource.SourceItem.ModItem is OpulentDartgun;
        }

        #endregion

        #region Use Visuals
        private static void RealUseStyle(Player player, OpulentDartgun dartgun)
        {
            // Change player direction based on cursor relative to player
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);

            // Setup item rotation and pos
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;
            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 7f;

            Vector2 itemSize = new Vector2(28, 28);
            Vector2 itemOrigin = new Vector2(0, 0);

            // Setup values for animation
            float rotationOffset = 0;
            Vector2 positionOffset = Vector2.Zero;

            float animProgress = 1f - (player.itemTime / (float)player.itemTimeMax);

            // Unique animation when self injecting
            if (AltUse(player) && dartgun.AltUseType == AltUseTypes.SelfInject)
            {
                if (dartgun.AltUseType == 0)
                    positionOffset += itemRotation.ToRotationVector2() * 3f;

                const float stateOne = 0.4f;
                const float stateTwo = 1f;

                if (animProgress < stateOne)
                {
                    rotationOffset = FablesUtils.PolyOutEasing(Math.Min(animProgress / stateOne, 1f)) * 3.5f;
                    positionOffset += itemRotation.ToRotationVector2() * (6f * FablesUtils.PolyInOutEasing(Math.Min(animProgress / stateOne, 1f), 3f) + 2f);
                    positionOffset += new Vector2(0, 2f) * animProgress / stateOne;
                }
                else if (animProgress < stateTwo)
                {
                    rotationOffset = 3.5f;
                    positionOffset += itemRotation.ToRotationVector2() * 8f;

                    if (animProgress > 0.45f)
                        positionOffset += itemRotation.ToRotationVector2() * -6f * FablesUtils.CircOutEasing((animProgress - 0.45f) / (stateTwo - 0.45f), 2f);
                    positionOffset += new Vector2(0, 2f);
                }
            }
            // Normal animation otherwise
            else
                positionOffset += itemRotation.ToRotationVector2() * 3f * MathF.Pow(Math.Min(animProgress * 1.3f, 1f), 2f);

            FablesUtils.CleanHoldStyle(player, itemRotation + rotationOffset * player.direction, itemPosition + positionOffset, itemSize, itemOrigin);
        }

        public override void UseItemFrame(Player player)
        {
            var dartgun = GetDartgun(player);

            // Setup arm rotation and stretch
            float armRotation = (player.Center - player.MouseWorld()).ToRotation() * player.gravDir + MathHelper.PiOver2;
            Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;

            float animProgress = 1f - (player.itemTime / (float)player.itemTimeMax);

            // Alt use animations
            if (AltUse(player))
            {
                if (dartgun.AltUseType == AltUseTypes.SelfInject)
                {
                    if (animProgress < 0.4f)
                        armRotation = Utils.AngleLerp(armRotation, -1.3f * player.direction, animProgress / 0.4f);
                    else
                        armRotation = -1.3f * player.direction;

                    stretch = (1f - FablesUtils.CircOutEasing((animProgress - 0.45f) / 0.55f, 2f)).ToStretchAmount();
                }
                else if (dartgun.AltUseType == AltUseTypes.Shoot)
                {
                    armRotation += (-0.5f + 0.5f * MathF.Pow(Math.Min(animProgress * 1.5f, 1f), 0.8f)) * player.direction;
                    stretch = (MathF.Pow(Math.Min(animProgress * 1.3f, 1f), 2f) * 0.5f + 0.5f).ToStretchAmount();
                }
            }
            // Normal use animation
            else
            {
                armRotation += (-0.15f + 0.15f * MathF.Pow(Math.Min(animProgress * 2f, 1f), 0.8f)) * player.direction;
                stretch = (MathF.Pow(Math.Min(animProgress * 1.3f, 1f), 2f) * 0.5f + 0.5f).ToStretchAmount();
            }

            // Set arm params
            player.SetCompositeArmFront(true, stretch, armRotation);
        }

        private void EjectCartridge(Player player)
        {
            // Get rotation. Need to add Pi when facing left
            float rotation = player.itemRotation;
            if (player.direction == -1)
                rotation += MathHelper.Pi;

            Vector2 gorePosition = player.MountedCenter + new Vector2(-4, -4 * player.direction).RotatedBy(rotation);
            Vector2 goreVelocity = new Vector2(-3, 0).RotatedBy(rotation) + new Vector2(0, -2);
            int type = Mod.Find<ModGore>("OpulentDartgunCartridgeGore").Type;   // Using the base ModGore with the cartridge texture

            Gore Cartridge = Gore.NewGoreDirect(player.GetSource_ItemUse(Item), gorePosition, goreVelocity, type, Item.scale);
            Cartridge.rotation = rotation;
            Cartridge.position -= new Vector2(Cartridge.Width / 2, Cartridge.Height / 2);
        }
        #endregion

        #region Item drawing
        public void DrawHeld(ref PlayerDrawSet drawInfo, Texture2D texture, Vector2 position, Rectangle frame, float rotation, Color color, float scale, Vector2 origin)
        {
            CartridgeTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "OpulentDartgunCartridge");
            CartridgeOverlayTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "OpulentDartgunCartridgeOverlay");

            // Get time left on cooldown
            int timeLeft = drawInfo.drawPlayer.FindCooldown(OpulentSyringeReload.ID, out var cooldown) ? cooldown.timeLeft : 0;

            if (timeLeft <= 40)
            {
                // Find cartridge position
                float cartridgeOffsetOfTime = FablesUtils.PolyOutEasing(Math.Clamp((timeLeft - 20) / 15f, 0f, 1f), 6f) * 8;
                Vector2 cartridgeOffset = new Vector2(6 + cartridgeOffsetOfTime, -12);
                Vector2 cartridgeOrigin = Vector2.Zero;
                Rectangle cartridgeFrame = new(0, 0, CartridgeTexture.Width(), CartridgeTexture.Height());
                drawInfo.AdjustItemOffsetOrigin(cartridgeFrame, ref cartridgeOffset, ref cartridgeOrigin);
                Vector2 cartridgePosition = position + cartridgeOffset.RotatedBy(rotation) * scale;

                // Draw cartridge itself
                DrawData cartridge = new(CartridgeTexture.Value, cartridgePosition, cartridgeFrame, color, rotation, cartridgeOrigin, scale, drawInfo.itemEffect);
                drawInfo.DrawDataCache.Add(cartridge);

                // Draw overlay if the cooldown is still active
                if (timeLeft > 0)
                {
                    Color overlayColor = Color.LightYellow * Math.Min(1f, timeLeft / 20f);

                    // Draw overlay to make it flash
                    DrawData cartridgeOverlay = new(CartridgeOverlayTexture.Value, cartridgePosition, cartridgeFrame, overlayColor, rotation, cartridgeOrigin, scale, drawInfo.itemEffect);
                    drawInfo.DrawDataCache.Add(cartridgeOverlay);
                }
            }
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // Draw with cartidge in inventory
            CartridgeTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "OpulentDartgunCartridge");

            spriteBatch.Draw(CartridgeTexture.Value, position + new Vector2(-8, -8) * scale, null, drawColor, 0, CartridgeTexture.Size() / 2, scale, SpriteEffects.None, 0);

            return true;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            // Draw with cartridge when on the ground
            CartridgeTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "OpulentDartgunCartridge");

            Main.GetItemDrawFrame(Item.type, out _, out Rectangle itemFrame);
            Vector2 origin = itemFrame.Size() / 2;
            Vector2 position = Item.Bottom - Main.screenPosition - new Vector2(0, origin.Y);

            spriteBatch.Draw(CartridgeTexture.Value, position + new Vector2(-4, 2).RotatedBy(rotation) * scale, null, lightColor, rotation, origin, scale, SpriteEffects.None, 0);

            return true;
        }
        #endregion

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.IllegalGunParts).
                AddIngredient(ItemID.BeeWax, 10).
                AddRecipeGroup(FablesRecipes.AnyGoldBarGroup, 10).
                AddIngredient(ItemID.Marble, 25).
                AddTile(TileID.Anvils).
                Register();
        }
    }

    #region Projectiles
    public class OpulentDartgunBigSyringe : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        internal static Asset<Texture2D> OutlineTexture;

        private PrimitiveTrail Trail;
        private List<Vector2> Cache;
        public const int TrailLength = 32;

        public ref float Timer => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            /*
            WARNINGWARNINGWARNING
            May you attain enlightenment.
            Servers don't sync
            Hostile projectiles
            Even player owned ones
            WARNINGWARNINGWARNING
            */
            Main.projHostile[Type] = false;
            ProjectileID.Sets.PlayerHurtDamageIgnoresDifficultyScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.friendly = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 1200;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation();

            // Experiences gravity and drag after 15 ticks
            if (Timer > 15)
            {
                if (Projectile.velocity.Y < 16)
                    Projectile.velocity.Y += 0.03f;

                if (Projectile.velocity.Length() > 8f)
                    Projectile.velocity *= 0.98f;
            }

            // Random dust
            if (Main.rand.NextBool(6))
            {
                Dust sparkles = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SpelunkerGlowstickSparkle);
                sparkles.velocity = Projectile.velocity * 0.4f;
            }

            ManageTrail();

            Timer++;
        }

        public override bool CanHitPlayer(Player target) => target.whoAmI != Projectile.owner;

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            Projectile sourceProjectile = Main.projectile[modifiers.DamageSource.SourceProjectileLocalIndex];
            InjectPlayer(target, sourceProjectile);

            // Cancel the first hit to prevent 2
            modifiers.Cancel();

            // Use a packet to trigger visuals and kill the projectile, otherwise it just goes straight through players
            new BigSyringePlayerHitPacket(Projectile).Send();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<OpulentInjectionBuff>(), OTHER_OVERDOSE_BUFF_TIME);

            // Spawn particle with hitTarget
            ParticleHandler.SpawnParticle(new OpulentDartgunSyringeParticle(this, true));
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Set velocity to old velocity so the particle it spawns doesnt go wacky
            Projectile.velocity = oldVelocity;

            // Spawn particle without hitTarget
            ParticleHandler.SpawnParticle(new OpulentDartgunSyringeParticle(this));

            return true;
        }

        public override void OnKill(int timeLeft)
        {
            // Spawn lingering trail if cache has been initialized
            if (Cache is null && Main.dedServ)
                return;

            GhostTrail clone = new (Cache, Trail, 0.3f, null, "Primitive_GlowingCoreWithOverlaidNoise", (effect, fading) => SetEffectParams(effect))
            {
                ShrinkTrailLenght = true,
                DrawLayer = DrawhookLayer.BehindTiles
            };
            GhostTrailsHandler.LogNewTrail(clone);
        }

        #region Prims
        private void ManageTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 position = Projectile.Center + Projectile.velocity;

            // Initialize cache and fill points
            Cache ??= [.. Enumerable.Repeat(position, TrailLength)];

            // Maintain cache
            Cache.Add(position);
            while (Cache.Count > TrailLength) 
                Cache.RemoveAt(0);

            // Initialize trail and set points
            Trail ??= new PrimitiveTrail(TrailLength, WidthFunction, ColorFunction);
            Trail.SetPositionsSmart(Cache, position);
        }

        private static float WidthFunction(float progress) => 4f + 4f * progress;
        private static Color ColorFunction(float progress) => Color.Lerp(Color.Gold, Color.OrangeRed * 1.5f, progress) * progress;

        public static void SetEffectParams(Effect effect)
        {
            effect.Parameters["scroll"].SetValue(-Main.GlobalTimeWrappedHourly * 0.7f * 2.5f);
            effect.Parameters["overlayScroll"].SetValue(-Main.GlobalTimeWrappedHourly * 0.7f);

            effect.Parameters["repeats"].SetValue(2f);
            effect.Parameters["overlayRepeats"].SetValue(2f * 0.2f);
            effect.Parameters["coreShrink"].SetValue(0.5f);
            effect.Parameters["coreOpacity"].SetValue(0.5f);
            effect.Parameters["overlayVerticalScale"].SetValue(0.5f);
            effect.Parameters["overlayMaxOpacityOverlap"].SetValue(2f);

            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            effect.Parameters["overlayNoise"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "FireTrail").Value);
        }
        #endregion

        #region Drawing
        public override bool PreDraw(ref Color lightColor)
        {
            OutlineTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + Name + "Outline");
            Texture2D tex = TextureAssets.Projectile[Type].Value;

            // Draw base texture
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2, Projectile.scale, SpriteEffects.None);

            // Draw two layer outline
            Main.EntitySpriteDraw(OutlineTexture.Value, Projectile.Center - Main.screenPosition, null, Color.Gold with { A = 0 } * 0.5f, Projectile.rotation, OutlineTexture.Size() / 2, Projectile.scale - 0.05f, SpriteEffects.None);
            Main.EntitySpriteDraw(OutlineTexture.Value, Projectile.Center - Main.screenPosition, null, Color.OrangeRed with { A = 0 } * 0.3f, Projectile.rotation, OutlineTexture.Size() / 2, Projectile.scale, SpriteEffects.None);

            return false;
        }

        public override bool PreDrawExtras()
        {
            // Draw Trail
            Effect effect = AssetDirectory.PrimShaders.GlowingCoreWithOverlaidNoise;
            SetEffectParams(effect);

            Trail?.Render(effect, -Main.screenPosition);

            return false;
        }
        #endregion
    }

    [Serializable]
    public class BigSyringePlayerHitPacket(Projectile projectile) : Module
    {
        public byte WhoAmI = (byte)Main.myPlayer;
        public byte Identity = (byte)projectile.identity;

        protected override void Receive()
        {
            if (Main.netMode == NetmodeID.Server)
                Send(-1, WhoAmI, false);
            else
                foreach (Projectile projectile in Main.ActiveProjectiles)
                    if (projectile.identity == Identity && projectile.ModProjectile is OpulentDartgunBigSyringe bigSyringe)
                    {
                        // Spawn particle with hitTarget
                        ParticleHandler.SpawnParticle(new OpulentDartgunSyringeParticle(bigSyringe, true));
                        projectile.Kill();
                    }
        }
    }

    public class EntitySource_OpulentDartgun(EntitySource_ItemUse_WithAmmo referenceSource, bool overdoseEmpowered) : EntitySource_ItemUse_WithAmmo(referenceSource.Player, referenceSource.Item, referenceSource.AmmoItemIdUsed, referenceSource.Context)
    {
        public bool OverdoseEmpowered { get; } = overdoseEmpowered;
    }

    public class OpulentDartProjectileHandling
    {
        public static void Load()
        {
            FablesProjectile.OnSpawnEvent += ConvertSpawnedProjectiles;
            FablesProjectile.AIEvent += AI;
            FablesProjectile.PreDrawEvent += PreDraw;
            FablesProjectile.PreDrawExtrasEvent += PreDrawExtras;
            FablesProjectile.OnKillEvent += OnKill;
            FablesProjectile.ModifyHitPlayerEvent += ModifyHitPlayer;
            FablesProjectile.ModifyHitNPCEvent += ModifyHitNPC;

            // We can register nulls here because the only thing that matters here is that the data itself is spread, no extra data needs to be written
            // This only works because OverdoseDamageBoostProjectileData has a parameterless constructor!
            FablesProjectile.RegisterSyncedData(typeof(OverdoseDamageBoostProjectileData), null, null);
            FablesProjectile.RegisterSyncedData(typeof(OpulentDartProjectileData), SendOpulentDartProjectileData, RecieveOpulentDartProjectileData);
        }

        #region Opulent Dart Data
        public class OpulentDartProjectileData : CustomGlobalData
        {
            public bool OverdoseBoosted;
            public float OnHitEffectMultiplier = 1f;

            public PrimitiveTrail Trail;
            public List<Vector2> Cache;

            public OpulentDartProjectileData() { }  // Empty constructor so it gets auto-instantiated during net sync

            public OpulentDartProjectileData(bool overdoseBoosted) => OverdoseBoosted = overdoseBoosted;
        }

        public class OverdoseDamageBoostProjectileData : CustomGlobalData { }   // Empty custom data that's just like, a propagating tag

        public static void SendOpulentDartProjectileData(CustomGlobalData data, Projectile proj, BitWriter bitWriter, BinaryWriter writer)
        {
            if (data is not OpulentDartProjectileData dartData)
                return;

            writer.Write(dartData.OverdoseBoosted);
            writer.Write(dartData.OnHitEffectMultiplier);
        }

        public static void RecieveOpulentDartProjectileData(CustomGlobalData data, Projectile proj, BitReader bitReader, BinaryReader reader)
        {
            if (data is not OpulentDartProjectileData dartData)
                return;

            dartData.OverdoseBoosted = reader.ReadBoolean();
            dartData.OnHitEffectMultiplier = reader.ReadSingle();
        }

        #endregion

        internal static Asset<Texture2D> OpulentDartsTexture;
        internal static Asset<Texture2D> OpulentDartsOutlineTexture;

        public static void ConvertSpawnedProjectiles(Projectile projectile, IEntitySource source)
        {
            // Use our custom projectile source to convert any shot darts into opulent darts
            if (source is EntitySource_OpulentDartgun dartgunSource)
                projectile.SetProjectileData(new OpulentDartProjectileData(dartgunSource.OverdoseEmpowered) { needsSyncing_forProjectiles = true });

            // Inheritance of damage boost between parent NPC/Projectiles and spawned projectiles
            else if (source is EntitySource_Parent parentSource)
            {
                if (parentSource.Entity is NPC npc && npc.GetNPCFlag("OpulentInjectionBuff"))
                    projectile.SetProjectileData(new OverdoseDamageBoostProjectileData() { needsSyncing_forProjectiles = true });
            }
        }

        public static void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
        {
            // Boost damage of projectile with tag
            if (projectile.GetProjectileData(out OverdoseDamageBoostProjectileData _))
                modifiers.SourceDamage *= NPC_OVERDOSING_DAMAGE_MULTIPLIER;
        }

        public static void ModifyHitNPC(Projectile projectile, NPC npc, ref NPC.HitModifiers modifiers)
        {
            // Boost damage of projectile with tag
            if (projectile.GetProjectileData(out OverdoseDamageBoostProjectileData _))
                modifiers.SourceDamage *= NPC_OVERDOSING_DAMAGE_MULTIPLIER;
        }

        private static void AI(Projectile projectile)
        {
            if (!projectile.GetProjectileData(out OpulentDartProjectileData data))
                return;

            // Glitterald
            if (data.OverdoseBoosted && Main.rand.NextBool(8))
            {
                Dust sparkles = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.SpelunkerGlowstickSparkle);
                sparkles.velocity = projectile.velocity * 0.4f;
            }

            ManageTrail(projectile, data);
        }

        public static void OnKill(Projectile projectile, int timeLeft)
        {
            // Spawn lingering trail if cache has been initialized
            if (Main.dedServ || !projectile.GetProjectileData(out OpulentDartProjectileData data) || data.Cache is null)
                return;

            GhostTrail clone = new(data.Cache, data.Trail, 0.2f, null, "Primitive_GlowingCoreWithOverlaidNoise", (effect, fading) => OpulentDartgunBigSyringe.SetEffectParams(effect))
            {
                ShrinkTrailLenght = true,
                ShrinkTrailWidth = true,
                DrawLayer = DrawhookLayer.BehindTiles
            };
            GhostTrailsHandler.LogNewTrail(clone);
        }

        #region Prims
        private static void ManageTrail(Projectile projectile, OpulentDartProjectileData data)
        {
            if (Main.dedServ)
                return;

            int trailLength = data.OverdoseBoosted ? 14 : 10;
            Vector2 position = projectile.Center + projectile.velocity;

            // Initialize cache and fill points
            data.Cache ??= [.. Enumerable.Repeat(position, trailLength)];

            // Maintain cache
            data.Cache.Add(position);
            while (data.Cache.Count > trailLength)
                data.Cache.RemoveAt(0);

            TrailWidthFunction widthFunction = data.OverdoseBoosted ? OverdoseWidthFunction : WidthFunction;
            TrailColorFunction colorFunction = data.OverdoseBoosted ? OverdoseColorFunction : ColorFunction;

            // Initialize trail and set points
            data.Trail ??= new PrimitiveTrail(trailLength, widthFunction, colorFunction);
            data.Trail.SetPositionsSmart(data.Cache, position);
        }

        private static float WidthFunction(float progress) => 3f + progress;
        private static float OverdoseWidthFunction(float progress) => 3f + 2f * progress;
        private static Color ColorFunction(float progress) => Color.Lerp(Color.OrangeRed, Color.Gold, progress - 0.2f) * progress * 0.3f;
        private static Color OverdoseColorFunction(float progress) => Color.Lerp(Color.OrangeRed, Color.Gold, progress - 0.3f) * progress;

        #endregion

        #region Drawing
        public static bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (!projectile.GetProjectileData(out OpulentDartProjectileData data))
                return true;

            // Get frame number. Dont continue if this projectile doesnt have an opulent dart texture
            if (!GetDartFrame(projectile.type, out int frameNum, out int totalFrames))
                return true;

            OpulentDartsTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "OpulentDartgunDarts");
            OpulentDartsOutlineTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "OpulentDartgunDartsOutline");

            Vector2 position = projectile.Center - Main.screenPosition;
            float rotation = projectile.rotation - MathHelper.PiOver2;

            // Get frame and origin account for padding
            Rectangle dartFrame = OpulentDartsTexture.Frame(1, totalFrames, 0, frameNum);
            Vector2 dartOrigin = dartFrame.Size() / 2;
            dartOrigin.Y -= 1;

            // Draw base texture
            Main.EntitySpriteDraw(OpulentDartsTexture.Value, position, dartFrame, lightColor * projectile.Opacity, rotation, dartOrigin, projectile.scale, SpriteEffects.None);

            // Add an outline if overdosed
            if (data.OverdoseBoosted)
            {
                // Get frame and origin account for padding
                Rectangle outlineFrame = OpulentDartsOutlineTexture.Frame(1, totalFrames, 0, frameNum);
                Vector2 outlineOrigin = outlineFrame.Size() / 2;
                outlineOrigin.Y -= 1;

                // Draw two layer outline
                Main.EntitySpriteDraw(OpulentDartsOutlineTexture.Value, position, outlineFrame, Color.Gold with { A = 0 } * 0.4f * projectile.Opacity, rotation, outlineOrigin, projectile.scale * 0.95f, SpriteEffects.None);
                Main.EntitySpriteDraw(OpulentDartsOutlineTexture.Value, position, outlineFrame, Color.OrangeRed with { A = 0 } * 0.3f * projectile.Opacity, rotation, outlineOrigin, projectile.scale, SpriteEffects.None);
            }

            return false;
        }

        public static bool PreDrawExtras(Projectile projectile)
        {
            if (!projectile.GetProjectileData(out OpulentDartProjectileData data))
                return true;

            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

            Vector2 position = projectile.Center - Main.screenPosition;
            Vector2 bloomScale = new(0.4f, 0.15f);

            // Draw 2 layers of bloom on the projectile
            Main.EntitySpriteDraw(bloom, position, null, Color.Orange with { A = 0 } * 0.2f, projectile.rotation + MathHelper.PiOver2, bloom.Size() / 2, bloomScale, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, position, null, Color.White with { A = 0 } * 0.2f, projectile.rotation + MathHelper.PiOver2, bloom.Size() / 2, bloomScale * 0.7f, SpriteEffects.None);

            // Draw Trail
            Effect effect = AssetDirectory.PrimShaders.GlowingCoreWithOverlaidNoise;
            OpulentDartgunBigSyringe.SetEffectParams(effect);

            data.Trail?.Render(effect, -Main.screenPosition);

            return false;
        }

        private static bool GetDartFrame(int id, out int frameNum, out int totalFrames)
        {
            totalFrames = 8;    // Update this whenever adding new frames for new darts

            // Peemo this cant use a switch
            if (id == ModContent.ProjectileType<WoodenDartProj>())
                frameNum = 0;
            else if (id == ProjectileID.PoisonDartBlowgun)
                frameNum = 1;
            else if (id == ModContent.ProjectileType<BouncyDartProj>())
                frameNum = 2;
            else if (id == ModContent.ProjectileType<SuperchargedDartProjectile>())
                frameNum = 3;
            else if (id == ModContent.ProjectileType<SkyDartProjectile>())
                frameNum = 4;
            else if (id == ProjectileID.CrystalDart)
                frameNum = 5;
            else if (id == ProjectileID.CursedDart)
                frameNum = 6;
            else if (id == ProjectileID.IchorDart)
                frameNum = 7;
            else
            {
                frameNum = -1;
                return false;
            }

            return true;
        }

        #endregion
    }

    #endregion

    public class OpulentInjectionBuff : ModBuff, ICustomDeathMessages
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        #region Overdose Data

        [Serializable]
        public class OpulentInjectionPlayerData : CustomGlobalData
        {
            public int Timer = 0;
            public int SlowingCounter = 0;
            public float BuffExtensionEffectiveness = 1f;
            public bool HealthDrain = true;

            /// <summary>
            /// Tracks the time and effectiveness of the heals caused by dartgun darts.
            /// Used to determine how effective the next heals should be by dividing the heal budget by hits per second.
            /// </summary>
            public Dictionary<int, float> RecentHeals = [];

            [NonSerialized]
            public SlotId TrippingLoopSlot;

            [NonSerialized]
            public VortexSwirlRenderTarget VortexTarget;
            public int SwirldSpawnTimer;

            [NonSerialized]
            public PrimitiveQuadrilateral GlowQuad;
        }

        [Serializable]
        public class SyncOverdoseDataModule(Player player, OpulentInjectionPlayerData data) : FablesPlayer.SyncPlayerMiscData<OpulentInjectionPlayerData>(player, data);

        #endregion

        public float DoTDeathMessagePriority => 1;

        public override void Load()
        {
            // Player stuff
            FablesPlayer.PostUpdateBuffsEvent += CheckForBuffRemoval;
            FablesPlayer.OnHitNPCWithProjEvent += OnHitNPCWithProj;
            FablesPlayer.UpdateBadLifeRegenEvent += UpdateBadLifeRegen;
            FablesPlayer.ModifyNurseHealEvent += ModifyNurseHeal;

            // NPC stuff
            FablesNPC.ModifyHitPlayerEvent += ModifyHitPlayer;
            FablesNPC.ModifyHitNPCEvent += ModifyHitNPC;
            FablesNPC.UpdateLifeRegenEvent += UpdateLifeRegen;

            // Player drawing
            FablesPlayer.PostUpdateEvent += UpdateScreenshaderAndSwirls;
            FablesDrawLayers.PreDrawPlayersEvent += DrawEffectsBack;
            FablesDrawLayers.DrawThingsAbovePlayersEvent += DrawEffectsFront;
        }

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        #region Player Effects
        public bool CustomDeathMessage(Player player, ref PlayerDeathReason deathMessage)
        {
            deathMessage.CustomReason = NetworkText.FromKey("Mods.CalamityFables.Extras.DeathMessages.OpulentOverdose." + Main.rand.Next(1, 4), player.name);
            return true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.SetPlayerFlag(Name);

            if (!player.GetPlayerData(out OpulentInjectionPlayerData data))
            {
                data = new OpulentInjectionPlayerData();
                player.SetPlayerData(data);
            }

            ManageSounds(player, data);

            // Update damage bonuses
            player.GetAttackSpeed(DamageClass.Generic) += PLAYER_OVERDOSING_ATTACK_SPEED_BONUS;
            player.GetDamage(DamageClass.Generic) += PLAYER_OVERDOSING_DAMAGE_BONUS;

            // Set healing sickness to the duration of the buff
            player.potionDelay = Math.Max(player.buffTime[buffIndex], player.potionDelay);

            // Remove old entries in recent heals
            data.RecentHeals.RemoveAll((time, value) => data.Timer - time >= 240);

            // Update slowness based on progress through slowing counter
            float slowingFactor = Utils.GetLerpValue(MIN_SLOWING_COUNTER, MAX_SLOWING_COUNTER, data.SlowingCounter, true);
            player.Fables().MoveSpeedModifier *= MathHelper.Lerp(1f, 1f - MAX_MOVE_SPEED_PENALTY, slowingFactor);

            // Update counters
            data.Timer++;
            if (data.SlowingCounter < MAX_SLOWING_COUNTER)
                data.SlowingCounter++;

            // Visual streaks
            if (Main.rand.NextBool(16))
            {
                float halfWidth = player.width / 2 + 12;
                float hOffset = Main.rand.NextFloat(-halfWidth, halfWidth);
                Color effectColor = Color.Lerp(Color.OrangeRed * 0.7f, Color.Gold, 1f - ((float)Math.Abs(hOffset) / halfWidth));
                Vector2 effectPos = new Vector2(player.MountedCenter.X + hOffset, player.Top.Y + Main.rand.NextFloat(16, player.height));
                Particle p = new PixelStreaks(effectPos, effectColor, Color.OrangeRed * 0.8f, 1f);
                ParticleHandler.SpawnParticle(p);
            }
        }

        private void CheckForBuffRemoval(Player player)
        {
            // Reset Effects
            if (!player.GetPlayerFlag("OpulentInjectionBuff") && player.GetPlayerData(out OpulentInjectionPlayerData data) && data.Timer > 0)
            {
                data.Timer = 0;
                data.RecentHeals.Clear();
                data.BuffExtensionEffectiveness = 1f;
                data.SlowingCounter = MIN_SLOWING_COUNTER;
                data.HealthDrain = true;
            }
        }

        public static void OnHitNPCWithProj(Player player, Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Ignore projectiles that aren't overdose boosted darts (or ones that have outlived their utility
            if (!proj.GetProjectileData(out OpulentDartProjectileData dartData) || !dartData.OverdoseBoosted || dartData.OnHitEffectMultiplier <= 0.2f || !target.CanBeChasedBy(proj))
                return;

            // Nothing to do if the player's not overdosing
            if (!player.GetPlayerData(out OpulentInjectionPlayerData playerData) || !player.GetPlayerFlag("OpulentInjectionBuff"))
            {
                MultihitPenalty();
                return;
            }

            // Calculate a factor based on previous heals
            // Aims to maintain an average healing rate around the healing budget
            float sum = playerData.RecentHeals.Values.Sum();
            float timeSpan = Math.Min(playerData.Timer, 240) / 60f;
            float healFactor = Math.Min(timeSpan / sum, 1f);

            // Reduce healing if damage was reduced by armor or damage reduction
            int baseDamage = hit.SourceDamage * (hit.Crit ? 2 : 1);
            healFactor *= Utils.GetLerpValue(0, baseDamage, damageDone, true);

            // Heal the player
            int healAmount = (int)(PLAYER_OVERDOSING_HEALING_BUDGET * healFactor * dartData.OnHitEffectMultiplier);
            if (healAmount > 0)
                player.Heal(healAmount);

            // Add heal instance to dict
            // If an entry on this tick already exists, simply add the current onHitEffectMult to that entry
            if (!playerData.RecentHeals.TryAdd(playerData.Timer, dartData.OnHitEffectMultiplier))
                playerData.RecentHeals[playerData.Timer] += dartData.OnHitEffectMultiplier;

            // Unslow player
            playerData.SlowingCounter = Math.Max(playerData.SlowingCounter - (int)(30 * dartData.OnHitEffectMultiplier), 0);

            // Extend buff duration on first hit
            if (Main.rand.NextBool(4) && dartData.OnHitEffectMultiplier > 0.9f)
            {
                int injectionBuffIndex = player.buffType.ToList().FindIndex(id => id == ModContent.BuffType<OpulentInjectionBuff>());
                if (injectionBuffIndex != -1)
                {
                    player.buffTime[injectionBuffIndex] += (int)(90 * playerData.BuffExtensionEffectiveness);
                    playerData.BuffExtensionEffectiveness *= 0.9f; // Prevent the buff from being extended indefinitely
                }
            }

            MultihitPenalty();
            player.SyncPlayerData<OpulentInjectionPlayerData, SyncOverdoseDataModule>();

            // Multihit darts lose effectiveness so they dont become goated
            void MultihitPenalty()
            {
                dartData.OnHitEffectMultiplier *= 0.3f;
                dartData.needsSyncing_forProjectiles = true;
                proj.netUpdate = true;
            }
        }

        public static void UpdateBadLifeRegen(Player player)
        {
            if (!player.GetPlayerData(out OpulentInjectionPlayerData data) || !player.GetPlayerFlag("OpulentInjectionBuff") || !data.HealthDrain)
                return;

            player.lifeRegenTime = 0;
            player.lifeRegen = Math.Min(player.lifeRegen, 0);
            player.lifeRegen -= PLAYER_OVERDOSING_DOT * 2;
        }

        public static bool ModifyNurseHeal(Player player, NPC nurse, ref int health, ref bool removeDebuffs, ref string chatText)
        {
            if (!player.GetPlayerData<OpulentInjectionPlayerData>(out _) || !player.GetPlayerFlag("OpulentInjectionBuff"))
                return true;

            chatText = Language.GetTextValue("Mods.CalamityFables.Extras.OpulentDartgunNurseDialogue." + Main.rand.Next(1, 4));
            return false;
        }

        private static void ManageSounds(Player player, OpulentInjectionPlayerData data)
        {
            if (player != Main.LocalPlayer)
                return;

            // Tripping Loop
            if (!SoundEngine.TryGetActiveSound(data.TrippingLoopSlot, out var sound))
                data.TrippingLoopSlot = SoundEngine.PlaySound(SyringeTrippingLoop);
            if (SoundEngine.TryGetActiveSound(data.TrippingLoopSlot, out sound))
                sound.Update();

            SoundHandler.TrackSound(data.TrippingLoopSlot);
        }

        #region Visual effects
        private static void UpdateScreenshaderAndSwirls(Player player)
        {
            if (Main.dedServ || !player.GetPlayerData(out OpulentInjectionPlayerData data))
                return;

            // Screen shader is client only
            if (player.whoAmI == Main.myPlayer)
            {
                const string shaderKey = "OpulentInjectionScreenShader";
                bool effectActive = Scene[shaderKey].IsActive();

                Effect shader = Scene[shaderKey].GetShader().Shader;

                // Activate and flash when the buff is active
                if (player.GetPlayerFlag("OpulentInjectionBuff") && !player.dead)
                {
                    if (!effectActive)
                        Scene.Activate(shaderKey).GetShader()
                            .UseImage(AssetDirectory.NoiseTextures.CracksDisplace2)
                            .UseColor(Color.Goldenrod);

                    // Flash when the buff is applied
                    Scene["OpulentInjectionScreenShader"].Opacity = 1f;
                    shader.Parameters["vignetteBrightness"].SetValue(MathF.Pow(Utils.GetLerpValue(40, 0, data.Timer, true), 2f));
                }
                // Deactivate the effect when the buff is inactive
                else if (effectActive)
                    Scene.Deactivate(shaderKey);
            }

            // Initialize vortex render target if it hasn't been already
            ref var vortexTarget = ref data.VortexTarget;
            if (vortexTarget is null && player.GetPlayerFlag("OpulentInjectionBuff"))
                vortexTarget = new VortexSwirlRenderTarget(new(300, 200), new(75f, 190f), 2f);

            // Stop here if the target is null
            if (vortexTarget is null)
                return;

            // Constantly reset vortex position
            vortexTarget.Position = player.MountedCenter + new Vector2(0, 12) + Vector2.UnitY * player.gfxOffY;

            // Spawn swirls while the buff is active
            if (player.GetPlayerFlag("OpulentInjectionBuff") && !player.dead)
            {
                // Spawn effects
                if (data.Timer == 1)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 position = new Vector2(i * 0.5f, 4f);
                        Vector2 trajectory = new Vector2(1.6f, -10f);

                        RTParticle swirl = CreateVortexSwirl(position, trajectory, Color.LightYellow, Color.Gold * 0.2f, 16f, 16f, 36, 1f, 2f);
                        vortexTarget.SpawnParticle(swirl);
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 position = new Vector2(i * 0.5f, 5f);
                        Vector2 trajectory = new Vector2(1.2f, -24f);

                        RTParticle swirl = CreateVortexSwirl(position, trajectory, Color.LightYellow, Color.Gold * 0.5f, 8f, 16f, 48, 1f, 2f);
                        vortexTarget.SpawnParticle(swirl);
                    }
                }

                // Continually spawn vortex particles
                if (data.SwirldSpawnTimer <= 0)
                {
                    Vector2 position = new Vector2(Main.rand.NextFloat(), Main.rand.NextFloat(4f));
                    Vector2 trajectory = new Vector2(Main.rand.NextFloat(1.8f, 2f) / 2, -10f);

                    RTParticle swirl = CreateVortexSwirl(position, trajectory, Color.Goldenrod with { A = 180 }, Color.Orange * 0.3f, Main.rand.NextFloat(4f, 7f), 8f, 36);
                    vortexTarget.SpawnParticle(swirl);

                    data.SwirldSpawnTimer = Main.rand.Next(8, 12);
                }
                data.SwirldSpawnTimer--;

                // Keep opacity at 1
                vortexTarget.Opacity = 1f;
            }
            // Fade out
            else
                vortexTarget.Opacity = Math.Max(data.VortexTarget.Opacity -= 0.06f, 0);

            // Clear effects when opacity reaches 0
            if (vortexTarget.Opacity <= 0 && data.VortexTarget.Initialized)
                vortexTarget.ClearAndDisposeRenderTarget();
        }

        private static VortexSwirlParticle CreateVortexSwirl(Vector2 position, Vector2 trajectory, Color frontColor, Color backColor, float startRadius, float radiusChange, int lifetime = 60, float swirlWidth = 1f, float swirlVariance = 1f)
        {
            return new VortexSwirlParticle(position, trajectory, frontColor, backColor, startRadius, radiusChange, lifetime, swirlWidth, swirlVariance)
            {
                StripDefinition = 8,
                RadiusProgressPower = 1,
                FlatSineBump = Vector2.UnitY * 2f,
                RadiusSineBump = 2f
            };
        }

        private static void DrawEffectsBack(bool afterProjectiles) => DrawOverdoseVortexSwirls(afterProjectiles, false);
        private static void DrawEffectsFront(bool afterProjectiles)
        {
            DrawOverdoseGlow(afterProjectiles);
            DrawOverdoseVortexSwirls(afterProjectiles, true);
        }

        private static void DrawOverdoseVortexSwirls(bool afterProjectiles, bool front)
        {
            if (!afterProjectiles)
                return;

            List<ParticleRenderTarget> targetsToDraw = [];

            // Check each player for overdosing player data and retrieve the vortex target
            foreach (Player player in Main.ActivePlayers)
            {
                if (!player.GetPlayerData(out OpulentInjectionPlayerData data) || data.VortexTarget is null || !data.VortexTarget.Initialized)
                    continue;

                targetsToDraw.Add(data.VortexTarget);
            }
            
            // Stop if there are no targets to draw
            if (targetsToDraw.Count <= 0)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // Draw batched RTs
            foreach (var vortexTarget in targetsToDraw)
            {
                // Cropped source rectangle depending on layer. The back layer is drawn next to the front layer on the same RT
                Point size = vortexTarget.Size;
                Rectangle source = new Rectangle(front ? 0 : size.X / 2, 0, size.X / 2, size.Y);

                vortexTarget.DrawRenderTarget(Main.spriteBatch, 0, source);
            }

            Main.spriteBatch.End();
        }

        private static void DrawOverdoseGlow(bool afterProjectiles)
        {
            if (!afterProjectiles)
                return;

            Dictionary<Player, OpulentInjectionPlayerData> playersToDraw = [];

            // Check for player with the overdosing data and get the render target opacity
            // The opacity is used to fade out this effect, so its how we decide to draw it or not
            foreach (Player player in Main.ActivePlayers)
            {
                if (!player.GetPlayerData(out OpulentInjectionPlayerData data) || data.VortexTarget is null || data.VortexTarget.Opacity <= 0f || player.dead)
                    continue;

                playersToDraw.Add(player, data);
            }

            // Stop if there are no targets to draw
            if (playersToDraw.Count <= 0)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (var entry in playersToDraw)
            {
                Player player = entry.Key;
                var data = entry.Value;

                Effect glowEffect = Scene["OpulentInjectionGlow"].GetShader().Shader;

                // Flash at the start of buff time
                float vfxFlash = player.GetPlayerFlag("OpulentInjectionBuff") ? Utils.GetLerpValue(40, 0, data.Timer, true) : 0f;
                Vector2 size = new Vector2(0.10f + 0.03f * FablesUtils.PolyOutEasing(vfxFlash, 0.5f), 0.07f) * 512f;    // Shader resolution and prim quad size

                // Passive effects
                float opacity = data.VortexTarget.Opacity;  // Using RT opacity as a shorcut for a fade effect
                float time = vfxFlash > 0 ? 0f : 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.5f) + 0.5f;  // Always zero while the VFX flash is happening

                glowEffect.Parameters["Color"].SetValue(Color.Lerp(Color.Gold, Color.White, vfxFlash));
                glowEffect.Parameters["Resolution"].SetValue(size * 0.5f);

                // Pulse the shader a bit and fade out at the end
                glowEffect.Parameters["Intensity"].SetValue(opacity);
                glowEffect.Parameters["FadePower"].SetValue(1f + 1f * time);
                glowEffect.Parameters["CenterFadePower"].SetValue(1f + 1.5f * time);

                // Render the shader to a primitive quad
                data.GlowQuad ??= new PrimitiveQuadrilateral();
                Vector2 quadPosition = player.MountedCenter + new Vector2(0, 4) + Vector2.UnitY * player.gfxOffY;
                data.GlowQuad.SetPositions(quadPosition.Floor() - Main.screenPosition, size.X, size.Y, 0);

                data.GlowQuad.Render(glowEffect, -Main.screenPosition);
            }

            Main.spriteBatch.End();
        }

        #endregion

        #endregion

        #region NPC effects
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.SetNPCFlag(Name);

            // Visual streaks
            if (Main.rand.NextBool(12))
            {
                float halfWidth = npc.width / 2 + 12;
                float hOffset = Main.rand.NextFloat(-halfWidth, halfWidth);
                Color effectColor = Color.Lerp(Color.OrangeRed * 0.7f, Color.Gold, 1f - ((float)Math.Abs(hOffset) / halfWidth));
                Vector2 effectPos = new Vector2(npc.Center.X + hOffset, npc.Top.Y + Main.rand.NextFloat(16, npc.height));
                Particle p = new PixelStreaks(effectPos, effectColor, Color.OrangeRed * 0.8f, 1f);
                ParticleHandler.SpawnParticle(p);
            }
        }

        public static void ModifyHitNPC(NPC npc, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (npc.GetNPCFlag("OpulentInjectionBuff"))
                modifiers.SourceDamage *= NPC_OVERDOSING_DAMAGE_MULTIPLIER;
        }

        public static void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (npc.GetNPCFlag("OpulentInjectionBuff"))
                modifiers.SourceDamage *= NPC_OVERDOSING_DAMAGE_MULTIPLIER;
        }

        private void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (!npc.GetNPCFlag("OpulentInjectionBuff"))
                return;

            npc.lifeRegen = Math.Min(npc.lifeRegen, 0);
            npc.lifeRegen -= NPC_OVERDOSING_DOT * 2;
            damage = 5;
        }

        #endregion
    }

    public class OpulentDartgunSyringeParticle : Particle
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "OpulentDartgunBigSyringe";

        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        private readonly bool HitTarget;
        private bool DoSpawnStuff = true;

        public OpulentDartgunSyringeParticle(OpulentDartgunBigSyringe projectile, bool hitTarget = false)
        {
            HitTarget = hitTarget;
            Position = projectile.Projectile.Center;
            Scale = projectile.Projectile.scale;
            Color = Color.White;
            Velocity = projectile.Projectile.velocity;
            Rotation = projectile.Projectile.rotation;
            Lifetime = 30;

            if (hitTarget)
                Velocity *= 0.1f;
            else
            {
                // Go wacky when not hitting target
                Velocity.X *= -Main.rand.NextFloat(0.1f, 0.5f);
                Velocity.Y = -Main.rand.NextFloat(4f, 7f);
            }
        }

        public override void Update()
        {
            if (HitTarget)
            {
                // Circle shine on spawn if a target was hit
                if (DoSpawnStuff)
                {
                    ParticleHandler.SpawnParticle(new CircularPulseShine(Position, Color.Gold, 0.7f));
                    DoSpawnStuff = false;
                }
                Velocity *= 0.93f;
            }
            else
            {
                // Spin and bounce back
                Velocity *= 0.95f;
                Rotation += Velocity.X * 0.05f;
                Velocity.Y += 0.3f;
            }

            Scale = 1f - LifetimeCompletion * 0.5f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D texture = ParticleTexture;
            OpulentDartgunBigSyringe.OutlineTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "OpulentDartgunBigSyringeOutline");

            float opacity = 1 - MathF.Pow(LifetimeCompletion, 2f);

            // Draw base texture
            spriteBatch.Draw(texture, Position - basePosition, null, Color * opacity, Rotation, texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);

            // Draw two layer outline
            Main.EntitySpriteDraw(OpulentDartgunBigSyringe.OutlineTexture.Value, Position - basePosition, null, Color.Gold with { A = 0 } * 0.5f * Math.Max(opacity * 2f - 1f, 0f), Rotation, OpulentDartgunBigSyringe.OutlineTexture.Size() / 2, Scale - 0.05f, SpriteEffects.None);
            Main.EntitySpriteDraw(OpulentDartgunBigSyringe.OutlineTexture.Value, Position - basePosition, null, Color.OrangeRed with { A = 0 } * 0.3f * Math.Max(opacity * 2f - 1f, 0f), Rotation, OpulentDartgunBigSyringe.OutlineTexture.Size() / 2, Scale, SpriteEffects.None);
        }
    }
}