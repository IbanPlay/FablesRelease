namespace CalamityFables.Particles
{
    public class SoundwaveRing : PrimitiveRingParticle
    {
        public override bool ApplyOpacity => false;

        public int Repeats { get; set; } = 2;
        public float DistortionStrength { get; set; } = 0.65f;

        protected Color Highlight;
        protected float WaveAmplitude;

        public SoundwaveRing(Vector2 position, Vector2 velocity, Color highlight, Color color, float maxRadius, float maxWidth, float amplitude, float opacity = 1f, int lifeTime = 30) : base(position, velocity, color, maxRadius, maxWidth, opacity, lifeTime)
        {
            Highlight = highlight;
            WaveAmplitude = amplitude;

            // Specific width calculations
            MinWidth = 5 + amplitude * 2;
            MaxWidth = Math.Max(MaxWidth, MinWidth);
        }

        public override void ExtraEffects(ref Effect effect)
        {
            float amplitude = Math.Min(Width - 5, WaveAmplitude);
            float amplitude2 = Math.Min(Width - 5, WaveAmplitude * 2);

            effect = Scene["SoundwaveRing"].GetShader().Shader;
            effect.Parameters["Width"].SetValue(Width);
            effect.Parameters["Amplitude"].SetValue(amplitude);
            effect.Parameters["Distortion"].SetValue(DistortionStrength);
            effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly * 6.5f);
            effect.Parameters["Repeats"].SetValue(Repeats);
            effect.Parameters["Voronoi"].SetValue(AssetDirectory.NoiseTextures.Voronoi.Value);

            // Optional second layer of distortion
            effect.Parameters["Amplitude2"].SetValue(amplitude2);
            effect.Parameters["Distortion2"].SetValue(2);

            Color highlight = Highlight * Opacity;
            Color baseColor = Color * Opacity;
            // White fades to the highlight color with opacity
            Color white = Color.Lerp(highlight, Color.White, Opacity);

            effect.Parameters["White"].SetValue(white.ToVector4());
            effect.Parameters["HighlightColor"].SetValue(highlight.ToVector4());
            effect.Parameters["MidrangeColor"].SetValue(baseColor.ToVector4());
            effect.Parameters["ShadowColor"].SetValue(baseColor.ToVector4() * 0.5f);
        }
    }
}