sampler uImage0 : register(s0);

float Scroll;
float2 Resolution;
float2 FrameSize;

float4 OutlineColor;
float4 DeepColor;
float4 LitColorTone1;
float4 LitColorTone2;
float GlowIntensity;
float4 GlowColor;

float Tone2Factor;
float ShadingCurveExponent;
int ColorDepth;

texture NoiseTexture;
sampler NoiseTextureSampler = sampler_state
{
    texture = <NoiseTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

#define PI 3.14159

float2 PartitionUV(float2 UV)
{
    float2 modUV = UV;
    modUV.x = (UV.x % (FrameSize.x / Resolution.x)) * (Resolution.x / FrameSize.x);
    modUV.y = (UV.y % (FrameSize.y / Resolution.y)) * (Resolution.y / FrameSize.y);
    
    return modUV;
}

float2 Distort(float2 UV)
{
    // Partition UV to match frame size
    float2 modUV = PartitionUV(UV);

    // Squish near top and bottom
    modUV.y = 1 - pow(2 * modUV.y - 1, 4);
    
    // Squish towards front
    modUV *= pow(modUV.x, 2);

    return modUV * 4;
}

float DistFromCenter(float2 UV)
{
    // Partition UV to match frame size
    float2 modUV = PartitionUV(UV);
    
    return sqrt(pow(modUV.x - 0.5f, 2) + pow(modUV.y - 0.5f, 2)) * sqrt(2);
}

float4 ColorFromLight(float light)
{
    float4 color = lerp(DeepColor, LitColorTone1, light);
    return lerp(color, LitColorTone2, pow(light, 1.2) * Tone2Factor);
}

float4 PixelShaderFunction(float4 tint : COLOR0, float2 coords : TEXCOORD) : COLOR
{
    // Colors from base
    float4 baseColor = tex2D(uImage0, coords);

    // Get noisemap color
    // Pixelated and distort coords for noise
    float2 modUV = Distort(coords);
    modUV = floor(coords * Resolution) / Resolution;
    
    float2 noiseL1Offset = float2(Scroll * 0.2f - 0.7f, 0);
    float2 noiseL2Offset = float2(Scroll - 0.3f, Scroll * 0.5f);
    
    float3 noise = (tex2D(NoiseTextureSampler, modUV * 0.5f + noiseL1Offset) + tex2D(NoiseTextureSampler, modUV + noiseL2Offset)) / 2;
    
    // Non-linear lighting curve looks better
    noise = pow(noise, ShadingCurveExponent);
    
    // Light from noise, mult by red channel. Set to blue channel if its greater
    float light = max(noise * baseColor.r, baseColor.b);

    // Extremely fake ambient occlusion
    light *= 0.6 + min(baseColor.r / 0.5, 1) * 0.4;
        
    // Quantize the color
    light = round(light * ColorDepth) / ColorDepth;
    
    // Get actual color from light
    float4 finalColor = ColorFromLight(light);
    finalColor *= tint;
    
    // Inner glow
    float glowLight = round(GlowIntensity * ColorDepth * pow(1 - DistFromCenter(modUV), 2)) / ColorDepth;
    finalColor = lerp(finalColor, GlowColor, glowLight);
    
    // Add ripple distortion if red channel is dominant
    if (baseColor.b <= 0.2)
        finalColor.rgb *= 1 - 0.5 * ceil(sin(noise.r * PI * 10)) * ceil(noise.r - 0.2);
        
    // Map outline color to green channel
    if (baseColor.g > 0)
    {
        // base outline is transparent
        float4 outlineColor = float4(0, 0, 0, 0);
        
        // lerp to glow color towards front
        float glowX = PartitionUV(modUV).x;
        float glowLerp = glowX > 0.5f ? 1 : pow(glowX * 2, 0.7f);
        outlineColor = lerp(outlineColor, GlowColor, glowLerp * GlowIntensity);
        
        finalColor = lerp(finalColor, outlineColor, baseColor.g);
    }
    // Set opacity
    else if (baseColor.a == 0)
        finalColor *= 0;
    else   
        finalColor.a = 1;
    
    return finalColor;
}

technique Technique1
{
    pass HorrorShaderPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}