sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor; //Screenlayercolor
float uOpacity;
float uSaturation; //Used as "idOffset" from desertghost
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


float3 screen(float3 base, float3 scrn)
{
    return float3(1, 1, 1) - 2 * (float3(1, 1, 1) - base) * (float3(1, 1, 1) - scrn);
}

float invLerp(float from, float to, float v)
{
    return clamp((v - from) / (to - from), 0, 1);
}


float4 Recolor(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 frameUv = (coords * uImageSize0 - uSourceRect.xy) / uSourceRect.zw;
    float2 distortion = tex2D(uImage1, coords + float2(uTime * 0.07, -uTime * 0.23 + uSaturation * 0.2)).gr;
    
    distortion -= float2(0.5, 0.5); //Center the distortion
    distortion /= uSourceRect.zw; //Scale the distortion properly
    distortion /= float2(9, 7); //Scale the distortion properly
    
    float distortionStrenght = invLerp(0.5, 1, frameUv.y); //only from mid-body down, and not earlier
    float4 unwarpedColor = tex2D(uImage0, coords);
    
    //Get the final distorted coords
    float2 warpedCoords = coords + distortion * distortionStrenght * 4;
    warpedCoords.x += sin(uTime * 2.3 + uSaturation + frameUv.y * 2.4) * (2 / uImageSize0.x);
    float4 warpedColor = tex2D(uImage0, warpedCoords);
    
    float4 baseColor = lerp(unwarpedColor, warpedColor, warpedColor.a);
    
    
    float opacity = 1;
    
    //Fade the sprite from the bottom more or less in a sine pattern so that the feet are either totally obscured or not
    float fadeTreshold = 0.25 + 0.25 * sin(uTime * 0.8 + uSaturation * 0.5);
    opacity *= fadeTreshold + (1 - fadeTreshold) * (1 - frameUv.y);
    
    //Opacity should be powed between 1 and 2
    float opacityPowSine = 1.1 + 0.1 * sin(uTime * 0.95 + uSaturation * 0.6);
    opacity = pow(opacity, opacityPowSine);

    //Tint to cyan with screen
    float screenSine = 0.8 + 0.2 * sin(uTime * 0.6 + uSaturation * 0.6 + 0.6);
    float3 screenedColor = screen(baseColor.rgb, uColor) * sampleColor.a * baseColor.a;
    baseColor.rgb = lerp(baseColor.rgb, screenedColor, (0.5 - 0.5 * frameUv.y) * screenSine);
    sampleColor.rgb = lerp(sampleColor.rgb, screenedColor, 0.2);
    
    //tint the darker parts
    baseColor.rgb = lerp(baseColor.rgb, uColor, invLerp(0.3, 0, opacity)) * baseColor.a * sampleColor.a;
    
    //Outlines near the bottom
    float brightness = length(baseColor.rgb) / 3;
    float outlineGlow = pow(max(0, (0.3 - brightness) / 0.3), 1.4);
    float outlineOpacity = pow(frameUv.y, 0.5) * baseColor.a * sampleColor.a;
    baseColor.rgb += outlineGlow * uSecondaryColor * 1.1 * outlineOpacity;
    
    opacity = max(0.25, opacity);
    
    return baseColor * sampleColor * opacity;
}

technique Technique1
{
    pass DesertMirageDyePass
    {
        PixelShader = compile ps_3_0 Recolor();
    }
}