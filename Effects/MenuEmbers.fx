texture displaceNoise;
sampler2D samplerTex = sampler_state { texture = <displaceNoise>;  magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap;};

texture sizeWarbleNoise;
sampler2D sizeWarble = sampler_state { texture = <sizeWarbleNoise>;  magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap;};


float time;
float pixelRes;
float3 emberColor;
float3 emberColor2;

float4 Main(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR
{
    float2 uv = coords - float2(0.5, 0.5);

    float2 displaceUv = uv + (tex2D(samplerTex, uv * 0.6 + float2(0.3 + sampleColor.r, time * 0.4)).rg - float2(0.5, 0.5)) * .102;
    float2 pixelUv = floor(displaceUv * pixelRes) / pixelRes;

    float radial = atan2(pixelUv.y, pixelUv.x);
    float radius = length(pixelUv) / 0.5;

    float gradient = tex2D(sizeWarble, float2(radial * 0.1, radius + time * 0.3 + sampleColor.r));
    float value = step(radius + gradient * 0.1, 0.5);
    value -= step(0.45, radius + gradient * 0.1) * 0.4;

    
    //Bloom external
    float unpixelLenght = length(uv) / 0.5;
    value +=pow( max(0, 1 - unpixelLenght), 0.6) * 0.3;
    
    float hasHole = step(sampleColor.b, 0.2);
//Hole inside
    value -= step(radius + gradient * 0.1, 0.35) * 0.7 * hasHole;
    value -= step(radius + gradient * 0.1, 0.3) * 0.7 * hasHole;
    
    float3 usedColor = lerp(emberColor, emberColor2, sampleColor.g);

    return float4(value * usedColor.x, value * usedColor.y, value * usedColor.z, value) * sampleColor.a;
}

technique Technique1
{
    pass MenuEmbersPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}