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

texture noiseTexture;
sampler2D noise = sampler_state
{
    texture = <noiseTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};


float2 noiseStretch;
float2 noiseOffset;
float fadeProgress;

float4 Recolor(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(samplerTex, coords);
    float originalAlpha = color.a * sampleColor.a;
    
    
    float fadeCoords = 1 -  smoothstep(0.4, 0.6, coords.y);
    float2 noiseCoords = coords * noiseStretch + noiseOffset;
    float fade = step(0, tex2D(noise, noiseCoords ).r - fadeProgress + fadeCoords);
    
    return sampleColor * originalAlpha * fade;
}

technique Technique1
{
    pass BurntCorpseFadePass
    {
        PixelShader = compile ps_2_0 Recolor();
    }
}