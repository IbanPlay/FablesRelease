using System.Diagnostics;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public interface IVignetteModifier
    {
        bool Finished { get; }
        void Update(ref float opacity);
    }


    public class VignetteFadeEffects : ModSystem
    {
        public static List<IVignetteModifier> vignetteEffects = new List<IVignetteModifier>();
        public static float vignetteOpacityOverride;

        public static List<IVignetteModifier> fadeEffects = new List<IVignetteModifier>();
        public static float fadeOpacityOverride;


        public override void Load()
        {
            if (Main.dedServ)
                return;

            On_Main.DrawInterface += DrawVignette;
            On_Main.DoUpdate += ResetVignette;
        }

        public void DrawVignette(On_Main.orig_DrawInterface orig, Main self, GameTime gameTime)
        {
            bool spriteBatchStarted = false;

            if (vignetteOpacityOverride > 0f || vignetteEffects != null)
            {
                if (!spriteBatchStarted)
                {
                    Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default);
                    spriteBatchStarted = true;
                }

                float opacity = vignetteOpacityOverride;
                if (opacity == 0f)
                {
                    foreach (IVignetteModifier modifier in vignetteEffects)
                        modifier.Update(ref opacity);
                }

                opacity = Math.Min(1, opacity);

                Texture2D tex = ModContent.Request<Texture2D>(AssetDirectory.UI + "VignetteLight").Value;
                Rectangle targetRect = new Rectangle(Main.screenWidth / 2, Main.screenHeight / 2, (int)(Main.screenWidth * 2.5f), (int)(Main.screenHeight * 2.5f));
                Main.spriteBatch.Draw(tex, targetRect, null, Color.White * opacity, 0, tex.Size() / 2, 0, 0);
            }

            if (fadeOpacityOverride > 0f || fadeEffects.Count > 0)
            {
                if (!spriteBatchStarted)
                {
                    Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default);
                    spriteBatchStarted = true;
                }

                float opacity = fadeOpacityOverride;
                if (opacity == 0f)
                {
                    foreach (IVignetteModifier modifier in fadeEffects)
                        modifier.Update(ref opacity);
                }

                opacity = Math.Min(1, opacity);

                Texture2D tex = ModContent.Request<Texture2D>(AssetDirectory.Visible).Value;
                Main.spriteBatch.Draw(tex, Vector2.Zero, null, Color.Black * opacity, 0, Vector2.Zero, new Vector2(Main.screenWidth, Main.screenHeight), 0, 0);
            }

            if (spriteBatchStarted)
                Main.spriteBatch.End();

            orig(self, gameTime);
        }

        [DebuggerStepThrough]
        private void ResetVignette(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
        {
            if (Main.gameMenu)
            {
                orig(self, ref gameTime);
                return;
            }

            vignetteEffects.RemoveAll(e => e.Finished);
            fadeEffects.RemoveAll(e => e.Finished);

            vignetteOpacityOverride = 0f;
            fadeOpacityOverride = 0f;

            orig(self, ref gameTime);
        }

        public static void AddVignetteEffect(IVignetteModifier modifier)
        {
            vignetteEffects.Add(modifier);
        }

        public static void AddFadeEffect(IVignetteModifier modifier)
        {
            fadeEffects.Add(modifier);
        }
    }

    public class VignettePunchModifier : IVignetteModifier
    {
        private EasingFunction _easing;
        private int _framesToLast;
        private float _opacityMultiplier;
        private int _framesLasted;
        public float easingPower = 1f;

        public bool Finished {
            get;
            private set;
        }

        public VignettePunchModifier(int duration, float opacityMultiplier = 1f, EasingFunction easing = null)
        {
            _framesToLast = duration;
            _opacityMultiplier = opacityMultiplier;
            _easing = easing;

            if (_easing == null)
                _easing = LinearEasing;
        }

        public void Update(ref float opacity)
        {
            float progress = 1 - (_framesLasted / (float)_framesToLast);
            if (_easing != null)
                progress = _easing(progress, easingPower);

            opacity += progress * _opacityMultiplier;

            _framesLasted++;
            if (_framesLasted >= _framesToLast)
                Finished = true;
        }
    }

    public class VignetteBeatModifier : IVignetteModifier
    {
        private int _framesToLast;
        private float _opacityMultiplier;
        private int _framesLasted;
        public float easingPower = 1f;

        public bool Finished {
            get;
            private set;
        }

        public VignetteBeatModifier(int duration, float opacityMultiplier = 1f)
        {
            _framesToLast = duration;
            _opacityMultiplier = opacityMultiplier;
        }

        public void Update(ref float opacity)
        {
            float progress = _framesLasted / (float)_framesToLast;
            if (progress > 0.5f)
                opacity += SineInOutEasing(Utils.GetLerpValue(1f, 0.5f, progress, true)) * _opacityMultiplier;
            else
                opacity += SineInOutEasing(Utils.GetLerpValue(0f, 0.5f, progress, true)) * _opacityMultiplier;

            _framesLasted++;
            if (_framesLasted >= _framesToLast)
                Finished = true;
        }
    }


}