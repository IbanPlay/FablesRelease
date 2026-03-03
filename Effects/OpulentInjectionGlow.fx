matrix uWorldViewProjection;

float4 Color;
float2 Resolution;

float Intensity = 1;
float ConeHeight = 0.5f;
float FadeHeight = 0.25f;
float FadePower = 1;
float CenterFadePower = 1;

float4 main(float2 uv : TEXCOORD) : COLOR
{
    // Pixelation
    uv.x -= uv.x % (1 / Resolution.x);
    uv.y -= uv.y % (1 / Resolution.y);

    float2 modUV = uv;

    // Shrinks the width depending on the height, to create a funnel-like shape
    float width = ConeHeight + pow(modUV.y, 3) * (1 - ConeHeight);
    modUV.x = modUV.x * 2 * width - 1 * width + 0.5f;

    // Throw away pixels outside of the cone shape
    clip(1 - abs(modUV.x * 2 - 1));  
    
    float fresnel = 1.2f * pow(abs(modUV.x * 2 - 1), CenterFadePower) * min((1 - abs(modUV.x * 2 - 1)) * 10, 1);
    
    // Warps the horizontal uv to give the appearance of a 3d funnel
    modUV.x = acos(modUV.x * 2 - 1) / 3.1415;
    modUV.x /= 2;
    modUV.y /= 2;
    modUV.x -= modUV.y;

    // Fade cone as Y coord increases
    float heightFactor = FadeHeight / (1 - FadeHeight);
    float fadeMap = 0;
    if (uv.y > FadeHeight)
        fadeMap = saturate(0.5f * pow((1 + heightFactor) * uv.y - heightFactor, FadePower));
    
    float4 color = Color * lerp(0, 0.5f, fadeMap) * Intensity;
    
    // Make it more transparent in the middle, and smooths the edge cutoff
    color *= fresnel * 0.7f + 0.3f;
    
    // Fade cone at the bottom to make a smooth transition
    color *= min((1 - uv.y) * 10, 1);
    
    return color;
}

technique Technique1
{
    pass OpulentInjectionGlowPass
    {
        PixelShader = compile ps_3_0 main();
    }
}