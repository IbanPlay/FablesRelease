namespace CalamityFables.Content.Tiles.Graves
{
    public class AshenGrave : BaseGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static List<int> ProjectileTypes = new List<int>();
        protected readonly static List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static List<AutoloadedGravestoneItem> ItemInstances = new();
        public override Color MapColor => new Color(101, 90, 102);
        public override int BreakDust => DustID.Ash;
        public override bool Glowing => true;

        public override string[] GravestoneNames => new string[] { "AshenGraveMarker", "AshenHeadstone", "AshenTombstone", "AshenGravestone" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Vector2 pos = new Vector2(i, j) * 16f;

            if (Main.rand.NextBool(50) && Main.tile[new Point(i, j)].TileFrameY % 36 == 18)
            {
                Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), 55, Vector2.UnitY.RotatedByRandom(0.3f) * -Main.rand.NextFloat(0.08f, 0.2f), 0, Color.White, Main.rand.NextFloat(0.5f, 1f));
            }
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            //lava glow
            FablesGeneralSystemHooks.SetRNGCoords(i, j);
            float glowRandom = FablesGeneralSystemHooks.fastRNG.Next(28, 42) * 0.005f;
            glowRandom += (270 - Main.mouseTextColor) / 1000f;
            r = 1f;
            g = 0.5f + glowRandom / 2f;
            b = 0f;
        }
    }

    public class MantleGrave : AshenGrave
    {
        public override string Texture => AssetDirectory.Graves + Name;

        public readonly static new List<int> ProjectileTypes = new List<int>();
        protected readonly static new List<AutoloadedGravestoneProjectile> ProjectileInstances = new();
        protected readonly static new List<AutoloadedGravestoneItem> ItemInstances = new();

        public override Color MapColor => new Color(238, 102, 70);
        public override int BreakDust => DustID.RedTorch;
        public override bool Gilded => true;

        public override string[] GravestoneNames => new string[] { "MantleGraveMarker", "MantleHeadstone", "MantleTombstone", "MantleGravestone" };
        public override List<int> ProjectilePool => ProjectileTypes;
        public override List<AutoloadedGravestoneProjectile> Projectiles => ProjectileInstances;
        public override List<AutoloadedGravestoneItem> Items => ItemInstances;


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
                int dustType = Main.rand.NextBool(3) ? 55 : DustID.RedTorch;
                Dust d = Dust.NewDustPerfect(pos + new Vector2(Main.rand.NextFloat(0, 16), Main.rand.NextFloat(8, 16)), dustType, Vector2.UnitY * -Main.rand.NextFloat(0.08f, 0.2f), 0, Color.White, Main.rand.NextFloat(0.5f, 1f));
                d.noGravity = true;
            }
        }


        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];

            if (tile.TileFrameX / 36 != 3 || tile.TileFrameY != 0)
                return;

            int xPos = tile.TileFrameX - 36 * 3;
            int yPos = tile.TileFrameY;
            Texture2D glowmask = GlowMask.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero + Vector2.UnitY * 2;
            Color drawColour = GetDrawColour(i, j, new Color(100, 100, 100, 0));
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
