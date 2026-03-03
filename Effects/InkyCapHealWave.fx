texture voronoi;
sampler2D voronoiTex = sampler_state { texture = <voronoi>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

texture noiseOverlay;
sampler2D noiseOverlayTex = sampler_state { texture = <noiseOverlay>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float repeats = 2;
float waveLenght; //The lenght of the actual visible portion of the trail. This effect is intended to be used on full circle effects
float2 trailStretch;
float trailScroll;
float3 colorMultiplier;
float verticalFadeStart; //Percentage of the thickness where the fade starts

matrix uWorldViewProjection;

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
    VertexShaderOutput output = (VertexShaderOutput)0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    float2 stretchedUV = coords;
    stretchedUV.x *= repeats;
    stretchedUV.x %= 1;
    stretchedUV.x /= waveLenght;
    
    float4 multiplier = float4(1, 1, 1, 1);
    
    //This ensures that nothing past the actual solid wave is considered. We don't want x coordinates higher than 1
    multiplier *= step(stretchedUV.x, 1);
    
    //The effect gets thicker near the center
    float thickness = sin(stretchedUV.x * 3.14159265);
    multiplier *= 1 - smoothstep(thickness * verticalFadeStart * 0.5, thickness * 0.5, abs(stretchedUV.y - 0.5));
    
    stretchedUV.y = ((stretchedUV.y - 0.5) * trailStretch.y) + 0.5;
    float4 trailColor = tex2D(noiseOverlayTex, float2(stretchedUV.x * trailStretch.x + trailScroll, stretchedUV.y));
    color.xyz *= colorMultiplier;
        
    return trailColor * color * multiplier;
}

technique Technique1
{
    pass InkyCapHealWavePass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
