using CalamityFables.Core.Visuals;
using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.Localization;
using Terraria.UI.Chat;
using static CalamityFables.Content.Items.EarlyGameMisc.PlasmaRod;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    [ReplacingCalamity("PlasmaRod")]
    public class PlasmaRod : ModItem
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public static Asset<Texture2D> MaxxedManaStarOverlay;
        public static Asset<Texture2D> MaxxedManaStarOutline;

        public static readonly SoundStyle NoitaZapSound = new("CalamityFables/Sounds/PlasmaRodBlast") { Volume = 0.8f };
        public static readonly SoundStyle WeakShootSound = new("CalamityFables/Sounds/PlasmaRodFire", 3) { Volume = 0.6f, PitchVariance = 0.1f, MaxInstances = 0 };
        public static readonly SoundStyle MaxMana = new("CalamityFables/Sounds/PlasmaRodMaxManaOverlay");

        public static float SMALL_BOLT_COST_MULT = 0.8f;
        public static float HOMING_RANGE = 350;
        public static float HOMING_STRENGTH = 0.18f;

        public static float BIG_BOLT_COST_MULT = 12f;
        public static float BIG_BOLT_DAMAGE_BONUS = 7f;
        public static float BIG_BOLT_RANGE = 700f;
        public static float BIG_BOLT_HOMING_RANGE = 275;
        public static float BIG_BOLT_HOMING_STRENGTH = 0.10f;

        public static int FRIED_DURATION = 480;
        public static int FRIED_DAMAGE_BONUS = 4;

        public override void Load()
        {
            FablesNPC.ModifyNPCLootEvent += DropFromGoblins;
            FablesPlayer.OnAchieveFullMana += ShineFullMana;

            if (!Main.dedServ)
            {
                MaxxedManaStarOverlay = ModContent.Request<Texture2D>(AssetDirectory.UI + "MaxMana_Plasma");
                MaxxedManaStarOutline = ModContent.Request<Texture2D>(AssetDirectory.UI + "MaxMana_PlasmaOutline");
            }
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Plasma Rod");
            Tooltip.SetDefault("A powerful staff that waxes and wanes with the user's mana\n"
                             + "Fires huge, searing bolts when you are full on mana, with at least {0} max mana)\n"
                             + "Fires darts of plasma when you don't have full mana\n" +
                             "The plasma darts home in and deal extra damage to enemies marked by the large bolt");
            Item.staff[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = DamageClass.Magic;
            Item.width = 42;
            Item.height = 44;
            Item.useTime = 8;
            Item.useAnimation = 8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.mana = 5;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = null;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PlasmaRodDart>();
            Item.shootSpeed = 10f;
        }

        #region Zappies
        public bool CanBigShoot(Player player) => player.GetFrameStartMana() >= player.statManaMax2 && player.statManaMax2 >= BIG_BOLT_COST_MULT * Item.mana;

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Big chunky noita zap
            if (CanBigShoot(player))
                type = ModContent.ProjectileType<PlasmaRodBlast>();
            else
            {
                type = ModContent.ProjectileType<PlasmaRodDart>();
                velocity += Main.rand.NextVector2Circular(1.5f, 1.5f);
            }

            // Add spawn position offset
            FablesUtils.ShiftShootPositionAhead(ref position, velocity.SafeNormalize(Vector2.Zero), 45);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Big chunky noita zap
            if (CanBigShoot(player))
            {
                CameraManager.Quake += 10;
                if (player.velocity.Length() < 8f)
                {
                    player.velocity -= velocity;
                    NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, Main.myPlayer);
                }
            }
            return true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            // Modifies damage so you can see it in the tooltip
            if (CanBigShoot(player))
                damage *= BIG_BOLT_DAMAGE_BONUS;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            // Both attacks now use a mult so the base cost can be enough to allow for all magic reforges
            mult *= CanBigShoot(player) ? BIG_BOLT_COST_MULT : SMALL_BOLT_COST_MULT;
        }

        public override float UseSpeedMultiplier(Player player)
        {
            // Longer attack cooldown when casting the big bolt
            if (CanBigShoot(player))
                return 0.12f;
            else
                return 1f;
        }

        public override void UseItemFrame(Player player)
        {
            // Item fizz after casting the big bolt (does not work atm)
            if (player.itemAnimation < 60)
            {
                if (Main.rand.NextBool(5))
                {
                    int lights = Dust.NewDust(player.itemLocation, 1, 1, 27, 0f, 0f, 150, default(Color), 0.5f); //light
                    Main.dust[lights].noGravity = true;
                    Main.dust[lights].position = Main.dust[lights].position.MoveTowards(player.itemLocation +   //should really crunch this into a function.
                        (Vector2.UnitX.RotatedBy(player.itemRotation) * Main.rand.NextFloat(40, 56) * player.direction) + //approximate length of staff
                        (Vector2.UnitY.RotatedBy(player.itemRotation) * Main.rand.NextFloat(-6, 6)), 100); //approximate width of ball
                }
                int sparks = Dust.NewDust(player.itemLocation, 1, 1, 205, 0f, 0f, 175);
                Main.dust[sparks].noGravity = true;
                //Main.dust[sparks].velocity = Vector2.UnitY * Main.rand.NextFloat(-2.5f);
                Main.dust[sparks].scale = Main.rand.NextFloat(0.5f + (player.itemAnimation / 30f));
                Main.dust[sparks].position = Main.dust[sparks].position.MoveTowards(player.itemLocation +
                    (Vector2.UnitX.RotatedBy(player.itemRotation) * Main.rand.NextFloat(40, 56) * player.direction) + //approximate length of staff
                    (Vector2.UnitY.RotatedBy(player.itemRotation) * Main.rand.NextFloat(-6, 6)), 100); //approximate width of ball

            }
        }
        #endregion

        #region Tooltip
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BIG_BOLT_COST_MULT * Item.mana);

        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        {
            Player player = Main.LocalPlayer;
            if (CanBigShoot(player) && (line.Name == "Damage" || line.Name == "UseMana") && line.Mod == "Terraria")
            {
                Vector2 basePosition = new Vector2(line.X, line.Y);

                Color alphaPurple = Color.Lerp(Color.MediumOrchid, Color.RoyalBlue, MathF.Sin(Main.GlobalTimeWrappedHourly * 2) * 0.5f + 0.5f);
                //Color alphaPurple = FablesUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.6f, Color.MediumOrchid, Color.DeepPink, Color.RoyalBlue);

                // Bg outline
                for (int i = 0; i < 8; i++)
                {
                    Vector2 outlinePos = basePosition + (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (2f);
                    ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, line.Text, outlinePos, alphaPurple * 0.7f, line.Rotation, line.Origin, line.BaseScale);
                }

                // Draw the main inner text.
                Color mainTextColor = Color.Lerp(alphaPurple, Color.White, 0.8f);
                ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, line.Text, basePosition, mainTextColor, line.Rotation, line.Origin, line.BaseScale);

                return false;
            }
            return true;
        }
        #endregion

        public override void HoldItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
                FablesResourceOverlay.SetOverlay(ShouldDrawManaStarOverlay, DrawManaStarOverlay);
        }

        #region Mana Overlay
        public float MaxManaEffectOpacity = 0;

        private bool ShouldDrawManaStarOverlay(PlayerStatsSnapshot snapshot, bool drawingLife)
        {
            bool drawOverlay = snapshot.Mana == snapshot.ManaMax && !drawingLife;

            if (!drawOverlay && !drawingLife)
                MaxManaEffectOpacity = 0f;
            else if (drawOverlay && MaxManaEffectOpacity < 1f)
            {
                MaxManaEffectOpacity = Math.Max(0.4f, MaxManaEffectOpacity);
                MaxManaEffectOpacity += 0.06f;
                if (MaxManaEffectOpacity > 1f)
                    MaxManaEffectOpacity = 1f;
            }

            return drawOverlay;
        }

        private void DrawManaStarOverlay(ResourceOverlayDrawContext context)
        {
            FablesResourceOverlay.ResourceType assetType = FablesResourceOverlay.GetAssetType(context.texture);

            //Don't draw if not the asset we care about, or in the case of fancy & classic, if we're past the 6 stars at te end of the bar
            if (assetType == FablesResourceOverlay.ResourceType.Other)
                return;

            bool lastStar = context.resourceNumber == context.snapshot.AmountOfManaStars - 1;
            float outlineOpacity = 1f;
            Color overlayColor = (new Color(226, 0, 255) * 1f) with { A = 0 };

            if (context.DisplaySet is HorizontalBarsPlayerResourcesDisplaySet)
                lastStar = false;

            switch (assetType)
            {
                case FablesResourceOverlay.ResourceType.Classic:
                case FablesResourceOverlay.ResourceType.Fancy:
                case FablesResourceOverlay.ResourceType.BarPanel:

                    if (assetType == FablesResourceOverlay.ResourceType.BarPanel)
                    {
                        lastStar = true;
                        outlineOpacity = 1f;
                        context.position.X += 20;
                        context.position.Y += 4;
                    }

                    Color colorCache = context.color;
                    context.texture = MaxxedManaStarOverlay;

                    //Draw the star itself
                    if (lastStar)
                    {
                        context.source = context.texture.Frame(1, 3, 0, 0, 0, -2);
                        context.color *= MaxManaEffectOpacity;
                        context.Draw();
                    }
                    //overlay ontop of nonfinal stars
                    else
                    {
                        context.color = overlayColor;
                        context.color *= 0.7f + 0.3f * (float)Math.Sin(context.resourceNumber * 0.8f - Main.GlobalTimeWrappedHourly * 3f);
                        context.color *= MaxManaEffectOpacity;
                        context.source = context.texture.Frame(1, 3, 0, assetType == FablesResourceOverlay.ResourceType.Fancy ? 1 : 2, 0, -2);
                        context.Draw();
                    }

                    context.color = colorCache;

                    //Frame 0: Classic/Bars outline for the final star
                    //Frame 1: Fancy outline for the final star
                    //Frame 2: Fancy outline for the not final stars, with a hole at the bottom since the stars overlap one another
                    //Frame 3: Classic outline for the not final stars, since the classic stars are shaped slightly differently
                    int frame = assetType != FablesResourceOverlay.ResourceType.Fancy ? (lastStar ? 0 : 3) : (lastStar ? 1 : 2);
                    context.texture = MaxxedManaStarOutline;
                    context.position -= Vector2.One * 2;
                    context.source = MaxxedManaStarOutline.Value.Frame(1, 4, 0, frame, 0, -2);
                    context.color *= outlineOpacity;
                    context.color *= MaxManaEffectOpacity;
                    context.Draw();
                    break;
                case FablesResourceOverlay.ResourceType.BarFill:
                    context.texture = MaxxedManaStarOverlay;
                    context.source = new Rectangle(5, 35, 12, 10);
                    context.color = overlayColor;
                    context.color *= 0.7f + 0.3f * (float)Math.Sin(context.resourceNumber * 0.8f + Main.GlobalTimeWrappedHourly * 3f);
                    context.color *= MaxManaEffectOpacity;
                    context.Draw();
                    break;
            }
        }

        #endregion

        private void ShineFullMana(Player player)
        {
            if (player.HeldItem != null && player.HeldItem.type == Type)
            {
                SoundEngine.PlaySound(MaxMana);
                ParticleHandler.SpawnParticle(new CircularPulseShine(player.Center, new Color(226, 0, 255)));
            }
        }

        private void DropFromGoblins(NPC npc, NPCLoot npcloot)
        {
            if (npc.type == NPCID.GoblinSorcerer)
                npcloot.Add(Type, new Fraction(1, 20));
        }
    }

    public class PlasmaRodDart : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        internal PrimitiveTrail Trail;
        private List<Vector2> Cache;
        private const int TrailLength = 40;

        private float lensFlareScale;

        public ref float HomingRotation => ref Projectile.ai[0];

        public ref float Timer => ref Projectile.ai[1]; // Opting to use a timer over Projectile.TimeLeft since it properly syncs lingering behavior

        public enum AIStates
        {
            Moving,
            Lingering
        }

        public AIStates State
        {
            get => (AIStates)Projectile.ai[2];
            set => Projectile.ai[2] = (float)value;
        }

        public float TimerProgress => State == AIStates.Moving ? Timer / 50f : Timer / 40f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plasma Dart");
        }

        public override void SetDefaults()
        {
            Projectile.width = 3;
            Projectile.height = 3;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.timeLeft = 600;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 1;

            lensFlareScale = 0.3f;
        }

        public override void AI()
        {
            float idealLensFlareScale = Utils.GetLerpValue(0f, 7f, Projectile.velocity.Length(), true);
            lensFlareScale = MathHelper.Lerp(lensFlareScale, idealLensFlareScale, 0.2f);

            // Update status if timer reaches the end
            if (TimerProgress >= 1f)
            {
                if (State == AIStates.Moving)
                    StartLingering();
                else
                    Projectile.Kill();
            }

            if (State == AIStates.Lingering)
                Projectile.velocity *= 0.95f;
            else
            {
                if (TimerProgress == 0)
                {
                    SoundEngine.PlaySound(WeakShootSound, Projectile.Center);

                    // Set homing rotation
                    HomingRotation = Projectile.velocity.ToRotation();

                    // Muzzle flash effects
                    SpawnEffects();
                }

                // Find a potential target and curve towards it gradually
                NPC target = Projectile.FindHomingTarget(HOMING_RANGE, MathHelper.Pi, (target, canBeChased) => canBeChased && target.GetNPCFlag("PlasmaRodFried"));
                if (target is not null)
                {
                    float distanceFromTarget = (target.Center - Projectile.Center).Length();
                    HomingRotation = HomingRotation.AngleTowards((target.Center - Projectile.Center).ToRotation(), HOMING_STRENGTH * MathF.Pow(1 - distanceFromTarget / HOMING_RANGE, 2));
                }

                // Set velocity depending on acceleration and homing rotation
                Projectile.velocity = HomingRotation.ToRotationVector2() * Projectile.velocity.Length();

                PassiveEffects();
            }

            ManageTrail();
            Timer++;
        }

        public override bool ShouldUpdatePosition() => State == AIStates.Moving;

        public override bool? CanDamage() => State == AIStates.Lingering ? false : null;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => StartLingering();

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (target.GetNPCFlag("PlasmaRodFried"))
                modifiers.FlatBonusDamage += FRIED_DAMAGE_BONUS;    //do a bit more damage when we hit a fried enemy
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = oldVelocity;
            StartLingering();
            return false;
        }

        public override void OnKill(int timeLeft) => KillEffects();

        public void StartLingering()
        {
            if (State == AIStates.Lingering)
                return;

            // Set state to lingering
            Timer = 0;
            State = AIStates.Lingering;
            Projectile.penetrate = -1;

            // Disable collision and normal behaviour
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.netUpdate = true;

            if (Cache != null)
            {
                while (Cache.Count > 3 && Cache[0] == Cache[1])
                    Cache.RemoveAt(0);

                // Set last position to the current projectile center
                Cache[^1] = Projectile.Center;
            }
        }

        #region Prims
        public void ManageTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 position = Projectile.Center + Projectile.velocity;

            // Initialize Cache
            Cache ??= [];
            if (Cache.Count == 0)
                for (int i = 0; i < 2; i++)
                    Cache.Add(position);

            // Only add to cache when moving
            if (State == AIStates.Moving)
                Cache.Add(position);

            // Remove some length every tick while lingering
            else if (Cache.Count > 2)
                Cache.RemoveAt(0);

            // Shorten cache if trail is longer than length
            while (Cache.Count > TrailLength)
                Cache.RemoveAt(0);

            // Manage trail
            Trail ??= new PrimitiveTrail(30, WidthFunction, ColorFunction, new RoundedTip());
            Trail.SetPositions(Cache);
            Trail.NextPosition = position + Projectile.velocity;
        }

        private float WidthFunction(float progress)
        {
            float baseWidth = 6f * MathF.Pow(progress, 0.3f);

            if (State == AIStates.Moving)
                return baseWidth;
            else
                return baseWidth * MathF.Pow(1f - TimerProgress, 0.5f);
        }

        private Color ColorFunction(float progress)
        {
            // Pow with a degree between 0 and 1 makes it only fade off near the end of the trail. This makes the trail look very long and stringy.
            float trailOpacity = 0.75f * MathF.Pow(progress, 0.1f);
            Color trailColor;

            if (progress > 0.5f)
                trailColor = Color.Lerp(new Color(78, 5, 177), new Color(255, 96, 255), 2f * (progress - 0.5f));
            else
                trailColor = Color.Lerp(new Color(57, 59, 57), new Color(78, 5, 177), 2f * progress);

            return trailColor with { A = 0 } * trailOpacity;
        }
        #endregion

        #region Visuals
        public override bool PreDraw(ref Color lightColor)  //arcane code i lifted from the toxic blowpipe
        {
            //Draw the trail
            Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;
            effect.Parameters["time"].SetValue(0f);
            effect.Parameters["fadeDistance"].SetValue(0.3f);
            effect.Parameters["fadePower"].SetValue(1 / 16f);
            effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);

            Trail?.Render(effect, -Main.screenPosition);

            //Draw the lens flare
            DrawBloomEffects();

            return false;
        }

        private void DrawBloomEffects()
        {
            Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;
            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

            Vector2 bloomPosition = Projectile.Center - Main.screenPosition - Projectile.velocity * 0.4f;
            Color alphaPurple = FablesUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.8f + Projectile.whoAmI * 0.1f) % 1, Color.MediumOrchid, Color.DeepPink, Color.RoyalBlue) with { A = 0 };

            //Draws 2 layers of circular bloom on the tip of the bolt
            Main.EntitySpriteDraw(bloom, bloomPosition, null, alphaPurple * 0.2f, 0, bloom.Size() / 2f, Projectile.scale * 0.45f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, bloomPosition, null, alphaPurple * 0.6f, 0, bloom.Size() / 2f, Projectile.scale * 0.15f, SpriteEffects.None, 0);

            //Draws 2 layer of lens flare ontop of the bolt, scaling with velocity
            Vector2 squishy = new Vector2(0.75f, 1.5f) * lensFlareScale;
            Main.EntitySpriteDraw(lensFlare, Projectile.Center - Main.screenPosition, null, alphaPurple * 0.3f, MathHelper.PiOver2, lensFlare.Size() / 2f, squishy * Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(lensFlare, Projectile.Center - Main.screenPosition, null, alphaPurple * 0.4f, MathHelper.PiOver2, lensFlare.Size() / 2f, squishy * Projectile.scale * 0.5f, SpriteEffects.None, 0);
        }

        private void SpawnEffects()
        {
            for (int i = 0; i < 6; i++)
            {
                Dust shadowSpark = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 150);    //shadowy
                shadowSpark.velocity = Projectile.velocity.RotatedByRandom(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * shadowSpark.velocity.Length() * 5f;
                shadowSpark.scale = 1f;
                shadowSpark.noGravity = true;
            }
            for (int i = 0; i < 7; i++)
            {
                Dust venomSpark = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.VenomStaff, 0f, 0f, 150);    //venomy
                venomSpark.velocity += Projectile.velocity / 2f;
                venomSpark.noGravity = true;
            }
        }

        private void PassiveEffects()
        {
            Dust shadowSpark = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 100);
            shadowSpark.noGravity = true;
            shadowSpark.scale = 0.8f;
            shadowSpark.velocity *= 1.2f;

            Dust venomSpark = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.VenomStaff, 0f, 0f, 150);
            venomSpark.velocity *= 0.5f;
            venomSpark.scale = 0.5f;
            venomSpark.noGravity = true;
            venomSpark.velocity += Projectile.velocity;
        }

        private void KillEffects()
        {
            for (int i = 0; i < 10; i++)
            {
                Dust shadowSpark = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 150);
                shadowSpark.scale = 0.6f;

                Dust venomSpark = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.VenomStaff, 0f, 0f, 150);
                venomSpark.velocity *= 0.8f;
                venomSpark.scale = 1.2f;
                venomSpark.noGravity = true;
            }
        }
        #endregion
    }

    public class PlasmaRodBlast : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        internal PrimitiveTrail Trail;
        private List<Vector2> Cache;
        private List<Vector2> ZappyCache;

        public ref float HomingRotation => ref Projectile.ai[0];

        public ref float Timer => ref Projectile.ai[1]; // Opting to use a timer over Projectile.TimeLeft since it properly syncs lingering behavior

        public enum AIStates
        {
            Moving,
            Lingering
        }

        public AIStates State
        {
            get => (AIStates)Projectile.ai[2];
            set => Projectile.ai[2] = (float)value;
        }

        public float TimerProgress => State == AIStates.Moving ? Timer / 80f : Timer / 30f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plasma Blast");
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.timeLeft = 600;
            Projectile.alpha = 255;

            Projectile.extraUpdates = 14;
        }

        public override void AI()
        {
            // Update status if timer reaches the end
            if (TimerProgress >= 1f)
            {
                if (State == AIStates.Moving)
                    ExplodeAndLinger();
                else
                    Projectile.Kill();
            }

            // Sit still if lingering
            if (State == AIStates.Lingering)
                Projectile.extraUpdates = 0;
            // Otherwise home towards target and spawn dust
            else
            {
                if (TimerProgress == 0)
                {
                    SoundEngine.PlaySound(NoitaZapSound, Projectile.Center);

                    // Set homing rotation
                    HomingRotation = Projectile.velocity.ToRotation();
                }

                // Find a potential target and curve towards it gradually
                NPC target = Projectile.FindHomingTarget(BIG_BOLT_HOMING_RANGE, MathHelper.Pi);
                if (target is not null)
                {
                    float distanceFromTarget = (target.Center - Projectile.Center).Length();
                    HomingRotation = HomingRotation.AngleTowards((target.Center - Projectile.Center).ToRotation(), BIG_BOLT_HOMING_STRENGTH * MathF.Pow(1 - distanceFromTarget / BIG_BOLT_HOMING_RANGE, 2));
                }

                // Set velocity depending on acceleration and homing rotation
                Projectile.velocity = HomingRotation.ToRotationVector2() * Projectile.velocity.Length();

                PassiveEffects();
            }

            ManageTrail();
            Timer++;
        }

        public override bool ShouldUpdatePosition() => State == AIStates.Moving;

        public override bool? CanDamage() => State == AIStates.Lingering ? false : null;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ExplodeAndLinger(target);
            target.AddBuff(ModContent.BuffType<PlasmaRodFried>(), FRIED_DURATION);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = oldVelocity;
            ExplodeAndLinger();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (State == AIStates.Moving)
                SpawnExplosion();
        }

        public void ExplodeAndLinger(NPC cannotHit = null)
        {
            if (State == AIStates.Lingering)
                return;

            SpawnExplosion(cannotHit);

            // Set state to lingering and reset timer
            Timer = 0;
            State = AIStates.Lingering;
            Projectile.penetrate = -1;

            // Disable collision and normal behaviour
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.netUpdate = true;

            if (Cache != null)
            {
                while (Cache.Count > 3 && Cache[0] == Cache[1])
                    Cache.RemoveAt(0);

                // Set last position to the current projectile center
                Cache[^1] = Projectile.Center;
            }
        }

        private void SpawnExplosion(NPC cannotHit = null)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                int cannotHitID = cannotHit != null ? cannotHit.whoAmI : -1;
                int type = ModContent.ProjectileType<PlasmaRodBoltExplosion>();
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, type, Projectile.damage / 2, Projectile.knockBack, Projectile.owner, cannotHitID);
            }
        }

        #region Prims
        public void ManageTrail()
        {
            if (Main.dedServ)
                return;

            // Initialize the cache
            Cache ??= [.. Enumerable.Repeat(Projectile.Center, 2)];

            // Add each new position to the trail (if moving)
            if (State == AIStates.Moving)
                Cache.Add(Projectile.Center);

            while (Cache.Count > 140)
                Cache.RemoveAt(0);

            // Every 5 frames, the zappy cache gets "reset".
            if (ZappyCache == null || Projectile.timeLeft % 5 == 0)
            {
                ZappyCache = [];
                ZappyCache.Add(Cache[0]);

                // The zappy cache takes every fourth part of the main position cache, and wiggles them a bit randomly for the lightning look
                for (int i = 4; i < Cache.Count; i += 4)
                {
                    float lerper = Utils.GetLerpValue(0, Cache.Count, i);

                    Vector2 point = Cache[i];
                    Vector2 nextPoint = i == Cache.Count - 1 ? Cache[i - 1] : Cache[i + 1];
                    Vector2 dir = Vector2.Normalize(nextPoint - point).RotatedBy(Main.rand.NextBool() ? MathHelper.PiOver2 : -MathHelper.PiOver2);

                    //if were at the tip of the trail or the direction points nowhere, add the trail point
                    if (i > Cache.Count - 3 || dir == Vector2.Zero || float.IsNaN(dir.X))
                        ZappyCache.Add(point);

                    // Add a point thats wiggly. The furthest from the tip, the wiggliest
                    else
                    {
                        float wiggliness = 25 - lerper * 15f;
                        if (State == AIStates.Lingering)
                            wiggliness *= MathF.Pow(1f - TimerProgress, 0.2f); // Wiggles less and less as the projectile fades

                        ZappyCache.Add(point + (dir * Main.rand.NextFloat(wiggliness)));
                    }
                }

                ZappyCache.Add(Cache[^1]);
            }

            // If we are not on a fifth frame, we add a random offset to all the zap trail points, and then also make them move AWAY (lerp negative) from their original position
            else
            {
                for (int i = 0; i < ZappyCache.Count; i++)
                {
                    float lerper = i / (float)ZappyCache.Count;
                    ZappyCache[i] = ZappyCache[i] + Main.rand.NextVector2Circular(8f, 8f) * (1f - MathF.Pow(lerper, 3f));

                    if (i * 4 < Cache.Count)
                        ZappyCache[i] = Vector2.Lerp(ZappyCache[i], Cache[i * 4], -0.1f);
                }
            }

            Trail ??= new PrimitiveTrail(40, WidthFunction, ColorFunction);
            Trail.SetPositionsSmart(ZappyCache, Projectile.Center);
        }

        private float WidthFunction(float progress)
        {
            const float baseWidth = 35f;
            if (State == AIStates.Moving)
                return baseWidth * MathF.Pow(progress, 0.5f);
            else
                return baseWidth * MathF.Pow(progress, 0.5f) * MathF.Pow(1f - TimerProgress, 1.2f);
        }

        private Color ColorFunction(float progress)
        {
            if (State == AIStates.Moving && progress > 0.99f)
                return Color.Transparent;

            float colorLerper = State == AIStates.Lingering ? 1f - TimerProgress : 1f;
            float trailOpacity = 0.75f;
            trailOpacity *= MathF.Pow(colorLerper, 0.1f);

            Color baseColor = Color.Lerp(new Color(255, 96, 255), Color.White, MathF.Pow(colorLerper, 3f));
            Color endColor = Color.Lerp(new Color(78, 5, 177), Color.RoyalBlue, colorLerper);
            Color trailColor = Color.Lerp(endColor, baseColor, progress);

            return trailColor with { A = 0 } * trailOpacity;
        }
        #endregion

        #region Visuals
        public override bool PreDraw(ref Color lightColor)
        {
            if (State == AIStates.Moving && TimerProgress < 0.2f)
                return false;

            // Draw the trail
            Effect effect = AssetDirectory.PrimShaders.TaperedTextureMap;
            effect.Parameters["time"].SetValue(0f);
            effect.Parameters["fadeDistance"].SetValue(1f);
            effect.Parameters["fadePower"].SetValue(1 / 16f);
            effect.Parameters["trailTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);

            Trail?.Render(effect, -Main.screenPosition);

            if (State == AIStates.Lingering)
            {
                Texture2D light = AssetDirectory.CommonTextures.PixelBloomCircle.Value;
                Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

                Vector2 bloomPosition = Projectile.Center - Main.screenPosition;

                Color alphaPurple = FablesUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.8f + Projectile.whoAmI * 0.1f) % 1, Color.MediumOrchid, Color.DeepPink, Color.RoyalBlue) with { A = 0 };
                Color alphaWhite = Color.White with { A = 0 };

                float bloomOpacity = MathF.Pow(1f - TimerProgress, 1.2f);
                float bloomSize = MathF.Pow(1f - TimerProgress, 0.4f);

                //Draws 2 layers of circular bloom on the tip of the bolt
                Main.EntitySpriteDraw(bloom, bloomPosition, null, alphaPurple * bloomOpacity, 0, bloom.Size() / 2f, bloomSize * Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(light, bloomPosition, null, alphaWhite * bloomOpacity, 0, light.Size() / 2f, bloomSize * Projectile.scale * 0.55f, SpriteEffects.None, 0);
            }

            return false;
        }

        private void PassiveEffects()
        {
            if (ZappyCache != null && ZappyCache.Count > 4)
            {
                //Make the dust appear inbetween the recenter points
                int previousPointIndex = ZappyCache.Count - Main.rand.Next(1, 4);
                Vector2 previousPosition = ZappyCache[previousPointIndex];
                Vector2 nextPosition = ZappyCache[previousPointIndex - 1];

                Vector2 position = Vector2.Lerp(previousPosition, nextPosition, Main.rand.NextFloat()) - Projectile.Size / 2;

                Dust venomSparks = Dust.NewDustDirect(position, Projectile.width, Projectile.height, DustID.VenomStaff, 0f, 0f, 150);    //bolt core (venomy)
                venomSparks.noGravity = true;
                venomSparks.velocity = previousPosition.DirectionTo(nextPosition) * 4f;
                venomSparks.scale = 1.5f;

                Dust shadowSparks = Dust.NewDustDirect(position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 150);    //bolt "spray"
                shadowSparks.noGravity = true;
                shadowSparks.velocity = previousPosition.DirectionTo(nextPosition) * 4f;
                shadowSparks.scale = 1.25f;
            }
        }
        #endregion
    }

    public class PlasmaRodBoltExplosion : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        public int CannotHit => (int)Projectile.ai[0];

        public bool DoSpawnEffects = true;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plasma Explosion");

            //MESSAGE BUFFER LINE 1542. 
            //HOSTILE PROJECTILES SPAWNED ON THE CLIENT WILL NOT GET SYNCED.
            //THIS IS THE ONLY USE OF THIS SET
            Main.projHostile[Type] = false;
            ProjectileID.Sets.PlayerHurtDamageIgnoresDifficultyScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 10;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;

            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (DoSpawnEffects)
            {
                for (int i = 0; i < 40; i++)
                {
                    Dust pinkSparks = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Firework_Pink, 0f, 0f, 150, new Color(255, 100, 0));    //lingering sparks
                    pinkSparks.scale = 1f;
                    pinkSparks.velocity *= Main.rand.NextFloat(4f, 7f);
                }
                for (int i = 0; i < 20; i++)
                {
                    Dust shadowSparks = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 150);    //shadowflame sparks
                    shadowSparks.position = Projectile.Center;
                    shadowSparks.velocity = Vector2.Normalize(shadowSparks.velocity);
                    shadowSparks.velocity *= Main.rand.NextFloat(5f, 10f);
                    shadowSparks.scale *= 1.1f;
                    shadowSparks.noGravity = true;

                    Dust venomSparks = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.VenomStaff, 0f, 0f, 150);    //purple sparks
                    venomSparks.velocity = Vector2.Normalize(venomSparks.velocity);
                    venomSparks.velocity *= Main.rand.NextFloat(5f);
                    venomSparks.position = Projectile.Center;
                    venomSparks.scale = 1.2f;
                    venomSparks.noGravity = true;
                }

                DoSpawnEffects = false;
            }
        }

        public override bool? CanHitNPC(NPC target) => !target.friendly && target.whoAmI != CannotHit ? null : false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!target.townNPC)
                target.AddBuff(ModContent.BuffType<PlasmaRodFried>(), FRIED_DURATION);
        }

        public override bool CanHitPlayer(Player target) => (Projectile.Center.Distance(target.Center) < Projectile.width / 2) ? true : false;

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) => modifiers.SourceDamage /= 4;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (!target.mount.Active || !target.mount.Cart)
                target.velocity = (target.Center - Projectile.Center).SafeNormalize(-Vector2.UnitY) * 7.5f;
        }
    }

    public class PlasmaRodFried : ModBuff
    {
        public override string Texture => AssetDirectory.Buffs + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fried");
            Description.SetDefault("Magnetized by a powerful plasma shock");
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
        }

        public override void Update(NPC NPC, ref int buffIndex)
        {
            NPC.SetNPCFlag(Name);

            if (Main.rand.NextBool(5))
            {
                Dust pinkSpark = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Firework_Pink, 0f, 0f, 255, default, 0.6f); //sparkies
                pinkSpark.velocity = Vector2.UnitY * Main.rand.NextFloat(-2.5f);
                pinkSpark.noGravity = true;

                Dust shadowSpark = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, 0f, 0f, 150, default, 0.5f); //light
                shadowSpark.noGravity = true;
            }
        }
    }
}