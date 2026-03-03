using CalamityFables.Particles;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public class LingeringSporeGas : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        public ref float MaxTime => ref Projectile.ai[0];
        public ref float Radius => ref Projectile.ai[1];
        public NPC NPC => Main.npc[(int)Projectile.ai[2]];

        public float Progress => (600 - Projectile.timeLeft) / MaxTime;

        Vector2 endPosition;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spore Gas");
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
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            if (Progress == 0)
                endPosition = Projectile.Center;

            if (Progress >= 1f)
            {
                Projectile.Kill();
                return;
            }

            if (NPC.active && NPC.type == ModContent.NPCType<Crabulon>() && NPC.ModNPC is Crabulon crab && crab.SubState == Crabulon.ActionState.Charge_DashForwards)
                endPosition = NPC.Center;

            else
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    float collisionPoint = 0f;
                    if (player.active && Collision.CheckAABBvLineCollision(player.TopLeft, player.Size, Projectile.Center, endPosition, 60f, ref collisionPoint))
                        player.AddBuff(ModContent.BuffType<CrabulonDOT>(), 180);
                }
            }

            GasVisuals();


            Lighting.AddLight(Projectile.Center, new Color(30, 27, 176).ToVector3() * 3 * (1 - Progress));
        }

        public void GasVisuals()
        {
            Vector2 velocityBoost = Vector2.Zero;
            if (NPC.active && NPC.type == ModContent.NPCType<Crabulon>() && NPC.ModNPC is Crabulon crab && crab.SubState == Crabulon.ActionState.Charge_DashForwards)
                velocityBoost = -NPC.velocity * crab.AttackTimer * 0.1f;

            float chance = 0.3f + 0.6f * Utils.GetLerpValue(200f, 600f, Projectile.Center.Distance(endPosition), true);
            if (Main.rand.NextFloat() < chance)
            {
                for (int i = 0; i < 2; i++)
                {
                    float randomAlongLineDistance = Main.rand.NextFloat();
                    Vector2 randomAlongLine = Vector2.Lerp(Projectile.Center, endPosition, randomAlongLineDistance);

                    float smokeSize = Main.rand.NextFloat(3.5f, 3.6f);
                    Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);

                    Vector2 smokeCenter = randomAlongLine + gushDirection * Radius * Main.rand.NextFloat(0.2f, 0.55f);
                    Vector2 velocity = gushDirection * Main.rand.NextFloat(0.6f, 3.6f) * (0.1f + randomAlongLineDistance * 0.9f);
                    Vector2 origin = randomAlongLine;
                    Particle smoke = new SporeGas(smokeCenter, velocity + velocityBoost, origin, Radius, smokeSize, 0.01f);
                    ParticleHandler.SpawnParticle(smoke);
                }
            }

            if (Main.rand.NextBool(4))
            {
                Vector2 randomAlongLine = Vector2.Lerp(Projectile.Center, endPosition, Main.rand.NextFloat());

                Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                Vector2 dustPosition = randomAlongLine + gushDirection * Radius * Main.rand.NextFloat(0.1f, 0.6f);
                Vector2 velocity = gushDirection.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.6f, 3.6f);
                Dust d = Dust.NewDustPerfect(dustPosition, DustID.GlowingMushroom, velocity);
                d.noLightEmittence = true;
            }
        }

    }
}
