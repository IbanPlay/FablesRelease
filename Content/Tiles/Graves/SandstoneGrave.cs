using CalamityFables.Content.Tiles.BurntDesert;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.DataStructures;

namespace CalamityFables.Content.Tiles.Graves
{
    public class SandstoneGrave : BaseGrave
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public virtual Color GlowColor => FablesUtils.MulticolorLerp(Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly)), Color.Cyan, Color.Turquoise, Color.MediumSpringGreen, Color.DeepSkyBlue) with { A = 0 };
        public static Asset<Texture2D> GlowingAura;
        public static Asset<Texture2D> GlowingLines;

        public readonly static List<int> ProjectileTypes = new();
        protected readonly static List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        public readonly static List<AutoloadedGravestoneItem> ItemInstances = new();

        public override Color MapColor => new Color(141, 111, 85);
        public override int BreakDust => DustID.Sand;
        public override bool Glowing => true;

        public override string[] GravestoneNames => new string[] { "SandstoneGraveMarker", "SandstoneTombstone", "SandstoneObelisk", "SandstoneCrossGraveMarker" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;
        public override string DirectoryPath => AssetDirectory.BurntDesert;

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (GlowingAura == null)
                GlowingAura = ModContent.Request<Texture2D>(AssetDirectory.Tiles + "PointOfInterest2xGlow");

            Point pos = new Point(i, j);
            Tile tile = Main.tile[pos];

            //Draw a glowing aura behind the grave (do it at the top left of the tile since that's the first part of the tile to be drawn in order
            if (tile.TileFrameX % 36 == 0 && tile.TileFrameY % 36 == 0)
            {
                Vector2 offScreen = new Vector2(Main.offScreenRange);
                if (Main.drawToScreen)
                    offScreen = Vector2.Zero;

                Texture2D glowAura = GlowingAura.Value;
                Vector2 origin = new Vector2(glowAura.Width / 2, glowAura.Height - 14);
                Vector2 effectScale = new Vector2(1f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.7f) * 0.14f + 0.74f);
                Color auraColor = GlowColor * (0.25f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.4f) * 0.08f);
                Rectangle frame = new Rectangle(0, 0, glowAura.Width, glowAura.Height - 14);

                spriteBatch.Draw(glowAura, new Vector2(i + 1, j + 2) * 16 + Vector2.UnitY * 2 + offScreen - Main.screenPosition, frame, auraColor, 0f, origin, effectScale, 0f, 0f);
            }
            return base.PreDraw(i, j, spriteBatch);
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (GlowingLines == null)
                GlowingLines = ModContent.Request<Texture2D>(AssetDirectory.Tiles + "PointOfInterestGlowLines");

            Point pos = new Point(i, j);
            Tile tile = Main.tile[pos];

            //Draw scrolling lines on the ground
            if (tile.TileFrameY % 36 == 18)
            {
                Vector2 offScreen = new Vector2(Main.offScreenRange);
                if (Main.drawToScreen)
                    offScreen = Vector2.Zero;

                Texture2D glowLines = GlowingLines.Value;

                Vector2 effectScale = new Vector2(1f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.7f) * 0.24f + 0.84f);
                Color auraColor = GlowColor * (0.25f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.4f + 0.3f) * 0.08f);

                float startX = (i * 16 + (Main.GameUpdateCount * 0.3333f)) % glowLines.Width;
                float frameWidth = Math.Min(glowLines.Width - startX, 16);

                Rectangle frame = new Rectangle((int)startX, 0, (int)frameWidth, glowLines.Height - 14);
                Vector2 origin = new Vector2(8, frame.Height);

                //-6
                Vector2 drawPosition = new Vector2(i, j + 1) * 16 + new Vector2(8, 2) + offScreen - Main.screenPosition;
                spriteBatch.Draw(glowLines, drawPosition, frame, auraColor, 0f, origin, effectScale, 0f, 0f);

                if (frameWidth < 16)
                {
                    origin = new Vector2(8 - (int)frameWidth, frame.Height);
                    frameWidth = 16 - (int)frameWidth;
                    frame = new Rectangle(0, 0, (int)frameWidth, glowLines.Height - 14);

                    spriteBatch.Draw(glowLines, drawPosition, frame, auraColor, 0f, origin, effectScale, 0f, 0f);
                }
            }
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(50) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), 43, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), 0, GlowColor, Main.rand.NextFloat(0.5f, 1f));
            }
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = GlowColor.R / 255f * 0.7f;
            g = GlowColor.G / 255f * 0.7f;
            b = GlowColor.B / 255f * 0.7f;
        }

        public override void MouseOver(int i, int j) => SpectralWater.MouseOverIcon(i, j);
    }

    public class GoldenSandstoneGrave : SandstoneGrave
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override Color GlowColor => FablesUtils.MulticolorLerp(Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly)), Color.Gold, Color.Goldenrod, Color.OrangeRed, Color.DarkOrange) with { A = 0 };
        public readonly static new List<int> ProjectileTypes = new List<int>();
        protected readonly static new List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static new List<AutoloadedGravestoneItem> ItemInstances = new();
        public override bool Gilded => true;

        public override string[] GravestoneNames => new string[] { "GoldenSandstoneGravestone", "GoldenSandstoneTombstone", "GoldenSandstoneObelisk", "GoldenSandstoneHeadstone" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;

        public override void MouseOver(int i, int j) {  }
    }
}
