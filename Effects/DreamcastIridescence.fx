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

float lightenStrenght;
float lightenBrightnesses[10];
float3 lightenColors[10];

float iridescenceStrenght;
float iridescenceBrightnesses[10];
float3 iridescenceColors[10];

float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

float GetBrightness(float4 color)
{
    return (color.r + color.g + color.b) / 2;
}

float3 GradientMap(float brightness, int gradientSegments, float segmentBrightness[10], float3 segmentColors[10])
{
    
    for (int i = 1; i < gradientSegments; i++)
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
    
    return segmentColors[gradientSegments - 1];
}

float4 Lighten(float4 baseColor, float4 secondColor)
{
    return (max(baseColor.x, secondColor.x), max(baseColor.y, secondColor.y), max(baseColor.z, secondColor.z));
}

//This shader is a one to one recreation of a visual edit. This recreates the csp layers
float4 Main(float4 unused : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 textureColor = tex2D(uImage0, coords);
    
    //Lighten gradient map
    float brightness = GetBrightness(textureColor);
    textureColor = lerp(textureColor, Lighten(textureColor, float4(GradientMap(brightness, 6, lightenBrightnesses, lightenColors), 1)), uOpacity * lightenStrenght);
    
    //Lighten gradient map
    brightness = GetBrightness(textureColor);
    textureColor = lerp(textureColor, Lighten(textureColor, float4(GradientMap(brightness, 6, iridescenceBrightnesses, iridescenceColors), 1)), uOpacity * iridescenceStrenght);
    
    return textureColor;
}

technique Technique1
{
    pass DreamcastIridescencePass
    {
        PixelShader = compile ps_3_0 Main();
    }
}