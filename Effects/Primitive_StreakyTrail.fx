matrix uWorldViewProjection;
float time;
float repeats;
float verticalStretch;

float overlayScroll;
float overlayOpacity;
float streakScale;

texture sampleTexture;
sampler2D samplerTex = sampler_state{ texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

texture streakNoiseTexture;
sampler2D streakNoiseTex = sampler_state{ texture = <streakNoiseTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };




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
    //Sample the base sampling texture with the provided repeat tiling and vertical stretch
    float2 stretchedUV = float2(input.TextureCoordinates.x * repeats, verticalStretch / 2 + input.TextureCoordinates.y * (1 - verticalStretch));
    float3 color = tex2D(samplerTex, stretchedUV + float2(time, 0)).xyz;
    
    float3 overlayColor = tex2D(samplerTex, stretchedUV + float2(overlayScroll, 0)).xyz * overlayOpacity;
    
    float streakOpacity = tex2D(streakNoiseTex, float2(stretchedUV.y * streakScale, time * 0.5)).x;
    
    return float4((color + overlayColor) * input.Color.rgb * (1.0 + color.x * 2.0), color.x * input.Color.w) * streakOpacity;
}

technique Technique1
{
    pass Primitive_StreakyTrailPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
