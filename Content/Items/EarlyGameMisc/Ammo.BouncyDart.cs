namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class BouncyDart : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bouncy Dart");
            Tooltip.SetDefault("Bounces between enemies");
            Item.ResearchUnlockCount = 99;
        }
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void SetDefaults()
        {
            Item.damage = 7;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 10;
            Item.height = 30;
            Item.useTime = 24;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.knockBack = 1.5f;
            Item.value = Item.buyPrice(0, 0, 0, 1);
            Item.rare = ItemRarityID.White;
            Item.shootSpeed = 3f;
            Item.shoot = ModContent.ProjectileType<BouncyDartProj>();
            Item.ammo = AmmoID.Dart;
        }

        public override void AddRecipes()
        {
            CreateRecipe(50).
                AddIngredient(ModContent.ItemType<WoodenDart>(), 50).
                AddIngredient(ItemID.PinkGel).
                Register();
        }
    }

    public class BouncyDartProj : ModProjectile
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "BouncyDart";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bouncy Dart");
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 4;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 9999;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
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

            Projectile.penetrate--;
            if (Projectile.penetrate == 0)
                return true;

            Projectile.position += Projectile.velocity;

            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y;
            Projectile.velocity *= 0.9f;

            SoundEngine.PlaySound(SoundID.Item150, Projectile.Center);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.penetrate > 1)
                SoundEngine.PlaySound(SoundID.Item150, Projectile.Center);

            if (Projectile.numHits == 0)
            {
                int[] potentialTargets = new int[10];
                int targetCount = 0;
                int maxBounceDistance = 700;
                int minBounceLenght = 20;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (i == target.whoAmI || !Main.npc[i].CanBeChasedBy(this))
                        continue;

                    float distanceToPotentialTarget = (Projectile.Center - Main.npc[i].Center).Length();
                    if (distanceToPotentialTarget > (float)minBounceLenght && distanceToPotentialTarget < (float)maxBounceDistance && Collision.CanHitLine(Projectile.Center, 1, 1, Main.npc[i].Center, 1, 1))
                    {
                        potentialTargets[targetCount] = i;
                        targetCount++;
                        if (targetCount >= 9)
                            break;
                    }
                }

                if (targetCount > 0)
                {
                    Projectile.velocity = Projectile.DirectionTo(Main.npc[potentialTargets[Main.rand.Next(targetCount)]].Center) * Projectile.velocity.Length() * 1.1f;
                    Projectile.netUpdate = true;
                }

                Projectile.penetrate = 2;
            }
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