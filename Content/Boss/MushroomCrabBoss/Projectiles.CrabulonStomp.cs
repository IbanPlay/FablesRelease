using CalamityFables.Content.Projectiles;
using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.Localization;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public class CrabulonStomp : GroundStomp, ICustomDeathMessages
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("fungal shockwave");
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Timer == 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    float smokeSize = Main.rand.NextFloat(3.5f, 3.6f);
                    Vector2 smokeCenter;
                    Vector2 velocity;

                    float height = Main.rand.NextFloat();
                    float direction = Main.rand.NextFloatDirection();

                    smokeCenter = Projectile.Center - Vector2.UnitY * height * 20f + Vector2.UnitX * direction * 30f;

                    float velocityRandom = 1 - (float)Math.Pow(Main.rand.NextFloat(), 2f);
                    velocity = Vector2.UnitX * direction * 10f * (0.4f + 2f * velocityRandom);
                    velocity.Y -= height;

                    Particle smoke = new SporeGas(smokeCenter, velocity, Projectile.Center, 122f, smokeSize, 0.01f);
                    ParticleHandler.SpawnParticle(smoke);
                }
            }

            base.AI();
        }

        public override int GoreType() => 375 + Main.rand.Next(3);

        public bool CustomDeathMessage(Player player, ref PlayerDeathReason customDeath)
        {
            customDeath.CustomReason = Language.GetText("Mods.CalamityFables.Extras.DeathMessages.CrabulonDropSlam." + Main.rand.Next(1, 5).ToString()).ToNetworkText(player.name);
            return true;
        }

        public override bool CanHitPlayer(Player target) => true;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.velocity += new Vector2((target.Center.X - Projectile.Center.X).NonZeroSign() * 13f, -9f);
        }

        public override void TileStompEffect(int i, int j, Tile t, bool solidTop, float distance)
        {
            if (!solidTop && Main.tileSolid[t.TileType])
            {
                float scale = (float)Math.Pow(Utils.GetLerpValue(0f, MaxDiameter * 8f, distance, true), 1.6f);
                float heightScale = scale * Utils.GetLerpValue(MaxDiameter * 8f, MaxDiameter * 8f - 48f, distance, true);

                ParticleHandler.SpawnParticle(new BouncingTileParticle(new Point(i, j), (int)(scale * 20), 20, 8f + 16f * heightScale));
            }
        }
    }
}
