using Terraria.DataStructures;
using Terraria.Utilities;

namespace CalamityFables.Content.Tiles.Graves
{
    public class SkyGrave : BaseGrave, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public static Asset<Texture2D> CloudSprites;
        public readonly static List<int> ProjectileTypes = new List<int>();
        protected readonly static List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static List<AutoloadedGravestoneItem> ItemInstances = new();

        public override Color MapColor => new Color(208, 178, 105);
        public override int BreakDust => DustID.WoodFurniture;

        public override string[] GravestoneNames => new string[] { "SkyGraveMarker", "SkyCrossGraveMarker", "SkyGravestone", "SkyCairn" };
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
            CloudSprites = CloudSprites ?? ModContent.Request<Texture2D>(AssetDirectory.Graves + "SkyGraveClouds");
            Texture2D cloud = CloudSprites.Value;

            FastRandom random = new FastRandom().WithModifier(x, y);
            float cycleOffset = random.NextFloat() * 10f;

            float cycle = (Main.GlobalTimeWrappedHourly * 1f + cycleOffset) % 10f / 10f;
            Vector2 drawPosition = new Vector2(x, y) * 16f + Vector2.UnitX * 16 - Main.screenPosition;
            Color lightColor = Lighting.GetColor(x, y);
            float heightOffset = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.2f + random.NextFloat()) * -2f + 4f;

            for (int i = 0; i < 2; i++)
            {
                float cyclePosition = i * 0.7f + random.NextFloat() * 0.6f;

                float rotationAngle = cycle * MathHelper.TwoPi + cyclePosition / 2f * MathHelper.TwoPi;

                float depth = (float)Math.Sin(rotationAngle + MathHelper.PiOver2);
                if ((depth < 0 && layer == TileDrawLayer.BehindTiles) || (depth >= 0 && layer == TileDrawLayer.Background))
                    continue;

                Rectangle frame = cloud.Frame(1, 2, 0, i, 0, -2);
                Vector2 origin = frame.Size() / 2f;

                float horizontalOffset = (float)Math.Sin(rotationAngle) * 15f;
                float verticalOffset = depth * 5f + heightOffset;
                Color color = lightColor * 0.4f * (0.5f + 0.6f * depth);
                float size = 1.1f + depth * 0.1f;

                Main.EntitySpriteDraw(cloud, drawPosition + new Vector2(horizontalOffset, verticalOffset), frame, color, 0f, origin, size, SpriteEffects.None);
            }
        }

    }

    public class SkywareGrave : SkyGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static new List<int> ProjectileTypes = new List<int>();
        protected readonly static new List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static new List<AutoloadedGravestoneItem> ItemInstances = new();
        public override Color MapColor => new Color(29, 153, 193);
        public override int BreakDust => DustID.Skyware;
        public override bool Gilded => true;
        public override bool Glowing => true;

        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;
        public override string[] GravestoneNames => new string[] { "SkywareTombstone", "SkywareCrossGraveMarker", "SkywareHeadstone", "SkywareGraveMarker" };

        public static Asset<Texture2D> GlowMask;
        public override void Load()
        {
            base.Load();
            if (!Main.dedServ)
                GlowMask = ModContent.Request<Texture2D>(Texture + "_Glow");
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(50) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), 43, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), 0, Color.Salmon, Main.rand.NextFloat(0.5f, 1f));
            }
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Main.tile[i, j].TileFrameY == 0 && Main.tile[i, j].TileFrameX / 36 == 2)
            {
                r = 1f;
                g = 1f;
                b = 0.7f;
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];

            if (tile.TileFrameY != 0 || tile.TileFrameX / 36 != 2)
                return;

            int xPos = tile.TileFrameX - 36 * 2;
            int yPos = tile.TileFrameY;
            Texture2D glowmask = GlowMask.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero + Vector2.UnitY * 2;
            Color drawColour = GetDrawColour(i, j, new Color(50, 50, 50, 0));
            Rectangle frame = new Rectangle(xPos, yPos, 18, 18);

            for (int x = 0; x < 5; x++)
            {
                float horizontalOffset = (float)Utils.RandomInt(ref Main.TileFrameSeed, -10, 11) * 0.16f;
                float verticalOffset = (float)Utils.RandomInt(ref Main.TileFrameSeed, -10, 1) * 0.16f;
                Main.spriteBatch.Draw(glowmask, drawOffset + new Vector2(horizontalOffset, verticalOffset), frame, drawColour, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
        }

        private Color GetDrawColour(int i, int j, Color colour)
        {
            int colType = Main.tile[i, j].TileColor;
            Color paintCol = WorldGen.paintColor(colType);
            if (colType >= 13 && colType <= 24)
            {
                colour.R = (byte)(paintCol.R / 255f * colour.R);
                colour.G = (byte)(paintCol.G / 255f * colour.G);
                colour.B = (byte)(paintCol.B / 255f * colour.B);
            }
            return colour;
        }
    }
}
