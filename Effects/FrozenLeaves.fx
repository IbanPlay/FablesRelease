matrix uWorldViewProjection;

float4 frame;
float2 textureResolution;
float time;
float2 noiseScale;
float2 dustThresholds;

float2 satTint;
float4 hueTint;

texture sampleTexture;
sampler2D samplerTex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = wrap;
    AddressV = wrap;
};

texture noiseTexture;
sampler2D noiseTex = sampler_state
{
    texture = <noiseTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = POINT;
    AddressU = wrap;
    AddressV = wrap;
};


struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float3 rgbToHsl(float3 rgb)
{
    float colorMax = max(rgb.r, max(rgb.g, rgb.b));
    float colorMin = min(rgb.r, min(rgb.g, rgb.b));
    float brightnessVariance = colorMax - colorMin;

    float hue = 0;
    float lightness = (colorMax + colorMin) / 2;
    float saturation = brightnessVariance == 0 ? 0 : (brightnessVariance / (1 - abs(2 * lightness - 1)));

    if (brightnessVariance == 0)
        hue = 0;

    else if (colorMax == rgb.r)
        hue = fmod((rgb.g - rgb.b) / brightnessVariance, 6);
    else if (colorMax == rgb.g)
        hue = (rgb.b - rgb.r) / brightnessVariance + 2;
    else
        hue = (rgb.r - rgb.g) / brightnessVariance + 4;

    return float3(hue / 6, saturation, lightness);
}  


float3 hslToRgbWithPresetHue(float3 hsl)
{
    float chroma = (1 - abs(2 * hsl.b - 1)) * hsl.g;
    //return (float3(0, 0.82, 1) * 0.5) * chroma + hsl.b;

    return hueTint.rgb * chroma + (hsl.z - chroma/2);
}

float3 hslToRgb(float3 hsl)
{
    float R = abs(hsl.r * 6 - 3) - 1;
    float G = 2 - abs(hsl.r * 6 - 2);
    float B = 2 - abs(hsl.r * 6 - 4);
    float3 pureRGB = saturate(float3(R,G,B));

    float chroma = (1 - abs(2 * hsl.b - 1)) * hsl.g;
    return (pureRGB ) * chroma +  (hsl.z - chroma/2);
}

float3 hslToRgbWithPresetSat(float3 hsl)
{
    float R = abs(hsl.r * 6 - 3) - 1;
    float G = 2 - abs(hsl.r * 6 - 2);
    float B = 2 - abs(hsl.r * 6 - 4);
    float3 pureRGB = saturate(float3(R,G,B));



    float chroma = (1 - abs(2 * hsl.z - 1)) * satTint.r;
    return (pureRGB * 0.5) * chroma + hsl.z;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates.xy;
    float2 frameUV = (input.TextureCoordinates * frame.zw + frame.xy) / textureResolution;
    float4 color = tex2D(samplerTex, frameUV);

    float3 hsl = rgbToHsl(color.rgb);
    color.rgb = lerp(color.rgb, hslToRgbWithPresetHue(hsl), hueTint.a);
    color.rgb = lerp(color.rgb, hslToRgbWithPresetSat(hsl), satTint.g);


    return color * input.Color * color.a;
}

technique Technique1
{
    pass FrozenLeavesPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}