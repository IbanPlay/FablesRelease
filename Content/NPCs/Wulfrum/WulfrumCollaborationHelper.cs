using Terraria.DataStructures;
using Terraria.ModLoader.Utilities;

namespace CalamityFables.Content.NPCs.Wulfrum
{
    public class WulfrumPairings : ILoadable
    {
        public void Load(Mod mod)
        {
            FablesNPC.SpawnNPCEvent += SpawnWulfrumCombos;
        }

        private void SpawnWulfrumCombos(int npc, int tileX, int tileY)
        {
            //Makes the wulfrum enemies spawn extra enemies
            if (Main.expertMode && Main.rand.NextBool(4) && Main.npc[npc].ModNPC is ISuperchargable)
            {
                EntitySource_SpawnNPC source = new EntitySource_SpawnNPC();

                if (Main.npc[npc].type == ModContent.NPCType<WulfrumMortar>())
                    NPC.NewNPC(source, tileX * 16, tileY * 16, ModContent.NPCType<WulfrumRover>());

                if (Main.npc[npc].type == ModContent.NPCType<WulfrumRover>())
                    NPC.NewNPC(source, tileX * 16, tileY * 16, ModContent.NPCType<WulfrumMortar>());

                if (Main.npc[npc].type == ModContent.NPCType<WulfrumGrappler>())
                    NPC.NewNPC(source, tileX * 16, tileY * 16, ModContent.NPCType<WulfrumMagnetizer>());

                if (Main.npc[npc].type == ModContent.NPCType<WulfrumMagnetizer>())
                    NPC.NewNPC(source, tileX * 16, tileY * 16, ModContent.NPCType<WulfrumRoller>());

                if (Main.npc[npc].type == ModContent.NPCType<WulfrumRoller>())
                    NPC.NewNPC(source, tileX * 16, tileY * 16, ModContent.NPCType<WulfrumGrappler>());

            }
        }

        public void Unload() { }
    }

    public static class WulfrumCollaborationHelper
    {
        public static float WulfrumGoonSpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.PlayerSafe || (!spawnInfo.Player.ZoneForest && !spawnInfo.Player.ZoneSnow))
                return 0f;

            float multiplier = spawnInfo.Player.ZoneSnow ? 0.4f : 1f;

            return SpawnCondition.OverworldDaySlime.Chance * (Main.hardMode ? 0.020f : 0.115f) * multiplier;
        }

        public static void Rover_StartingRollerCombo(this NPC rover)
        {
            //LocalAI = 1 means its got a friend
            rover.localAI[0] = 1;
        }

        public static void Rover_GetChargedByRoller(this NPC rover, float timeBetweenElectrifications)
        {
            //LocalAI = 2 means its being charged (done to spin the cog up faster)
            rover.localAI[0] = 2;


            //Electrify the shield by augmenting the rover's fourth ai variable
            //The fourth ai variable is used both for the shield size increase (0 to 1), and if it goes above 2, the shield will get electrified

            if (rover.ai[0] > 0)
            {
                if (rover.ai[3] < 1)
                {
                    rover.ai[3] = MathHelper.Lerp(rover.ai[3], 1, 0.2f);
                    rover.ai[3] += 0.02f;
                }

                else
                    rover.ai[3] += 1 / (60f * timeBetweenElectrifications);
            }
        }

        public static void Rover_StopBeingCharged(this NPC rover)
        {
            //LocalAI = 1 means its got a friend
            rover.localAI[0] = 0;

            rover.netUpdate = true;
            rover.netSpam = 0;
        }


        public static void Magnetizer_GetExplodedByMortar(this NPC magnet)
        {
            WulfrumMagnetizer magnetron = magnet.ModNPC as WulfrumMagnetizer;
            if (magnetron.AttachedDebris.Count > 0)
            {
                magnetron.ActionTimer = 10f;
                magnetron.TimeUntilNextAction = 90f;
            }
        }

        public static void Roller_BeMagnetized(this NPC roller, float cogRotationOverride)
        {
            roller.ai[0] = 0;
            roller.ai[3] = 1f;
            roller.localAI[2] = cogRotationOverride;
        }

        public static void Roller_PreventDashes(this NPC roller)
        {
            roller.ai[2] = 0;
            roller.ai[1] = 0;
        }

        public static void Roller_StartToGlow(this NPC roller, float glowPercent)
        {
            roller.ai[2] = 1;
            roller.ai[1] = 0.75f + (1 - glowPercent) * 0.25f;
        }

        public static void Roller_ForceDash(this NPC roller)
        {
            roller.ai[0] = 1f;
            roller.ai[1] = 0f;
            roller.ai[2] = 1f;
            roller.ai[3] = 0f;
        }
    }


}
