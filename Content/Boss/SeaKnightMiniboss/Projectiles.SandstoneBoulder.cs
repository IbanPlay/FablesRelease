using CalamityFables.Particles;

namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public class SandstoneBoulder : ModProjectile
    {
        public static readonly SoundStyle BreakSound = new(SoundDirectory.Nautilus + "SandstoneBoulderBreak");
        public static readonly SoundStyle DropSound = new(SoundDirectory.Nautilus + "SandstoneBoulderFall");

        public override string Texture => AssetDirectory.SirNautilus + Name;

        public ref float WaitTimer => ref Projectile.ai[0];
        public ref float TileCollisionImmunityTimer => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("a sandstone boulder");

            if (Main.dedServ)
                return;

            for (int i = 1; i < 7; i++)
            {
                ChildSafety.SafeGore[Mod.Find<ModGore>("SandstoneBoulder" + i.ToString()).Type] = true;
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 52;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
        }

        public override bool ShouldUpdatePosition() => WaitTimer <= 0;
        public override bool? CanDamage() => WaitTimer <= 0;

        public override void AI()
        {
            if (WaitTimer > 0)
            {
                WaitTimer--;
                if (Main.rand.NextBool(3))
                {
                    int dustType = Main.rand.NextBool() ? 32 : Main.rand.NextBool() ? 287 : Main.rand.NextBool() ? 280 : 283;
                    Dust.NewDustPerfect(Projectile.Center + Vector2.UnitX * Main.rand.NextFloat(-20f, 20f) * (float)Math.Pow(Main.rand.NextFloat(), 0.3f), dustType, Vector2.UnitY * 2f, Scale: Main.rand.NextFloat(0.8f, 1.2f));
                }

                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(10))
                {
                    Vector2 gorePos = Projectile.Center + Vector2.UnitY * 10f + Vector2.UnitX * (float)Math.Pow(Main.rand.NextFloat(), 1.5f) * 15f * (Main.rand.NextBool() ? -1 : 1);
                    Gore wore = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), gorePos, Vector2.UnitY * 2f, Mod.Find<ModGore>("SandstoneBoulder" + Main.rand.Next(1, 7).ToString()).Type, Main.rand.NextFloat(0.5f, 0.7f));
                    wore.timeLeft = 30;
                }

                if (Main.rand.NextBool(2))
                {
                    float dustSpacing = (float)Math.Pow(Main.rand.NextFloat(), 1.5f);
                    Vector2 dustOrigin = Projectile.Center + Vector2.UnitX * dustSpacing * 16f * (Main.rand.NextBool() ? -1 : 1);
                    dustOrigin += Vector2.UnitY * Main.rand.NextFloat(50f);

                    Vector2 dustSpeed = Vector2.UnitY * Main.rand.NextFloat(4f, 8f) * (dustSpacing * 0.6f + 0.4f);

                    Dust zeSand = Dust.NewDustPerfect(dustOrigin, 148, dustSpeed);
                    zeSand.fadeIn = 1f;
                    zeSand.rotation = Main.rand.NextFloat(MathHelper.TwoPi);

                }

                if (Main.rand.NextBool(2))
                {
                    float dustSpacing = (float)Math.Pow(Main.rand.NextFloat(), 1.5f);
                    Vector2 dustOrigin = Projectile.Center + Vector2.UnitX * dustSpacing * 16f * (Main.rand.NextBool() ? -1 : 1);
                    dustOrigin += Vector2.UnitY * Main.rand.NextFloat(50f);

                    Vector2 dustSpeed = Vector2.UnitY * Main.rand.NextFloat(3f, 6f) * (dustSpacing * 0.6f + 0.4f);

                    Dust zeSand = Dust.NewDustPerfect(dustOrigin, 148, dustSpeed, 200, Scale: 1.3f);
                    zeSand.fadeIn = 1f;
                    zeSand.rotation = Main.rand.NextFloat(MathHelper.TwoPi);

                }

                if (Main.rand.NextBool(7))
                {
                    float dustSpacing = (float)Math.Pow(Main.rand.NextFloat(), 2.5f);

                    Vector2 dustPosition = Projectile.Center + Vector2.UnitX * 16f * (float)Math.Pow(dustSpacing, 2f) * (Main.rand.NextBool() ? -1 : 1) + Vector2.UnitY * dustSpacing * 30f - Vector2.UnitY * 6f;
                    Color dustColor = Color.Lerp(Color.White, Color.Gold, 0.6f);

                    Dust zust = Dust.NewDustPerfect(dustPosition, 43, Vector2.UnitY * Main.rand.NextFloat(3f, 6f) * (dustSpacing * 0.6f + 0.4f), 40, dustColor, Main.rand.NextFloat(0.7f, 1.5f));
                    zust.noGravity = false;

                    if (!Main.rand.NextBool(5))
                        zust.noLightEmittence = true;
                }


                if (Main.rand.NextBool(5))
                {
                    Particle smokey = new SmallSmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(35f, 16f), Vector2.UnitY * 2f, Color.SaddleBrown, Color.SaddleBrown, Main.rand.NextFloat(1f, 1.5f), 150, MathHelper.PiOver4 * 0.03f * Main.rand.NextFloatDirection(), true);
                    ParticleHandler.SpawnParticle(smokey);
                }


                return;
            }


            if (WaitTimer == 0)
            {
                WaitTimer--;
                SoundEngine.PlaySound(DropSound, Projectile.Center);
            }

            if (TileCollisionImmunityTimer < 20f)
                TileCollisionImmunityTimer++;

            if (TileCollisionImmunityTimer >= 20f && Collision.SolidCollision(Projectile.Center - Vector2.One * 7f, 14, Projectile.height / 2, false))
                Projectile.Kill();

            Projectile.velocity += Vector2.UnitY * (TileCollisionImmunityTimer / 20f);
            if (Projectile.velocity.Y > 22)
                Projectile.velocity.Y = 22;

            Projectile.rotation += MathHelper.PiOver2 * 0.1f * (Projectile.identity % 2 == 0 ? -1 : 1);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            FablesProjectile.DrawBehindTilesAlways.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (WaitTimer > 0)
            {
                Texture2D faisceau = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "SporedSkeletonTelegraph").Value;
                Vector2 origin = new Vector2(faisceau.Width / 2, 0);

                float bump = (float)Math.Sin(WaitTimer / 40f * MathHelper.Pi);
                bump = Math.Max(0, bump);

                Color bloomColor = new Color(200, 41, 28) with { A = 0 } * bump * 0.15f;
                Vector2 scale = new Vector2(1.2f, 4f);
                scale *= (float)Math.Pow(bump, 0.5f);

                Main.EntitySpriteDraw(faisceau, Projectile.Center - Vector2.UnitY * 30f - Main.screenPosition, null, bloomColor, 0f, origin, scale, 0, 0);

                scale.X *= 0.4f;
                bloomColor.G = (byte)(22 + 25 * bump * 0.2f);
                bloomColor.B = (byte)(25 * bump * 0.2f);
                Main.EntitySpriteDraw(faisceau, Projectile.Center - Vector2.UnitY * 30f - Main.screenPosition, null, bloomColor * 0.5f, 0f, origin, scale, 0, 0);

                return false;
            }

            lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());

            Texture2D tex = TextureAssets.Projectile[Type].Value;
            int variant = Projectile.identity % 3;
            Rectangle frame = new Rectangle(tex.Width / 3 * variant, 0, tex.Width / 3 - 2, tex.Height);

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(BreakSound, Projectile.position);
            //TODO - particles and debris, etc

            if (Main.netMode != NetmodeID.Server)
            {
                if (Main.rand.NextBool())
                {
                    Gore yore = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), Projectile.Center, -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4 * 1.4f) * Main.rand.NextFloat(2f, 6f), Mod.Find<ModGore>("SandstoneBoulderLarge" + Main.rand.Next(1, 6).ToString()).Type, 1f);
                    yore.timeLeft = 30;
                }

                for (int i = 0; i < 4; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Circular(1f, 1f);
                    if (direction.Y < 0)
                        direction.Y *= -1;

                    Particle smokey = new SmallSmokeParticle(Projectile.Center + direction * Main.rand.NextFloat(4f, 10f), direction * 7f - Vector2.UnitY * 7f, Color.SaddleBrown, Color.SaddleBrown, Main.rand.NextFloat(1f, 1.5f), 150, MathHelper.PiOver4 * 0.03f * Main.rand.NextFloatDirection(), true);
                    ParticleHandler.SpawnParticle(smokey);
                }

                int minigoreCount = Main.rand.Next(0, 3);

                for (int i = 0; i < minigoreCount; i++)
                {
                    Gore wore = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), Projectile.Center, -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4 * 1.4f) * Main.rand.NextFloat(3f, 8f), Mod.Find<ModGore>("SandstoneBoulder" + Main.rand.Next(1, 7).ToString()).Type, Main.rand.NextFloat(0.5f, 1f));
                    wore.timeLeft = 15;
                }
            }
        }
    }
}
