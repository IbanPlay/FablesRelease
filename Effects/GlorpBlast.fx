texture voronoi;
sampler2D voronoiTex = sampler_state { texture = <voronoi>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

texture noiseOverlay;
sampler2D noiseOverlayTex = sampler_state { texture = <noiseOverlay>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float repeats = 2;
float time;
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

//Used for the carrion detonator blast

// The X coordinate is the trail completion, the Y coordinate is the same as any other.
// This is simply how the primitive TextCoord is layed out in the C# code.
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    float2 stretchedUV = coords;
    stretchedUV.x *= repeats;
    stretchedUV.y *= 0.6;
    
    //Get scrolling voronoi
    float col = 1.0 - pow(tex2D(voronoiTex, stretchedUV * 0.26 + float2(time * 0.25, 0)).x, 0.8);
    
    float halfPos = 0.5 - abs(coords.y - 0.5);
    //Add fade to the edge
    if (halfPos < 0.05)
        col *= halfPos / 0.05;
        
    if (halfPos > 0.1)
        col *= 1 + 2.3 * (halfPos - 0.1) / 0.4;
    
    //Multiply by noise
    col *= (0.7 + 0.3 * tex2D(noiseOverlayTex, (stretchedUV + float2(time * 0.24, 0)) * 0.6).x);
    
    //Add posterization effects
    col *= 1.0 - step(col, 0.4);
    if (col > step(col, 0.95))
        col = 1.0;
    else if (col > step(col, 0.8))
        col = 0.9;
        
    return float4(color.xyz * color.a * col, color.a * col);
}

technique Technique1
{
    pass GlorpBlastPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
