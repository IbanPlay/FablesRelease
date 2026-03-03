using CalamityFables.Core.DrawLayers;
using CalamityFables.Particles;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using static CalamityFables.Content.Items.EarlyGameMisc.InbuedDartProjectileHandling.ImbuedDartProjectileData;
using static CalamityFables.Content.Items.EarlyGameMisc.ToxicBlowpipe;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class ToxicBlowpipe : ModItem, ICustomHeldDraw
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        internal static Asset<Texture2D> Held;
        internal static Asset<Texture2D> TrailSmoke;
        internal static Asset<Texture2D> BigSmoke;
        internal static Asset<Texture2D> BigBlendedSmoke;
        internal static Asset<Texture2D> ToxicSkull;

        public static readonly SoundStyle NeurotoxinFireSound = new("CalamityFables/Sounds/ToxicBlowpipeFire") { PitchVariance = 0.2f, MaxInstances = 0, Volume = 0.6f };
        public static readonly SoundStyle AcidFireSound = new("CalamityFables/Sounds/ToxicBlowpipeFireAlt") { PitchVariance = 0.2f, MaxInstances = 0, Volume = 0.6f };

        public static readonly SoundStyle ExplosionSound = new("CalamityFables/Sounds/BlowpipeExplosion") { PitchVariance = 0.1f, Volume = 0.75f };
        public static readonly SoundStyle BubbleSound = new("CalamityFables/Sounds/BlowpipeBubbles") { PitchVariance = 0.1f, Volume = 1.5f };

        public static int LIFELEECH_DPS = 10;
        public static int LIFELEECH_DURATION = 300;

        public static int ACID_ARMOR_PENETRATION = 5;
        public static int ACID_DURATION = 300;
        public static float ACID_EXPLOSION_DAMAGE_MULT = 1.5f;

        public static int MIST_REGEN = 7;
        public static int MIST_DURATION = 180;

        public static LocalizedText AcidExplosionDamageText;

        public override void Load()
        {
            On_WorldGen.KillTile_ShouldDropSeeds += DropSeedsWithBlowpipe;
            FablesWorld.ModifyChestContentsEvent += SpawnInJungleChests;
            FablesItem.ModifyItemLootEvent += DropFromJungleCrates;

            // Best way to handle loading for these assets
            TrailSmoke = ModContent.Request<Texture2D>(AssetDirectory.Particles + "BlendedSmokeTrail");
            BigSmoke = ModContent.Request<Texture2D>(AssetDirectory.Particles + "BigSmoke");
            BigBlendedSmoke = ModContent.Request<Texture2D>(AssetDirectory.Particles + "BigBlendedSmoke");
            ToxicSkull = ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "ToxicSkull");
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Toxic Blowpipe");
            Tooltip.SetDefault("Left-click to coat your Darts with defense-reducing acid\n"
                + "Right-click to coat your Darts with life-draining neurotoxin\n"
                + "The chemicals are particularly volatile when combined..."); //"The chemicals may take on regenerative or destructive qualities when combined in different ways..."

            AcidExplosionDamageText = Mod.GetLocalization("Extras.ItemTooltipExtras.ToxicBlowpipeAcidExplosionDamage");

            FablesSets.HasCustomHeldDrawing[Type] = true;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 16;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 34;
            Item.height = 28;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.holdStyle = ItemHoldStyleID.HoldFront;
            Item.noMelee = true;
            Item.knockBack = 3;
            Item.value = Item.sellPrice(silver: 50);
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = AcidFireSound;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.useAmmo = AmmoID.Dart;
            Item.shootSpeed = 14f;

            Item.noUseGraphic = true;
            Item.ChangePlayerDirectionOnShoot = false;
        }

        public override void HoldItem(Player player)
        {
            player.SyncMousePosition();
            player.SyncRightClick();
        }

        public override void UseAnimation(Player player)
        {
            Item.UseSound = AcidFireSound;
            if (player.altFunctionUse == 2)
                Item.UseSound = NeurotoxinFireSound;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Use a custom entity source, which can be read in Projectile.SpawnProjectile which is called inside of NewProjectile
            // Doing it this way lets us set data before the projectile sync packet gets sent to the server in multiplayer
            // Otherwise we'll be setting data to the dart after its been sent on the server which means a race condition which is akward
            InbueType usedInbue = player.altFunctionUse == 2 ? InbueType.Leech : InbueType.Acid;
            source = new EntitySource_ToxicBlowpipe(source, usedInbue);
            Projectile.NewProjectileDirect(source, position + velocity.SafeNormalize(Vector2.Zero) * 7f, velocity, type, damage, knockback, player.whoAmI);

            return false;
        }

        #region Held Visuals
        public override void UseStyle(Player player, Rectangle heldItemFrame) => SetItemInHand(player);
        public override void HoldStyle(Player player, Rectangle heldItemFrame) => SetItemInHand(player);

        public void SetItemInHand(Player player)
        {
            // Change player direction based on cursor relative to player
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);

            //Default
            Vector2 itemPosition = player.MountedCenter + new Vector2(6f * player.direction, -2f * player.gravDir);
            float itemRotation = (player.MouseWorld() - itemPosition).ToRotation();

            Vector2 itemSize = new Vector2(48, 24);
            Vector2 itemOrigin = new Vector2(-22, 4);
            FablesUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin, true);
        }

        public override void UseItemFrame(Player player) => SetPlayerArms(player);
        public override void HoldItemFrame(Player player)
        {
            if (Main.gameMenu || player.pulley)
                return;

            SetPlayerArms(player);
        }

        public void SetPlayerArms(Player player)
        {
            // Change player direction based on cursor relative to player
            player.direction = Math.Sign((player.MouseWorld() - player.Center).X);

            Vector2 itemPosition = player.MountedCenter + new Vector2(6f * player.direction, -2f * player.gravDir);
            Vector2 handGrabPosition = itemPosition + (player.MouseWorld() - itemPosition).SafeNormalize(Vector2.One) * 10f;

            Vector2 direction = (player.MountedCenter - Vector2.UnitY * 4f).DirectionTo(handGrabPosition);
            direction.Y *= player.gravDir;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, direction.ToRotation() - MathHelper.PiOver2);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, direction.ToRotation() - MathHelper.PiOver2);
        }

        public void DrawHeld(ref PlayerDrawSet drawInfo, Texture2D texture, Vector2 position, Rectangle frame, float rotation, Color color, float scale, Vector2 origin)
        {
            // Draw Item, cant be texture since it doesnt use the item texture
            DrawData item = new(Held.Value, position, frame, color, rotation, origin, scale, drawInfo.itemEffect);
            drawInfo.DrawDataCache.Add(item);
        }

        public Rectangle GetDrawFrame(Texture2D texture, PlayerDrawSet drawInfo)
        {
            // Use the held sprite to get the right frame
            Held ??= ModContent.Request<Texture2D>(Texture + "_Held");
            return Held.Frame();
        }
        #endregion

        #region Chest and Crate loot
        private bool SpawnInJungleChests(Chest chest, Tile chestTile, bool alreadyAddedItem)
        {
            //Jungle chest
            if (chestTile.TileFrameX != 360 || chest.y < Main.maxTilesY / 2 || chestTile.TileType != TileID.Containers)
                return false;

            //1 / 4
            if (!Main.rand.NextBool(4))
                return false;

            int slot = 1;
            while (IsSecondaryJungleChestLoot(chest.item[slot].type) && slot < 20)
                slot++;
            if (slot == 20)
                return false;

            Item item = new Item(Type);
            item.Prefix(ItemLoader.ChoosePrefix(item, Main.rand));
            chest.item[slot] = item;
            return true;
        }

        private bool IsSecondaryJungleChestLoot(int itemType) => itemType == ItemID.LivingMahoganyWand || itemType == ItemID.LivingMahoganyLeafWand || itemType == ItemID.BeeMinecart || itemType == ItemID.HoneyDispenser;

        private void DropFromJungleCrates(Item item, ItemLoot itemLoot)
        {
            if (item.type != ItemID.JungleFishingCrate && item.type != ItemID.JungleFishingCrateHard)
                return;

            List<IItemDropRule> dropRules = itemLoot.Get();
            //Crates have 3 rules
            //1st one is for the crate-specific loot
            //2nd one is for an extra potion, shared between all crates
            //3rd one is for extra bait, shared between all crates

            //The first droprule of the crate-specific rule is the one for the chest items you can get in the crate
            if (dropRules[0] is not AlwaysAtleastOneSuccessDropRule crateSpecificLoot)
                return;

            //First one here has all the "chest" items from the crate
            if (crateSpecificLoot.rules[0] is not SequentialRulesNotScalingWithLuckRule chestItemLoot)
                return;

            //First one is the rarer flower boots drop, second one is the rest of the ivy chest items
            if (chestItemLoot.rules[1] is not OneFromOptionsNotScaledWithLuckDropRule regularItemLootPool)
                return;

            //Add yourself to the end 
            regularItemLootPool.dropIds = regularItemLootPool.dropIds.Append(Type).ToArray();
        }
        #endregion

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int damageIndex = tooltips.FindIndex(tooltip => tooltip.Name == "Damage" && tooltip.Mod == "Terraria");
            int baseDamage = Main.LocalPlayer.GetWeaponDamage(Item, true);

            TooltipLine AcidExplosionDamage = new TooltipLine(Mod, "CalamityFables:AcidExplosionDamage", AcidExplosionDamageText.Format((int)(baseDamage * ACID_EXPLOSION_DAMAGE_MULT)));

            AcidExplosionDamage.OverrideColor = Color.Lerp(Color.White, Color.YellowGreen, MathF.Pow(0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2), 4));

            tooltips.Insert(damageIndex + 1, AcidExplosionDamage);
        }

        public override void AddRecipes()
        {
            //Recipe similar to old Amazon. Jungle weapon, but really easy to get w/ a jungle suicide trip
            CreateRecipe().
                AddIngredient(ItemID.Blowpipe).
                AddIngredient(ItemID.Stinger, 2).
                AddIngredient(ItemID.JungleSpores, 6).
                AddTile(TileID.Anvils).
                Register();
        }

        private bool DropSeedsWithBlowpipe(On_WorldGen.orig_KillTile_ShouldDropSeeds orig, int x, int y)
        {
            if (orig(x, y))
                return true;

            //Worldgen.GetPlayerForTile is private so lol lmao
            return Main.rand.NextBool() && Main.player[Player.FindClosest(new Vector2(x, y) * 16f, 16, 16)].HasItem(Type);
        }
    }

    public class EntitySource_ToxicBlowpipe : EntitySource_ItemUse_WithAmmo
    {
        public InbueType Inbue { get; }

        public EntitySource_ToxicBlowpipe(EntitySource_ItemUse_WithAmmo referenceSource, InbueType inbue) : base(referenceSource.Player, referenceSource.Item, referenceSource.AmmoItemIdUsed, referenceSource.Context)
        {
            Inbue = inbue;
        }
    }

    public class InbuedDartProjectileHandling : ILoadable
    {
        public void Load(Mod mod)
        {
            FablesProjectile.OnSpawnEvent += ConvertSpawnedProjectiles;
            FablesProjectile.AIEvent += AI;
            FablesProjectile.ModifyHitNPCEvent += ModifyHitNPC;
            FablesProjectile.ModifyHitPlayerEvent += ModifyHitPlayer;
            FablesProjectile.OnHitNPCEvent += OnHitNPC;
            FablesProjectile.OnHitPlayerEvent += OnHitPlayer;
            FablesProjectile.PreDrawExtrasEvent += PreDrawExtras;

            FablesProjectile.RegisterSyncedData(typeof(ImbuedDartProjectileData), SendInbuedDartProjectileData, RecieveInbuedDartProjectileData);
        }

        public void Unload()
        {
            FablesProjectile.OnSpawnEvent -= ConvertSpawnedProjectiles;
            FablesProjectile.AIEvent -= AI;
            FablesProjectile.ModifyHitNPCEvent -= ModifyHitNPC;
            FablesProjectile.ModifyHitPlayerEvent -= ModifyHitPlayer;
            FablesProjectile.OnHitNPCEvent -= OnHitNPC;
            FablesProjectile.OnHitPlayerEvent -= OnHitPlayer;
            FablesProjectile.PreDrawExtrasEvent -= PreDrawExtras;
        }

        #region Global Data
        public class ImbuedDartProjectileData : CustomGlobalData
        {
            public InbueType Inbue;

            public enum InbueType
            {
                Acid,
                Leech
            }
        }

        public static void SendInbuedDartProjectileData(CustomGlobalData data, Projectile proj, BitWriter bitWriter, BinaryWriter writer)
        {
            if (data is not ImbuedDartProjectileData dartData)
                return;

            writer.Write((byte)dartData.Inbue);
        }

        public static void RecieveInbuedDartProjectileData(CustomGlobalData data, Projectile proj, BitReader bitReader, BinaryReader reader)
        {
            if (data is not ImbuedDartProjectileData dartData)
                return;

            dartData.Inbue = (InbueType)reader.ReadByte();
        }
        #endregion

        public static void ConvertSpawnedProjectiles(Projectile projectile, IEntitySource source)
        {
            // Use our custom projectile source to convert any shot darts into inbued darts
            if (source is EntitySource_ToxicBlowpipe inbueSource)
                projectile.SetProjectileData(new ImbuedDartProjectileData() { Inbue = inbueSource.Inbue, needsSyncing_forProjectiles = true });
        }

        private void AI(Projectile projectile)
        {
            if (!projectile.GetProjectileData<ImbuedDartProjectileData>(out var data))
                return;

            DoParticleEffects(projectile, data);
        }

        private void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!projectile.GetProjectileData<ImbuedDartProjectileData>(out var data))
                return;

            // The explosion ignores the initial target, so the darts damage must be increased
            // Also makes the damage effect from the sky dart actually work
            if (data.Inbue == InbueType.Leech && target.HasBuff(ModContent.BuffType<BlowpipeAcid>()))
                modifiers.ScalingBonusDamage += ACID_EXPLOSION_DAMAGE_MULT - 1f;
        }

        private void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
        {
            if (!projectile.GetProjectileData<ImbuedDartProjectileData>(out var data))
                return;

            // Same thing as NPC hits but we dont care about the explosion since Iframes
            if (data.Inbue == InbueType.Leech && target.HasBuff(ModContent.BuffType<BlowpipeAcid>()))
                modifiers.SourceDamage += ACID_EXPLOSION_DAMAGE_MULT - 1f;
        }

        private void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!projectile.GetProjectileData<ImbuedDartProjectileData>(out var data))
                return;

            if (data.Inbue == InbueType.Acid)
            {
                // Remove buff and create leech explosion
                if (target.HasBuff(ModContent.BuffType<BlowpipeLeech>()))
                {
                    target.RequestBuffRemoval(ModContent.BuffType<BlowpipeLeech>());

                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), projectile.Center, Vector2.Zero, ModContent.ProjectileType<LeechExplosion>(), projectile.damage, 0, projectile.owner);
                    SoundEngine.PlaySound(BubbleSound, projectile.position);
                }
                else
                    target.AddBuff(ModContent.BuffType<BlowpipeAcid>(), ACID_DURATION);
            }
            // Remove buff and create an acid explosion
            else if (data.Inbue == InbueType.Leech)
            {
                if (target.HasBuff(ModContent.BuffType<BlowpipeAcid>()))
                {
                    target.RemoveBuff(ModContent.BuffType<BlowpipeAcid>());

                    // Get damage and pass in the target so the explosion cannot hit them, only the initial hit can
                    int damage = (int)(projectile.damage * ACID_EXPLOSION_DAMAGE_MULT);
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), projectile.Center, Vector2.Zero, ModContent.ProjectileType<AcidExplosion>(), damage, 0, projectile.owner, target.whoAmI);
                }
                else
                    target.AddBuff(ModContent.BuffType<BlowpipeLeech>(), LIFELEECH_DURATION);
            }
        }

        private void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
        {
            if (!projectile.GetProjectileData<ImbuedDartProjectileData>(out var data))
                return;

            if (data.Inbue == InbueType.Acid)
            {
                // Remove buff and create leech explosion
                if (target.HasBuff(ModContent.BuffType<BlowpipeLeech>()))
                {
                    target.ClearBuff(ModContent.BuffType<BlowpipeLeech>());
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), projectile.Center, Vector2.Zero, ModContent.ProjectileType<LeechExplosion>(), projectile.damage, 0, projectile.owner);
                }
                else
                    target.AddBuff(ModContent.BuffType<BlowpipeAcid>(), ACID_DURATION);
            }
            // Remove buff and create an acid explosion
            else if (data.Inbue == InbueType.Leech)
            {
                if (target.HasBuff(ModContent.BuffType<BlowpipeAcid>()))
                {
                    target.ClearBuff(ModContent.BuffType<BlowpipeAcid>());

                    int damage = (int)(projectile.damage * ACID_EXPLOSION_DAMAGE_MULT);
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), projectile.Center, Vector2.Zero, ModContent.ProjectileType<AcidExplosion>(), damage, 0, projectile.owner);
                }
                else
                    target.AddBuff(ModContent.BuffType<BlowpipeLeech>(), LIFELEECH_DURATION);
            }
        }

        /// <summary>
        /// Every color associated with the Toxic Blowpipe inbues.
        /// </summary>
        /// <param name="inbue"></param>
        /// <param name="highlight"></param>
        /// <param name="highlightFade"></param>
        /// <param name="shadow"></param>
        /// <param name="shadowFade"></param>
        public static void GetInbueColors(InbueType inbue, out Color highlight, out Color highlightFade, out Color shadow, out Color shadowFade)
        {
            if (inbue == InbueType.Acid)
            {
                highlight = Color.YellowGreen;
                highlightFade = Color.DarkOliveGreen;
                shadow = new(0, 155, 0);
                shadowFade = Color.OliveDrab * 0.5f;
            }
            else
            {
                highlight = Color.DeepSkyBlue;
                highlightFade = new(38, 106, 220);
                shadow = new(38, 106, 220);
                shadowFade = new(26, 80, 166);
            }
        }

        #region Visuals
        private bool PreDrawExtras(Projectile projectile)
        {
            if (!projectile.GetProjectileData<ImbuedDartProjectileData>(out var data))
                return true;

            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

            Vector2 bloomPosition = projectile.Center - projectile.velocity.SafeNormalize(Vector2.Zero) * 4f - Main.screenPosition;
            Vector2 scale = new Vector2(0.6f, 1.2f);

            Color inbueColor = data.Inbue == InbueType.Leech ? Color.Aqua : new Color(120, 202, 12);
            inbueColor.A = 0;

            //Draws 2 layers of circular bloom behind the projectile
            Main.EntitySpriteDraw(bloom, bloomPosition, null, inbueColor * 0.4f, projectile.rotation, bloom.Size() / 2f, scale * projectile.scale * 0.3f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, bloomPosition, null, Color.White with { A = 0 } * 0.5f, projectile.rotation, bloom.Size() / 2f, scale * projectile.scale * 0.12f, SpriteEffects.None, 0);

            return true;
        }

        private static void DoParticleEffects(Projectile projectile, ImbuedDartProjectileData data)
        {
            if (Main.dedServ)
                return;

            GetInbueColors(data.Inbue, out Color highlight, out Color highlightFade, out Color shadow, out Color shadowFade);

            if (data.Inbue == InbueType.Acid && Main.rand.NextBool())
                highlight = new(121, 171, 19);

            // Light smoke layer
            int smokeCount = Main.rand.Next(2);
            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 particlePosition = projectile.Center - projectile.velocity * Main.rand.NextFloat();

                FadingBlendedSmoke smoke = new FadingBlendedSmoke(particlePosition, Main.rand.NextVector2Circular(1f, 1f), highlight, highlightFade, Main.rand.Next(8, 12), 1f, 0)
                {
                    Rotation = projectile.velocity.ToRotation(),
                    FadeEasing = FablesUtils.PolyInEasing,
                    LightEasing = FablesUtils.PolyInEasing
                };
                smoke.SetTexture(TrailSmoke.Value, new(3, 5));
                RTParticleHandler.SpawnParticle<MergeBlendSmokeRenderTarget>(smoke);
            }

            // Darker smoke layer
            smokeCount = Main.rand.Next(1, 3);
            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 particlePosition = projectile.Center - projectile.velocity * Main.rand.NextFloat() + Main.rand.NextVector2Circular(3f, 3f);

                FadingBlendedSmoke smoke = new FadingBlendedSmoke(particlePosition, Main.rand.NextVector2Circular(1f, 1f), shadow, shadowFade, Main.rand.Next(10, 15), 1f, 0)
                {
                    Rotation = projectile.velocity.ToRotation(),
                    FadeEasing = FablesUtils.PolyInEasing,
                    LightEasing = FablesUtils.PolyInEasing
                };
                smoke.SetTexture(TrailSmoke.Value, new(3, 5));
                RTParticleHandler.SpawnParticle<MergeBlendSmokeRenderTarget>(smoke);
            }

            // Acid specific particles
            if (data.Inbue == InbueType.Acid)
            {
                // Droplet Particles
                if (Main.rand.NextBool(15))
                {
                    Vector2 particlePosition = projectile.position + Main.rand.NextVector2Circular(5f, 5f);
                    Vector2 particleVelocity = projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.3f, 0.6f);

                    Particle drops = new PrimitiveStreak(particlePosition, particleVelocity, highlight, shadow, 3, 0, 6, Main.rand.Next(20, 30), shadow, highlightFade)
                    {
                        TrailTip = new TriangularTip(4),
                        Acceleration = Vector2.Zero,
                        Collision = true
                    };
                    ParticleHandler.SpawnParticle(drops);
                }
                // Dust
                if (Main.rand.NextBool(3))
                {
                    Vector2 dustPosition = projectile.position + Main.rand.NextVector2Circular(10f, 10f);

                    Dust dust = Dust.NewDustPerfect(dustPosition, DustID.ToxicBubble, Main.rand.NextVector2Circular(0.2f, 0.2f), 0, highlight, Main.rand.NextFloat(0.5f, 1.2f));
                    dust.noGravity = false;
                }
            }
            // Leech particles
            else
            {
                // Small bloom particles
                if (Main.rand.NextBool(5))
                {
                    Vector2 particlePosition = projectile.position + Main.rand.NextVector2Circular(5f, 5f);

                    Particle glowies = new BloomParticle(particlePosition, Main.rand.NextVector2Circular(1f, 1f), highlight, shadow * 0.25f, Main.rand.NextFloat(0.75f, 1f), Main.rand.NextFloat(0.8f, 1f)) { Pixelated = true };
                    ParticleHandler.SpawnParticle(glowies);
                }
            }
        }
        #endregion
    }

    #region Acid
    public class AcidExplosion : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        public ref float CannotHit => ref Projectile.ai[0];

        private bool DoBlastEffects = true;

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 10;

            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 20;
        }

        public override void AI()
        {
            // Spawn effects on first frame
            if (DoBlastEffects)
            {
                BlastEffects();
                DoBlastEffects = false;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => FablesUtils.AABBvCircle(targetHitbox, Projectile.Center, 90) && Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.Center.ToVector2(), 1, 1);

        // Uses the NPC ID stored in ai[0] to ignore the initial target
        public override bool? CanHitNPC(NPC target) => target.whoAmI == CannotHit ? false : null;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Chain reaction
            if (target.HasBuff<BlowpipeAcid>())
            {
                target.RequestBuffRemoval(ModContent.BuffType<BlowpipeAcid>());
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, Type, (int)(Projectile.damage * 1.2f), 0, Projectile.owner);
            }
        }

        public void BlastEffects()
        {
            SoundEngine.PlaySound(ExplosionSound, Projectile.Center);
            CameraManager.Quake += 5;

            InbuedDartProjectileHandling.GetInbueColors(InbueType.Acid, out Color highlight, out Color highlightFade, out Color shadow, out Color shadowFade);

            // Lighter front layer of smoke
            for (int i = 0; i < 12; i++)
            {
                Vector2 particleVelocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
                Vector2 particlePosition = Projectile.Center + particleVelocity.SafeNormalize(Vector2.One) * Main.rand.NextFloat(30f, 60f);

                Color startColor = Main.rand.NextBool() ? new(121, 171, 19) : highlight;

                FadingBlendedSmoke smoke = new FadingBlendedSmoke(particlePosition, particleVelocity, startColor, highlightFade, Main.rand.Next(45, 60))
                {
                    OverrideTexture = BigBlendedSmoke.Value,
                    FadeEasing = FablesUtils.PolyInEasing,
                    LightEasing = FablesUtils.PolyInEasing,
                    Layer = DrawhookLayer.AbovePlayer
                };
                RTParticleHandler.SpawnParticle<MergeBlendSmokeRenderTarget>(smoke);
            }

            // Darker smoke
            for (int i = 0; i < 20; i++)
            {
                Vector2 particleVelocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
                Vector2 particlePosition = Projectile.Center + particleVelocity.SafeNormalize(Vector2.One) * Main.rand.NextFloat(25f, 50f);

                FadingBlendedSmoke smoke = new FadingBlendedSmoke(particlePosition, particleVelocity, shadow, shadowFade, Main.rand.Next(45, 60))
                {
                    OverrideTexture = BigBlendedSmoke.Value,
                    FadeEasing = FablesUtils.PolyInEasing,
                    LightEasing = FablesUtils.PolyInEasing,
                    Layer = DrawhookLayer.AbovePlayer
                };
                RTParticleHandler.SpawnParticle<MergeBlendSmokeRenderTarget>(smoke);
            }

            // Skull
            FadingBlendedSmoke skull = new FadingBlendedSmoke(Projectile.Center, Vector2.Zero, highlight, highlightFade, 60, 1f, -0.03f)
            {
                Rotation = Main.rand.NextFloat(-0.2f, 0.2f),
                FadeEasing = FablesUtils.PolyInEasing,
                LightEasing = FablesUtils.PolyInEasing,
                Layer = DrawhookLayer.AbovePlayer

            };
            skull.SetTexture(ToxicSkull.Value, new(1, 10), new(0, 0));
            RTParticleHandler.SpawnParticle<MergeBlendSmokeRenderTarget>(skull);

            // Droplet particles
            int dropCount = Main.rand.Next(5, 10);
            for (int i = 0; i < dropCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.PiOver2);
                Vector2 particleVelocity = -Vector2.UnitY.RotatedBy(angle * Main.rand.NextFromList(1, -1)) * Main.rand.NextFloat(3f, 6f);

                // Lifetime increases when the droplets are angled higher
                int lifetime = (int)(Utils.GetLerpValue(MathHelper.PiOver2, 0, angle) * 40) + Main.rand.Next(20, 30);

                Particle drops = new PrimitiveStreak(Projectile.Center, particleVelocity, highlight, shadow, 3, 0, 6, lifetime, shadow, highlightFade, true)
                {
                    TrailTip = new TriangularTip(4),
                    Acceleration = Vector2.Zero,
                    Collision = true
                };
                ParticleHandler.SpawnParticle(drops);
            }

            // Dust
            dropCount = Main.rand.Next(10, 20);
            for (int i = 0; i < dropCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.PiOver2);
                Vector2 dustVelocity = -Vector2.UnitY.RotatedBy(angle * Main.rand.NextFromList(1, -1)) * Main.rand.NextFloat(3f, 5f);
                Vector2 dustPosition = Projectile.Center + dustVelocity.SafeNormalize(Vector2.One) * Main.rand.NextFloat(15f, 25f);

                Dust dust = Dust.NewDustPerfect(dustPosition, DustID.ToxicBubble, dustVelocity, 0, highlight, Main.rand.NextFloat(0.5f, 1.2f));
                dust.noGravity = false;
            }
        }
    }

    public class BlowpipeAcid : ModBuff
    {
        public override string Texture => AssetDirectory.Buffs + Name;

        public override void Load()
        {
            FablesNPC.ModifyIncomingHitEvent += ReduceDefense;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Acid");
            Description.SetDefault("Defense reduced by 5");
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            BuffID.Sets.CanBeRemovedByNetMessage[Type] = true;
        }

        public override void Update(NPC NPC, ref int buffIndex)
        {
            NPC.SetNPCFlag(Name);
            DoEffects(NPC);
        }

        private void ReduceDefense(NPC npc, ref NPC.HitModifiers modifiers)
        {
            // Needed since npc defense doesn't reset every tick
            if (npc.GetNPCFlag(Name))
                modifiers.Defense.Flat -= ACID_ARMOR_PENETRATION;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Can just subtract from player defense
            player.statDefense -= ACID_ARMOR_PENETRATION;
            DoEffects(player);
        }

        private static void DoEffects(Entity entity)
        {
            InbuedDartProjectileHandling.GetInbueColors(InbueType.Acid, out Color highlight, out Color highlightFade, out Color shadow, out Color shadowFade);

            // Lighter smoke
            if (Main.rand.NextBool(15))
            {
                Vector2 particlePosition = entity.Center + Main.rand.NextVector2Circular(entity.width / 2, entity.height / 2);
                Vector2 particleVelocity = Main.rand.NextVector2Circular(1f, 1f);

                RTParticle smoke = new FadingBlendedSmoke(particlePosition, particleVelocity, highlight, highlightFade, Main.rand.Next(45, 60)) { Layer = DrawhookLayer.AbovePlayer };
                RTParticleHandler.SpawnParticle<MergeBlendSmokeRenderTarget>(smoke);
            }

            // Darker smoke
            if (Main.rand.NextBool(10))
            {
                Vector2 particlePosition = entity.Center + Main.rand.NextVector2Circular(entity.width / 2, entity.height / 2);
                Vector2 particleVelocity = Main.rand.NextVector2Circular(1f, 1f);

                RTParticle smoke = new FadingBlendedSmoke(particlePosition, particleVelocity, shadow, shadowFade, Main.rand.Next(45, 60)) { Layer = DrawhookLayer.AbovePlayer };
                RTParticleHandler.SpawnParticle<MergeBlendSmokeRenderTarget>(smoke);
            }

            // Droplet particles
            if (Main.rand.NextBool(30))
            {
                Vector2 particlePosition = Main.rand.NextVector2FromRectangle(entity.Hitbox);
                Particle drops = new PrimitiveStreak(particlePosition, Vector2.Zero, highlight, shadow, 2f, 0, 6, Main.rand.Next(30, 45), shadow, highlightFade, true, false)
                {
                    TrailTip = new TriangularTip(4),
                    Acceleration = Vector2.Zero,
                    Collision = true
                };
                ParticleHandler.SpawnParticle(drops);
            }

            if (Main.rand.NextBool(8))
            {
                Dust drip = Dust.NewDustDirect(entity.position, entity.width, entity.height, DustID.ToxicBubble, 0f, 0f, 0, highlight);
                drip.noGravity = false;
                drip.scale = Main.rand.NextFloat(0.5f, 1.2f);
                drip.velocity *= 0.2f;
                drip.velocity += Main.rand.NextVector2Circular(0.2f, 0.2f);
            }
        }
    }
    #endregion

    #region Leech
    public class LeechExplosion : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        private bool SpawnedVisualSmoke = true;

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 80;
        }

        public override void AI()
        {
            // Spawn effects on first frame
            if (SpawnedVisualSmoke)
            {
                HealingMistEffects();
                SpawnedVisualSmoke = false;
            }

            foreach (Player player in Main.ActivePlayers)
            {
                // Check the team so it doesnt heal an enemy player
                if (player.InOpposingTeam(Projectile.GetOwner()))
                    continue;

                if (FablesUtils.AABBvCircle(player.Hitbox, Projectile.Center, 150f + 30f * Projectile.timeLeft / 80f))
                    player.AddBuff(ModContent.BuffType<BlowpipeRegen>(), MIST_DURATION);
            }
        }

        private void HealingMistEffects()
        {
            SoundEngine.PlaySound(BubbleSound, Projectile.position);
            InbuedDartProjectileHandling.GetInbueColors(InbueType.Leech, out Color highlight, out Color highlightFade, out Color shadow, out Color shadowFade);

            int smokeCount = Main.rand.Next(15, 38);
            int mistSparks = Main.rand.Next(45, 68);
            int sparklyCount = Main.rand.Next(15, 32);

            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 particleVelocity = Main.rand.NextVector2Circular(3.2f, 3.2f);
                Vector2 particlePosition = Projectile.Center + Main.rand.NextVector2Circular(170f, 90f);
                float smokeSize = Main.rand.NextFloat(1.25f, 1.8f);

                RefreshingMist smoke = new RefreshingMist(particlePosition, particleVelocity, highlightFade * 0.8f, highlightFade, shadow, smokeSize);
                if (Main.rand.NextBool())
                    smoke.OverrideTexture = BigSmoke.Value;
                ParticleHandler.SpawnParticle(smoke);
            }

            for (int i = 0; i < mistSparks; i++)
            {
                Vector2 dustVelocity = Main.rand.NextVector2Circular(1f, 1f);
                Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(170f, 90f);
                Color dustColor = Color.Lerp(Color.White, Main.rand.NextBool() ? Color.GreenYellow : Color.SpringGreen, Main.rand.NextFloat());
                float dustScale = Main.rand.NextFloat(1.5f, 2.75f);

                Dust sparks = Dust.NewDustPerfect(dustPosition, ModContent.DustType<BlowpipeHealingMistDust>(), dustVelocity, Main.rand.Next(110), dustColor, dustScale);
                sparks.noGravity = true;
                sparks.customData = Color.DodgerBlue;
            }

            for (int i = 0; i < sparklyCount; i++)
            {
                Vector2 dustPosition = Projectile.position + Main.rand.NextVector2Circular(170f, 90f);
                for (int k = 0; k < 3; k++)
                {
                    Dust sparks = Dust.NewDustPerfect(dustPosition, DustID.UltraBrightTorch, Main.rand.NextVector2CircularEdge(10f, 10f), 100);
                    sparks.velocity = Main.rand.NextVector2Circular(3f, 3f);
                    sparks.noGravity = true;
                    sparks.scale = 0.8f;
                }
            }
        }

        public override bool? CanDamage() => false;
    }

    public class BlowpipeLeech : ModBuff
    {
        public override string Texture => AssetDirectory.Buffs + Name;

        public override void Load()
        {
            FablesNPC.UpdateLifeRegenEvent += UpdateLifeRegen;
            FablesPlayer.UpdateBadLifeRegenEvent += UpdateLifeRegen;
        }

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            BuffID.Sets.CanBeRemovedByNetMessage[Type] = true;
        }

        public override void Update(NPC NPC, ref int buffIndex)
        {
            NPC.SetNPCFlag(Name);
            DoEffects(NPC);
        }

        private void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (!npc.GetNPCFlag(Name))
                return;

            if (npc.lifeRegen > 0)
                npc.lifeRegen = 0;

            npc.lifeRegen -= LIFELEECH_DPS * 2;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.SetPlayerFlag(Name);
            DoEffects(player);
        }

        private void UpdateLifeRegen(Player player)
        {
            if (!player.GetPlayerFlag(Name))
                return;

            if (player.lifeRegen > 0)
                player.lifeRegen = 0;

            player.lifeRegen -= LIFELEECH_DPS * 2;
        }

        private static void DoEffects(Entity entity)
        {
            InbuedDartProjectileHandling.GetInbueColors(InbueType.Leech, out Color highlight, out Color highlightFade, out Color shadow, out Color shadowFade);

            if (Main.rand.NextBool(8))
            {
                Vector2 particlePosition = Main.rand.NextVector2FromRectangle(entity.Hitbox);
                Vector2 particleVelocity = Main.rand.NextVector2Circular(1f, 1f);
                float smokeSize = Main.rand.NextFloat(0.8f, 1.2f);

                Particle smoke = new NeurotoxinMist(particlePosition, particleVelocity, highlightFade * 0.8f, highlightFade, shadow, smokeSize, dustSpawnRate: 1 / 32f);
                ParticleHandler.SpawnParticle(smoke);
            }
        }
    }

    public class BlowpipeRegen : ModBuff
    {
        public override string Texture => AssetDirectory.Buffs + Name;

        public override void Load()
        {
            FablesPlayer.UpdateLifeRegenEvent += UpdateLifeRegen;
        }

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.SetPlayerFlag(Name);
            DoEffects(player);
        }

        private void UpdateLifeRegen(Player player)
        {
            if (player.GetPlayerFlag(Name))
                player.lifeRegen += MIST_REGEN;
        }

        private static void DoEffects(Player player)
        {
            InbuedDartProjectileHandling.GetInbueColors(InbueType.Leech, out Color highlight, out Color highlightFade, out Color shadow, out Color shadowFade);

            if (Main.rand.NextBool(8))
            {
                Vector2 particlePosition = Main.rand.NextVector2FromRectangle(player.Hitbox);
                Vector2 particleVelocity = Main.rand.NextVector2Circular(1f, 1f);
                float smokeSize = Main.rand.NextFloat(0.8f, 1.2f);

                Particle smoke = new RefreshingMist(particlePosition, particleVelocity, highlightFade * 0.8f, highlightFade, shadow, smokeSize, dustSpawnRate: 1 / 32f);
                ParticleHandler.SpawnParticle(smoke);
            }

            if (Main.rand.NextBool(10))
            {
                Vector2 dustPosition = player.Center + Main.rand.NextVector2Circular(player.width / 2, player.height / 2);
                for (int k = 0; k < 3; k++)
                {
                    Dust sparks = Dust.NewDustPerfect(dustPosition, DustID.UltraBrightTorch, Main.rand.NextVector2CircularEdge(10f, 10f), 100);
                    sparks.velocity = Main.rand.NextVector2Circular(3f, 3f);
                    sparks.noGravity = true;
                    sparks.scale = 0.8f;
                }
            }
        }
    }
    #endregion
}

#region Dust

public class BlowpipeNeurotoxinDust : ModDust
{
    public override string Texture => AssetDirectory.Visible;

    public override void OnSpawn(Dust dust)
    {
        dust.scale = Main.rand.NextFloat(0.9f, 1.2f);
        dust.noLight = true;
        dust.noLightEmittence = false;
    }

    public override Color? GetAlpha(Dust dust, Color lightColor)
    {
        return Color.White;
    }

    public override bool Update(Dust dust)
    {
        //update position / rotation
        if (!dust.noGravity)
            dust.velocity.Y += 0.1f;
        else
        {
            dust.velocity.Y -= 0.05f;
            if (dust.velocity.Y < -5f)
                dust.velocity.Y = -5;

            dust.velocity *= 0.985f;
        }

        dust.position += dust.velocity;
        dust.rotation += dust.velocity.Y + dust.velocity.X;

        if (dust.alpha < 80)
            dust.alpha += 6;
        else
            dust.alpha += 2;


        if (dust.alpha > 150)
        {
            dust.active = false;
        }

        if (dust.customData != null && dust.customData is Color)
            dust.color = Color.Lerp(dust.color, (Color)dust.customData, 0.01f);

        dust.scale *= 0.96f;

        if (!dust.noLightEmittence)
        {
            float strength = dust.scale * 1.4f;
            if (strength > 1f)
            {
                strength = 1f;
            }
            Lighting.AddLight(dust.position, dust.color.ToVector3() * strength * 0.2f);
        }

        if (dust.active)
            NoitaBloomLayer.bloomedDust.Add(new BloomInfo(dust.color, dust.position, dust.scale, dust.alpha));

        return false;
    }
}

public class BlowpipeHealingMistDust : ModDust
    {
        public override string Texture => AssetDirectory.Dust + "HealthCross";

        public override void OnSpawn(Dust dust)
        {
            dust.noLight = true;
            dust.noLightEmittence = false;
            dust.rotation = 0;
        }

        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            return Color.White;
        }

        public override bool Update(Dust dust)
        {
            //update position / rotation
            if (!dust.noGravity)
                dust.velocity.Y += 0.1f;
            else
            {
                dust.velocity.Y -= 0.05f;
                if (dust.velocity.Y < -5f)
                    dust.velocity.Y = -5;

                dust.velocity *= 0.985f;
            }

            dust.position += dust.velocity;

            if (dust.alpha < 80)
                dust.alpha += 6;
            else
                dust.alpha += 2;


            if (dust.alpha > 150)
            {
                dust.active = false;
            }

            if (dust.customData != null && dust.customData is Color)
                dust.color = Color.Lerp(dust.color, (Color)dust.customData, 0.01f);

            dust.scale *= 0.96f;

            if (!dust.noLightEmittence)
            {
                float strength = dust.scale * 0.3f;
                if (strength > 1f)
                {
                    strength = 1f;
                }
                Lighting.AddLight(dust.position, dust.color.ToVector3() * strength * 0.2f);
            }

            if (dust.active)
                NoitaBloomLayer.bloomedDust.Add(new BloomInfo(Color.DodgerBlue * 0.45f, dust.position, dust.scale * 0.3f, dust.alpha));

            return false;
        }
    }
#endregion