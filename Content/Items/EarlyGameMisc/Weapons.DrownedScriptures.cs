using CalamityFables.Core.DrawLayers;
using CalamityFables.Core.RenderTargets;
using CalamityFables.Particles;
using ReLogic.Utilities;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;
using static CalamityFables.Content.Items.EarlyGameMisc.DrownedScriptures;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class DrownedScriptures : ModItem, ICustomHeldDraw
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        internal static Asset<Texture2D> HeldTexture;
        internal static LocalizedText HorrorDamageText;

        #region Sounds
        public static readonly string DrownedScripturesSoundPath = SoundDirectory.Sounds + "DrownedScriptures/";

        public static readonly SoundStyle ShootSound = new(DrownedScripturesSoundPath + "DarkblotFire", 3) { PitchVariance = 0.05f, Volume = 0.8f };
        public static readonly SoundStyle MineExplodeSound = new(DrownedScripturesSoundPath + "DarkblotExplode", 3) { PitchVariance = 0.05f, Volume = 1f };
        public static readonly SoundStyle MineExplodeEmpoweredExtraSound = new(DrownedScripturesSoundPath + "DarkblotExplodeExtra", 3) { PitchVariance = 0.05f, Volume = 1f };

        public static readonly SoundStyle HorrorShootSound = new(DrownedScripturesSoundPath + "InkMonsterDeploy", 2) { PitchVariance = 0.05f, Volume = 1f };
        public static readonly SoundStyle HorrorBite = new(DrownedScripturesSoundPath + "InkMonsterBite", 4) { Volume = 1f };
        public static readonly SoundStyle HorrorDeath = new(DrownedScripturesSoundPath + "InkMonsterDie", 3) { PitchVariance = 0.05f, Volume = 1f };
        public static readonly SoundStyle HorrorPowerLoop = new(DrownedScripturesSoundPath + "InkMonsterPowerLoop", 1) { Volume = 1f, IsLooped = true };
        public static readonly SoundStyle HorrorTravelLoop = new(DrownedScripturesSoundPath + "InkMonsterTravelLoop", 1) { Volume = 1f, IsLooped = true };

        #endregion

        #region Shaders
        public static Effect MineShader => Scene["DrownedScriptures/InkMine"].GetShader().Shader;
        public static Effect ExplosionRingShader => Scene["DrownedScriptures/InkExplosionRing"].GetShader().Shader;
        public static Effect HorrorShader => Scene["DrownedScriptures/HorrorShader"].GetShader().Shader;
        public static Effect HorrorBaseShader => Scene["DrownedScriptures/HorrorBaseShader"].GetShader().Shader;
        public static Effect HorrorTrailShader => Scene["DrownedScriptures/HorrorTrailShader"].GetShader().Shader;

        #endregion

        public static float MINE_EXPLOSION_DAMAGE_MULT = 0.75f;
        public static int MINE_PROXIMITY_DETONATION_TIME = 30;
        public static int MINE_FLOATING_TIME = 150;
        public static Point MINE_EXPLOSION_RADIUS_RANGE = new(90, 125);
        public static Vector2 MINE_DECELERATION = new(0.05f, 0.05f);

        public static int NPC_DEBUFF_DURATION = 720;
        public static int NPC_DEBUFF_COOLDOWN = 60;

        #region Horror Reflection Fields
        public static float HORROR_TARGET_RANGE = 1200;
        public static int HITS_TO_FULL_POWER = 8;
        public static Vector2 HORROR_DAMAGE_MULT_RANGE = new(1.5f, 3.5f);
        public static Vector2 HORROR_DISTANCE_LERP = new(50, 600);  // The range of distances that determines curve rate and speed
        public static Vector2 HORROR_CURVE_SPEED_RANGE = new(0.2f, 1f); // Determines how fast the monster curves towards its target. Lerps from X to Y as distance to target decreases
        public static Vector2 HORROR_SPEED_RANGE = new(15f, 10f);     // Determines the max speed of the monster. Lerps from X to Y as distance to target decreases
        public static float HORROR_MAX_SPEED_MULT = 3f;  // Maximum speed mult when the horror reaches full strength
        public static Point HORROR_WAIT_TIME_RANGE = new(5, 15);
        public static int HORROR_DEATH_TIME = 30;

        #endregion

        internal static int MineID;
        internal static int HorrorID;

        private int HoldOutTime = 0;

        public override void Load()
        {
            FablesWorld.ModifyChestContentsEvent += SpawnInShadowChests;
            FablesItem.ModifyItemLootEvent += DropFromObsidianLockBoxes;
        }

        public override void SetStaticDefaults()
        {
            FablesSets.HasCustomHeldDrawing[Type] = true;
            HorrorDamageText = Mod.GetLocalization("Extras.ItemTooltipExtras.DrownedScripturesHorrorDamage");
        }

        public override void SetDefaults()
        {
            Item.damage = 48;
            Item.DamageType = DamageClass.Magic;
            Item.width = 32;
            Item.height = 32;
            Item.useTime = Item.useAnimation = 40;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.knockBack = 6f;
            Item.value = Item.sellPrice(gold: 3);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = MineID;
            Item.autoReuse = true;
            Item.shootSpeed = 16f;
            Item.mana = 35;
            Item.UseSound = ShootSound;

            Item.noUseGraphic = true;
        }

        public override void UseAnimation(Player player)
        {
            player.SyncAltFunctionUse();

            // Different sound on alt use
            Item.UseSound = player.altFunctionUse == 2 ? HorrorShootSound : ShootSound;
        }

        public override bool AltFunctionUse(Player player)
        {
            bool viableTargets = false;

            // Find at least one viable target
            foreach(NPC npc in Main.ActiveNPCs)
            {
                bool canBeChasedBy = npc.CanBeChasedBy(ContentSamples.ProjectilesByType[HorrorID]);
                if (npc.Distance(player.MountedCenter) < HORROR_TARGET_RANGE && DrownedScripturesHorror.ValidTarget(npc, canBeChasedBy))
                {
                    viableTargets = true;
                    break;
                }
            }

            // Alt func usable when theres a viable target and no other monster
            return viableTargets && player.ownedProjectileCounts[HorrorID] <= 0;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            // No mana on alt use
            if (player.altFunctionUse == 2)
                mult = 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot monster
            if (player.altFunctionUse == 2)
                Projectile.NewProjectile(source, position, velocity, HorrorID, damage, 0f, player.whoAmI);
            // Shoot mine
            else
            {
                // Add player velocity for extra range
                velocity += player.velocity;
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            }

            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int damageIndex = tooltips.FindIndex(tooltip => tooltip.Name == "Damage" && tooltip.Mod == "Terraria");

            int baseDamage = Main.LocalPlayer.GetWeaponDamage(Item, true);
            Point curseDamage = new((int)(baseDamage * HORROR_DAMAGE_MULT_RANGE.X), (int)(baseDamage * HORROR_DAMAGE_MULT_RANGE.Y));

            TooltipLine horrorDamageLine = new TooltipLine(Mod, "CalamityFables:HorrorDamage", HorrorDamageText.Format(curseDamage.X, curseDamage.Y));
            horrorDamageLine.OverrideColor = Color.Lerp(Color.White, new Color(140, 52, 247), MathF.Pow(0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2f), 4f));

            tooltips.Insert(damageIndex + 1, horrorDamageLine);
        }

        #region Held Visuals
        public override void HoldItem(Player player)
        {
            // Keep hold out time updated
            if (player.ItemAnimationActive)
                HoldOutTime = 35;
            else if (HoldOutTime > 0)
                HoldOutTime--;
        }

        public bool HoldOut => HoldOutTime > 0;

        /// <summary>
        /// Uses player animation progress to create a function. <br/>
        /// Used to determine droplet spawn rate and goop overlay opacity.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private static float GoopIntensity(Player player)
        {
            float animProgress = Utils.GetLerpValue(player.itemAnimationMax, 0, player.itemAnimation, true);
            return FablesUtils.PiecewiseAnimation(animProgress, [new(FablesUtils.PolyOutEasing, 0f, 0f, 1f), new(FablesUtils.PolyOutEasing, 0.1f, 1f, -1f), new(FablesUtils.ConstantEasing, 0.8f, 0f, 0f)]);
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame) => AnimationStyle(player);

        public override void HoldStyle(Player player, Rectangle heldItemFrame) => AnimationStyle(player);

        private static void AnimationStyle(Player player)
        {
            // Run if the item should be held
            if (player.HeldItem.ModItem is not DrownedScriptures drownedScriptures || !drownedScriptures.HoldOut)
                return;

            int direction = player.direction;
            float closingProgress = FablesUtils.PolyOutEasing(Utils.GetLerpValue(10, 25, drownedScriptures.HoldOutTime, true));

            // Keep item at consistent position
            float itemRotation = MathHelper.Lerp(0.2f, 0.4f, closingProgress) * player.gravDir;
            Vector2 itemPosition = player.MountedCenter.Floor() + new Vector2(12f * direction, 4f * player.gravDir);

            Vector2 itemSize = new(18, 38);
            Vector2 itemOrigin = new(-9, -19);

            // CleanHoldStyle abstracts sprite placement calculations
            FablesUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin, true, true);

            // Spawn droplet particles
            float goopIntensity = GoopIntensity(player);
            if (goopIntensity > 0)
            {
                if (Main.rand.NextFloat() <= goopIntensity * 0.15f)
                {
                    Vector2 particlePosition = itemPosition + Main.rand.NextVector2Circular(10f, 10f);
                    Color particleColor = Color.Lerp(DrownedScripturesMine.DarkColor, DrownedScripturesMine.LightColor, Main.rand.NextFloat(0.2f, 0.7f));

                    Particle particle = new DripParticle(particlePosition, particleColor, DrownedScripturesMine.DarkColor, 1f) { Lifetime = Main.rand.Next(20, 30) };
                    ParticleHandler.SpawnParticle(particle);
                }
            }
        }

        public override void UseItemFrame(Player player) => AnimationItemFrame(player);
        public override void HoldItemFrame(Player player)
        {
            if (Main.gameMenu || player.pulley)
                return; 
            
            AnimationItemFrame(player);
        }

        private static void AnimationItemFrame(Player player)
        {
            // Run if the item should be held
            if (player.HeldItem.ModItem is not DrownedScriptures drownedScriptures || !drownedScriptures.HoldOut)
                return;

            float closingProgress = FablesUtils.PolyOutEasing(Utils.GetLerpValue(10, 25, drownedScriptures.HoldOutTime, true));

            // Arm rotation depending on scroll state
            float frontArmRotation = MathHelper.Lerp(0.3f, 0.6f, closingProgress) - MathHelper.PiOver2;
            float backArmRotation = MathHelper.Lerp(0.3f, 0f, closingProgress) - MathHelper.PiOver2;

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation * player.direction);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, backArmRotation * player.direction);
        }

        public void DrawHeld(ref PlayerDrawSet drawInfo, Texture2D texture, Vector2 position, Rectangle frame, float rotation, Color color, float scale, Vector2 origin)
        {
            DrawData item = new(HeldTexture.Value, position, frame, color, rotation, origin, scale, drawInfo.itemEffect);
            drawInfo.DrawDataCache.Add(item);

            // Top goopy layer
            Color goopColor = Color.White * GoopIntensity(drawInfo.drawPlayer);
            frame.X += frame.Width + 2;

            item = new(HeldTexture.Value, position, frame, goopColor, rotation, origin, scale, drawInfo.itemEffect);
            drawInfo.DrawDataCache.Add(item);
        }

        public Rectangle GetDrawFrame(Texture2D texture, PlayerDrawSet drawInfo)
        {
            HeldTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + Name + "_Held");

            float closingProgress = Utils.GetLerpValue(10, 25, HoldOutTime, true);
            int frame = drawInfo.drawPlayer.ItemAnimationActive ? 3 : (int)MathHelper.Lerp(0, 3, closingProgress);
            return HeldTexture.Frame(2, 4, 0, frame, -2, -2);
        }

        #endregion

        #region Chest and Crate loot
        private bool SpawnInShadowChests(Chest chest, Tile chestTile, bool alreadyAddedItem)
        {
            // Shadow Chest
            if (chestTile.TileFrameX != 4 * 36 || chest.y < Main.maxTilesY / 2 || chestTile.TileType != TileID.Containers)
                return false;

            // 1 / 5
            if (!Main.rand.NextBool(5))
                return false;

            // Add as a secondary item
            Item item = new Item(Type);
            item.Prefix(-1);

            var loot = chest.item.ToList();
            loot.Insert(1, item);
            chest.item = [.. loot];

            return true;
        }

        private void DropFromObsidianLockBoxes(Item item, ItemLoot itemLoot)
        {
            if (item.type != ItemID.ObsidianLockbox)
                return;

            itemLoot.Add(ItemDropRule.Common(Type, 5));
        }

        #endregion
    }

    public class DrownedScripturesMine : ModProjectile
    {
        public override string Texture => AssetDirectory.Assets + "Invisible";

        public static readonly Color LightColor = new Color(102, 76, 255);
        public static readonly Color LightColor2 = new Color(132, 0, 175);
        public static readonly Color DarkColor = new Color(7, 0, 51);
        public static readonly Color GlowColor = new Color(255, 114, 127);

        private readonly float TimeOffset = Main.rand.NextFloat(0, 30);

        private int Lifetime;
        private int ProximityTimer = 0;
        private PrimitiveQuadrilateral MineQuad;

        private float DetonationProgress => Utils.GetLerpValue(0, MINE_PROXIMITY_DETONATION_TIME, ProximityTimer);
        private bool Falling => Projectile.timeLeft < Lifetime - MINE_FLOATING_TIME;

        public override void SetStaticDefaults() => MineID = Type;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = Lifetime = 1200;
            Projectile.friendly = true;
        }

        public override void AI()
        {
            Projectile.velocity.X *= 1f - MINE_DECELERATION.X;

            if (Falling)
                Projectile.velocity.Y += 0.05f;
            else
            {
                Projectile.velocity.Y *= 1f - MINE_DECELERATION.Y;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && projectile.type == Type && projectile.Distance(Projectile.Center) < 48)
                        Projectile.velocity += Projectile.SafeDirectionFrom(projectile.Center) * 0.2f;
                }
            }

            PassiveEffects();

            // Check for enemies within range for detonation
            bool enemyClose = Projectile.FindTarget(1200, true, ValidTarget) != null;

            // Update proximity timer
            if (enemyClose)
            {
                if (ProximityTimer < MINE_PROXIMITY_DETONATION_TIME)
                    ProximityTimer++;
            }
            else if (ProximityTimer > 0)
                ProximityTimer--;

            // Detonate once timer is high enough
            if (DetonationProgress >= 1f)
                Explode();
        }

        private bool ValidTarget(NPC npc, bool canBeChased)
        {
            // Use velocity and detonation time to estimate if the mine will be in range soon
            Vector2 projectedMovement = npc.velocity * (1f - DetonationProgress) * (MINE_PROXIMITY_DETONATION_TIME - 5);
            Rectangle projectedHitbox = npc.Hitbox;
            projectedHitbox.Offset(projectedMovement.ToPoint());

            // Check if target is currently in explosion range or will be
            return canBeChased && (npc.Hitbox.Distance(Projectile.Center) < MINE_EXPLOSION_RADIUS_RANGE.Y || projectedHitbox.Distance(Projectile.Center) < MINE_EXPLOSION_RADIUS_RANGE.Y);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            DrownedScripturesNPCDebuff.ApplyDebuff(target);
            Explode(target);    // Explode, but prevent the target from getting hit by the explosion
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Explode();
            return true;
        }

        private void Explode(NPC cannotHit = null)
        {
            // Spawn explosion
            if (Projectile.owner == Main.myPlayer)
            {
                int cannotHitID = cannotHit != null ? cannotHit.whoAmI : -1;
                int type = ModContent.ProjectileType<DrownedScripturesExplosion>();
                int damage = (int)(Projectile.damage * MINE_EXPLOSION_DAMAGE_MULT);

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, type, damage, Projectile.knockBack, Projectile.owner, DetonationProgress, cannotHitID);
            }
            Projectile.Kill();
        }

        #region Visuals
        private void PassiveEffects()
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(20))
            {
                Vector2 particlePosition = Main.rand.NextVector2Circular(20f, 12f);
                particlePosition.Y += 6;
                float factor = 1f - Math.Abs(particlePosition.X / 24f);
                particlePosition += Projectile.Center;
                Color particleColor = Color.Lerp(DarkColor, LightColor, Main.rand.NextFloat(0.2f, 0.3f) + 0.2f * factor);

                Particle droplet = new DripParticle(particlePosition, particleColor, DarkColor, 1f);
                droplet.Velocity.Y += Math.Max(Projectile.velocity.Y * 0.5f, 0f);

                ParticleHandler.SpawnParticle(droplet);
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.4f, 0.2f, 0.7f) * 0.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float velocityLerp = !Falling ? Math.Min(Projectile.velocity.Length() / 12, 1) : 0;
            float rotation = Utils.AngleLerp(0, Projectile.velocity.ToRotation() + MathHelper.PiOver2, Math.Min(velocityLerp * 2, 1));
            Vector2 scale = new Vector2(1f - 0.7f * velocityLerp, 1 + 0.2f * velocityLerp) * Projectile.scale;

            DrawBloom(Main.spriteBatch, rotation, scale);

            DrawBall(lightColor, rotation, scale);

            return false;
        }

        private void DrawBloom(SpriteBatch spriteBatch, float rotation, Vector2 scale)
        {
            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            Texture2D bloomNoBG = AssetDirectory.CommonTextures.BloomCircleTransparent.Value;

            Color darkGlowColor = new Color(0.1f, 0f, 0.3f, 0.3f);
            Vector2 position = Projectile.Center - Main.screenPosition;

            // Dark glow lerp value and scale
            float darkGlowIntensity = Math.Max(1f - FablesUtils.PolyInEasing((Lifetime - Projectile.timeLeft) / 60f, 0.5f), 0);
            Vector2 darkGlowScale = scale * (1f + darkGlowIntensity * 0.3f);

            // Red glow lerp value and scale
            float brightGlowIntensity = FablesUtils.PolyInEasing(DetonationProgress, 2f);
            float glowScale = 1f + brightGlowIntensity * 0.5f;

            // Draw dark bloom non-premult
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Three dark bloom layers
            Main.EntitySpriteDraw(bloomNoBG, position, null, darkGlowColor, rotation, bloomNoBG.Size() / 2, darkGlowScale * 1.5f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloomNoBG, position, null, darkGlowColor with { A = 127 }, rotation, bloomNoBG.Size() / 2, darkGlowScale * 1.1f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloomNoBG, position, null, Color.Black * 0.4f * brightGlowIntensity, 0, bloomNoBG.Size() / 2, glowScale * 2f, SpriteEffects.None);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Red bloom when detonating
            Main.EntitySpriteDraw(bloom, position, null, GlowColor with { A = 0 } * brightGlowIntensity * 0.4f, 0, bloom.Size() / 2, glowScale, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, position, null, GlowColor with { A = 0 } * brightGlowIntensity * 0.5f, 0, bloom.Size() / 2, glowScale * 0.5f, SpriteEffects.None);
        }

        private void DrawBall(Color lightColor, float rotation, Vector2 scale)
        {
            Texture2D voronoi = AssetDirectory.NoiseTextures.Voronoi.Value;
            Texture2D voronoiNormalMap = AssetDirectory.NoiseTextures.VoronoiNormalMap.Value;

            Vector2 quadSize = new Vector2(60) * scale;
            quadSize *= 1 + 0.6f * FablesUtils.PolyInEasing(DetonationProgress, 5f);

            Effect effect = MineShader;
            effect.Parameters["Scroll"].SetValue(Main.GlobalTimeWrappedHourly * 0.15f + TimeOffset);
            effect.Parameters["Resolution"].SetValue(quadSize * 0.5f);
            effect.Parameters["NoiseTexture"].SetValue(voronoi);
            effect.Parameters["NormalTexture"].SetValue(voronoiNormalMap);

            //inky/corruption colors
            effect.Parameters["DeepColor"].SetValue(DarkColor);
            effect.Parameters["LitColorTone1"].SetValue(LightColor);
            effect.Parameters["LitColorTone2"].SetValue(LightColor2);
            effect.Parameters["GlowColor"].SetValue(GlowColor);

            //crimson colors
            //shader.Parameters["deepColor"].SetValue(new Color(0.2f, 0.1f, 0f, 1f));
            //shader.Parameters["litColorTone1"].SetValue(new Color(0.8f, 0.1f, 0f, 1f));
            //shader.Parameters["litColorTone2"].SetValue(new Color(0.8f, 0.4f, 0.2f, 1f));
            //shader.Parameters["innerGlowColor"].SetValue(new Color(0.9f, 0.7f, 0.2f, 1f));

            //vitriol colors
            //shader.Parameters["deepColor"].SetValue(new Color(0.1f, 0.03f, 0.02f, 1f));
            //shader.Parameters["litColorTone1"].SetValue(new Color(0.5f, 0.2f, 0.1f, 1f));
            //shader.Parameters["litColorTone2"].SetValue(new Color(0.9f, 0.7f, 0.2f, 1f));
            //shader.Parameters["innerGlowColor"].SetValue(new Color(1f, 0.5f, 0.1f, 1f));

            effect.Parameters["Tone2Factor"].SetValue(0.6f);
            effect.Parameters["GlowIntensity"].SetValue(FablesUtils.PolyInEasing(DetonationProgress, 2f) * 1.5f);
            effect.Parameters["ShadingTranslucency"].SetValue(0.7f);
            effect.Parameters["ShadingCurveExponent"].SetValue(1.6f);
            effect.Parameters["ColorDepth"].SetValue(5);
            effect.Parameters["Outline"].SetValue(true);

            // Render the shader to a primitive quad
            MineQuad ??= new PrimitiveQuadrilateral();
            MineQuad.color = lightColor;
            MineQuad.SetPositions(Projectile.Center, quadSize.X, quadSize.Y, rotation);

            // Draw outline
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (i * MathHelper.PiOver2).ToRotationVector2() * 2f;
                MineQuad.Render(effect, offset - Main.screenPosition);
            }

            effect.Parameters["Outline"].SetValue(false);
            MineQuad.Render(effect, -Main.screenPosition);
        }

        #endregion
    }

    public class DrownedScripturesExplosion : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        private float Strength => Projectile.ai[0];
        private int CannotHit => (int)Projectile.ai[1];

        private bool DoSpawnEffects = true;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            if (DoSpawnEffects)
            {
                SoundEngine.PlaySound(MineExplodeSound, Projectile.Center);
                if (Strength >= 1f)
                    SoundEngine.PlaySound(MineExplodeEmpoweredExtraSound, Projectile.Center);

                ExplosionEffects();

                DoSpawnEffects = false;
            }
        }

        public static float ExplosionRadius(float strength) => MathHelper.Lerp(MINE_EXPLOSION_RADIUS_RANGE.X, MINE_EXPLOSION_RADIUS_RANGE.Y, strength);

        public override bool? CanHitNPC(NPC target) => target.whoAmI != CannotHit ? null : false;

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HitDirectionOverride = (target.Center - Projectile.Center).X.NonZeroSign();  // For some reason the direction isnt right by default

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => DrownedScripturesNPCDebuff.ApplyDebuff(target);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => FablesUtils.AABBvCircle(targetHitbox, Projectile.Center, ExplosionRadius(Strength)) && Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.Center.ToVector2(), 1, 1);

        private void ExplosionEffects()
        {
            if (Main.dedServ)
                return;

            // Ring particle
            Particle goopRing = new GoopRing(Projectile.Center, Vector2.Zero, Color.White, ExplosionRadius(Strength), 32, Strength)
            {
                NoLight = false,
                WidthEasing = FablesUtils.PolyInEasing,
                InvertWidthEasing = true,
                WidthEasingDegree = 2f,
                RadiusEasingDegree = 4f
            };

            ParticleHandler.SpawnParticle(goopRing);

            // Droplets
            int particleCount = Main.rand.Next(10, 14);
            for (int i = 0; i < particleCount; i++)
            {
                Vector2 particlePosition = Projectile.Center + Main.rand.NextVector2Circular(12f, 12f);
                Vector2 particleVelocity = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(4f, 8f);
                particleVelocity *= Strength >= 1f ? 1 : 0.8f;

                Color particleColor = Color.Lerp(DrownedScripturesMine.DarkColor, DrownedScripturesMine.LightColor, Main.rand.NextFloat(0.1f, 0.3f));
                float particleWidth = Main.rand.NextFloat(2.5f, 4f);
                int particleLifetime = Main.rand.Next(40, 70);

                Color colorFunction(float progress) => Color.Lerp(DrownedScripturesMine.DarkColor, particleColor, MathF.Pow(progress, 3f));
                float widthFunction(float progress) => particleWidth * progress;

                Particle droplet = new PrimitiveStreak(particlePosition, particleVelocity, colorFunction, widthFunction, 12, particleLifetime)
                {
                    TrailTip = new TriangularTip(8f),
                    Gravity = 0.18f,
                    Acceleration = new Vector2(-0.005f),
                    Collision = true
                };

                ParticleHandler.SpawnParticle(droplet);
            }

            // Weird bubble things
            particleCount = Main.rand.Next(25, 35);
            for (int i = 0; i < particleCount; i++)
            {
                Vector2 particlePosition = Projectile.Center + Main.rand.NextVector2Circular(16f, 16f);
                Vector2 particleVelocity = Main.rand.NextVector2Circular(2.4f, 2.4f);
                particleVelocity *= Strength >= 1f ? 1 : 0.8f;
                Color particleColor = Color.Lerp(DrownedScripturesMine.DarkColor, DrownedScripturesMine.LightColor, Main.rand.NextFloat(0.1f, 0.3f));

                Particle goopBlot = new GoopBlotParticle(particlePosition, particleVelocity, particleColor, 2f, 50);

                ParticleHandler.SpawnParticle(goopBlot);
            }

            // Crazy red droplets
            if (Strength >= 1f)
            {
                particleCount = Main.rand.Next(8, 12);
                for (int i = 0; i < particleCount; i++)
                {
                    Vector2 particlePosition = Projectile.Center + Main.rand.NextVector2Circular(18f, 18f);
                    Vector2 particleVelocity = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(4f, 10f);

                    Color particleColor = DrownedScripturesMine.GlowColor with { G = (byte)Main.rand.Next(80, 100), B = (byte)Main.rand.Next(70, 90) } * Main.rand.NextFloat(1f, 1.3f);
                    float particleWidth = 2f;
                    int particleLifetime = Main.rand.Next(40, 100);

                    Color colorFunction(float progress) => Color.Lerp(particleColor, particleColor * 1.5f, MathF.Pow(progress, 3f));
                    Color outlineColorFunction(float progress) => particleColor with { A = 100 } * 0.3f;
                    float widthFunction(float progress) => particleWidth * progress;

                    Particle glowyDroplet = new PrimitiveStreak(particlePosition, particleVelocity, colorFunction, widthFunction, 12, particleLifetime, outlineColorFunction, true, true, false)
                    {
                        Gravity = 0f,
                        Acceleration = new Vector2(-0.02f - 0.06f * Utils.GetLerpValue(100, 0, particleLifetime, true)),
                        Collision = true,
                        LightColor = DrownedScripturesMine.GlowColor * 0.3f,
                        GlowAtTip = true,
                        FreakyMovement = true
                    };

                    ParticleHandler.SpawnParticle(glowyDroplet);
                }
            }
        }
    }

    public class DrownedScripturesHorror : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        internal static Asset<Texture2D> ColorGradingTexture;

        public ref float AICounter => ref Projectile.ai[0];

        public ref float WaitTimer => ref Projectile.ai[1];

        public enum AIStates
        {
            Flying,
            Dying
        }

        public AIStates State
        {
            get => (AIStates)Projectile.ai[2];
            set {
                // Reset counter when AI state is changed
                AICounter = 0;
                Projectile.netUpdate = true;

                Projectile.ai[2] = (int)value;
            }
        }

        public NPC Target;
        private float MaxSpeed;
        private float LastDistanceLerp = 0f;
        private bool JustSpawned = true;

        private HorrorTentacle[] Tentacles;
        private EasyRenderTarget TentaclesTarget;
        private SlotId TravelSoundSlot;
        private SlotId PowerSoundSlot;

        public float MaxPowerProgress => Utils.GetLerpValue(0, HITS_TO_FULL_POWER, Projectile.numHits, true);
        public int MaxWaitTime => (int)MathHelper.Lerp(HORROR_WAIT_TIME_RANGE.Y, HORROR_WAIT_TIME_RANGE.X, MaxPowerProgress);

        public override void Load() => FablesGeneralSystemHooks.PostUpdateEverythingEvent += UpdateTrail;

        public override void SetStaticDefaults() => HorrorID = Type;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 3000;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.tileCollide = false;
        }

        #region AI
        public override void AI()
        {
            if (JustSpawned)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                SpawnEffects();
                JustSpawned = false;
            }

            if (State == AIStates.Flying)
                FlightAI();

            if (State == AIStates.Dying)
            {
                // Play sound at start of death
                if (AICounter == 0)
                    SoundEngine.PlaySound(HorrorDeath, Projectile.Center);

                // Search for targets during start of anim. Buffer to be more forgiving
                if (AICounter <= HORROR_DEATH_TIME * 0.8f && SwitchTargetAndReset())
                    return;

                Projectile.velocity *= 0.9f;
                DeathEffects();

                if (AICounter > HORROR_DEATH_TIME)
                    Projectile.Kill();
            }

            PassiveEffects();

            if (WaitTimer > 0 && State != AIStates.Dying)
                WaitTimer--;
            else
                AICounter++;
        }

        private void FlightAI()
        {
            if (!ValidTarget(Target))
            {
                // Search for new target, stops here if no valid target was found
                if (!SwitchTargetAndReset())
                    return;
            }

            ManageSounds();

            // Find distance to target and create a lerp value
            float distanceLerp = GetDistanceLerp(out Vector2 directionToTarget, out float _);

            // Gradually angle towards target, angle faster when close to target. Greatly reduced while waiting
            ref float rotation = ref Projectile.rotation;
            float idealRotation = directionToTarget.ToRotation();
            float maxChange = MathHelper.Lerp(HORROR_CURVE_SPEED_RANGE.X, HORROR_CURVE_SPEED_RANGE.Y, distanceLerp) * MathF.Pow(Utils.GetLerpValue(MaxWaitTime, 0, WaitTimer, true), 2);
            rotation = rotation.AngleTowards(idealRotation, maxChange);

            // Update max speed
            MaxSpeed = MathHelper.Lerp(HORROR_SPEED_RANGE.Y, HORROR_SPEED_RANGE.X, distanceLerp) * MathHelper.Lerp(1f, HORROR_MAX_SPEED_MULT, MaxPowerProgress);

            // Create goal velocity
            // Set to zero when waiting, matches target velocity if greater than max speed
            float speed = WaitTimer > 0 ? 0 : Math.Max(Target.velocity.Length(), MaxSpeed);
            Vector2 goalVelocity = speed * rotation.ToRotationVector2();

            // Lerp from current velocity to goal velocity. Acts as acceleration/decel and makes movement curvier
            float velocityLerp = MathHelper.Lerp(0.05f, 0.2f, MathF.Pow(distanceLerp, 2));

            // Momentum decreases when near target to prevent overshooting
            Vector2 momentum = goalVelocity * 0.05f * (1f - distanceLerp);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity + momentum, goalVelocity, velocityLerp);
        }

        private bool ValidTarget(NPC npc) => npc != null && ValidTarget(npc, npc.CanBeChasedBy(Projectile));

        public static bool ValidTarget(NPC npc, bool canBeChased) => canBeChased && npc.GetNPCFlag("DrownedScripturesNPCDebuff");

        private bool SwitchTargetAndReset()
        {
            // Locate a new target
            Target = FablesUtils.FindTarget(Projectile, HORROR_TARGET_RANGE, false, ValidTarget);

            // If a target was found, reset AI state to flying 
            if (Target != null)
            {
                State = AIStates.Flying;
                return true;
            }
            // If there are no valid targets, kill the projectile
            else if (State != AIStates.Dying)
                State = AIStates.Dying;

            return false;
        }

        #endregion

        private float GetDistanceLerp(out Vector2 direction, out float distance)
        {
            direction = Target is null ? Vector2.Zero : Target.Center - Projectile.Center;
            distance = direction.Length();

            // Run normally when flying and not waiting/dying
            if (State == AIStates.Flying && WaitTimer <= 0)
            {
                // Get actual lerp value
                float distanceLerp = Utils.GetLerpValue(HORROR_DISTANCE_LERP.Y, HORROR_DISTANCE_LERP.X, distance, true);
                distanceLerp *= Utils.GetLerpValue(0, 20, AICounter, true); // Limit when AICounter is low
                LastDistanceLerp = distanceLerp;
                return distanceLerp;
            }

            // Otherwise, return 0
            return 0;
        }

        public override bool? CanHitNPC(NPC target) => target == Target && ValidTarget(target) && WaitTimer <= 0;    // Can only hit the current target while not waiting

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target != Target || !ValidTarget(target))
                return;

            // Set wait time
            WaitTimer = MaxWaitTime;

            DrownedScripturesNPCDebuff.RemoveDebuff(target);

            float pitch = MathHelper.Lerp(0f, 0.25f, MathF.Pow(MaxPowerProgress, 0.5f));
            SoundEngine.PlaySound(HorrorBite with { Pitch = pitch }, Projectile.Center);
            new HorroHitSoundPacket(Projectile.Center, pitch: pitch).Send();

            ChompEffects();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.SourceDamage *= MathHelper.Lerp(HORROR_DAMAGE_MULT_RANGE.X, HORROR_DAMAGE_MULT_RANGE.Y, MaxPowerProgress);

        // Update trail in PreUpdateItems so we dont need to add velocity to position, which creates inconsistencies
        private static void UpdateTrail()
        {
            if (Main.dedServ)
                return;

            // Finds each horror and runs manage trail
            foreach (var projectile in Main.ActiveProjectiles)
            {
                if (projectile.ModProjectile is not DrownedScripturesHorror horrorProj)
                    continue;

                horrorProj.ManageTrail();
            }
        }

        private void ManageTrail()
        {
            // Maintain Render target
            TentaclesTarget ??= new(DrawTentacles, TargetActive, () => new(Main.screenWidth / 2, Main.screenHeight / 2), true);
            EasyRenderTargetHandler.TryAddTarget(TentaclesTarget);

            // Maintain tentacle instances
            Tentacles ??= [new HorrorTentacle(15), new HorrorTentacle(15, 0.5f)];

            // Update each tentacle
            foreach (var tentacle in Tentacles)
            {
                // Counter time dependant on percentage of max speed
                float time = Utils.GetLerpValue(0, MaxSpeed, Projectile.velocity.Length());
                tentacle.Update(time, Projectile);

                // Shrink trail when dying
                float dyingProgress = State == AIStates.Dying ? Utils.GetLerpValue(0, HORROR_DEATH_TIME, AICounter, true) : 0f;

                tentacle.MaxOffset = MathHelper.Lerp(tentacle.BaseMaxOffset, 6f, dyingProgress);
                tentacle.TrailWidth = tentacle.BaseTrailWidth * (1f - dyingProgress);
                tentacle.TrailPoints = (int)MathHelper.Lerp(tentacle.BaseTrailPoints, 2, dyingProgress);
            }
        }

        private bool TargetActive() => Projectile != null && Projectile.active;

        private void DrawTentacles(SpriteBatch spriteBatch)
        {
            if (Tentacles is null || Tentacles.Length <= 0 || TentaclesTarget is null || TentaclesTarget.RenderTarget is null)
                return;

            foreach (var tentacle in Tentacles)
                tentacle.DrawTentacle();
        }

        private void ManageSounds()
        {
            // Travel Loop
            if (!SoundEngine.TryGetActiveSound(TravelSoundSlot, out var sound))
                TravelSoundSlot = SoundEngine.PlaySound(HorrorTravelLoop, Projectile.Center);
            if (SoundEngine.TryGetActiveSound(TravelSoundSlot, out sound))
            {
                // Increases volume with velocity
                sound.Position = Projectile.Center;
                sound.Volume = MathHelper.Lerp(0.5f, 1f, Utils.GetLerpValue(0, 20, Projectile.velocity.Length(), true));

                sound.Update();
            }

            // Power Loop
            if (!SoundEngine.TryGetActiveSound(PowerSoundSlot, out sound))
                PowerSoundSlot = SoundEngine.PlaySound(HorrorPowerLoop, Projectile.Center);
            if (SoundEngine.TryGetActiveSound(PowerSoundSlot, out sound))
            {
                // Increases volume with power progress
                sound.Position = Projectile.Center;
                sound.Volume = MathF.Pow(MaxPowerProgress, 0.5f);

                sound.Update();
            }

            // Prevent looping forever
            SoundHandler.TrackSound(TravelSoundSlot);
            SoundHandler.TrackSound(PowerSoundSlot);
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write((byte)(Target is null ? 0 : Target.whoAmI));  // Sync so the monster acts more normal on remote clients

        public override void ReceiveExtraAI(BinaryReader reader) => Target = Main.npc[reader.ReadByte()];

        #region Visuals
        private void PassiveEffects()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.4f, 0.2f, 0.7f) * 0.5f);

            if (Main.rand.NextBool(15))
            {
                Vector2 particlePosition = Main.rand.NextVector2FromRectangle(Projectile.Hitbox);
                Color particleColor = Color.Lerp(DrownedScripturesMine.DarkColor, DrownedScripturesMine.LightColor, Main.rand.NextFloat(0.2f, 0.7f));

                Particle particle = new DripParticle(particlePosition, particleColor, DrownedScripturesMine.DarkColor, 1f);
                ParticleHandler.SpawnParticle(particle);
            }
        }

        private void SpawnEffects()
        {
            // Ring particle
            Particle goopRing = new GoopRing(Projectile.Center + Projectile.velocity * 2f, Vector2.Zero, Color.White, 50f, 28, lifeTime: 30)
            {
                NoLight = false,
                Squish = 0.5f,
                Rotation = Projectile.rotation + MathHelper.PiOver2,
                WidthEasing = FablesUtils.PolyInEasing,
                InvertWidthEasing = true,
                WidthEasingDegree = 2f,
                RadiusEasingDegree = 4f,
                DrawLayer = DrawhookLayer.AbovePlayer
            };

            ParticleHandler.SpawnParticle(goopRing);
        }

        private void ChompEffects()
        {
            Vector2 basePosition = Projectile.Center + Projectile.velocity;

            Particle Jaws = new HorrorJaws(basePosition);
            ParticleHandler.SpawnParticle(Jaws);

            int particleCount = Main.rand.Next(3, 5);
            for (int i = 0; i < particleCount; i++)
            {
                Vector2 particlePosition = Projectile.Center + Main.rand.NextVector2Circular(18f, 18f);
                Vector2 particleVelocity = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(4f, 6f);

                Color particleColor = DrownedScripturesMine.GlowColor with { G = (byte)Main.rand.Next(80, 100), B = (byte)Main.rand.Next(70, 90) } * Main.rand.NextFloat(1f, 1.3f);
                float particleWidth = 2f;
                int particleLifetime = Main.rand.Next(40, 100);

                Color colorFunction(float progress) => Color.Lerp(particleColor, particleColor * 1.5f, MathF.Pow(progress, 3f));
                Color outlineColorFunction(float progress) => particleColor with { A = 0 } * 0.3f;
                float widthFunction(float progress) => particleWidth * progress;

                Particle glowyDroplet = new PrimitiveStreak(particlePosition, particleVelocity, colorFunction, widthFunction, 12, particleLifetime, outlineColorFunction, true, true, false)
                {
                    Gravity = 0f,
                    Acceleration = new Vector2(-0.02f - 0.06f * Utils.GetLerpValue(100, 0, particleLifetime, true)),
                    Collision = true,
                    LightColor = DrownedScripturesMine.GlowColor * 0.3f,
                    GlowAtTip = true,
                    FreakyMovement = true
                };
                ParticleHandler.SpawnParticle(glowyDroplet);
            }
        }

        private void DeathEffects()
        {
            int projSpawnChance = (int)MathHelper.Lerp(2, 10, Utils.GetLerpValue(0, HORROR_DEATH_TIME, AICounter, true));
            if (Main.rand.NextBool(projSpawnChance))
            {
                Vector2 particlePosition = Main.rand.NextVector2FromRectangle(Projectile.Hitbox);
                Color particleColor = Color.Lerp(DrownedScripturesMine.DarkColor, DrownedScripturesMine.LightColor, Main.rand.NextFloat(0.2f, 0.7f));

                Particle particle = new DripParticle(particlePosition, particleColor, DrownedScripturesMine.DarkColor, 1f);
                ParticleHandler.SpawnParticle(particle);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            // Determine animation progress from distance lerp
            float animationProgress = GetDistanceLerp(out _, out _);

            // Specific anim progress for different states
            if (State == AIStates.Dying)
                animationProgress = LastDistanceLerp * FablesUtils.ExpInEasing(Utils.GetLerpValue(HORROR_DEATH_TIME, 0, AICounter, true));
            else if (WaitTimer > 0)
                animationProgress = LastDistanceLerp * FablesUtils.ExpInEasing(Utils.GetLerpValue(0, MaxWaitTime, WaitTimer, true));

            int frameNum = (int)(animationProgress * 4f);
            float glowIntensity = MathF.Pow(animationProgress, 2f);

            // Determine rotation. Use velocity as a direction when possible
            float rotation = Projectile.rotation;
            if (Projectile.velocity != Vector2.Zero)
               rotation = Projectile.velocity.ToRotation();

            // Draw position, shake back and forth when dying
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            if (State == AIStates.Dying)
            {
                Vector2 direction = rotation.ToRotationVector2();
                drawPosition += new Vector2(direction.Y, -direction.X) * 2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 60f);
            }

            // Sprite scale based on time left while dying
            Vector2 scale = Vector2.One;
            if (State == AIStates.Dying)
                scale.Y *= MathF.Pow(Utils.GetLerpValue(HORROR_DEATH_TIME, 0, AICounter, true), 0.5f);

            SpriteBatch spriteBatch = Main.spriteBatch;

            // Draw bloom first
            DrawBloom(spriteBatch, drawPosition, rotation, scale, glowIntensity);

            // Draw base jaw layer
            DrawBaseJaws(spriteBatch, texture, drawPosition, lightColor, rotation, scale, glowIntensity, frameNum);

            // Draw trail
            DrawTentacleTarget(spriteBatch);

            // Draw shadermap jaw layer last
            DrawShaderJaws(spriteBatch, texture, drawPosition, lightColor, rotation, scale, glowIntensity, frameNum);

            return false;
        }

        private static void DrawBaseJaws(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color lightColor, float rotation, Vector2 scale, float glowIntensity, int frameNum)
        {
            ColorGradingTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "DrownedScripturesTeethColorGrading");

            Rectangle frame = texture.Frame(2, 5, 0, frameNum, -2, -2);

            Effect effect = HorrorBaseShader;
            effect.Parameters["OutlineColor"].SetValue(DrownedScripturesMine.DarkColor);
            effect.Parameters["GlowIntensity"].SetValue(glowIntensity);
            effect.Parameters["GradingTexture"].SetValue(ColorGradingTexture.Value);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

            // Draw map layer
            Main.EntitySpriteDraw(texture, position, frame, lightColor, rotation, frame.Size() / 2, scale, SpriteEffects.None);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static void DrawBloom(SpriteBatch spriteBatch, Vector2 position, float rotation, Vector2 scale, float glowIntensity)
        {
            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            Texture2D bloomNoBG = AssetDirectory.CommonTextures.BloomCircleTransparent.Value;

            Color darkGlowColor = new Color(0.1f, 0f, 0.3f, 0.3f);

            Vector2 glowScale = scale * (0.6f + glowIntensity * 0.4f);

            // Draw dark bloom non-premult
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Three dark bloom layers
            Main.EntitySpriteDraw(bloomNoBG, position, null, darkGlowColor, rotation, bloomNoBG.Size() / 2, scale * 1.5f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloomNoBG, position, null, darkGlowColor with { A = 127 }, rotation, bloomNoBG.Size() / 2, scale * 1.1f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloomNoBG, position, null, Color.Black * 0.4f * glowIntensity, rotation, bloomNoBG.Size() / 2, glowScale * 2f, SpriteEffects.None);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Red bloom when near a target
            Main.EntitySpriteDraw(bloom, position, null, DrownedScripturesMine.GlowColor with { A = 0 } * glowIntensity * 0.4f, rotation, bloom.Size() / 2, glowScale, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, position, null, DrownedScripturesMine.GlowColor with { A = 0 } * glowIntensity * 0.5f, rotation, bloom.Size() / 2, glowScale * 0.5f, SpriteEffects.None);
        }

        private static void DrawShaderJaws(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color lightColor, float rotation, Vector2 scale, float glowIntensity, int frameNum)
        {
            Rectangle frame = texture.Frame(2, 5, 1, frameNum, -2, -2);

            Effect effect = HorrorShader;
            effect.Parameters["Scroll"].SetValue(Main.GlobalTimeWrappedHourly * 0.15f);
            effect.Parameters["Resolution"].SetValue(texture.Size() / 2);
            effect.Parameters["FrameSize"].SetValue(frame.Size() / 2);
            effect.Parameters["NoiseTexture"].SetValue(AssetDirectory.NoiseTextures.Voronoi.Value);

            //inky/corruption colors
            effect.Parameters["DeepColor"].SetValue(DrownedScripturesMine.DarkColor);
            effect.Parameters["LitColorTone1"].SetValue(DrownedScripturesMine.LightColor);
            effect.Parameters["LitColorTone2"].SetValue(DrownedScripturesMine.LightColor2);
            effect.Parameters["GlowColor"].SetValue(DrownedScripturesMine.GlowColor);
            effect.Parameters["GlowIntensity"].SetValue(glowIntensity);

            //crimson colors
            //shader.Parameters["deepColor"].SetValue(new Color(0.2f, 0.1f, 0f, 1f));
            //shader.Parameters["litColorTone1"].SetValue(new Color(0.8f, 0.1f, 0f, 1f));
            //shader.Parameters["litColorTone2"].SetValue(new Color(0.8f, 0.4f, 0.2f, 1f));
            //shader.Parameters["innerGlowColor"].SetValue(new Color(0.9f, 0.7f, 0.2f, 1f));

            //vitriol colors
            //shader.Parameters["deepColor"].SetValue(new Color(0.1f, 0.03f, 0.02f, 1f));
            //shader.Parameters["litColorTone1"].SetValue(new Color(0.5f, 0.2f, 0.1f, 1f));
            //shader.Parameters["litColorTone2"].SetValue(new Color(0.9f, 0.7f, 0.2f, 1f));
            //shader.Parameters["innerGlowColor"].SetValue(new Color(1f, 0.5f, 0.1f, 1f));

            effect.Parameters["Tone2Factor"].SetValue(0.6f);
            effect.Parameters["ShadingCurveExponent"].SetValue(1.6f);
            effect.Parameters["ColorDepth"].SetValue(5);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

            // Draw map layer
            Main.EntitySpriteDraw(texture, position, frame, lightColor, rotation, frame.Size() / 2, scale, SpriteEffects.None);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private void DrawTentacleTarget(SpriteBatch spriteBatch)
        {
            if (TentaclesTarget is null || !TentaclesTarget.Initialized || !TentaclesTarget.Active() || TentaclesTarget.RenderTarget is null)
                return;

            spriteBatch.Draw(TentaclesTarget.RenderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
        }

        #endregion
    }
    
    [Serializable]
    public class HorroHitSoundPacket(Vector2 pos, float vol = 1f, float pitch = 0f) : SyncSoundPacket(pos, vol, pitch)
    {
        public override SoundStyle SyncedSound => HorrorBite;
    }

    public class DrownedScripturesNPCDebuff : ModBuff
    {
        public override string Texture => AssetDirectory.Invisible;

        #region Data
        [Serializable]
        public class DrownedScripturesNPCDebuffData : CustomGlobalData
        {
            public int DebuffCooldown = 0;

            [NonSerialized]
            public MarkedRingVFX Mark;
        }

        [Serializable]
        public class DrownedScripturesNPCDebuffDataModule(NPC npc, DrownedScripturesNPCDebuffData data) : FablesNPC.SyncNPCMiscData<DrownedScripturesNPCDebuffData>(npc, data) { }

        #endregion

        public override void Load()
        {
            FablesNPC.PostAIEvent += PostAINPC;
            FablesNPC.PreDrawEvent += PreDrawNPC;
            FablesNPC.PostDrawEvent += PostDrawNPC;
        }

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            BuffID.Sets.CanBeRemovedByNetMessage[Type] = true;
            BuffID.Sets.IsATagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // Ensure the afflicted NPC has the data
            if (!npc.GetNPCData<DrownedScripturesNPCDebuffData>(out _))
                npc.SetNPCData(new DrownedScripturesNPCDebuffData());

            npc.SetNPCFlag(Name);
            PassiveEffects(npc);
        }

        private void PostAINPC(NPC npc)
        {
            // Run on NPCs with the debuff data
            if (!npc.GetNPCData(out DrownedScripturesNPCDebuffData data))
                return;

            ref var mark = ref data.Mark;

            // Stop here without debuff or cooldown. Ensure opacity is 0
            if (!npc.GetNPCFlag(Name) && data.DebuffCooldown <= 0)
            {
                if (mark != null)
                    mark.Opacity = 0;
                return;
            }

            // Make sure the mark is initialized
            if (data.Mark is null)
            {
                int markWidth = (int)Math.Max(npc.frame.Width * npc.scale, npc.width);
                mark = new(markWidth);
            }

            mark.MaintainTarget();

            if (npc.GetNPCFlag(Name))
            {
                // Fade in
                if (mark.Opacity < 1)
                    mark.Opacity = Math.Clamp(mark.Opacity + 0.1f, 0, 1);

                mark.FrontColor = Color.Lerp(DrownedScripturesMine.DarkColor, DrownedScripturesMine.LightColor, 0.5f);
                mark.BackColor = DrownedScripturesMine.DarkColor * 0.8f;
                mark.Scale = 1f;
            }
            else if (data.DebuffCooldown > 0)
            {
                float cooldownProgress = Utils.GetLerpValue(NPC_DEBUFF_COOLDOWN, 0, data.DebuffCooldown);

                // Fade out
                mark.Opacity = 1f - MathF.Pow(Utils.GetLerpValue(0.5f, 1f, cooldownProgress, true), 2);

                mark.FrontColor = Color.White;
                mark.BackColor = Color.LightGray * 0.8f;
                mark.Scale = MathHelper.Lerp(1f, 1.3f, MathF.Pow(cooldownProgress, 2.5f));

                // Last, update cooldown time
                data.DebuffCooldown--;
            }
            else
                mark.Opacity = 0;
        }

        #region Visuals
        private static void PassiveEffects(NPC npc)
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(30))
            {
                Rectangle dripRect = npc.Hitbox;
                dripRect.Offset((-npc.Center).ToPoint());
                Vector2 dropletPos = Main.rand.NextVector2FromRectangle(dripRect);
                float factor = 1f - Math.Abs(dropletPos.X / 24f);
                Color dropletColor = Color.Lerp(DrownedScripturesMine.DarkColor, DrownedScripturesMine.LightColor, Main.rand.NextFloat(0.2f, 0.3f) + 0.4f * factor);
                dropletPos += npc.Center;

                DripParticle ps = new(dropletPos, dropletColor, DrownedScripturesMine.DarkColor, 1f);
                //ps.Velocity.Y += Math.Max(Projectile.velocity.Y * 0.5f, 0f);

                ParticleHandler.SpawnParticle(ps);
            }
        }

        private bool PreDrawNPC(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!npc.GetNPCData(out DrownedScripturesNPCDebuffData data) || data.Mark is null || data.Mark.Opacity <= 0)
                return true;

            data.Mark.DrawMark(spriteBatch, npc.Center, false);
            return true;
        }

        private void PostDrawNPC(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!npc.GetNPCData(out DrownedScripturesNPCDebuffData data) || data.Mark is null || data.Mark.Opacity <= 0)
                return;

            data.Mark.DrawMark(spriteBatch, npc.Center);
        }

        #endregion

        #region Helpers
        /// <summary>
        /// Adds this debuff if the target is not under cooldown.
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        public static bool ApplyDebuff(NPC npc)
        {
            if (npc.GetNPCData(out DrownedScripturesNPCDebuffData data) && data.DebuffCooldown > 0)
                return false;

            npc.AddBuff(ModContent.BuffType<DrownedScripturesNPCDebuff>(), NPC_DEBUFF_DURATION);
            return true;
        }

        /// <summary>
        /// Removes this debuff and applies the debuff cooldown
        /// </summary>
        /// <param name="npc"></param>
        public static void RemoveDebuff(NPC npc)
        {
            if (!npc.GetNPCFlag("DrownedScripturesNPCDebuff"))
                return;

            npc.RemoveBuff(ModContent.BuffType<DrownedScripturesNPCDebuff>());

            if (!npc.GetNPCData(out DrownedScripturesNPCDebuffData data))
            {
                data = new DrownedScripturesNPCDebuffData();
                npc.SetNPCData(data);
            }

            data.DebuffCooldown = NPC_DEBUFF_COOLDOWN;
            npc.SyncNPCData<DrownedScripturesNPCDebuffData, DrownedScripturesNPCDebuffDataModule>();    // Data only needs to be synced when the cooldown is added
        }

        #endregion
    }

    #region Visual Classes
    public class HorrorTentacle(int cycleLength, float cycleOffset = 0, float maxOffset = 12, float trailWidth = 14, int trailLength = 15)
    {
        public float Counter = 0f;
        public float CycleProgress = 0f;

        private readonly int CycleLength = cycleLength;
        private readonly float CycleOffset = cycleOffset;
        public readonly float BaseMaxOffset = maxOffset;
        public readonly float BaseTrailWidth = maxOffset;
        public readonly int BaseTrailPoints = trailLength;

        public float MaxOffset = maxOffset;
        public float TrailWidth = trailWidth;
        public int TrailPoints = trailLength;

        private PrimitiveTrail Trail;
        private List<TentaclePosition> Cache;

        public readonly struct TentaclePosition(Vector2 position, float cyclePosition)
        {
            public readonly Vector2 Position = position;
            public readonly float CyclePosition = cyclePosition;
        }

        public void Update(float time, Projectile projectile)
        {
            // Add provided value to the counter, allows the counter to update faster or slower depending on circumstance
            Counter += time;

            // Update cycle progress based on given time
            CycleProgress = Utils.GetLerpValue(0, CycleLength, (CycleLength * CycleOffset + Counter) % CycleLength, true);

            // Manage Tentacle trail
            ManageTrail(projectile);
        }

        private void ManageTrail(Projectile projectile)
        {
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.Zero);
            Vector2 position = projectile.Center + direction * -10f;

            // Update base cache
            Cache ??= [.. Enumerable.Repeat(new TentaclePosition(position, CycleProgress), TrailPoints)];

            Cache.Add(new TentaclePosition(position, CycleProgress));
            while (Cache.Count > TrailPoints)
                Cache.RemoveAt(0);

            // Create a final list of positions using the cached points and their cycle position
            List<Vector2> positions = [];

            for (int i = 0; i < Cache.Count; i++)
            {
                Vector2 cachePos = Cache[i].Position;
                float cyclePos = Cache[i].CyclePosition;

                // Find base offset using cycle position, creates the spiral pattern
                Vector2 baseOffset = new Vector2(direction.Y, -direction.X) * MaxOffset * MathF.Cos(MathHelper.Pi * 2 * cyclePos);

                // Multiplier for offset based on progress through cache, creates waves in the trail
                float progress = Utils.GetLerpValue(Cache.Count - 1, 0, i, true);
                float mult = 1f + 1.2f * MathF.Exp(-4f * progress) * MathF.Sin(MathHelper.Pi * 4f * progress);

                // Final position
                positions.Add(cachePos + baseOffset * mult);
            }

            /*
            // Modify already existing cache positions
            for (int i = 0; i < Cache.Count; i++)
            {
                Vector2 offsetPosition = new Vector2(direction.Y, -direction.X) * MaxOffset * MathF.Cos(MathHelper.Pi * 2 * CycleProgress);

                float progress = Utils.GetLerpValue(OffsetCache.Count - 1, 0, i, true);
                float mult = 1f + 1.2f * MathF.Exp(-4f * progress) * MathF.Sin(MathHelper.Pi * 4f * progress);

                progress *= 20f;
                progress += Main.GameUpdateCount * 0.02f;
                float randMult = 1f + 0.25f * (MathF.Sin(3f * progress) * MathF.Sin(2f * progress));

                offset.Add(OffsetCache[i] * mult * randMult);
            }

            // Create positions with base cache added to offset cache
            List<Vector2> positions = [];
            for (int i = 0; i < Cache.Count; i++)
                positions.Add(Cache[i] + offset[i]);
            */

            // Create trail and fill positions
            Trail ??= new PrimitiveTrail(30, f => TrailWidth, ColorFunction);
            Trail.SetPositionsSmart(positions, position, FablesUtils.RigidPointRetreivalFunction);
        }

        private Color ColorFunction(float progress)
        {
            int index = (int)MathHelper.Lerp(0, Cache.Count - 1, progress);
            Vector2 cachePos = Cache[index].Position;
            float cyclePos = Cache[index].CyclePosition;

            // Light color from cache pos
            Color lightColor = Lighting.GetColor(cachePos.ToSafeTileCoordinates());

            // Brightness mult determined by cycle position
            float lerp = 0.5f + 0.5f * MathF.Cos(MathHelper.Pi * 2 * cyclePos);
            float brightness = MathHelper.Lerp(0.5f, 1f, MathF.Pow(lerp, 0.5f));

            return (lightColor * brightness) with { A = 255 };
        }

        public void DrawTentacle()
        {
            if (Trail is null)
                return;

            float trailLength = FablesUtils.TrailLength(Trail.Positions);
            Vector2 trailSize = new(trailLength, TrailWidth * 2f);

            Effect effect = HorrorTrailShader;
            effect.Parameters["Scroll"].SetValue(Main.GlobalTimeWrappedHourly * 0.15f + CycleOffset);
            effect.Parameters["Resolution"].SetValue(trailSize / 2);
            effect.Parameters["NoiseTexture"].SetValue(AssetDirectory.NoiseTextures.Voronoi.Value);

            //inky/corruption colors
            effect.Parameters["DeepColor"].SetValue(DrownedScripturesMine.DarkColor);
            effect.Parameters["LitColorTone1"].SetValue(DrownedScripturesMine.LightColor);
            effect.Parameters["LitColorTone2"].SetValue(DrownedScripturesMine.LightColor2);

            //crimson colors
            //shader.Parameters["deepColor"].SetValue(new Color(0.2f, 0.1f, 0f, 1f));
            //shader.Parameters["litColorTone1"].SetValue(new Color(0.8f, 0.1f, 0f, 1f));
            //shader.Parameters["litColorTone2"].SetValue(new Color(0.8f, 0.4f, 0.2f, 1f));

            //vitriol colors
            //shader.Parameters["deepColor"].SetValue(new Color(0.1f, 0.03f, 0.02f, 1f));
            //shader.Parameters["litColorTone1"].SetValue(new Color(0.5f, 0.2f, 0.1f, 1f));
            //shader.Parameters["litColorTone2"].SetValue(new Color(0.9f, 0.7f, 0.2f, 1f));

            effect.Parameters["Tone2Factor"].SetValue(0.6f);
            effect.Parameters["ShadingCurveExponent"].SetValue(1.6f);
            effect.Parameters["ColorDepth"].SetValue(5);
            effect.Parameters["NoiseScale"].SetValue(1.5f);
            effect.Parameters["FadePower"].SetValue(2.5f);
            effect.Parameters["NoiseFadePower"].SetValue(0.65f);

            Trail?.Render(effect, -Main.screenPosition);
        }
    }

    public class GoopRing : PrimitiveRingParticle
    {
        private readonly float TimeOffset;
        private readonly float BaseGlowIntensity;

        public GoopRing(Vector2 position, Vector2 velocity, Color color, float maxRadius, float maxWidth, float glowIntensity = 0f, float opacity = 1f, int lifeTime = 40) : base(position, velocity, color, maxRadius, maxWidth, opacity, lifeTime)
        {
            BaseGlowIntensity = glowIntensity;
            TimeOffset = Main.rand.NextFloat(0, 30);

            MinWidth = 8f;
            MaxWidth = Math.Max(MaxWidth, MinWidth);
        }

        public override void Update()
        {
            base.Update();
            Lighting.AddLight(Position, new Vector3(0.4f, 0.2f, 0.7f) * (1 - LifetimeCompletion));
        }

        public override void ExtraEffects(ref Effect effect)
        {
            effect = ExplosionRingShader;
            effect.Parameters["Scroll"].SetValue(Main.GlobalTimeWrappedHourly * 2.2f + TimeOffset);
            effect.Parameters["Repeats"].SetValue(1f);
            effect.Parameters["NoiseTexture"].SetValue(AssetDirectory.NoiseTextures.Voronoi.Value);
            effect.Parameters["NormalTexture"].SetValue(AssetDirectory.NoiseTextures.VoronoiNormalMap.Value);
            effect.Parameters["NoiseOverlayTexture"].SetValue(AssetDirectory.NoiseTextures.ManifoldRidges.Value);

            //inky/corruption colors
            effect.Parameters["DeepColor"].SetValue(DrownedScripturesMine.DarkColor);
            effect.Parameters["LitColorTone1"].SetValue(DrownedScripturesMine.LightColor);
            effect.Parameters["LitColorTone2"].SetValue(DrownedScripturesMine.LightColor2);
            effect.Parameters["GlowColor"].SetValue(DrownedScripturesMine.GlowColor);

            //crimson colors
            //shader.Parameters["deepColor"].SetValue(new Color(0.2f, 0.1f, 0f, 1f));
            //shader.Parameters["litColorTone1"].SetValue(new Color(0.8f, 0.1f, 0f, 1f));
            //shader.Parameters["litColorTone2"].SetValue(new Color(0.8f, 0.4f, 0.2f, 1f));
            //shader.Parameters["innerGlowColor"].SetValue(new Color(0.9f, 0.7f, 0.2f, 1f));

            //vitriol colors
            //shader.Parameters["deepColor"].SetValue(new Color(0.1f, 0.03f, 0.02f, 1f));
            //shader.Parameters["litColorTone1"].SetValue(new Color(0.5f, 0.2f, 0.1f, 1f));
            //shader.Parameters["litColorTone2"].SetValue(new Color(0.9f, 0.7f, 0.2f, 1f));
            //shader.Parameters["innerGlowColor"].SetValue(new Color(1f, 0.5f, 0.1f, 1f));

            effect.Parameters["Tone2Factor"].SetValue(0.6f);
            effect.Parameters["GlowIntensity"].SetValue(FablesUtils.PolyInEasing(1f - LifetimeCompletion, 3f) * BaseGlowIntensity * 1.8f);
            effect.Parameters["ShadingTranslucency"].SetValue(0.6f);
            effect.Parameters["ShadingCurveExponent"].SetValue(1.4f);
            effect.Parameters["ColorDepth"].SetValue(4);
        }

        public override void DrawPixelated(SpriteBatch spriteBatch)
        {
            Effect effect = null;
            ExtraEffects(ref effect);

            // Draw outline
            effect.Parameters["Outline"].SetValue(true);
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (i * MathHelper.PiOver2).ToRotationVector2() * 2f;
                Ring?.Render(effect, offset - Main.screenPosition);
            }

            effect.Parameters["Outline"].SetValue(false);
            Ring?.Render(effect, -Main.screenPosition);
        }
    }

    public class HorrorJaws : Particle
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "HorrorJaws";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public HorrorJaws(Vector2 position)
        {
            Position = position;

            Velocity = Vector2.Zero;
            Color = Color.White;
            Rotation = Main.rand.NextFloat(-0.3f, 0.3f);
            Scale = 1.2f;
            Lifetime = 15;
        }

        public override void Update()
        {
            Scale = Utils.GetLerpValue(1.5f, 0.9f, MathF.Pow(LifetimeCompletion, 0.5f), true);
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D texture = ParticleTexture;

            // Frame and origin of the jaw halves
            Rectangle topFrame = texture.Frame(1, 2, 0, 1);
            Vector2 topJawOrigin = topFrame.Size() / 2;
            Rectangle bottomFrame = texture.Frame(1, 2, 0, 0);
            Vector2 bottomJawOrigin = bottomFrame.Size() / 2;

            // Jaw displacement from center pos
            Vector2 jawDisplacement = Vector2.UnitY.RotatedBy(Rotation) * Scale;
            jawDisplacement *= Utils.GetLerpValue(1f, 0.25f, MathF.Pow(LifetimeCompletion, 0.5f), true) * 16f;
            Vector2 topJawPosition = Position + jawDisplacement - basePosition;
            Vector2 bottomJawPosition = Position - jawDisplacement - basePosition;

            float drawScale = Scale;
            float drawRotation = Rotation;
            drawRotation += 0.5f * MathF.Pow(Utils.GetLerpValue(7, 0, Time, true), 5f);

            float opacity = Utils.GetLerpValue(1f, 0.8f, LifetimeCompletion, true) * Utils.GetLerpValue(0f, 0.1f, LifetimeCompletion, true);
            Color color = (Color * opacity) with { A = 100 };

            // Draw base layer
            spriteBatch.Draw(texture, topJawPosition, topFrame, color, drawRotation, topJawOrigin, drawScale, SpriteEffects.None, 0);
            spriteBatch.Draw(texture, bottomJawPosition, bottomFrame, color, drawRotation, bottomJawOrigin, drawScale, SpriteEffects.None, 0);

            drawScale *= MathHelper.Lerp(2.5f, 1f, MathF.Pow(LifetimeCompletion, 4f));
            color *= 0.4f * MathF.Pow(1f - LifetimeCompletion, 2f);
            color.A = 0;

            // Draw additive impact layer
            spriteBatch.Draw(texture, topJawPosition, topFrame, color, drawRotation, topJawOrigin, drawScale, SpriteEffects.None, 0);
            spriteBatch.Draw(texture, bottomJawPosition, bottomFrame, color, drawRotation, bottomJawOrigin, drawScale, SpriteEffects.None, 0);
        }
    }

    public class MarkedRingVFX(int maxWidth)
    {
        private EasyRenderTarget RingTarget;

        public Color FrontColor;
        public Color BackColor;

        public float Opacity;
        public float Scale;
        public float Width = maxWidth;

        public readonly float TimeOffset = Main.rand.NextFloat(MathHelper.TwoPi);

        float DrawWidth => Math.Min(RenderTargetSize().Value.X / 2, Width + 8);

        private Point? RenderTargetSize() => new Point((int)((Width + 8f) * 2f), 32);

        /// <summary>
        /// Should be ran every frame to ensure the render target is initialized while in use.
        /// </summary>
        public void MaintainTarget()
        {
            RingTarget ??= new(DrawRingToRenderTarget, () => Opacity > 0, RenderTargetSize, true);
            EasyRenderTargetHandler.TryAddTarget(RingTarget);
        }

        /// <summary>
        /// Draws either the front or back of this marked VFX.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="position"></param>
        /// <param name="front"></param>
        public void DrawMark(SpriteBatch spriteBatch, Vector2 position, bool front = true)
        {
            if (RingTarget is null || !RingTarget.Initialized || !RingTarget.Active() || RingTarget.RenderTarget is null)
                return;

            Vector2 drawPosition = position - Main.screenPosition;

            Point targetSize = RenderTargetSize().Value;
            Rectangle sourceRectangle = new Rectangle(front ? 0 : targetSize.X / 2, 0, targetSize.X / 2, targetSize.Y); // Change source rectangle depending on back or front
            Vector2 origin = new(targetSize.X / 4, targetSize.Y / 2);

            spriteBatch.Draw(RingTarget.RenderTarget, drawPosition, sourceRectangle, Color.White * Opacity, 0, origin, 2f, 0, 0);
        }

        private void DrawRingToRenderTarget(SpriteBatch spriteBatch)
        {
            Point targetSize = RenderTargetSize().Value;

            // Matrices to ensure proper origin
            Matrix translation = Matrix.CreateTranslation(targetSize.X / 4, targetSize.Y / 2, 0);
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, targetSize.X, targetSize.Y, 0, -1, 1);

            // Create and apply shader
            Effect swirlEffect = Scene["DustDevilPrimitive"].GetShader().Shader;
            swirlEffect.Parameters["uWorldViewProjection"].SetValue(translation * projection);

            ConstructPrimitives(out var vertices, out var indices, new(targetSize.X / 2, 0));

            // Apply effect to generated prims
            swirlEffect.CurrentTechnique.Passes[0].Apply();
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3);
        }

        private void ConstructPrimitives(out VertexPositionColorTexture[] vertices, out short[] indices, Vector2 backSideOffset)
        {
            int pointCount = 32;

            int vertexCount = pointCount * 2 + 2;
            int indexCount = pointCount * 6;

            vertices = new VertexPositionColorTexture[vertexCount * 2];
            indices = new short[indexCount * 2];

            for (int i = 0; i < pointCount + 1; i++)
            {
                float progress = (float)i / pointCount;

                float depth = (float)Math.Cos(progress * MathHelper.TwoPi);

                float sizeFactor = (DrawWidth / 36);

                float heightOffset = (float)Math.Sin(progress * MathHelper.TwoPi * 4f * sizeFactor + Main.timeForVisualEffects * 0.15f + TimeOffset);
                heightOffset *= (float)Math.Sin(progress * MathHelper.TwoPi + Main.timeForVisualEffects * 0.05f + TimeOffset) * 1f;
                heightOffset += (float)Math.Cos(progress * MathHelper.TwoPi) * 2f * Math.Min(sizeFactor, 3f);

                Vector2 pointPosition = new((float)Math.Sin(progress * MathHelper.TwoPi) * DrawWidth / 2 * 0.5f, heightOffset);

                pointPosition *= Scale;
                pointPosition *= ((float)Math.Sin(progress * MathHelper.TwoPi * 6f + Main.timeForVisualEffects * 0.2f + TimeOffset) * 0.5f + 0.5f) * 0.1f + 0.9f;
                pointPosition *= (float)Math.Sin(Main.timeForVisualEffects * 0.1f + TimeOffset) * 0.08f / sizeFactor + 1f;

                Vector2 outerThickness = pointPosition * 0.05f / sizeFactor;

                Vector2 bottomVertexPos = pointPosition;
                Vector2 topVertexOffset = outerThickness * 4f * Scale + new Vector2(0, 3);
                Vector2 topVertexPos = pointPosition + topVertexOffset;

                float colorMul = 1f - Math.Abs(depth) * 0.2f;

                vertices[i * 2] = new(bottomVertexPos.Vec3(), BackColor * colorMul, new(1, depth));
                vertices[i * 2 + 1] = new(topVertexPos.Vec3(), FrontColor * colorMul, new(1, depth));

                vertices[i * 2 + vertexCount] = new((bottomVertexPos + backSideOffset).Vec3(), BackColor * colorMul, new(-1, depth));
                vertices[i * 2 + 1 + vertexCount] = new((topVertexPos + backSideOffset).Vec3(), BackColor * colorMul, new(-1, depth));

                if (i < pointCount)
                {
                    //Front side indices
                    indices[i * 6] = (short)((i + 1) * 2);
                    indices[i * 6 + 1] = (short)((i + 1) * 2 + 1);
                    indices[i * 6 + 2] = (short)(i * 2 + 1);
                    indices[i * 6 + 3] = (short)((i + 1) * 2);
                    indices[i * 6 + 4] = (short)(i * 2 + 1);
                    indices[i * 6 + 5] = (short)(i * 2);

                    //Back side indices
                    indices[i * 6 + indexCount] = (short)((i + 1) * 2 + vertexCount);
                    indices[i * 6 + 1 + indexCount] = (short)((i + 1) * 2 + 1 + vertexCount);
                    indices[i * 6 + 2 + indexCount] = (short)(i * 2 + 1 + vertexCount);
                    indices[i * 6 + 3 + indexCount] = (short)((i + 1) * 2 + vertexCount);
                    indices[i * 6 + 4 + indexCount] = (short)(i * 2 + 1 + vertexCount);
                    indices[i * 6 + 5 + indexCount] = (short)(i * 2 + vertexCount);
                }
            }
        }
    }

    #endregion
}