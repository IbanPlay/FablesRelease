sampler2D uImage0 : register(s0);
float useLight;
float3 uColor;
float4 uSecColor;
float uAlpha;
float2 textureSize;

float4 startingColor;
float4 endingColor;
float4 outlineColor;

float time;
texture noiseTex;
sampler2D noiseSampler = sampler_state { texture = <noiseTex>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float2 resize(float2 coords, float2 offset)
{
	return ((coords * textureSize) + offset) / textureSize;
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float edges = 0 == length(tex2D(uImage0, resize(uv, float2(1, 0))).rgba) / 4;
    edges += 0 == length(tex2D(uImage0, resize(uv, float2(0, 1))).rgba) / 4;
    edges += 0 == length(tex2D(uImage0, resize(uv, float2(-1, 0))).rgba) / 4;
    edges += 0 == length(tex2D(uImage0, resize(uv, float2(0, -1))).rgba) / 4;
    
    float4 color = tex2D(uImage0, uv);
    
    float noiseOpacity = tex2D(noiseSampler, (uv + float2(time * 1, 0)) * 4).x * tex2D(noiseSampler, (uv + float2(0, time * 1.5)) * 4).x;
    float completion = clamp(color.r / (color.r + color.g) * (0.8 + 0.2 * noiseOpacity), 0, 1);
    
    float edgeMask = edges > 0;
    //return color;
    
    float4 outline = outlineColor * (0.8 + 0.2 * noiseOpacity) * lerp(startingColor.a, endingColor.a, completion) * edgeMask;
    float4 fill = lerp(startingColor, endingColor, completion) * (0.5 + 0.5 * noiseOpacity) * (1 - edgeMask);
    
    return (fill + outline) * color.a;
}

technique Technique1
{
    pass SpectralWaterLayerPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}