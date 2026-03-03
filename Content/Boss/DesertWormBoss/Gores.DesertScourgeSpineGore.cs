using CalamityFables.Particles;
using Terraria.DataStructures;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    [Autoload(false)]
    public class AutoloadedScourgeSpineGore : ModGore
    {
        public override string Texture => AssetDirectory.DesertScourge + "Gores/" + Name;
        public override string Name => InternalName != "" ? InternalName : base.Name;

        public string InternalName;

        public AutoloadedScourgeSpineGore(string name)
        {
            InternalName = name;
        }

        public override void SetStaticDefaults()
        {
            GoreID.Sets.DrawBehind[Type] = true;
            ChildSafety.SafeGore[Type] = true;
        }

        public override void OnSpawn(Gore gore, IEntitySource source)
        {
            gore.behindTiles = true;

            for (int i = 0; i < 4; i++)
            {
                Vector2 dustPos = gore.position + new Vector2(gore.Width, gore.Height) * 0.5f + Main.rand.NextVector2Circular(30f, 30f);
                Dust d = Dust.NewDustPerfect(dustPos, 284, -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4 * 0.3f) * 1f, 0, Color.Tan);
            }

            gore.position -= new Vector2(gore.Width / 2, gore.Height / 2);
        }

        public CurveSegment MoreAndMoreDust = new(LinearEasing, 0, 0.5f, 0.3f);
        public CurveSegment AbruptStop = new(PolyOutEasing, 0.5f, 0.7f, -0.7f, 3f);

        public float DustChance(Gore gore) => PiecewiseAnimation(gore.alpha / 255f, MoreAndMoreDust, AbruptStop);

        public override bool Update(Gore gore)
        {
            int shakeTreshold = 0;
            int fallTreshold = -50;


            int fadeTreshold = -60;



            if (Main.rand.NextBool(2) && Main.rand.NextFloat() < DustChance(gore) || gore.velocity.Y != 0)
            {
                Vector2 dustPos = gore.position + new Vector2(gore.Width, gore.Height) * 0.5f + Main.rand.NextVector2Circular(30f, 30f);
                Vector2 dustVelocity = Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4 * 0.3f);
                if (Main.rand.NextBool())
                    dustVelocity.Y *= -1;

                dustVelocity += gore.velocity;
                Dust.NewDustPerfect(dustPos, 284, dustVelocity, 0, Color.Tan);
            }

            if (Main.rand.NextBool(2) && Main.rand.NextFloat() < DustChance(gore) && true || gore.velocity.Y != 0)
            {
                Vector2 dustPos = gore.position + new Vector2(gore.Width, gore.Height) * 0.5f + Main.rand.NextVector2Circular(30f, 30f);
                Vector2 dustVelocity = Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4 * 0.3f);
                Dust.NewDustPerfect(dustPos, 26, dustVelocity, 0, Color.White, Main.rand.NextFloat(0.2f, 1f));
            }


            gore.timeLeft--;
            if (gore.timeLeft > 0)
                return false;

            //
            //gore.rotation +=
            //gore.velocity = Collision.TileCollision(gore.position, gore.velocity, (int)((float)gore.Width * gore.scale), (int)((float)gore.Height * gore.scale));


            gore.position += gore.velocity;

            if (gore.timeLeft < fadeTreshold)
                gore.alpha += 2 * GoreID.Sets.DisappearSpeedAlpha[gore.type];

            if (gore.timeLeft < shakeTreshold && gore.timeLeft > fallTreshold)
                gore.drawOffset = Main.rand.NextVector2Circular(2f, 2f) * Utils.GetLerpValue(shakeTreshold, fallTreshold, gore.timeLeft, true);

            else if (gore.timeLeft <= fallTreshold)
            {
                gore.velocity.Y += 0.3f * Utils.GetLerpValue(fallTreshold, fallTreshold - 20, gore.timeLeft, true);



                if (Collision.SolidCollision(gore.position, (int)gore.Width, 1))
                {
                    //If was already in tile, just silently break
                    if (gore.timeLeft == fallTreshold)
                    {
                        gore.active = false;
                        return false;
                    }

                    Vector2 goreCenter = gore.position + new Vector2(gore.Width, 0) * 0.5f;
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 dustPos = goreCenter + Main.rand.NextVector2Circular(40f, 10f);
                        Vector2 dustVelocity = (goreCenter + Vector2.UnitY * 40f).DirectionTo(dustPos) * Main.rand.NextFloat(4f, 8f);
                        Dust d = Dust.NewDustPerfect(dustPos, 284, dustVelocity, 0, Color.Tan, Main.rand.NextFloat(1f, 2f));
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 dustPos = goreCenter + Main.rand.NextVector2Circular(40f, 10f);
                        Vector2 dustVelocity = -Vector2.UnitY * Main.rand.NextFloat(3f, 6f);
                        Dust d = Dust.NewDustPerfect(dustPos, 284, dustVelocity, 0, Color.Tan, Main.rand.NextFloat(2f, 3f));
                        d.noGravity = true;
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 dustPos = goreCenter + Main.rand.NextVector2Circular(40f, 10f);
                        Vector2 dustVelocity = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2 * 0.8f) * Main.rand.NextFloat(3f, 6f);
                        Dust d = Dust.NewDustPerfect(dustPos, 26, dustVelocity, 0, Color.White, Main.rand.NextFloat(0.8f, 1.3f));
                    }

                    for (int i = 0; i < 9; i++)
                    {
                        Vector2 dustPos = goreCenter + Main.rand.NextVector2Circular(30f, 10f);
                        Vector2 dustVelocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 3f);
                        dustVelocity += goreCenter.SafeDirectionTo(dustPos) * 2f;

                        dustPos += Vector2.UnitY * 16f;

                        Color dustColor = new Color(133, 122, 94);
                        SmokeParticle sandsmoke = new SmokeParticle(dustPos, dustVelocity, dustColor * 0.8f, dustColor * 0.4f, Main.rand.NextFloat(0.8f, 1.3f), 0.8f, 40, 0.03f);
                        ParticleHandler.SpawnParticle(sandsmoke);
                    }

                    Collision.HitTiles(gore.position - Vector2.UnitY * 10f, gore.velocity, (int)gore.Width, 3);

                    SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt, gore.position);
                    gore.active = false;
                }
            }

            if (gore.alpha >= 255)
                gore.active = false;
            return false;
        }
    }

}
