texture sampleTexture;
sampler2D samplerTex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = clamp;
    AddressV = clamp;
};

texture noiseTexture;
sampler2D noiseMap = sampler_state
{
    texture = <noiseTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};


float time;
float minRadius;
float2 lerpStrenght;
float4 color;
float noiseScale;
float radiusDisplacement;


float realCos(float value)
{
    return sin(value + 1.57079);
}

float4 MainPS(float2 coords : TEXCOORD0) : COLOR
{
    float angle = (atan2(coords.y - 0.5, coords.x - 0.5) + 3.14) / 6.28;
    float radius = length(coords - float2(0.5, 0.5));
    float fade = 1;
    if (radius > 0.5)
        fade = 1 - min(((radius - 0.5) / 0.2), 1.0);
    
    //radius = lerp(radius, min(radius, minRadius), tex2D(noiseMap, float2(angle, time)).x * lerpStrenght);
    radius = lerp(radius, max(minRadius, radius - radiusDisplacement), tex2D(noiseMap, float2(angle * noiseScale, time)).x * lerpStrenght);
    
    float4 col = tex2D(samplerTex, float2(0.5, 0.5 + radius)) * fade;
    col *= color;
    return col;

}


technique Technique1
{
    pass RadialWarpPass
    {
        PixelShader = compile ps_2_0 MainPS();
    }
}