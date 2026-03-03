namespace CalamityFables.Particles
{
    public class BouncingTileParticle : Particle
    {
        public override string Texture => AssetDirectory.Invisible;
        public override bool UseAdditiveBlend => false;
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        Point tilePosition;
        int delay;
        int bounceTime;
        float bounceHeight;
        Tile tile;

        short xFrameOffset = 0;
        short yFrameOffset = 0;

        public BouncingTileParticle(Point tilePosition, int delay, int bounceTime, float bounceHeight)
        {
            //This particle uses specific shit to the client so we just cut it off here to avoid errors
            if (Main.dedServ)
                return;

            Position = tilePosition.ToWorldCoordinates(0, 0);
            Velocity = Vector2.Zero;
            this.tilePosition = tilePosition;
            this.delay = delay;
            this.bounceTime = bounceTime;
            this.bounceHeight = bounceHeight;

            tile = Main.tile[tilePosition];

            xFrameOffset = tile.TileFrameX;
            yFrameOffset = tile.TileFrameY;

            Main.instance.TilesRenderer.GetTileDrawData(tilePosition.X, tilePosition.Y, tile, (ushort)tile.TileType, ref xFrameOffset, ref yFrameOffset, out _, out _, out _, out _, out int frameXExtra, out int frameYExtra, out _, out _, out _, out _);

            xFrameOffset += (short)frameXExtra;
            yFrameOffset += (short)frameYExtra;

            Lifetime = delay + bounceTime;
            FrontLayer = false;
        }

        public override void Update()
        {
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            if (Time < delay)
                return;
            float bounceDisplace = bounceHeight * FablesUtils.SineBumpEasing((Time - delay) / (float)bounceTime); ;

            Texture2D texture = TextureAssets.Tile[tile.TileType].Value;
            Rectangle frame = new Rectangle(xFrameOffset, yFrameOffset, 16, 18);
            Vector2 position = Position - Vector2.UnitY * bounceDisplace;
            Color color = Lighting.GetColor(tilePosition);

            spriteBatch.Draw(texture, position - basePosition, frame, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

            Rectangle bottomFrame = new Rectangle(xFrameOffset, (yFrameOffset / 18) * 18 + 14, 16, 2);
            Vector2 origin = new Vector2(0, 2);
            Vector2 scale = new Vector2(1f, bounceDisplace / 2f);

            spriteBatch.Draw(texture, Position + Vector2.UnitY * 16f - basePosition, bottomFrame, color * 0.5f, 0f, origin, scale, SpriteEffects.None, 0);
        }
    }
}
