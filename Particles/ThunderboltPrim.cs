using Steamworks;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Particles
{
    public class ThunderboltPrim : Particle, IDrawPixelated
    {
        public virtual DrawhookLayer layer => DrawhookLayer.BehindTiles;

        public override string Texture => AssetDirectory.Invisible;
        public override bool SetLifetime => true;

        public float thickness;
       
        private List<LightningZap> lightningArcs = new List<LightningZap>();

        public Vector2 endPoint;
        public bool hasFade;

        public int ArcCount;
        public int DelayBetweenArcs;
        public int ArcLifetime;

        private int arcSpawnTimer = 0;
        private int spawnedArcs = 0;

        public ThunderboltPrim(Vector2 start, Vector2 end, bool faded, int arcCount = 3, int delayBetweenArcs = 4, int arcTime = 8)
        {
            Position = start;
            endPoint = end;
            Velocity = Vector2.Zero;
            hasFade = faded;
            Lifetime = arcTime * arcCount;

            ArcCount = arcCount;
            DelayBetweenArcs = delayBetweenArcs;
            ArcLifetime = arcTime;
        }

        public override void Update()
        {
            if (arcSpawnTimer == 0)
            {
                lightningArcs.Add(new(Position, endPoint, ArcLifetime, hasFade));
                arcSpawnTimer = DelayBetweenArcs;
                spawnedArcs++;
            }
            else if (spawnedArcs < ArcCount)
                arcSpawnTimer--;


            for (int i = lightningArcs.Count - 1; i >= 0; i--)
            {
                LightningZap z = lightningArcs[i];
                z.Update();
                if (z.timeLeft < 0)
                {
                    lightningArcs.Remove(z);
                    Vector2 dustVelNormal = (endPoint - Position).SafeNormalize(Vector2.Zero);

                    for (int j = 0; j < 15; j++)
                    {
                        Vector2 dustPos = z.trailDrawer.Positions[j * 2] + Main.rand.NextVector2Circular(4f, 10f);
                        Dust d = Dust.NewDustPerfect(dustPos, 206, dustVelNormal * Main.rand.NextFloat(1f, 5f), 200, Color.White, Main.rand.NextFloat(0.9f, 1.4f));
                        d.noLightEmittence = !Main.rand.NextBool(6);
                        d.noLight = true;
                    }
                }
            }

            if (lightningArcs.Count > 0)
            {
                DelegateMethods.v3_1 = Color.Blue.ToVector3() * 0.5f;
                Utils.PlotTileLine(Position, endPoint, 8f, DelegateMethods.CastLightOpen);
            }
        }




        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            foreach (LightningZap z in lightningArcs)
                z.Render();
        }


        public class LightningZap
        {
            internal PrimitiveTrail trailDrawer;
            private static Color colorUsed;
            internal bool hasFade;

            public LightningZap(Vector2 start, Vector2 end, int time, bool hasFade)
            {
                this.start = start;
                this.end = end;
                timeLeft = time;
                lifeTime = time;
                this.hasFade = hasFade;

                Vector2 joint1Pos = Vector2.Lerp(start, end, 0.33f);
                Vector2 joint2Pos = Vector2.Lerp(start, end, 0.66f);

                float minJointBend = 28f; // 18f
                float maxJointBend = 46f; //30f;


                Vector2 perpendicular = (start - end).RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero);
                joint1Pos += perpendicular * Main.rand.NextFloat(minJointBend, maxJointBend) * (Main.rand.NextBool() ? 1 : -1);
                joint2Pos += perpendicular * Main.rand.NextFloat(minJointBend, maxJointBend) * (Main.rand.NextBool() ? 1 : -1);

                Vector2 joint1velocity = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 joint2velocity = Main.rand.NextVector2CircularEdge(1f, 1f);

                joint1 = new Vector4(joint1Pos, joint1velocity.X, joint1velocity.Y);
                joint2 = new Vector4(joint2Pos, joint2velocity.X, joint2velocity.Y);
            }

            public Vector2 start;
            public Vector2 end;
            public int timeLeft;
            public int lifeTime;
            public float Completion => 1 - timeLeft / (float)lifeTime;

            public Vector4 joint1;
            public Vector4 joint2;

            public void Update()
            {
                float jointMoveSpeed = 0.4f;
                joint1 = new Vector4(joint1.X + joint1.Z * jointMoveSpeed, joint1.Y + joint1.W * jointMoveSpeed, joint1.Z, joint1.W);
                joint2 = new Vector4(joint2.X + joint2.Z * jointMoveSpeed, joint2.Y + joint2.W * jointMoveSpeed, joint2.Z, joint2.W);
                ManageTrail();
                timeLeft--;
            }


            public virtual void ManageTrail()
            {
                trailDrawer = trailDrawer ?? new PrimitiveTrail(30, TrailWidth, TrailColor);
                trailDrawer.SetPositions(new Vector2[] {start, joint1.XY(), joint2.XY(), end}, RigidPointRetreivalFunction);
                trailDrawer.NextPosition = end;
            }

            private Color TrailColor(float factor)
            {
                Color color = Color.Lerp(Color.White, Color.DeepSkyBlue, (float)Math.Pow(Completion, 0.76f));
                if (hasFade)
                    color *= factor;

                return colorUsed.MultiplyRGBA(color);
            }

            private float TrailWidth(float factor)
            {
                return 4f * (float)Math.Pow(1 - Completion, 2f) + 18f * (float)Math.Pow(1 - Completion, 8f);
            }

            public void Render()
            {
                colorUsed = Color.RoyalBlue with { A = 0 };
                for (int i = 0; i < 4; i++)
                {
                    trailDrawer?.Render(null, -Main.screenPosition + (i / 4f * MathHelper.TwoPi).ToRotationVector2() * 2f);
                }

                colorUsed = Color.White with { A = (byte)(200 * Math.Pow(1 - Completion, 4f)) };
                trailDrawer?.Render(null, -Main.screenPosition);
            }


        }
    }
}
