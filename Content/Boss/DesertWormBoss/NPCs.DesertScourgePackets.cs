namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public partial class DesertScourge : ModNPC
    {
        [Serializable]
        public class DesertScourgeSubstatePacket : Module
        {
            int index;
            int substateFrom;

            Vector2 position;
            Vector2 velocity;
            float[] ai;

            public DesertScourgeSubstatePacket(DesertScourge scourge)
            {
                index = scourge.NPC.whoAmI;
                this.substateFrom = (int)scourge.SubState;

                position = scourge.NPC.position;
                velocity = scourge.NPC.velocity;
                ai = (float[])scourge.NPC.ai.Clone();
            }

            protected override void Receive()
            {
                NPC npc = Main.npc[index];
                if (npc.type != ModContent.NPCType<DesertScourge>() || npc.ModNPC is not DesertScourge scourge)
                    return;

                //Makes sure we don't skip substates, and also just in case we got synced already
                if ((int)scourge.SubState != substateFrom)
                    return;

                float empty = 0f;
                scourge.AttackTimer = 0;
                scourge.ActBasedOnSubstate(true, true, ref empty, ref empty, ref empty, ref empty);

                //Set everything to how it is
                npc.position = position;
                npc.velocity = velocity;
                for (int i = 0; i < 4; i++)
                    npc.ai[i] = ai[i];
            }
        }

        [Serializable]
        public class DesertScourgeIntroAnimPacket : Module
        {
            int index;
            Vector2 grubbyPosition;
            Vector2 grubbySize;

            public DesertScourgeIntroAnimPacket(DesertScourge scourge, NPC grubby)
            {
                index = scourge.NPC.whoAmI;
                grubbyPosition = grubby.position;
                grubbySize = grubby.Size;
            }

            protected override void Receive()
            {
                NPC npc = Main.npc[index];
                if (npc.type != ModContent.NPCType<DesertScourge>() || npc.ModNPC is not DesertScourge scourge)
                    return;

                NPC grubby = scourge.FindTargetedGrub(out bool foundGrubby);
                if (foundGrubby)
                    return;
                else
                {
                    for (int k = 0; k < 3; k++)
                    {
                        Dust.NewDust(grubbyPosition, (int)grubbySize.X, (int)grubbySize.Y, 238,  0, -1f, 0, default, 1f);
                    }

                    for (int k = 0; k < 20; k++)
                    {
                        Dust.NewDust(grubbyPosition, (int)grubbySize.X, (int)grubbySize.Y, 238, 0, -1f, 0, default, Main.rand.NextFloat(0.8f, 1.2f));
                    }
                    

                    SoundEngine.PlaySound(SoundID.NPCDeath37, grubbyPosition);
                    SoundEngine.PlaySound(SpawnBiteSound, grubbyPosition);
                    scourge.mandibleJerkiness = -0.2f;
                    scourge.AttackTimer = 0;
                    CameraManager.Shake += 35;
                    CameraManager.UnHideUI();
                }
            }
        }

        [Serializable]
        public class DesertScourgeDespawnTailPacket : Module
        {
            int[] indexes;
            int npcIndex;

            public DesertScourgeDespawnTailPacket(DesertScourge scourge)
            {
                npcIndex = scourge.NPC.whoAmI;
                indexes = new int[SegmentCount];
                int nextHitbox = scourge.nextHitbox;
                int i = 0;
                while (ValidSegment(nextHitbox, scourge.NPC))
                {
                    indexes[i] = nextHitbox;
                    i++;
                    nextHitbox = (int)Main.npc[nextHitbox].ai[1];
                }
            }

            protected override void Receive()
            {
                NPC npc = Main.npc[npcIndex];
                for (int i = 0; i < indexes.Length; i++)
                    if (ValidSegment(indexes[i], npc))
                        Main.npc[indexes[i]].active = false;
            }
        }
    }
}
