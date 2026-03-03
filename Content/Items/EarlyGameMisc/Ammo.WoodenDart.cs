namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class WoodenDart : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wooden Dart");
            Item.ResearchUnlockCount = 99;
        }
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void SetDefaults()
        {
            Item.damage = 6;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 10;
            Item.height = 30;
            Item.useTime = 24;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.knockBack = 1.5f;
            Item.value = Item.buyPrice(0, 0, 0, 1);
            Item.rare = ItemRarityID.White;
            Item.shootSpeed = 2f;
            Item.shoot = ModContent.ProjectileType<WoodenDartProj>();
            Item.ammo = AmmoID.Dart;
        }

        public override void AddRecipes()
        {
            CreateRecipe(25).
                AddRecipeGroup(RecipeGroupID.Wood, 2).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }

    public class WoodenDartProj : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "WoodenDart";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wooden Dart");
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 9999;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
        }

        public override void AI()
        {
            // AI 1 behavior
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.velocity.Y < 16)
                Projectile.velocity.Y += 0.1f;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            return base.OnTileCollide(oldVelocity);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int grust = 0; grust < 5; grust++)
            {
                Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, 0, 0f, 0f, 0, default(Color), 0.7f);
            }
        }
    }
}