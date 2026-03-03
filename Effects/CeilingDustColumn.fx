matrix uWorldViewProjection;

texture noise;
sampler2D noiseTexture = sampler_state
{
    texture = <noise>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture dustNoise;
sampler2D dustNoiseTexture = sampler_state
{
    texture = <dustNoise>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture rgbNoise;
sampler2D rgbNoiseTex = sampler_state
{
    texture = <rgbNoise>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float columnHeight;
float time;
float2 resolution;
float2 dustThresholds;

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

float invLerp(float from, float to, float v)
{
    return clamp((v - from) / (to - from), 0, 1);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float stretch = 16 / columnHeight;
    
    //First, get the fade for the effect (downwards fade using the UV, and side fade using the input color
    //I can't make the color fade with height because of weird interpolation issues that makes the segments not smooth looking
    float sideFade = invLerp(0, 0.4, input.Color.g) * invLerp(1, 0.6, input.Color.g);
    sideFade = floor(sideFade * 5) / 5;
    float downFade = 1 - min(1, input.TextureCoordinates.y);
    
    float opacityMask = sideFade * downFade * input.Color.r;
    
    //Grime is a layer on the main glow noise to tint  it with some random noise to make it look like super subtle coloration on the light rays
    float3 grime = tex2D(dustNoiseTexture, coords * float2(0.5 * stretch, 0.5) + float2(0, -time * 0.02)).rgb;
    grime *= tex2D(dustNoiseTexture, coords * float2(0.5 * stretch, 0.5) + float2(0.6, -time * 0.04 + 0.4)).rgb;
    //Tint the grime towards just being pure white, we don't want it to be overwhelming
    grime = lerp(grime, float3(1, 1, 1), 0.6);
    
    //Downwards shimmery dust that scrolls downwards. The sampling oscillates the positions vertically to make it look like the dust is waving n the wind a little
    float2 shimmeringDust = tex2D(rgbNoiseTex, coords * float2(0.2 * stretch, 0.2) + float2(sin(coords.y * 4 + time * 0.1) * 0.003, -time * 0.01)).rg;
    shimmeringDust.g = tex2D(rgbNoiseTex, coords * float2(0.2 * stretch, 0.2) + float2(sin(coords.y * 4 + time * 0.1) * 0.003, -time * 0.005)).g;
    
    //Only get the brightestmost of shimmering dust
    shimmeringDust = max(0, shimmeringDust - dustThresholds) / (float2(1, 1) - dustThresholds);
    float glowColor = shimmeringDust.r + shimmeringDust.g * 0.4;
    glowColor += 0.02 + input.Color.b * 0.05;
    
    //By default a blueish tint, becomes more yellow on pixels that overlap the shimmery dust flakes
    float3 tint = float3(0, 0, 0);

    if (input.Color.b > 0)
        tint = float3(0.10 + shimmeringDust.r * 0.3, 0.90 + shimmeringDust.r * 0.4, 1);
    else
        tint = float3(0.50 + shimmeringDust.r * 0.3, 0.50 + shimmeringDust.r * 0.4, 0.5);
    
    return float4(glowColor * tint, 0) * opacityMask;
}

technique Technique1
{
    pass CeilingDustColumnPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}