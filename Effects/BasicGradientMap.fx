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

float effectOpacity;
float brightnesses[10];
float3 colors[10];
int segments;
float4 lightColor;
float3 lastColor;
bool useLuminance;

float GetLuminance(float4 color)
{
    if (!useLuminance)
        return (color.r + color.g + color.b) / 3;
    return dot(color.rgb, float3(0.299, 0.587, 0.114));
}

float3 GradientMap(float brightness, int gradientSegments)
{
    for (int i = 1; i < gradientSegments; i++)
    {
        float currentBrightness = brightnesses[i];
        
        
        if (currentBrightness >= brightness)
        {
            /*
            if (currentBrightness == brightness)
            {
                return colors[i];
            }*/
            
            float previousBrightness = brightnesses[i - 1];
            float segmentLenght = currentBrightness - previousBrightness;
            float segmentProgress = (brightness - previousBrightness) / segmentLenght;
            
            return colors[i - 1] * (1 - segmentProgress) + colors[i] * segmentProgress;
            
            
            return lerp(colors[i - 1], colors[i], segmentProgress);
        }
    }
    
    //Somehow we cant just use gradientSegments - 1, so its something separate i guess
    return lastColor;
}


float4 Main(float2 coords : TEXCOORD0) : COLOR
{
    float4 textureColor = tex2D(samplerTex, coords);
    
    if (effectOpacity <= 0)
        return textureColor * lightColor;
    
    //apply gradient map
    float4 finalColor = textureColor;
    float brightness = GetLuminance(textureColor);
    float4 gradientMapColor = float4(GradientMap(brightness, segments), textureColor.a);
    finalColor = lerp(finalColor, gradientMapColor, effectOpacity);
    
    finalColor.rgb *= textureColor.a;
    
    return finalColor * lightColor;
}

technique Technique1
{
    pass BasicGradientMapPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}