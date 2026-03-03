global using CalamityFables.Core;
global using CalamityFables.Helpers;
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using ReLogic.Content;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using Terraria;
global using Terraria.Audio;
global using Terraria.GameContent;
global using Terraria.ID;
global using Terraria.ModLoader;
global using static Terraria.Graphics.Effects.Filters;
global using NetEasy;
global using CalamityFables.Packets;
using System.IO;

namespace CalamityFables
{
    public class CalamityFables : Mod
    {
        public static CalamityFables Instance { get; set; }
        internal Mod CalamityTheOriginal = null;
        public static bool CalamityEnabled = false;

        internal static Mod STARLIGHTRIVER = null;
        public static bool SLREnabled = false;

        internal static Mod SpiritReforged = null;
        public static bool SpiritEnabled = false;

        internal static Mod Remnants = null;
        public static bool RemnantsEnabled = false;


        /// <summary>
        /// If enabled, Nautilus's dialogue flags reset on world exit
        /// When generating a world fill the world with sandstone so its only nautiluses chamber
        /// Additionally, if enabled doesn't save keepsakes
        /// </summary>
        internal static readonly bool NautilusDemo = false;

        /// <summary>
        /// Controls if DS still spawns after killing it once
        /// </summary>
        internal static readonly bool DesertScourgeDemo = false;

        /// <summary>
        /// If enabled, the game will always crabulon as not having been defeat previously
        /// This means that he will always passively spawn
        /// </summary>
        internal static readonly bool CrabulonDemo = false;

        public CalamityFables()
        {
            Instance = this;
        }

        public override void Load()
        {
            CalamityTheOriginal = null;
            CalamityEnabled = ModLoader.TryGetMod("CalamityMod", out CalamityTheOriginal);

            STARLIGHTRIVER = null;
            SLREnabled = ModLoader.TryGetMod("StarlightRiver", out STARLIGHTRIVER);

            SpiritReforged = null;
            SpiritEnabled = ModLoader.TryGetMod("SpiritReforged", out SpiritReforged);

            Remnants = null;
            RemnantsEnabled = ModLoader.TryGetMod("Remnants", out Remnants);
        }

        public override void PostSetupContent()
        {
            NetEasy.NetEasy.Register(this);
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            NetEasy.NetEasy.HandleModule(reader, whoAmI);
        }

        public override object Call(params object[] args) => FablesModCalls.Call(args);
    }
}