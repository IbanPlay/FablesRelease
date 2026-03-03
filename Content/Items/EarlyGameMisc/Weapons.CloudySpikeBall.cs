using CalamityFables.Particles;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class CloudySpikeBall : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cloudy Spike Ball");
            Item.ResearchUnlockCount = 99;
        }
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void SetDefaults()
        {
            Item.damage = 16;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 10;
            Item.height = 30;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.knockBack = 1f;
            Item.value = Item.sellPrice(0, 0, 0, 16);
            Item.rare = ItemRarityID.White;
            Item.shootSpeed = 5f;
            Item.shoot = ModContent.ProjectileType<CloudySpikeBallProjectile>();
            Item.UseSound = SoundID.Item1;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.SpikyBall).
                AddTile(TileID.SkyMill).
                Register();
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity = velocity.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.7f, 1.3f);
        }
    }

    public class CloudySpikeBallProjectile : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cloudy Spiky Ball");
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 3;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.aiStyle = ProjAIStyleID.MoveShort;
            Projectile.timeLeft = 60 * 15; //15 seconds
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool bounced = false;

            if (oldVelocity.X != Projectile.velocity.X)
            {
                Projectile.velocity.X = -oldVelocity.X;
                bounced = true;
            }

            if (oldVelocity.Y != Projectile.velocity.Y)
            {
                Projectile.velocity.Y = -oldVelocity.Y;
                bounced = true;
            }

            if (bounced)
            {
                SoundEngine.PlaySound(SoundID.DoubleJump with { Volume = SoundID.DoubleJump.Volume * 0.2f, MaxInstances = 0 }, Projectile.Center);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 dustOffset = Vector2.UnitY.RotatedBy(i / 8f * MathHelper.TwoPi);
                    Dust.NewDustPerfect(Projectile.Center + dustOffset * 10f, DustID.Cloud, dustOffset * Main.rand.NextFloat(0.2f, 0.7f), 100, default(Color), 1f);
                }
            }

            Projectile.velocity *= 0.7f;
            return false;
        }

        public override bool PreAI()
        {
            Projectile.velocity *= 0.97f;
            Projectile.rotation += Projectile.velocity.X * 0.06f;

            if (Math.Abs(Projectile.velocity.X) < 0.1f)
                Projectile.rotation += Math.Sign(Projectile.rotation) * 0.01f;

            if (Main.rand.NextBool(40))
            {
                Vector2 dustOffset = Main.rand.NextVector2Circular(5f, 5f);
                Dust cloudDust = Dust.NewDustPerfect(Projectile.Center + dustOffset, DustID.Cloud, dustOffset * 0.2f, 100, default(Color), 1.5f);
                cloudDust.velocity *= 0.5f;
            }

            if (Main.rand.NextBool(32 + (int)(Utils.GetLerpValue(0f, 3f, Projectile.velocity.Length(), true)) * 40))
            {
                Vector2 velocity = Main.rand.NextVector2Circular(1f, 1f);
                velocity *= 0.1f;

                float smokeSize = Main.rand.NextFloat(1f, 1.7f);
                Particle smoke = new CloudySmoke(Projectile.Center + Main.rand.NextVector2Circular(25f, 25f), velocity, Color.White * 0.06f, Color.White * 0.15f, Color.LightSkyBlue * 0.08f, smokeSize, 0.002f);
                ParticleHandler.SpawnParticle(smoke);
            }

            return base.PreAI();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DoubleJump with { Volume = SoundID.DoubleJump.Volume * 0.2f, MaxInstances = 0 }, Projectile.Center);

            //Ring of small dust
            for (int i = 0; i < 8; i++)
            {
                Vector2 dustOffset = Vector2.UnitY.RotatedBy(i / 8f * MathHelper.TwoPi);
                Dust.NewDustPerfect(Projectile.Center + dustOffset * 10f, DustID.Cloud, dustOffset * Main.rand.NextFloat(0.2f, 0.7f), 100, default(Color), 1f);
            }

            for (int i = 0; i < 6; i++)
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(3f, 3f) * 10f, DustID.Cloud, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), 100, default(Color), 1f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0, 0);

            return false;
        }
    }


    public class CloudySmoke : Particle
    {
        public override string Texture => AssetDirectory.Particles + "Smoke";

        internal int alpha;
        private Color ColorFire;
        private Color ColorFade;
        private Color ColorEnd;
        private float Spin;
        public override int FrameVariants => 3;
        public override bool SetLifetime => false;

        public CloudySmoke(Vector2 position, Vector2 velocity, Color colorFire, Color colorFade, Color colorEnd, float scale, float rotationSpeed = 0f)
        {
            Position = position;
            Velocity = velocity;
            ColorFire = colorFire;
            ColorFade = colorFade;
            ColorEnd = colorEnd;
            Scale = scale;
            alpha = Main.rand.Next(60);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationSpeed;
            Variant = Main.rand.Next(3);
        }

        public override void Update()
        {
            Rotation += Spin * ((Velocity.X > 0) ? 1f : -1f);

            if (Math.Abs(Velocity.X) > 7)
                Velocity.X *= 0.85f;
            else
                Velocity.X *= 0.92f;

            if (Velocity.Y > 3.5f)
                Velocity.Y = 3.5f;
            else
                Velocity.Y *= 0.95f;

            if (alpha > 100)
            {
                Scale *= 1.01f;
                alpha += 2;

                Velocity.Y -= 0.06f;
                if (Velocity.Y < -0.12f)
                    Velocity.Y = -0.12f;
            }

            else
            {
                Lighting.AddLight(Position, Color.ToVector3() * 0.1f);
                alpha += 4;
            }

            if (alpha >= 255)
                Kill();

            //Shifts from the fire color to the gray color
            if (alpha < 80)
                Color = Color.Lerp(ColorFire, ColorFade, alpha / 80f);
            else if (alpha < 190)
                Color = Color.Lerp(ColorFade, ColorEnd, (float)Math.Pow((alpha - 80) / 110f, 2f));
            else
                Color = ColorEnd;

            Color *= (255 - alpha) / 255f; //Fades with alpha

            Color = Color.MultiplyRGBA(Lighting.GetColor(Position.ToTileCoordinates()));
        }
    }
}