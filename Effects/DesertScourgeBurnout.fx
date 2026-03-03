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
float enabled;

float segmentCount;
float noiseRange;
float timeLeftSkeleton;


//Hlsl's % operator applies a modulo but conserves the sign of the dividend, hence the need for a custom modulo
float mod(float a, float n)
{
    return a - floor(a / n) * n;
}
float LinearLight(float bottom, float top)
{
    return bottom + top * 2 - 1;
}

float invlerp(float from, float to, float value)
{
    return clamp((value - from) / (to - from), 0.0, 1.0);
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(samplerTex, uv);
    
    float2 frameUV = (uv - float2(sourceFrame.xy / texSize)) * float2(texSize.x / sourceFrame.z, texSize.y / sourceFrame.w);
    frameUV.x *= sourceFrame.w / sourceFrame.z;
    
    frameUV.x -= frameUV.x % (1 / (sourceFrame.z * 0.5));
    frameUV.y -= frameUV.y % (1 / (sourceFrame.w * 0.5));
    
    float upwardsGradient = lerp(topIndex, bottomIndex, frameUV.y) / segmentCount;
    //float correctedGradient = clamp((upwardsGradient - gradientStart) / (gradientEnd - gradientStart), 0, 1); //Squish our gradient to fit only into the range we need it.
    
    float progress = (1 + timeLeftSkeleton) - (1 + timeLeftSkeleton + noiseRange) * (1 - generalProgress);
    progress += pow((1 - upwardsGradient) * generalProgress, 3);
    
    //Multiply
    float multiplyValue = tex2D(noiseTex, float2(frameUV.x * multiplyNoiseScale, (frameUV.y + topIndex) * multiplyNoiseScale * 0.5));
    progress += (multiplyValue * noiseRange);
    
    //if disabled, progress gets turned to zero
    progress *= enabled;
    
    //if progress goes above 1 that means its been burnt fully and should display invisible
    float cropMask = progress < 1;
    
    //Fade goes from 0.3 to 1, so we remove the initial part of it
    progress -= 0.3;
    progress /= 0.7;
    progress = clamp(progress, 0, 1);
    
    //Gradient as it gets cropped so its not a hard edge
    cropMask *= invlerp(1, 0.95, progress);
    
    color.r += progress * 2;
    color.g += pow(progress, 1.2) * 1.2;
    color.b += progress * 0.5;
    
    color.xyz *= lightColor;
    
    //color.a *= (1 - correctedGradient);
    return color * color.a * cropMask;
}

technique Technique1
{
    pass DesertScourgeBurnoutPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}