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

texture rgbNoise;
sampler2D rgbNoiseTex = sampler_state
{
    texture = <rgbNoise>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = wrap;
    AddressV = wrap;
};

float displaceStrenght;
float glowStrenght;

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
    float4 baseColor = tex2D(uImage0, coords);
    
    float2 rgbNoiseUv = frameUv * (uSourceRect.zw / uImageSize1) * 0.5;
    rgbNoiseUv = floor(rgbNoiseUv * uImageSize1) / uImageSize1;
    rgbNoiseUv += float2(0, uTime * 0.08);
    
    float3 rgbNoise = tex2D(rgbNoiseTex, rgbNoiseUv).rgb;
    
    //Pixel distortion
    float distortTreshold = 0.1;
    float2 distortScaling = uImageSize0 * 0.5;
    float displaceStr = displaceStrenght * uOpacity;
    
    //Distortion
    float2 displacedUvs = coords + step(float2(1 - distortTreshold, 1 - distortTreshold), rgbNoise.xy) / distortScaling * displaceStr;
    displacedUvs -= step(rgbNoise.xy, float2(distortTreshold, distortTreshold)) / distortScaling * displaceStr;
    float4 warpedColor = tex2D(uImage0, displacedUvs);
    
    float3 returnColor = lerp(baseColor, warpedColor, warpedColor.a);
    
    returnColor.g = max(returnColor.g, returnColor.b);
    
    //Tint blueish and then greenish towards the top
    returnColor = lerp(returnColor, screen(returnColor, uColor), 0.6);
    returnColor = lerp(returnColor, screen(returnColor, uSecondaryColor), frameUv.y);
    
    //Add glowing dots based on blue channel
    returnColor += step(0.99, rgbNoise.b) * uSecondaryColor;
    
    //Brighten dark colors towards the bottom
    float brightness = length(warpedColor.rgb) / 3;
    returnColor += invLerp(0.2, 0.1, brightness) * uColor * frameUv.y * glowStrenght;
    
    
    returnColor = lerp(baseColor.rgb, returnColor, min(uOpacity, 1));
    float opacity = max(baseColor.a, warpedColor.a) * sampleColor.a;
    //Subtle fullbright
    sampleColor.rgb = lerp(sampleColor.rgb, float3(1, 1, 1), 0.4 * uOpacity);
    return float4(returnColor * sampleColor.rgb * opacity, opacity);
}

technique Technique1
{
    pass ElectricDyePass
    {
        PixelShader = compile ps_3_0 Recolor();
    }
}