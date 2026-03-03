using System.IO;
using Terraria.ModLoader.IO;

namespace CalamityFables.Core
{
    public class WorldProgressionSystem : ModSystem
    {
        private static bool defeatedDesertScourge = false;
        public static bool DefeatedDesertScourge {
            get => defeatedDesertScourge;
            set {
                if (!value)
                    defeatedDesertScourge = false;
                else
                    NPC.SetEventFlagCleared(ref defeatedDesertScourge, -1);

                ParasiteCoreSystem.CalamityDesertScourgeDownedProperty?.SetValue(null, value);
            }
        }

        private static bool defeatedCrabulon = false;
        public static bool DefeatedCrabulon {
            get => defeatedCrabulon && !CalamityFables.CrabulonDemo;
            set {
                if (!value)
                    defeatedCrabulon = false;
                else
                    NPC.SetEventFlagCleared(ref defeatedCrabulon, -1);

                ParasiteCoreSystem.CalamityCrabulonDownedProperty?.SetValue(null, value);
            }
        }
        public static int crabulonsDefeated = 0;
        public static bool encounteredCrabulon = false;

        public override void ClearWorld()
        {
            defeatedDesertScourge = false;
            defeatedCrabulon = false;
            crabulonsDefeated = 0;
            encounteredCrabulon = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["defeatedDesertScourge"] = defeatedDesertScourge;
            tag["defeatedCrabulon"] = defeatedCrabulon;
            tag["crabulonsDefeated"] = crabulonsDefeated;
            tag["encounteredCrabulon"] = encounteredCrabulon;

        }
        public override void LoadWorldData(TagCompound tag)
        {
            defeatedDesertScourge = tag.GetOrDefault<bool>("defeatedDesertScourge");
            defeatedCrabulon = tag.GetOrDefault<bool>("defeatedCrabulon");
            if (tag.TryGet("crabulonsDefeated", out int crabKills))
                crabulonsDefeated = crabKills;
            encounteredCrabulon = tag.GetOrDefault<bool>("encounteredCrabulon");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.WriteFlags(
                defeatedDesertScourge,
                defeatedCrabulon,
                encounteredCrabulon);

            writer.Write(crabulonsDefeated);
        }

        public override void NetReceive(BinaryReader reader)
        {
            reader.ReadFlags(
                out defeatedDesertScourge,
                out defeatedCrabulon,
                out encounteredCrabulon);
            crabulonsDefeated = reader.ReadInt32();
        }
    }
}
