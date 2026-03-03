matrix uWorldViewProjection;

texture layer1Texture;
sampler2D layer1Tex = sampler_state { texture = <layer1Texture>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

float parallaxStrenght;
float2 parallaxDisplace;

float4 alphaBlend(float4 background, float4 overlay)
{
    return float4(background.rgb * (1 - overlay.a) + overlay.rgb * overlay.a, min(1, background.a + overlay.a));
}

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
    float2 coords = float2(input.TextureCoordinates.x, input.TextureCoordinates.y);
    float4 color1 = tex2D(layer1Tex, coords + parallaxDisplace * parallaxStrenght);
    return color1 * input.Color;
}

technique Technique1
{
    pass BunkerParallaxPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}