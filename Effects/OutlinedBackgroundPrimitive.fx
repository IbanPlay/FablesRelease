matrix uWorldViewProjection;

float2 resolution;
float thickness;
float4 outlineColor;
float4 backgroundColor;

float time;

texture outlineNoiseTexture;
sampler2D outlineNoiseTex = sampler_state { texture = <outlineNoiseTexture>;  magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

texture centerNoiseTexture;
sampler2D centerNoiseTex = sampler_state { texture = <centerNoiseTexture>;  magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };


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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates * resolution;
    
    //TODO eventually make the outline part of the mesh instead of being done through the shader, because it doesnt work good with the warped UVs
    float outlineMask = (uv.y < thickness) + (uv.y >= resolution.y - thickness) + (uv.x < thickness) + (uv.x >= resolution.x - thickness);
    outlineMask = min(1, outlineMask);
    
    float4 color = lerp(backgroundColor, outlineColor, outlineMask);
    float noiseScaleMult = lerp(0.6, 0.15, outlineMask);
    
    color *= max(0.8, tex2D(outlineNoiseTex, (uv * 0.001 + float2(time * 0.6, 0)) * noiseScaleMult).x * tex2D(outlineNoiseTex, (uv * 0.002 + float2(0, time * 0.5)) * noiseScaleMult).x);
    return color;
}

technique Technique1
{
    pass OutlinedBackgroundPrimitivePass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}