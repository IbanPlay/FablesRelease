matrix uWorldViewProjection;

float Width;
float Amplitude;
float Repeats;
float WaveEnd;
float DistortionAmplitude;
float Time;

float4 White;
float4 HighlightColor;
float4 ShadowColor;

texture Voronoi;
sampler2D VoronoiSampler = sampler_state 
{ 
    texture = <Voronoi>; 
    magfilter = LINEAR; 
    minfilter = LINEAR; 
    mipfilter = LINEAR; 
    AddressU = wrap; 
    AddressV = wrap; 
};

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
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
	output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

#define PI 3.14159

float WaveFunction(float x)
{
    return Amplitude * sin((Repeats / WaveEnd * PI * x) - 2 * Time) * (1 - x / WaveEnd);
}

float DistortionFunction(float x, float time)
{
    return DistortionAmplitude * (2 * tex2D(VoronoiSampler, float2(x + time * 0.1f, time * 0.05f)).x - 1);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;

    // Color will be transparent if not overridden
    float4 finalColor = float4(0, 0, 0, 0);

    // Distortion value generated from noise map. Used to generate waviness
    float lineOffset = DistortionFunction(coords.x, Time) * DistortionFunction(coords.x, -Time);

    // Add wave distortion
    if (coords.x <= WaveEnd)
        lineOffset += WaveFunction(coords.x);
    else if (coords.x >= 1 - WaveEnd)
        lineOffset += WaveFunction(1 - coords.x);
    
    float lineWidth = 5 / Width;
    float lineStart = 0.5f - lineWidth * 0.5f + (lineOffset / Width);
    float lineEnd = 0.5f + lineWidth * 0.5f + (lineOffset / Width);
    
    if (coords.y > lineStart && coords.y < lineEnd)
    {
        float lineProgress = (coords.y - lineEnd + lineWidth) / lineWidth;
        
        if (lineProgress > 0.4f && lineProgress < 0.6f)
            finalColor = White;
        else if (lineProgress > 0.2f && lineProgress < 0.8f)
            finalColor = HighlightColor;
        else
            finalColor = ShadowColor;
    }
    
    return finalColor * color;
}

technique Technique1
{
    pass SoundwaveRingPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}