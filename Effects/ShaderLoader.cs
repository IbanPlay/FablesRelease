using System.Reflection;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader.Core;
using static Terraria.ModLoader.Core.TmodFile;

namespace CalamityFables.Effects
{
    public class ShaderLoader : ModSystem //Shoutouts to slr for making me aware of automatically loading this stuff thru reflection
    {
        public override void Load()
        {
            if (Main.dedServ)
                return;

            MethodInfo info = typeof(Mod).GetProperty("File", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true);
            var file = (TmodFile)info.Invoke(CalamityFables.Instance, null);
            var shaders = file.Where(n => n.Name.StartsWith("Effects/") && n.Name.EndsWith(".xnb"));

            foreach (FileEntry entry in shaders)
            {
                var name = entry.Name.Replace(".xnb", "").Replace("Effects/", "");
                var path = entry.Name.Replace(".xnb", "");
                LoadShader(name, path);
            }

        }

        public static void LoadShader(string name, string path)
        {
            var screenRef = new Ref<Effect>(CalamityFables.Instance.Assets.Request<Effect>(path, AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene[name] = new Filter(new ScreenShaderData(screenRef, name + "Pass"), EffectPriority.High);
            Filters.Scene[name].Load();
        }
    }
}
