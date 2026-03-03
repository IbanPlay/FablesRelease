float3 color;
float3 darkerColor;
float bloomSize;
float bloomMaxOpacity;
float bloomFadeStrenght;
float mainOpacity;
float laserAngle;
float laserWidth;
float laserLightStrenght;
float noiseOffset;

float2 Resolution;

texture sampleTexture;
sampler2D Texture1Sampler = sampler_state { texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

texture sampleTexture2;
sampler2D NoiseMap = sampler_state { texture = <sampleTexture2>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };



float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 garbage = tex2D(Texture1Sampler, uv);
    garbage *= 0.0;
    garbage += tex2D(NoiseMap, uv);
    
    return garbage;
    
}

technique Technique1
{
    pass SecondaryTextureEffectTestPass
    {
        PixelShader = compile ps_2_0 main();
    }
}