namespace CalamityFables.Particles
{
    public class KnockoutStarParticle : Particle, IDrawPixelated
    {
        public bool EmitLight;
        public float SpinTilt;
        public float VerticalSquishFactor;


        public float lightInfluence = 0.2f;

        public Color colorMult;
        public Color TintedColor(Color color) => Color.Lerp(color, color.MultiplyRGB(colorMult), lightInfluence);


        public Color primColor;
        public Color outlineColor;

        public PrimitiveTrail TrailDrawer;
        public List<Vector2> PositionCache = new();

        public override string Texture => AssetDirectory.Particles + "KnockoutStar";

        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;



        public KnockoutStarParticle(Vector2 position, Color color, Color primColor, Color outlineColor, float scale, float spinTilt, float verticalSquishFactor, bool emitLight = true)
        {
            Position = position;
            Color = color;
            Scale = scale;
            EmitLight = emitLight;
            SpinTilt = spinTilt;
            VerticalSquishFactor = verticalSquishFactor;

            this.primColor = primColor;
            this.outlineColor = outlineColor;
        }

        public override void Update()
        {
            // Update the trail.
            TrailDrawer ??= new PrimitiveTrail(30, StreakWidthFunction, StreakColorFunction);
            TrailDrawer.SetPositionsSmart(PositionCache, Position, FablesUtils.RigidPointRetreivalFunction);

            // Append positions to the trail position cache if sufficiently transparent.
            PositionCache.Add(Position);
            while (PositionCache.Count > 13)
                PositionCache.RemoveAt(0);

            // Add light if requested.
            if (EmitLight)
                Lighting.AddLight(Position, Color.ToVector3() * 0.3f);

            colorMult = Lighting.GetColor(Position.ToTileCoordinates());
        }

        public Vector2 OffsetForAngle(float angle)
        {
            Vector2 offset = angle.ToRotationVector2() * new Vector2(1f, VerticalSquishFactor);
            return offset.RotatedBy(SpinTilt);
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            // Draw the star.
            Texture2D starTexture = ParticleTexture;
            spriteBatch.Draw(starTexture, Position - basePosition, null, Color.White, Rotation, starTexture.Size() * 0.5f, Scale * 1.1f, 0, 0);
        }


        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;
        public static Color PrimColor;

        public float StreakWidthFunction(float completionRatio) => Scale * completionRatio * 3f;
        public Color StreakColorFunction(float completionRatio) => Color.Lerp(PrimColor, TintedColor(outlineColor), (1 - completionRatio) * 0.3f);

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            PrimColor = TintedColor(outlineColor);
            TrailDrawer?.Render(null, -Main.screenPosition + Vector2.UnitY * 2f);

            PrimColor = TintedColor(primColor);
            TrailDrawer?.Render(null, -Main.screenPosition);
        }
    }
}
