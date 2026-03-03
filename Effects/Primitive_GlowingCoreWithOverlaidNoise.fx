matrix uWorldViewProjection;

texture sampleTexture;
sampler2D glowSampler = sampler_state{ texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

texture overlayNoise;
sampler2D overlayNoiseTex = sampler_state{ texture = <overlayNoise>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float repeats;
float scroll;
float coreShrink;
float coreOpacity;

float overlayRepeats;
float overlayScroll;
float overlayVerticalScale;

float overlayMaxOpacityOverlap = 1;


struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

// The X coordinate is the trail completion, the Y coordinate is the same as any other.
// This is simply how the primitive TextCoord is layed out in the C# code.
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    float4 glowCoreColor = tex2D(glowSampler, float2(coords.x * repeats - scroll, coords.y));
    float4 glowCoreColorZoom = tex2D(glowSampler, float2(coords.x * repeats - scroll, coreShrink / 2 + coords.y * (1 - coreShrink)));
    
    float4 streakColor = tex2D(overlayNoiseTex, float2(coords.x * overlayRepeats - overlayScroll, overlayVerticalScale / 2 + coords.y * (1 - overlayVerticalScale)));
    
    //The opacity of the color is dictated by a combination of the "core" noise and the overlay noise
    float opacity = min(overlayMaxOpacityOverlap, glowCoreColorZoom.r + streakColor.r);
    color = (lerp(color, glowCoreColorZoom, 0.1) + streakColor * color) * opacity;
    
    //Add the glowing core on top
    color += glowCoreColor.x * float4(lerp(float3(1, 1, 1), color.xyz, 0.6), 0) * coreOpacity * coords.x;
    
    color.a = 0;
    return color;
}

technique Technique1
{
    pass Primitive_GlowingCoreWithOverlaidNoisePass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
