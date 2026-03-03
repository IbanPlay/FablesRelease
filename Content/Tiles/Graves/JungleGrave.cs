namespace CalamityFables.Content.Tiles.Graves
{
    public class JungleGrave : BaseGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static List<int> ProjectileTypes = new List<int>();
        protected readonly static List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static List<AutoloadedGravestoneItem> ItemInstances = new();

        public override Color MapColor => new Color(163, 99, 104);
        public override int BreakDust => DustID.JungleGrass;

        public override string[] GravestoneNames => new string[] { "BambooGraveMarker", "JungleGraveMarker", "JungleCrossGraveMarker", "JungleHeadstone" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(133) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                Dust d = Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), DustID.JungleSpore, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), Scale: Main.rand.NextFloat(0.5f, 1.2f));
                d.noGravity = false;
            }
        }
    }
    public class LihzahrdGrave : JungleGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static new List<int> ProjectileTypes = new List<int>();
        protected readonly static new List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static new List<AutoloadedGravestoneItem> ItemInstances = new();

        public override Color MapColor => new Color(194, 96, 21);
        public override int BreakDust => DustID.Lihzahrd;
        public override bool Gilded => true;
        public override string[] GravestoneNames => new string[] { "LihzahrdGravestone", "LihzahrdTombstone", "LihzahrdGraveMarker", "LihzahrdHeadstone" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(70) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                Color color = Color.Gold with { A = 0 };
                Dust d = Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), DustID.TintableDustLighted, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), 0, color, Main.rand.NextFloat(0.5f, 1f));
            }
        }
    }
}
