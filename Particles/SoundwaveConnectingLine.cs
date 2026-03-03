namespace CalamityFables.Particles
{
    public class SoundwaveConnectingLine : ConnectingLineParticle
    {
        public override bool ApplyOpacity => false;

        protected Color Highlight;
        protected float WaveAmplitude;

        public SoundwaveConnectingLine(Color highlight, Color color, float width, int lifeTime, float amplitude, float opacity, params List<Vector2> positions) : base(color, width, lifeTime, opacity, positions)
        {
            Highlight = highlight;
            WaveAmplitude = amplitude;
        }

        public override void ExtraEffects(ref Effect effect)
        {
            float amplitude = Math.Min((Width - 5) / 2, WaveAmplitude);
            float distortionAmplitude = (Width - 5) / 2 - amplitude;

            effect = Scene["SoundwaveLine"].GetShader().Shader;
            effect.Parameters["Width"].SetValue(Width);
            effect.Parameters["Amplitude"].SetValue(amplitude);
            effect.Parameters["Repeats"].SetValue(4);
            effect.Parameters["WaveEnd"].SetValue(0.25f);
            effect.Parameters["DistortionAmplitude"].SetValue(distortionAmplitude);
            effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly * 5);
            effect.Parameters["Voronoi"].SetValue(AssetDirectory.NoiseTextures.Voronoi.Value);

            Color highlight = Highlight * Opacity;
            Color baseColor = Color * Opacity;
            // White fades to the highlight color with opacity
            Color white = Color.Lerp(highlight, Color.White, Opacity);

            effect.Parameters["White"].SetValue(white.ToVector4());
            effect.Parameters["HighlightColor"].SetValue(highlight.ToVector4());
            effect.Parameters["ShadowColor"].SetValue(baseColor.ToVector4() * 0.5f);
        }
    }
}