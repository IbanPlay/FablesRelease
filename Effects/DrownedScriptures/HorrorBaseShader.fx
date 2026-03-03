sampler uImage0 : register(s0);

float4 OutlineColor;
float GlowIntensity;

texture GradingTexture;
sampler GradingTextureSampler = sampler_state
{
    texture = <GradingTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = wrap;
    AddressV = wrap;
};

float4 PixelShaderFunction(float4 tint : COLOR0, float2 coords : TEXCOORD) : COLOR
{
    // Colors from base
    float4 baseColor = tex2D(uImage0, coords);
    
    float4 finalColor = float4(0, 0, 0, 0);
    
    if (baseColor.r > 0)
    {
        // Samples from color grading. Selects color based on red channel
        float4 baseSampleColor = tex2D(GradingTextureSampler, float2(0, 1 - baseColor.r));
        baseSampleColor *= tint;

        // Get glow color through same method
        float4 glowSampleColor = tex2D(GradingTextureSampler, float2(0.5f, 1 - baseColor.r));

        // Determine final color based on glow intensity
        finalColor = lerp(baseSampleColor, glowSampleColor, GlowIntensity);
    }
    
    // Add outline color based on green channel
    finalColor = lerp(finalColor, OutlineColor, baseColor.g);
    
    if (baseColor.a == 0)
        finalColor *= 0;
    else
        finalColor.a = 1;

    return finalColor;
}

technique Technique1
{
    pass HorrorBaseShaderPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}