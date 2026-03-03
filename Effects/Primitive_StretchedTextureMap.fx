matrix uWorldViewProjection;

float stretch;
float4 frame;
float2 textureResolution;

texture sampleTexture;
sampler2D samplerTex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 color = float4(0, 0, 0, 0);

    // Prevents texture sampling if the texture is shorter than the trail length
    if (coords.x > (1 - stretch))
    {
        // Remaps coord.x depending on stretch
        float stretchedCoords = (coords.x * (1 / stretch)) - (1 / stretch) + 1;

        float2 frameUV = float2(stretchedCoords * frame.z + frame.x, coords.y * frame.w + frame.y) / textureResolution;
        float4 color = tex2D(samplerTex, frameUV);
        return color * input.Color;
    }
    return color * input.Color;
}

technique Technique1
{
    pass Primitive_StretchedTextureMap
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}