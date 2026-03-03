texture sampleTexture;
sampler2D samplerTex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float brightnessShift;
float brightnesses[10];
float3 colors[10];
int segments;
float4 lightColor;

float3 GradientMap(float brightness, int gradientSegments, float segmentBrightness[10], float3 segmentColors[10])
{
    float lastBrightnessLenght = 1 - segmentBrightness[gradientSegments - 1];
    
    for (int i = 0; i < gradientSegments; i++)
    {
        float currentBrightness = segmentBrightness[i];
        
        if (currentBrightness >= brightness)
        {
            //if we found a direct match, no usre for a lerp
            if (currentBrightness == brightness)
                return segmentColors[i];
            
            float previousBrightness = 0;
            if (i == 0)
                previousBrightness = -lastBrightnessLenght;
            else
                previousBrightness = segmentBrightness[i - 1];
            
            float segmentLenght = currentBrightness - previousBrightness;
            float segmentProgress = (brightness - previousBrightness) / segmentLenght;
            
            return lerp(segmentColors[i - 1], segmentColors[i], segmentProgress);
        }
    
        return segmentColors[gradientSegments - 1];
    }
    
    float firstBrightness = segmentBrightness[0];
    float segmentLenght = lastBrightnessLenght + firstBrightness;
    float segmentProgress = (brightness - segmentBrightness[gradientSegments - 1]) / segmentLenght;
    return lerp(segmentColors[gradientSegments - 1], segmentColors[0], segmentProgress);
}


float4 Main(float2 coords : TEXCOORD0) : COLOR
{
    float4 textureColor = tex2D(samplerTex, coords);
    
    if (textureColor.a == 0)
        return float4(0, 0, 0, 0);
    
    //apply gradient map
    float brightness = (textureColor.r + brightnessShift) % 1;
    float4 finalColor = float4(0, 0, brightness, textureColor.a);
    return finalColor * lightColor * textureColor.a;
}

technique Technique1
{
    pass CrabulonFissureTestPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}