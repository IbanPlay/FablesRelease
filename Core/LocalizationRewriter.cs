using System.Reflection;
using Terraria.Localization;
using static Microsoft.Xna.Framework.Input.Keys;


//Courtesy of scalie
namespace CalamityFables.Core
{
    internal class LocalizationRewriter : ModSystem
    {
        public static readonly MethodInfo refreshInfo = typeof(LocalizationLoader).GetMethod("UpdateLocalizationFilesForMod", BindingFlags.NonPublic | BindingFlags.Static, new Type[] { typeof(Mod), typeof(string), typeof(GameCulture) });

        public override void PostSetupContent()
        {
#if DEBUG
            if (!Main.keyState.IsKeyDown(LeftControl) && false)
                refreshInfo.Invoke(null, new object[] { CalamityFables.Instance, null, Language.ActiveCulture });
#endif
        }
    }

    internal static class LocalizationRoundabout
    {
        public static readonly PropertyInfo valueProp = typeof(LocalizedText).GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        public static void SetDefault(this LocalizedText text, string value)
        {
#if DEBUG
            if (!Main.keyState.IsKeyDown(LeftControl) && false)
            {
                LanguageManager.Instance.GetOrRegister(text.Key, () => value);
                valueProp.SetValue(text, value);
            }
#endif
        }


        public static LocalizedText DefaultText(string key, string english)
        {
            LocalizedText text = Language.GetOrRegister($"Mods.CalamityFables.{key}", () => english);
            text.SetDefault(english);

            return text;
        }
    }

}
