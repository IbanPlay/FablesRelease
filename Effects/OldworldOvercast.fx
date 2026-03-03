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

float lightenGradientMapStrenght;
float lightenGradientMapBrightnesses[6];
float3 lightenGradientMapColors[6];

float colorDodgeStrenght;

float2 desaturationMaskStretch;
float desaturationMaskBlend;
float edgeDesaturationStrenght;

float2 colorDodgeMaskStretch;
float colorDodgeMaskBlend;
float colorDodgeGradientStrenght;
float colorDodgeGradientBrightnesses[6];
float3 colorDodgeGradientColors[6];

float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

float GetBrightness(float4 color)
{
    return (color.r + color.g + color.b) / 3;
}

float3 Lighten(float3 baseColor, float3 secondColor)
{
    return float3(max(baseColor.x, secondColor.x), max(baseColor.y, secondColor.y), max(baseColor.z, secondColor.z));
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

float3 ColorDodge(float3 bottomLayer, float3 topLayer)
{
    float3 white = float3(1, 1, 1);
    
    //Invert top layer
    topLayer = white - topLayer;
    
    //Make sure to not divide by zero (woopsie!)
    float3 result = white;
    
    if (topLayer.x != 0)
        result.x = bottomLayer.x / topLayer.x;
    
    if (topLayer.y != 0)
        result.y = bottomLayer.y / topLayer.y;
    
    if (topLayer.z != 0)
        result.z = bottomLayer.z / topLayer.z;
    
    return result;
}

float GetCircularMask(float2 coords, float2 stretch, float blend)
{
    float2 center = float2(0, 0);
    float2 fromCenter = (coords - center) * 2;
    
    //Squish the coordinates
    fromCenter.x *= stretch.x;
    fromCenter.y *= stretch.y;
    
    float distanceToCenter = length(fromCenter);
    
    //Anything outside the mask
    if (distanceToCenter > 1)
        return 1;
    //If fully inside the mask
    if (distanceToCenter < 1 - blend)
        return 0;
    
    return (distanceToCenter - (1 - blend)) / blend;
}

//This shader is a one to one recreation of a visual edit. This recreates the csp layers
float4 Main(float4 unused : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 textureColor = tex2D(uImage0, coords);
    float luminosity = GetBrightness(textureColor);
    
    //Saturation -17%
    textureColor.rbg = lerp(textureColor.rbg, float3(luminosity, luminosity, luminosity), uOpacity * 0.17);
    //Brightness -12%
    textureColor.rbg *= 1 - uOpacity * 0.12;
    
    //53% gradient map in lighten
    luminosity = GetBrightness(textureColor);
    float3 lightenedColor = Lighten(textureColor.xyz, GradientMap(luminosity, 6, lightenGradientMapBrightnesses, lightenGradientMapColors));
    textureColor.xyz = lerp(textureColor.xyz, lightenedColor, uOpacity * lightenGradientMapStrenght);
    
    //38% opacity color dodge, of (71, 125, 205)
    float3 colDodge = float3(71, 125, 205) / 255;
    float3 simpleColorDodge = ColorDodge(textureColor.xyz, colDodge);
    textureColor.xyz = lerp(textureColor.xyz, simpleColorDodge, uOpacity * colorDodgeStrenght);
    
    /*
    
    
    //76% desaturation on the edges (and around the sun)
    luminosity = GetBrightness(textureColor);
    float desaturationMask = GetCircularMask(coords, desaturationMaskStretch, desaturationMaskBlend);
    if (desaturationMask > 0)
        textureColor.xyz = lerp(textureColor.rbg, float3(luminosity, luminosity, luminosity), uOpacity * desaturationMask * edgeDesaturationStrenght);
    
    //18% gradient map in color dodge
    float colorDodgeMask = 1 - GetCircularMask(coords, colorDodgeMaskStretch, colorDodgeMaskBlend);
    if (colorDodgeMask > 0)
    {
        luminosity = GetBrightness(textureColor);
        float colorDodge = GradientMap(luminosity, 6, colorDodgeGradientBrightnesses, colorDodgeGradientColors);
        textureColor.xyz = lerp(textureColor.xyz, ColorDodge(textureColor.xyz, colorDodge), uOpacity * colorDodgeGradientStrenght * colorDodgeMask);
    }
*/    

    return textureColor;
}

technique Technique1
{
    pass OldworldOvercastPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}