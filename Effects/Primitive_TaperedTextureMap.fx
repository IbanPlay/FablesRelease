matrix uWorldViewProjection;
float time;
float fadeDistance;
float fadePower;
texture trailTexture;
sampler2D TrailSampler = sampler_state { texture = <trailTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

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
    
    // Read the fade map as a streak.
    float4 fadeMapColor = tex2D(TrailSampler, float2(frac(coords.x - time * 2.5), coords.y));
    float opacity = fadeMapColor.r;
    
    // Fade out at the edges of the trail to give a blur effect.
    // This is very important.
    float power = lerp(10, 3, coords.x);
    opacity = lerp(pow(sin(coords.y * 3.141), power), opacity, 1 - coords.x);
    
    // Fade out at the end of the streak.
    if (coords.x < fadeDistance)
        opacity *= pow(coords.x / fadeDistance, fadePower);
    return color * opacity * 1.5;
}

technique Technique1
{
    pass Primitive_TaperedTextureMapPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
