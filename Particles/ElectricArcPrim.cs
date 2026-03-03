using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Particles
{
    public class ElectricArcPrim : Particle, IDrawPixelated
    {
        private DrawhookLayer drawLayer;
        public virtual DrawhookLayer layer => drawLayer;

        public override string Texture => AssetDirectory.Invisible;
        public override bool SetLifetime => true;

        public float thickness;
        internal PrimitiveTrail TrailDrawer;
        private List<Vector2> cache;
        private static Color colorUsed;
        public Vector2 endPoint;

        public Vector2 controlA;
        public Vector2 controlB;


        public BezierCurve arcCurve;

        public ElectricArcPrim(Vector2 start, Vector2 end, Vector2 leaning, float thickness, bool aboveTiles = false)
        {
            Position = start;
            endPoint = end;
            Velocity = Vector2.Zero;
            this.thickness = thickness;
            controlA = Vector2.Lerp(Position, endPoint, 0.2f) + leaning;
            controlB = Vector2.Lerp(Position, endPoint, 0.8f) + leaning;
            Lifetime = Main.rand.Next(10, 20);
            arcCurve = null;

            drawLayer = aboveTiles ? DrawhookLayer.BehindTiles : DrawhookLayer.AboveNPCs;
        }

        public ElectricArcPrim(Vector2 start, Vector2 end, Vector2 controlA, Vector2 controlB, float thickness, bool aboveTiles = false)
        {
            Position = start;
            endPoint = end;
            Velocity = Vector2.Zero;
            this.thickness = thickness;
            this.controlA = controlA;
            this.controlB = controlB;
            Lifetime = Main.rand.Next(10, 20);
            arcCurve = null;

            drawLayer = aboveTiles ? DrawhookLayer.BehindTiles : DrawhookLayer.AboveNPCs;
        }

        public ElectricArcPrim(BezierCurve arcCurve, float thickness, bool aboveTiles = false)
        {
            Position = arcCurve.ControlPoints[0];
            Velocity = Vector2.Zero;
            this.thickness = thickness;
            Lifetime = Main.rand.Next(10, 20);

            this.arcCurve = arcCurve;
            drawLayer = aboveTiles ? DrawhookLayer.BehindTiles : DrawhookLayer.AboveNPCs;
        }

        public override void Update()
        {
            ManageCache();
            ManageTrail();
        }

        public void ManageCache()
        {
            //Initialize the cache
            if (cache == null)
            {
                if (arcCurve == null)
                    arcCurve = new BezierCurve(Position, controlA, controlB, endPoint);
                cache = arcCurve.GetEvenlySpacedPoints(30);
            }

            else
                for (int i = 0; i < 30; i++)
                {
                    float degradation = (float)Math.Pow(Math.Sin(MathHelper.Pi * i / 29f), 0.4f);
                    cache[i] += Main.rand.NextVector2Circular(4f, 4f) * degradation;
                }
        }

        public virtual void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, TrailWidth, TrailColor);
            TrailDrawer.SetPositionsSmart(cache, Position, RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = Position + Velocity;
        }

        private Color TrailColor(float factor)
        {
            Color color = Color.Lerp(Color.White, Color.DeepSkyBlue, (float)Math.Pow(LifetimeCompletion, 0.76f));

            return colorUsed.MultiplyRGBA(color);

            //return Color.Lerp(new Color(255, 70, 40), new Color(255, 160, 60), (float)Math.Sin(textureCoordinates.X * 6.28f + Main.GameUpdateCount / 100f)) * (float)Math.Pow(Math.Sin((1 - textureCoordinates.X) * 3.14f), 4);
        }

        private float TrailWidth(float factor)
        {
            return 0.5f + (float)Math.Sin(factor * MathHelper.Pi) * thickness * (float)Math.Pow(1 - LifetimeCompletion, 2f);
        }


        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            colorUsed = Color.RoyalBlue with { A = 0 };
            for (int i = 0; i < 4; i++)
            {
                TrailDrawer?.Render(null, -Main.screenPosition + (i / 4f * MathHelper.TwoPi).ToRotationVector2() * 2f);
            }

            colorUsed = Color.White with { A = (byte)(200 * Math.Pow(1 - LifetimeCompletion, 4f)) };
            TrailDrawer?.Render(null, -Main.screenPosition);
        }
    }
}
