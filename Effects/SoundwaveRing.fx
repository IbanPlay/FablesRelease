matrix uWorldViewProjection;

float Width;
float Amplitude;
float Distortion;
float Repeats;
float Time;

float Amplitude2;
float Distortion2;

float4 White;
float4 HighlightColor;
float4 MidrangeColor;
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

float DistortionFunction(float2 coords, float power) 
{
    return pow(tex2D(VoronoiSampler, float2(coords.x * Repeats + Time * 0.1, Time * 0.05)).x * tex2D(VoronoiSampler, float2(coords.x * Repeats + -Time * 0.1, Time * 0.05)).x, power);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;

    // Color will be transparent if not overridden
    float4 finalColor = float4(0, 0, 0, 0);

    // Distortion value generated from noise map. Used to generate waviness
    float distortion = DistortionFunction(coords, Distortion);      

    // Furthest distance pixels are drawn based on amplitude
    float realAmplitude = Amplitude / Width;
    float maxRingDistance = realAmplitude * distortion + (1 - realAmplitude) - (1 / Width);

    // Layer over base wave function with the second one if the parameters have been set
    if (Amplitude2 != 0 || Distortion2 != 0) 
    {
        float distortion2 = DistortionFunction(coords, Distortion2);
        float realAmplitude2 = Amplitude2 / Width;
        float greaterAmplitude = max(realAmplitude, realAmplitude2);
        // The greater of the two functions will be used
        maxRingDistance = max(realAmplitude * distortion + (1 - greaterAmplitude) - (1 / Width), realAmplitude2 * distortion2 + (1 - greaterAmplitude) - (1 / Width));
        realAmplitude = greaterAmplitude;
    }

    // Width of each band. Highlight width is always 3 pixels and the mid and dark rings fill the rest
    float highlightWidth = 3 / Width;
    float midWidth = (1 - realAmplitude) * 0.67f;
    float darkWidth = (1 - realAmplitude);

    // Determine color based on Y coordinates. 1 is the ring outer edge, 0 is the inner edge
    if (coords.y <= maxRingDistance && coords.y > maxRingDistance - highlightWidth) 
    {
        // Draw white on the innermost section of the lightest band
        float bandProgress = (coords.y - maxRingDistance + highlightWidth) / highlightWidth;
        if (bandProgress < 0.67f && bandProgress > 0.33f)
            finalColor = White;
        else
            finalColor = HighlightColor;
    }
    else if (coords.y <= maxRingDistance && coords.y > maxRingDistance - midWidth) 
    {
        finalColor = MidrangeColor;
    }
    else if (coords.y <= maxRingDistance + (1 / Width) && coords.y > maxRingDistance - darkWidth) 
    {
        finalColor = ShadowColor;
    }

    return finalColor * color;
}

technique Technique1
{
    pass SoundwaveRingPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}