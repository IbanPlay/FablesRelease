using Terraria.Graphics.CameraModifiers;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public class PunchCameraWithEasings : ICameraModifier
    {
        private int _framesToLast;
        private Vector2 _direction;
        private float _strength;
        private int _framesLasted;
        private EasingFunction easingUsed;
        private float _easingDegree;

        public string UniqueIdentity {
            get;
            private set;
        }

        public bool Finished {
            get;
            private set;
        }

        public PunchCameraWithEasings(float maxStrenght, int frames, EasingFunction easing = null, float easingDegree = 1f, Vector2? direction = null, string uniqueIdentity = null)
        {
            if (direction == null)
                direction = Vector2.Zero;

            if (easing == null)
                easing = LinearEasing;

            easingUsed = easing;
            _easingDegree = easingDegree;

            _direction = direction.Value;
            _strength = maxStrenght * FablesConfig.Instance.ScreenshakeMultiplier;
            _framesToLast = frames;
            UniqueIdentity = uniqueIdentity;
        }

        public void Update(ref CameraInfo cameraInfo)
        {
            Vector2 usedDirection = _direction * Main.rand.NextFloat(-1f, 1f);
            if (usedDirection == Vector2.Zero)
                usedDirection = Main.rand.NextVector2CircularEdge(1f, 1f);

            float fadeOffWithTime = easingUsed(Utils.Remap(_framesLasted, 0f, _framesToLast, 1f, 0f), _easingDegree);

            cameraInfo.CameraPosition += usedDirection * fadeOffWithTime * _strength;
            _framesLasted++;
            if (_framesLasted >= _framesToLast)
                Finished = true;
        }
    }

    public class ShakeCamera : ICameraModifier
    {
        private int _framesToLast;
        private float _strength;
        private int _framesLasted;

        public string UniqueIdentity {
            get;
            private set;
        }

        public bool Finished {
            get;
            private set;
        }

        public ShakeCamera(float maxStrenght, int frames, string uniqueIdentity = null)
        {
            _strength = maxStrenght;
            _framesToLast = frames;
            UniqueIdentity = uniqueIdentity;
        }

        public void Update(ref CameraInfo cameraInfo)
        {
            cameraInfo.CameraPosition += Main.rand.NextVector2Circular(_strength, _strength);
            _framesLasted++;
            if (_framesLasted >= _framesToLast)
                Finished = true;
        }
    }

    public class ViolentlyBrutalizeCamera : ICameraModifier
    {
        private int _framesToLast;
        private float _strength;
        private int _framesLasted;
        private int _timeToNextShakeShift;

        private float _timeBetweenShakeShifts;
        private Vector2 _shakeDirection;

        public string UniqueIdentity {
            get;
            private set;
        }

        public bool Finished {
            get;
            private set;
        }

        public ViolentlyBrutalizeCamera(float maxStrenght, int frames, int shakeShifts, string uniqueIdentity = null)
        {
            _strength = maxStrenght;
            _framesToLast = frames;
            UniqueIdentity = uniqueIdentity;

            _timeBetweenShakeShifts = frames / shakeShifts;
            _timeToNextShakeShift = (int)_timeBetweenShakeShifts;

            _shakeDirection = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
        }

        public void Update(ref CameraInfo cameraInfo)
        {
            _timeToNextShakeShift--;
            if (_timeToNextShakeShift <= 0)
            {
                _timeToNextShakeShift = (int)_timeBetweenShakeShifts;
                _shakeDirection = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
            }

            float shakeBounce = Math.Max((float)Math.Sin(_timeToNextShakeShift / _timeBetweenShakeShifts * MathHelper.Pi), 0);
            float shakeShiftPower = (float)Math.Pow(shakeBounce, 0.2f);
            float strenghtFadeoff = (float)Math.Pow(1 - _framesLasted / (float)_framesToLast, 0.2f);

            cameraInfo.CameraPosition += _shakeDirection * _strength * shakeShiftPower * strenghtFadeoff;

            _framesLasted++;
            if (_framesLasted >= _framesToLast)
                Finished = true;
        }
    }


    public class DirectionalCameraTug : ICameraModifier
    {
        private int _framesToLast;
        private int _framesLasted;
        private Vector2 _direction;
        private float _oscillations;

        private EasingFunction easingUsed;
        private float _easingDegree;

        public string UniqueIdentity {
            get;
            private set;
        }

        public bool Finished {
            get;
            private set;
        }

        public DirectionalCameraTug(Vector2 direction, float oscillations, int frames, EasingFunction easing = null, float easingDegree = 2, string uniqueIdentity = null)
        {
            if (easing == null)
                easing = PolyInEasing;

            _direction = direction * FablesConfig.Instance.ScreenshakeMultiplier;
            _oscillations = oscillations;
            _framesToLast = frames;
            easingUsed = easing;
            _easingDegree = easingDegree;
            UniqueIdentity = uniqueIdentity;
        }

        public void Update(ref CameraInfo cameraInfo)
        {
            float completion = _framesLasted / (float)_framesToLast;
            cameraInfo.CameraPosition += _direction * (float)Math.Sin(MathHelper.PiOver4 + _oscillations * MathHelper.TwoPi * completion) * easingUsed(1 - completion, _easingDegree);
            _framesLasted++;
            if (_framesLasted >= _framesToLast)
                Finished = true;
        }
    }

    public class SnappyDirectionalCameraTug : ICameraModifier
    {
        private int _framesToLast;
        private int _framesLasted;
        private Vector2 _direction;
        public CurveSegment[] easings;

        public string UniqueIdentity {
            get;
            private set;
        }

        public bool Finished {
            get;
            private set;
        }

        public SnappyDirectionalCameraTug(Vector2 direction, int frames, float originalTugPercent, float risePercent, EasingFunction easeIn, float easeInDegree, EasingFunction easeOut, float easingOutDegree, string uniqueIdentity = null)
        {
            if (easeIn == null)
                easeIn = PolyInEasing;

            easings = new CurveSegment[] {
                new CurveSegment(easeIn, 0, originalTugPercent, 1 - originalTugPercent, easeInDegree),
                new CurveSegment(easeOut, risePercent, 1f, -1f, easingOutDegree) };

            _direction = direction * FablesConfig.Instance.ScreenshakeMultiplier;
            _framesToLast = frames;
            UniqueIdentity = uniqueIdentity;
        }

        public float AnimationEasing => PiecewiseAnimation(_framesLasted / (float)_framesToLast, easings);

        public void Update(ref CameraInfo cameraInfo)
        {
            cameraInfo.CameraPosition += _direction * AnimationEasing;
            _framesLasted++;
            if (_framesLasted >= _framesToLast)
                Finished = true;
        }
    }
}