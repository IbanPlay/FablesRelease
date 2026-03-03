using Terraria.GameContent.Drawing;

namespace CalamityFables.Content.Tiles.Graves
{
    public class CorruptGrave : BaseGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static List<int> ProjectileTypes = new List<int>();
        protected readonly static List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static List<AutoloadedGravestoneItem> ItemInstances = new();

        public override Color MapColor => new Color(104, 100, 126);
        public override int BreakDust => DustID.CorruptPlants;

        public override string[] GravestoneNames => new string[] { "CorruptCrossGraveMarker", "CorruptObelisk", "CorruptTombstone", "CorruptGraveMarker" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if ((!Main.gamePaused && Main.instance.IsActive) && Main.rand.NextBool(400) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.PooFly, new ParticleOrchestraSettings
                {
                    PositionInWorld = new Vector2(i * 16 + 8, j * 16 - 8)
                });
            }
        }
    }

    public class DemoniteGrave : CorruptGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static new List<int> ProjectileTypes = new List<int>();
        protected readonly static new List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static new List<AutoloadedGravestoneItem> ItemInstances = new();
        public override Color MapColor => new Color(120, 99, 197);
        public override int BreakDust => DustID.Demonite;
        public override bool Gilded => true;

        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;
        public override string[] GravestoneNames => new string[] { "DemoniteHeadstone", "DemoniteObelisk", "DemoniteTombstone", "DemoniteGraveMarker" };


        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(50) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), 43, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), 0, Color.Lavender, Main.rand.NextFloat(0.5f, 1f));
            }
        }
    }
}
