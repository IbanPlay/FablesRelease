using CalamityFables.Content.Items.EarlyGameMisc;
using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.Localization;
using static CalamityFables.Content.Items.Sky.SkyDart;

namespace CalamityFables.Content.Items.Sky
{
    public class SkyDart : ModItem
    {
        public override string Texture => AssetDirectory.SkyItems + Name;

        public static float MAX_RANGE = 650f; // Where acceleration and damage increasing ends
        public static float MIN_RANGE = 250f; // Where acceleration and damage increasing begins
        public static float MAX_SPEED_MULT = 2f;
        public static float ACCELERATION_POWER = 1.8f;
        public static float MAX_DAMAGE_BONUS = 0.25f;

        public static float HOMING_RANGE = 300;
        public static Vector2 HOMING_STRENGTH = new(0.03f, 0.09f); // Increases at longer range

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 99;
        }

        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 7;
            Item.height = 14;
            Item.useTime = 24;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.knockBack = 2;
            Item.value = Item.sellPrice(copper: 4);
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 1;
            Item.shoot = ModContent.ProjectileType<SkyDartProjectile>();
            Item.ammo = AmmoID.Dart;
        }

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MAX_DAMAGE_BONUS);

        public override void AddRecipes()
        {
            CreateRecipe(50).
                AddIngredient(ItemID.Feather).
                AddRecipeGroup(FablesRecipes.AnyGoldBarGroup).
                Register();
        }
    }

    public class SkyDartProjectile : ModProjectile
    {
        public override string Texture => AssetDirectory.SkyItems + "SkyDart";
        private static Asset<Texture2D> AfterimagesTexture;

        private List<Vector2> Cache;
        private List<Vector2> TrailCache;
        private PrimitiveTrail Trail;

        public ref float DistanceTraveled => ref Projectile.ai[0];
        public ref float InitialSpeed => ref Projectile.ai[1];
        public ref float HomingRotation => ref Projectile.ai[2];

        private int SpeedEffectCounterMax => 20 * Projectile.MaxUpdates;
        private int SpeedEffectCounter = -1;

        private float RangeCompletion => Utils.GetLerpValue(MIN_RANGE, MAX_RANGE, DistanceTraveled, true);

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 600 * Projectile.MaxUpdates;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Reduce velocity based on max updates and set initial velocity to that value
            Projectile.velocity /= Projectile.MaxUpdates;
            InitialSpeed = Projectile.velocity.Length();

            // Set homing rotation, seperate from the projectile rotation since that's used for drawing
            HomingRotation = Projectile.velocity.ToRotation();
        }

        public override void AI()
        {
            // Track distance
            DistanceTraveled += Projectile.velocity.Length();

            // Find a potential target and curve towards it gradually
            NPC target = Projectile.FindHomingTarget(HOMING_RANGE);
            if (target is not null)
            {
                float distanceFromTarget = (target.Center - Projectile.Center).Length();
                float homingStrength = MathHelper.Lerp(HOMING_STRENGTH.X, HOMING_STRENGTH.Y, RangeCompletion);
                HomingRotation = HomingRotation.AngleTowards((target.Center - Projectile.Center).ToRotation(), homingStrength * MathF.Pow(1 - distanceFromTarget / HOMING_RANGE, 2));
            }

            // Set velocity depending on acceleration and homing rotation
            Projectile.velocity = HomingRotation.ToRotationVector2() * MathHelper.Lerp(InitialSpeed, InitialSpeed * MAX_SPEED_MULT, MathF.Pow(RangeCompletion, ACCELERATION_POWER));

            // Set rotation depending on homing rotation, plus offset
            Projectile.rotation = HomingRotation + MathHelper.PiOver2;

            PassiveEffects();
            ManageCache();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.ScalingBonusDamage += MAX_DAMAGE_BONUS * RangeCompletion;

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            return true;
        }

        #region Prims
        public void ManageCache()
        {
            // Track cache for afterimages when above min range
            if (DistanceTraveled <= MIN_RANGE || Main.dedServ)
                return;

            Vector2 offset = Projectile.velocity.SafeNormalize(Vector2.Zero) * -16;
            Vector2 position = Projectile.Center + Projectile.velocity + offset;

            Cache ??= [];
            if (Cache.Count == 0)
                for (int i = 0; i < 2; i++)
                    Cache.Add(position);

            Cache.Add(position);
            while (Cache.Count > 12)
                Cache.RemoveAt(0);

            // Track cache for trail when speed effects have been triggered
            if (SpeedEffectCounter <= 0)
                return;

            TrailCache ??= [];
            if (TrailCache.Count == 0)
                for (int i = 0; i < 2; i++)
                    TrailCache.Add(position);

            TrailCache.Add(position);
            while (TrailCache.Count > 24)
                TrailCache.RemoveAt(0);

            // Manage trail
            Trail ??= new PrimitiveTrail(30, WidthFunction, ColorFunction);
            Trail.SetPositions(TrailCache);
        }

        private float WidthFunction(float progress) => progress * 10f;

        private Color ColorFunction(float progress) 
        {
            // Find lighting at trail position
            Vector2 position = TrailCache[(int)(progress * (TrailCache.Count - 1))];
            Color lighting = Lighting.GetColor(position.ToTileCoordinates());

            // Find color and opacity based on trail position and time left in speed effects
            Color color = new Color(213, 234, 231).MultiplyRGB(lighting);
            float trailOpacity = 0.5f * MathF.Pow(progress, 2) * SpeedEffectCounter / SpeedEffectCounterMax;
            return color * trailOpacity;
        }
        #endregion

        #region Visuals
        private void PassiveEffects()
        {
            // Feather Particles
            if (Main.rand.NextBool(30 * Projectile.MaxUpdates))
            {
                // Add random rotation to the particles. The particles will curve back towards the dart
                Vector2 particlePosition = Projectile.Center + Main.rand.NextVector2Circular(8, 8);
                float rotation = Main.rand.NextFloat(-0.25f, 0.25f);
                Vector2 particleVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(rotation) * InitialSpeed * Main.rand.NextFloat(2f, 2.5f);
                int gravDirection = (rotation * Projectile.velocity.X).NonZeroSign();

                ParticleHandler.SpawnParticle(new FeatherParticle(particlePosition, particleVelocity, Main.rand.NextFloat(0.75f, 1), null, gravDirection * -0.1f, gravDirection > 0));
            }

            // Max Speed effect
            if (SpeedEffectCounter == -1 && RangeCompletion >= 1)
            {
                // Create ring particle 
                PrimitiveRingParticle ring = new SoundwaveRing(Projectile.Center, Vector2.Zero, new Color(189, 211, 255), new Color(105, 155, 255), Main.rand.Next(22, 27), 10, 2, 0.75f, 30)
                {
                    RadiusEasingDegree = 4,
                    Repeats = 1,
                    NoLight = false
                };
                ring.Squash(Projectile.rotation, 0.5f);
                ParticleHandler.SpawnParticle(ring);

                // Create several cloud dusts
                int dustAmount = Main.rand.Next(4, 8);
                for (int i = 0; i < dustAmount; i++)
                {
                    Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f);
                    Vector2 dustVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotateRandom(1.2f) * Main.rand.NextFloat(1f, 1.5f);
                    float dustScale = Main.rand.NextFloat(1.3f, 1.7f);
                    Dust.NewDustPerfect(dustPosition, DustID.Cloud, dustVelocity, 0, Color.White, dustScale);
                }

                // Set effects timer for extra particles and a trail
                SpeedEffectCounter = SpeedEffectCounterMax;
            }

            // Create cloud particles after speed effects have been triggered
            if (SpeedEffectCounter > 0)
            {
                // Dust spawn chance is higher when the timer just started 
                float counterProgress = Utils.GetLerpValue(SpeedEffectCounterMax, 0, SpeedEffectCounter);
                int dustChance = (int)MathHelper.Lerp(2, 6, counterProgress);
                if (Main.rand.NextBool(dustChance))
                {
                    Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f);
                    Vector2 dustVelocity = Projectile.velocity * Main.rand.NextFloat(0.1f, 0.2f);
                    float dustScale = MathHelper.Lerp(1f, 0.25f, counterProgress) * Main.rand.NextFloat(1f, 1.6f);
                    Dust.NewDustPerfect(dustPosition, DustID.Cloud, dustVelocity, 0, Color.White, dustScale);
                }

                // Decrement counter
                SpeedEffectCounter--;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Create afterimages
            if (DistanceTraveled > MIN_RANGE)
            {
                AfterimagesTexture ??= ModContent.Request<Texture2D>(Texture + "Trail");
                int cacheCount = Cache.Count;
                int Afterimages = Math.Min(Cache.Count, 6);

                for (int i = 0; i < Afterimages; i++)
                {
                    // Finds cache index based on loop iterations
                    int index = (int)((cacheCount - 1) * Utils.GetLerpValue(0, Afterimages, i));
                    Vector2 position = Cache[index];

                    float afterimageProgress = i / (float)Afterimages;

                    Rectangle frame = AfterimagesTexture.Frame(1, 3, 0, (int)(3f * (1f - afterimageProgress)));
                    Color color = Projectile.GetAlpha(lightColor) * afterimageProgress;

                    Main.EntitySpriteDraw(AfterimagesTexture.Value, position - Main.screenPosition, frame, color, Projectile.rotation, frame.Size() / 2, Projectile.scale, SpriteEffects.None);
                }
            }
            // Create speed trail
            if (Trail is not null && SpeedEffectCounter > 0 && !Projectile.GetProjectileData<InbuedDartProjectileHandling.ImbuedDartProjectileData>(out _))
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

                Trail?.Render(effect, -Main.screenPosition);
            }
            return true;
        }
        #endregion
    }

    public class FeatherParticle : Particle
    {
        public override string Texture => AssetDirectory.SkyItems + "FeatherParticle";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;
        public override int FrameVariants => 3;

        private float Opacity;
        private float Gravity;
        private SpriteEffects SpriteEffect;

        public FeatherParticle(Vector2 position, Vector2 velocity, float scale = 1, int? lifetime = null, float gravity = 0.1f, bool? flipped = null)
        {
            Position = position;
            Velocity = velocity;
            Scale = scale;
            Color = Color.White;
            Lifetime = lifetime is null ? Main.rand.Next(30, 60) : lifetime.Value;
            Variant = Main.rand.Next(3);
            Gravity = gravity;
            bool flip = flipped is null ? Main.rand.NextBool() : flipped.Value;
            SpriteEffect = flip ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        }

        public override void Update()
        {
            Velocity *= 0.98f;
            Velocity.Y += Gravity;

            if (velocity.Length() > 0)
                Rotation = velocity.ToRotation() - MathHelper.PiOver2;

            // Despawn faster if a tile is in the path of thee particle
            if (!FablesUtils.CanHitLine(Position, Position + Velocity, true) && Lifetime > 10)
                Lifetime = 10;

            Opacity = Utils.GetLerpValue(1f, 0.5f, LifetimeCompletion, true);
            Color = Lighting.GetColor(Position.ToSafeTileCoordinates()) * Opacity;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D texture = ParticleTexture;
            Rectangle frame = texture.Frame(FrameVariants, 1, Variant, 0, -2);
            spriteBatch.Draw(texture, Position - basePosition, frame, Color, Rotation, frame.Size() * 0.5f, Scale, SpriteEffect, 0f);
        }
    }
}