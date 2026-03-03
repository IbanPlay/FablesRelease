sampler uImage0 : register(s0);

texture noise;
sampler2D noiseTexture = sampler_state
{
    texture = <noise>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture outline;
sampler2D outlineTexture = sampler_state
{
    texture = <outline>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = clamp;
    AddressV = clamp;
};

float4 sourceRect;
float2 resolution;
float time;
float fadePercent;
float idOffset;

float3 screenLayerColor;

float ditherPattern(float2 coords, float opacity)
{
    float2 resScaled = floor(coords * resolution * 0.5);
    
    if (opacity > 0.55)
        return 1;
    if (opacity > 0.5)
     //   return  (1 - step(1, resScaled.y  % 2  ) * step(1, resScaled.x % 2  ));
    if (opacity > 0.25)
        return 0.5 + 0.5 * (step(1, (resScaled.y + resScaled.x) % 2));

    return 0.5 + 0.5 * (step(1, resScaled.y % 2) * step(1, resScaled.x % 2)); // Sparse dither

}

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
    float2 frameUv = (coords * resolution - sourceRect.xy) / sourceRect.zw;
    float2 distortion = tex2D(noiseTexture, coords + float2(time * 0.07, -time * 0.23 + idOffset * 0.2)).gr ;
    
    distortion -= float2(0.5, 0.5); //Center the distortion
    distortion /= sourceRect.zw; //Scale the distortion properly
    
    float distortionStrenght = invLerp(0.5, 1, frameUv.y); //only from body down, and not earlier
    float extraDistortionStrenght = max(0, sin(time * 0.5));
    
    //Get the final distorted coords
    float2 warpedCoords = coords + distortion * distortionStrenght * 4 + distortion * extraDistortionStrenght;
    
    float4 baseColor = tex2D(uImage0, warpedCoords);
    
    float opacity = 1;
    
    //Fade the sprite from the bottom more or less in a sine pattern so that the feet are either totally obscured or not
    float fadeTreshold = 0.25 + 0.25 * sin(time * 0.8 + idOffset * 0.5);
    opacity *= fadeTreshold + (1 - fadeTreshold) * (1 - frameUv.y);
    
    //Opacity should be powed between 1 and 2
    float opacityPowSine = 1.5 + 0.5 * sin(time * 0.95 + idOffset * 0.6);
    opacity = pow(opacity, opacityPowSine);
    

    //Tint to cyan
    float screenSine = 0.8 + 0.2 * sin(time * 0.6 + idOffset * 0.6 + 0.6);
    float3 screenedColor = screen(baseColor.rgb, screenLayerColor);
    baseColor.rgb = lerp(baseColor.rgb, screenedColor, (0.5 - 0.5 * frameUv.y) * screenSine) * baseColor.a;
    
    return baseColor * sampleColor * opacity;
}

technique Technique1
{
    pass DesertGraveGhostPass
    {
        PixelShader = compile ps_3_0 Recolor();
    }
}