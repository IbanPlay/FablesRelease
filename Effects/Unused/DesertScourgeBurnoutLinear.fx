texture sampleTexture;
sampler2D samplerTex = sampler_state { texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

texture noiseTexture;
sampler2D noiseTex = sampler_state { texture = <noiseTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float4 sourceFrame;
float2 texSize;
float4 lightColor;

float topIndex;
float bottomIndex;
float gradientStart;
float gradientEnd;

float multiplyNoiseScale;
float lightNoiseScale;
float lightOpacity;
float generalProgress = 0;

float segmentCount;


//Hlsl's % operator applies a modulo but conserves the sign of the dividend, hence the need for a custom modulo
float mod(float a, float n)
{
    return a - floor(a / n) * n;
}
float LinearLight(float bottom, float top)
{
    return bottom + top * 2 - 1;
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float2 frameUV = (uv - float2(sourceFrame.xy / texSize)) * float2(texSize.x / sourceFrame.z, texSize.y / sourceFrame.w);
    frameUV.x *= sourceFrame.w / sourceFrame.z;
    
    float4 color = tex2D(samplerTex, uv);
    if (color.a == 0)
        return color;
    
    float upwardsGradient = lerp(topIndex, bottomIndex, frameUV.y);
    float correctedGradient = clamp((upwardsGradient - gradientStart) / (gradientEnd - gradientStart), 0, 1); //Squish our gradient to fit only into the range we need it.
    
    correctedGradient += generalProgress;
    
    //Multiply
    float multiplyValue = tex2D(noiseTex, float2(frameUV.x * multiplyNoiseScale, (frameUV.y + topIndex) * multiplyNoiseScale));
    float multipliedGradient = lerp(correctedGradient, correctedGradient * multiplyValue, pow(1 - correctedGradient, 1.4));
    
    //Linear light
    if (correctedGradient > 0)
    {
        float lightValue = tex2D(noiseTex, float2(frameUV.x * lightNoiseScale, (frameUV.y + topIndex) * lightNoiseScale));
        
        lightValue = lerp(lightValue, 0, pow(1 - correctedGradient, 1));
        lightValue *= 2 * correctedGradient + 1;
        multipliedGradient += lightValue * 1.2 * lightOpacity;
    }
    
    
    float4 empty = float4(0, 0, 0, 0);
    if (multipliedGradient <= 0)
    {
        color.xyz *= lightColor;
        return color;
    }
    
    else if (multipliedGradient >= 1)
        return empty;
    
    
    color.r += multipliedGradient * 2;
    color.g += pow(multipliedGradient, 1.2) * 1.2;
    color.b += multipliedGradient * 0.5;
    
    color.xyz *= lightColor;
    
    if (multipliedGradient > 0.95)
        return lerp(color, empty, (multipliedGradient - 0.95) / 0.05);
    
    //color.a *= (1 - correctedGradient);
    return color;
}

technique Technique1
{
    pass DesertScourgeBurnoutPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}