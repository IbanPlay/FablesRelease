using CalamityFables.Content.Boss.MushroomCrabBoss;

namespace CalamityFables.Particles
{
    public class SporeGas : Particle
    {
        public override string Texture => AssetDirectory.Particles + "Smoke";

        public int counter;
        public float Spin;
        public float dustSpawnRate;
        public Vector2 sporeOrigin;
        public float radius;

        public override bool SetLifetime => false;

        public override int FrameVariants => 3;
        public override bool Important => true;

        public virtual bool DoDust => true;
        public virtual Color EffectColor => new Color(30, 32, 176);

        public SporeGas(Vector2 position, Vector2 velocity, Vector2 origin, float radius, float scale, float rotationSpeed = 0f)
        {
            Position = position;
            Velocity = velocity;
            sporeOrigin = origin;
            this.radius = radius;
            Scale = scale;
            counter = Main.rand.Next(60);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationSpeed;
            Variant = Main.rand.Next(3);
        }

        public bool forceNoDust = false;

        public override void Update()
        {
            Rotation += Spin * ((Velocity.X > 0) ? 1f : -1f);

            //Slow down when too far from the sporebud (avoids unclear hitboxes)
            if (Position.DistanceSQ(sporeOrigin) > radius * radius)
            {
                Velocity *= 0.96f;
                Scale *= 0.99f;
            }

            //Fly up
            if (Velocity.Y > -2 && counter > 140)
                Velocity.Y -= 0.05f * Math.Min(1f, (counter - 140) / 60f);

            //Fade out
            if (counter > 100)
            {
                Scale += 0.01f;
                counter += 2;
            }
            else
            {
                Scale *= 0.985f;
                counter += 4;
            }
            //Dissapear
            if (counter >= 255)
                Kill();


            Lighting.AddLight(Position, Color.ToVector3() * 5.5f);

            //Slowly fade in
            float opacity = 0.1f + 0.15f * Math.Min(1f, counter / 90f);
            Color = EffectColor * opacity;
            Color.A = (byte)(Math.Min(Color.A * 0.5f, 80)); //Color is always at least a bit glowy

            //Fade off in terms of opacity
            if (counter > 150)
                Color *= (float)Math.Pow(1 - (counter - 150) / 105f, 1.2f);

            //Slightly turn into the bg color
            Color backgroundColor = Lighting.GetColor(Position.ToTileCoordinates());
            Color = Color.Lerp(Color, Color.MultiplyRGBA(backgroundColor), 0.7f);

            //Start bright and fade away
            if (counter < 150)
            {
                byte oldA = Color.A;
                Color *= 1 + 2f * (float)Math.Pow(1 - counter / 150f, 4);
                Color.A = oldA;
            }

            if (DoDust && !forceNoDust && counter < 200 && Main.rand.NextFloat() < 0.01f * (0.3f + 0.7f * (1 - (counter / 255f))))
            {
                Dust sparks = Dust.NewDustPerfect(Position + Main.rand.NextVector2Circular(3f, 3f) * Scale, ModContent.DustType<SporeBudDust>(), Main.rand.NextVector2CircularEdge(10f, 10f));
                sparks.velocity = Main.rand.NextVector2Circular(1f, 1f);
                sparks.noGravity = true;
                sparks.customData = Color.RoyalBlue * 0.3f;
                sparks.rotation = MathHelper.PiOver4;
                sparks.scale = Main.rand.NextFloat(0.5f, 0.75f);
                sparks.color = Color.Lerp(Color * (255 / (float)Color.A), Main.rand.NextBool() ? Color.RoyalBlue : Color.CornflowerBlue, 0.7f);
                sparks.alpha = Main.rand.Next(110);
            }
        }
    }

    public class JungleSporeGas : SporeGas
    {
        public override bool DoDust => false;
        public float colorShift;

        public override Color EffectColor => Color.Lerp(base.EffectColor, Color.Olive, FablesUtils.PolyInOutEasing(colorShift, 5f));

        public JungleSporeGas(Vector2 position, Vector2 velocity, Vector2 origin, float radius, float scale, float rotationSpeed = 0f, float colorShift = 1f) : base(position, velocity, origin, radius, scale, rotationSpeed)
        {
            this.colorShift = colorShift;
        }
    }
}
