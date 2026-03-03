namespace CalamityFables.Content.Projectiles
{
    public class HostileDirectStrike : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("a strong blow");
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 0;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = 2;
        }

        // If the AI parameter isn't a valid player slot, it can hit anything. Otherwise it can only hit one player.
        public override bool CanHitPlayer(Player target)
        {
            if (Projectile.ai[0] < 0f || Projectile.ai[0] > Main.maxPlayers || Projectile.ai[0] == target.whoAmI)
                return true;

            return false;
        }

        //If we don't set kncokback to 0 here, the strike will not give custom KB
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            Vector2 knockback = new Vector2(Projectile.ai[1], Projectile.ai[2]);
            if (knockback != Vector2.Zero)
                modifiers.Knockback *= 0;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Vector2 knockback = new Vector2(Projectile.ai[1], Projectile.ai[2]);
            if (knockback != Vector2.Zero)
            {
                target.velocity = knockback;
                target.jump = Player.jumpHeight / 2;
                target.fallStart = (int)(target.position.Y / 16f);
            }
        }
    }
}
