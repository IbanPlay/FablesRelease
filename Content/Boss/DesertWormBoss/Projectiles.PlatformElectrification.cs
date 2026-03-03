using CalamityFables.Content.Dusts;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public class PlatformElectrification : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        internal PrimitiveTrail TrailDrawer;
        private List<Vector2> cache;
        private List<Vector2> trailDisplacements;

        public static int trailLenght = 60;
        public static int remainTime = 30;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("over 2000 volts");
        }

        public override void SetDefaults()
        {
            remainTime = 50;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 600;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return base.Colliding(projHitbox, targetHitbox);
        }
        public override bool CanHitPlayer(Player target)
        {
            return base.CanHitPlayer(target);
        }

        public override bool? CanDamage() => Projectile.timeLeft < remainTime ? false : null;

        public override void AI()
        {
            MoveAlongTiles();
            ManageCache();
            ManageTrail();

            if (Projectile.timeLeft < remainTime)
                Projectile.extraUpdates = 0;

            Color[] prettyColors = new Color[] { Color.DodgerBlue, Color.HotPink, Color.Orange };
            Lighting.AddLight(Projectile.Center, new Vector3(20, 21, 100) * 0.1f);

            if (Main.rand.NextBool(5) || (Projectile.timeLeft > remainTime))
            {
                Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(8f, 8f);
                int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();

                Dust dus = Dust.NewDustPerfect(dustPos, dusType, (dustPos - Projectile.Center) * 0.1f, 70);
                dus.noGravity = true;
                dus.scale = Main.rand.NextFloat(0.2f, 0.4f);

                dus.velocity = -Vector2.UnitY * Main.rand.NextFloat(0.4f, 2f);

                dus.customData = Main.rand.Next(prettyColors);

                if (Projectile.timeLeft > remainTime && Main.rand.NextBool())
                {
                    dus = Dust.NewDustPerfect(Projectile.Center - Vector2.UnitY * 8f + Vector2.UnitX * Main.rand.NextFloat(-8f, 8f), ModContent.DustType<ElectroDust>(), Vector2.Zero, 40);
                    dus.noGravity = true;
                    dus.scale = Main.rand.NextFloat(0.2f, 0.4f);

                    dus.velocity = -Vector2.UnitY * Main.rand.NextFloat(0.2f, 0.5f);
                }
            }
        }

        public void MoveAlongTiles()
        {
            if (Projectile.timeLeft <= remainTime)
                return;

            Point currentTilePos = Projectile.Center.ToSafeTileCoordinates();
            Point nextTilePos = currentTilePos + new Point(Projectile.velocity.X.NonZeroSign(), 0);

            Tile nextTile = Main.tile[nextTilePos];
            if (CanExistOnTile(nextTilePos))
            {
                Projectile.Center = nextTilePos.ToWorldCoordinates();
            }

            else
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    if (CanExistOnTile(nextTilePos + new Point(0, i)))
                    {
                        Projectile.Center = (nextTilePos + new Point(0, i)).ToWorldCoordinates();
                        return;
                    }
                }

                if (CanExistOnTile(nextTilePos + new Point(Projectile.velocity.X.NonZeroSign(), 0)))
                {
                    Projectile.Center = nextTilePos.ToWorldCoordinates();
                    return;
                }

                Projectile.timeLeft = remainTime;
            }
        }

        public bool CanExistOnTile(Point tilePos)
        {
            Tile nextTile = Main.tile[tilePos];
            if (nextTile.HasUnactuatedTile && (TileID.Sets.Platforms[nextTile.TileType] || nextTile.TileType == TileID.PlanterBox))
                return true;

            else if (nextTile.LiquidAmount > 0)
            {
                nextTile = Main.tile[tilePos + new Point(0, -1)];
                return nextTile.LiquidAmount <= 0;
            }

            return false;
        }

        public void ManageCache()
        {
            //Initialize the cache
            if (cache == null)
            {
                cache = new List<Vector2>();
                trailDisplacements = new List<Vector2>();

                for (int i = 0; i < trailLenght; i++)
                {
                    cache.Add(Projectile.Center - Vector2.UnitY * 8f);
                    trailDisplacements.Add(Vector2.UnitY);
                }
            }

            //Add each new position to the trail
            if (Projectile.timeLeft > remainTime)
            {
                cache.Add(Projectile.Center - Vector2.UnitY * 8f);
                trailDisplacements.Add(Vector2.UnitY * Main.rand.NextFloat(-1f, 1f));
            }

            while (cache.Count > trailLenght)
            {
                cache.RemoveAt(0);
                trailDisplacements.RemoveAt(0);
            }

            if (Projectile.timeLeft > remainTime)
                return;

            float remainingPercent = Projectile.timeLeft / (float)remainTime;

            for (int i = 0; i < cache.Count; i++)
            {
                cache[i] += trailDisplacements[i] * (float)Math.Pow(remainingPercent, 0.5f);
            }
        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(60, f =>
            {
                float baseWidth = 6f;
                if (Projectile.timeLeft > remainTime)
                    return baseWidth * (float)Math.Pow(f, 0.5f);

                return baseWidth * (float)Math.Pow(f, 0.5f) * (float)Math.Pow(Projectile.timeLeft / (float)remainTime, 1.2f);

            },
            factor =>
            {
                float trailOpacity = 0.95f;
                float colorLerper = 1f;
                if (Projectile.timeLeft <= remainTime)
                {
                    colorLerper = Projectile.timeLeft / (float)remainTime;
                    trailOpacity *= (float)Math.Pow(Projectile.timeLeft / (float)remainTime, 0.6f);
                }

                if (factor > 0.99f)
                    return Color.Transparent;

                Color trailColor;

                Color baseColor = Color.Lerp(Color.Cyan, Color.White, colorLerper);
                Color endColor = Color.Lerp(Color.DodgerBlue, Color.White, colorLerper);

                trailColor = Color.Lerp(endColor, baseColor, factor);

                return trailColor * trailOpacity;
            });

            TrailDrawer.SetPositionsSmart(cache, Projectile.Center - Vector2.UnitY * 8f, RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.Center - Vector2.UnitY * 8f + Projectile.velocity;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.velocity += -Vector2.UnitY * 8f;
            target.velocity.X *= 0.6f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            TrailDrawer?.Render(null, -Main.screenPosition);
            return false;
        }
    }
}
