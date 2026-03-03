using System.Reflection;
using Terraria.GameContent.Drawing;

namespace CalamityFables.Core
{
    public static class CustomBlendStates
    {
        public static BlendState Lighten = new BlendState
        {
            AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.Max,
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };

        public static BlendState Darken = new BlendState
        {
            AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.Min,
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };

        public static readonly BlendState Substract = new BlendState
        {
            AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.ReverseSubtract,
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };


        public static BlendState AdditiveNoAlpha = new BlendState
        {
            AlphaBlendFunction = BlendFunction.Add,
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.One,
            ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue,
            ColorWriteChannels1 = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue,
            ColorWriteChannels2 = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue,
            ColorWriteChannels3 = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue
        };

    }
}
