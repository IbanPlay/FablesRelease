namespace CalamityFables.Content.Tiles.Graves
{
    public class MyceliumGrave : BaseGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static List<int> ProjectileTypes = new List<int>();
        protected readonly static List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static List<AutoloadedGravestoneItem> ItemInstances = new();
        public override Color MapColor => new Color(95, 98, 215);
        public override int BreakDust => DustID.GlowingMushroom;
        public override bool Glowing => true;

        public override string[] GravestoneNames => new string[] { "MycelialHeadstone", "MycelialGravestone", "MycelialGraveMarker", "MycelialTombstone" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(50) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), DustID.GlowingMushroom, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), 0, Color.White, Main.rand.NextFloat(0.5f, 1f));
            }
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            //Mushroom glow
            FablesGeneralSystemHooks.SetRNGCoords(i, j);
            float glowRandom = FablesGeneralSystemHooks.fastRNG.Next(28, 42) * 0.005f;
            glowRandom += (270 - Main.mouseTextColor) / 1000f;
            r = 0f;
            g = 0.2f + glowRandom / 2f;
            b = 1f;
        }
    }

    public class GoldenMyceliumGrave : MyceliumGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly new static List<int> ProjectileTypes = new List<int>();
        protected readonly static new List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static new List<AutoloadedGravestoneItem> ItemInstances = new();
        public override bool Gilded => true;

        public override string[] GravestoneNames => new string[] { "GoldenMycelialCrossGraveMarker", "GoldenMycelialGraveMarker", "GoldenMycelialHeadstone", "GoldenMycelialTombstone" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(30) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                int dustType = Main.rand.NextBool(3) ? DustID.GlowingMushroom : DustID.TintableDustLighted;
                Color color = dustType == DustID.GlowingMushroom ? Color.White : Color.Gold;
                Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), dustType, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), 0, color, Main.rand.NextFloat(0.5f, 1f));
            }

            /*
            if (Main.rand.NextBool(130) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                MushroomBootParticle mushroomParticle = MushroomBoots._particlePool.RequestParticle();
                Asset<Texture2D> texture = ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "MushroomBootsShrooms", AssetRequestMode.ImmediateLoad);
                Asset<Texture2D> glowTexture = ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "MushroomBootsShrooms_Glow", AssetRequestMode.ImmediateLoad);

                float particleLifetime = Main.rand.NextFloat(60f, 90f);
                mushroomParticle.SetBasicInfo(texture, texture.Value.Frame(1, 3, 0, Main.rand.Next(3), -2, -2), Vector2.Zero, new Vector2(i * 16 + Main.rand.NextFloat(16f), j * 16f + 16));
                mushroomParticle.SetTypeInfo(particleLifetime);

                float scale = Main.rand.NextFloat() * 0.8f + 0.67f;
                Vector2 squish = Vector2.One * Main.rand.NextFloat(1f, 1.2f);
                squish.X = 2 - squish.X;

                mushroomParticle._glowmaskTexture = glowTexture;
                mushroomParticle.FadeOutNormalizedTime = 0.4f;
                mushroomParticle.ScaleVelocity = new Vector2(0f, 1f) * scale * 0.3f;
                mushroomParticle.ScaleAcceleration = squish * scale * (-1f / 60f) / particleLifetime;
                mushroomParticle.Scale = squish * scale;
                Main.ParticleSystem_World_BehindPlayers.Add(mushroomParticle);
            }
            */
        }
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            //Mushroom glow
            FablesGeneralSystemHooks.SetRNGCoords(i, j);
            float glowRandom = FablesGeneralSystemHooks.fastRNG.Next(28, 42) * 0.005f;
            glowRandom += (270 - Main.mouseTextColor) / 1000f;
            r = 0.76f;
            g = 0.4f + glowRandom / 2f;
            b = 0.2f;
        }
    }
}
