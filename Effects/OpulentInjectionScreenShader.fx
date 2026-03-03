sampler uImage0 : register(s0); // The contents of the screen
sampler distortionTex : register(s1);

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

float vignetteBrightness;

float4 Main(float4 unused : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 distortion = tex2D(distortionTex, coords * 0.5 + float2(uTime * 0.04f, uTime * 0.01f)) + tex2D(distortionTex, coords * 0.5 + 0.3 - float2(uTime * 0.04f, 0));
    float fade = max(uOpacity, vignetteBrightness);
    //vignette value
    float vignetteFac = length(float2(0.5, 0.5) - coords) * fade;
    //warping
    float2 modUV = coords + distortion.xy * 0.015 * max(vignetteFac - 0.2, 0) * (1 + vignetteBrightness);
    float4 screen = tex2D(uImage0, modUV);
    //vignette tint
    float4 vignetteColor = lerp(float4(uColor, 1), float4(1, 1, 1, 1), vignetteBrightness * 0.5);
    screen = lerp(screen, vignetteColor, max(vignetteFac / 0.7 - 0.4 + sin(uTime * 0.4) * 0.05 + vignetteBrightness * 0.2, 0) * uOpacity * ((screen.r + screen.g + screen.b) / 3 * 0.9 + 0.1));
    //exposure
    screen *= (1 + (vignetteFac) * 0.8 * uOpacity + vignetteBrightness * 0.2);
    return screen;
}

technique Technique1
{
    pass OpulentInjectionScreenShaderPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}