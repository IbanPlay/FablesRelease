float4 greenScreenColor;

texture sampleTexture;
sampler2D decalSampler = sampler_state { texture = <sampleTexture>; magfilter = POINT; minfilter = POINT; mipfilter = POINT;  AddressU = clamp; AddressV = clamp;};

texture canvas;
sampler2D canvasSampler = sampler_state { texture = <canvas>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

float4 Decal(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(canvasSampler, coords);
    float mask = length(color - greenScreenColor) / 4 < 0.1;
    return tex2D(decalSampler, coords) * mask;
}

technique Technique1
{
    pass UIButtonDecalPass
    {
        PixelShader = compile ps_2_0 Decal();
    }
}