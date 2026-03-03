texture sampleTexture;
sampler2D samplerTex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = clamp;
    AddressV = clamp;
};

float4 recolor;

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(samplerTex, uv);
    
    float4 returnColor = recolor;
    returnColor *= color.a;
    return returnColor;
}

technique Technique1
{
    pass SilouetteifyPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}