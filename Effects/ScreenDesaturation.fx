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

float desatPercent;

/*
float Epsilon = 1e-10;
 
float3 RGBtoHCV(in float3 RGB)
{
    // Based on work by Sam Hocevar and Emil Persson
    float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0 / 3.0) : float4(RGB.gb, 0.0, -1.0 / 3.0);
    float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
    float C = Q.x - min(Q.w, Q.y);
    float H = abs((Q.w - Q.y) / (6 * C + Epsilon) + Q.z);
    return float3(H, C, Q.x);
}

float3 RGBtoHSL(in float3 RGB)
{
    float3 HCV = RGBtoHCV(RGB);
    float L = HCV.z - HCV.y * 0.5;
    float S = HCV.y / (1 - abs(L * 2 - 1) + Epsilon);
    return float3(HCV.x, S, L);
}

float3 HUEtoRGB(in float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R, G, B));
}

float3 HSLtoRGB(float3 hsl)
{
    if (hsl.g == 0)
        return float3(hsl.b, hsl.b, hsl.b);
    
    float3 RGB = HUEtoRGB(hsl.x);
    float C = (1 - abs(2 * hsl.z - 1)) * hsl.y;
    return (RGB - 0.5) * C + hsl.z;
}
*/


float4 Main(float4 unused : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float brightness = dot(color.rgb, float3(0.299, 0.587, 0.114));
    float3 desatColor = float3(brightness, brightness, brightness);
    
    return float4(lerp(color.rgb, desatColor, desatPercent), color.a);
}

technique Technique1
{
    pass ScreenDesaturationPass
    {
        PixelShader = compile ps_2_0 Main();
    }
}