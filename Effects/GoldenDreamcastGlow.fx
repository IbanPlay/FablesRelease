sampler uImage0 : register(s0); // The contents of the screen

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

float gradientMapStrenght;
float gradientMapBrightnesses[6];
float3 gradientMapColors[6];

float linearLightStrenght;
float linearLightBrightnesses[6];
float3 linearLightColors[6];

float brightnessOffset;

float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

float GetBrightness(float4 color)
{
    return (color.r + color.g + color.b) / (3 + brightnessOffset);
}

float3 GradientMap(float brightness, int gradientSegments, float segmentBrightness[6], float3 segmentColors[6])
{
    
    for (int i = 1; i < 6; i++)
    {
        float currentBrightness = segmentBrightness[i];
        
        if (currentBrightness >= brightness)
        {
            if (currentBrightness == brightness)
            {
                return segmentColors[i];
            }
            
            float previousBrightness = segmentBrightness[i - 1];
            float segmentLenght = currentBrightness - previousBrightness;
            float segmentProgress = (brightness - previousBrightness) / segmentLenght;
            
            return lerp(segmentColors[i - 1], segmentColors[i], segmentProgress);
        }
    }
    
    return float3(1, 1, 1);
}

//This shader is a one to one recreation of a visual edit. This recreates the csp layers
float4 Main(float4 unused : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 textureColor = tex2D(uImage0, coords);
    //Brighten +3
    textureColor.rbg += uOpacity * (3 / 255);
    
    //Direct gradient map
    float brightness = GetBrightness(textureColor);
    textureColor = lerp(textureColor, float4(GradientMap(brightness, 6, gradientMapBrightnesses, gradientMapColors), 1), uOpacity * gradientMapStrenght) ;
    
    //10% gradient map in linear light
    brightness = GetBrightness(textureColor);
    float4 linearLightedColor = textureColor + 2 * float4(GradientMap(brightness, 6, linearLightBrightnesses, linearLightColors), 0) - float4(1, 1, 1, 0);
    textureColor = lerp(textureColor, linearLightedColor, linearLightStrenght * uOpacity);
    
    return textureColor;
}

technique Technique1
{
    pass GoldenDreamcastGlowPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}