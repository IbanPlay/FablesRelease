sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
texture mask;
sampler MaskSampler = sampler_state
{
    Texture = (mask);
};
///float2 maskOffset;
//float2 maskScale;

float4 Main(float2 coords : TEXCOORD0) : COLOR
{
    float4 color = tex2D(uImage0, coords);
    float4 maskColor = tex2D(MaskSampler, coords); // * maskScale + maskOffset);
    return color * maskColor.a;
}

technique Technique1
{
    pass LayerMaskPass
    {
        PixelShader = compile ps_2_0 Main();
    }
}