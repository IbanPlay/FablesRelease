using Terraria.DataStructures;
using Terraria.Utilities;

namespace CalamityFables.Content.Tiles.Graves
{
    public class PearlstoneGrave : BaseGrave, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public virtual Color GlowColor => FablesUtils.MulticolorLerp(Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.5f)), Color.DarkOrange, Color.Gold, Color.SpringGreen, Color.DeepSkyBlue, Color.Fuchsia) with { A = 0 };

        public readonly static List<int> ProjectileTypes = new List<int>();
        protected readonly static List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static List<AutoloadedGravestoneItem> ItemInstances = new();

        public override Color MapColor => new Color(181, 172, 190);
        public override bool Glowing => true;
        public override int BreakDust => DustID.Pearlwood;

        public override string[] GravestoneNames => new string[] { "PearlstoneTombstone", "PearlwoodGraveMarker", "PearlstoneGraveMarker", "PearlstoneGravestone" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile t = Main.tile[i, j];
            if (t.TileFrameY == 0 && t.TileFrameX % 36 == 0)
            {
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.Background, true);
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.BehindTiles, true);
            }
        }

        public void DrawSpecialLayer(int x, int y, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            Texture2D auroraTexture = AssetDirectory.CommonTextures.BloomDiamondColumn.Value;

            Vector2 origin = auroraTexture.Size() / 2f;
            FastRandom random = new FastRandom().WithModifier(x, y);
            float cycleOffset = random.NextFloat() * 10f;
            float colorOffset = random.NextFloat();

            float cycle = (Main.GlobalTimeWrappedHourly * 1.2f + cycleOffset) % 10f / 10f;
            Vector2 drawPosition = new Vector2(x, y) * 16f + Vector2.UnitX * 16 - Main.screenPosition;

            float height = -(9f + 5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.14f));

            float colorOpacityMultiplier = 0.3f * (0.45f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly));

            for (int i = 0; i < 15; i++)
            {
                float rotationAngle = cycle * MathHelper.TwoPi + (i / 15f) * MathHelper.TwoPi;
                float depth = (float)Math.Sin(rotationAngle + MathHelper.PiOver2);
                if ((depth < 0 && layer == TileDrawLayer.BehindTiles) || (depth >= 0 && layer == TileDrawLayer.Background))
                    continue;

                float horizontalOffset = (float)Math.Sin(cycle * 2f * MathHelper.TwoPi + (i / 15f) * MathHelper.TwoPi + MathHelper.PiOver2) * 30f;
                float verticalOffset = depth * height * 0.7f;

                Color color;
                if (Gilded)
                {
                    float hue = i / 15f + colorOffset;
                    color = Main.hslToRgb(hue % 1f, 1f, 0.5f);
                }
                else
                {
                    float hue = colorOffset + Main.GlobalTimeWrappedHourly * 0.12f + (-0.2f + 0.4f * i / 15f);
                    color = Main.hslToRgb(hue % 1f, 1f, 0.65f) * 0.7f;
                }

                color *= colorOpacityMultiplier;
                color *= (0.3f + 0.7f * (0.5f + 0.5f * depth));
                color.A = 0;

                float rotation = (horizontalOffset / 30f) * -0.14f;

                Main.EntitySpriteDraw(auroraTexture, drawPosition + new Vector2(horizontalOffset, verticalOffset), null, color, rotation, origin, new Vector2(0.8f, 0.7f), SpriteEffects.None);
                Main.EntitySpriteDraw(auroraTexture, drawPosition + new Vector2(horizontalOffset, verticalOffset), null, color * 0.4f, rotation, origin, new Vector2(0.4f, 0.6f), SpriteEffects.None);
            }
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = GlowColor.R / 255f * 0.7f;
            g = GlowColor.G / 255f * 0.7f;
            b = GlowColor.B / 255f * 0.7f;
        }
    }

    public class CrystalGrave : PearlstoneGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static new List<int> ProjectileTypes = new List<int>();
        protected readonly static new List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static new List<AutoloadedGravestoneItem> ItemInstances = new();
        public override Color GlowColor => Color.Lerp(Main.DiscoColor, Color.White, 0.5f) with { A = 0 };

        public override Color MapColor => new Color(187, 118, 247);
        public override int BreakDust => DustID.PurpleCrystalShard;
        public override bool Glowing => true;
        public override bool Gilded => true;

        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;
        public override string[] GravestoneNames => new string[] { "CrystalTombstone", "CrystalHeadstone", "CrystalGraveMarker", "CrystalGravestone" };

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(50) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                Dust d = Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), DustID.RainbowTorch, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), Scale: Main.rand.NextFloat(0.5f, 1f));
                d.noGravity = true;
            }
        }
    }
}
