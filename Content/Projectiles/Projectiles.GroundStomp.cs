namespace CalamityFables.Content.Projectiles
{
    public class GroundStomp : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("a shockwave");
        }

        public override string Texture => AssetDirectory.Invisible;

        public ref float Timer => ref Projectile.ai[0];
        public ref float MaxDiameter => ref Projectile.ai[1];
        public virtual float Height => 95;

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
        }

        public override bool ShouldUpdatePosition() => false;
        public override void AI()
        {
            Timer++;


            if (Timer > 9f)
            {
                Projectile.Kill();
                return;
            }

            //Increase the size of the stomp
            Projectile.position = Projectile.Center;
            Projectile.Size = new Vector2(16 * MathHelper.Lerp(5f, MaxDiameter, Utils.GetLerpValue(0f, 9f, Timer)), Height);
            Projectile.Center = Projectile.position;

            Point topLeft = Projectile.TopLeft.ToTileCoordinates() + new Point(0, 1);
            Point bottomRight = Projectile.BottomRight.ToTileCoordinates();

            int centerX = topLeft.X / 2 + bottomRight.X / 2;
            int Radius = Projectile.width / 2;

            if (Timer % 3 != 0)
                return;

            int stompStage = (int)Timer / 3;

            for (int i = topLeft.X; i <= bottomRight.X; i++)
            {
                for (int j = topLeft.Y; j <= bottomRight.Y; j++)
                {
                    float distance = Vector2.Distance(Projectile.Center, new Vector2(i * 16 + 8, j * 16 + 8));
                    if (distance > Radius)
                        continue;


                    Tile stompedTile = Framing.GetTileSafely(i, j);
                    Tile stompedTileAbove = Framing.GetTileSafely(i, j - 1);

                    if (!stompedTile.HasUnactuatedTile)
                        continue;

                    bool solidTileAbove = stompedTileAbove.HasUnactuatedTile && Main.tileSolid[stompedTileAbove.TileType];
                    bool solidTopTileAbove = stompedTileAbove.HasUnactuatedTile && (Main.tileSolid[stompedTileAbove.TileType] || Main.tileSolidTop[stompedTileAbove.TileType]);

                    bool isHalfSolid = Main.tileSolidTop[stompedTile.TileType];

                    if ((Main.tileSolid[stompedTile.TileType] && solidTileAbove) || (isHalfSolid && solidTopTileAbove))
                        continue;

                    if (stompStage >= 2)
                        TileStompEffect(i, j, stompedTile, isHalfSolid, distance);

                    int dustCount = WorldGen.KillTile_GetTileDustAmount(fail: true, stompedTile, i, j);

                    for (int k = 0; k < dustCount / 2; k++)
                    {
                        Dust tileBreakDust = Main.dust[WorldGen.KillTile_MakeTileDust(i, j, stompedTile)];
                        tileBreakDust.velocity.Y -= 3f + (float)stompStage * 1.5f;
                        tileBreakDust.velocity.Y *= Main.rand.NextFloat();
                        tileBreakDust.velocity.Y *= 0.75f;
                        tileBreakDust.scale += (float)stompStage * 0.03f;
                    }

                    if (stompStage >= 2)
                    {
                        for (int m = 0; m < dustCount / 2 - 1; m++)
                        {
                            Dust tileBreakDust2 = Main.dust[WorldGen.KillTile_MakeTileDust(i, j, stompedTile)];
                            tileBreakDust2.velocity.Y -= 1f + (float)stompStage;
                            tileBreakDust2.velocity.Y *= Main.rand.NextFloat();
                            tileBreakDust2.velocity.Y *= 0.75f;
                        }
                    }

                    if (dustCount <= 0 || Main.rand.NextBool(3) || (isHalfSolid && !Main.rand.NextBool(4)))
                        continue;

                    float distanceFromCenter = (float)Math.Abs(centerX - i) / (Radius / 2f);

                    Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromAI(), Projectile.position, Vector2.Zero, GoreType(), 1f - (float)stompStage * 0.15f + distanceFromCenter * 0.5f);
                    gore.velocity.Y -= 0.1f + (float)stompStage * 0.5f + distanceFromCenter * (float)stompStage;
                    gore.velocity.Y *= Main.rand.NextFloat();
                    gore.velocity.X *= 0.05f;

                    gore.position = new Vector2(i * 16, j * 16);
                }
            }
        }

        public override bool CanHitPlayer(Player target)
        {
            return target.velocity.Y <= 0 || Projectile.Center.Y < target.Bottom.Y;
        }

        public virtual void TileStompEffect(int i, int j, Tile t, bool solidTop, float distance) { }

        public virtual int GoreType() => GoreID.Smoke1 + Main.rand.Next(3);
    }
}
