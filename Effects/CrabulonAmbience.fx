sampler uImage0 : register(s0); // The contents of the screen
sampler gradientMap : register(s1);
sampler distortionTex : register(s2);
sampler psychedelicTex : register(s3);

float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float4 uShaderSpecificData;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

//Constants
float brightnessTreshold = 0.2409972;
float satTreshold = 0.30747926;
float blueTreshold = 0.91689754;

//uniform vec2 screenResolution = vec2(1920, 1008);

//Returns the saturation of any given color
float getSaturation(float3 color)
{
    float minValue = min(color.r, min(color.g, color.b));
    float maxValue = max(color.r, max(color.g, color.b));
    
    float luminosity = maxValue - minValue;
    float stepValue = step(0.5, luminosity);
    
    return (maxValue - minValue) / ((maxValue + minValue) * (1 - stepValue) + (2 - maxValue - minValue) * stepValue) * ceil(maxValue);
}

//Gets a black and white mask from a color, depending on if it meets 3 separate tresholds (brightness, saturation, blueness)
float getMask(float3 color)
{
    float brightness = length(color) / 3;
    float blueness = dot(normalize(color), normalize(uColor.xyz));
    float saturation = getSaturation(color);
    
    return step(brightnessTreshold, brightness) *
    step(satTreshold, saturation) *
    step(blueTreshold, blueness);
}

//Returns a mask that combines the mask of the pixel & the adjacent ones, for a blur-looking effect
float2 getMaskWithBlur(float3 baseColor, float2 uv)
{
    float baseMask = getMask(baseColor);
    
    float2 unit = float2(2., 2.) / uScreenResolution;
    
    float bloomMask = min(getMask(tex2D(uImage0, uv + float2(unit.x, 0)).xyz) +
                          getMask(tex2D(uImage0, uv - float2(unit.x, 0)).xyz) +
                          getMask(tex2D(uImage0, uv + float2(0, unit.y)).xyz) +
                          getMask(tex2D(uImage0, uv - float2(0, unit.y)).xyz),
    1) * step(baseMask, 0.5);
    return float2(baseMask, bloomMask);
}

float4 Main(float4 unused : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = coords;
    float distanceToCenter = length(float2(0.5, 0.5) - uv) * 1.4142;
    
    float darknessMultiplier = uProgress; //Used to avoid having the shader get too ugly when in broad daylight on the surface
    float drugIntensity = uIntensity; //Controls the strenght of the extra trippy psychedelic effects when spored
    float fadeInOut = uOpacity; //Controls the overall fade of the effect
    
    //Distort the edges of the screen (based on "drug intensity")
    float distortionStrenght = pow(smoothstep(0.4, 1, distanceToCenter), 2);
    float4 distortionNoise = tex2D(distortionTex, uv * 0.4 + float2(uTime * 0.2, uTime * 0.2)) * tex2D(distortionTex, uv * 0.3 + float2(-uTime * 0.1, -uTime * 0.1));
    uv += (float2(0.5, 0.5) - distortionNoise.xy) * distortionStrenght * 0.056 * drugIntensity;
    
    float4 baseColor = tex2D(uImage0, uv);
    
    
    //Tint the image, by making it lerp to a gradient map
    float brightness = length(baseColor.xyz) / 3;
    float tintStrenght = 0.4 * (fadeInOut * 0.4 + drugIntensity * 0.6);
    baseColor = lerp(baseColor, tex2D(gradientMap, float2(0, pow(1 - brightness, 3))), tintStrenght);
    
    
    //Get a mask of all blue-tinted areas on screen and add a psychedelic rainbow tint to them
    float2 blueMask = getMaskWithBlur(baseColor.xyz, uv);
    
    //tint the edges of the screen (when not drugged)
    baseColor.xyz = lerp(baseColor.xyz, uColor, min(1, pow(distanceToCenter, 6)) * 0.06 * (1 - drugIntensity) * fadeInOut);
    
    //Apply an anti-vignette effect that adds exposure to the edges of the screen, to give it a dreamy feel
    float vignette = pow(distanceToCenter, 5) * 10 * drugIntensity;
    baseColor *= 1 + vignette * (0.2 + 0.8 * darknessMultiplier);
    
    
    //Add rainbow scrolling noise in all the blue areas
    float4 scrollingNoise = tex2D(psychedelicTex, uv * float2(1.3, 4.3) + float2(uTime * 0.3, uTime * 0.3)) *
                            tex2D(psychedelicTex, uv * float2(1.3, 4.3) + float2(uTime, uTime) * -0.2) * 0.9;
    float psychStrenght = (0.6 * drugIntensity + 0.2) * darknessMultiplier * fadeInOut; //Intensity is calculated from both the drug intensity and the regular intensity
    float4 output = baseColor + (blueMask.x * 1 + blueMask.y * .5) * scrollingNoise * psychStrenght;
    
    //tint the bottom of the screen with the color
    output.xyz += uColor.xyz * (0.2 + 0.2 * drugIntensity) * smoothstep(0.5, 1., uv.y) * fadeInOut;
    
    output.a = 1;
    return output;
}

technique Technique1
{
    pass CrabulonAmbiencePass
    {
        PixelShader = compile ps_3_0 Main();
    }
}