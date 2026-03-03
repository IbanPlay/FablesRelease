sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
matrix uWorldViewProjection;

float edgeSize; //The thickness of the edge visuals
float edgeSizePower; //The power at which this thickness shrinks along the trail
float edgeTransitionSize; //The thickness of the "edge transition" zone, between the edge and the rest of the slash
float edgeTransitionOpacity; //The opacity of the "edge transition" zone. The transition recieves the same effects as the edge multiplied by this opacity

float4 edgeColorMultiplier; //A color multiplier applied to the trail's edge
float4 edgeColorAdd; //An additive color applied to the trail's edge

float horizontalPower; //The power at which the trail fades off alongside the lenght of the slash
float verticalPower; //The power at which the trail fades off alongside the height of the sword

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
    float4 baseColor = input.Color;
    float2 coords = input.TextureCoordinates;
    
    float actualEdgeSize = edgeSize * pow(coords.x, edgeSizePower);
    float isEdge = step(coords.y, actualEdgeSize);
    isEdge += step(coords.y, actualEdgeSize + edgeTransitionSize) * (1 - isEdge) * edgeTransitionOpacity;
    
    baseColor *= 1 + isEdge * edgeColorMultiplier;
    baseColor += isEdge * edgeColorAdd;
    
    baseColor *= pow(1 - coords.y, verticalPower) * pow(coords.x, horizontalPower);
    
    return baseColor;
}

technique Technique1
{
    pass SlicePrimitivePass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
