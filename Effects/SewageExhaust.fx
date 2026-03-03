matrix uWorldViewProjection;
float time;

float2 resolution;
float opacityMultiplier;
float2 fadeNoiseUvOffset;
float fadeNoiseStretch;
float ditherIntensity;

texture waterTexture;
sampler2D waterTex = sampler_state
{
    texture = <waterTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = clamp;
    AddressV = clamp;
};

texture mainNoiseTexture;
sampler2D mainNoiseTex = sampler_state
{
    texture = <mainNoiseTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = wrap;
    AddressV = wrap;
};


texture displaceTexture;
sampler2D displaceTex = sampler_state
{
    texture = <displaceTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture fadeTexture;
sampler2D fadeTex = sampler_state
{
    texture = <fadeTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
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


float invlerp(float from, float to, float value)
{
    return clamp((value - from) / (to - from), 0.0, 1.0);
}

float4 alphaBlend(float4 bottomLayer, float4 topLayer)
{
    float4 returnColor = bottomLayer * (1 - topLayer.a) + topLayer * topLayer.a;
    returnColor.a = min(1, max(bottomLayer.a, topLayer.a));
    return returnColor;
}

// This grabs some twirly noise centered around the bottom of the waterfall
float getNoiseValue(float2 uv, float fadeValue)
{
    float2 noiseOrigin = fadeNoiseUvOffset - uv;
    float noise = fadeValue;
    
    noise *= 0.5 + 0.5 * tex2D(fadeTex, noiseOrigin * float2(0.9, 0.5 * fadeNoiseStretch) + float2(-time * 0.23, time * 0.49)).x;
    noise *= tex2D(fadeTex, noiseOrigin * float2(0.9, 1.5 * fadeNoiseStretch) + float2(time * 0.13, time * 0.83)).x;;
    
    noise = pow(noise, 0.5);
    
    float upValue = invlerp(0.2, 1.1, noiseOrigin.y);
    noise = lerp(noise, 1, upValue);
    
    //make the very end of it SUPER extra faded
    noise *= pow(invlerp(0, 0.2, fadeValue), 0.3);
    return clamp(noise, 0, 1);
}


float ditherPattern(float2 coords, float opacity)
{
    //Dither is done in 2x2
    float2 resScaled = floor(coords * resolution * float2(1, 0.5));
    
    if (opacity > 0.55)
        return 1;
    if (opacity > 0.25)
        return 0.5 + 0.5 * (step(1, (resScaled.y + resScaled.x) % 2));

    return 0.5 + 0.5 * (step(1, resScaled.y % 2) * step(1, resScaled.x % 2)); // Sparse dither
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates.xy;
    uv = floor(uv * resolution + 0.5) / resolution;
    float2 preDistortedUvs = uv;
    
    uv += (tex2D(displaceTex, uv * float2(0.6, 0.7) + float2(0, -time * 0.8)).xy - float2(0.5, 0.5)) * .2;
    //uv.x += sin(uv.y * 5 - time * 6) * 0.02;
    
    float noiseX = floor(uv.x * 2) * 0.5 + frac(uv.x * 2) * 0.57;
    float noise = tex2D(mainNoiseTex, float2(uv.y * 0.35 - time * 0.14, noiseX)).x + tex2D(mainNoiseTex, float2(uv.y * 0.6 - time * 0.13, noiseX)).x;
    float thickness = abs(uv.x * resolution.x * 2 - resolution.x);
    thickness += noise * 4;
    
    //Trim at a max dist
    float waterUvY = max(0, 1 - thickness / 22);
    float4 color = tex2D(waterTex, float2(0, waterUvY));
    
    color *= step(thickness, 24);
    float opacity = 1 - floor(waterUvY * 4) / 8 * 0.2 * uv.y;
    opacity *= 1 - step(0.15, waterUvY) * 0.05;
    
    float fadeNoise = getNoiseValue(preDistortedUvs, color.a);
    fadeNoise *= (1 - ditherIntensity) + ditherIntensity * ditherPattern(lerp(preDistortedUvs, uv, 0.7), fadeNoise);
    opacity *= fadeNoise;
    //opacity *= 1 + invlerp(0.5, 0, frac(fadeNoise * 2.1 + uv.y * 1)) * 0.4;
    opacity = clamp(opacity, 0, 1);
    
    return color * opacity * input.Color * opacityMultiplier;
}

technique Technique1
{
    pass SewageExhaustPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}