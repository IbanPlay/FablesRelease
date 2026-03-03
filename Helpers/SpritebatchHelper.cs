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
    public static class SpriteBatchHelper
    {
         
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "sortMode")]
        private static extern ref SpriteSortMode SortMode(SpriteBatch sb);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "spriteEffectPass")]
        private static extern ref EffectPass SpriteEffectPass(SpriteBatch sb);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "beginCalled")]
        private static extern ref bool SBHasBegun(SpriteBatch sb);

        public static SpriteSortMode CurrentSortMode
        {
            get => SortMode(Main.spriteBatch);
        }

        public static bool HasBegun
        {
            get => SBHasBegun(Main.spriteBatch);
        }

        public static void ApplyDefaultEffectPass()
        {
            SpriteEffectPass(Main.spriteBatch).Apply();
        }
    }
}
