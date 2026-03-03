namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public partial class Crabulon : ModNPC
    {
        [Serializable]
        public class CrabulonSubstatePacket : Module
        {
            int index;
            int substateFrom;

            Vector2 position;
            Vector2 velocity;
            float[] ai;

            public CrabulonSubstatePacket(Crabulon crab)
            {
                index = crab.NPC.whoAmI;
                this.substateFrom = (int)crab.SubState;

                position = crab.NPC.position;
                velocity = crab.NPC.velocity;
                ai = (float[])crab.NPC.ai.Clone();
            }

            protected override void Receive()
            {
                NPC npc = Main.npc[index];
                if (npc.type != ModContent.NPCType<Crabulon>() || npc.ModNPC is not Crabulon crab)
                    return;

                //Makes sure we don't skip substates, and also just in case we got synced already
                if ((int)crab.SubState != substateFrom)
                    return;

                crab.AttackTimer = 0;

                if (crab.AIState == ActionState.Chasing)
                    crab.IdleMotion();
                else if (crab.AIState == ActionState.Charge)
                    crab.ChargeAttack();
                else if (crab.AIState == ActionState.SporeMines)
                    crab.SporeMineAttack();
                else if (crab.AIState == ActionState.SporeBomb)
                    crab.SporeBombAttack();
                else if (crab.AIState == ActionState.HuskDrop)
                    crab.HuskDrop();
                else if (crab.AIState == ActionState.Slam)
                    crab.ClawSlam();
                else if (crab.AIState == ActionState.Snip)
                    crab.ClawSnip();
                else if (crab.AIState == ActionState.Slingshot)
                    crab.Slingshot();
                else if (crab.AIState == ActionState.Desperation)
                    crab.DesperationSlams();
                else if (crab.AIState == ActionState.Despawning)
                    crab.DespawnBehavior();
                else if (crab.AIState == ActionState.Raving)
                    crab.RaveOnACorpse();
                else if (crab.AIState == ActionState.Dead)
                    crab.DeathRagdoll();
                else if (crab.AIState == ActionState.SpawningUp)
                    crab.SpawnAnimation();
                else if (crab.AIState == ActionState.ClentaminatedAway)
                    crab.ClentaminatorDeath();

                //Set everything to how it is
                npc.position = position;
                npc.velocity = velocity;
                for (int i = 0; i < 4; i++)
                    npc.ai[i] = ai[i];
            }
        }
    }
}
