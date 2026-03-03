matrix uWorldViewProjection;
float scroll;
float repeats;
float4 outlineColor;
float4 fadeInColor;

texture sampleTexture;
sampler2D samplerTex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = clamp;
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
   //Taken from spirit ily spirit <3 shoutouts to them
    //Wait im not sure if this comment was copypasted or not, it might not be spirit
    float2 coords = float2((input.TextureCoordinates.x * repeats) + scroll, input.TextureCoordinates.y);
    float4 color = tex2D(samplerTex, coords);
    
    float4 color2 = tex2D(samplerTex, float2((input.TextureCoordinates.x * repeats * 0.7f) + scroll * 0.5, input.TextureCoordinates.y));
    
    color *= color2;
    
    //outline in the green channel
    float outlineMask = color.g > 0;
    float4 returnColor = outlineColor * outlineMask + lerp(float4(0, 0, 0, 1), fadeInColor, color.r) * (1 - outlineMask);
    
    return returnColor * input.Color * color.a;
}

technique Technique1
{
    pass TarTrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}