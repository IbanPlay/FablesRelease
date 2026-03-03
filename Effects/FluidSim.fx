sampler simGrid : register(s0);
texture simGridButLinear;
sampler2D simGridLinear = sampler_state
{
    texture = <simGridButLinear>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = clamp;
    AddressV = clamp;
};

texture divergenceGrid;
sampler2D divergenceSampler = sampler_state
{
    texture = <divergenceGrid>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = clamp;
    AddressV = clamp;
};

matrix drawDataMatrix;
texture drawDataTexture;
sampler2D drawData = sampler_state
{
    texture = <drawDataTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = clamp;
    AddressV = clamp;
};

float cellSize;
float2 step;
float2 stepX;
float2 stepY;

float4 diffuse_strength;
float4 diffuse_dissipation;
float advect_strenght;
float2 advect_additionalVelocity;

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
    float4 pos = mul(input.Position, drawDataMatrix);
    output.Position = pos;
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 DrawTexture(VertexShaderOutput input) : COLOR0
{
    return tex2D(drawData, input.TextureCoordinates).a * input.Color;
}

float4 DrawOmnidirectional(VertexShaderOutput input) : COLOR0
{
    float2 centeredUv = input.TextureCoordinates - float2(0.5, 0.5);
    float distToCenter = length(centeredUv) * 2;
    
    //Crop in a circle
    float mask = distToCenter < 1;
    mask *= pow(1 - distToCenter, input.Color.g);
    
    //since its omnidirectional we only look at the red channel for the velocity's speed
    float2 outpourVelocity = input.Color.r * normalize(centeredUv);
    return float4(outpourVelocity, input.Color.ba) * mask;
}


float4 Diffuse(float2 coords : TEXCOORD0) : COLOR
{
    //All the max clamps are because we use the substract blend mode to create the fade zones at the borders, which may put some of the values into the negatives
    float4 myValue = tex2D(simGrid, coords);
    float4 left =tex2D(simGrid, coords - stepX);
    float4 right = tex2D(simGrid, coords + stepX);
    float4 top = tex2D(simGrid, coords - stepY);
    float4 bot = tex2D(simGrid, coords + stepY);
    
    myValue.z = max(0, myValue.z);
    left.z = max(0, left.z);
    right.z = max(0, right.z);
    top.z = max(0, top.z);
    bot.z = max(0, bot.z);
    
    float4 lerpedVal = myValue + diffuse_strength * (left + right + top + bot - 4 * myValue) / (cellSize * cellSize);
    //Theres no negative density
    lerpedVal.z = max(0, lerpedVal.z);
    
    return lerpedVal * diffuse_dissipation;
}

float4 Advect(float2 coords : TEXCOORD0) : COLOR
{
    float4 myValue = tex2D(simGrid, coords);
    float2 lastCoords = coords - (myValue.xy * advect_strenght + advect_additionalVelocity) / cellSize;
    
    //Get the bilinear interpolation done for free by using the linear sampling of the texture <3
    return tex2D(simGridLinear, lastCoords);
}

float4 InitializeDivergence(float2 coords : TEXCOORD0) : COLOR
{
    float pressure = tex2D(simGrid, coords + stepX).x - tex2D(simGrid, coords - stepX).x + tex2D(simGrid, coords + stepY).y - tex2D(simGrid, coords - stepY).y;
    return pressure * 0.5;
}

float4 IterateProjection(float2 coords : TEXCOORD0) : COLOR
{
    float pressureSum = tex2D(simGrid, coords + stepX) + tex2D(simGrid, coords - stepX) + tex2D(simGrid, coords + stepY) + tex2D(simGrid, coords - stepY);
    pressureSum -= tex2D(divergenceSampler, coords);
    
    return pressureSum * 0.25;
}

float4 ClearDivergence(float2 coords : TEXCOORD0) : COLOR
{
    float4 value = float4(0, 0, 0, 0);
    value.x = (tex2D(simGrid, coords + stepX) - tex2D(simGrid, coords - stepX)) * 0.5f;
    value.y = (tex2D(simGrid, coords + stepY) - tex2D(simGrid, coords - stepY)) * 0.5f;
    return value;
}

technique Technique1
{
    pass DrawTexturePass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 DrawTexture();
    }

    pass DrawOmnidirectionalTexturePass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 DrawOmnidirectional();
    }

    pass DiffusionPass
    {
        PixelShader = compile ps_3_0 Diffuse();
    }

    pass AdvectionPass
    {
        PixelShader = compile ps_3_0 Advect();
    }

    pass DivergenceInitializationPass
    {
        PixelShader = compile ps_3_0 InitializeDivergence();
    }

    pass IterateProjectionPass
    {
        PixelShader = compile ps_3_0 IterateProjection();
    }

    pass ClearDivergencePass
    {
        PixelShader = compile ps_3_0 ClearDivergence();
    }
}