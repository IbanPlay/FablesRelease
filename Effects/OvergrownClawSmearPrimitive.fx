uint repeats = 1;

float time;
float4 edgeSliceColor;
float4 edgeFadeColor;
float4 flecksColor;
float4 flecksHorizontalFade;
float intensity;

matrix uWorldViewProjection;

texture sampleTexture;
sampler2D samplerTexture = sampler_state { texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

struct VertexShaderIn 
{
    float4 Position : POSITION;
    float2 TexCoords : TEXCOORD;
    float4 Color : COLOR0;
};

struct VertexShaderOut
{
    float4 Position : POSITION;
    float2 TexCoords : TEXCOORD;
    float4 Color : COLOR0;
};

VertexShaderOut VertexShaderFunc(VertexShaderIn input)
{
    VertexShaderOut output;
    output.Color = input.Color;
    output.TexCoords = input.TexCoords;
    output.Position = mul(input.Position, uWorldViewProjection);
    return output;
}

float4 PixelShaderFunc(VertexShaderOut input) : COLOR0
{
    float4 noise = tex2D(samplerTexture, float2(input.TexCoords.x / 2.0f + frac(time * 0.5f), input.TexCoords.y * 2.0f));
    float4 flecksNoise = tex2D(samplerTexture, float2((input.TexCoords.x + frac(time * 0.8f / 6.0f) * 6.0f) / 6.0f, input.TexCoords.y * .7f));

    float4 edgeSlice = 1.0f - noise.rgba;
    float4 flecks = saturate(flecksNoise - .35f);

    edgeSlice.rgba = saturate(edgeSlice.rgba * .23f + (.5f - input.TexCoords.y * .5f));

    edgeSlice.rgba -= saturate(1.0f - input.TexCoords.x - .5f) * .7f;

    edgeSlice.rgba = round(edgeSlice.r * intensity);
    flecks.rgba = round(flecks.r * intensity);

    float4 color = edgeSlice * lerp(edgeSliceColor, edgeFadeColor, ceil(pow(1.0f - input.TexCoords.y, 6) * 3.0f) / 3.0f);
    
    float flecksMask = flecks.a > 0;
    color = color * (1 - flecksMask) + flecks * lerp(flecksColor, flecksHorizontalFade, pow(input.TexCoords.x, 2)) * flecksMask;
    return color;
}

technique Technique1
{
    pass SmokeTrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunc();
        PixelShader = compile ps_2_0 PixelShaderFunc();
    }
};