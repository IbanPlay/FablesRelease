matrix uWorldViewProjection;

texture layer1Texture;
sampler2D layer1Tex = sampler_state { texture = <layer1Texture>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };
texture layer1GlowRed;
sampler2D layer1red = sampler_state { texture = <layer1GlowRed>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };
texture layer1GlowBlue;
sampler2D layer1blu = sampler_state { texture = <layer1GlowBlue>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

texture layer2Texture;
sampler2D layer2Tex = sampler_state {texture = <layer2Texture>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp;  AddressV = clamp;};
texture layer2GlowRed;
sampler2D layer2red = sampler_state {texture = <layer2GlowRed>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp;  AddressV = clamp;};
texture layer2GlowBlue;
sampler2D layer2blu = sampler_state {texture = <layer2GlowBlue>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp;  AddressV = clamp;};

texture layer3Texture;
sampler2D layer3Tex = sampler_state { texture = <layer3Texture>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp;};
texture layer3GlowRed;
sampler2D layer3red = sampler_state { texture = <layer3GlowRed>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp;};
texture layer3GlowBlu;
sampler2D layer3blu = sampler_state { texture = <layer3GlowBlu>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp;};



float3 parallaxStrenght;
float2 parallaxDisplace;
float3 layerTints;
float blueness;
float flameFlicker;
float opacity;

float4 alphaBlend(float4 background, float4 overlay)
{
    return float4(background.rgb * (1 - overlay.a) + overlay.rgb * overlay.a, min(1, background.a + overlay.a));
}


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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = float2(input.TextureCoordinates.x, input.TextureCoordinates.y);
    
    float4 color1 = tex2D(layer1Tex, coords + parallaxDisplace * parallaxStrenght.x);
    float4 color2 = tex2D(layer2Tex, coords + parallaxDisplace * parallaxStrenght.y);
    float4 color3 = tex2D(layer3Tex, coords + parallaxDisplace * parallaxStrenght.z);
    
    float4 color1glow = lerp(tex2D(layer1red, coords + parallaxDisplace * parallaxStrenght.x), tex2D(layer1blu, coords + parallaxDisplace * parallaxStrenght.x), blueness) * 0.2;
    float4 color2glow = lerp(tex2D(layer2red, coords + parallaxDisplace * parallaxStrenght.y), tex2D(layer2blu, coords + parallaxDisplace * parallaxStrenght.y), blueness) * 0.8;
    float4 color3glow = lerp(tex2D(layer3red, coords + parallaxDisplace * parallaxStrenght.z), tex2D(layer3blu, coords + parallaxDisplace * parallaxStrenght.z), blueness);
    
    color1.rgb *= layerTints.x;
    color2.rgb *= layerTints.y;
    color3.rgb *= layerTints.z;
    
    color3.rgb += color3glow * flameFlicker;
    color2.rgb += color2glow * flameFlicker;
    
    float4 topLayer = alphaBlend(color2, color1);
    return (alphaBlend(color3, topLayer) + color1glow * flameFlicker) * input.Color * opacity;
}

technique Technique1
{
    pass SealedChamberParallaxPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}