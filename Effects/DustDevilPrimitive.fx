matrix uWorldViewProjection;

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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    //The x in the uvs is the "mask for which half we are on. Positive x means we want the front face, negative x means we want the negative face
    float mask = input.TextureCoordinates.x; 
    
    //return float4(1, 1, 1, 1);
    
    //If the mask and the y in the Uvs (which represents the fake 3D dimension) have the same sign, it'll be positive and the color won't be masked
    //If the mask and the third dimension don't, it'll be negative, and then get turned into a zero from the max 
    return input.Color * max(0, sign(input.TextureCoordinates.y) * mask);
}

technique Technique1
{
    pass DustDevilPrimitivePass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
