using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    #region interface and struct
    public interface IChromaAbberationModifier
    {
        bool Finished { get; }
        void Update(out ChromaticAbberationParameters parameters);
    }
    public struct ChromaticAbberationParameters
    {
        public float ChromaticAbberationDirection;
        public float ChromaticAbberationStrenght;
        public float ChromaticAbberationOpacity;

        public ChromaticAbberationParameters(float direction, float strenght, float opacity)
        {
            ChromaticAbberationDirection = direction;
            ChromaticAbberationStrenght = strenght;
            ChromaticAbberationOpacity = opacity;
        }
    }
    #endregion

    //I ripped off a lot of this from CameraModifiers
    public class ChromaticAbberationManager : ModSystem
    {
        private static IChromaAbberationModifier _modifier = null;

        public override void PostUpdateEverything()
        {
            if (Main.dedServ)
                return;

            if (_modifier != null && _modifier.Finished)
                _modifier = null;

            if (_modifier == null)
            {
                if (Scene["ChromaticAbberationBasic"].IsActive())
                    Scene["ChromaticAbberationBasic"].Deactivate();
                return;
            }

            Effect shader = Scene["ChromaticAbberationBasic"].GetShader().Shader;
            _modifier.Update(out ChromaticAbberationParameters parameters);

            parameters.ChromaticAbberationStrenght *= FablesConfig.Instance.ChromaticAbberationMultiplier;

            shader.Parameters["strenght"].SetValue(parameters.ChromaticAbberationStrenght);
            shader.Parameters["rotation"].SetValue(parameters.ChromaticAbberationDirection);
            shader.Parameters["opacity"].SetValue(parameters.ChromaticAbberationOpacity);
            shader.Parameters["pixelSize"].SetValue(1 / (float)Main.screenWidth);

            if (Main.netMode != NetmodeID.Server && !Scene["ChromaticAbberationBasic"].IsActive())
            {
                Scene.Activate("ChromaticAbberationBasic").GetShader().UseProgress(0f).UseColor(Color.White.ToVector3());
            }
        }

        public static void Add(IChromaAbberationModifier modifier, bool overridePrevious = false)
        {
            if (!overridePrevious && _modifier != null)
                return;

            _modifier = modifier;
        }
    }

    //This is an adaptation of PunchCameraModifier
    public class ChromaShakeModifier : IChromaAbberationModifier
    {
        private EasingFunction _easing;
        private int _framesToLast;
        private float _direction;
        private float _strength;
        private int _framesLasted;

        public bool Finished {
            get;
            private set;
        }

        public ChromaShakeModifier(float direction, float strength, int duration, EasingFunction easing = null)
        {
            _easing = easing;
            _direction = direction;
            _strength = strength;
            _framesToLast = duration;
        }

        public void Update(out ChromaticAbberationParameters chromaInfo)
        {
            float shakeFactor = _strength * Main.rand.NextFloat(0.4f, 1f);

            float shakeFade = Utils.Remap(_framesLasted, 0f, _framesToLast, 1f, 0f);
            if (_easing != null)
                shakeFade = _easing(shakeFade, 1);

            float opacity = shakeFade;

            chromaInfo = new ChromaticAbberationParameters(_direction, shakeFactor * shakeFade, opacity);

            _framesLasted++;
            if (_framesLasted >= _framesToLast)
                Finished = true;
        }
    }
}