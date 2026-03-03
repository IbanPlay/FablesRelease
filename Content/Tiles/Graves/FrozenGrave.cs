namespace CalamityFables.Content.Tiles.Graves
{
    public class FrozenGrave : BaseGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static List<int> ProjectileTypes = new List<int>();
        protected readonly static List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static List<AutoloadedGravestoneItem> ItemInstances = new();

        public override Color MapColor => new Color(95, 146, 195);
        public override int BreakDust => DustID.Cobalt;

        public override string[] GravestoneNames => new string[] { "FrozenHeadstone", "FrozenTombstone", "FrozenGraveMarker", "FrozenObelisk" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;


        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(50) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                Dust d = Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), DustID.IceTorch, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), 0, Color.White, Main.rand.NextFloat(0.5f, 1.6f));
                d.noGravity = true;
                d.noLightEmittence = true;
            }
        }
    }

    public class GoldenFrozenGrave : FrozenGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static new List<int> ProjectileTypes = new List<int>();
        protected readonly static new List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static new List<AutoloadedGravestoneItem> ItemInstances = new();
        public override bool Gilded => true;

        public override string[] GravestoneNames => new string[] { "GoldenFrozenHeadstone", "GoldenFrozenTombstone", "GoldenFrozenGraveMarker", "GoldenFrozenObelisk" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(50) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                int dustType = Main.rand.NextBool(3) ? DustID.IceTorch : DustID.TintableDustLighted;
                Color color = dustType == DustID.IceTorch ? Color.White : Color.Gold;
                Dust d = Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), dustType, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), 0, color, Main.rand.NextFloat(0.5f, 1f));
                if (dustType == DustID.IceTorch)
                    d.noGravity = true;
            }
        }
    }
}
