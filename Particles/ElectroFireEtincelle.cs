using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Particles
{
    public class ElectroFireEtincelle : Particle, IDrawPixelated
    {
        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;

        public override string Texture => AssetDirectory.Invisible;
        public override bool SetLifetime => true;

        public float thickness;
        internal PrimitiveTrail TrailDrawer;
        private List<Vector2> cache;
        public int streakLenght;
        public bool drawingOutline;

        public ElectroFireEtincelle(Vector2 position, Vector2 velocity, int streakLenght = 20, float lineThickness = 2f, int lifetime = 0)
        {
            Position = position;
            Velocity = velocity;
            Scale = lineThickness;
            Lifetime = lifetime == 0 ? Main.rand.Next(30, 80) : lifetime;
            this.streakLenght = streakLenght;
        }

        public override void Update()
        {
            ManageCache();
            ManageTrail();

            Velocity += Vector2.UnitY * 0.3f;

            if (Main.rand.NextBool(30))
            {
                Particle particle = new GenericSparkle(Position, Velocity, Color.White, Color.DarkOrange, 1f, 10, 0f, 3f);
                particle.Rotation = 0f;
                ParticleHandler.SpawnParticle(particle);
            }

            if (Main.rand.NextBool(7))
            {
                Dust d = Dust.NewDustPerfect(Position, 6, Vector2.Zero, 200, default, Main.rand.NextFloat(0.4f, 1.3f));
                d.noGravity = true;
            }
        }

        public void ManageCache()
        {
            //Initialize the cache
            if (cache == null)
            {
                cache = new List<Vector2>(streakLenght);
                for (int i = 0; i < streakLenght; i++)
                    cache.Add(Position);
            }

            cache.Add(Position);
            while (cache.Count > streakLenght)
                cache.RemoveAt(0);
        }

        public virtual void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(streakLenght, TrailWidth, TrailColor);
            TrailDrawer.SetPositionsSmart(cache, Position, RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = Position + Velocity;
        }

        private Color TrailColor(float factor)
        {
            Color color = Color.Lerp(Color.DodgerBlue, Color.DarkOrange, (float)Math.Pow(LifetimeCompletion, 0.4f));

            color = Color.Lerp(color, Color.White, (float)(Math.Pow(factor, 14f) * Math.Pow(1 - LifetimeCompletion, 0.5f)));

            color.A = (byte)((1 - factor) * 250f);

            if (drawingOutline)
            {
                color = Color.Lerp(color, Color.DarkRed with { A = color.A }, 0.4f);
                color.A = (byte)Math.Min(255, color.A + 70);

                color *= 0.2f;
            }

            if (LifetimeCompletion > 0.55f)
                color *= 1 - (LifetimeCompletion - 0.55f) / 0.45f;

            return color;
        }

        private float TrailWidth(float factor)
        {
            return (float)Math.Pow(factor, 2f) * Scale * (float)Math.Pow(1 - LifetimeCompletion, 0.2);
        }


        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            drawingOutline = true;
            for (int i = 0; i < 4; i++)
            {
                TrailDrawer?.Render(null, -Main.screenPosition + (i / 4f * MathHelper.TwoPi).ToRotationVector2() * 2f);
            }

            drawingOutline = false;
            TrailDrawer?.Render(null, -Main.screenPosition);
        }
    }
}
