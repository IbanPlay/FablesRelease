using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Content.Tiles.Graves;
using MonoMod.Cil;
using NetEasy;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using static CalamityFables.Helpers.FablesUtils;
using static Mono.Cecil.Cil.OpCodes;
using static Terraria.ModLoader.ModContent;
using MonoMod.RuntimeDetour;
using Terraria.Utilities;
using System.IO;

namespace CalamityFables.Core
{
    public class ModdedMoons : ILoadable
    {
        public const int MODDED_MOONS_COUNT = 16;
        public static int VanillaMoonCount;

        public void Load(Mod mod)
        {
            //Add our own moons and resize the vanilla array
            VanillaMoonCount = TextureAssets.Moon.Length;
            Array.Resize(ref TextureAssets.Moon, VanillaMoonCount + MODDED_MOONS_COUNT);

            if (!Main.dedServ)
                for (int i = 0; i < MODDED_MOONS_COUNT; i++)
                    TextureAssets.Moon[VanillaMoonCount + i] = Request<Texture2D>(AssetDirectory.Assets + "Misc/Moon" + (1 + i));

            IL_WorldGen.RandomizeMoonState += IL_WorldGen_RandomizeMoonState;

            //Actually not needed because it syncs as a byte
            //FablesGeneralSystemHooks.NetSendEvent += SyncMoonProperly;
            //FablesGeneralSystemHooks.NetReceiveEvent += RecieveMoonProperly;
        }

        private void SyncMoonProperly(BinaryWriter writer)
        {
            writer.Write(Main.moonType);
        }

        private void RecieveMoonProperly(BinaryReader reader)
        {
            Main.moonType = reader.ReadInt32();
        }


        private void IL_WorldGen_RandomizeMoonState(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdcI4(out int _),
                i => i.MatchCallvirt<UnifiedRandom>("Next"),
                i => i.MatchStsfld<Main>("moonType")))
            {
                LogILEpicFail("Add extra moons", "Coud not locate Rand.Next(9) call");
                return;
            }

            cursor.Remove();
            cursor.EmitDelegate(GetMoonCount);
        }

        public static int GetMoonCount()
        {
            return TextureAssets.Moon.Length;
        }

        public void Unload() 
        {
            int totalMoonCount = TextureAssets.Moon.Length;
            Asset<Texture2D> firstMoon = Request<Texture2D>(AssetDirectory.Assets + "Misc/Moon1");
            int fablesMoonStart = -1;

            for (int i = 0; i < totalMoonCount; i++)
            {
                if (TextureAssets.Moon[i] == firstMoon)
                {
                    fablesMoonStart = i;
                    break;
                }
            }

            //In case another mod also added modded moons to the list we need to shift the contents after fables moons to be where they used to be
            if (totalMoonCount > VanillaMoonCount + MODDED_MOONS_COUNT)
            {
                int m = 0;
                for (int i = fablesMoonStart + MODDED_MOONS_COUNT; i < totalMoonCount; i++)
                {
                    TextureAssets.Moon[fablesMoonStart + m] = TextureAssets.Moon[i];
                    m++;
                }
            }

            Array.Resize(ref TextureAssets.Moon, totalMoonCount - MODDED_MOONS_COUNT);
        }
    }

}
