matrix uWorldViewProjection;
float4 colorTint;
float rand;
float time;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TextureCoordinates : TEXCOORD0;
};

float getPseudoRandom(float input)
{
    return frac(sin(input) * 43758.5453123);
}

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    
    float4 basePos = input.Position;
    
    basePos.xy *= getPseudoRandom(rand + input.TextureCoordinates.y) * 0.7 + 0.3 + sin(time * 3 + input.TextureCoordinates.y  * 6131345 + rand) * 0.25;
    
    float4 pos = mul(basePos, uWorldViewProjection);
    output.Position = pos;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    return colorTint;
}

technique Technique1
{
    pass IceShardPrimitivePass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
