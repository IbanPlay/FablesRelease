texture sampleTexture;
sampler2D samplerTex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = wrap;
    AddressV = wrap;
};

float brightnessShift;
float brightnesses[10];
float3 colors[10];
int segments;
float4 lightColor;
float gradientScaleMultiplier;
float brightnessStep;

texture noiseTexture;
sampler2D noiseTex = sampler_state
{
    texture = <noiseTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};
float2 noiseScale;
float noiseScroll;
float3 noiseColor;
float noiseTreshold;
float noisePower;

float3 depthColor;
float3 globalMultiplyColor;

/*
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
            
            float previousBrightness = -lastBrightnessLenght;
            float3 previousColor = segmentColors[gradientSegments - 1];
            
            if (i > 0)
            {
                previousBrightness = segmentBrightness[i - 1];
                previousColor = segmentColors[i - 1];
            }
            
            float segmentLenght = currentBrightness - previousBrightness;
            float segmentProgress = (brightness - previousBrightness) / segmentLenght;
            
            return lerp(previousColor, segmentColors[i], segmentProgress);
        }
    
        return segmentColors[gradientSegments - 1];
    }
    
    float firstBrightness = segmentBrightness[0];
    float segmentLenght = lastBrightnessLenght + firstBrightness;
    float segmentProgress = (brightness - segmentBrightness[gradientSegments - 1]) / segmentLenght;
    return lerp(segmentColors[gradientSegments - 1], segmentColors[0], segmentProgress);
}
*/

//Hlsl's % operator applies a modulo but conserves the sign of the dividend, hence the need for a custom modulo
float mod(float a, float n)
{
    return a - floor(a / n) * n;
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


float GetNoiseStrenght(float2 baseCoords)
{
    float noiseValue = tex2D(noiseTex, baseCoords * noiseScale).r;
    if (noiseValue < noiseTreshold)
        return 0;
    return pow(noiseValue, noisePower);
}


float4 Main(float2 coords : TEXCOORD0) : COLOR
{
    float4 textureColor = tex2D(samplerTex, coords);
    
    //apply gradient map
    float brightness = mod(textureColor.r * gradientScaleMultiplier + brightnessShift, 1);
    brightness -= brightness % brightnessStep;
    
    float4 finalColor = float4(GradientMap(brightness, 7, brightnesses, colors), textureColor.a);
    
    //Add spores that slide throught the effect
    float noiseValue = GetNoiseStrenght(float2(textureColor.g, textureColor.r + noiseScroll));
    finalColor += float4(noiseValue * noiseColor, 1);
    
    //Apply a global color multiplication, used to make the fissure glow
    finalColor.rgb *= globalMultiplyColor;
    
    //Add depth based on the blue channel
    finalColor.rgb *= lerp(float3(1, 1, 1), depthColor, textureColor.b);
    
    //if the alpha isnt 100%, make it additive
    finalColor.a *= floor(textureColor.a);
    if (textureColor.a < 1)
        finalColor.rgb *= max(min(globalMultiplyColor.b, 2) - 1, 0) * 0.3;
    
    return finalColor * lightColor * ceil(textureColor.a);
}

technique Technique1
{
    pass CrabulonFissurePass
    {
        PixelShader = compile ps_3_0 Main();
    }
}