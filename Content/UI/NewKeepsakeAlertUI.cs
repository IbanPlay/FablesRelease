using CalamityFables.Particles;
using Terraria.UI;

namespace CalamityFables.Content.UI
{
    public class NewKeepsakeAlertUI : SmartUIState
    {
        public override int InsertionIndex(List<GameInterfaceLayer> layers) => layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
        public override bool UpdatesWhileInvisible => true;

        public override bool Visible {
            get {
                bool visible = Main.playerInventory && Main.EquipPageSelected != 2;

                if (visible && !KeepsakeRackUI.NewKeepsakeCheckItOut)
                    visible = false;
                return visible;
            }
        }


        public static Vector2 equipmentTabPosition;
        List<Particle> shimmerSparks;
        public float shimmerSpawnTimer;

        public override void OnInitialize()
        {
            shimmerSparks = new List<Particle>();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            foreach (Particle particle in shimmerSparks)
            {
                if (particle == null)
                    continue;
                particle.Position += particle.Velocity;
                particle.Time++;
                particle.Update();
            }
            //Clear out particles whose time is up
            shimmerSparks.RemoveAll(particle => (particle.Time >= particle.Lifetime && particle.SetLifetime));

            if (Visible)
            {
                if (Main.rand.NextBool(10) && shimmerSparks.Count < 4)
                {
                    Color sparkColor = Main.rand.Next(new Color[] { Color.Gold, Color.Coral, Color.IndianRed });

                    shimmerSparks.Add(new UIGlowSpark(
                        equipmentTabPosition + Main.rand.NextVector2Circular(20f, 20f) * Main.UIScale,
                        Vector2.Zero,
                        Color.White,
                        sparkColor,
                        Main.rand.NextFloat(0.8f, 1.4f),
                        Main.rand.Next(20, 26),
                        0.04f,
                        2f));
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            foreach (Particle sharticle in shimmerSparks)
            {
                sharticle.CustomDraw(spriteBatch, Vector2.Zero);
            }
        }

    }
}
