using CalamityFables.Cooldowns;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Graphics;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    public static class ProjectileHelper
    {         
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "CutTilesAt")]
        private static extern void PCutTilesAt(Projectile projectile, Vector2 boxPosition, int boxWidth, int boxHeight);

        public static void CutTilesAt(this Projectile projectile, Vector2 boxPosition, int boxWidth, int boxHeight) => PCutTilesAt(projectile, boxPosition, boxWidth, boxHeight);
        public static void CutTilesAtPosition(this Projectile projectile) => PCutTilesAt(projectile, projectile.position, projectile.width, projectile.height);
    }
}
