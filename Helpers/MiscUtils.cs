using CalamityFables.Cooldowns;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Graphics;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.UI.Chat;
using Terraria.Utilities;
using static Terraria.GameContent.FontAssets;
using static Terraria.Player;

namespace CalamityFables.Helpers
{
    //Use this for utilies that have to do with player vanity
    public static partial class FablesUtils
    {
        public static void GetBiomeInfluences(out float corroInfluence, out float crimInfluence, out float hallowInfluence)
        {
            hallowInfluence = Math.Min(1f, Main.SceneMetrics.HolyTileCount / (float)SceneMetrics.HallowTileMax);
            corroInfluence = Math.Min(1f, Main.SceneMetrics.EvilTileCount / (float)SceneMetrics.CorruptionTileMax);
            crimInfluence = Math.Min(1f, Main.SceneMetrics.BloodTileCount / (float)SceneMetrics.CrimsonTileMax);
        }
    }
}
