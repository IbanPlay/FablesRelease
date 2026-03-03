using CalamityFables.Particles;
using Terraria.DataStructures;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    [ReplacingCalamity("WulfrumKnife")]
    public class ObsidianThrowingDagger : ModItem
    {
        public static readonly SoundStyle ThrowSound = new(SoundDirectory.Wulfrum + "WulfrumKnifeThrowFull") { PitchVariance = 0.4f };
        public static readonly SoundStyle TileHitSound = new(SoundDirectory.Wulfrum + "WulfrumKnifeTileHit", 2) { PitchVariance = 0.4f, MaxInstances = 3 };

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Obsidian Throwing Dagger");
            Tooltip.SetDefault("Gets thrown in a burst of 3 iridescent daggers");
            Item.ResearchUnlockCount = 1;
        }

        public int shootCount = 0;

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.damage = 16;
            Item.crit = 8;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Swing;
            //Clockwork burst
            Item.useAnimation = 10;
            Item.useTime = 4;
            Item.reuseDelay = 24;
            Item.useLimitPerAnimation = 3;

            Item.knockBack = 1f;
            Item.UseSound = ThrowSound;
            Item.autoReuse = true;
            Item.height = 38;
            Item.value = Item.sellPrice(0, 0, 25, 0);
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<ObsidianThrowingDaggerProj>();
            Item.shootSpeed = 4f;
            Item.DamageType = DamageClass.Ranged;
        }


        //Random spread
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity = velocity.RotatedByRandom(shootCount / 2f * MathHelper.PiOver4 * 0.1f);
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.Obsidian, 35).
                AddIngredient(ItemID.ShadowScale, 5).
                AddTile(TileID.Anvils).
                Register();

            CreateRecipe().
                AddIngredient(ItemID.Obsidian, 35).
                AddIngredient(ItemID.TissueSample, 5).
                AddTile(TileID.Anvils).
                Register();
        }
    }


    public class ObsidianThrowingDaggerProj : ModProjectile
    {
        internal PrimitiveTrail TrailDrawer;
        internal List<Vector2> cache;

        public override string Texture => AssetDirectory.EarlyGameMisc + "ObsidianThrowingDaggerProjectile";

        public static Asset<Texture2D> IridescenceTexture;
        public static Asset<Texture2D> OutlineTexture;


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dagger");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }


        public static int Lifetime = 950;
        public float LifetimeCompletion => MathHelper.Clamp((Lifetime - Projectile.timeLeft) / (float)Lifetime, 0f, 1f);

        public float ProjectileHue => (Projectile.whoAmI / 0.21f - Main.GlobalTimeWrappedHourly * 2f) % 1;


        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 6;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity *= 0.998f;

            if (Projectile.timeLeft < Lifetime - 100)
                Projectile.velocity.Y += 0.01f;


            if (Main.dedServ)
                return;

            ManageCache();
            ManageTrail();

            if (Main.rand.NextBool(17))
            {
                Vector2 dustCenter = Projectile.Center + Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-3f, 3f);

                Dust chust = Dust.NewDustPerfect(dustCenter, 15, -Projectile.velocity * Main.rand.NextFloat(0.6f, 1.5f), Scale: Main.rand.NextFloat(1f, 1.4f));
                chust.noGravity = true;

                if (!Main.rand.NextBool(5))
                    chust.noLightEmittence = true;
            }


        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(ObsidianThrowingDagger.TileHitSound, Projectile.Center);
            return base.OnTileCollide(oldVelocity);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            ObsidianDaggerBounceParticle daggerBounce = new ObsidianDaggerBounceParticle(Projectile);
            ParticleHandler.SpawnParticle(daggerBounce);

            if (cache.Count > 4)
            {
                GhostTrail clone = new GhostTrail(cache, TrailDrawer, 0.65f, null, "", null);

                if (!daggerBounce.hitTarget)
                    clone.AttachedEntity = daggerBounce;

                clone.ShrinkTrailLenght = true;
                clone.Pixelated = false;
                clone.DrawLayer = DrawhookLayer.AboveTiles;
                GhostTrailsHandler.LogNewTrail(clone);
            }
        }

        #region Prim trail
        public void ManageCache()
        {
            //Initialize the cache
            if (cache == null)
            {
                cache = new List<Vector2>();

                for (int i = 0; i < 45; i++)
                {
                    cache.Add(Projectile.Center);
                }
            }

            cache.Add(Projectile.Center);

            while (cache.Count > 100)
            {
                cache.RemoveAt(0);
            }

        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction);
            TrailDrawer.SetPositions(cache);
            TrailDrawer.NextPosition = Projectile.Center + Projectile.velocity;
        }

        internal Color ColorFunction(float completionRatio)
        {
            return Main.hslToRgb((ProjectileHue + completionRatio * 0.3f) % 1, 0.4f, 0.5f + completionRatio * 0.2f) * 0.4f * completionRatio;
        }

        internal float WidthFunction(float completionRatio)
        {
            return 3f * completionRatio;
        }

        public void DrawPrims()
        {
            /*Effect effect = AssetDirectory.PrimShaders.GlowingCoreWithOverlaidNoise;
            effect.Parameters["scroll"].SetValue(-Main.GlobalTimeWrappedHourly * 1.75f);
            effect.Parameters["overlayScroll"].SetValue(-Main.GlobalTimeWrappedHourly * 1.1f);

            effect.Parameters["repeats"].SetValue(2f);
            effect.Parameters["overlayRepeats"].SetValue(2f);
            effect.Parameters["coreShrink"].SetValue(0.5f);
            effect.Parameters["coreOpacity"].SetValue(0.5f);
            effect.Parameters["overlayVerticalScale"].SetValue(0.3f);
            effect.Parameters["overlayMaxOpacityOverlap"].SetValue(1f);

            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "ZapTrail").Value);
            effect.Parameters["overlayNoise"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "ZapTrail").Value);
            */

            TrailDrawer?.Render(null, - Main.screenPosition);
        }
        #endregion

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            float hue = ProjectileHue;
            float iridescenceOpacity = Utils.GetLerpValue(1f, 2f, Projectile.velocity.Length(), true);

            DrawPrims();

            float opacitey = MathHelper.Clamp(LifetimeCompletion * 15f, 0f, 1f);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor) * opacitey, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);

            Color outlineColor = Main.hslToRgb(hue % 1, 0.6f, 0.8f);
            OutlineTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "ObsidianThrowingDaggerProjectileOutline");

            outlineColor *= Utils.GetLerpValue(0.3f, 0.1f, LifetimeCompletion, true);
            
            Main.EntitySpriteDraw(OutlineTexture.Value, Projectile.Center - Main.screenPosition, null, outlineColor * opacitey, Projectile.rotation, OutlineTexture.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);


            IridescenceTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "ObsidianThrowingDaggerProjectileIridescence");
            Texture2D iridescence = IridescenceTexture.Value;
            
            for (int i = 0; i < 8; i++)
            {
                Rectangle frame = iridescence.Frame(8, 1, i);
                Color iridesenceColor = Main.hslToRgb(hue % 1, 0.6f, 0.7f) with { A = 0 }; 
                Main.EntitySpriteDraw(iridescence, Projectile.Center - Main.screenPosition, frame, iridesenceColor * iridescenceOpacity, Projectile.rotation, frame.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
                hue += 0.065f;
                //Sudden big shift for swag
                if (i == 4)
                    hue += 0.1f;
            }

            return false;
        }
    }

    public class ObsidianDaggerBounceParticle : Particle
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "ObsidianThrowingDaggerSilouette";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public bool hitTarget;
        public float hue;

        public ObsidianDaggerBounceParticle(Projectile dagger, int lifetime = 40)
        {
            hitTarget = dagger.numHits > 0;
            hue = (dagger.whoAmI / 0.21f - Main.GlobalTimeWrappedHourly * 2f) % 1;

            Position = dagger.Center;
            Scale = dagger.scale;
            Color = Color.White;
            Velocity = dagger.velocity;
            Rotation = dagger.rotation;
            Lifetime = lifetime;

            if (!hitTarget)
            {
                Lifetime = 120;

                //Bounce back
                Velocity.X *= -Main.rand.NextFloat(0.1f, 0.5f);
                if (Math.Abs(Velocity.X) < 1f)
                    Velocity.X = (Main.rand.NextBool() ? -1 : 1) * Main.rand.NextFloat(1f, 3f);

                Velocity.Y = -Main.rand.NextFloat(3.5f, 7f);
            }

            else
            {
                Lifetime = 20;
                Velocity *= 0.15f;
                FrontLayer = false;
            }

        }

        public override void Update()
        {
            //Just slow down and sink into the target
            if (hitTarget)
            {
                Velocity *= 0.97f;
            }

            //Bounce back and spin
            else
            {
                Rotation += Velocity.X * 0.16f;
                Velocity.Y += 0.3f;
                Velocity.X *= 0.98f;

                Scale *= 0.996f;
            }

            hue += 0.02f;

            Color = Lighting.GetColor(Position.ToTileCoordinates());
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D texture = ParticleTexture;
            float opacity = 1 - MathF.Pow(LifetimeCompletion, 2f);
            Color drawColor = Main.hslToRgb(hue % 1, 0.6f, 0.6f);

            if (!hitTarget)
                drawColor = Color.Lerp(Color, drawColor, opacity + 0.1f);

            spriteBatch.Draw(texture, Position - basePosition, null, drawColor * opacity, Rotation, texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);

            if (!hitTarget)
            {
                texture = TextureAssets.Projectile[ModContent.ProjectileType<ObsidianThrowingDaggerProj>()].Value;
                opacity *= Utils.GetLerpValue(0f, 0.2f, LifetimeCompletion, true);

                spriteBatch.Draw(texture, Position - basePosition, null, Color * opacity, Rotation, texture.Size() * 0.5f, Scale, SpriteEffects.None, 0f);

            }
        }
    }
}
