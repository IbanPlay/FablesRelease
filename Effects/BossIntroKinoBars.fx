matrix uWorldViewProjection;

texture tireScratch;
sampler2D tireTexture = sampler_state{ texture = <tireScratch>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float fadeInPercent;
float fadeOutPercent;
float2 fadeStretch;
float2 fadeScroll;
texture fadeNoise;
sampler2D fadeTexture = sampler_state{ texture = <fadeNoise>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float2 scroll;
float2 horizontalRepeats;
float texturePercent;

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
    float2 coords = input.TextureCoordinates;
    
    float verticalUV = 1 - texturePercent + coords.y * texturePercent;
    float4 tire1 = tex2D(tireTexture, float2(coords.x * horizontalRepeats.x + scroll.x, verticalUV));
    float4 tire2 = tex2D(tireTexture, float2(coords.x * horizontalRepeats.y + scroll.y, verticalUV));
    float fade = tex2D(fadeTexture, coords * fadeStretch + fadeScroll).x;
    
    //Use the two scrolling tire scratches to get a good endless random looking paint stroke effect
    float opacity = min(1, tire1.a + tire2.a);
    
    
    float fadeProgress = smoothstep(fadeOutPercent - 1, fadeOutPercent, coords.x);
    opacity *= 0 + fadeProgress * lerp(1, fade, fadeOutPercent);
    
    fadeProgress = smoothstep(fadeInPercent - 1, fadeInPercent, 1 - coords.x);
    opacity *= 0 + fadeProgress * lerp(1, fade, fadeInPercent);
    
    return input.Color * opacity;
}

technique Technique1
{
    pass BossIntroKinoBarsPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
