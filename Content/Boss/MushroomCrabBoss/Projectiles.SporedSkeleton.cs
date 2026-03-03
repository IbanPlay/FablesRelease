using static CalamityFables.Content.Boss.MushroomCrabBoss.SporedCorpse;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public class SporedSkeleton : ModProjectile
    {
        public override string Texture => AssetDirectory.Crabulon + Name;

        public float TargetHeight => Projectile.ai[0];
        public List<SporedCorpseRoot> roots;

        public Vector2? originPosition;

        public float Timer => 180 - Projectile.timeLeft;

        public int rootCount;
        public int variant;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("a spored corpse");
        }

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 0;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = 180;
            Projectile.hide = true;
            rootCount = Main.rand.Next(0, 4);
            variant = Main.rand.Next(0, 4);

            //Rare chance to be a mushroom
            if (Main.rand.NextBool(10))
            {
                variant = 4;
                rootCount = 0;
            }
        }

        public override void AI()
        {
            if (!originPosition.HasValue)
                originPosition = Projectile.Center;

            roots = roots ?? new List<SporedCorpseRoot>();
            while (roots.Count < rootCount)
                roots.Add(new SporedCorpseRoot(14f));

            Projectile.velocity.Y += 0.3f;
            Projectile.rotation += 0.04f;

            if (!Projectile.tileCollide && Projectile.Center.Y > TargetHeight)
                Projectile.tileCollide = true;

            Lighting.AddLight(Projectile.Center, Color.RoyalBlue.ToVector3() * 1.3f);

            if (Timer < 30)
            {
                if (Main.rand.NextBool(4))
                {
                    float dustSpacing = (float)Math.Pow(Main.rand.NextFloat(), 1.5f);
                    Vector2 dustOrigin = originPosition.Value + Vector2.UnitX * dustSpacing * 16f * (Main.rand.NextBool() ? -1 : 1);
                    dustOrigin += Vector2.UnitY * Main.rand.NextFloat(200f);

                    Vector2 dustSpeed = Vector2.UnitY * Main.rand.NextFloat(3f, 6f) * (dustSpacing * 0.6f + 0.4f);

                    int dustType = Main.rand.NextBool() ? DustID.MushroomTorch : DustID.MushroomSpray;

                    Dust dust = Dust.NewDustPerfect(dustOrigin, dustType, dustSpeed, 50, Scale: Main.rand.NextFloat(0.7f, 1.2f));
                    dust.fadeIn = 1f;
                    dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                }
            }
        }

        public override bool ShouldUpdatePosition()
        {
            return Timer > 12f;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(DeathSound with { Volume = 0.2f }, Projectile.Center);

            //Skeleton variants
            if (variant < 4)
            {
                SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt, Projectile.Center);
                int skullType = Main.rand.Next(new int[] { 42, 267, 268, 269 });
                Gore.NewGorePerfect(Projectile.GetSource_FromThis(), Projectile.Center - Vector2.UnitY * 5f, -Vector2.UnitY * 4f + Vector2.UnitX * Main.rand.NextFloatDirection() * 4f, skullType);
            }

            //375 - 377 : Mushroom cloud particles from the death of mushroom enemies
            //DOesnt have a goreID name fsr rip
            for (int i = 0; i < 4; i++)
            {
                Vector2 direction = Main.rand.NextVector2Circular(1f, 1f);
                Gore.NewGorePerfect(Projectile.GetSource_FromThis(), Projectile.Center + direction * Main.rand.NextFloat(4f, 10f), direction * 1f, 375 + Main.rand.Next(3));
            }

            for (int i = 0; i < 9; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Dust.NewDustPerfect(Projectile.Center + direction * Main.rand.NextFloat(6f, 12f), DustID.MushroomSpray, direction * Main.rand.NextFloat(0.4f, 1f), Scale: Main.rand.NextFloat(0.7f, 1.1f));
            }

            base.OnKill(timeLeft);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            FablesProjectile.DrawBehindTilesAlways.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //Draw light from the ceiling
            if (originPosition.HasValue && Timer <= 40)
            {
                Texture2D faisceau = ModContent.Request<Texture2D>(Texture + "Telegraph").Value;
                Vector2 origin = new Vector2(faisceau.Width / 2, 0);

                float bump = (float)Math.Sin(Timer / 40f * MathHelper.Pi);

                Color bloomColor = new Color(50, 20, 255) with { A = 0 } * bump * 0.2f;
                Vector2 scale = new Vector2(1.3f, 14f);
                scale *= (float)Math.Pow(bump, 0.5f);

                Main.EntitySpriteDraw(faisceau, originPosition.Value - Vector2.UnitY * 30f - Main.screenPosition, null, bloomColor, 0f, origin, scale, 0, 0);

                scale.X *= 0.4f;
                bloomColor.G = (byte)(25 * bump * 0.2f);
                bloomColor.R = (byte)(35 * bump * 0.2f);
                Main.EntitySpriteDraw(faisceau, originPosition.Value - Vector2.UnitY * 30f - Main.screenPosition, null, bloomColor * 0.5f, 0f, origin, scale, 0, 0);
            }

            lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());

            Texture2D bloom = ModContent.Request<Texture2D>(AssetDirectory.Assets + "Glow").Value;
            Color backlightColor = new Color(30, 50, 200, 0);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, backlightColor * 0.1f, 0f, bloom.Size() / 2f, 3.4f, 0, 0);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, backlightColor * 0.16f, 0f, bloom.Size() / 2f, 1.7f, 0, 0);

            Texture2D rootTex = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MushroomInfestationRoots").Value;

            if (roots != null)
                DrawRoots(rootTex, false, lightColor);

            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, 5, 0, variant, 0, -2);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.Lerp(lightColor, Color.White, 0.2f), Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0, 0);

            return false;
        }

        public void DrawRoots(Texture2D rootTex, bool abovePlayer, Color lightColor)
        {
            foreach (SporedCorpseRoot root in roots)
            {
                Vector2 scale = new Vector2(1f, 1f * root.stretch);

                Vector2 gorePosition = Projectile.Center + (root.rotation + Projectile.rotation).ToRotationVector2() * root.distance;
                Rectangle frame = rootTex.Frame(1, 7, 0, root.variant, 0, -2);
                Vector2 origin = new Vector2(frame.Width / 2, frame.Height);
                float rotation = Projectile.rotation + root.rotation + root.tilt + MathHelper.PiOver2;
                rotation += (float)Math.Sin((Projectile.timeLeft + root.rotation * 400f) * 0.1f * root.wiggleSpeed) * 0.1f;

                SpriteEffects effects = root.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Main.EntitySpriteDraw(rootTex, gorePosition - Main.screenPosition, frame, lightColor, rotation, origin, scale * Projectile.scale * 0.5f, effects, 0);
            }
        }
    }
}
