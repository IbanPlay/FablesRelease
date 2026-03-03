sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;

float4 uShaderSpecificData;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;


float GetNoiseStrenght(float2 baseCoords)
{
    float noiseValue = tex2D(uImage1, baseCoords * 1.2).r;
    if (noiseValue < 0.6)
        return 0;
    return pow(noiseValue, 6);
}

float4 Recolor(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float frameY = (coords.y * uImageSize0.y - uSourceRect.y) / uSourceRect.w;
    float frameX = (coords.x * uImageSize0.x - uSourceRect.x) / uSourceRect.z;
    
    float4 color = tex2D(uImage0, coords);
    float originalAlpha = color.a * sampleColor.a;
    
    float brightness = length(color.rgb) / 3;
    float outlineGlow = pow(max(0, (0.3 - brightness) / 0.3), 1.4) * uSaturation;
    float underGlow = pow(frameY, 2.5);
    
    //Darken base
    color.r *= 0;
    color.gb *= 0.15 + 0.2 * uSaturation;
    
    //Lighten outlines
    float2 noiseCoords = (coords * uImageSize0 - uSourceRect.xy) / uImageSize1;
    float outlineIntensity = 1.66 + 0.7 * pow(tex2D(uImage1, noiseCoords + float2(sin(uTime * 0.6) * 0.1, uTime * 0.07)).g * 1.3, 1.4);
    
    color.rgb += uColor * outlineIntensity * outlineGlow;
    
    //Add a blue underglow
    color.g += underGlow * 0.4 * uSaturation;
    color.b += underGlow * 1.3 * uSaturation;
    
    //Add spores that slide throught the effect
    float noiseValue = GetNoiseStrenght(noiseCoords + float2(0.3, uTime * 0.3)) + GetNoiseStrenght(noiseCoords + float2(0.7 + uTime * 0.025, uTime * 0.25 - 0.5));
    color.rg += noiseValue * 2.5;
    color.b += noiseValue * 8;
    
    //Make the spores float up
    if (originalAlpha == 0 && noiseValue > 0.1)
        originalAlpha += noiseValue * min(1, frameY * 2);
    
    color = lerp(color, float4(1, 1, 1, 1), uOpacity);
    
    return color * originalAlpha;
}

technique Technique1
{
    pass LuminousSporeDyePass
    {
        PixelShader = compile ps_2_0 Recolor();
    }
}