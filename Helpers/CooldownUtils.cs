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
    public static partial class FablesUtils
    {
        public static bool HasCooldown(this Player p, string id)
        {
            if (p is null)
                return false;
            CooldownsPlayer modPlayer = p.GetModPlayer<CooldownsPlayer>();
            return !(modPlayer is null) && modPlayer.cooldowns.ContainsKey(id);
        }

        public static bool FindCooldown(this Player p, string id, out CooldownInstance cooldown)
        {
            cooldown = null;
            if (p is null)
                return false;
            CooldownsPlayer modPlayer = p.GetModPlayer<CooldownsPlayer>();

            bool cooldownFound = modPlayer.cooldowns.TryGetValue(id, out var cd);
            cooldown = cd;
            return cooldownFound;
        }

        /// <summary>
        /// Applies the specified cooldown to the player, creating a new instance automatically.<br/>
        /// By default, overwrites existing instances of this cooldown, but this behavior can be disabled.
        /// </summary>
        /// <param name="p">The player to whom the cooldown should be applied.</param>
        /// <param name="id">The string ID of the cooldown to apply. This is referenced against the Cooldown Registry.</param>
        /// <param name="duration">The duration, in frames, of this instance of the cooldown.</param>
        /// <param name="overwrite">Whether or not to overwrite any existing instances of this cooldown. Defaults to true.</param>
        /// <returns>The cooldown instance which was created. <b>Note the cooldown is always created, but may not be necessarily applied to the player.</b></returns>
        public static CooldownInstance AddCooldown(this Player p, string id, int duration, bool overwrite = true)
        {
            var cd = CooldownLoader.Get(id);
            CooldownInstance instance = new CooldownInstance(p, cd, duration);

            bool alreadyHasCooldown = p.HasCooldown(id);
            if (!alreadyHasCooldown || overwrite)
            {
                CooldownsPlayer modPlayer = p.GetModPlayer<CooldownsPlayer>();
                modPlayer.cooldowns[id] = instance;

                if (instance.handler.MultiplayerSynced)
                    modPlayer.SyncCooldownAddition(Main.netMode == NetmodeID.Server, instance);
            }

            return instance;
        }

        /// <summary>
        /// Applies the specified cooldown to the player, creating a new instance automatically.<br/>
        /// By default, overwrites existing instances of this cooldown, but this behavior can be disabled.
        /// </summary>
        /// <param name="p">The player to whom the cooldown should be applied.</param>
        /// <param name="id">The string ID of the cooldown to apply. This is referenced against the Cooldown Registry.</param>
        /// <param name="duration">The duration, in frames, of this instance of the cooldown.</param>
        /// <param name="overwrite">Whether or not to overwrite any existing instances of this cooldown. Defaults to true.</param>
        /// <param name="handlerArgs">Arbitrary extra arguments to pass to the CooldownHandler constructor via reflection.</param>
        /// <returns>The cooldown instance which was created. <b>Note the cooldown is always created, but may not be necessarily applied to the player.</b></returns>
        public static CooldownInstance AddCooldown(this Player p, string id, int duration, bool overwrite = true, params object[] handlerArgs)
        {
            var cd = CooldownLoader.Get(id);
            CooldownInstance instance = new CooldownInstance(p, cd, duration, handlerArgs);

            bool alreadyHasCooldown = p.HasCooldown(id);
            if (!alreadyHasCooldown || overwrite)
                p.GetModPlayer<CooldownsPlayer>().cooldowns[id] = instance;

            return instance;
        }

        public static bool RemoveCooldown(this Player p, string id)
        {
            if (!p.HasCooldown(id))
                return false;

            CooldownsPlayer cooldownHaver = p.GetModPlayer<CooldownsPlayer>();
            cooldownHaver.cooldowns.Remove(id);
            cooldownHaver.SyncCooldownRemoval(Main.netMode == NetmodeID.Server, new List<string>() { id });

            return true;
        }

        public static IList<CooldownInstance> GetDisplayedCooldowns(this Player p)
        {
            List<CooldownInstance> ret = new List<CooldownInstance>(16);
            if (p is null || p.GetModPlayer<CooldownsPlayer>() is null)
                return ret;

            foreach (CooldownInstance instance in p.GetModPlayer<CooldownsPlayer>().cooldowns.Values)
                if (instance.handler.ShouldDisplay)
                    ret.Add(instance);
            return ret;
        }
    }
}
