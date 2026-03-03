namespace CalamityFables.Content.Tiles.Graves
{
    public class SandyGrave : BaseGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static List<int> ProjectileTypes = new List<int>();
        protected readonly static List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static List<AutoloadedGravestoneItem> ItemInstances = new();

        public override Color MapColor => new Color(170, 132, 91);
        public override int BreakDust => DustID.BeachShell;

        public override string[] GravestoneNames => new string[] { "PalmwoodCrossGraveMarker", "PalmwoodGraveMarker", "PalmwoodShipwreckMarker", "SeashellTombstone" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;
    }

    public class CoralGrave : SandyGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static new List<int> ProjectileTypes = new List<int>();
        protected readonly static new List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static new List<AutoloadedGravestoneItem> ItemInstances = new();
        public override Color MapColor => new Color(235, 114, 80);
        public override int BreakDust => DustID.RedStarfish;
        public override bool Gilded => true;

        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;
        public override string[] GravestoneNames => new string[] { "CoralGraveMarker", "CoralTombstone", "ReefGraveMarker", "ReefTombstone" };

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(50) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), 43, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), 0, Color.Salmon, Main.rand.NextFloat(0.5f, 1f));
            }
        }
    }
}
