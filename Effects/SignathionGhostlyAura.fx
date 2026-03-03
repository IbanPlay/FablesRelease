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

texture noiseTex;
sampler2D noiseSampler = sampler_state
{
    texture = <noiseTex>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture vanishingNoise;
sampler2D vanishSampler = sampler_state
{
    texture = <vanishingNoise>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float vanishCompletion;

float completion;
float time;
float4 sourceFrame;
float2 texSize;
float4 lightColor;

float noiseFadeStrenght;
float upwardsGradientOpacity;
float upwardsGradientStrenght;

float4 glowColor;
float outlineOpacity;
float opacity;
float maxEffectOpacity;

//Hlsl's % operator applies a modulo but conserves the sign of the dividend, hence the need for a custom modulo
float mod(float a, float n)
{
    return a - floor(a / n) * n;
}

float2 resize(float2 coords, float2 offset)
{
    return ((coords * texSize) + offset) / texSize;
}

bool AmIOnTheEdge(float outlineThickness, float2 uv)
{
    float unitX = outlineThickness / texSize.x;
    float unitY = outlineThickness / texSize.y;
    
    if (uv.x < unitX || uv.x >= 1 - unitX || uv.x < unitY || uv.y >= 1 - unitY)
        return true;
    
    return false;
}


float4 GhostlyFeetVisuals(float2 uv, float2 frameUV, float4 color)
{
    float upwardsGradient = frameUV.y;
    
    float noise = tex2D(noiseSampler, frameUV + float2(time * 3, time * 0.4)).x;
    noise *= 1.0 - tex2D(noiseSampler, frameUV * 1.6 + float2(-time, time)).x;
    
    float addedColorOpacity = noise * pow(upwardsGradient, noiseFadeStrenght);
    addedColorOpacity += upwardsGradientOpacity * pow(upwardsGradient, upwardsGradientStrenght);
    
    if (addedColorOpacity > maxEffectOpacity)
        addedColorOpacity = maxEffectOpacity;
    
    color += (glowColor * glowColor.a) * addedColorOpacity * color.a;
    
    if (outlineOpacity > 0)
    {
        bool outline = AmIOnTheEdge(2, uv);
        
        if (!outline)
        {
            bool4 edges;
            edges.x = 0 == tex2D(samplerTex, resize(uv, float2(2.1, 0))).a;
            edges.y = 0 == tex2D(samplerTex, resize(uv, float2(0, 2.1))).a;
            edges.z = 0 == tex2D(samplerTex, resize(uv, float2(-2.1, 0))).a;
            edges.w = 0 == tex2D(samplerTex, resize(uv, float2(0, -2.1))).a;
            
            outline = edges.x || edges.y || edges.z || edges.w;
        }
        
        if (outline)
            color *= 1 + upwardsGradient * outlineOpacity;
    }
    
    return color;
}


float4 VanishingEffect(float2 uv, float2 frameUV, float4 color) //This effect is an adaptation of slrs ceiros MoltenForm shader. BAsically, its a really cool treshold map thing. Was very hard to understand though
{
    float upwardsGradient = 1 - frameUV.y;
    
    float originalAlpha = color.a;
    color.a = 0;
    
    //Get the current completion of the shader
    //Darken it by 50% as we go up the image (aka as we go up theres a smooth gradient which gets up to 50% darker
    //Additionally, darken it by an additional 50% with the noise map.
    //These darkenings mean that the value will only go down. If the completion is zero, value will be negative for all the image, as the gradient applies there
    //If the completion is at 0.5, the gradient map will never put it into the negatives, but the noise map can still do it
    //If the completion is at 1, both the gradient map and the noise map combined wouldnt be able to put it into the negatives
    float value = vanishCompletion - 0.5 * (upwardsGradient + 1 - tex2D(vanishSampler, (1 - frameUV) - float2(0, time * 2)).x);
    value = mod(value, 1);
    
    value = mod(value, 1); //Apply a modulo. If the value got into the negative, this will loop it back to being brighter than the completion
    color.a += value > vanishCompletion ? 0.0 : 1.0; //So now, we can use this as a trehsold. If the value got put into the negatives from the gradient map or the noise map, we ignore it
    
    value *= 2;
    float doubleCompletion = vanishCompletion * 2;
    
    color.b += max(1.4 - abs(doubleCompletion - upwardsGradient), 0.0) * (1.0 - value);
    color.g += max(1.4 - abs(doubleCompletion - upwardsGradient), 0.0) * (1.0 - value);
    //color.r += (1.0 - (doubleCompletion - upwardsGradient)) * 0.5 * max(1.2 - abs(doubleCompletion - upwardsGradient), 0.0) * (1.0 - value);
    
    return color * originalAlpha;
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float2 frameUV = (uv - float2(sourceFrame.xy / texSize)) * float2(texSize.x / sourceFrame.z, texSize.y / sourceFrame.w);
    
    float4 color = tex2D(samplerTex, uv);
    float upwardsGradient = frameUV.y;
   
    color = GhostlyFeetVisuals(uv, frameUV, color);
    
    if (vanishCompletion < 1.5)
        color = VanishingEffect(uv, frameUV, color);
    
    color.xyz *= lightColor.xyz * opacity * color.a;
    return color;
}

technique Technique1
{
    pass SignathionGhostlyAuraPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}