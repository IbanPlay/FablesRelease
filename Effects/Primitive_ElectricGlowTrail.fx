matrix uWorldViewProjection;

texture sampleTexture;
sampler2D glowSampler = sampler_state{ texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float repeats;
float scroll;

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


float invlerp(float from, float to, float value)
{
    return clamp((value - from) / (to - from), 0.0, 1.0);
}

// The X coordinate is the trail completion, the Y coordinate is the same as any other.
// This is simply how the primitive TextCoord is layed out in the C# code.
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    float glowMask = tex2D(glowSampler, float2(coords.x * repeats - scroll, 0.25 + coords.y * 0.5)).x;
    

    //Add the glowing core on top
    float4 returnColor = lerp(glowMask, color, 0.1) * glowMask;
   // returnColor += glowMask * float4(lerp(float3(1, 1, 1), color.xyz, 0.6), 0) * coords.x;
    
    returnColor += step(0.65 - coords.x * 0.05, glowMask);
    returnColor += invlerp(0.0, 0.6, glowMask) * color * 0.7 ;
    
    returnColor.a = 0;
    return returnColor;
}

technique Technique1
{
    pass Primitive_ElectricGlowTrailPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
