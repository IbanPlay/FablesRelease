texture sampleTexture;
sampler2D samplerTex = sampler_state { texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

texture sampleTexture2;
sampler2D samplerTex2 = sampler_state { texture = <sampleTexture2>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float completion;
float4 sourceFrame;
float2 texSize;
float4 lightColor;
bool sidewaysGradient;

//Hlsl's % operator applies a modulo but conserves the sign of the dividend, hence the need for a custom modulo
float mod(float a, float n)
{
    return a - floor(a / n) * n;
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0 //This effect is an adaptation of slrs ceiros MoltenForm shader. BAsically, its a really cool treshold map thing. Was very hard to understand though
{
    float2 frameUV = (uv - float2(sourceFrame.xy / texSize)) * float2(texSize.x / sourceFrame.z, texSize.y / sourceFrame.w);
    
    float4 color = float4(tex2D(samplerTex, uv).xyz, 0);
    
    float upwardsGradient = 1 - frameUV.y;
    if (sidewaysGradient)
        upwardsGradient = 1 - frameUV.x;
    
    //Get the current completion of the shader
    //Darken it by 50% as we go up the image (aka as we go up theres a smooth gradient which gets up to 50% darker
    //Additionally, darken it by an additional 50% with the noise map.
    //These darkenings mean that the value will only go down. If the completion is zero, value will be negative for all the image, as the gradient applies there
    //If the completion is at 0.5, the gradient map will never put it into the negatives, but the noise map can still do it
    //If the completion is at 1, both the gradient map and the noise map combined wouldnt be able to put it into the negatives
    float value = completion - 0.5 * (upwardsGradient + tex2D(samplerTex2, (1 - frameUV)).x);

    value = mod(value, 1); //Apply a modulo. If the value got into the negative, this will loop it back to being brighter than the completion
    color.a += value > completion ? 0.0 : 1.0; //So now, we can use this as a trehsold. If the value got put into the negatives from the gradient map or the noise map, we ignore it
    
    value *= 2;
    float doubleCompletion = completion * 2;
    
    color.b += max(1.2 - abs(doubleCompletion - frameUV.y), 0.0) * (1.0 - value);
    color.g += max(1.6 - abs(doubleCompletion - upwardsGradient), 0.0) * (1.0 - value);
    color.r += (1.0 - (doubleCompletion - upwardsGradient)) * 0.5 * max(1.2 - abs(doubleCompletion - upwardsGradient), 0.0) * (1.0 - value);
    
    color *= tex2D(samplerTex, uv).a;
    color.xyz *= lightColor.xyz;
    
    return color;
}

technique Technique1
{
    pass NautilusTridentApparificationPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}