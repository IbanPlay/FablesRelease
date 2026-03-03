using CalamityFables.Helpers;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace CalamityFables.Content.UI
{
    public class AnimatedModIcon : ILoadable
    {
        //I LOVE REFLECTION!!!!

        private static readonly Type UIModItemType = typeof(ModItem).Assembly.GetType("Terraria.ModLoader.UI.UIModItem");
        private static readonly MethodInfo InitializeModItemUIMethod = UIModItemType?.GetMethod("OnInitialize", BindingFlags.Instance | BindingFlags.Public);

        private static readonly PropertyInfo ModNameProperty = UIModItemType?.GetProperty("ModName", BindingFlags.Instance | BindingFlags.Public);
        private static readonly FieldInfo ModIconField = UIModItemType?.GetField("_modIcon", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ModNameElement = UIModItemType?.GetField("_modName", BindingFlags.Instance | BindingFlags.NonPublic);

        public delegate void orig_OnInitialize(UIElement self);
        public delegate void hook_UpdateLifeRegen(orig_OnInitialize orig, UIElement self);
        public static Hook SwagModIconUpLogo;

        public static string FablesModName;

        public void Load(Mod mod)
        {
            if (InitializeModItemUIMethod == null || ModIconField == null || ModNameProperty == null)
            {
                FablesUtils.LogILEpicFail("Add custom logo visuals", "One of the reflected fields couldn't be found!");
                return;
            }

            SwagModIconUpLogo = new Hook(InitializeModItemUIMethod, AppendOurCoolUIStuffToIcon);
            FablesModName = mod.Name;
        }

        public void AppendOurCoolUIStuffToIcon(orig_OnInitialize orig, UIElement element)
        {
            orig(element);

            if (!element.GetType().IsAssignableTo(UIModItemType))
                return;

            object potentialModName = ModNameProperty.GetValue(element);
            if (potentialModName == null || potentialModName is not string modName || modName != FablesModName)
                return;

            object potentiallyTheIcon = ModIconField.GetValue(element);
            if (potentiallyTheIcon is UIImage modIconImage)
            {
                FancyModIconUI addedDrawLogic = new FancyModIconUI((UIText)ModNameElement.GetValue(element));
                modIconImage.Append(addedDrawLogic);
                modIconImage.Color = Color.Transparent;
            }
        }

        public void Unload()
        {
            if (InitializeModItemUIMethod == null || ModIconField == null || SwagModIconUpLogo == null)
                return;

            SwagModIconUpLogo.Undo();
            SwagModIconUpLogo.Dispose();
            SwagModIconUpLogo = null;
        }
    }

    public class FancyModIconUI : UIElement
    {
        public UIText ModName;

        public FancyModIconUI(UIText nameUI)
        {
            Width.Set(80, 0f);
            Height.Set(80, 0f);

            ModName = nameUI;
        }

        public static Asset<Texture2D> Background;
        public static Asset<Texture2D> BackgroundMap;
        public static Asset<Texture2D> BackgroundPalette;

        public static Asset<Texture2D> SquigglePalette;

        public static Asset<Texture2D> InsigniaBloom;
        public static Asset<Texture2D> InsigniaFill;
        public static Asset<Texture2D> InsigniaOutline;
        public static Asset<Texture2D> InsigniaShadow;

        public override void Update(GameTime gameTime)
        {
            if (ModName == null)
                return;

            Color deepTeal = new Color(47, 80, 55);
            Color glowingGold = new Color(255, 205, 012) * 0.6f;

            if ((DateTime.Now.Month == 4 && DateTime.Now.Day == 1))
            {
                ModName.SetText("Spirit Fables v0.1");
                glowingGold = new Color(40, 200, 255) * 0.6f;
            }

            ModName.TextColor = deepTeal;
            ModName.ShadowColor = glowingGold with { A = (byte)((0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly)) * 20) };
        }

        private static readonly RasterizerState OverflowHiddenRasterizerState = new RasterizerState
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true
        };

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            float defaultBloomHue = 38f;

            //Spirit easter egg

            if ((DateTime.Now.Month == 4 && DateTime.Now.Day == 1))
            {
                if (Background == null)
                {
                    Background ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "Spirit/SpiritBackground");
                    BackgroundPalette ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "Spirit/SpiritBackgroundPalette");

                    SquigglePalette ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "Spirit/SpiritSquigglePalette");

                    InsigniaBloom ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "Spirit/SpiritBloom");
                    InsigniaFill ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "Spirit/SpiritFill");
                    InsigniaOutline ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "Spirit/SpiritOutline");
                    InsigniaShadow ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "Spirit/SpiritShadow");
                }

                defaultBloomHue = 220f;
            }

            Background ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "ModIconBackground");
            BackgroundMap ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "ModIconBackgroundMap");
            BackgroundPalette ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "ModIconBackgroundPalette");

            SquigglePalette ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "ModIconSquigglePalette");

            InsigniaBloom ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "ModIconInsigniaBloom");
            InsigniaFill ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "ModIconInsigniaFill");
            InsigniaOutline ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "ModIconInsigniaOutline");
            InsigniaShadow ??= ModContent.Request<Texture2D>(AssetDirectory.ModIcon + "ModIconInsigniaShadow");



            Effect effect = Scene["AnimatedModIcon"].GetShader().Shader;
            effect.Parameters["backgroundRampTexture"].SetValue(BackgroundMap.Value);
            effect.Parameters["backgroundPaletteTexture"].SetValue(BackgroundPalette.Value);
            effect.Parameters["squigglePaletteTexture"].SetValue(SquigglePalette.Value);
            effect.Parameters["insigniaFillTexture"].SetValue(InsigniaFill.Value);
            effect.Parameters["insigniaOutlineTexture"].SetValue(InsigniaOutline.Value);
            effect.Parameters["insigniaBloomTexture"].SetValue(InsigniaBloom.Value);
            effect.Parameters["insigniaShadowTexture"].SetValue(InsigniaShadow.Value);

            //Color goldBloom = new Color(56, 47, 32);

            float bloomhue = defaultBloomHue + MathHelper.Lerp(0f, (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 130f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f));
            if (bloomhue < 0)
                bloomhue += 1;
            else if (bloomhue > 1)
                bloomhue -= 1;

            Color bloomTinted = Main.hslToRgb(new Vector3(bloomhue / 360f, 0.27f, 0.17f));
            effect.Parameters["bloomTint"].SetValue(bloomTinted.ToVector4());
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Vector2 mousePos = new Vector2(Main.mouseX, Main.mouseY);
            Vector2 centerOfIcon = dimensions.Center();

            float glow = Utils.GetLerpValue(500f, 60f, mousePos.Distance(centerOfIcon), true);
            glow *= 0.3f + 0.7f * (0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.4f));

            effect.Parameters["intensify"].SetValue(1f + glow * 0.5f);

            TurnOnShader(dimensions, spriteBatch, effect, out RasterizerState prevRasterizer, out Rectangle prevCrop);

            //Draw gradient
            Texture2D background = Background.Value;
            Vector2 drawPos = dimensions.Position();
            spriteBatch.Draw(background, drawPos - Vector2.One * 0.5f , null, Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            //Remove the crop
            spriteBatch.End();
            spriteBatch.GraphicsDevice.ScissorRectangle = prevCrop;
            spriteBatch.GraphicsDevice.RasterizerState = prevRasterizer;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, prevRasterizer, null, Main.UIScaleMatrix);
        }

        public void TurnOnShader(CalculatedStyle dimensions, SpriteBatch spriteBatch, Effect effect, out RasterizerState previousRasterizer, out Rectangle previousCrop)
        {
            previousCrop = Main.graphics.GraphicsDevice.ScissorRectangle;
            previousRasterizer = Main.graphics.GraphicsDevice.RasterizerState;

            spriteBatch.End();
            Vector2 topLeft = new Vector2(dimensions.X, dimensions.Y);
            Vector2 bottomRight = topLeft + new Vector2(dimensions.Width, dimensions.Height);
            topLeft = Vector2.Transform(topLeft, Main.UIScaleMatrix);
            bottomRight = Vector2.Transform(bottomRight, Main.UIScaleMatrix);

            Rectangle clippingRectangle = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)(bottomRight.X - topLeft.X), (int)(bottomRight.Y - topLeft.Y));
            Rectangle adjustedClippingRectangle = Rectangle.Intersect(clippingRectangle, spriteBatch.GraphicsDevice.ScissorRectangle);

            spriteBatch.GraphicsDevice.ScissorRectangle = adjustedClippingRectangle;
            spriteBatch.GraphicsDevice.RasterizerState = OverflowHiddenRasterizerState;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, effect, Main.UIScaleMatrix);
        }
    }
}
