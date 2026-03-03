using CalamityFables.Particles;
using Terraria.DataStructures;
using static CalamityFables.Content.Boss.MushroomCrabBoss.Crabulon;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    [Autoload(false)]
    public class CrabulonLimbsGoreSimulator : Particle
    {
        public override string Texture => AssetDirectory.Invisible;
        public override bool Important => true;
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public List<CrabulonLeg> LegGores;
        public List<CrabulonArm> ArmGores;
        public List<Vector2> velocities = new List<Vector2>();

        private Crabulon dummyCrabulon;
        public Vector2 deadCrabulonPosition; //Necessary because in multiplayer the position gets reset? I don't exactly know how, i assume its syncing stuff messing with the ModNPC when it dies

        public CrabulonLimbsGoreSimulator(Crabulon deadCrabulon)
        {
            LegGores = deadCrabulon.Legs;
            ArmGores = deadCrabulon.Arms;
            dummyCrabulon = deadCrabulon;
            dummyCrabulon.lookingSideways = false;
            dummyCrabulon.TopDownView = true; //Top down view so the whole arm draws at once
            dummyCrabulon.SubState = ActionState.Dead_InternalGoreSimulation;
            deadCrabulonPosition = dummyCrabulon.NPC.position;

            Lifetime = 120;

            foreach (CrabulonLeg leg in LegGores)
            {
                Vector2 velocityImprint = -Vector2.UnitY * Main.rand.NextFloat(0.1f, 6f) + Vector2.UnitX * leg.Direction * Main.rand.NextFloat(1f, 1.2f);

                foreach (VerletPoint point in leg.LimpingLeg.points)
                {
                    Vector2 localImprint = velocityImprint;
                    localImprint.X *= Main.rand.NextFloat(1f, 2f);
                    localImprint.Y *= Main.rand.NextFloat(1f, 3f);
                    if (Main.rand.NextBool(4))
                        localImprint.Y *= -1;

                    point.oldPosition = point.position - localImprint;
                    point.locked = false;
                }

            }

            foreach (CrabulonArm arm in ArmGores)
            {
                Vector2 velocityImprint = -Vector2.UnitY * Main.rand.NextFloat(2f, 6f);
                velocityImprint.X += Main.rand.NextFloat(0.1f, 1f) * (arm.largeClaw ? -1f : 1f);
                int i = 0;

                foreach (VerletPoint point in arm.LimpingArm.points)
                {
                    Vector2 localImprint = velocityImprint;
                    localImprint.X *= Main.rand.NextFloat(1f, 2f + i * 3f);
                    localImprint.Y *= Main.rand.NextFloat(1f, 3f);


                    point.oldPosition = point.position - localImprint;
                    point.locked = false;
                    i++;
                }
            }
        }

        public override void Update()
        {
            //Gotta keep setting it back to that for multiplayer purposes
            dummyCrabulon.SubState = ActionState.Dead_InternalGoreSimulation;
            dummyCrabulon.NPC.position = deadCrabulonPosition;
            dummyCrabulon.AttackTimer = LifetimeCompletion;

            foreach (CrabulonLeg leg in LegGores)
            {
                leg.LimpUpdate();
                leg.legOriginGraphic = leg.LimpingLeg.points[0].position;
            }

            foreach (CrabulonArm arm in ArmGores)
            {
                arm.LimpUpdate();

            }
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Color baseColor = Lighting.GetColor(dummyCrabulon.NPC.Center.ToTileCoordinates());
            float lerper = 0.4f + 0.5f * LifetimeCompletion;

            foreach (CrabulonLeg leg in LegGores)
            {
                Color lightColor = Lighting.GetColor(leg.legOriginGraphic.ToTileCoordinates());
                leg.Draw(spriteBatch, basePosition, Color.Lerp(baseColor, lightColor, lerper) * (1 - LifetimeCompletion));
            }
            foreach (CrabulonArm arm in ArmGores)
            {
                Color lightColor = Lighting.GetColor(arm.Anchor.ToTileCoordinates());
                arm.Draw(spriteBatch, basePosition, Color.Lerp(baseColor, lightColor, lerper) * (1 - LifetimeCompletion), ArmDrawLayer.BehindBody);
            }
        }
    }
}
