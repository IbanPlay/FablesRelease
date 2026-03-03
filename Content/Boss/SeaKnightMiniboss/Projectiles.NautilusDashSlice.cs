namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public class NautilusDashSlice : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Nautilus' complete mastery over his trident");
        }

        public override string Texture => AssetDirectory.Invisible;

        public ref float Width => ref Projectile.ai[0];
        public ref float Height => ref Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 8;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
        }

        public override bool ShouldUpdatePosition() => false;
        public override void AI()
        {
            //Increase the size of the stomp
            Projectile.position = Projectile.Center;
            Projectile.Size = new Vector2(Math.Abs(Width), Math.Abs(Height));
            Projectile.Center = Projectile.position;
        }
    }
}
