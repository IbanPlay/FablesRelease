texture sampleTexture;
sampler2D samplerTex = sampler_state { texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

texture zapTexture;
sampler2D zapTex = sampler_state { texture = <zapTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };


float fresnelStrenght = 7.0;
float fresnelAdd = 0.15;

float coreSolidRadius = 0.6 ;
float coreFadeRadius = 0.2;
float coreFadeStrenght = 3;

float4 coreColor = float4(0.9, 1.0, 1.2, 1.0);
float4 edgeColor = float4(0.3, 0.5, 0.85, 1.0);
float4 zapColor = float4(0.1, 0.16, 0.26, 0.3);

float edgeFadeDistance = 0.05;
float blowUpPower = 0.2;
float blowUpSize = 0.2;

float time;
float maxRadius = 1;
float2 resolution;

float invlerp(float from, float to, float value)
{
    return clamp((value - from) / (to - from), 0.0, 1.0);
}

float Fresnel(float2 uv)
{
    float2 fresnelCoords = float2(uv.x, time * 0.2);
    float fresnelBoost = tex2D(samplerTex, fresnelCoords).x;
    //float fresnelBoost2 = tex2D(iChannel2, fresnelCoords).x;
    
    float fresnel = pow(min(uv.y / maxRadius + fresnelBoost * fresnelAdd, 1.0), fresnelStrenght); // * (1.0 + fresnelBoost2 * 0.2);
    fresnel += pow(min(uv.y, 1.0), fresnelStrenght * 4.0) * 2.5;
    return fresnel;
}

float GlowCore(float2 uv)
{
    return pow(invlerp(coreSolidRadius + coreFadeRadius, coreSolidRadius, uv.y), coreFadeStrenght);
}

float Zaps(float2 uv, float radius)
{
    float blownUpUVX = pow(radius, 1.2);
    float2 blownUpUV = float2(blownUpUVX - 0.3 * time, uv.y);
    
    float2 timeScroll = float2(time, time) * 0.1;
    float zap = tex2D(zapTex, blownUpUV * 0.3 + timeScroll).x;
    zap += tex2D(zapTex, blownUpUV * 0.3 - timeScroll).x;
    return zap;
}


float4 MainPS(float2 coords : TEXCOORD0) : COLOR
{
    //Pixelate?
    coords.x -= coords.x % (1 / resolution);
    coords.y -= coords.y % (1 / resolution);
    
    float angle = (atan2(coords.y - 0.5, coords.x - 0.5) + 3.14) / 6.28;
    float radius = length(coords - float2(0.5, 0.5)) * 2;
    
    radius = lerp(radius, maxRadius, tex2D(samplerTex, float2(angle, time * 0.1 - radius)).x * 0.16);
    
    float circleMask = radius <= maxRadius;
    float2 sphereUv = float2(angle, radius);

    float4 fresnel = Fresnel(sphereUv) * edgeColor;
    float4 core = GlowCore(sphereUv) * coreColor;
    float4 zaps = Zaps(coords, radius) * zapColor;
    
    float4 color = fresnel + core + zaps;
    
    //Fade at the edges
    color *= invlerp(maxRadius, maxRadius - edgeFadeDistance, radius);
    
    color.a = min(1, color.a);
    color.xyz *= color.a;
    return color * circleMask;
}


technique Technique1
{
    pass ElectroOrbPass
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}