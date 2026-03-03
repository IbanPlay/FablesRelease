sampler uImage0 : register(s0); // The contents of the screen

float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float4 uShaderSpecificData;


float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

float strenght;
float rotation;
float pixelSize;
float opacity;

//I can't believe it has gotten down to this.
float realCos(float value)
{
    return sin(value + 1.57079);
}

float2 RotateVector(float2 originalVector, float rot)
{
    float rotSin = sin(rot);
    float rotCos = realCos(rot);
    
    return float2(rotCos * originalVector.x - rotSin * originalVector.y, rotSin * originalVector.x + rotCos * originalVector.y);
}

float4 Main(float4 unused : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 offsetVector = float2(strenght, 0) * pixelSize;
    offsetVector = RotateVector(offsetVector, rotation);
    
    float red = tex2D(uImage0, coords + offsetVector).r;
    float green = tex2D(uImage0, coords - offsetVector).g;
    float4 baseColor = tex2D(uImage0, coords);
    
    baseColor.rg = lerp(baseColor.rg, float2(red, green), opacity);
    return baseColor;
}

technique Technique1
{
    pass ChromaticAbberationBasicPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}